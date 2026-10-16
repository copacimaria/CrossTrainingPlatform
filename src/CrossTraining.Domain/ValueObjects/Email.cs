using CrossTraining.Domain.Common;

namespace CrossTraining.Domain.ValueObjects;

/// <summary>
/// Adresa de email a unui cont.
///
/// Se valideaza si se normalizeaza o singura data, la creare: fara spatii la capete si
/// cu litere mici. Oriunde apare un <see cref="Email"/>, valoarea este deja corecta, deci
/// nu mai trebuie verificata in fiecare caz de utilizare. Inainte, emailul era un
/// <c>string</c> oarecare, iar " Ana@Exemplu.ro" si "ana@exemplu.ro" erau doua valori diferite.
/// </summary>
public sealed record Email
{
    public const int MaxLength = 256;

    private Email(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static DomainResult<Email> Create(string? input)
    {
        // Emailurile se compara fara diferenta intre litere; forma canonica este cu litere mici.
        var value = input?.Trim().ToLowerInvariant() ?? string.Empty;

        if (value.Length == 0)
            return DomainResult.Fail<Email>("Emailul este obligatoriu.");

        if (value.Length > MaxLength)
            return DomainResult.Fail<Email>($"Emailul poate avea cel mult {MaxLength} de caractere.");

        if (!IsWellFormed(value))
            return DomainResult.Fail<Email>("Emailul nu are un format valid.");

        return DomainResult.Ok(new Email(value));
    }

    /// <summary>
    /// O valoare citita din baza de date, deja validata la salvare. Nu se valideaza din nou:
    /// o regula de format inasprita in timp nu trebuie sa faca imposibila citirea unui cont vechi.
    /// </summary>
    public static Email FromStored(string value) => new(value);

    public override string ToString() => Value;

    private static bool IsWellFormed(string value)
    {
        if (value.Any(char.IsWhiteSpace))
            return false;

        var at = value.IndexOf('@', StringComparison.Ordinal);
        if (at <= 0 || at != value.LastIndexOf('@') || at == value.Length - 1)
            return false;

        var host = value[(at + 1)..];
        return host.Contains('.', StringComparison.Ordinal)
            && !host.StartsWith('.')
            && !host.EndsWith('.');
    }
}
