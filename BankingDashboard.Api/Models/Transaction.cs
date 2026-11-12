namespace BankingDashboard.Api.Models;

public class Transaction
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = "Completed";
    
    public Account? Account { get; set; }
}