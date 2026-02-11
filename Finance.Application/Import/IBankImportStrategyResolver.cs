using Finance.Domain.Import;

namespace Finance.Application.Import;

public interface IBankImportStrategyResolver
{
    IBankImportStrategy Resolve(string sourceSystem, string fileName);
}
