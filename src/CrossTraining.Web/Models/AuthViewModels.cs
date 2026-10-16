using System.ComponentModel.DataAnnotations;

namespace CrossTraining.Web.Models;

// Modelele de formular traiesc in stratul Web: ele poarta atributele de validare pentru
// UI si stiu ce campuri are ecranul. Controllerul le traduce in comenzi si interogari;
// stratul Application nu vede niciodata un model de formular.

public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

}

public class RegisterViewModel
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string FamilyName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6, ErrorMessage = "Parola trebuie sa aiba minim 6 caractere.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

}

public class ChangePasswordViewModel
{
    [Required, DataType(DataType.Password)]
    public string OldPassword { get; set; } = string.Empty;

    [Required, MinLength(6, ErrorMessage = "Parola trebuie sa aiba minim 6 caractere.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

}
