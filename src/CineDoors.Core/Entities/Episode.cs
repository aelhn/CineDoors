namespace CineDoors.Core.Entities;

 // Episode de saison
public class Episode
{
    public int Id { get; set; }

    // Clé étrangère de la saison
    public int SeasonId { get; set; }
    public int Number { get; set; }
    public string? Title { get; set; }

    public DateOnly? AirDate { get; set; } // Date de diffusion
    public int? RuntimeMinutes { get; set; } // Durée en minute 
}
