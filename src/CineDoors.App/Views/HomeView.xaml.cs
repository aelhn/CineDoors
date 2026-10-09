using CineDoors.App.Views;
using CineDoors.Core.Entities;
using CineDoors.Infrastructure.Services;
using CineDoors.Infrastructure.Tmdb;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace CineDoors.App.Views;

// Ecran d'accueil : la grille des dernières sorties de films
public partial class HomeView : UserControl
{

    private readonly TmdbClient _tmdbClient;
    private readonly MovieService _movieService;
    private readonly AppUser _user ;

    private bool _isBusy = false; // Vrai pendant qu'un clic sur une carte est en cours

    public HomeView(TmdbClient tmdbClient, MovieService movieService, AppUser user)
    {
        InitializeComponent();

        _tmdbClient = tmdbClient;
        _movieService = movieService;
        _user = user;
    }


    // Appelée par WPF quand l'écran vient d'être affiché (Loaded="..." dans le XAML).
    // On charge les films ici et non dans le constructeur : un constructeur ne peut pas utiliser "await" (charger des films est un appel réseau, on serait alors obligé de freeze l'application entière le temps de).
    private async void HomeView_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            List<Movie> movies = await _tmdbClient.GetNowPlayingAsync();

            // Films déjà suivis par l'utilisateur, pour afficher les boutons dans le bon état
            Dictionary<int, MovieStatus> statuses = await _movieService.GetStatusesAsync(_user.Id);

            // On prépare une carte par film.
            List<MovieCard> cards = new List<MovieCard>();

            foreach (Movie movie in movies)
            {
                MovieCard card = new MovieCard
                {
                    Movie = movie,
                    Title = movie.Title,
                    Subtitle = BuildSubtitle(movie),
                    PosterUrl = TmdbClient.GetPosterUrl(movie.PosterPath)
                };

                // TryGetValue : vrai si le film est dans le dictionnaire, et son statut est rangé dans "status"
                if (statuses.TryGetValue(movie.TmdbId, out MovieStatus status))
                {
                    card.IsToWatch = status == MovieStatus.ToWatch;
                    card.IsWatched = status == MovieStatus.Watched;
                }

                cards.Add(card);
            }

            // On donne la liste à la grille : WPF crée une carte à l'écran pour chaque élément.
            MoviesList.ItemsSource = cards;
            StatusText.Visibility = Visibility.Collapsed;
        }
        catch (Exception)
        {
            StatusText.Text = "Impossible de charger les films. Vérifiez la connexion Internet et le jeton TMDB (variable CINEDOORS_TMDB_TOKEN).";
        }
    }

    // --- Boutons sur les cartes de films (les blocs composant la grille de film à l'accueil)
    // Bouton "+" pour ajouter à la liste des films à voir. L'enlève si y est déjà
    private async void ToWatchButton_Click(object sender, RoutedEventArgs e)
    {
        await ToggleStatusAsync(sender, MovieStatus.ToWatch);
    }

    // Bouton "V" pour ajouter à la liste des films vus. L'enlève si y est déjà
    private async void WatchedButton_Click(object sender, RoutedEventArgs e)
    {
        await ToggleStatusAsync(sender, MovieStatus.Watched);
    }

    // Traitement commun aux 2 boutons. "status" est le statut demandé par le bouton cliqué
    private async Task ToggleStatusAsync(object sender, MovieStatus status)
    {
        // Un seul traitement à la fois (effectuer X opérations simultannées est refusé par la base)
        if (_isBusy)
        {
            return;
        }

        _isBusy = true;

        try
        {
            // DataContext = objet lié au bouton qu'on vient de cliquer (chaque carte est liée à son objet MovieCard). 
            // On récupère donc le film en question ciblé par le traitement
            Button button = (Button)sender;
            MovieCard card = (MovieCard)button.DataContext;

            bool alreadySet = (status == MovieStatus.ToWatch && card.IsToWatch)
                           || (status == MovieStatus.Watched && card.IsWatched);
            if (alreadySet)
            {
                // Si bouton déjà vert, donc à enlever
                await _movieService.RemoveAsync(_user.Id, card.Movie.TmdbId);
                card.IsToWatch = false;
                card.IsWatched = false;
            }
            else
            {
                // Durée pour le compteur. Récup la première fois qu'on l'ajoute
                if (card.Movie.RuntimeMinutes == null)
                {
                    card.Movie.RuntimeMinutes = await _tmdbClient.GetRuntimeAsync(card.Movie.TmdbId);
                }

                await _movieService.SetStatusAsync(_user.Id, card.Movie, status);

                // Un film n'a qu'un seul statut quand suivi : soit "à voir", soit "vu"
                card.IsToWatch = status == MovieStatus.ToWatch;
                card.IsWatched = status == MovieStatus.Watched;
            }
        }

        catch (Exception)
        {
            StatusText.Text = "L'enregistrement a échoué. Vérifiez que Docker est démarré et la connexion Internet fonctionnelle.";
            StatusText.Visibility = Visibility.Visible;
        }
        finally
        {
            _isBusy = false;
        }      
    }
    
    // ---

    // Ligne sous le titre : "30 sept. 2026 · Science-Fiction". Chaque partie est facultative.
    private string BuildSubtitle(Movie movie)
    {
        List<string> parts = new List<string>();

        if (movie.ReleaseDate != null)
        {
            // "d MMM yyyy" : jour, mois abrégé, année. Préciser "fr-FR" donne le mois en format français.
            // ".Value" lit la date contenue dans le DateOnly? (on vient de vérifier qu'elle n'est pas null).
            parts.Add(movie.ReleaseDate.Value.ToString("d MMM yyyy", new CultureInfo("fr-FR")));
        }

        if (movie.Genres != null)
        {
            // Genres vaut "Science-Fiction, Action" : ne garde que le premier par manque de place.
            parts.Add(movie.Genres.Split(", ")[0]);
        }

        return string.Join(" · ", parts);
    }
}