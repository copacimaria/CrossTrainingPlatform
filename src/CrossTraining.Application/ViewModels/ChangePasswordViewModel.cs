using System.ComponentModel.DataAnnotations;

namespace CrossTraining.Application.ViewModels;

public class ChangePasswordViewModel
{
    [Required]
    public string OldPassword { get; set; } = string.Empty;

    [Required, MinLength(6, ErrorMessage = "Parola trebuie sa aiba minim 6 caractere.")]
    public string NewPassword { get; set; } = string.Empty;
}
