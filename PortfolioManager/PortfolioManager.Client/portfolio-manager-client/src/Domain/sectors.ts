import { SectorEnum } from "../DTO/StockDto";

const NAMES: Record<number, string> = {
    [SectorEnum.Technology]: 'Технології',
    [SectorEnum.FinancialServices]: 'Фінанси',
    [SectorEnum.Healthcare]: 'Охорона здоров’я',
    [SectorEnum.ConsumerCyclical]: 'Циклічне споживання',
    [SectorEnum.Industrial]: 'Промисловість',
    [SectorEnum.CommunicationServices]: 'Комунікації',
    [SectorEnum.ConsumerDefensive]: 'Базове споживання',
    [SectorEnum.Energy]: 'Енергетика',
    [SectorEnum.BasicMaterials]: 'Сировина',
    [SectorEnum.RealEstate]: 'Нерухомість',
    [SectorEnum.Utilities]: 'Комунальні послуги',
};

export const sectorName = (sector: number): string => NAMES[sector] ?? 'Інше';

/**
 * The colour of a sector is fixed by the sector itself, not by its rank in the chart, so «Технології» stays
 * the same colour when another sector appears or disappears. The theme has eight categorical colours; sectors
 * beyond the eighth share the neutral «other» colour and are told apart by their label.
 */
export function sectorColor(sector: number, palette: string[], other: string): string {
    return sector >= 0 && sector < palette.length && NAMES[sector] ? palette[sector] : other;
}
