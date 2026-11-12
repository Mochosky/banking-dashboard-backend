using BankingDashboard.Api.Models;

namespace BankingDashboard.Api.Data;

public static class DbInitializer
{
    public static void Initialize(BankingDbContext context)
    {
        // Check if the database has already been seeded
        if (context.Accounts.Any())
        {
            return;
        }

        var accounts = new Account[]
        {
            new Account { AccountName = "Mercury Business Checking", AccountType = "Checking", Balance = 124500.50m, Currency = "USD", LastUpdated = DateTime.UtcNow },
            new Account { AccountName = "Slash Vault Savings", AccountType = "Savings", Balance = 55000.00m, Currency = "USD", LastUpdated = DateTime.UtcNow }
        };

        context.Accounts.AddRange(accounts);
        context.SaveChanges();

        var transactions = new List<Transaction>();
        var random = new Random();
        var checkingId = accounts[0].Id;
        var categories = new[] { "Software", "Payroll", "Travel", "Office Supplies", "Marketing" };
        var vendors = new[] { "AWS", "Gusto", "Delta Airlines", "Apple", "Google Ads", "Stripe", "WeWork" };

        // Generate 50 realistic transactions for the last 30 days
        for (int i = 0; i < 50; i++)
        {
            var isCredit = random.NextDouble() > 0.7; // 30% chance of being income
            var amount = isCredit
                ? (decimal)(random.NextDouble() * 5000 + 500) // Income between 500 and 5500
                : -(decimal)(random.NextDouble() * 1500 + 10); // Expense between -10 and -1510

            transactions.Add(new Transaction
            {
                AccountId = checkingId,
                Date = DateTime.UtcNow.AddDays(-random.Next(0, 30)).AddHours(-random.Next(1, 24)),
                Description = isCredit ? "Stripe Payout" : vendors[random.Next(vendors.Length)],
                Amount = Math.Round(amount, 2),
                Category = isCredit ? "Income" : categories[random.Next(categories.Length)],
                Status = "Completed"
            });
        }

        context.Transactions.AddRange(transactions.OrderByDescending(t => t.Date));
        context.SaveChanges();
    }
}