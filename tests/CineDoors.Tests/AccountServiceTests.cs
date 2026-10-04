using CineDoors.Core.Entities;
using CineDoors.Infrastructure.Data;
using CineDoors.Infrastructure.Security;
using CineDoors.Infrastructure.Services;

namespace CineDoors.Tests;

// Tests d'intégration (utilise postgreSQL, donc Docker doit être lancé)
public class AccountServiceTests
{
    // Créé un service, comme le ferait l'application. Regroupe les traitements inscription et connexion
    private AccountService CreateService(CineDoorsDbContext context)
    {
        return new AccountService(context, new PasswordHasher());
    }

    [Fact] // Fact = indique à xUnit la méthode comme à tester
    public async Task Inscription_PuisConnexion()
    {
        CineDoorsDbContext context = new CineDoorsDbContextFactory().CreateDbContext([]);
        AccountService service = CreateService(context);

        string username = "test_" + Guid.NewGuid().ToString("N").Substring(0,12); // Guid.NewGuid() créé un identifiant random (unique à chaque lancement)
        string email = username + "@exemple.fr";

        // Inscription
        string? error = await service.RegisterAsync(username, email, "motdepasse123");
        Assert.Null(error);

        // Test : Connexion avec le bon mdp
        AppUser? user = await service.LoginAsync(username, "motdepasse123");
        Assert.NotNull(user);

        // Test : Connexion avec un mauvais mdp
        AppUser? wrong = await service.LoginAsync(username, "mauvais-mot-de-passe");
        Assert.Null(wrong);

        // Test : Second compte avec le même pseudo (doit être refusé)
        string? duplicate = await service.RegisterAsync(username, "autre_" + email, "motdepasse123");
        Assert.NotNull(duplicate);

        // Suppression du compte de test
        context.Users.Remove(user);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task MotDePasseTropCourt_EstRefuse()
    {
        CineDoorsDbContext context = new CineDoorsDbContextFactory().CreateDbContext([]);
        AccountService service = CreateService(context);

        string? error = await service.RegisterAsync("quelquun", "quelquun@exemple.fr", "court");

        Assert.NotNull(error);
    }

    [Fact]
    public async Task Reinitialisation_ChangeLeMotDePasse()
    {
        CineDoorsDbContext context = new CineDoorsDbContextFactory().CreateDbContext([]);
        AccountService service = CreateService(context);

        string username = "test_" + Guid.NewGuid().ToString("N").Substring(0, 12);
        await service.RegisterAsync(username, username + "@exemple.fr", "ancien-mdp-123");

        // Réinitialisation
        string? error = await service.ResetPasswordAsync(username, "nouveau-mdp-456");
        Assert.Null(error);

        // Test : l'ancien mdp ne fonctionne plus
        AppUser? withOld = await service.LoginAsync(username, "ancien-mdp-123");
        Assert.Null(withOld);

        // Test : le nouveau fonctionne
        AppUser? withNew = await service.LoginAsync(username, "nouveau-mdp-456");
        Assert.NotNull(withNew);

        // Suppression du compte de test
        context.Users.Remove(withNew);
        await context.SaveChangesAsync();
    }
}