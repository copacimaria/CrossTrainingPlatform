namespace CrossTraining.Application.DTOs;

/// <summary>Rezultatul unei operatii de autentificare / inregistrare.</summary>
public sealed class AuthResult
{
    public bool Succeeded { get; init; }
    public string? Error { get; init; }

    /// <summary>Utilizatorul autentificat. Null la inregistrare si la eroare.</summary>
    public AuthenticatedUserDto? User { get; init; }

    public int NewUserId { get; init; }

    /// <summary>Numele rolului utilizatorului (pentru claim-ul de rol din cookie).</summary>
    public string? RoleName { get; init; }

    /// <summary>Permisiunile active ale utilizatorului, derivate din rol.</summary>
    public IReadOnlyList<string> Permissions { get; init; } = [];

    public static AuthResult Success(
        AuthenticatedUserDto user,
        string? roleName,
        IReadOnlyList<string> permissions)
        => new() { Succeeded = true, User = user, RoleName = roleName, Permissions = permissions };

    public static AuthResult Registered(int newUserId) => new() { Succeeded = true, NewUserId = newUserId };

    public static AuthResult Fail(string error) => new() { Succeeded = false, Error = error };
}
