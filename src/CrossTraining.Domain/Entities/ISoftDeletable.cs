namespace CrossTraining.Domain.Entities;

/// <summary>
/// Entitate care nu se sterge fizic, ci se dezactiveaza.
///
/// Inainte, jumatate din entitati faceau soft-delete (rol, joc) si jumatate hard-delete
/// (domeniu, drept), fara niciun motiv. Interfata face regula explicita si permite
/// filtrul global din <c>AppDbContext</c>.
///
/// Doar citire: starea se schimba prin metodele entitatii (<c>Deactivate</c>,
/// <c>Reactivate</c>), care isi verifica regulile - nu prin setarea directa a proprietatii.
/// </summary>
public interface ISoftDeletable
{
    bool IsActive { get; }
}
