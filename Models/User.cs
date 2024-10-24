namespace VaultShare.Models
{
    public class User
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Email { get; set; }
        public string Name { get; set; }
        public string GoogleId { get; set; }

        public User(string id, string email, string name, string googleId)
        {
            Id = id;
            Email = email;
            Name = name;
            GoogleId = googleId;
        }
    }

}
