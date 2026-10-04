namespace CineDoors.Core.Entities;

public class AppUser // user est un mot réservé en SQL, évite les erreurs. Ici désigne l'utilisateur tout simplement
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; } // Empreinte du mdp (hash)
    public DateTime CreatedAt { get; set; } // Différent de DateOnly, car contient une heure en plus de la date
    public DateOnly? LastReleaseCheck { get; set; } // Enregistre la date de la dernière vérification des sorties (sert à ne notifier qu'une fois)
}
