namespace CrossTraining.Web.Security;

/// <summary>Numele politicilor de rate limiting, ca sa nu circule ca string-uri libere.</summary>
public static class RateLimitPolicies
{
    /// <summary>Login si register: acelasi buget de incercari per adresa IP.</summary>
    public const string Authentication = "auth";
}
