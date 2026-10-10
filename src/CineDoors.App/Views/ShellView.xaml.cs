using System.Printing;
using System.Windows;
using System.Windows.Controls;
using CineDoors.Core.Entities;
using CineDoors.Infrastructure.Services;
using CineDoors.Infrastructure.Tmdb;

namespace CineDoors.App.Views;

// Ecran principal une fois connecté (Navigation à gauche, résultat de l'écran choisi à droite)

public partial class ShellView : UserControl // UserControl désigne une fenêtre imbriquée (fenêtre qui devra s'ouvrir dans une autre)
{
    private readonly AppUser _user;
    private readonly MainWindow _mainWindow;
    private readonly TmdbClient _tmdbClient;
    private readonly MovieService _movieService;

    public ShellView(AppUser user, MainWindow mainWindow, TmdbClient tmdbClient, MovieService movieService)
    {
        InitializeComponent();

        _user = user;
        _mainWindow = mainWindow;
        _tmdbClient = tmdbClient;
        _movieService = movieService;

        // Affiché en bas à gauche (partie compte utilisateur)
        UsernameText.Text = user.Username;
        UserInitialText.Text = user.Username.Substring(0, 1).ToUpper(); // Première lettre du pseudo comme icone temporairement

        // Accueil est sélectionné par défaut après la connexion
        HomeButton.IsChecked = true;
    }

    private void NavButton_Checked(object sender, RoutedEventArgs e) // Permet de naviguer entre les X écrans grâce aux boutons de navigation à gauche. "sender" retient celui qui a été cliqué
    {
        RadioButton button = (RadioButton)sender; // RadioButton est précisé pour que le compilateur puisse le lire l'acter comme et en lire le texte. Evite des erreurs après compilation
        string pageName = (string)button.Content;

        // Accueil : le vrai écran
        if (button == HomeButton)
        {
            PageHost.Content = new HomeView(_tmdbClient, _movieService, _user);
            return;
        }

        // Ecran provisoire # TODO
        TextBlock placeholder = new TextBlock();
        placeholder.Text = pageName + " : bientôt disponible";
        placeholder.FontSize = 22;
        placeholder.HorizontalAlignment = HorizontalAlignment.Center;
        placeholder.VerticalAlignment = VerticalAlignment.Center;

        PageHost.Content = placeholder;
    }

    // Clic sur la pastille ou le pseudo (Affiche le profil)
    private void ProfileButton_Click(object sender, RoutedEventArgs e)
    {
        // Ne concerne pas les boutons de la barre, on les décoche tous pour qu'aucun ne reste affiché en vert comme écran actif
        HomeButton.IsChecked = false;
        SearchButton.IsChecked = false;
        MoviesButton.IsChecked = false;
        SeriesButton.IsChecked = false;
        UpcomingButton.IsChecked = false;

        PageHost.Content = new ProfileView(_movieService, _user);
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e) // Si appui sur btn déconnexion
    {
        _mainWindow.ShowLogin(); // Retour à l'écran de connexion
    }
}