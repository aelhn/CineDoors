using System.ComponentModel;
using CineDoors.Core.Entities;

namespace CineDoors.App.Views;

// Ce qu'un objet "MovieCard" affiche à l'écran (un film = un objet MovieCard).
// Le XAML lit ces propriétés par leur nom : {Binding Title} affiche la propriété Title.
public class MovieCard : INotifyPropertyChanged // Déclaration WPF permettant de surveiller les actions effectuées sur cette classe
{
    public required Movie Movie { get; set; } // Film associé à la "carte" (bloc visuel sur l'écran d'accueil)
    public required string Title { get; set; }

    // Ligne sous le titre, ex. "30 sept. 2026 · Science-Fiction"
    public required string Subtitle { get; set; }

    // Adresse complète de l'affiche. null si le film n'en a pas.
    public string? PosterUrl { get; set; }

    // --- Etat du film pour l'utilisateur connecté
    private bool _isToWatch;
    public bool IsToWatch
    {
        get { return _isToWatch; } 
        set 
        {
            _isToWatch = value;
            OnPropertyChanged(nameof(IsToWatch)); 
        }
    }

    private bool _isWatched;
    public bool IsWatched
    {
        get { return _isWatched; }
        set
        {
            _isWatched = value;
            OnPropertyChanged(nameof(IsWatched));
        }
    }
    // ---

    // Signal envoyé à l'écran quand une propriété change (imposé par INotifyPropertyChanged).
    // WPF le surveille de lui-même : quand il le reçoit, il relit la propriété et met l'affichage à jour (boutons gris à vert sur la carte du film à l'accueil).
    public event PropertyChangedEventHandler? PropertyChanged;

    // Envoie le signal, avec le nom de la propriété qui vient de changer.
    private void OnPropertyChanged(string propertyName)
    {
        // "?." : n'envoie le signal que si l'écran surveille la carte. Sinon PropertyChanged vaut null et il n'y a rien à faire.
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}