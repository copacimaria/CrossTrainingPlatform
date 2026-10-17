using System.ComponentModel.DataAnnotations;

namespace CrossTraining.Web.Models;

// Modelele de formular traiesc in stratul Web: ele poarta atributele de validare pentru
// UI si stiu ce campuri are ecranul. Controllerul le traduce in comenzi si interogari;
// stratul Application nu vede niciodata un model de formular.
//
// Mesajele sunt scrise explicit: cele implicite ale atributelor sunt in engleza
// ("The Email field is required."), iar interfata este in romana.

public class LoginViewModel
{
    [Required(ErrorMessage = "Introduceti adresa de email.")]
    [EmailAddress(ErrorMessage = "Adresa de email nu este valida.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Introduceti parola.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Introduceti prenumele.")]
    [StringLength(100, ErrorMessage = "Prenumele poate avea cel mult 100 de caractere.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Introduceti numele de familie.")]
    [StringLength(100, ErrorMessage = "Numele poate avea cel mult 100 de caractere.")]
    public string FamilyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Introduceti adresa de email.")]
    [EmailAddress(ErrorMessage = "Adresa de email nu este valida.")]
    [StringLength(256, ErrorMessage = "Adresa de email poate avea cel mult 256 de caractere.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Alegeti o parola.")]
    [MinLength(6, ErrorMessage = "Parola trebuie sa aiba minim 6 caractere.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Introduceti parola actuala.")]
    [DataType(DataType.Password)]
    public string OldPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Alegeti parola noua.")]
    [MinLength(6, ErrorMessage = "Parola trebuie sa aiba minim 6 caractere.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

}
