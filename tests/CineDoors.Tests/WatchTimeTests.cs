using CineDoors.Core;

namespace CineDoors.Tests;

// Test du découpage d'un nombre de minutes au format jours / heures / minutes
public class WatchTimeTests 
{
    [Fact]
    public void Decoupage_EnJoursHeuresMinutes()
    {
        // 1 jour (1440 mn) + 2 heures (120 mn) + 15mn
        WatchTime time = new WatchTime(1575);

        Assert.Equal(1, time.Days);
        Assert.Equal(2, time.Hours);
        Assert.Equal(15, time.Minutes);
    }

    [Fact]
    public void TempsNegatif_EstRefuse()
    {
        // Test OK si le constructeur lève l'exception liée au temps négatif
        Assert.Throws<ArgumentOutOfRangeException>(() => new WatchTime(-1));
    }
}