using Finance.Domain.Repositories;

namespace Finance.Application.Categories;

public sealed class CategoryAssignmentService : ICategoryAssignmentService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICategoryRepository _categoryRepository;

    public CategoryAssignmentService(
        ITransactionRepository transactionRepository,
        ICategoryRepository categoryRepository)
    {
        _transactionRepository = transactionRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task AssignCategoryAsync(AssignCategoryCommand command, CancellationToken cancellationToken = default)
    {
        // Controleer of de categorie bestaat
        var category = await _categoryRepository.GetByIdAsync(command.CategoryId, cancellationToken);
        if (category is null)
        {
            throw new InvalidOperationException($"Category '{command.CategoryId}' not found.");
        }

        // Haal de transactie op
        var transaction = await _transactionRepository.GetByIdAsync(command.TransactionId, cancellationToken);
        if (transaction is null)
        {
            throw new InvalidOperationException($"Transaction '{command.TransactionId}' not found.");
        }

        // Toewijzing (recategoriseren is toegestaan in deze MVP)
        transaction.CategoryId = command.CategoryId;

        await _transactionRepository.UpdateAsync(transaction, cancellationToken);
    }

    public async Task UnassignCategoryAsync(UnassignCategoryCommand command, CancellationToken cancellationToken = default)
    {
        // Haal de transactie op
        var transaction = await _transactionRepository.GetByIdAsync(command.TransactionId, cancellationToken);
        if (transaction is null)
        {
            throw new InvalidOperationException($"Transaction '{command.TransactionId}' not found.");
        }
        // Verwijder de categorie
        transaction.CategoryId = null;
        await _transactionRepository.UpdateAsync(transaction, cancellationToken);
    }
}
