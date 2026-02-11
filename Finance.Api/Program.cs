using Finance.Api.HealthChecks;
using Finance.Application.BackgroundTasks;
using Finance.Domain.Import;
using Finance.Domain.Repositories;
using Finance.Infrastructure.Data;
using Finance.Infrastructure.Import;
using Finance.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Register health checks (readiness/liveness endpoints provided via controller)
builder.Services.AddHealthChecks()
    .AddCheck<DbHealthCheck>("database");

// Register the DbHealthCheck so it can be resolved
builder.Services.AddScoped<DbHealthCheck>();

// Database (SQLite of andere provider) - voorlopig SQLite file in root
builder.Services.AddDbContext<FinanceDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("FinanceDatabase")
                         ?? "Data Source=finance.db";
    options.UseSqlite(connectionString);
});

// Repositories
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IImportBatchRepository, ImportBatchRepository>();
builder.Services.AddScoped<ICategorizationRuleRepository, CategorizationRuleRepository>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<ITransactionLinkRepository, TransactionLinkRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IBackgroundTaskConfigRepository, BackgroundTaskConfigRepository>();
builder.Services.AddScoped<IAccountFiscalYearRepository, AccountFiscalYearRepository>();

// Application services
builder.Services.AddScoped<Finance.Application.Import.IImportService, Finance.Application.Import.ImportService>();
builder.Services.AddScoped<Finance.Application.Import.ITransactionLinkerService, Finance.Application.Import.TransactionLinkerService>();

builder.Services.AddScoped<Finance.Application.Categories.ICategoryAssignmentService, Finance.Application.Categories.CategoryAssignmentService>();
builder.Services.AddScoped<Finance.Application.Categories.ICategoryQueryService, Finance.Application.Categories.CategoryQueryService>();
builder.Services.AddScoped<Finance.Application.Categories.ICategoryCommandService, Finance.Application.Categories.CategoryCommandService>();

builder.Services.AddScoped<Finance.Application.Reports.ITransactionReportService, Finance.Application.Reports.TransactionReportService>();
builder.Services.AddScoped<Finance.Application.Reports.IMonthlySummaryService, Finance.Application.Reports.MonthlySummaryService>();
builder.Services.AddScoped<Finance.Application.Reports.ICategorySummaryService, Finance.Application.Reports.CategorySummaryService>();

// Bank import strategy (ING)
builder.Services.AddScoped<IBankImportStrategy, IngCsvImportStrategy>();
builder.Services.AddScoped<IBankImportStrategy, IngSpaarCsvImportStrategy>();
builder.Services.AddScoped<IBankImportStrategy, AsnCsvImportStrategy>();

// Strategy resolver
builder.Services.AddScoped<Finance.Application.Import.IBankImportStrategyResolver, Finance.Application.Import.BankImportStrategyResolver>();

// CORS voor frontend (development)
// Lees de frontend origin uit de configuratie (appsettings)
var allowedOrigin = builder.Configuration["Cors:AllowedOrigin"];
if (string.IsNullOrWhiteSpace(allowedOrigin))
    throw new InvalidOperationException("Cors:AllowedOrigin moet gezet zijn in de configuratie (appsettings.json of appsettings.Development.json)");

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigin)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
    );
});

// Swagger UI (Swashbuckle)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Background tasks
builder.Services.AddScoped<IBackgroundTaskService, BackgroundTaskService>();

var app = builder.Build();

app.UseRouting();
app.UseCors(); // Zet CORS direct na UseRouting

// Zorg dat de database en tabellen bestaan (MVP)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
