namespace CrossTraining.Domain.Entities;

/// <summary>
/// Entitate care nu se sterge fizic, ci se dezactiveaza.
///
/// Inainte, jumatate din entitati faceau soft-delete (rol, joc) si jumatate hard-delete
/// (domeniu, drept), fara niciun motiv. Interfata face regula explicita si permite
/// filtrul global din <c>AppDbContext</c>.
/// </summary>
public interface ISoftDeletable
{
    bool IsActive { get; set; }
}
