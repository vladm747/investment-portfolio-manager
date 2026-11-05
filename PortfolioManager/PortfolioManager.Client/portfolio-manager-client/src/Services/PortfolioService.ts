import { http } from "./http";
import PortfolioDto from "../DTO/PortfolioDto";
import { CreatePortfolioDto } from "../DTO/CreatePortfolioDto";

/** Portfolios of the signed-in user; the server knows who that is from the access token. */
export const GetAllPortfoliosAsync = async (): Promise<PortfolioDto[]> => {
    const response = await http.get<PortfolioDto[]>('portfolios');
    return response.data;
};

export const GetPortfolioAsync = async (id: number): Promise<PortfolioDto> => {
    const response = await http.get<PortfolioDto>('portfolio/' + id);
    return response.data;
};

export const CreatePortfolio = async (portfolioDto: CreatePortfolioDto): Promise<PortfolioDto> => {
    const response = await http.post<PortfolioDto>('portfolio', portfolioDto);
    return response.data;
};

export const DeletePortfolio = async (portfolioId: number) => {
    const response = await http.delete(`portfolio/${portfolioId}`);
    return response.data;
};
