using PortfolioManager.Common;

namespace PortfolioManager.BLL.Helpers;

/// <summary>Yahoo Finance's sector names, which contain spaces, to <see cref="SectorEnum"/>.</summary>
public static class SectorMapper
{
    private static readonly Dictionary<string, SectorEnum> Sectors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Technology"] = SectorEnum.Technology,
        ["Financial Services"] = SectorEnum.FinancialServices,
        ["Healthcare"] = SectorEnum.Healthcare,
        ["Consumer Cyclical"] = SectorEnum.ConsumerCyclical,
        ["Industrials"] = SectorEnum.Industrial, // Yahoo writes it in the plural
        ["Industrial"] = SectorEnum.Industrial,
        ["Communication Services"] = SectorEnum.CommunicationServices,
        ["Consumer Defensive"] = SectorEnum.ConsumerDefensive,
        ["Energy"] = SectorEnum.Energy,
        ["Basic Materials"] = SectorEnum.BasicMaterials,
        ["Real Estate"] = SectorEnum.RealEstate,
        ["Utilities"] = SectorEnum.Utilities,
    };

    /// <summary>
    /// The sector for Yahoo's text. Null, empty (ETFs and funds have no sector) and names not listed above
    /// give <see cref="SectorEnum.Unknown"/>: a position is never refused because of its sector.
    /// </summary>
    public static SectorEnum FromYahoo(string? sector) =>
        sector is not null && Sectors.TryGetValue(sector.Trim(), out var mapped) ? mapped : SectorEnum.Unknown;
}
