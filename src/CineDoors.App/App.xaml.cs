using System.Windows;
using System.Net.Http;
using System.Net.Http.Headers;
using CineDoors.Infrastructure.Data;
using CineDoors.Infrastructure.Security;
using CineDoors.Infrastructure.Services;
using CineDoors.Infrastructure.Tmdb;

namespace CineDoors.App;

// Point de départ de l'application
public partial class App : Application
{
    // Appelée par WPF au lancement, avant l'affichage de la première fenêtre
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Sans la chaine de connexion, l'application ne peut rien faire, on prévient et on s'arrête proprement plutôt que de planter
        if (Environment.GetEnvironmentVariable("CINEDOORS_DB") == null)
        {
            MessageBox.Show("La variable d'environnement CINEDOORS_DB n'est pas définie.", "Cinedoors");
            Shutdown();
            return;
        }

        // Construction des objets avant de les transmettre aux écrans
        CineDoorsDbContext context = new
        CineDoorsDbContextFactory().CreateDbContext([]);
        AccountService accountService = new AccountService(context, new PasswordHasher());
        MovieService movieService = new MovieService(context);

        // Client HTTP réglé pour TMDB : adresse de l'API, et jeton d'accès envoyé avec chaque requête (Sans jeton, l'application démarre quand même : seul l'accueil affichera une erreur).
        HttpClient http = new HttpClient();
        http.BaseAddress = new Uri("https://api.themoviedb.org/3/");
        string? tmdbToken = Environment.GetEnvironmentVariable("CINEDOORS_TMDB_TOKEN");
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tmdbToken);
        TmdbClient tmdbClient = new TmdbClient(http);

        MainWindow window = new MainWindow(accountService, tmdbClient, movieService);
        
        window.Show();
    }
}