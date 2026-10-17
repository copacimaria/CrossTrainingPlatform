namespace CrossTraining.Web.Extensions;

/// <summary>
/// Ajutoare de afisare pentru view-uri: initialele unui nume (avatar, iconita unei
/// aplicatii) si o nuanta de culoare care ramane aceeasi pentru acelasi nume.
/// </summary>
public static class DisplayExtensions
{
    // Nuante alese sa arate bine pe fundalul inchis: cyan, albastru, indigo, violet,
    // magenta, roz, chihlimbar, verde.
    private static readonly int[] Palette = [190, 210, 235, 262, 288, 330, 38, 158];

    private static readonly char[] WordSeparators = [' ', '-', '_', '.'];

    /// <summary>Doua litere: initialele primelor doua cuvinte, altfel primele doua litere.</summary>
    public static string Initials(this string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "?";

        var words = name.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries);
        var letters = words.Length >= 2
            ? string.Concat(words[0][0], words[1][0])
            : words[0][..Math.Min(2, words[0].Length)];

        return letters.ToUpperInvariant();
    }

    /// <summary>
    /// Nuanta (0-360) pentru un text. Calculata explicit, nu cu <c>string.GetHashCode</c>,
    /// care da alt rezultat la fiecare pornire a aplicatiei.
    /// </summary>
    public static int Hue(this string? text)
    {
        if (string.IsNullOrEmpty(text))
            return Palette[0];

        var hash = 0u;
        foreach (var c in text)
            hash = unchecked((hash * 31) + c);

        return Palette[(int)(hash % (uint)Palette.Length)];
    }
}
