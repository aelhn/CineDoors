namespace CineDoors.Infrastructure.Tmdb;

// Copie de la forme du JSON renvoyé par TMDB. Qu'en lecture 

public class TmdbMovieListResponse
{
    public List<TmdbMovieResult> Results { get; set; } = new List<TmdbMovieResult>();
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