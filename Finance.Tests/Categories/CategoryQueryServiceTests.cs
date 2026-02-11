using Finance.Application.Categories;
using Finance.Domain.Repositories;
using Finance.Infrastructure.Data;
using Finance.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Finance.Tests.Categories;

public class CategoryQueryServiceTests
{
    private static FinanceDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new FinanceDbContext(options);
    }

    [Test]
    public async Task GetCategoriesAsync_SeedsDefaultCategories_WhenNoneExist()
    {
        await using var context = CreateInMemoryContext();

        ICategoryRepository categoryRepository = new CategoryRepository(context);
        var service = new CategoryQueryService(categoryRepository);

        var result = await service.GetCategoriesAsync(CancellationToken.None);

        // Verwacht dat de seed-categorieën zijn aangemaakt
        Assert.That(result.Count, Is.EqualTo(5));
        var names = result.Select(c => c.Name).OrderBy(n => n).ToArray();
        Assert.That(names, Is.EquivalentTo(new[] { "Abonnementen", "Boodschappen", "Inkomen", "Overig", "Wonen" }));
    }

    [Test]
    public async Task GetCategoriesAsync_DoesNotSeedTwice_OnSubsequentCalls()
    {
        await using var context = CreateInMemoryContext();

        ICategoryRepository categoryRepository = new CategoryRepository(context);
        var service = new CategoryQueryService(categoryRepository);

        var firstCall = await service.GetCategoriesAsync(CancellationToken.None);
        var secondCall = await service.GetCategoriesAsync(CancellationToken.None);

        Assert.That(firstCall.Count, Is.EqualTo(5));
        Assert.That(secondCall.Count, Is.EqualTo(5));

        // Controleer dat er in totaal nog steeds 5 categorieën in de database zijn
        var allCategories = await categoryRepository.GetAllAsync(CancellationToken.None);
        Assert.That(allCategories.Count, Is.EqualTo(5));
    }
}
