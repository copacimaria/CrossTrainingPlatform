using CrossTraining.Application.DTOs;

namespace CrossTraining.Web.Models;

/// <summary>Pasul de alegere a domeniilor din fluxul de inregistrare.</summary>
public class SelectDomainViewModel
{
    /// <summary>Token-ul semnat emis la register (vezi ADR-0004).</summary>
    public string? Token { get; set; }

    public IReadOnlyList<DomainDto> Domains { get; set; } = [];
}

/// <summary>Schimbarea domeniilor din profilul unui utilizator autentificat.</summary>
public class ChangeDomainViewModel
{
    public List<int> SelectedDomainIds { get; set; } = [];

    public IReadOnlyList<DomainDto> AvailableDomains { get; set; } = [];
}
