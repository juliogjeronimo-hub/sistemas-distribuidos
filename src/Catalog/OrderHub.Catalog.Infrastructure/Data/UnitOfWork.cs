using OrderHub.Catalog.Application.Abstractions;

namespace OrderHub.Catalog.Infrastructure.Data;

internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly CatalogDbContext _context;

    public UnitOfWork(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task CommitAsync(CancellationToken ct)
    {
        await _context.SaveChangesAsync(ct);
    }
}
