using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CineDoors.Infrastructure.Data;

// Utilisée par les commandes "dotnef ef" (migrations et maj de base). Ont besoin de créer un CineDoorsDbContext sans lancer systématiquement l'application

public class CineDoorsDbContextFactory : IDesignTimeDbContextFactory<CineDoorsDbContext>
{
    public CineDoorsDbContext CreateDbContext(string[] args)
    {
        // La chaine contient le mdp, on la lit la variable CINEDOORS_DB plutôt qu'en dur dans le code
        string? connectionString = Environment.GetEnvironmentVariable("CINEDOORS_DB");

        if (connectionString == null)
        {
            throw new InvalidOperationException("La variable d'environnement CINEDOORS_DB n'est pas définie.");
        }

        DbContextOptionsBuilder<CineDoorsDbContext> builder = new DbContextOptionsBuilder<CineDoorsDbContext>();
        builder.UseNpgsql(connectionString);
        builder.UseSnakeCaseNamingConvention(); // Convertit les noms C# en noms SQL (runtimeMinutes > runtime_minutes)

        return new CineDoorsDbContext(builder.Options);
    }
}