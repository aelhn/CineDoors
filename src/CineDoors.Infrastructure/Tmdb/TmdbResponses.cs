using System.Text.Json.Serialization;

namespace CineDoors.Infrastructure.Tmdb;

// Copie de la forme du JSON renvoyé par TMDB. Qu'en lecture 

public class TmdbMovieListResponse
{
    public List<TmdbMovieResult> Results { get; set; } = new List<TmdbMovieResult>();

    public int TotalPages { get; set; } // TMDB découpe ses pages en 20 films/série chacune ("total_pages" dans le JSON)
}

// Info d'un film
public class TmdbMovieResult
{
    public int Id { get; set; }
    public string Title { get; set; } = "";

    // TMDB envoie la date en texte ("2026-09-30"), parfois vide : on la convertit nous-mêmes.
    public string? ReleaseDate { get; set; }

    public string? PosterPath { get; set; }
    public string? BackdropPath { get; set; }
    public string? Overview { get; set; }

    // Les genres arrivent sous forme d'identifiants (28, 878...), pas de noms.
    public List<int> GenreIds { get; set; } = new List<int>();
}

// Second appel à l'API ("genre/movie/list") pour construire une liste liant un genre à X infos : { "genres": [ { "id": 28, "name": "Action" }, ... ] }
public class TmdbGenreListResponse
{
    public List<TmdbGenre> Genres { get; set; } = new List<TmdbGenre>();
}

// Réponse de "movie/{id}" (fiche détaillée d'un film). On n'en lit que la durée, en minutes (pour le récap du temps)
public class TmdbMovieDetailsResponse
{
    public int? Runtime {  get; set; }
}

public class TmdbGenre
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

// Réponse de "movie/{id}/release_dates" : toutes les dates de sortie d'un film, pays par pays.
// { "results": [ { "iso_3166_1": "FR", "release_dates": [ { "release_date": "2026-09-23T00:00:00.000Z", "type": 3 } ] } ] }
public class TmdbReleaseDatesResponse
{
    public List<TmdbCountryReleases> Results { get; set; } = new List<TmdbCountryReleases>();
}

public class TmdbCountryReleases
{
    // Code du pays ("FR"). Le nom JSON contient des chiffres que la conversion automatique
    // ne sait pas retrouver : on l'indique à la main avec cet attribut.
    [JsonPropertyName("iso_3166_1")]
    public string CountryCode { get; set; } = "";

    public List<TmdbReleaseDate> ReleaseDates { get; set; } = new List<TmdbReleaseDate>();
}

public class TmdbReleaseDate
{
    // Date et heure en texte : "2026-09-23T00:00:00.000Z"
    public string ReleaseDate { get; set; } = "";

    // Type de sortie : 2 et 3 = cinéma, 4 = numérique, 5 = DVD / Blu-ray, 6 = télévision
    public int Type { get; set; }
}