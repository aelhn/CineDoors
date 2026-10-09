using CineDoors.Core.Entities;
using CineDoors.Infrastructure.Data;
using CineDoors.Infrastructure.Security;
using CineDoors.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace CineDoors.Tests;

// Test d'intégration du suivi des films (utilise PostgreSQL, donc Docker doit être lancé)
public class MovieServiceTests
{
    [Fact]
    public async Task AVoir_PuisVu_PuisRetire()
    {
        CineDoorsDbContext context = new CineDoorsDbContextFactory().CreateDbContext([]);
        AccountService accountService = new AccountService(context, new PasswordHasher());
        MovieService movieService = new MovieService(context);

        // --- Préparation : un utilisateur et un film de test ---
        string username = "test_" + Guid.NewGuid().ToString("N").Substring(0, 12);
        await accountService.RegisterAsync(username, username + "@exemple.fr", "motdepasse123");
        AppUser? user = await accountService.LoginAsync(username, "motdepasse123");
        Assert.NotNull(user);

        // Identifiant TMDB négatif : aucun vrai film n'en a, donc pas de conflit avec les données réelles.
        int tmdbId = -Random.Shared.Next(1, 1000000);
        Movie movie = new Movie { TmdbId = tmdbId, Title = "Film de test", RuntimeMinutes = 100 };

        // --- Ajout à la liste "à voir" ---
        await movieService.SetStatusAsync(user.Id, movie, MovieStatus.ToWatch);
        Dictionary<int, MovieStatus> statuses = await movieService.GetStatusesAsync(user.Id);
        Assert.Equal(MovieStatus.ToWatch, statuses[tmdbId]);

        // --- Passage à "vu" : le statut change, sans créer une 2e ligne ---
        await movieService.SetStatusAsync(user.Id, movie, MovieStatus.Watched);
        statuses = await movieService.GetStatusesAsync(user.Id);
        Assert.Equal(MovieStatus.Watched, statuses[tmdbId]);
        Assert.Single(statuses);

        // Le film n'est enregistré qu'une seule fois dans le catalogue
        int movieCount = await context.Movies.CountAsync(m => m.TmdbId == tmdbId);
        Assert.Equal(1, movieCount);

        // --- Retrait ---
        await movieService.RemoveAsync(user.Id, tmdbId);
        statuses = await movieService.GetStatusesAsync(user.Id);
        Assert.Empty(statuses);

        // --- Nettoyage ---
        context.Movies.Remove(movie);
        context.Users.Remove(user);
        await context.SaveChangesAsync();
    }
}