namespace VaultShare.Models
{
    public class CardDetails
    {
        public string CardNumber { get; set; } = string.Empty;
        public string Expiry { get; set; } = string.Empty;
        public string Cvc { get; set; } = string.Empty;
        public string CardNickname { get; set; } = "Default Card";
    }

    public class Vault
    {
        public string VaultId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<string> Members { get; set; } = new List<string>();
    }

    public class User
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string GoogleId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        // Additional fields for profile
        public string? Bio { get; set; } = string.Empty;
        public bool NotificationsEnabled { get; set; } = true;
        public List<CardDetails> Cards { get; set; } = new List<CardDetails>();
        public List<string> FriendIds { get; set; } = new List<string>();
        public List<Vault> Vaults { get; set; } = new List<Vault>();

        // Payment Method and Password
        public CardDetails? PaymentMethod { get; set; }
        public string Password { get; set; } = string.Empty;
    }
}
