using Microsoft.EntityFrameworkCore;
using OrderHub.Catalog.Application.Abstractions;
using OrderHub.Catalog.Domain.Aggregates;

namespace OrderHub.Catalog.Infrastructure.Data.Repositories;

internal sealed class ProductRepository : IProductRepository
{
    private readonly CatalogDbContext _context;

    public ProductRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _context.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<Product?> GetBySkuAsync(string sku, CancellationToken ct)
    {
        return await _context.Products.FirstOrDefaultAsync(p => p.Sku == sku, ct);
    }

    public async Task<List<Product>> ListAsync(bool includeInactive, CancellationToken ct)
    {
        var query = _context.Products.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        return await query.ToListAsync(ct);
    }

    public async Task AddAsync(Product product, CancellationToken ct)
    {
        await _context.Products.AddAsync(product, ct);
    }

    public async Task UpdateAsync(Product product, CancellationToken ct)
    {
        _context.Products.Update(product);
        await Task.CompletedTask;
    }
}
