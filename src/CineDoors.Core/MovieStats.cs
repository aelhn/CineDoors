namespace CineDoors.Core;

// Transport de données entre le service et l'écran, chiffres du profil pour les films d'un utilisateur
public class MovieStats
{
    // Nombre de films marqués "vu"
    public int WatchedCount { get; set; }

    // Nombre de films dans "à voir"
    public int ToWatchCount { get; set; }

    // Somme des durées des films vus, en minutes
    public int TotalMinutes { get; set; }

    // Films vus dont la durée est inconnue (ne comptent pas dans le total)
    public int UnknowRuntimeCount { get; set; }
}