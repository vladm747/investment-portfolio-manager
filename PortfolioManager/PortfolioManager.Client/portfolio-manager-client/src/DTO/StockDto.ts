export default interface StockDto {
    id: number;
    symbol: string;
    name: string;
    quantity: number;
    entryPrice: number;
    currentPrice: number;
    totalCurrentPrice: number;
    gain: number;
    gainPercentage: number;
    entryDate: Date;
    currency: string;
    sector: SectorEnum;
    portfolioId: number;
}

export const getSectorText = (sector: number): string => {
    switch (sector) {
        case SectorEnum.Technology:
            return "Technology";
        case SectorEnum.FinancialServices:
            return "Financial Services";
        case SectorEnum.Healthcare:
            return "Healthcare";
        case SectorEnum.ConsumerCyclical:
            return "Consumer Cyclical";
        case SectorEnum.Industrial:
            return "Industrial";
        case SectorEnum.CommunicationServices:
            return "Communication Services";
        case SectorEnum.ConsumerDefensive:
            return "Consumer Defensive";
        case SectorEnum.Energy:
            return "Energy";
        case SectorEnum.BasicMaterials:
            return "Basic Materials";
        case SectorEnum.RealEstate:
            return "Real Estate";
        case SectorEnum.Utilities:
            return "Utilities";
        default:
            return "Unknown";
    }
}

export enum SectorEnum {
    Technology = 0,
    FinancialServices = 1,
    Healthcare = 2,
    ConsumerCyclical = 3,
    Industrial = 4,
    CommunicationServices = 5,
    ConsumerDefensive = 6,
    Energy = 7,
    BasicMaterials = 8,
    RealEstate = 9,
    Utilities = 10
}
