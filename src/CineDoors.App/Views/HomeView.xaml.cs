using CineDoors.App.Views;
using CineDoors.Core.Entities;
using CineDoors.Infrastructure.Tmdb;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace CineDoors.App.Views;

// Ecran d'accueil : la grille des dernières sorties de films
public partial class HomeView : UserControl
{
    private readonly TmdbClient _tmdbClient;

    public HomeView(TmdbClient tmdbClient)
    {
        InitializeComponent();

        _tmdbClient = tmdbClient;
    }

    // Appelée par WPF quand l'écran vient d'être affiché (Loaded="..." dans le XAML).
    // On charge les films ici et non dans le constructeur : un constructeur ne peut pas utiliser "await" (charger des films est un appel réseau, on serait alors obligé de freeze l'application entière le temps de).
    private async void HomeView_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            List<Movie> movies = await _tmdbClient.GetNowPlayingAsync();

            // On prépare une carte par film.
            List<MovieCard> cards = new List<MovieCard>();

            foreach (Movie movie in movies)
            {
                MovieCard card = new MovieCard
                {
                    Title = movie.Title,
                    Subtitle = BuildSubtitle(movie),
                    PosterUrl = TmdbClient.GetPosterUrl(movie.PosterPath)
                };

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