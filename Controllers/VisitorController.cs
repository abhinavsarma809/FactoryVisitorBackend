using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using FactoryVisitorApp.Data;
using FactoryVisitorApp.Models;
using System.Security.Cryptography;
using System.Text;

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
        public IActionResult Login([FromBody] Visitor visitor)
        {
            string hashedPassword = HashPassword(visitor.PasswordHash);

            using var conn = DBHelper.GetConnection();
            conn.Open();

            string query = "SELECT VisitorID FROM Visitors WHERE Email=@Email AND PasswordHash=@PasswordHash";
            var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Email", visitor.Email);
            cmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);

            var result = cmd.ExecuteScalar();
            if (result != null)
                return Ok(new { message = "Login successful", visitorId = result });
            else
                return Unauthorized(new { message = "Invalid credentials" });
        }

[HttpPost("checkin/{id}")]
public IActionResult CheckIn(int id)
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

       
        var checkInTime = DateTime.Now;
        string updateQuery = "UPDATE Visitors SET CheckInTime = @CheckInTime WHERE VisitorID = @VisitorID";
        var updateCmd = new MySqlCommand(updateQuery, conn);
        updateCmd.Parameters.AddWithValue("@CheckInTime", checkInTime);
        updateCmd.Parameters.AddWithValue("@VisitorID", id);
        updateCmd.ExecuteNonQuery();

        string logQuery = @"INSERT INTO VisitorLogs (VisitorID, ZoneID, ActionType, Timestamp)
                            VALUES (@VisitorID, (SELECT ZoneID FROM Visitors WHERE VisitorID = @VisitorID), 'CheckIn', @Timestamp)";
        var logCmd = new MySqlCommand(logQuery, conn);
        logCmd.Parameters.AddWithValue("@VisitorID", id);
        logCmd.Parameters.AddWithValue("@Timestamp", checkInTime);
        logCmd.ExecuteNonQuery();

        var response = new
        {
            message = isRestricted ? "ALERT: Visitor entered a restricted zone!" : "Check-in recorded.",
            zone = zoneName,
            purpose = purpose,
            checkInTime = checkInTime
        };

        return Ok(response);
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
    }
}
