using BankingDashboard.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// (Leave existing Swagger/OpenAPI services here)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var renderDbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrEmpty(renderDbUrl))
{
    var uri = new Uri(renderDbUrl);
    var userInfo = uri.UserInfo.Split(':');
    connectionString = $"Host={uri.Host};Database={uri.LocalPath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};Port={uri.Port};Ssl Mode=Require;Trust Server Certificate=true;";
}

builder.Services.AddDbContext<BankingDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add DbContext.
builder.Services.AddDbContext<BankingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add CORS policy to allow requests from Angular frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    options.AddPolicy("AllowVercel", policy =>
    {
        policy.WithOrigins("https://banking-dashboard-frontend-three.vercel.app")
                        .AllowAnyMethod()
                        .AllowAnyHeader();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BankingDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Use the CORS policy defined above
app.UseCors("AllowAngular");
app.UseCors("AllowVercel");

app.UseHttpsRedirection();

// Account summary endpoint
app.MapGet("/api/accounts/summary", async (BankingDbContext db) =>
{
    var accounts = await db.Accounts.ToListAsync();
    var totalBalance = accounts.Sum(a => a.Balance);
    return Results.Ok(new
    {
        Accounts = accounts,
        TotalBalance = totalBalance
    });
})
.WithName("GetAccountSummary");

// Transactions endoint with pagination, search, and category filtering
app.MapGet("/api/transactions", async (BankingDbContext db, int page = 1, int pageSize = 15, string? search = null, string? category = null) =>
{
    var query = db.Transactions.AsQueryable();

    if (!string.IsNullOrWhiteSpace(search))
    {
        var searchTerm = search.ToLower();
        query = query.Where(t => t.Description.ToLower().Contains(searchTerm));
    }

    if (!string.IsNullOrWhiteSpace(category) && category != "All")
    {
        query = query.Where(t => t.Category == category);
    }

    var totalItems = await query.CountAsync();
    var transactions = await query
        .OrderByDescending(t => t.Date)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return Results.Ok(new
    {
        TotalItems = totalItems,
        Page = page,
        PageSize = pageSize,
        TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
        Data = transactions
    });
})
.WithName("GetTransactions");

// Analytics endpoint for cash flow over the last 30 days
app.MapGet("/api/analytics/cashflow", async (BankingDbContext db) =>
{
    var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

    var transactions = await db.Transactions
        .Where(t => t.Date >= thirtyDaysAgo)
        .ToListAsync();

    // Grouping done in memory to avoid complex SQL translation issues
    var dailyFlow = transactions
        .GroupBy(t => t.Date.Date)
        .Select(g => new
        {
            Date = g.Key.ToString("yyyy-MM-dd"),
            Income = g.Where(t => t.Amount > 0).Sum(t => t.Amount),
            Expenses = Math.Abs(g.Where(t => t.Amount < 0).Sum(t => t.Amount))
        })
        .OrderBy(d => d.Date)
        .ToList();

    return Results.Ok(dailyFlow);
})
.WithName("GetCashflowAnalytics");

// Seed the database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<BankingDbContext>();
        DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating the DB.");
    }
}

app.Run();