namespace CineDoors.Core.Entities;

public class Movie // Correspond à la table "movie"
{
    public int Id { get; set; } // id interne
    public int TmdbId { get; set; } // id TMDB (API)
    public required string Title { get; set; } // Titre obligatoire

    // "?" autoriste juste une valeur null
    public DateOnly? ReleaseDate { get; set; } 
    public int? RuntimeMinutes { get; set; } 
    public string? PosterPath { get; set; } // Affiche
    public string? BackdropPath { get; set; } 
    public string? Genres { get; set; }
    public string? Overview { get; set; } 
}