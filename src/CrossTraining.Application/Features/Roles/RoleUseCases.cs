using CrossTraining.Application.Abstractions.Messaging;
using CrossTraining.Application.Abstractions.Persistence;
using CrossTraining.Application.Authorization;
using CrossTraining.Application.DTOs;
using CrossTraining.Domain.Entities;
using FluentValidation;

namespace CrossTraining.Application.Features.Roles;

// ------------------- CITIRI -------------------

/// <summary>Lista paginata de roluri, inclusiv cele dezactivate, ca sa poata fi reactivate.</summary>
public sealed record SearchRolesQuery(string? SearchTerm, int Page, int PageSize)
    : IQuery<PagedResult<RoleDto>>;

public sealed class SearchRolesValidator : AbstractValidator<SearchRolesQuery>
{
    public SearchRolesValidator()
    {
        RuleFor(q => q.Page).GreaterThan(0);
        RuleFor(q => q.PageSize).InclusiveBetween(1, 200);
    }
}

public sealed class SearchRolesHandler : IQueryHandler<SearchRolesQuery, PagedResult<RoleDto>>
{
    private readonly IAppRoleRepository _roles;

    public SearchRolesHandler(IAppRoleRepository roles) => _roles = roles;

    public async Task<PagedResult<RoleDto>> Handle(SearchRolesQuery query, CancellationToken cancellationToken)
    {
        var result = await _roles.SearchForAdminAsync(query.SearchTerm, query.Page, query.PageSize);
        return result.Map(r => new RoleDto(r.RoleId, r.RoleName, r.IsActive));
    }
}

public sealed record GetRoleQuery(int RoleId) : IQuery<RoleDto?>;

public sealed class GetRoleHandler : IQueryHandler<GetRoleQuery, RoleDto?>
{
    private readonly IAppRoleRepository _roles;

    public GetRoleHandler(IAppRoleRepository roles) => _roles = roles;

    public async Task<RoleDto?> Handle(GetRoleQuery query, CancellationToken cancellationToken)
    {
        var role = await _roles.GetForAdminAsync(query.RoleId);
        return role == null ? null : new RoleDto(role.RoleId, role.RoleName, role.IsActive);
    }
}

public sealed record GetRoleClaimsQuery(int RoleId) : IQuery<List<ClaimDto>>;

public sealed class GetRoleClaimsHandler : IQueryHandler<GetRoleClaimsQuery, List<ClaimDto>>
{
    private readonly IAppRoleRepository _roles;

    public GetRoleClaimsHandler(IAppRoleRepository roles) => _roles = roles;

    public async Task<List<ClaimDto>> Handle(GetRoleClaimsQuery query, CancellationToken cancellationToken)
        => (await _roles.GetClaimsForRoleAsync(query.RoleId))
            .Select(c => new ClaimDto(c.ClaimId, c.ClaimName, c.IsActive))
            .ToList();
}

// ------------------- COMENZI -------------------

// Ca la drepturi: regulile care cer acces la date sau protejeaza rolurile de sistem se
// intorc ca OperationResult, cu un mesaj pentru ecran.

public sealed record CreateRoleCommand(string RoleName) : ICommand<OperationResult>;

public sealed class CreateRoleValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleValidator() => RuleFor(c => c.RoleName).NotEmpty().MaximumLength(100);
}

public sealed class CreateRoleHandler : ICommandHandler<CreateRoleCommand, OperationResult>
{
    private readonly IAppRoleRepository _roles;
    private readonly IUnitOfWork _unitOfWork;

    public CreateRoleHandler(IAppRoleRepository roles, IUnitOfWork unitOfWork)
    {
        _roles = roles;
        _unitOfWork = unitOfWork;
    }

    public async Task<OperationResult> Handle(CreateRoleCommand command, CancellationToken cancellationToken)
    {
        var name = command.RoleName.Trim();

        if (SystemRoles.IsSystemRole(name))
            return RoleRules.ReservedName(name);

        var conflict = await RoleRules.FindNameConflictAsync(_roles, name, roleId: null);
        if (conflict != null)
            return conflict;

        _roles.Add(new AppRole { RoleName = name, IsActive = true });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}

public sealed record RenameRoleCommand(int RoleId, string RoleName) : ICommand<OperationResult>;

public sealed class RenameRoleValidator : AbstractValidator<RenameRoleCommand>
{
    public RenameRoleValidator()
    {
        RuleFor(c => c.RoleId).GreaterThan(0);
        RuleFor(c => c.RoleName).NotEmpty().MaximumLength(100);
    }
}

public sealed class RenameRoleHandler : ICommandHandler<RenameRoleCommand, OperationResult>
{
    private readonly IAppRoleRepository _roles;
    private readonly IUnitOfWork _unitOfWork;

    public RenameRoleHandler(IAppRoleRepository roles, IUnitOfWork unitOfWork)
    {
        _roles = roles;
        _unitOfWork = unitOfWork;
    }

    public async Task<OperationResult> Handle(RenameRoleCommand command, CancellationToken cancellationToken)
    {
        var role = await _roles.GetForAdminAsync(command.RoleId);
        if (role == null)
            return RoleRules.NotFound();

        var name = command.RoleName.Trim();

        // Salvarea formularului fara nicio schimbare nu este o redenumire.
        if (string.Equals(role.RoleName, name, StringComparison.Ordinal))
            return OperationResult.Success();

        // Rolurile de sistem sunt recunoscute dupa nume (vezi SystemRoles), deci un rol
        // de sistem redenumit si-ar pierde protectia.
        if (SystemRoles.IsSystemRole(role.RoleName))
            return OperationResult.Fail(
                $"Rolul \"{role.RoleName}\" este folosit de aplicatie si nu poate fi redenumit.");

        if (SystemRoles.IsSystemRole(name))
            return RoleRules.ReservedName(name);

        var conflict = await RoleRules.FindNameConflictAsync(_roles, name, role.RoleId);
        if (conflict != null)
            return conflict;

        role.RoleName = name;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}

/// <summary>Dezactiveaza rolul. Nu se sterge fizic (vezi ADR-0007).</summary>
public sealed record DeactivateRoleCommand(int RoleId) : ICommand<OperationResult>;

public sealed class DeactivateRoleValidator : AbstractValidator<DeactivateRoleCommand>
{
    public DeactivateRoleValidator() => RuleFor(c => c.RoleId).GreaterThan(0);
}

public sealed class DeactivateRoleHandler : ICommandHandler<DeactivateRoleCommand, OperationResult>
{
    private readonly IAppRoleRepository _roles;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateRoleHandler(IAppRoleRepository roles, IUnitOfWork unitOfWork)
    {
        _roles = roles;
        _unitOfWork = unitOfWork;
    }

    public async Task<OperationResult> Handle(DeactivateRoleCommand command, CancellationToken cancellationToken)
    {
        var role = await _roles.GetForAdminAsync(command.RoleId);
        if (role == null)
            return RoleRules.NotFound();

        // Un rol dezactivat isi pierde toate drepturile (filtrul global). Pentru Admin,
        // asta inseamna ca nimeni nu mai poate administra aplicatia; pentru User, ca
        // fiecare cont nou porneste dintr-un rol inexistent.
        if (SystemRoles.IsSystemRole(role.RoleName))
            return OperationResult.Fail(
                $"Rolul \"{role.RoleName}\" este folosit de aplicatie si nu poate fi dezactivat.");

        role.IsActive = false;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}

/// <summary>Reactiveaza un rol dezactivat, impreuna cu drepturile pe care le avea.</summary>
public sealed record ReactivateRoleCommand(int RoleId) : ICommand<OperationResult>;

public sealed class ReactivateRoleValidator : AbstractValidator<ReactivateRoleCommand>
{
    public ReactivateRoleValidator() => RuleFor(c => c.RoleId).GreaterThan(0);
}

public sealed class ReactivateRoleHandler : ICommandHandler<ReactivateRoleCommand, OperationResult>
{
    private readonly IAppRoleRepository _roles;
    private readonly IUnitOfWork _unitOfWork;

    public ReactivateRoleHandler(IAppRoleRepository roles, IUnitOfWork unitOfWork)
    {
        _roles = roles;
        _unitOfWork = unitOfWork;
    }

    public async Task<OperationResult> Handle(ReactivateRoleCommand command, CancellationToken cancellationToken)
    {
        var role = await _roles.GetForAdminAsync(command.RoleId);
        if (role == null)
            return RoleRules.NotFound();

        // Idempotent: reactivarea unui rol deja activ nu este o eroare.
        if (role.IsActive)
            return OperationResult.Success();

        role.IsActive = true;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}

public sealed record GrantClaimToRoleCommand(int RoleId, int ClaimId) : ICommand<OperationResult>;

public sealed class GrantClaimToRoleValidator : AbstractValidator<GrantClaimToRoleCommand>
{
    public GrantClaimToRoleValidator()
    {
        RuleFor(c => c.RoleId).GreaterThan(0);
        RuleFor(c => c.ClaimId).GreaterThan(0);
    }
}

public sealed class GrantClaimToRoleHandler : ICommandHandler<GrantClaimToRoleCommand, OperationResult>
{
    private readonly IAppRoleRepository _roles;
    private readonly IAppClaimRepository _claims;
    private readonly IUnitOfWork _unitOfWork;

    public GrantClaimToRoleHandler(IAppRoleRepository roles, IAppClaimRepository claims, IUnitOfWork unitOfWork)
    {
        _roles = roles;
        _claims = claims;
        _unitOfWork = unitOfWork;
    }

    public async Task<OperationResult> Handle(GrantClaimToRoleCommand command, CancellationToken cancellationToken)
    {
        // Idempotent: acordarea unui drept deja acordat nu este o eroare.
        var existing = await _roles.GetRoleClaimAsync(command.RoleId, command.ClaimId);
        if (existing != null)
            return OperationResult.Success();

        // Verificate aici, nu lasate pe seama cheii straine: un id inexistent ar ajunge
        // altfel la utilizator ca eroare 500, nu ca mesaj.
        if (await _roles.GetForAdminAsync(command.RoleId) == null)
            return RoleRules.NotFound();

        var claim = await _claims.GetForAdminAsync(command.ClaimId);
        if (claim == null || !claim.IsActive)
            return OperationResult.Fail("Dreptul nu exista sau este dezactivat.");

        _roles.AddClaim(new RoleClaim { RoleId = command.RoleId, ClaimId = command.ClaimId });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}

public sealed record RevokeClaimFromRoleCommand(int RoleId, int ClaimId) : ICommand<OperationResult>;

public sealed class RevokeClaimFromRoleValidator : AbstractValidator<RevokeClaimFromRoleCommand>
{
    public RevokeClaimFromRoleValidator()
    {
        RuleFor(c => c.RoleId).GreaterThan(0);
        RuleFor(c => c.ClaimId).GreaterThan(0);
    }
}

public sealed class RevokeClaimFromRoleHandler : ICommandHandler<RevokeClaimFromRoleCommand, OperationResult>
{
    private readonly IAppRoleRepository _roles;
    private readonly IAppClaimRepository _claims;
    private readonly IUnitOfWork _unitOfWork;

    public RevokeClaimFromRoleHandler(IAppRoleRepository roles, IAppClaimRepository claims, IUnitOfWork unitOfWork)
    {
        _roles = roles;
        _claims = claims;
        _unitOfWork = unitOfWork;
    }

    public async Task<OperationResult> Handle(RevokeClaimFromRoleCommand command, CancellationToken cancellationToken)
    {
        var link = await _roles.GetRoleClaimAsync(command.RoleId, command.ClaimId);
        if (link == null)
            return OperationResult.Success();

        // Rolul Admin pastreaza drepturile de sistem. Fara roles.manage, de exemplu, nimeni
        // nu ar mai putea acorda inapoi dreptul scos - nici macar adminul care l-a scos.
        var role = await _roles.GetForAdminAsync(command.RoleId);
        var claim = await _claims.GetForAdminAsync(command.ClaimId);
        if (role != null && claim != null
            && SystemRoles.IsAdmin(role.RoleName)
            && Permissions.IsSystemPermission(claim.ClaimName))
        {
            return OperationResult.Fail(
                $"Dreptul \"{claim.ClaimName}\" nu poate fi scos din rolul {SystemRoles.Admin}: " +
                "aplicatia ar ramane fara cine sa o administreze.");
        }

        _roles.RemoveClaim(link);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}

/// <summary>Regulile si mesajele comune comenzilor pe roluri.</summary>
internal static class RoleRules
{
    public static OperationResult NotFound() => OperationResult.Fail("Rolul nu exista.");

    public static OperationResult ReservedName(string name)
        => OperationResult.Fail($"Numele \"{name}\" este rezervat unui rol al aplicatiei.");

    /// <summary>
    /// Refuzul, daca numele este deja folosit de alt rol; altfel null. Vede si rolurile
    /// dezactivate, la fel ca indexul unic.
    /// </summary>
    public static async Task<OperationResult?> FindNameConflictAsync(
        IAppRoleRepository roles,
        string name,
        int? roleId)
    {
        var existing = await roles.GetByNameForAdminAsync(name);
        if (existing == null || existing.RoleId == roleId)
            return null;

        return existing.IsActive
            ? OperationResult.Fail($"Exista deja un rol cu numele \"{existing.RoleName}\".")
            : OperationResult.Fail(
                $"Exista deja un rol dezactivat cu numele \"{existing.RoleName}\". " +
                "Reactivati-l in loc sa creati unul nou.");
    }
}
