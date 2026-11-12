import { http } from "./http";
import PortfolioDto from "../DTO/PortfolioDto";
import { CreatePortfolioDto } from "../DTO/CreatePortfolioDto";

/** Portfolios of the signed-in user; the server knows who that is from the access token. */
export const GetAllPortfoliosAsync = async (): Promise<PortfolioDto[]> => {
    const response = await http.get<PortfolioDto[]>('portfolios');
    return response.data;
};

export const GetPortfolioAsync = async (id: number): Promise<PortfolioDto> => {
    const response = await http.get<PortfolioDto>(`portfolios/${id}`);
    return response.data;
};

/** Answered with 201 and the new portfolio. */
export const CreatePortfolio = async (portfolioDto: CreatePortfolioDto): Promise<PortfolioDto> => {
    const response = await http.post<PortfolioDto>('portfolios', portfolioDto);
    return response.data;
};

/** Deletes the portfolio and its positions; answered with 204. */
export const DeletePortfolio = async (portfolioId: number): Promise<void> => {
    await http.delete(`portfolios/${portfolioId}`);
};
