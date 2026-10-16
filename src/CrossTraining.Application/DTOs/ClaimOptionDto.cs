namespace CrossTraining.Application.DTOs;

/// <summary>
/// Un claim oferit ca optiune de selectie in UI (autocomplete la adaugarea intr-un rol).
/// Contine strict ce afiseaza si ce trimite inapoi clientul.
/// </summary>
public sealed record ClaimOptionDto(int ClaimId, string ClaimName);
