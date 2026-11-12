using PortfolioManager.BLL.Helpers;
using PortfolioManager.Common;

namespace PortfolioManager.Tests.Services;

public class SectorMapperTests
{
    // The sector names Yahoo Finance uses; the ones with spaces are why Enum.Parse could not be used.
    [Theory]
    [InlineData("Technology", SectorEnum.Technology)]
    [InlineData("Financial Services", SectorEnum.FinancialServices)]
    [InlineData("Healthcare", SectorEnum.Healthcare)]
    [InlineData("Consumer Cyclical", SectorEnum.ConsumerCyclical)]
    [InlineData("Industrials", SectorEnum.Industrial)]
    [InlineData("Industrial", SectorEnum.Industrial)]
    [InlineData("Communication Services", SectorEnum.CommunicationServices)]
    [InlineData("Consumer Defensive", SectorEnum.ConsumerDefensive)]
    [InlineData("Energy", SectorEnum.Energy)]
    [InlineData("Basic Materials", SectorEnum.BasicMaterials)]
    [InlineData("Real Estate", SectorEnum.RealEstate)]
    [InlineData("Utilities", SectorEnum.Utilities)]
    public void Yahoo_sector_names_map_to_the_enum(string yahoo, SectorEnum expected) =>
        Assert.Equal(expected, SectorMapper.FromYahoo(yahoo));

    [Theory]
    [InlineData("  financial services ")]
    [InlineData("TECHNOLOGY")]
    public void Case_and_surrounding_spaces_do_not_matter(string yahoo) =>
        Assert.NotEqual(SectorEnum.Unknown, SectorMapper.FromYahoo(yahoo));

    [Theory]
    [InlineData(null)] // an ETF or fund
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Crypto Assets")] // a sector Yahoo adds later
    public void Missing_or_new_sectors_are_Unknown_instead_of_an_error(string? yahoo) =>
        Assert.Equal(SectorEnum.Unknown, SectorMapper.FromYahoo(yahoo));

    [Fact]
    public void Unknown_is_appended_so_no_stored_sector_changes_meaning() =>
        Assert.Equal(11, (int)SectorEnum.Unknown);

    [Fact]
    public void Every_sector_except_Unknown_can_be_produced_by_the_mapper()
    {
        var mapped = new[]
            {
                "Technology", "Financial Services", "Healthcare", "Consumer Cyclical", "Industrials",
                "Communication Services", "Consumer Defensive", "Energy", "Basic Materials", "Real Estate", "Utilities"
            }
            .Select(SectorMapper.FromYahoo)
            .ToHashSet();

        var all = Enum.GetValues<SectorEnum>().Where(sector => sector != SectorEnum.Unknown);

        Assert.Equal(all.OrderBy(s => s), mapped.OrderBy(s => s));
    }
}
