namespace FactoryVisitorApp.Models
{
    public class VisitorLoginDto
    {
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string? AuthorizedBy { get; set; }
    }
}
