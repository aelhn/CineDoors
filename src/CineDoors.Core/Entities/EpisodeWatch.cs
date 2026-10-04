namespace CineDoors.Core.Entities;

// Episode vu par l'utilisateur. Une saison entière = une ligne pour chaque épisode, par utilisateur
public class EpisodeWatch
{
    public int UserId { get; set; }

    public int EpisodeId { get; set; }

    public DateTime WatchedAt { get; set; }
}