import { sectorName } from '../Domain/sectors';

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

/** The sector's name in Ukrainian (see Domain/sectors.ts). */
export const getSectorText = (sector: number): string => sectorName(sector);

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
