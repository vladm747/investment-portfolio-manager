using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortfolioManager.DAL.Migrations
{
    /// <summary>
    /// One clean-up migration. It DROPS data, so back up before applying it to a database that matters
    /// (the API applies migrations at startup to whatever database the connection string names):
    /// <list type="bullet">
    /// <item>tables StockDataHistory and PortfolioStatisticForOptimization: nothing reads or writes them any more;</item>
    /// <item>Portfolios.TotalEntryPrice / TotalCurrenPrice (the typo goes with it) / TotalGain / TotalGainPercentage and
    /// Stocks.TotalCurrentPrice / Gain / GainPercentage: derived numbers, now worked out from quantity and prices on
    /// every read, so they cannot go stale. Nothing is lost that the remaining columns do not give back;</item>
    /// <item>Stocks.EntryPrice and CurrentPrice: float becomes decimal(18,4). Values are rounded to four decimals.</item>
    /// </list>
    /// Down restores the columns and the empty tables, and recalculates the derived columns from the prices.
    /// </summary>
    public partial class SchemaCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PortfolioStatisticForOptimization");

            migrationBuilder.DropTable(
                name: "StockDataHistory");

            migrationBuilder.DropColumn(
                name: "Gain",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "GainPercentage",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "TotalCurrentPrice",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "TotalCurrenPrice",
                table: "Portfolios");

            migrationBuilder.DropColumn(
                name: "TotalEntryPrice",
                table: "Portfolios");

            migrationBuilder.DropColumn(
                name: "TotalGain",
                table: "Portfolios");

            migrationBuilder.DropColumn(
                name: "TotalGainPercentage",
                table: "Portfolios");

            migrationBuilder.AlterColumn<decimal>(
                name: "EntryPrice",
                table: "Stocks",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<decimal>(
                name: "CurrentPrice",
                table: "Stocks",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "EntryPrice",
                table: "Stocks",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<double>(
                name: "CurrentPrice",
                table: "Stocks",
                type: "float",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AddColumn<double>(
                name: "Gain",
                table: "Stocks",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GainPercentage",
                table: "Stocks",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "TotalCurrentPrice",
                table: "Stocks",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "TotalCurrenPrice",
                table: "Portfolios",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "TotalEntryPrice",
                table: "Portfolios",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "TotalGain",
                table: "Portfolios",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TotalGainPercentage",
                table: "Portfolios",
                type: "float",
                nullable: true);

            // The restored columns would otherwise all read 0 until somebody changed each position: fill them
            // in from the prices, the way the application calculated them (see PositionValuation).
            migrationBuilder.Sql(@"
                UPDATE [Stocks] SET
                    [TotalCurrentPrice] = [Quantity] * [CurrentPrice],
                    [Gain] = [Quantity] * [CurrentPrice] - [Quantity] * [EntryPrice],
                    [GainPercentage] = CASE WHEN [Quantity] * [EntryPrice] > 0
                        THEN ([Quantity] * [CurrentPrice] - [Quantity] * [EntryPrice]) / ([Quantity] * [EntryPrice]) * 100
                        ELSE 0 END;");

            migrationBuilder.Sql(@"
                UPDATE p SET
                    [TotalEntryPrice] = totals.[Entry],
                    [TotalCurrenPrice] = totals.[Current],
                    [TotalGain] = totals.[Current] - totals.[Entry],
                    [TotalGainPercentage] = CASE WHEN totals.[Entry] > 0
                        THEN (totals.[Current] - totals.[Entry]) / totals.[Entry] * 100
                        ELSE 0 END
                FROM [Portfolios] AS p
                JOIN (
                    SELECT [PortfolioId],
                           SUM([Quantity] * [EntryPrice]) AS [Entry],
                           SUM([Quantity] * [CurrentPrice]) AS [Current]
                    FROM [Stocks] GROUP BY [PortfolioId]
                ) AS totals ON totals.[PortfolioId] = p.[Id];");

            migrationBuilder.CreateTable(
                name: "PortfolioStatisticForOptimization",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LastUpdate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PortfolioId = table.Column<int>(type: "int", nullable: false),
                    Symbols = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Weights = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioStatisticForOptimization", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockDataHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PortfolioId = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockDataHistory", x => x.Id);
                });
        }
    }
}
