namespace CineDoors.Core;

// Temps de visionnage complet, découpé en jours, heures et minutes (gardé en minutes dans la base, petit reformatage nécessaire avant affichage)
// ex : 1500 minutes = 1 jour, 1h et 0mn.
public class WatchTime
{
    private const int MinutesPerHour = 60;
    private const int MinutesPerDay = 24*MinutesPerHour;

    // Juste besoin de récup les infos sans les modifier, pas besoin de "set" avec le "get"
    public int Days { get; }
    public int Hours { get; }
    public int Minutes { get; }

    public WatchTime(int totalMinutes) 
    {
        if (totalMinutes <0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalMinutes), "Un temps de visionnage ne peut pas être négatif.");
        }

        Days = totalMinutes / MinutesPerDay;

        // "%" modulo, donne le reste de la division 
        int minutesLeft = totalMinutes % MinutesPerDay;

        Hours = minutesLeft / MinutesPerHour;
        Minutes = minutesLeft % MinutesPerHour;
    }
}