namespace CineDoors.Core.Entities;

public class Season
{
    public int Id { get; set; }
    public int SeriesId { get; set; }
    public int Number { get; set; }

    public List<Episode> Episodes { get; set; } = new List<Episode>();
}
