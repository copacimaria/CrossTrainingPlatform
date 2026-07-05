namespace CrossTraining.Application.DTOs;

/// <summary>Rezultatul unei operatii de schimbare a parolei.</summary>
public class OperationResult
{
    public bool Succeeded { get; init; }
    public string? ErrorField { get; init; }
    public string? Error { get; init; }

    public static OperationResult Success() => new() { Succeeded = true };
    public static OperationResult Fail(string error, string? field = null)
        => new() { Succeeded = false, Error = error, ErrorField = field };
}
