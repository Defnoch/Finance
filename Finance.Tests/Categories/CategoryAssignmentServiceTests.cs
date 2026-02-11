using Finance.Application.Categories;
using Finance.Domain.Entities;
using Finance.Domain.Repositories;
using Finance.Infrastructure.Data;
using Finance.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Finance.Tests.Categories;

public class CategoryAssignmentServiceTests
{
    private static FinanceDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new FinanceDbContext(options);
    }

    [Test]
    public async Task AssignCategoryAsync_SetsCategoryId_OnTransaction()
    {
        await using var context = CreateInMemoryContext();

        ITransactionRepository transactionRepository = new TransactionRepository(context);
        ICategoryRepository categoryRepository = new CategoryRepository(context);

        // Arrange: seed één categorie en één transactie
        var category = new Category(Guid.NewGuid(), "TestCategorie", CategoryKind.Expense, "#FFFFFF", false);
        await categoryRepository.AddAsync(category, CancellationToken.None);

        var transaction = new Transaction
        {
            TransactionId = Guid.NewGuid(),
            SourceSystem = "TEST",
            SourceReference = "REF-1",
            BookingDate = new DateOnly(2025, 01, 01),
            Amount = 10m,
            Currency = "EUR",
            AccountIdentifier = "ACC",
            Description = "Test",
            ImportBatchId = Guid.NewGuid()
        };

        await transactionRepository.AddRangeAsync(new[] { transaction }, CancellationToken.None);

        var service = new CategoryAssignmentService(transactionRepository, categoryRepository);

        var command = new AssignCategoryCommand
        {
            TransactionId = transaction.TransactionId,
            CategoryId = category.CategoryId
        };

        // Act
        await service.AssignCategoryAsync(command, CancellationToken.None);

        // Assert
        var updated = await transactionRepository.GetByIdAsync(transaction.TransactionId, CancellationToken.None);
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.CategoryId, Is.EqualTo(category.CategoryId));
    }
}

