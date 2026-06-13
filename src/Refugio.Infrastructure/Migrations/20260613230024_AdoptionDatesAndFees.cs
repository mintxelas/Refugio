using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Refugio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdoptionDatesAndFees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AdoptionDate",
                table: "Adoptions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AdoptionFeeCharged",
                table: "Adoptions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PreAdoptionDate",
                table: "Adoptions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PreAdoptionFeeCharged",
                table: "Adoptions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdoptionDate",
                table: "Adoptions");

            migrationBuilder.DropColumn(
                name: "AdoptionFeeCharged",
                table: "Adoptions");

            migrationBuilder.DropColumn(
                name: "PreAdoptionDate",
                table: "Adoptions");

            migrationBuilder.DropColumn(
                name: "PreAdoptionFeeCharged",
                table: "Adoptions");
        }
    }
}
