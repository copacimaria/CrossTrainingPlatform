using CrossTraining.Web.Extensions;

namespace CrossTraining.IntegrationTests.Web;

/// <summary>
/// Initialele si culoarea din avatar si de pe cardurile aplicatiilor. Functii pure, fara
/// aplicatie pornita; stau aici pentru ca doar acest proiect vede stratul Web.
/// </summary>
public class DisplayExtensionsTests
{
    [Theory]
    [InlineData("Ana Popescu", "AP")]
    [InlineData("joc-128c1207", "J1")]
    [InlineData("Matematica", "MA")]
    [InlineData("x", "X")]
    [InlineData("  ", "?")]
    [InlineData(null, "?")]
    public void Initialele(string? name, string expected)
        => name.Initials().ShouldBe(expected);

    [Fact]
    public void Nuanta_EsteAceeasiPentruAcelasiNume()
    {
        // string.GetHashCode ar da alta culoare la fiecare pornire a aplicatiei.
        "Simulator de chimie".Hue().ShouldBe("Simulator de chimie".Hue());
    }

    [Fact]
    public void Nuanta_VineDinPaletaTemei()
    {
        int[] palette = [190, 210, 235, 262, 288, 330, 38, 158];

        var hues = Enumerable.Range(0, 200).Select(i => $"aplicatia-{i}".Hue()).Distinct().ToList();

        hues.ShouldAllBe(h => palette.Contains(h));
        hues.Count.ShouldBeGreaterThan(4, "numele diferite trebuie sa primeasca nuante diferite");
    }
}
