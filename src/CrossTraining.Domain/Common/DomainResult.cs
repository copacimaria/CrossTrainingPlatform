namespace CrossTraining.Domain.Common;

/// <summary>
/// Rezultatul unei operatii pe o entitate: reusita, sau refuzul impreuna cu motivul lui.
///
/// O regula de business incalcata (un nume prea lung, un rol de sistem care nu poate fi
/// dezactivat) este un raspuns obisnuit, nu o situatie exceptionala - deci nu se
/// semnaleaza prin exceptie. Apelantul primeste motivul si il arata utilizatorului.
/// </summary>
public class DomainResult
{
    protected DomainResult(bool succeeded, string? error)
    {
        Succeeded = succeeded;
        Error = error;
    }

    public bool Succeeded { get; }

    /// <summary>Motivul refuzului, pe intelesul utilizatorului. Null daca operatia a reusit.</summary>
    public string? Error { get; }

    public static DomainResult Ok() => new(true, null);

    public static DomainResult Fail(string error) => new(false, error);

    public static DomainResult<T> Ok<T>(T value) => new(value);

    public static DomainResult<T> Fail<T>(string error) => new(error);
}

/// <summary>Rezultatul unei operatii care, daca reuseste, produce o valoare (o entitate noua).</summary>
public sealed class DomainResult<T> : DomainResult
{
    private readonly T? _value;

    internal DomainResult(T value) : base(true, null)
    {
        _value = value;
    }

    internal DomainResult(string error) : base(false, error)
    {
    }

    /// <summary>Valoarea produsa. Citirea ei dupa un refuz este o greseala de programare.</summary>
    public T Value => Succeeded
        ? _value!
        : throw new InvalidOperationException($"Operatia a fost refuzata: {Error}");
}
