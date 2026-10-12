using CineDoors.App.Views;
using CineDoors.Core.Entities;
using CineDoors.Infrastructure.Services;
using CineDoors.Infrastructure.Tmdb;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;

namespace CineDoors.App.Views;

// Ecran d'accueil : la grille des dernières sorties de films
public partial class HomeView : UserControl
{

    private readonly TmdbClient _tmdbClient;
    private readonly MovieService _movieService;
    private readonly AppUser _user ;

    private bool _isBusy = false; // Vrai pendant qu'un clic sur une carte est en cours

    private const int BatchSize = 20; // Nb de cartes de film/série ajouté à chaque fois que l'on arrive en bas de la page

    // Tous les films en salle, triés par date. Chargés une fois, puis affichés par batch
    private List<MovieCard> _allCards = new List<MovieCard>();

    // Cartes réellement affichées.
    // "ObservableCollection" est une liste qui prévient l'écran quand on lui ajoute un élément, pour qu'elle apparaisse seule plutôt que de tout recréer
    private readonly ObservableCollection<MovieCard> _visibleCards = new ObservableCollection<MovieCard>();

    // Vrai une fois les films chargés. Vu que écran gardé en mémoire pendant la session, ce bool empêche de tout recharger à chaque retour sur l'accueil (permet de savoir si les films ont déjà été chargés une fois)
    private bool _isLoaded = false;

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
        // Loaded se déclenche à chaque fois que l'écran revient à l'affichage, pour qu'on ne charge qu'une seule fois
        if (_isLoaded)
        {
            return;
        }
        _isLoaded = true;

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

            _allCards = cards;

            // On donne, à la grille, la liste des cartes affichées (vide pour l'instant), puis on y ajoute le premier batch
            MoviesList.ItemsSource = _visibleCards;
            StatusText.Visibility = Visibility.Collapsed;

            await ShowNextBatchAsync();
        }
        catch (Exception)
        {
            StatusText.Text = "Impossible de charger les films. Vérifiez la connexion Internet et le jeton TMDB (variable CINEDOORS_TMDB_TOKEN).";
            // Chargement échoué, donc on réessaiera au prochain affichage de l'écran d'accueil
            _isLoaded = false;
        }
    }

    // Affiche les 20 films suivants (ou le reste si moins), puis récup leur durée en même temps
    private async Task ShowNextBatchAsync()
    {
        // Calcul de où commence et où finit la tranche de films à afficher (0 à 19, 20 à 39, etc...).
        int start = _visibleCards.Count;
        int end = Math.Min(start + BatchSize, _allCards.Count);

        List<MovieCard> batch = new List<MovieCard>();

        for (int i = start; i < end; i++)
        {
            batch.Add(_allCards[i]);
            _visibleCards.Add(_allCards[i]);
        }

        // La grille est affichée : On complète ensuite les cartes avec la durée de chaque film
        await LoadRuntimesAsync(batch);
    }

    // Appelé par WPF à chaque mouvement de page (molette/barre défilement/changement de taille)
    private async void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        // Si tout est déjà affiché, ou rien n'est encore chargé : ne rien faire
        if (_visibleCards.Count >= _allCards.Count)
        {
            return;
        }

        // "VerticalOffset" indique ce qui est caché au-dessus
        // "ViewportHeight" indique ce qui est visible
        // "ExtentHeight" indique la hauteur total. Quand il reste moins de 300 pixels avant le bas, on charge la suite
        double remaining = e.ExtentHeight - (e.VerticalOffset + e.ViewportHeight);

        if (remaining < 300)
        {
            await ShowNextBatchAsync();
        }
    }

    // Demande à TMDB la durée de chaque film, l'un après l'autre, et met à jour sa carte.
    // TMDB ne donne pas la durée dans la liste des sorties : il faut une demande par film.
    private async Task LoadRuntimesAsync(List<MovieCard> cards)
    {
        try
        {
            foreach (MovieCard card in cards)
            {
                if (card.Movie.RuntimeMinutes == null)
                {
                    card.Movie.RuntimeMinutes = await _tmdbClient.GetRuntimeAsync(card.Movie.TmdbId);
                }

                // Changer Subtitle suffit : la carte prévient l'écran, qui réaffiche la ligne
                card.Subtitle = BuildSubtitle(card.Movie);
            }
        }
        catch (Exception)
        {
            // La durée est un plus : si TMDB ne répond pas, les cartes restent affichées sans elle
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
            string dateText = movie.ReleaseDate.Value.ToString("d MMM yyyy", new CultureInfo("fr-FR"));

            // On affiche la date de la première sortie. Pour une ressortie, le film est classé
            // à sa date de retour en salle : l'astérisque explique pourquoi la date affichée paraît ancienne.
            if (movie.ReReleaseDate != null)
            {
                dateText = dateText + " *";
            }

            parts.Add(dateText);
        }

        if (movie.RuntimeMinutes != null)
        {
            // 112 minutes -> "1 h 52". "D2" écrit le nombre sur 2 chiffres : 5 devient "05".
            int hours = movie.RuntimeMinutes.Value / 60;
            int minutes = movie.RuntimeMinutes.Value % 60;
            parts.Add(hours + " h " + minutes.ToString("D2"));
        }

        if (movie.Genres != null)
        {
            // Genres vaut "Science-Fiction, Action" : ne garde que le premier par manque de place.
            parts.Add(movie.Genres.Split(", ")[0]);
        }

        return string.Join(" · ", parts);
    }
}