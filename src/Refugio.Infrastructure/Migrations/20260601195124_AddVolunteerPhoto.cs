using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Refugio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVolunteerPhoto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                table: "Volunteers",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoUrl",
                table: "Volunteers");
        }
    }
}
