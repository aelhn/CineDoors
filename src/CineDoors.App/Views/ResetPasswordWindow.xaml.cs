using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using CineDoors.Infrastructure.Services;

namespace CineDoors.App.Views;

// Fenêtre d'oubli de mot de passe : uniquement pour test, réinitialisation direct sans mail envoyé #TODO
public partial class ResetPasswordWindow : Window 
{
    private readonly AccountService _accountService;

    // username récupéré depuis l'écran de connexion
    public ResetPasswordWindow(AccountService accountService, string username)
    {
        InitializeComponent();

        _accountService = accountService;
        UsernameBox.Text = username;
    }

    private async void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        MessageText.Text = "";
        ResetButton.IsEnabled = false;

        try
        {
            string? error = await _accountService.ResetPasswordAsync(UsernameBox.Text, NewPasswordBox.Password);

            if (error != null)
            {
                MessageText.Foreground = (Brush)FindResource("ErrorBrush");
                MessageText.Text = error;
                return; 
            }

            // Réussite => Message en vert
            MessageText.Foreground = (Brush)FindResource("AccentTextBrush");
            MessageText.Text = "Mot de passe modifié. Vous pouvez fermer cette fenêtre et vous connecter.";
        }
        catch (Exception)
        {
            MessageText.Foreground = (Brush)FindResource("ErrorBrush");
            MessageText.Text = "Impossible de joindre la base de données. Docker est-il démarré ?";
        }
        finally
        {
            // S'exécute systématiquement (finally)
            ResetButton.IsEnabled = true;
        }
    }

    // --- Textes indicatifs des champs ("Pseudo", "Nouveau mot de passe") ---

    private void UsernameBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdatePlaceholder(UsernamePlaceholder, UsernameBox.Text);
    }

    private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        UpdatePlaceholder(NewPasswordPlaceholder, NewPasswordBox.Password);
    }

    // Affiche l'indication si le champ est vide, la masque dès qu'il contient du texte.
    private void UpdatePlaceholder(TextBlock placeholder, string text)
    {
        if (text.Length == 0)
        {
            placeholder.Visibility = Visibility.Visible;
        }
        else
        {
            placeholder.Visibility = Visibility.Collapsed;
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    // La fenêtre n'a pas de barre de titre : on la déplace en la tenant par la carte.
    private void Card_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }
}