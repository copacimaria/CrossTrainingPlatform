using CrossTraining.Application.Abstractions.Messaging;
using CrossTraining.Application.Abstractions.Persistence;
using CrossTraining.Application.DTOs;
using CrossTraining.Domain.Entities;

namespace CrossTraining.Application.Features.Roles;

// Cazurile de utilizare pentru roluri. Regulile (roluri de sistem protejate, Admin nu
// pierde drepturi de sistem) le aplica entitatea AppRole; aici raman unicitatea numelui,
// existenta datelor si orchestrarea.

// ------------------- CITIRI -------------------

/// <summary>Lista paginata de roluri, inclusiv cele dezactivate, ca sa poata fi reactivate.</summary>
public sealed record SearchRolesQuery(string? SearchTerm, int Page, int PageSize)
    : IQuery<PagedResult<RoleDto>>;

public sealed class SearchRolesHandler : IQueryHandler<SearchRolesQuery, PagedResult<RoleDto>>
{
    private readonly IAppRoleRepository _roles;

    public SearchRolesHandler(IAppRoleRepository roles) => _roles = roles;

    public async Task<PagedResult<RoleDto>> Handle(SearchRolesQuery query, CancellationToken cancellationToken)
    {
        var result = await _roles.SearchForAdminAsync(query.SearchTerm, Math.Max(query.Page, 1), query.PageSize);
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

public sealed record CreateRoleCommand(string RoleName) : ICommand<OperationResult>;

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
        var created = AppRole.Create(command.RoleName);
        if (!created.Succeeded)
            return OperationResult.From(created);

        var conflict = await RoleRules.FindNameConflictAsync(_roles, created.Value.RoleName, roleId: null);
        if (conflict != null)
            return conflict;

        _roles.Add(created.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}

public sealed record RenameRoleCommand(int RoleId, string RoleName) : ICommand<OperationResult>;

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

        var conflict = await RoleRules.FindNameConflictAsync(_roles, command.RoleName.Trim(), role.RoleId);
        if (conflict != null)
            return conflict;

        var renamed = role.Rename(command.RoleName);
        if (!renamed.Succeeded)
            return OperationResult.From(renamed);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}

/// <summary>Dezactiveaza rolul. Nu se sterge fizic (vezi ADR-0007).</summary>
public sealed record DeactivateRoleCommand(int RoleId) : ICommand<OperationResult>;

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

        var deactivated = role.Deactivate();
        if (!deactivated.Succeeded)
            return OperationResult.From(deactivated);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}

/// <summary>Reactiveaza un rol dezactivat, impreuna cu drepturile pe care le avea.</summary>
public sealed record ReactivateRoleCommand(int RoleId) : ICommand<OperationResult>;

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

        role.Reactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}

public sealed record GrantClaimToRoleCommand(int RoleId, int ClaimId) : ICommand<OperationResult>;

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
        // Verificate aici, nu lasate pe seama cheii straine: un id inexistent ar ajunge
        // altfel la utilizator ca eroare 500, nu ca mesaj.
        var role = await _roles.GetWithClaimLinksAsync(command.RoleId);
        if (role == null)
            return RoleRules.NotFound();

        var claim = await _claims.GetForAdminAsync(command.ClaimId);
        if (claim == null)
            return OperationResult.Fail("Dreptul nu exista sau este dezactivat.");

        var granted = role.Grant(claim);
        if (!granted.Succeeded)
            return OperationResult.From(granted);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}

public sealed record RevokeClaimFromRoleCommand(int RoleId, int ClaimId) : ICommand<OperationResult>;

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
        var role = await _roles.GetWithClaimLinksAsync(command.RoleId);
        var claim = await _claims.GetForAdminAsync(command.ClaimId);

        // Idempotent: daca rolul sau dreptul nu exista, nu exista nici legatura de scos.
        if (role == null || claim == null)
            return OperationResult.Success();

        var revoked = role.Revoke(claim);
        if (!revoked.Succeeded)
            return OperationResult.From(revoked);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}

/// <summary>Regulile care cer acces la date si mesajele comune comenzilor pe roluri.</summary>
internal static class RoleRules
{
    public static OperationResult NotFound() => OperationResult.Fail("Rolul nu exista.");

    /// <summary>
    /// Refuzul, daca numele este deja folosit de alt rol; altfel null. Vede si rolurile
    /// dezactivate, la fel ca indexul unic.
    /// </summary>
    public static async Task<OperationResult?> FindNameConflictAsync(
        IAppRoleRepository roles,
        string name,
        int? roleId)
    {
        if (name.Length == 0)
            return null;

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
