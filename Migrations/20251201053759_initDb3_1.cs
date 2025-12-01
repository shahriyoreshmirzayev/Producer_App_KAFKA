using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MVCandKAFKA3.Migrations
{
    /// <inheritdoc />
    public partial class initDb3_1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReviewComments",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedBy",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedDate",
                table: "Products",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReviewComments",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ReviewedBy",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ReviewedDate",
                table: "Products");
        }
    }
}
