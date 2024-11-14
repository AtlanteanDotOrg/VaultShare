namespace VaultShare.Models
{
    public class User
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string GoogleId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        // Profile-related properties
        public string? Bio { get; set; } = string.Empty;
        public bool NotificationsEnabled { get; set; } = true;

        // Bank account information directly in User
        public string AccountNumber { get; set; } = string.Empty;
        public string RoutingNumber { get; set; } = string.Empty;
        public string MerchantID { get; set; } = string.Empty;

        // Card details directly in User
        public string CardNumber { get; set; } = string.Empty;
        public string CardExpiry { get; set; } = string.Empty;
        public string CardCvc { get; set; } = string.Empty;
        public string CardNickname { get; set; } = "Default Card";
        

        public List<string> FriendIds { get; set; } = new List<string>();
        public List<Vault> Vaults { get; set; } = new List<Vault>();

        // Password for account (ensure it's securely stored in a real app)
        public string Password { get; set; } = string.Empty;
    }

    public class Vault
    {
        public string VaultId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<VaultMember> Members { get; set; } = new List<VaultMember>();

        // Full card details
        public string CardId { get; set; } = string.Empty;
        public string CardNumber { get; set; } = string.Empty;
        public string CardExpiry { get; set; } = string.Empty;
        public string CardCvc { get; set; } = string.Empty;
        public string CardNickname { get; set; } = "Vault Card";
    }

    public class VaultMember
    {
        public string UserId { get; set; } = string.Empty;
        public string Role { get; set; } = "member"; // Default role is "member"
    }
}
