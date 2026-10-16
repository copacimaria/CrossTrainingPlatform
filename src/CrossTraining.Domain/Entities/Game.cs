using CrossTraining.Domain.Common;

namespace CrossTraining.Domain.Entities;

/// <summary>
/// Joc educational asociat unui domeniu.
///
/// Linkul de descarcare ajunge direct intr-un <c>href</c> in pagina de jocuri, deci se
/// accepta doar adrese http/https complete. Altfel, un link de forma <c>javascript:...</c>
/// ar rula cod in browserul oricui da click pe el.
/// </summary>
public class Game : ISoftDeletable
{
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 500;
    public const int DownloadLinkMaxLength = 500;

    /// <summary>Folosit de EF Core la materializare.</summary>
    private Game()
    {
    }

    public int GameId { get; private set; }
    public string GameName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTime? CreatedAt { get; private set; }

    /// <summary>Coloana permite NULL in baza de date, deci tipul o reflecta.</summary>
    public string? Description { get; private set; }
    public string? DownloadLink { get; private set; }

    public int? DomainId { get; private set; }
    public TrainingDomain? Domain { get; private set; }

    /// <summary>
    /// Un joc nou, activ. Existenta domeniului o verifica apelantul, care are acces la date.
    /// </summary>
    public static DomainResult<Game> Create(
        string name,
        string? description,
        string? downloadLink,
        int? domainId,
        DateTime createdAt)
    {
        var details = Validate(name, description, downloadLink);
        if (!details.Succeeded)
            return DomainResult.Fail<Game>(details.Error!);

        var (validName, validDescription, validLink) = details.Value;

        return DomainResult.Ok(new Game
        {
            GameName = validName,
            Description = validDescription,
            DownloadLink = validLink,
            DomainId = domainId,
            IsActive = true,
            CreatedAt = createdAt
        });
    }

    public DomainResult Update(string name, string? description, string? downloadLink, int? domainId)
    {
        var details = Validate(name, description, downloadLink);
        if (!details.Succeeded)
            return details;

        (GameName, Description, DownloadLink) = details.Value;
        DomainId = domainId;
        return DomainResult.Ok();
    }

    public void Deactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;

    private static DomainResult<(string Name, string? Description, string? Link)> Validate(
        string? name,
        string? description,
        string? downloadLink)
    {
        var validName = name?.Trim() ?? string.Empty;
        var validDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        var validLink = string.IsNullOrWhiteSpace(downloadLink) ? null : downloadLink.Trim();

        if (validName.Length == 0)
            return Fail("Numele jocului este obligatoriu.");

        if (validName.Length > NameMaxLength)
            return Fail($"Numele jocului poate avea cel mult {NameMaxLength} de caractere.");

        if (validDescription?.Length > DescriptionMaxLength)
            return Fail($"Descrierea poate avea cel mult {DescriptionMaxLength} de caractere.");

        if (validLink != null)
        {
            if (validLink.Length > DownloadLinkMaxLength)
                return Fail($"Linkul de descarcare poate avea cel mult {DownloadLinkMaxLength} de caractere.");

            if (!IsHttpLink(validLink))
                return Fail("Linkul de descarcare trebuie sa fie o adresa completa care incepe cu http:// sau https://.");
        }

        return DomainResult.Ok((validName, validDescription, validLink));

        static DomainResult<(string, string?, string?)> Fail(string error)
            => DomainResult.Fail<(string, string?, string?)>(error);
    }

    private static bool IsHttpLink(string link)
        => Uri.TryCreate(link, UriKind.Absolute, out var uri)
           && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
