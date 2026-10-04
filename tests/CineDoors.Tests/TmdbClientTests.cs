using CineDoors.Core.Entities;
using CineDoors.Infrastructure.Tmdb;

namespace CineDoors.Tests;

// Test de la lecture des réponses de TMDB, sans appel réseau : le JSON est écrit en dur.
public class TmdbClientTests
{
    [Fact]
    public void ParseMovies_ConvertitLeJsonEnFilms()
    {
        // Extrait d'une réponse de TMDB. Le 2e film n'a ni date, ni affiche, ni genre connu.
        // Les trois guillemets (""") ouvrent un texte sur plusieurs lignes, guillemets compris.
        string json = """
            {
              "results": [
                {
                  "id": 101,
                  "title": "Orbite silencieuse",
                  "release_date": "2026-09-30",
                  "poster_path": "/affiche.jpg",
                  "backdrop_path": "/fond.jpg",
                  "overview": "Un résumé.",
                  "genre_ids": [878, 28]
                },
                {
                  "id": 102,
                  "title": "Film sans infos",
                  "release_date": "",
                  "poster_path": null,
                  "genre_ids": [999]
                }
              ]
            }
            """;

        Dictionary<int, string> genreNames = new Dictionary<int, string>();
        genreNames[28] = "Action";
        genreNames[878] = "Science-Fiction";

        TmdbClient client = new TmdbClient(new HttpClient());
        List<Movie> movies = client.ParseMovies(json, genreNames);

        Assert.Equal(2, movies.Count);

        // 1er film : tout est renseigné
        Assert.Equal(101, movies[0].TmdbId);
        Assert.Equal("Orbite silencieuse", movies[0].Title);
        Assert.Equal(new DateOnly(2026, 9, 30), movies[0].ReleaseDate);
        Assert.Equal("/affiche.jpg", movies[0].PosterPath);
        Assert.Equal("Science-Fiction, Action", movies[0].Genres);

        // 2e film : les infos absentes donnent null, sans faire planter la lecture
        Assert.Null(movies[1].ReleaseDate);
        Assert.Null(movies[1].PosterPath);
        Assert.Null(movies[1].Genres);
    }
}