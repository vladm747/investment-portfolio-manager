using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortfolioManager.DAL.Migrations
{
    /// <inheritdoc />
    public partial class updateTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EntryValue",
                table: "Stocks",
                newName: "TotalValue");

            migrationBuilder.RenameColumn(
                name: "CurrentValue",
                table: "Stocks",
                newName: "EntryPrice");

            migrationBuilder.RenameColumn(
                name: "TotalEntryValue",
                table: "Portfolios",
                newName: "TotalEntryPrice");

            migrationBuilder.RenameColumn(
                name: "TotalCurrentValue",
                table: "Portfolios",
                newName: "TotalCurrenPrice");

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentPrice",
                table: "Stocks",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Portfolios",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "StockSymbols",
                columns: table => new
                {
                    Symbol = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sector = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockSymbols", x => x.Symbol);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockSymbols");

            migrationBuilder.DropColumn(
                name: "CurrentPrice",
                table: "Stocks");

            migrationBuilder.RenameColumn(
                name: "TotalValue",
                table: "Stocks",
                newName: "EntryValue");

            migrationBuilder.RenameColumn(
                name: "EntryPrice",
                table: "Stocks",
                newName: "CurrentValue");

            migrationBuilder.RenameColumn(
                name: "TotalEntryPrice",
                table: "Portfolios",
                newName: "TotalEntryValue");

            migrationBuilder.RenameColumn(
                name: "TotalCurrenPrice",
                table: "Portfolios",
                newName: "TotalCurrentValue");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Portfolios",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }
    }
}
