
namespace CineDoors.Core.Entities;
public class MovieTracking // Suivi d'un film par un utilisateur, une ligne = une association

{
    // clés vers utilisateur et film
    public int UserId { get; set; }
    public int MovieId { get; set; }
    public MovieStatus Status { get; set; }

    public DateTime? WatchedAt { get; set; } // Date du visionnage. A vide = "à voir"

    public bool Notify {  get; set; } // Si l'utilisateur veut être prévenu de la sortie

    public DateTime AddedAt { get; set; }

    public Movie? Movie { get; set; } // Lié au MovieId. Permet donc de récupérer le titre du film plus tard. Cette ligne correspond donc à une association et pas une colonne en elle-même 
}
