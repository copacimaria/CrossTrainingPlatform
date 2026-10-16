using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace CrossTraining.Infrastructure.Repositories;

/// <summary>
/// Cautare "contine" tradusa in SQL LIKE, cu wildcard-urile introduse de utilizator
/// neutralizate.
///
/// Fara escapare, un "%" scris in caseta de cautare devine wildcard si interogarea
/// intoarce tot tabelul; un "_" se potriveste cu orice caracter. Ambele cazuri sunt
/// acoperite de teste pe SQL Server real.
/// </summary>
internal static class LikeFilter
{
    private const string EscapeCharacter = @"\";

    public static IQueryable<T> Apply<T>(
        IQueryable<T> query,
        string? searchTerm,
        Expression<Func<T, string>> property)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return query;

        var pattern = $"%{Escape(searchTerm)}%";

        // Construim EF.Functions.Like(property, pattern, escape) pe expresia primita,
        // ca sa poata fi refolosit de orice repository.
        var likeCall = Expression.Call(
            typeof(DbFunctionsExtensions),
            nameof(DbFunctionsExtensions.Like),
            Type.EmptyTypes,
            Expression.Constant(EF.Functions),
            property.Body,
            Expression.Constant(pattern),
            Expression.Constant(EscapeCharacter));

        var predicate = Expression.Lambda<Func<T, bool>>(likeCall, property.Parameters);
        return query.Where(predicate);
    }

    private static string Escape(string value)
        => value.Replace(@"\", @"\\", StringComparison.Ordinal)
                .Replace("%", @"\%", StringComparison.Ordinal)
                .Replace("_", @"\_", StringComparison.Ordinal)
                .Replace("[", @"\[", StringComparison.Ordinal);
}
