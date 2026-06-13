using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Refugio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdoptionPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdoptionPhotos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AdoptionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdoptionPhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdoptionPhotos_Adoptions_AdoptionId",
                        column: x => x.AdoptionId,
                        principalTable: "Adoptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdoptionPhotos_AdoptionId",
                table: "AdoptionPhotos",
                column: "AdoptionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdoptionPhotos");
        }
    }
}
