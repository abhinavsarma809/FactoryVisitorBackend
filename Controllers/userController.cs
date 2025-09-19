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
        public IActionResult Login([FromBody]User user)
        {
            string hashedPassword = HashPassword(user.PasswordHash);

            using (var conn = DBHelper.GetConnection())
            {
                conn.Open();
                string query = "SELECT Role FROM Users WHERE Email=@Email AND PasswordHash=@PasswordHash";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Email", user.Email);
                cmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);

                var role = cmd.ExecuteScalar();
                if (role != null)
                {
                    return Ok(new { message = "Login successful", role = role.ToString() });
                }
                else
                {
                    return Unauthorized(new { message = "Invalid credentials" });
                }
            }
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
