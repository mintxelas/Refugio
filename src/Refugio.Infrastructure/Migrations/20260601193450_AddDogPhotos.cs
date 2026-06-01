using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Refugio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDogPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DogPhotos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DogId = table.Column<int>(type: "INTEGER", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DogPhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DogPhotos_Dogs_DogId",
                        column: x => x.DogId,
                        principalTable: "Dogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DogPhotos_DogId",
                table: "DogPhotos",
                column: "DogId");

            // Backfill: existing single PhotoUrl becomes each dog's default gallery photo.
            migrationBuilder.Sql(
                "INSERT INTO DogPhotos (DogId, Url, IsDefault, UploadedAt, DeletedAt) " +
                "SELECT Id, PhotoUrl, 1, CURRENT_TIMESTAMP, NULL FROM Dogs " +
                "WHERE PhotoUrl IS NOT NULL AND PhotoUrl <> '' AND DeletedAt IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DogPhotos");
        }
    }
}
