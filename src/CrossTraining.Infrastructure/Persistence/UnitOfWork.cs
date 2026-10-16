using CrossTraining.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace CrossTraining.Infrastructure.Persistence;

/// <inheritdoc />
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;

    public UnitOfWork(AppDbContext db)
    {
        _db = db;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _db.SaveChangesAsync(cancellationToken);

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => new EfTransaction(await _db.Database.BeginTransactionAsync(cancellationToken));

    private sealed class EfTransaction : IUnitOfWorkTransaction
    {
        private readonly IDbContextTransaction _transaction;

        public EfTransaction(IDbContextTransaction transaction)
        {
            _transaction = transaction;
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
            => _transaction.CommitAsync(cancellationToken);

        // Fara Commit explicit, Dispose anuleaza tranzactia. Asa, o exceptie pe traseu
        // nu lasa in urma modificari partiale.
        public ValueTask DisposeAsync() => _transaction.DisposeAsync();
    }
}
