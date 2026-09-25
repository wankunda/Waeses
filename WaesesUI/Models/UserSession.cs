namespace WaesesUI
{
    public class UserSession
    {
        public string Email { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string? FullName { get; set; }
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpireIn { get; set; }
        public List<string> Roles { get; set; } = new();
        public bool IsAuthenticated => !string.IsNullOrEmpty(Email);
        public bool IsInRole(string role)
        {
            return Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
        }
    }
}
