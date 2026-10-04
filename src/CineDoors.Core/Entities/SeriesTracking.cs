namespace CineDoors.Core.Entities;

public class SeriesTracking // Suivi d'une série pas un utilisateur. L'état de la série se process à partir des épisodes vus (plus safe et moins de risques de désynchronisation)
{
    public int UserId { get; set; }
    public int SeriesId { get; set; }

    public bool Notify { get; set; }

    public DateTime AddedAt { get; set; }

    public Series? Series { get; set; } // CF MovieTracking, association avec l'identifiant de la série, n'est pas une colonne
}
