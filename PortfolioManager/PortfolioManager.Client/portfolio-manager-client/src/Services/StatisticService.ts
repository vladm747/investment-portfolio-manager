import { http } from "./http";

// The optimisation downloads years of prices in the Python service, so allow it time.
const STATISTICS_TIMEOUT_MS = 100000;

export const GetOptimisationData = async (portfolioId: number) => {
    const response = await http.get('statistics/opt/' + portfolioId, { timeout: STATISTICS_TIMEOUT_MS });
    return response.data;
};

export const GetGrowthStatsData = async (portfolioId: number) => {
    const response = await http.get('statistics/growth/' + portfolioId, { timeout: STATISTICS_TIMEOUT_MS });
    return response.data;
};
