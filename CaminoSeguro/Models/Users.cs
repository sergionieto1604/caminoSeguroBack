namespace CaminoSeguro.Models
{
    public class Users
    {
        public int Id { get; set; }
        public string Alias { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

    }
}
