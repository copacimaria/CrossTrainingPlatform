namespace CrossTraining.Application.Abstractions.Persistence;

/// <summary>
/// Granita de tranzactie a unui caz de utilizare.
///
/// Repository-urile doar pregatesc modificarile; salvarea se face prin
/// <see cref="SaveChangesAsync"/>. Tranzactia explicita este deschisa de pipeline pentru
/// fiecare comanda, deci un handler care salveaza de mai multe ori ramane atomic.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

/// <summary>Tranzactia deschisa. Daca nu se face Commit, la Dispose se anuleaza.</summary>
public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
