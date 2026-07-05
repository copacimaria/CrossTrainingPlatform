using CrossTraining.Domain.Entities;

namespace CrossTraining.Application.DTOs;

/// <summary>Rezultatul unei operatii de autentificare / inregistrare.</summary>
public class AuthResult
{
    public bool Succeeded { get; init; }
    public string? Error { get; init; }
    public AppUser? User { get; init; }
    public int NewUserId { get; init; }

    /// <summary>Numele rolului utilizatorului (pentru claim-ul de rol din cookie).</summary>
    public string? RoleName { get; init; }

    /// <summary>Permisiunile (claim-urile) utilizatorului, derivate din rol.</summary>
    public List<string> Permissions { get; init; } = new();

    public static AuthResult Success(AppUser user, string? roleName, List<string> permissions)
        => new() { Succeeded = true, User = user, RoleName = roleName, Permissions = permissions };

    public static AuthResult Registered(int newUserId) => new() { Succeeded = true, NewUserId = newUserId };
    public static AuthResult Fail(string error) => new() { Succeeded = false, Error = error };
}
