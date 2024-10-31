namespace VaultShare.Models
{
    public class User
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string GoogleId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public User() { }

        public User(string id, string googleId, string email, string name)
        {
            Id = id;
            GoogleId = googleId;
            Email = email;
            Name = name;
        }
    }
}
