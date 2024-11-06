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
        public string Username { get; set; } = string.Empty;

        // Additional fields for profile
        public string? Bio { get; set; } = string.Empty;
        public bool NotificationsEnabled { get; set; } = true;
        public List<string> Notifications { get; set; } = new List<string>();
        public List<CardDetails> Cards { get; set; } = new List<CardDetails>();
        public List<string> FriendIds { get; set; } = new List<string>();
        public List<Vault> Vaults { get; set; } = new List<Vault>();

        // Payment Method and Password
        public CardDetails? PaymentMethod { get; set; }
        public string Password { get; set; } = string.Empty;
        public decimal Balance { get; set; } = 0;
        public string? Photo { get; set; }
    }

    public class Notification
    {
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public string Type { get; set; }
    }

    public class DashboardViewModel
    {
        public User User { get; set; }
        public List<User> FriendsInVaults { get; set; }
        public decimal Balance { get; set; }
        public List<string> Notifications { get; set; }
        public List<Transaction> Transactions { get; set; }
    }

    public class Transaction
    {
        public string TransactionId { get; set; } = Guid.NewGuid().ToString(); // Unique ID for the transaction
        public string UserId { get; set; } = string.Empty; // User ID associated with the transaction
        public decimal Amount { get; set; } // Amount involved in the transaction
        public DateTime Date { get; set; } = DateTime.UtcNow; // Date and time when the transaction occurred
        public string Description { get; set; } = string.Empty; // Description of the transaction (e.g., payment, deposit, etc.)
        public TransactionType Type { get; set; } // Type of transaction (e.g., Deposit, Withdrawal, etc.)
    }

    // Enum to define the type of transaction
    public enum TransactionType
    {
        Approved,
        Denied
    }


}
