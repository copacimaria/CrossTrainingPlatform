using CrossTraining.Application.Abstractions.Messaging;
using CrossTraining.Application.Abstractions.Persistence;
using CrossTraining.Application.Authorization;
using CrossTraining.Application.DTOs;
using CrossTraining.Domain.Entities;
using FluentValidation;

namespace CrossTraining.Application.Features.Claims;

// Cazurile de utilizare pentru drepturi. Comanda / validatorul / handlerul stau
// impreuna: cand se schimba una, se schimba de obicei si celelalte.

// ------------------- CITIRI -------------------

/// <summary>Lista paginata de drepturi pentru administrare, inclusiv cele dezactivate.</summary>
public sealed record SearchClaimsQuery(string? SearchTerm, int Page, int PageSize)
    : IQuery<PagedResult<ClaimDto>>;

public sealed class SearchClaimsValidator : AbstractValidator<SearchClaimsQuery>
{
    public SearchClaimsValidator()
    {
        RuleFor(q => q.Page).GreaterThan(0);
        RuleFor(q => q.PageSize).InclusiveBetween(1, 200);
    }
}

public sealed class SearchClaimsHandler : IQueryHandler<SearchClaimsQuery, PagedResult<ClaimDto>>
{
    private readonly IAppClaimRepository _claims;

    public SearchClaimsHandler(IAppClaimRepository claims) => _claims = claims;

    public async Task<PagedResult<ClaimDto>> Handle(SearchClaimsQuery query, CancellationToken cancellationToken)
    {
        var result = await _claims.SearchForAdminAsync(query.SearchTerm, query.Page, query.PageSize);
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

public sealed class SearchAvailableClaimsValidator : AbstractValidator<SearchAvailableClaimsQuery>
{
    public SearchAvailableClaimsValidator() => RuleFor(q => q.RoleId).GreaterThan(0);
}

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

// Regulile de mai jos cer acces la date (numele e deja luat?) sau tin de drepturile de
// care depinde codul, deci se intorc ca OperationResult, nu ca exceptie de validare -
// ecranul arata mesajul utilizatorului.

public sealed record CreateClaimCommand(string ClaimName) : ICommand<OperationResult>;

public sealed class CreateClaimValidator : AbstractValidator<CreateClaimCommand>
{
    public CreateClaimValidator() => RuleFor(c => c.ClaimName).NotEmpty().MaximumLength(100);
}

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
        var name = command.ClaimName.Trim();

        // Drepturile de sistem vin doar din migrari. Altfel, cineva cu claims.manage ar
        // putea crea un "roles.manage" al lui, daca cel adevarat lipseste din baza.
        if (Permissions.IsSystemPermission(name))
            return ClaimRules.ReservedName(name);

        var conflict = await ClaimRules.FindNameConflictAsync(_claims, name, claimId: null);
        if (conflict != null)
            return conflict;

        _claims.Add(new AppClaim { ClaimName = name, IsActive = true });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}

public sealed record RenameClaimCommand(int ClaimId, string ClaimName) : ICommand<OperationResult>;

public sealed class RenameClaimValidator : AbstractValidator<RenameClaimCommand>
{
    public RenameClaimValidator()
    {
        RuleFor(c => c.ClaimId).GreaterThan(0);
        RuleFor(c => c.ClaimName).NotEmpty().MaximumLength(100);
    }
}

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

        var name = command.ClaimName.Trim();

        // Salvarea formularului fara nicio schimbare nu este o redenumire.
        if (string.Equals(claim.ClaimName, name, StringComparison.Ordinal))
            return OperationResult.Success();

        // Autorizarea cauta drepturile dupa nume. Un drept de sistem redenumit inceteaza
        // sa mai deschida ecranele pe care le protejeaza.
        if (Permissions.IsSystemPermission(claim.ClaimName))
            return OperationResult.Fail(
                $"Dreptul \"{claim.ClaimName}\" este folosit de aplicatie si nu poate fi redenumit.");

        // Si invers: un drept oarecare, deja acordat unui rol, redenumit in "roles.manage"
        // ar da acelui rol administrarea tuturor drepturilor.
        if (Permissions.IsSystemPermission(name))
            return ClaimRules.ReservedName(name);

        var conflict = await ClaimRules.FindNameConflictAsync(_claims, name, claim.ClaimId);
        if (conflict != null)
            return conflict;

        claim.ClaimName = name;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}

/// <summary>Dezactiveaza dreptul. Nu se sterge fizic (vezi ADR-0007).</summary>
public sealed record DeactivateClaimCommand(int ClaimId) : ICommand<OperationResult>;

public sealed class DeactivateClaimValidator : AbstractValidator<DeactivateClaimCommand>
{
    public DeactivateClaimValidator() => RuleFor(c => c.ClaimId).GreaterThan(0);
}

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

        // Un drept de sistem dezactivat dispare din toate rolurile (filtrul global), deci
        // ecranele protejate de el devin inaccesibile pentru toata lumea.
        if (Permissions.IsSystemPermission(claim.ClaimName))
            return OperationResult.Fail(
                $"Dreptul \"{claim.ClaimName}\" este folosit de aplicatie si nu poate fi dezactivat.");

        claim.IsActive = false;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}

/// <summary>Reactiveaza un drept dezactivat. Rolurile care il aveau il primesc inapoi.</summary>
public sealed record ReactivateClaimCommand(int ClaimId) : ICommand<OperationResult>;

public sealed class ReactivateClaimValidator : AbstractValidator<ReactivateClaimCommand>
{
    public ReactivateClaimValidator() => RuleFor(c => c.ClaimId).GreaterThan(0);
}

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

        claim.IsActive = true;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}

/// <summary>Regulile si mesajele comune comenzilor pe drepturi.</summary>
internal static class ClaimRules
{
    public static OperationResult NotFound() => OperationResult.Fail("Dreptul nu exista.");

    public static OperationResult ReservedName(string name)
        => OperationResult.Fail($"Numele \"{name}\" este rezervat unui drept al aplicatiei.");

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
