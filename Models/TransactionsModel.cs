public class TransactionModel
{
    public string Description { get; set; }
    public decimal Amount { get; set; }
    public bool IsNegative { get; set; }
    public string ImageUrl { get; set; }
}