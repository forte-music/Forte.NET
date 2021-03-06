using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Forte.NET.Database;
using GraphQL.Types;

namespace Forte.NET.Schema {
    [Table("album")]
    public class Album {
        [Column("id", TypeName = "BLOB")]
        public Guid Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = null!;

        [Column("artist_id", TypeName = "BLOB")]
        public Guid ArtistId { get; set; }

        [Column("artwork_path", TypeName = "BLOB")]
        public string? ArtworkPath { get; set; }

        [Column("release_year")]
        public int? ReleaseYear { get; set; }

        [Column("time_added", TypeName = "TIMESTAMP")]
        public TimeWrapper TimeAdded { get; set; }

        [Column("last_played", TypeName = "TIMESTAMP")]
        public TimeWrapper? LastPlayed { get; set; }
    }

    public sealed class AlbumType : ObjectGraphType<Album> {
        public AlbumType() {
            Name = "Album";
            Field(album => album.Id);
            Field(album => album.Name);
            Field(album => album.ReleaseYear, true);
            Field(album => album.TimeAdded);
            Field("stats", album => new UserStats($"stats:{album.Id}", album.LastPlayed),
                type: typeof(NonNullGraphType<UserStatsType>));
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<SongType>>>>(
                "songs",
                resolve: context => {
                    var dbContext = context.ForteDbContext();
                    var album = context.Source;
                    return dbContext.Songs
                        .Where(song => song.AlbumId == album.Id)
                        .OrderBy(song => song.DiskNumber)
                        .ThenBy(song => song.TrackNumber);
                }
            );
            Field<NonNullGraphType<ArtistType>>(
                "artist",
                resolve: context => {
                    var dbContext = context.ForteDbContext();
                    var album = context.Source;
                    return dbContext.Artists.Find(album.ArtistId);
                }
            );
            Field(
                "artworkUrl",
                album => album.ArtworkPath == null ? null : $"/files/artwork/{album.Id}/raw",
                type: typeof(StringGraphType)
            );
            Field<NonNullGraphType<IntGraphType>>("duration", resolve: context => {
                var dbContext = context.ForteDbContext();
                var album = context.Source;
                return dbContext.Songs
                    .Where(song => song.AlbumId == album.Id)
                    .Sum(song => song.Duration);
            });
        }
    }
}
