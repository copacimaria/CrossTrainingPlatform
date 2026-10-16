using System.Globalization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace CrossTraining.Web.Security;

/// <summary>
/// Semneaza id-ul utilizatorului pentru pasul de alegere a domeniilor din fluxul de
/// inregistrare.
///
/// Pasul acela se petrece inainte de login, deci nu exista un utilizator autentificat
/// din care sa citim id-ul. Varianta anterioara trimitea id-ul in clar prin query string
/// si il accepta ca atare la POST, ceea ce insemna ca oricine putea rescrie domeniile
/// oricarui cont doar ghicind un numar (IDOR).
///
/// Token-ul este semnat cu cheia de Data Protection a aplicatiei si expira, deci nu poate
/// fi fabricat de client si nu ramane valabil la nesfarsit.
/// </summary>
public sealed class RegistrationTokenProtector
{
    /// <summary>
    /// Scopul izoleaza criptografic aceste token-uri: unul emis aici nu poate fi folosit
    /// in alt context care foloseste Data Protection.
    /// </summary>
    private const string Purpose = "CrossTraining.Registration.SelectDomain.v1";

    /// <summary>Cat are utilizatorul la dispozitie ca sa termine pasul de alegere a domeniilor.</summary>
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

    private readonly ITimeLimitedDataProtector _protector;

    public RegistrationTokenProtector(IDataProtectionProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _protector = provider.CreateProtector(Purpose).ToTimeLimitedDataProtector();
    }

    public string Protect(int userId)
        => _protector.Protect(userId.ToString(CultureInfo.InvariantCulture), Lifetime);

    /// <summary>Id-ul din token, sau <c>null</c> daca token-ul lipseste, e falsificat sau a expirat.</summary>
    public int? Unprotect(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            var raw = _protector.Unprotect(token);
            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var userId)
                ? userId
                : null;
        }
        catch (CryptographicException)
        {
            // Token invalid sau expirat. Tratat ca absent, fara sa scurgem motivul.
            return null;
        }
    }
}
