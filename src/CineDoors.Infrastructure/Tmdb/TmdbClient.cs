using CineDoors.Core.Entities;
using System.Globalization;
using System.Text.Json;

namespace CineDoors.Infrastructure.Tmdb;

// Interroge l'API de TMDB (catalogue de films) et convertit ses réponses en entités Movie.
public class TmdbClient
{
    // Adresse du serveur d'images de TMDB. "w342" est la largeur de l'affiche, en pixels.
    private const string PosterBaseUrl = "https://image.tmdb.org/t/p/w342";

    private const int MaxPages = 15; // Nombre max de pages demandées à TMDB pour une liste (20 film/série par page)

    private const int MinRuntimeMinutes = 40; // Durée minimale d'un film pour apparaitre sur l'accueil (pour éviter les court-métrage)
    private const int InTheatersDays = 42; // Un film est considéré "en salle" s'il est sorti en france dans les 6 dernières semaines
    private readonly HttpClient _http;

    // Réglage de lecture du JSON : "poster_path" (JSON) correspond à PosterPath (chez nous en C#).
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    // Noms des genres par identifiant (28 -> "Action"). Chargé une seule fois, au premier besoin.
    private Dictionary<int, string>? _genreNames;

    // Durées déjà demandées à TMDB (id du film -> durée). Evite de redemander la même à chaque retour sur l'accueil.
    private readonly Dictionary<int, int?> _runtimes = new Dictionary<int, int?>();

    // HttpClient est fourni déjà configuré (adresse de l'API, jeton d'accès) par App.xaml.cs.
    public TmdbClient(HttpClient http)
    {
        _http = http;
    }

    // Renvoie les films actuellement en salle, du plus récent au plus ancien (règle RG23).
    public async Task<List<Movie>> GetNowPlayingAsync()
    {
        Dictionary<int, string> genreNames = await GetGenreNamesAsync();


        // Adresse de la liste : les films sortis au cinéma en France (types de sortie 2 et 3) sur les 6 dernières semaines, d'une durée d'au moins 40 minutes.
        // "%7C" est le caractère "|" écrit pour une adresse web : il signifie "ou" pour TMDB.
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        string from = today.AddDays(-InTheatersDays).ToString("yyyy-MM-dd");
        string to = today.AddDays(2).ToString("yyyy-MM-dd");

        string listUrl = "discover/movie?language=fr-FR&region=FR&with_release_type=2%7C3"
            + "&release_date.gte=" + from
            + "&release_date.lte=" + to
            + "&with_runtime.gte=" + MinRuntimeMinutes
            + "&sort_by=popularity.desc";

        // GetStringAsync envoie la requête et renvoie le texte de la réponse (du JSON).
        // Il lève une exception si le serveur répond par une erreur (jeton refusé, pas de réseau, etc..).
        string json = await _http.GetStringAsync(listUrl + "&page=1");

        List<Movie> movies = ParseMovies(json, genreNames);

        // Tri par date de sortie décroissante.
        // return movies.OrderByDescending(m => m.ReleaseDate).ToList();

        // TMDB range la liste par popularité, sur plusieurs pages. On récupère tout, on trie, et on affiche ensuite
        int totalPages = ReadTotalPages(json);

        for (int page = 2; page <= totalPages; page++)
        {
            string pageJson = await _http.GetStringAsync(listUrl + "&page=" + page);
            movies.AddRange(ParseMovies(pageJson, genreNames));
        }

        // Dans le doute si un même film apparait sur deux pages, on ne garde que le premier de chaque id TMDB
        List<Movie> uniqueMovies = new List<Movie>();
        HashSet<int> seenIds = new HashSet<int>();

        foreach (Movie movie in movies)
        {
            // Sans affiche, la carte serait un rectangle gris : on écarte ces films (souvent très confidentiels)
            if (string.IsNullOrEmpty(movie.PosterPath))
            {
                continue;
            }

            // Add renvoie faux si l'id est déjà dans l'ensemble
            if (seenIds.Add(movie.TmdbId))
            {
                uniqueMovies.Add(movie);
            }
        }

        // Un film sorti il y a plus d'un an et de nouveau en salle est une ressortie.
        // On demande à TMDB la date de son retour au cinéma en France, pour le classer à cette date.
        DateOnly oneYearAgo = today.AddYears(-1);
        DateOnly latestDate = today.AddDays(2);

        foreach (Movie movie in uniqueMovies)
        {
            if (movie.ReleaseDate != null && movie.ReleaseDate.Value < oneYearAgo)
            {
                movie.ReReleaseDate = await GetFrenchReReleaseDateAsync(movie.TmdbId, movie.ReleaseDate.Value, latestDate);
            }
        }

        // Tri par date de sortie décroissante
        return uniqueMovies.OrderByDescending(m => m.ReReleaseDate ?? m.ReleaseDate).ToList();
    }

    // Cherche la dernière sortie au cinéma d'un film en France, postérieure à sa première sortie.
    // Renvoie null si TMDB n'en connaît pas.
    private async Task<DateOnly?> GetFrenchReReleaseDateAsync(int tmdbId, DateOnly firstRelease, DateOnly latestDate)
    {
        string json = await _http.GetStringAsync("movie/" + tmdbId + "/release_dates");

        return ParseFrenchReReleaseDate(json, firstRelease, latestDate);
    }

    // Lit, dans la réponse de TMDB, la dernière sortie au cinéma en France après la première sortie du film.
    // Méthode séparée et publique pour pouvoir la tester sans appeler TMDB, comme ParseMovies.
    public DateOnly? ParseFrenchReReleaseDate(string json, DateOnly firstRelease, DateOnly latestDate)
    {
        TmdbReleaseDatesResponse? response = JsonSerializer.Deserialize<TmdbReleaseDatesResponse>(json, _jsonOptions);

        if (response == null)
        {
            return null;
        }

        DateOnly? best = null;

        foreach (TmdbCountryReleases country in response.Results)
        {
            if (country.CountryCode != "FR")
            {
                continue;
            }

            foreach (TmdbReleaseDate release in country.ReleaseDates)
            {
                // On ne garde que les sorties au cinéma (types 2 et 3)
                if (release.Type != 2 && release.Type != 3)
                {
                    continue;
                }

                // "2026-09-23T00:00:00.000Z" : les 10 premiers caractères sont la date
                DateOnly? date = null;

                if (release.ReleaseDate.Length >= 10)
                {
                    date = ParseDate(release.ReleaseDate.Substring(0, 10));
                }

                // Il faut une date après la première sortie, et pas dans le futur
                if (date == null || date.Value <= firstRelease || date.Value > latestDate)
                {
                    continue;
                }

                // On garde la plus récente
                if (best == null || date.Value > best.Value)
                {
                    best = date;
                }
            }
        }

        return best;
    }

    // Vérifie le nb de pages annoncé par TMDB, dans le doute (plafonné à 15 comme défini plus haut) 
    private int ReadTotalPages(string json)
    {
        TmdbMovieListResponse? response = JsonSerializer.Deserialize<TmdbMovieListResponse>(json, _jsonOptions);
        
        if (response == null)
        {
            return 1;
        }
        return Math.Min(response.TotalPages, MaxPages);
    }



    // Récupération de la durée d'un film en minutes. TMDB ne la donne pas donc on la calcule via la fiche du film. Renvoie null si la durée n'est pas connue.
    public async Task<int?> GetRuntimeAsync(int tmdbId)
    {
        // Déjà demandée : on renvoie la valeur gardée en mémoire, sans appel réseau
        if (_runtimes.TryGetValue(tmdbId, out int? knownRuntime))
        {
            return knownRuntime;
        }

        string json = await _http.GetStringAsync("movie/" + tmdbId + "?language=fr-FR");
        TmdbMovieDetailsResponse? response = JsonSerializer.Deserialize<TmdbMovieDetailsResponse>(json, _jsonOptions);

        // TMDB Renvoie 0 si durée inconnue, donc forcer le null
        int? runtime = null;

        if (response != null && response.Runtime != 0)
        {
            runtime = response.Runtime;
        }

        _runtimes[tmdbId] = runtime;
        return runtime;
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