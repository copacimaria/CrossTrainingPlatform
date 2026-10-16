using CrossTraining.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace CrossTraining.Web.Security;

/// <summary>Numele claim-ului in care se scriu permisiunile la autentificare.</summary>
public static class PermissionClaim
{
    public const string Type = "permission";
}

/// <summary>Cerinta: utilizatorul are dreptul numit.</summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }

    public string Permission { get; }
}

/// <summary>
/// Verifica dreptul in claim-urile din cookie.
///
/// Nu exista scurtatura pentru rolul Admin: sistemul de drepturi exista tocmai ca sa nu
/// se mai lucreze pe roluri hardcodate. Rolul Admin primeste toate drepturile prin
/// migrarea SeedPermissions, deci comportamentul ramane acelasi, dar sursa de adevar
/// este baza de date, nu un `if` din cod.
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.HasPermission(requirement.Permission))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

/// <summary>Cere un drept anume pe o actiune sau pe un controller.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission)
        : base(PermissionPolicyProvider.PolicyPrefix + permission)
    {
    }
}

/// <summary>
/// Construieste politicile de permisiuni la cerere.
///
/// Alternativa ar fi inregistrarea manuala a cate unei politici pentru fiecare drept in
/// Program.cs - o lista care s-ar desincroniza de
/// <see cref="CrossTraining.Application.Authorization.Permissions"/> la prima
/// adaugare uitata.
/// </summary>
public sealed class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public const string PolicyPrefix = "perm:";

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
    {
    }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var existing = await base.GetPolicyAsync(policyName);
        if (existing != null)
            return existing;

        if (!policyName.StartsWith(PolicyPrefix, StringComparison.Ordinal))
            return null;

        var permission = policyName[PolicyPrefix.Length..];

        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permission))
            .Build();
    }
}
