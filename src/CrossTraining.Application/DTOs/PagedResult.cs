namespace CrossTraining.Application.DTOs;

/// <summary>
/// O pagina de rezultate, impreuna cu numarul total de randuri care corespund filtrului.
///
/// Contine doar randurile paginii: numararea si decuparea se fac in baza de date, nu
/// prin incarcarea intregului tabel in memorie.
/// </summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>Aceeasi pagina, cu elementele convertite in alt tip.</summary>
    public PagedResult<TOut> Map<TOut>(Func<T, TOut> selector)
        => new(Items.Select(selector).ToList(), Page, PageSize, TotalCount);
}
