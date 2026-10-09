using CineDoors.Core.Entities;
using System.Globalization;
using System.Text.Json;

namespace CineDoors.Infrastructure.Tmdb;

// Interroge l'API de TMDB (catalogue de films) et convertit ses réponses en entités Movie.
public class TmdbClient
{
    // Adresse du serveur d'images de TMDB. "w342" est la largeur de l'affiche, en pixels.
    private const string PosterBaseUrl = "https://image.tmdb.org/t/p/w342";

    private readonly HttpClient _http;

    // Réglage de lecture du JSON : "poster_path" (JSON) correspond à PosterPath (chez nous en C#).
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    // Noms des genres par identifiant (28 -> "Action"). Chargé une seule fois, au premier besoin.
    private Dictionary<int, string>? _genreNames;

    // HttpClient est fourni déjà configuré (adresse de l'API, jeton d'accès) par App.xaml.cs.
    public TmdbClient(HttpClient http)
    {
        _http = http;
    }

    // Renvoie les films actuellement en salle, du plus récent au plus ancien (règle RG23).
    public async Task<List<Movie>> GetNowPlayingAsync()
    {
        Dictionary<int, string> genreNames = await GetGenreNamesAsync();

        // GetStringAsync envoie la requête et renvoie le texte de la réponse (du JSON).
        // Il lève une exception si le serveur répond par une erreur (jeton refusé, pas de réseau, etc..).
        string json = await _http.GetStringAsync("movie/now_playing?language=fr-FR&region=FR&page=1");

        List<Movie> movies = ParseMovies(json, genreNames);

        // Tri par date de sortie décroissante.
        return movies.OrderByDescending(m => m.ReleaseDate).ToList();
    }



    // Récupération de la durée d'un film en minutes. TMDB ne la donne pas donc on la calcule via la fiche du film. Renvoie null si la durée n'est pas connue.
    public async Task<int?> GetRuntimeAsync(int tmdbId)
    {
        string json = await _http.GetStringAsync("movie/" + tmdbId + "?language=fr-FR");
        TmdbMovieDetailsResponse? response = JsonSerializer.Deserialize<TmdbMovieDetailsResponse>(json, _jsonOptions);

        // TMDB Renvoie 0 si durée inconnue, donc forcer le null
        if (response ==null || response.Runtime == 0)
        {
            return null;
        }
        return response.Runtime;
    }


    // Convertit le JSON d'une liste de films en entités Movie.
    // Méthode séparée et publique pour pouvoir la tester sans appeler TMDB.
    public List<Movie> ParseMovies(string json, Dictionary<int, string> genreNames)
    {
        TmdbMovieListResponse? response = JsonSerializer.Deserialize<TmdbMovieListResponse>(json, _jsonOptions);

        List<Movie> movies = new List<Movie>();

        if (response == null)
        {
            return movies;
        }

        foreach (TmdbMovieResult result in response.Results)
        {
            Movie movie = new Movie
            {
                TmdbId = result.Id,
                Title = result.Title,
                ReleaseDate = ParseDate(result.ReleaseDate),
                PosterPath = result.PosterPath,
                BackdropPath = result.BackdropPath,
                Overview = result.Overview,
                Genres = BuildGenreText(result.GenreIds, genreNames)
            };

            movies.Add(movie);
        }

        return movies;
    }

    // Adresse complète d'une affiche, à partir du chemin fourni par TMDB ("/abc123.jpg").
    // "static" car pas besoin d'objet, se suffit à elle-même pour fonctionner 
    public static string? GetPosterUrl(string? posterPath)
    {
        if (string.IsNullOrEmpty(posterPath))
        {
            return null;
        }

        return PosterBaseUrl + posterPath;
    }

    // Charge la liste des genres depuis TMDB, la première fois seulement.
    private async Task<Dictionary<int, string>> GetGenreNamesAsync()
    {
        if (_genreNames != null)
        {
            return _genreNames;
        }

        string json = await _http.GetStringAsync("genre/movie/list?language=fr");
        TmdbGenreListResponse? response = JsonSerializer.Deserialize<TmdbGenreListResponse>(json, _jsonOptions);

        Dictionary<int, string> names = new Dictionary<int, string>();

        if (response != null)
        {
            foreach (TmdbGenre genre in response.Genres)
            {
                names[genre.Id] = genre.Name;
            }
        }

        _genreNames = names;
        return names;
    }

    // "2026-09-30" -> date. Texte vide ou illisible -> null.
    private static DateOnly? ParseDate(string? text)
    {
        // TryParseExact renvoie vrai si la conversion a réussi, et range le résultat dans "date" (mot-clé out).
        if (DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date))
        {
            return date;
        }

        return null;
    }

    // Associe un id à un libellé ([28, 878] -> "Action, Science-Fiction" par exemple), si identifiant inconnu, il est ignoré
    private static string? BuildGenreText(List<int> genreIds, Dictionary<int, string> genreNames)
    {
        List<string> names = new List<string>();

        foreach (int id in genreIds)
        {
            // TryGetValue renvoie vrai si la clé existe, et range la valeur dans "name".
            if (genreNames.TryGetValue(id, out string? name))
            {
                names.Add(name);
            }
        }

        if (names.Count == 0)
        {
            return null;
        }

        return string.Join(", ", names);
    }
}