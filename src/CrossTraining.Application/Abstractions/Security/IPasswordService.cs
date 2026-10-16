using CrossTraining.Domain.Entities;

namespace CrossTraining.Application.Abstractions.Security;

/// <summary>
/// Abstractie pentru hashing-ul si verificarea parolelor.
/// Implementarea concreta traieste in stratul Infrastructure.
/// </summary>
public interface IPasswordService
{
    string Hash(AppUser user, string plainPassword);
    bool Verify(AppUser user, string storedHash, string plainPassword);
}
