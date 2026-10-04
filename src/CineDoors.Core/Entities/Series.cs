namespace CineDoors.Core.Entities;

public class Series
{
    public int Id { get; set; }
    public int TmdbId { get; set; }
    public required string Title { get; set; } // required car obligatoire

    // "?" autoriste juste une valeur null
    public DateOnly? FirstAirDate { get; set; }
    public string? PosterPath { get; set; }
    public string? BackdropPath { get; set; } // Grande image horizontale (en-tête de la fiche)
    public string? Genres { get; set; } // Genres prêts à afficher, ex. "Drame, Policier"
    public string? Overview { get; set; }

    // liste des saisons d'une série. Représente une relation plus qu'une colonne en elle-même
    public List<Season> Seasons { get; set; } = new List<Season>(); 
}