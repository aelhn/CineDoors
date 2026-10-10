using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using CineDoors.Core;
using CineDoors.Core.Entities;
using CineDoors.Infrastructure.Services;

namespace CineDoors.App.Views;

// Ecran du profil
// Compteur de l'utilisateur connecté
public partial class ProfileView : UserControl
{
    private readonly MovieService _movieService;
    private readonly AppUser _user;

    public ProfileView(MovieService movieService, AppUser user)
    {
        InitializeComponent();

        _movieService = movieService;
        _user = user;

        // En-tête, affiche ce qu'on a déjà dans l'appli
        UsernameText.Text = user.Username;
        InitialText.Text = user.Username.Substring(0, 1).ToUpper();

        // Est enregistré en temps "universel", format changé en fonction du poste en affichage
        MemberSinceText.Text = "Membre depuis le " + user.CreatedAt.ToLocalTime().ToString("d MMMM yyyy", new CultureInfo("fr-FR"));
    }

    // Récup chiffres en base
    private async void ProfileView_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            MovieStats stats = await _movieService.GetStatsAsync(_user.Id);
            WatchTime time = new WatchTime(stats.TotalMinutes);
            WatchTimeText.Text = time.Days + " j " + time.Hours + " h " + time.Minutes + " min";

            // ToString() transforme le nombre en texte, pour pouvoir l'affecter à un champ texte
            WatchedCountText.Text = stats.WatchedCount.ToString();
            ToWatchCountText.Text = stats.ToWatchCount.ToString();

            if (stats.UnknowRuntimeCount >0)
            {
                UnknownRuntimeText.Text = "Durée inconnue pour " + stats.UnknowRuntimeCount + " film(s) vu(s) : non compté(s) dans le temps de visionnage.";
                UnknownRuntimeText.Visibility = Visibility.Visible;
            }

            StatusText.Visibility = Visibility.Collapsed;
            TilesPanel.Visibility = Visibility.Visible;
        }
        catch (Exception)
        {
            StatusText.Text = "Impossible de charger les statistiques. Vérifiez que Docker est démarré.";
        }
    }
}