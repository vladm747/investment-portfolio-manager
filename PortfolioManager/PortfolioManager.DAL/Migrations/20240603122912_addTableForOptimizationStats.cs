using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PortfolioManager.DAL.Migrations
{
    /// <inheritdoc />
    public partial class addTableForOptimizationStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "62f1f1f5-0f1c-40d2-be45-68b9613709d5");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "ab0541da-1692-47f2-b799-0366af00791b");

            migrationBuilder.AlterColumn<string>(
                name: "Symbol",
                table: "StockDataHistory",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "PortfolioId",
                table: "StockDataHistory",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PortfolioStatisticForOptimization",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Symbols = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Weights = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastUpdate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PortfolioId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioStatisticForOptimization", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "3c6d7e7d-e01b-4782-8bb8-a329b99c9dcc", null, "investor", "INVESTOR" },
                    { "df92799c-6e3d-4e50-9097-e345702711a2", null, "admin", "ADMIN" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PortfolioStatisticForOptimization");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "3c6d7e7d-e01b-4782-8bb8-a329b99c9dcc");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "df92799c-6e3d-4e50-9097-e345702711a2");

            migrationBuilder.DropColumn(
                name: "PortfolioId",
                table: "StockDataHistory");

            migrationBuilder.AlterColumn<string>(
                name: "Symbol",
                table: "StockDataHistory",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(15)",
                oldMaxLength: 15);

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "62f1f1f5-0f1c-40d2-be45-68b9613709d5", null, "admin", "ADMIN" },
                    { "ab0541da-1692-47f2-b799-0366af00791b", null, "investor", "INVESTOR" }
                });
        }
    }
}
