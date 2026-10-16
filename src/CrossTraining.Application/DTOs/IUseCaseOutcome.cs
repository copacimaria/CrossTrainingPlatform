namespace CrossTraining.Application.DTOs;

/// <summary>
/// Rezultatul unui caz de utilizare care poate fi refuzat dintr-un motiv de business.
///
/// Il citeste LoggingBehavior, ca un refuz (login gresit, nume deja folosit) sa apara in
/// log ca refuz, nu ca reusita.
/// </summary>
public interface IUseCaseOutcome
{
    bool Succeeded { get; }

    /// <summary>Motivul refuzului, pe intelesul utilizatorului. Nu contine date sensibile.</summary>
    string? RefusalReason { get; }
}
