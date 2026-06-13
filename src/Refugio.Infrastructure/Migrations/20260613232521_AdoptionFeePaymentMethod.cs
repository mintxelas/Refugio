using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Refugio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdoptionFeePaymentMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdoptionFeePaymentMethod",
                table: "Adoptions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreAdoptionFeePaymentMethod",
                table: "Adoptions",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdoptionFeePaymentMethod",
                table: "Adoptions");

            migrationBuilder.DropColumn(
                name: "PreAdoptionFeePaymentMethod",
                table: "Adoptions");
        }
    }
}
