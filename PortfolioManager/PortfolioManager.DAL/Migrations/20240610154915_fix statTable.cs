using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PortfolioManager.DAL.Migrations
{
    /// <inheritdoc />
    public partial class fixstatTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "3c6d7e7d-e01b-4782-8bb8-a329b99c9dcc");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "df92799c-6e3d-4e50-9097-e345702711a2");

            migrationBuilder.AlterColumn<string>(
                name: "Symbols",
                table: "PortfolioStatisticForOptimization",
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
                    { "35b4e5c7-7404-47f8-ab51-fc11e110997a", null, "investor", "INVESTOR" },
                    { "750f280d-0d9a-4599-ab6d-26646e720b9d", null, "admin", "ADMIN" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "35b4e5c7-7404-47f8-ab51-fc11e110997a");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "750f280d-0d9a-4599-ab6d-26646e720b9d");

            migrationBuilder.AlterColumn<string>(
                name: "Symbols",
                table: "PortfolioStatisticForOptimization",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "3c6d7e7d-e01b-4782-8bb8-a329b99c9dcc", null, "investor", "INVESTOR" },
                    { "df92799c-6e3d-4e50-9097-e345702711a2", null, "admin", "ADMIN" }
                });
        }
    }
}
