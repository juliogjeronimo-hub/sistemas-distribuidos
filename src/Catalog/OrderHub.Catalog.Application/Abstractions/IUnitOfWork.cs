namespace OrderHub.Catalog.Application.Abstractions;

public interface IUnitOfWork
{
    Task CommitAsync(CancellationToken ct);
}
