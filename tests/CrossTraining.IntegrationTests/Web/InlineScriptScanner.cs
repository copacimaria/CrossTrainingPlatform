using System.Text.RegularExpressions;

namespace CrossTraining.IntegrationTests.Web;

/// <summary>
/// Gaseste in HTML-ul randat scripturile pe care CSP-ul (<c>script-src 'self'</c>, fara
/// <c>'unsafe-inline'</c>) le blocheaza in browser: atribute de eveniment (<c>onclick=...</c>),
/// blocuri <c>&lt;script&gt;</c> fara <c>src</c> si adrese <c>javascript:</c>.
///
/// Pe server nimic nu esueaza: butonul ar fi doar mort in browser. De aceea verificarea
/// ruleaza pe paginile reale, in teste.
/// </summary>
internal static partial class InlineScriptScanner
{
    public static IReadOnlyList<string> FindViolations(string html)
        =>
        [
            .. EventHandlerAttribute().Matches(html).Select(m => m.Value),
            .. InlineScriptBlock().Matches(html).Select(m => m.Value),
            .. JavaScriptUrl().Matches(html).Select(m => m.Value)
        ];

    [GeneratedRegex("""<[a-z][^>]*\son[a-z]+\s*=[^>]*>""", RegexOptions.IgnoreCase)]
    private static partial Regex EventHandlerAttribute();

    [GeneratedRegex("""<script\b(?![^>]*\bsrc\s*=)[^>]*>""", RegexOptions.IgnoreCase)]
    private static partial Regex InlineScriptBlock();

    [GeneratedRegex("""\b(?:href|src|action|formaction)\s*=\s*["']?\s*javascript:""", RegexOptions.IgnoreCase)]
    private static partial Regex JavaScriptUrl();
}
