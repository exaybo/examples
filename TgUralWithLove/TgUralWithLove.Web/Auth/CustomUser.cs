namespace TgUralWithLove.Web.Auth
{
    public class CustomUser
    {
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsAuthenticated { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public long? Id { get; set; }
        public string? PhotoUrl { get; set; }
    }

}