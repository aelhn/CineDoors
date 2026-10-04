using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CineDoors.Core.Entities;
using CineDoors.Infrastructure.Services;

namespace CineDoors.App.Views;

// Code associé à l'écran de connexion (LoginView.xaml).
// "partial" : la classe est écrite en deux morceaux, ce fichier et celui généré à partir du XAML.
public partial class LoginView : UserControl
{

    private readonly AccountService _accountService;
    private readonly MainWindow _mainWindow;
    private bool _isRegisterMode = false; // Si faux, l'écran sert à se connecter. Si Vrai, sert à créer un compte
    public LoginView(AccountService accountService, MainWindow mainWindow) 
    {
        // Construit les contrôles décrits dans le fichier XAML.
        InitializeComponent();

        _accountService = accountService;
        _mainWindow = mainWindow;

        LoadPosterWall();
    }


    // Clic sur btn vert (ou "Entrée") > Connexion/Création de compte
    // "async void" et pas "async task" parce que WPF nous l'impose pour les gestionnaires de clic. On ne peut donc pas attraper d'erreurs de l'extérieur
    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = "";
        // Désactivation du btn en attendant la fin du traitement pour éviter un double clic
        LoginButton.IsEnabled = false;

        try
        {
            if (_isRegisterMode)
            {
                await RegisterAsync();
            }
            else
            {
                await LoginAsync();
            }
        }
        catch (Exception)
        {
            // Erreur si Docker ne répond pas (ou tout autre problème avec la bdd au global)
            ErrorText.Text = "Impossible de joindre la base de données. Docker est-il démarré ?";
        }
        finally
        {
            // "finally" s'exécute dans tous les cas, erreur ou non, réactive systématiquement le bouton
            LoginButton.IsEnabled = true;
        }
    }

    private async Task LoginAsync()
    {
        AppUser? user = await _accountService.LoginAsync(UsernameBox.Text, GetPassword());
        
        if (user == null)
        {
            // Sécurité : Ne pas donner d'indice sur l'élément incorrect
            ErrorText.Text = "Pseudo ou mot de passe incorrect.";
            return;
        }

        _mainWindow.ShowHome(user);
    }

    private async Task RegisterAsync()
    {
        string? error = await _accountService.RegisterAsync(UsernameBox.Text, EmailBox.Text, GetPassword());
        if (error != null)
        {
            ErrorText.Text = error;
            return;
        }

        // Compte créé et connexion en suivant
        await LoginAsync();
    }

    // Quand clic sur le lien en bas de l'écran, bascule entre "connexion" et "création"
    private void CreateAccountLink_Click(object sender, RoutedEventArgs e)
    {
        _isRegisterMode = !_isRegisterMode;
        ErrorText.Text = "";

        if (_isRegisterMode)
        {
            TitleText.Text = "CRÉER UN COMPTE";
            EmailFrame.Visibility = Visibility.Visible;
            OptionsRow.Visibility = Visibility.Collapsed;
            LoginButton.Content = "Créer mon compte";
            SwitchModeText.Text = "Déjà un compte ? ";
            CreateAccountLink.Content = "Se connecter";
        }
        else
        {
            TitleText.Text = "CONNEXION";
            EmailFrame.Visibility = Visibility.Collapsed;
            OptionsRow.Visibility = Visibility.Visible;
            LoginButton.Content = "Se connecter";
            SwitchModeText.Text = "Pas encore de compte ? ";
            CreateAccountLink.Content = "Créer un compte";
        }
    }



    // Textes indicatifs pour préciser la type de donnée attendue (Pseudo/Email/Mot de passe)
    // On vérifie quand le champ est rempli, s'il est vide, on affiche le texte indicatif
    private void UsernameBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdatePlaceholder(UsernamePlaceholder, UsernameBox.Text);
    }

    private void EmailBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdatePlaceholder(EmailPlaceholder, EmailBox.Text);
    }

    // Le mdp a deux champs superposés (masqué et en clair) pour une seule indication.
    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        UpdatePlaceholder(PasswordPlaceholder, PasswordBox.Password);
    }

    private void PasswordVisibleBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdatePlaceholder(PasswordPlaceholder, PasswordVisibleBox.Text);
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




    // Clic sur "Mot de passe oublié ?" : ouvre la fenêtre de réinitialisation
    private void ForgotPasswordLink_Click(object sender, RoutedEventArgs e)
    {
        ResetPasswordWindow window = new ResetPasswordWindow(_accountService, UsernameBox.Text);
        // Owner : la fenêtre s'ouvre centrée sur la fenêtre principale et reste au-dessus d'elle.
        window.Owner = _mainWindow;
        // ShowDialog bloque la fenêtre principale tant que celle-ci n'est pas fermée.
        window.ShowDialog();
    }


    // Clic sur l'œil : affiche le mot de passe en clair, ou le masque à nouveau.
    // Un PasswordBox ne sait pas afficher son contenu : on bascule donc entre deux champs
    // superposés, le PasswordBox (masqué) et un TextBox ordinaire (en clair).
    private void TogglePasswordButton_Click(object sender, RoutedEventArgs e)
    {
        if (PasswordBox.Visibility == Visibility.Visible)
        {
            // --- Passage en clair ---
            // On recopie la saisie dans le champ en clair, puis on échange les deux champs.
            PasswordVisibleBox.Text = PasswordBox.Password;
            PasswordBox.Visibility = Visibility.Collapsed;
            PasswordVisibleBox.Visibility = Visibility.Visible;

            // L'œil passe en vert pour signaler que le mot de passe est visible.
            // FindResource va chercher la couleur du thème déclarée dans App.xaml.
            TogglePasswordButton.Foreground = (Brush)FindResource("AccentTextBrush");

            // On remet le curseur dans le champ, à la fin du texte.
            PasswordVisibleBox.Focus();
            PasswordVisibleBox.CaretIndex = PasswordVisibleBox.Text.Length;
        }
        else
        {
            // --- Retour au masquage ---
            PasswordBox.Password = PasswordVisibleBox.Text;
            PasswordVisibleBox.Visibility = Visibility.Collapsed;
            PasswordBox.Visibility = Visibility.Visible;

            // ClearValue retire la couleur posée ci-dessus : l'œil reprend celle de son style.
            TogglePasswordButton.ClearValue(ForegroundProperty);

            PasswordBox.Focus();
        }
    }

    // Renvoie le mot de passe saisi, quel que soit le champ affiché.
    private string GetPassword()
    {
        if (PasswordBox.Visibility == Visibility.Visible)
        {
            return PasswordBox.Password;
        }

        return PasswordVisibleBox.Text;
    }

    // Remplit le fond de l'écran avec les affiches du dossier "Posters", placé à côté de l'application.
    private void LoadPosterWall()
    {
        // AppContext.BaseDirectory est le dossier où se trouve le .exe.
        string folder = Path.Combine(AppContext.BaseDirectory, "Posters");

        if (!Directory.Exists(folder))
        {
            return;
        }

        // --- Liste des fichiers image du dossier ---
        List<string> files = new List<string>();

        foreach (string file in Directory.GetFiles(folder))
        {
            // On ne garde que les images, au cas où le dossier contiendrait autre chose.
            string extension = Path.GetExtension(file).ToLower();
            if (extension == ".jpg" || extension == ".jpeg" || extension == ".png")
            {
                files.Add(file);
            }
        }

        if (files.Count == 0)
        {
            return;
        }

        // --- Mélange ---
        // L'ordre change à chaque ouverture de l'écran : le fond n'est jamais deux fois le même.
        string[] shuffledFiles = files.ToArray();
        Random.Shared.Shuffle(shuffledFiles);

        // PosterWall est la grille déclarée dans le XAML (x:Name="PosterWall").
        int cellCount = PosterWall.Rows * PosterWall.Columns;

        // On ne charge pas plus d'images qu'il n'y a de cases :
        // avec 200 affiches dans le dossier, seules les 28 premières du mélange sont lues.
        int posterCount = Math.Min(shuffledFiles.Length, cellCount);

        // --- Chargement des images retenues ---
        List<BitmapImage> posters = new List<BitmapImage>();

        for (int i = 0; i < posterCount; i++)
        {
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(shuffledFiles[i]);
            // L'image est réduite à 300 pixels de large dès le chargement : inutile de garder
            // en mémoire une affiche en pleine résolution pour un fond assombri.
            bitmap.DecodePixelWidth = 300;
            bitmap.EndInit();

            posters.Add(bitmap);
        }

        // --- Remplissage de la grille ---
        for (int i = 0; i < cellCount; i++)
        {
            Image image = new Image();
            // Le modulo (%) fait reboucler sur la liste : s'il y a moins d'affiches que de cases,
            // on reprend au début après la dernière.
            image.Source = posters[i % posters.Count];
            // L'image remplit toute sa case, quitte à être un peu rognée.
            image.Stretch = Stretch.UniformToFill;
            image.Margin = new Thickness(8);

            PosterWall.Children.Add(image);
        }
    }
}