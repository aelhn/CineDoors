using CineDoors.App.Views;
using CineDoors.Core.Entities;
using CineDoors.Infrastructure.Services;
using CineDoors.Infrastructure.Tmdb;
using System.Windows;
using System.Windows.Controls;

namespace CineDoors.App;

// Code associé à la fenêtre principale (MainWindow.xaml).
public partial class MainWindow : Window
{
    private readonly AccountService _accountService;
    private readonly TmdbClient _tmdbClient;
    public MainWindow(AccountService accountService, TmdbClient tmdbClient)
    {
        InitializeComponent();

        _accountService = accountService;
        _tmdbClient = tmdbClient;

        ShowLogin(); // Premier écran affiché, connexion
    }

    // --- Navigation : Quel écran est affiché dans la fenêtre ---
    public void ShowLogin()
    {
        // Pour afficher ou modifier un écran, on affecte à ScreenHost.Content (comme ScreenHost.Content = new LoginView()
        // this est la fenêtre elle-même
        ScreenHost.Content = new LoginView(_accountService, this);
    }

    public void ShowHome(AppUser user) // Ecran principal
    {
        ScreenHost.Content = new ShellView(user, this, _tmdbClient);
    }


    // --- Les trois boutons de la barre de titre ---
    // Chaque méthode est reliée à son bouton dans le XAML par l'attribut Click="...".

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        // Le même bouton agrandit la fenêtre ou la ramène à sa taille normale.
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
        }
        else
        {
            WindowState = WindowState.Maximized;
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    // Appelée par WPF à chaque changement d'état de la fenêtre (agrandie, normale, réduite).
    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            // Une fenêtre agrandie sans barre de titre Windows dépasse de l'écran de 8 pixels
            // de chaque côté : on compense alors par une marge pour que rien ne soit coupé.
            RootGrid.Margin = new Thickness(8);
            // Icône "restaurer" (deux carrés superposés).
            MaximizeButton.Content = "\uE923";
        }
        else
        {
            RootGrid.Margin = new Thickness(0);
            // Icône "agrandir" (un carré).
            MaximizeButton.Content = "\uE922";
        }
    }
}