using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PortfolioManager.DAL.Migrations
{
    /// <inheritdoc />
    public partial class updatestockDataHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "0079b904-7866-4767-9114-9d41120c61d9");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "ba0531e4-03ba-4067-9acb-24f620cf5c98");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "04c7f224-83d2-471b-981d-08b35962f91b", null, "admin", "ADMIN" },
                    { "0a1cde23-4095-47e3-b17e-5ae66c7c2cc7", null, "investor", "INVESTOR" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "04c7f224-83d2-471b-981d-08b35962f91b");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "0a1cde23-4095-47e3-b17e-5ae66c7c2cc7");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "0079b904-7866-4767-9114-9d41120c61d9", null, "investor", "INVESTOR" },
                    { "ba0531e4-03ba-4067-9acb-24f620cf5c98", null, "admin", "ADMIN" }
                });
        }
    }
}
