namespace CineDoors.App.Views;

// Ce qu'un objet "MovieCard" affiche à l'écran (un film = un objet MovieCard).
// Le XAML lit ces propriétés par leur nom : {Binding Title} affiche la propriété Title.
public class MovieCard
{
    public required string Title { get; set; }

    // Ligne sous le titre, ex. "30 sept. 2026 · Science-Fiction"
    public required string Subtitle { get; set; }

    // Adresse complète de l'affiche. null si le film n'en a pas.
    public string? PosterUrl { get; set; }
}