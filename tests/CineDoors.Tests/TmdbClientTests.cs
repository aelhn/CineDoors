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

    [Fact]
    public void Ressortie_GardeLaDerniereSortieCinemaEnFrance()
    {
        // Dates de sortie d'un film de 2019, ressorti au cinéma en France en septembre 2026
        string json = """
            {
              "results": [
                {
                  "iso_3166_1": "US",
                  "release_dates": [
                    { "release_date": "2026-10-01T00:00:00.000Z", "type": 3 }
                  ]
                },
                {
                  "iso_3166_1": "FR",
                  "release_dates": [
                    { "release_date": "2019-04-24T00:00:00.000Z", "type": 3 },
                    { "release_date": "2019-08-30T00:00:00.000Z", "type": 5 },
                    { "release_date": "2026-09-23T00:00:00.000Z", "type": 3 },
                    { "release_date": "2026-10-05T00:00:00.000Z", "type": 6 },
                    { "release_date": "2027-01-15T00:00:00.000Z", "type": 3 }
                  ]
                }
              ]
            }
            """;

        TmdbClient client = new TmdbClient(new HttpClient());
        DateOnly firstRelease = new DateOnly(2019, 4, 24);
        DateOnly latestDate = new DateOnly(2026, 10, 14);

        DateOnly? result = client.ParseFrenchReReleaseDate(json, firstRelease, latestDate);

        // Ecartés : la sortie américaine, le DVD (type 5), la télévision (type 6), la date future de 2027,
        // et la première sortie elle-même. Il reste le retour en salle du 23 septembre 2026.
        Assert.Equal(new DateOnly(2026, 9, 23), result);
    }

    [Fact]
    public void Ressortie_NullSiLeFilmNEstSortiQuUneFois()
    {
        string json = """
            {
              "results": [
                {
                  "iso_3166_1": "FR",
                  "release_dates": [
                    { "release_date": "2019-04-24T00:00:00.000Z", "type": 3 }
                  ]
                }
              ]
            }
            """;

        TmdbClient client = new TmdbClient(new HttpClient());

        DateOnly? result = client.ParseFrenchReReleaseDate(json, new DateOnly(2019, 4, 24), new DateOnly(2026, 10, 14));

        Assert.Null(result);
    }
}
