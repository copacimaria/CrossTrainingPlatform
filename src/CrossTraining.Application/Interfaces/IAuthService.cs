using CrossTraining.Application.DTOs;
using CrossTraining.Application.ViewModels;
using CrossTraining.Domain.Entities;

namespace CrossTraining.Application.Interfaces;

/// <summary>Logica de autentificare / inregistrare.</summary>
public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterViewModel model);
    Task<AuthResult> LoginAsync(LoginViewModel model);
}
