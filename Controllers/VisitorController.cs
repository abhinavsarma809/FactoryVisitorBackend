using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using FactoryVisitorApp.Data;
using FactoryVisitorApp.Models;
using System.Security.Cryptography;
using System.Text;
using System.ComponentModel.Design;

namespace FactoryVisitorApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VisitorController : ControllerBase
    {
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
        }
        [HttpPost("insertzone")]
        public IActionResult InsertZone([FromBody] Zone zone)
        {
            using var conn = DBHelper.GetConnection();
            conn.Open();

            string query = "INSERT INTO Zones (ZoneName, Description) VALUES (@ZoneName, @Description)";
            var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@ZoneName", zone.ZoneName);
            cmd.Parameters.AddWithValue("@Description", zone.Description);
            cmd.ExecuteNonQuery();

            return Ok(new { message = "Zone inserted successfully." });
        }


        [HttpPost("register")]
        public IActionResult Register([FromBody] Visitor visitor)
        {
            visitor.PasswordHash = HashPassword(visitor.PasswordHash);

            using var conn = DBHelper.GetConnection();
            conn.Open();

            string query = @"INSERT INTO Visitors (FullName, ContactNumber, Email, Purpose, ZoneID, PasswordHash, Gender)
                     VALUES (@FullName, @ContactNumber, @Email, @Purpose, @ZoneID, @PasswordHash, @Gender)";
            var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@FullName", visitor.FullName);
            cmd.Parameters.AddWithValue("@ContactNumber", visitor.ContactNumber);
            cmd.Parameters.AddWithValue("@Email", visitor.Email);
            cmd.Parameters.AddWithValue("@Purpose", visitor.Purpose);
            cmd.Parameters.AddWithValue("@ZoneID", visitor.ZoneID);
            cmd.Parameters.AddWithValue("@PasswordHash", visitor.PasswordHash);
            cmd.Parameters.AddWithValue("@Gender", visitor.Gender);

            cmd.ExecuteNonQuery();

            return Ok(new { message = "Visitor registered successfully.", gender = visitor.Gender });
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] VisitorLoginDto visitor)
        {
            string hashedPassword = HashPassword(visitor.PasswordHash);

using var conn = DBHelper.GetConnection();
conn.Open();


string query = "SELECT VisitorID, FullName FROM Visitors WHERE Email=@Email AND PasswordHash=@PasswordHash";
var cmd = new MySqlCommand(query, conn);
cmd.Parameters.AddWithValue("@Email", visitor.Email);
cmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);

using var reader = cmd.ExecuteReader();
if (reader.Read())
{
    int visitorId = reader.GetInt32("VisitorID");
    string fullName = reader.GetString("FullName");

    return Ok(new
    {
        message = "Login successful",
        visitorId = visitorId,
        fullName = fullName
    });
}
else
{
    return Unauthorized(new { message = "Invalid email or password" });
}

        }
      [HttpPost("checkin/{id}")]
public IActionResult CheckIn(int id, [FromBody] CheckInDto dto)
{
    using var conn = DBHelper.GetConnection();
    conn.Open();

    string zoneQuery = @"SELECT Z.ZoneName, Z.IsRestricted, V.Purpose 
                         FROM Visitors V 
                         JOIN Zones Z ON V.ZoneID = Z.ZoneID 
                         WHERE V.VisitorID = @VisitorID";

    var zoneCmd = new MySqlCommand(zoneQuery, conn);
    zoneCmd.Parameters.AddWithValue("@VisitorID", id);
    var reader = zoneCmd.ExecuteReader();

    if (reader.Read())
    {
        string zoneName = reader["ZoneName"].ToString();
        bool isRestricted = Convert.ToBoolean(reader["IsRestricted"]);
        string purpose = reader["Purpose"]?.ToString() ?? "Not specified";

        reader.Close();

        var checkInTime = dto.CheckInTime;

        // Update Visitors table with check-in time
        string updateQuery = "UPDATE Visitors SET CheckInTime = @CheckInTime WHERE VisitorID = @VisitorID";
        var updateCmd = new MySqlCommand(updateQuery, conn);
        updateCmd.Parameters.AddWithValue("@CheckInTime", checkInTime);
        updateCmd.Parameters.AddWithValue("@VisitorID", id);
        updateCmd.ExecuteNonQuery();

        // Log the check-in action
        string logQuery = @"INSERT INTO VisitorLogs (VisitorID, ZoneID, ActionType, Timestamp)
                            VALUES (@VisitorID, (SELECT ZoneID FROM Visitors WHERE VisitorID = @VisitorID), 'CheckIn', @Timestamp)";
        var logCmd = new MySqlCommand(logQuery, conn);
        logCmd.Parameters.AddWithValue("@VisitorID", id);
        logCmd.Parameters.AddWithValue("@Timestamp", checkInTime);
        logCmd.ExecuteNonQuery();

        // Convert UTC to IST and format
        var istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        var istTime = TimeZoneInfo.ConvertTimeFromUtc(checkInTime.ToUniversalTime(), istZone);
        string formattedTime = istTime.ToString("dd-MMM-yyyy hh:mm tt");

        var response = new
        {
            message = isRestricted ? "ALERT: Visitor entered a restricted zone!" : "Check-in recorded.",
            zone = zoneName,
            purpose = purpose,
            checkInTime = formattedTime
        };

        return Ok(response);
    }

    return NotFound(new { message = "Visitor not found." });
}

[HttpPut("access/{visitorId}")]
public IActionResult UpdateVisitorAccess(int visitorId, [FromBody] List<int> allowedZoneIds)
{
    if (allowedZoneIds == null || !allowedZoneIds.Any())
    {
        return BadRequest("Zone ID list cannot be empty.");
    }

    using var conn = DBHelper.GetConnection();
    conn.Open();

    using var transaction = conn.BeginTransaction();

    try
    {
        // Delete existing access
        var deleteCmd = new MySqlCommand("DELETE FROM VisitorZoneAccess WHERE VisitorID = @VisitorID", conn, transaction);
        deleteCmd.Parameters.AddWithValue("@VisitorID", visitorId);
        deleteCmd.ExecuteNonQuery();

        // Insert new access
        foreach (var zoneId in allowedZoneIds)
        {
            var insertCmd = new MySqlCommand("INSERT INTO VisitorZoneAccess (VisitorID, ZoneID) VALUES (@VisitorID, @ZoneID)", conn, transaction);
            insertCmd.Parameters.AddWithValue("@VisitorID", visitorId);
            insertCmd.Parameters.AddWithValue("@ZoneID", zoneId);
            insertCmd.ExecuteNonQuery();
        }

        transaction.Commit();
        return Ok(new { Message = "Visitor access updated successfully.", VisitorID = visitorId, ZonesAssigned = allowedZoneIds });
    }
    catch (Exception ex)
    {
        transaction.Rollback();
        return StatusCode(500, $"Internal server error: {ex.Message}");
    }
}



[HttpGet("access/{visitorId}")]
public IActionResult GetVisitorAccess(int visitorId)
{
    using var conn = DBHelper.GetConnection();
    conn.Open();

    var cmd = new MySqlCommand(@"
        SELECT z.ZoneID, z.ZoneName, z.Description, z.IsRestricted
        FROM VisitorZoneAccess vza
        JOIN Zones z ON vza.ZoneID = z.ZoneID
        WHERE vza.VisitorID = @VisitorID", conn);

    cmd.Parameters.AddWithValue("@VisitorID", visitorId);

    var reader = cmd.ExecuteReader();
    var allowedZones = new List<object>();

    while (reader.Read())
    {
        allowedZones.Add(new
        {
            ZoneID = reader.GetInt32("ZoneID"),
            ZoneName = reader.GetString("ZoneName"),
            Description = reader.GetString("Description"),
            IsRestricted = reader.GetBoolean("IsRestricted")
        });
    }

    return Ok(new { allowedZones });
}



        [HttpPut("update/{id}")]
public IActionResult UpdateVisitorDetails(int id, [FromBody] VisitorUpdateDto dto)
{
    using var conn = DBHelper.GetConnection();
    conn.Open();

    string updateQuery = @"UPDATE Visitors 
                           SET ZoneID = @ZoneID, Purpose = @Purpose, AuthorizedBy = @AuthorizedBy 
                           WHERE VisitorID = @VisitorID";

    using var cmd = new MySqlCommand(updateQuery, conn);
    cmd.Parameters.AddWithValue("@ZoneID", dto.ZoneID);
    cmd.Parameters.AddWithValue("@Purpose", dto.Purpose);
    cmd.Parameters.AddWithValue("@AuthorizedBy", dto.AuthorizedBy);
    cmd.Parameters.AddWithValue("@VisitorID", id);

    int rowsAffected = cmd.ExecuteNonQuery();

    if (rowsAffected > 0)
    {
        return Ok(new { message = "Visitor details updated successfully." });
    }

    return NotFound(new { message = "Visitor not found." });
}


        [HttpPost("checkout/{id}")]
        public IActionResult CheckOut(int id)
        {
            using var conn = DBHelper.GetConnection();
            conn.Open();

            var checkOutTime = DateTime.Now;

            string updateQuery = "UPDATE Visitors SET CheckOutTime = @CheckOutTime WHERE VisitorID = @VisitorID";
            var updateCmd = new MySqlCommand(updateQuery, conn);
            updateCmd.Parameters.AddWithValue("@CheckOutTime", checkOutTime);
            updateCmd.Parameters.AddWithValue("@VisitorID", id);
            updateCmd.ExecuteNonQuery();


            string infoQuery = @"SELECT V.Purpose, Z.ZoneName, Z.IsRestricted 
                         FROM Visitors V 
                         JOIN Zones Z ON V.ZoneID = Z.ZoneID 
                         WHERE V.VisitorID = @VisitorID";
            var infoCmd = new MySqlCommand(infoQuery, conn);
            infoCmd.Parameters.AddWithValue("@VisitorID", id);
            var reader = infoCmd.ExecuteReader();

            if (reader.Read())
            {
                string zoneName = reader["ZoneName"].ToString();
                string purpose = reader["Purpose"]?.ToString() ?? "Not specified";
                bool isRestricted = Convert.ToBoolean(reader["IsRestricted"]);

                reader.Close();

                string logQuery = @"INSERT INTO VisitorLogs (VisitorID, ZoneID, ActionType, Timestamp)
                            VALUES (@VisitorID, (SELECT ZoneID FROM Visitors WHERE VisitorID = @VisitorID), 'CheckOut', @Timestamp)";
                var logCmd = new MySqlCommand(logQuery, conn);
                logCmd.Parameters.AddWithValue("@VisitorID", id);
                logCmd.Parameters.AddWithValue("@Timestamp", checkOutTime);
                logCmd.ExecuteNonQuery();

                var response = new
                {
                    message = isRestricted ? "ALERT: Visitor entered a restricted area!" : "Check-out recorded.",
                    checkOutTime = checkOutTime,
                    zone = zoneName,
                    purpose = purpose
                };

                return Ok(response);
            }

            return NotFound(new { message = "Visitor not found." });
        }
        [HttpDelete("/{id}")]
public IActionResult DeleteVisitor(int id)
{
    using var conn = DBHelper.GetConnection();
    conn.Open();

    string deleteLogsQuery = "DELETE FROM VisitorLogs WHERE VisitorID = @VisitorID";
    var logCmd = new MySqlCommand(deleteLogsQuery, conn);
    logCmd.Parameters.AddWithValue("@VisitorID", id);
    logCmd.ExecuteNonQuery();

    string deleteVisitorQuery = "DELETE FROM Visitors WHERE VisitorID = @VisitorID";
    var visitorCmd = new MySqlCommand(deleteVisitorQuery, conn);
    visitorCmd.Parameters.AddWithValue("@VisitorID", id);
    int rowsAffected = visitorCmd.ExecuteNonQuery();

    if (rowsAffected > 0)
    {
        return Ok(new { message = "Visitor data deleted successfully." });
    }

    return NotFound(new { message = "Visitor not found." });
}


        [HttpGet("info/{id}")]
        public IActionResult GetVisitorInfo(int id)
        {
            using var conn = DBHelper.GetConnection();
            conn.Open();

            string query = @"SELECT V.FullName, V.Purpose, V.CheckInTime, V.CheckOutTime, Z.ZoneName
                             FROM Visitors V
                             JOIN Zones Z ON V.ZoneID = Z.ZoneID
                             WHERE V.VisitorID = @VisitorID";

            var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@VisitorID", id);
            var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                var info = new
                {
                    FullName = reader["FullName"],
                    Purpose = reader["Purpose"],
                    CheckInTime = reader["CheckInTime"],
                    CheckOutTime = reader["CheckOutTime"],
                    Zone = reader["ZoneName"]
                };
                return Ok(info);
            }

            return NotFound(new { message = "Visitor not found." });
        }
        [HttpGet("all")]
        public IActionResult GetAllVisitors()
        {
            var visitors = new List<object>();
            using var conn = DBHelper.GetConnection();
            conn.Open();
            string query = "SELECT VisitorID, FullName, CheckInTime, CheckOutTime FROM Visitors";

            var cmd = new MySqlCommand(query, conn);
            var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
               visitors.Add(new
{
    VisitorID = Convert.ToInt32(reader["VisitorID"]),
    FullName = reader["FullName"].ToString(),
    CheckInTime = reader["CheckInTime"] == DBNull.Value ? null : reader["CheckInTime"],
    CheckOutTime = reader["CheckOutTime"] == DBNull.Value ? null : reader["CheckOutTime"]
});

            }
            return Ok(visitors);
        }

        [HttpGet("zone")]
        public IActionResult GetAllZone()
        {
            var zones = new List<object>();
            using var conn = DBHelper.GetConnection();
            conn.Open();

            var query = "SELECT ZoneID, ZoneName, IsRestricted FROM Zones";
            var cmd = new MySqlCommand(query, conn);
            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                zones.Add(new
                {
                    ZoneID = Convert.ToInt32(reader["ZoneID"]),
                    ZoneName = reader["ZoneName"].ToString(),
                    IsRestricted = Convert.ToBoolean(reader["IsRestricted"])
                });
            }

            return Ok(zones);
        }
[HttpDelete("zone/{id}")]
public IActionResult DeleteZone(int id)
{
    using var conn = DBHelper.GetConnection();
    conn.Open();

    // Optional: Check if any visitors are still assigned to this zone
    string checkVisitorsQuery = "SELECT COUNT(*) FROM Visitors WHERE ZoneID = @ZoneID";
    var checkCmd = new MySqlCommand(checkVisitorsQuery, conn);
    checkCmd.Parameters.AddWithValue("@ZoneID", id);
    int visitorCount = Convert.ToInt32(checkCmd.ExecuteScalar());

    if (visitorCount > 0)
    {
        return BadRequest(new { message = "Cannot delete zone. Visitors are still assigned to this zone." });
    }

    string deleteZoneQuery = "DELETE FROM Zones WHERE ZoneID = @ZoneID";
    var zoneCmd = new MySqlCommand(deleteZoneQuery, conn);
    zoneCmd.Parameters.AddWithValue("@ZoneID", id);
    int rowsAffected = zoneCmd.ExecuteNonQuery();

    if (rowsAffected > 0)
    {
        return Ok(new { message = "Zone deleted successfully." });
    }

    return NotFound(new { message = "Zone not found." });
}

[HttpPut("zone/restriction/{zoneId}")]
public IActionResult UpdateZoneRestriction(int zoneId, [FromBody] bool isRestricted)
{
    using var conn = DBHelper.GetConnection();
    conn.Open();
    var cmd = new MySqlCommand("UPDATE Zones SET IsRestricted = @IsRestricted WHERE ZoneID = @ZoneID", conn);
    cmd.Parameters.AddWithValue("@IsRestricted", isRestricted);
    cmd.Parameters.AddWithValue("@ZoneID", zoneId);
    cmd.ExecuteNonQuery();
    return Ok(new { message = "Zone restriction updated." });
}


    }
}
