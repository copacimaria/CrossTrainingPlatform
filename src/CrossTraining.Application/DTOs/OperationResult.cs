namespace CrossTraining.Application.DTOs;

/// <summary>
/// Rezultatul unei comenzi care poate fi refuzata dintr-un motiv de business: parola
/// veche gresita, un nume deja folosit, un drept sau un rol de care depinde aplicatia.
///
/// Refuzurile acestea nu sunt exceptii: sunt raspunsuri asteptate, cu un mesaj pe care
/// ecranul il arata utilizatorului.
/// </summary>
public class OperationResult
{
    public bool Succeeded { get; init; }
    public string? ErrorField { get; init; }
    public string? Error { get; init; }

    public static OperationResult Success() => new() { Succeeded = true };
    public static OperationResult Fail(string error, string? field = null)
        => new() { Succeeded = false, Error = error, ErrorField = field };
}
