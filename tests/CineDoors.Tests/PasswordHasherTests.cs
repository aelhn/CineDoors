using CineDoors.Infrastructure.Security;

namespace CineDoors.Tests;

// Test de PasswordHasher

// Chaque méthode marquée [Fact] est considérée comme un test à effectuer par xUnit

public class PasswordHasherTests
{
    [Fact]
    public void BonMotDePasse_EstAccepte()
    {
        PasswordHasher hasher = new PasswordHasher();
        string storedHash = hasher.Hash("azerty123");

        bool result = hasher.Verify("azerty123", storedHash);
        Assert.True(result);
    }

    [Fact]
    public void MauvaisMotDePasse_EstRefuse()
    {
        PasswordHasher hasher = new PasswordHasher();
        string storedHash = hasher.Hash("azerty123");

        bool result = hasher.Verify("azerty124", storedHash);

        Assert.False(result);
    }

    [Fact]
    public void MemeMotDePasse_DonneDeuxEmpreintesDifferentes()
    {
        PasswordHasher hasher = new PasswordHasher();

        string first = hasher.Hash("azerty123");
        string second = hasher.Hash("azerty123");

        Assert.NotEqual(first, second);
    }
}