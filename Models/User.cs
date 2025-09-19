namespace FactoryVisitorApp.Models
{
    public class User
    {
        public int UserID { get; set; }

        public string? FullName { get; set; }

        
        public required string Email { get; set; }

     
        public required string PasswordHash { get; set; }

        
        public string? Role { get; set; }
    }
}
