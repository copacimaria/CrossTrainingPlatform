using CrossTraining.Domain.Entities;
using CrossTraining.Infrastructure.Security;

namespace CrossTraining.UnitTests.Infrastructure;

/// <summary>
/// Teste pe implementarea reala de Argon2id - nu are sens sa fie mock-uita,
/// tocmai comportamentul criptografic ne intereseaza.
/// </summary>
public class PasswordServiceTests
{
    private readonly PasswordService _sut = new();
    private static readonly AppUser User = new() { UserId = 1, Email = "test@example.com" };

    [Fact]
    public void Hash_NuIntoarceParolaInClar()
    {
        var hash = _sut.Hash(User, "parola-secreta");

        hash.ShouldNotContain("parola-secreta");
        hash.ShouldStartWith("$argon2id$", Case.Sensitive);
    }

    [Fact]
    public void Hash_AceeasiParola_ProduceHashuriDiferite()
    {
        // Salt unic per apel: doua conturi cu aceeasi parola nu se vad ca fiind identice.
        var first = _sut.Hash(User, "aceeasi-parola");
        var second = _sut.Hash(User, "aceeasi-parola");

        first.ShouldNotBe(second);
    }

    [Fact]
    public void Verify_CuParolaCorecta_IntoarceTrue()
    {
        var hash = _sut.Hash(User, "parola-corecta");

        _sut.Verify(User, hash, "parola-corecta").ShouldBeTrue();
    }

    [Fact]
    public void Verify_CuParolaGresita_IntoarceFalse()
    {
        var hash = _sut.Hash(User, "parola-corecta");

        _sut.Verify(User, hash, "parola-gresita").ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("nu-este-un-hash")]
    [InlineData("$argon2id$stricat")]
    public void Verify_CuHashInvalid_IntoarceFalseFaraExceptie(string storedHash)
    {
        // Conturi vechi sau date corupte trebuie tratate ca parola gresita,
        // nu ca eroare de server.
        Should.NotThrow(() => _sut.Verify(User, storedHash, "orice"));
        _sut.Verify(User, storedHash, "orice").ShouldBeFalse();
    }

    [Fact]
    public void Hash_IncludeParametriiInRezultat()
    {
        // Formatul PHC isi poarta parametrii, deci pot fi cresctuti fara
        // sa invalideze hash-urile deja stocate (vezi ADR-0002).
        var hash = _sut.Hash(User, "parola");

        hash.ShouldContain("m=65536");
        hash.ShouldContain("t=3");
        hash.ShouldContain("p=2");
    }
}
