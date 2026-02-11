using Finance.Domain.Import;

namespace Finance.Application.Import;

public sealed class BankImportStrategyResolver : IBankImportStrategyResolver
{
    private readonly IEnumerable<IBankImportStrategy> _strategies;

    public BankImportStrategyResolver(IEnumerable<IBankImportStrategy> strategies)
    {
        _strategies = strategies;
    }

    public IBankImportStrategy Resolve(string sourceSystem, string fileName)
    {
        // Voorbeeld: kies op basis van sourceSystem, of fileName extensie
        // Implementeer hier logica per bank/bron
        return _strategies.FirstOrDefault(s => s.CanHandle(sourceSystem, fileName))
            ?? throw new InvalidOperationException($"No strategy found for {sourceSystem} / {fileName}");
    }
}
