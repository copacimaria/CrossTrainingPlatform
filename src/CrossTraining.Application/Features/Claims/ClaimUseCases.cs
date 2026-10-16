using CrossTraining.Application.Abstractions.Messaging;
using CrossTraining.Application.Abstractions.Persistence;
using CrossTraining.Application.DTOs;
using CrossTraining.Domain.Entities;

namespace CrossTraining.Application.Features.Claims;

// Cazurile de utilizare pentru drepturi. Regulile despre un drept (nume valid, drepturi de
// sistem protejate) le aplica entitatea AppClaim; aici raman doar regulile care cer acces
// la date - unicitatea numelui - si orchestrarea: incarca, cere entitatii, salveaza.

// ------------------- CITIRI -------------------

/// <summary>Lista paginata de drepturi pentru administrare, inclusiv cele dezactivate.</summary>
public sealed record SearchClaimsQuery(string? SearchTerm, int Page, int PageSize)
    : IQuery<PagedResult<ClaimDto>>;

public sealed class SearchClaimsHandler : IQueryHandler<SearchClaimsQuery, PagedResult<ClaimDto>>
{
    private readonly IAppClaimRepository _claims;

    public SearchClaimsHandler(IAppClaimRepository claims) => _claims = claims;

    public async Task<PagedResult<ClaimDto>> Handle(SearchClaimsQuery query, CancellationToken cancellationToken)
    {
        var result = await _claims.SearchForAdminAsync(query.SearchTerm, Math.Max(query.Page, 1), query.PageSize);
        return result.Map(c => new ClaimDto(c.ClaimId, c.ClaimName, c.IsActive));
    }
}

/// <summary>Dreptul impreuna cu rolurile care il au. Null daca nu exista.</summary>
public sealed record GetClaimQuery(int ClaimId) : IQuery<ClaimWithRolesDto?>;

public sealed class GetClaimHandler : IQueryHandler<GetClaimQuery, ClaimWithRolesDto?>
{
    private readonly IAppClaimRepository _claims;

    public GetClaimHandler(IAppClaimRepository claims) => _claims = claims;

    public async Task<ClaimWithRolesDto?> Handle(GetClaimQuery query, CancellationToken cancellationToken)
    {
        var claim = await _claims.GetForAdminAsync(query.ClaimId);
        if (claim == null)
            return null;

        var roleNames = claim.RoleClaims
            .Select(rc => rc.Role?.RoleName)
            .Where(name => !string.IsNullOrEmpty(name))
            .Select(name => name!)
            .OrderBy(name => name, StringComparer.CurrentCulture)
            .ToList();

        return new ClaimWithRolesDto(claim.ClaimId, claim.ClaimName, roleNames);
    }
}

/// <summary>
/// Drepturile care mai pot fi adaugate unui rol, filtrate dupa un termen de cautare.
/// Rezultatul este limitat, ca sa nu se intoarca tot tabelul catre browser.
/// </summary>
public sealed record SearchAvailableClaimsQuery(int RoleId, string? SearchTerm)
    : IQuery<List<ClaimOptionDto>>;

public sealed class SearchAvailableClaimsHandler
    : IQueryHandler<SearchAvailableClaimsQuery, List<ClaimOptionDto>>
{
    private const int ResultLimit = 20;

    private readonly IAppClaimRepository _claims;

    public SearchAvailableClaimsHandler(IAppClaimRepository claims) => _claims = claims;

    public async Task<List<ClaimOptionDto>> Handle(
        SearchAvailableClaimsQuery query,
        CancellationToken cancellationToken)
    {
        var claims = await _claims.SearchAvailableForRoleAsync(query.RoleId, query.SearchTerm, ResultLimit);
        return claims.Select(c => new ClaimOptionDto(c.ClaimId, c.ClaimName)).ToList();
    }
}

// ------------------- COMENZI -------------------

public sealed record CreateClaimCommand(string ClaimName) : ICommand<OperationResult>;

public sealed class CreateClaimHandler : ICommandHandler<CreateClaimCommand, OperationResult>
{
    private readonly IAppClaimRepository _claims;
    private readonly IUnitOfWork _unitOfWork;

    public CreateClaimHandler(IAppClaimRepository claims, IUnitOfWork unitOfWork)
    {
        _claims = claims;
        _unitOfWork = unitOfWork;
    }

    public async Task<OperationResult> Handle(CreateClaimCommand command, CancellationToken cancellationToken)
    {
        var created = AppClaim.Create(command.ClaimName);
        if (!created.Succeeded)
            return OperationResult.From(created);

        var conflict = await ClaimRules.FindNameConflictAsync(_claims, created.Value.ClaimName, claimId: null);
        if (conflict != null)
            return conflict;

        _claims.Add(created.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}

public sealed record RenameClaimCommand(int ClaimId, string ClaimName) : ICommand<OperationResult>;

public sealed class RenameClaimHandler : ICommandHandler<RenameClaimCommand, OperationResult>
{
    private readonly IAppClaimRepository _claims;
    private readonly IUnitOfWork _unitOfWork;

    public RenameClaimHandler(IAppClaimRepository claims, IUnitOfWork unitOfWork)
    {
        _claims = claims;
        _unitOfWork = unitOfWork;
    }

    public async Task<OperationResult> Handle(RenameClaimCommand command, CancellationToken cancellationToken)
    {
        var claim = await _claims.GetForAdminAsync(command.ClaimId);
        if (claim == null)
            return ClaimRules.NotFound();

        // Unicitatea cere acces la date, deci se verifica aici, inainte ca entitatea sa se
        // schimbe. Restul regulilor (drept de sistem, nume rezervat, lungime) le aplica Rename.
        var conflict = await ClaimRules.FindNameConflictAsync(_claims, command.ClaimName.Trim(), claim.ClaimId);
        if (conflict != null)
            return conflict;

        var renamed = claim.Rename(command.ClaimName);
        if (!renamed.Succeeded)
            return OperationResult.From(renamed);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}

/// <summary>Dezactiveaza dreptul. Nu se sterge fizic (vezi ADR-0007).</summary>
public sealed record DeactivateClaimCommand(int ClaimId) : ICommand<OperationResult>;

public sealed class DeactivateClaimHandler : ICommandHandler<DeactivateClaimCommand, OperationResult>
{
    private readonly IAppClaimRepository _claims;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateClaimHandler(IAppClaimRepository claims, IUnitOfWork unitOfWork)
    {
        _claims = claims;
        _unitOfWork = unitOfWork;
    }

    public async Task<OperationResult> Handle(DeactivateClaimCommand command, CancellationToken cancellationToken)
    {
        var claim = await _claims.GetForAdminAsync(command.ClaimId);
        if (claim == null)
            return ClaimRules.NotFound();

        var deactivated = claim.Deactivate();
        if (!deactivated.Succeeded)
            return OperationResult.From(deactivated);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}

/// <summary>Reactiveaza un drept dezactivat. Rolurile care il aveau il primesc inapoi.</summary>
public sealed record ReactivateClaimCommand(int ClaimId) : ICommand<OperationResult>;

public sealed class ReactivateClaimHandler : ICommandHandler<ReactivateClaimCommand, OperationResult>
{
    private readonly IAppClaimRepository _claims;
    private readonly IUnitOfWork _unitOfWork;

    public ReactivateClaimHandler(IAppClaimRepository claims, IUnitOfWork unitOfWork)
    {
        _claims = claims;
        _unitOfWork = unitOfWork;
    }

    public async Task<OperationResult> Handle(ReactivateClaimCommand command, CancellationToken cancellationToken)
    {
        var claim = await _claims.GetForAdminAsync(command.ClaimId);
        if (claim == null)
            return ClaimRules.NotFound();

        // Idempotent: reactivarea unui drept deja activ nu este o eroare.
        if (claim.IsActive)
            return OperationResult.Success();

        claim.Reactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}

/// <summary>Regulile care cer acces la date si mesajele comune comenzilor pe drepturi.</summary>
internal static class ClaimRules
{
    public static OperationResult NotFound() => OperationResult.Fail("Dreptul nu exista.");

    /// <summary>
    /// Refuzul, daca numele este deja folosit de alt drept; altfel null.
    ///
    /// Verificarea vede si drepturile dezactivate: indexul unic le vede si el, iar
    /// mesajul spune ce e de facut in loc. Doua cereri simultane cu acelasi nume pot trece
    /// amandoua de aici - pe a doua o opreste indexul.
    /// </summary>
    public static async Task<OperationResult?> FindNameConflictAsync(
        IAppClaimRepository claims,
        string name,
        int? claimId)
    {
        if (name.Length == 0)
            return null;

        var existing = await claims.GetByNameForAdminAsync(name);
        if (existing == null || existing.ClaimId == claimId)
            return null;

        return existing.IsActive
            ? OperationResult.Fail($"Exista deja un drept cu numele \"{existing.ClaimName}\".")
            : OperationResult.Fail(
                $"Exista deja un drept dezactivat cu numele \"{existing.ClaimName}\". " +
                "Reactivati-l in loc sa creati unul nou.");
    }
}
