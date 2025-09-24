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
    public class UserController : ControllerBase
    {
        [HttpPost("register")]
        public IActionResult Register([FromBody]User user)
        {
            string hashedPassword = HashPassword(user.PasswordHash);

            using (var conn = DBHelper.GetConnection())
            {
                conn.Open();
                string query = "INSERT INTO Users (FullName, Email, PasswordHash, Role) VALUES (@FullName, @Email, @PasswordHash, @Role)";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@FullName", user.FullName);
                cmd.Parameters.AddWithValue("@Email", user.Email);
                cmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                cmd.Parameters.AddWithValue("@Role", user.Role);
                cmd.ExecuteNonQuery();
            }

            return Ok(new { message = "User registered successfully." });
        }

       [HttpPost("login")]
public IActionResult Login([FromBody] User user)
{
    string hashedPassword = HashPassword(user.PasswordHash);

    using (var conn = DBHelper.GetConnection())
    {
        conn.Open();
        string query = "SELECT UserID, Role FROM Users WHERE Email=@Email AND PasswordHash=@PasswordHash";
        MySqlCommand cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@Email", user.Email);
        cmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            int userId = reader.GetInt32("UserID");
            string role = reader.GetString("Role");

            return Ok(new
            {
                message = "Login successful",
                role = role,
                UserID = userId 
            });
        }
        else
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }
    }
}

       [HttpPut("{id}")]
public IActionResult UpdateUser(int id, [FromBody] User updatedUser)
{
    using var conn = DBHelper.GetConnection();
    conn.Open();

    string hashedPassword = HashPassword(updatedUser.PasswordHash);

    string query = "UPDATE users SET Email = @Email, PasswordHash = @PasswordHash WHERE UserID = @UserID";

    using var cmd = new MySqlCommand(query, conn);
    cmd.Parameters.AddWithValue("@Email", updatedUser.Email);
    cmd.Parameters.AddWithValue("@PasswordHash", hashedPassword); 
    cmd.Parameters.AddWithValue("@UserID", id);

    int rowsAffected = cmd.ExecuteNonQuery();

    if (rowsAffected > 0)
    {
        return Ok(new { message = "User updated successfully" });
    }
    else
    {
        return NotFound(new { message = "User not found or no changes made" });
    }
}

[HttpGet("admins")]
    public IActionResult GetAdmins()
    {
        using var conn = DBHelper.GetConnection();
        conn.Open();

        string query = @"SELECT FullName, Email FROM Users WHERE Role = 'Admin'";
        var cmd = new MySqlCommand(query, conn);
        var reader = cmd.ExecuteReader();

        var admins = new List<object>();
        while (reader.Read())
        {
            admins.Add(new
            {
                name = reader["FullName"].ToString(),
                email = reader["Email"].ToString()
            });
        }

        return Ok(admins);
    }



        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
        }
    }
}
