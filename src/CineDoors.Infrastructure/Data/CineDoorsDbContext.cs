using CineDoors.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CineDoors.Infrastructure.Data;

// Structure de la bdd
public class CineDoorsDbContext : DbContext
{
    // Constructeur, reçoit les options et les transmet à la classe DbContext
    public CineDoorsDbContext(DbContextOptions<CineDoorsDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> Users { get; set; }
    public DbSet<Movie> Movies { get; set; }
    public DbSet<Series> Series { get; set; }
    public DbSet<Season> Seasons { get; set; }
    public DbSet<Episode> Episodes { get; set; }
    public DbSet<MovieTracking> MovieTrackings { get; set; }
    public DbSet<SeriesTracking> SeriesTrackings { get; set; }
    public DbSet<EpisodeWatch> EpisodeWatches { get; set; }

    // EF Core utilise la suite pour préciser certains éléments (ne devine pas les clés composés, l'unicité, les maxLenght ou les clés étrangères)
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // --- Utilisateur ---   
        modelBuilder.Entity<AppUser>().ToTable("app_user");
        modelBuilder.Entity<AppUser>().Property(u => u.Username).HasMaxLength(30); // "Property(u => u.Username)" Equivaut à "Property("Username")" mais évite des erreurs liées à la frappe et au renommage
        modelBuilder.Entity<AppUser>().Property(u => u.Email).HasMaxLength(254);
        modelBuilder.Entity<AppUser>().HasIndex(u => u.Username).IsUnique(); // Pseudo et e-mail uniques
        modelBuilder.Entity<AppUser>().HasIndex(u => u.Email).IsUnique();

        // --- Catalogue  ---   
        modelBuilder.Entity<Movie>().ToTable("movie");
        modelBuilder.Entity<Movie>().Property(m =>  m.Title).HasMaxLength(300);
        modelBuilder.Entity<Movie>().HasIndex(m => m.TmdbId).IsUnique();         // Un film ne doit être enregistré qu'une seule fois

        modelBuilder.Entity<Series>().ToTable("series");
        modelBuilder.Entity<Series>().Property(s => s.Title).HasMaxLength(300);
        modelBuilder.Entity<Series>().HasIndex(s => s.TmdbId).IsUnique();

        modelBuilder.Entity<Season>().ToTable("season");
        modelBuilder.Entity<Season>().HasIndex(s => new { s.SeriesId, s.Number }).IsUnique(); // Une série n'a qu'une seule saison X, pas deux identiques (pas plusieurs saisons 1, 2, etc..)

        modelBuilder.Entity<Episode>().ToTable("episode");
        modelBuilder.Entity<Episode>().Property(e => e.Title).HasMaxLength(300);
        modelBuilder.Entity<Episode>().HasIndex(e => new { e.SeasonId, e.Number }).IsUnique();
        modelBuilder.Entity<Episode>().HasIndex(e => e.AirDate); // Index pour récupérer rapidement les épisodes par date de diffusion (onglet "à venir")

        // --- Suivi des films ---   
        modelBuilder.Entity<MovieTracking>().ToTable("movie_tracking");
        modelBuilder.Entity<MovieTracking>().HasKey(t => new { t.UserId, t.MovieId }); // Clé primaire composée ; Une ligne par couple utilisateur + film
        modelBuilder.Entity<MovieTracking>().Property(t => t.Status).HasConversion<string>().HasMaxLength(10); // Le enum enregistré en texte, pas en int (0 ou 1)
        modelBuilder.Entity<MovieTracking>().HasIndex(t => new { t.UserId, t.Status });
        modelBuilder.Entity<MovieTracking>().HasOne<AppUser>().WithMany().HasForeignKey(t => t.UserId); // UserId est une clé étrangère qui pointe vers app_user (le préciser à EF Core, car la classe n'a pas de propriété de navigation vers AppUser)

        // --- Suivi des Séries ---   
        modelBuilder.Entity<SeriesTracking>().ToTable("series_tracking");
        modelBuilder.Entity<SeriesTracking>().HasKey(t => new { t.UserId, t.SeriesId });
        modelBuilder.Entity<SeriesTracking>().HasOne<AppUser>().WithMany().HasForeignKey(t => t.UserId);

        // --- Episodes vus ---   
        modelBuilder.Entity<EpisodeWatch>().ToTable("episode_watch");
        modelBuilder.Entity<EpisodeWatch>().HasKey(w => new { w.UserId, w.EpisodeId });
        modelBuilder.Entity<EpisodeWatch>().HasOne<AppUser>().WithMany().HasForeignKey(w => w.UserId);
        modelBuilder.Entity<EpisodeWatch>().HasOne<Episode>().WithMany().HasForeignKey(w => w.EpisodeId);
    }

}
