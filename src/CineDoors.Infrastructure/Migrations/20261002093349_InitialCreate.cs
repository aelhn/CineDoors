using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CineDoors.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "app_user",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    username = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_release_check = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_user", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "movie",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tmdb_id = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    release_date = table.Column<DateOnly>(type: "date", nullable: true),
                    runtime_minutes = table.Column<int>(type: "integer", nullable: true),
                    poster_path = table.Column<string>(type: "text", nullable: true),
                    backdrop_path = table.Column<string>(type: "text", nullable: true),
                    genres = table.Column<string>(type: "text", nullable: true),
                    overview = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_movie", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "series",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tmdb_id = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    first_air_date = table.Column<DateOnly>(type: "date", nullable: true),
                    poster_path = table.Column<string>(type: "text", nullable: true),
                    backdrop_path = table.Column<string>(type: "text", nullable: true),
                    genres = table.Column<string>(type: "text", nullable: true),
                    overview = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_series", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "movie_tracking",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    movie_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    watched_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notify = table.Column<bool>(type: "boolean", nullable: false),
                    added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_movie_tracking", x => new { x.user_id, x.movie_id });
                    table.ForeignKey(
                        name: "fk_movie_tracking_app_user_user_id",
                        column: x => x.user_id,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_movie_tracking_movie_movie_id",
                        column: x => x.movie_id,
                        principalTable: "movie",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "season",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    series_id = table.Column<int>(type: "integer", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_season", x => x.id);
                    table.ForeignKey(
                        name: "fk_season_series_series_id",
                        column: x => x.series_id,
                        principalTable: "series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "series_tracking",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    series_id = table.Column<int>(type: "integer", nullable: false),
                    notify = table.Column<bool>(type: "boolean", nullable: false),
                    added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_series_tracking", x => new { x.user_id, x.series_id });
                    table.ForeignKey(
                        name: "fk_series_tracking_app_user_user_id",
                        column: x => x.user_id,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_series_tracking_series_series_id",
                        column: x => x.series_id,
                        principalTable: "series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "episode",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    season_id = table.Column<int>(type: "integer", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    air_date = table.Column<DateOnly>(type: "date", nullable: true),
                    runtime_minutes = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_episode", x => x.id);
                    table.ForeignKey(
                        name: "fk_episode_season_season_id",
                        column: x => x.season_id,
                        principalTable: "season",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "episode_watch",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    episode_id = table.Column<int>(type: "integer", nullable: false),
                    watched_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_episode_watch", x => new { x.user_id, x.episode_id });
                    table.ForeignKey(
                        name: "fk_episode_watch_app_user_user_id",
                        column: x => x.user_id,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_episode_watch_episode_episode_id",
                        column: x => x.episode_id,
                        principalTable: "episode",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_app_user_email",
                table: "app_user",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_app_user_username",
                table: "app_user",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_episode_air_date",
                table: "episode",
                column: "air_date");

            migrationBuilder.CreateIndex(
                name: "ix_episode_season_id_number",
                table: "episode",
                columns: new[] { "season_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_episode_watch_episode_id",
                table: "episode_watch",
                column: "episode_id");

            migrationBuilder.CreateIndex(
                name: "ix_movie_tmdb_id",
                table: "movie",
                column: "tmdb_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_movie_tracking_movie_id",
                table: "movie_tracking",
                column: "movie_id");

            migrationBuilder.CreateIndex(
                name: "ix_movie_tracking_user_id_status",
                table: "movie_tracking",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_season_series_id_number",
                table: "season",
                columns: new[] { "series_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_series_tmdb_id",
                table: "series",
                column: "tmdb_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_series_tracking_series_id",
                table: "series_tracking",
                column: "series_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "episode_watch");

            migrationBuilder.DropTable(
                name: "movie_tracking");

            migrationBuilder.DropTable(
                name: "series_tracking");

            migrationBuilder.DropTable(
                name: "episode");

            migrationBuilder.DropTable(
                name: "movie");

            migrationBuilder.DropTable(
                name: "app_user");

            migrationBuilder.DropTable(
                name: "season");

            migrationBuilder.DropTable(
                name: "series");
        }
    }
}
