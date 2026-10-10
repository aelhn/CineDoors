using CineDoors.Core.Entities;
using CineDoors.Infrastructure.Data;
using CineDoors.Core;
using Microsoft.EntityFrameworkCore;
using System.Runtime.ConstrainedExecution;

namespace CineDoors.Infrastructure.Services;

// Films suivis (liste des films "à voir" et "vus")
public class MovieService
{
    private readonly CineDoorsDbContext _context;
    public MovieService(CineDoorsDbContext context)
    {
        _context = context;
    }

    // Statut de chaque film suivi par l'utilisateur, renvoie un dictionnaire avec identifiant TMDB du film avec leur statut (à voir/vu)
    public async Task<Dictionary<int, MovieStatus>> GetStatusesAsync(int userId)
    {
        // Fetch dans la base, tous les films que l'utilisateur suit (sous forme "UserId = 1, MovieId = 12, Status = ToWatch" par ligne)
        List<MovieTracking> trackings = await _context.MovieTrackings
            .Include(t => t.Movie) // Associe l'objet "film" ciblé en entier grâce à l'Id de la liste
            .Where(t => t.UserId == userId) // Ne garde que les films de cet utilisateur en particulier
            .ToListAsync(); // Renvoie les infos dans une liste

        Dictionary<int, MovieStatus> statuses = new Dictionary<int, MovieStatus>();


        foreach (MovieTracking tracking in trackings)
        {
            if (tracking.Movie != null)
            {
                statuses[tracking.Movie.TmdbId] = tracking.Status; // Change le statut des films de l'accueil en utilisant le résultat des films suivis. Simplifie le traitement d'affichage  
            }
        }
        return statuses;
    }

    // Donne un statut à un film pour un utilisateur : "à voir" ou "vu". Créé donc le suivi s'il n'existe pas, le modifie sinon (un seul statut par film).
    public async Task SetStatusAsync(int userId, Movie movie, MovieStatus status)
    {
        Movie storedMovie = await GetOrAddMovieAsync(movie);
        
        MovieTracking? tracking = await _context.MovieTrackings.FirstOrDefaultAsync(t => t.UserId == userId && t.MovieId == storedMovie.Id);

        if (tracking == null)
        {
            tracking = new MovieTracking
            {
                UserId = userId,
                MovieId = storedMovie.Id,
                AddedAt = DateTime.UtcNow
            };

            _context.MovieTrackings.Add(tracking);
        }

        tracking.Status = status;

        // La date du visionnage n'existe que pour un film vu
        if (status == MovieStatus.Watched)
        {
            tracking.WatchedAt = DateTime.UtcNow;
        }
        else
        {
            tracking.WatchedAt = null;
        }

        await _context.SaveChangesAsync();
    }

    // Retire un film des listes de l'utilisateur (mais reste dans le catalogue quand même)
    public async Task RemoveAsync(int userId, int tmdbId)
    {
        MovieTracking? tracking = await _context.MovieTrackings.FirstOrDefaultAsync(
            t => t.UserId == userId &&
            t.Movie!.TmdbId == tmdbId); // "!" devant Movie marque le fait que la valeur ne peut pas être nulle (Dans une requête EF Core, t.Movie.TmdbId est traduit en jointure SQL, donc pas d'objet null envisageable)

        if (tracking == null)
        {
            return;
        }

        _context.MovieTrackings.Remove(tracking);
        await _context.SaveChangesAsync();
    }

    // Chiffres du profil : films vus, films à voir et temps total de visionnage
    public async Task<MovieStats> GetStatsAsync(int userId) // Enregistre des infos dans stats. via requêtes SQL
    {
        IQueryable<MovieTracking> trackings = _context.MovieTrackings.Where(t =>  t.UserId == userId);
        IQueryable<MovieTracking> watched = trackings.Where(t =>  t.Status == MovieStatus.Watched);
        MovieStats stats = new MovieStats();

        // On garde dans stats. les résultats de requêtes SQL. Les opérations de calcul sont faites par PostgreSQL 
        stats.WatchedCount = await watched.CountAsync();
        stats.ToWatchCount = await trackings.CountAsync(t => t.Status == MovieStatus.ToWatch); // Equivaut à un count(*)
        stats.TotalMinutes = await watched.SumAsync(t => t.Movie!.RuntimeMinutes ?? 0); // "?? 0" précise que, si la durée est nulle, on la remplace par 0
        stats.UnknowRuntimeCount = await watched.CountAsync(t => t.Movie!.RuntimeMinutes == null); // On compte, parmi les films vus, ceux dont on ne connait pas la durée

        return stats;
    }

    // Le suivi pointe vers un film, donc on enregistre ce film de notre côté s'il est inconnu dans notre base.
    // La référence vers laquelle pointe notre suivi doit juste être connue, donc on ne l'enregistre qu'une seule fois (même si suivis par 50 utilisateurs).

    private async Task<Movie> GetOrAddMovieAsync(Movie movie)
    {
        Movie? storedMovie = await _context.Movies.FirstOrDefaultAsync(m => m.TmdbId == movie.TmdbId);

        if (storedMovie != null)
        {
            return storedMovie;
        }

        _context.Movies.Add(movie);

        await _context.SaveChangesAsync(); // Après l'enregistrement, EF Core remplit movie.Id avec l'identifiant généré par la base.

        return movie;
    }
    // -- Fin TODO
}