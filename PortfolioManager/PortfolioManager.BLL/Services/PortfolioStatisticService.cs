using AutoMapper;
using Newtonsoft.Json;
using PortfolioManager.BLL.Interfaces;
using PortfolioManager.BLL.Models;
using PortfolioManager.Common.DTO;
using PortfolioManager.DAL.Entities;
using PortfolioManager.DAL.Infrastructure.DI.Abstract;
using PortfolioManager.DAL.Infrastructure.DI.Abstract.Base;
using RestSharp;
using YahooFinance.Client.Models;
using static PortfolioManager.BLL.Services.PortfolioComparer;

namespace PortfolioManager.BLL.Services;

public class PortfolioStatisticService: IPortfolioStatisticService
{
    private readonly IPortfolioStatisticForOptRepository _statRepo;
    private readonly IPortfolioRepository _portRepo;
    private readonly IMapper _mapper;
    
    public PortfolioStatisticService(IPortfolioStatisticForOptRepository statRepo, IPortfolioRepository portfolioRepo, IMapper mapper)
    {
        _statRepo = statRepo;
        _portRepo = portfolioRepo;
        _mapper = mapper;
    }

    public async Task<IEnumerable<StockDTO>> GetDataForGrowthStats(int portfolioId)
    {
        var portfolio = await _portRepo.GetAsync(portfolioId);
        var stocks = _mapper.Map<IEnumerable<StockDTO>>(portfolio.Stocks);
        return stocks;
    }
    
     public async Task<PortfolioGrowthResponse> GetPortfolioGrowsStats(int portfolioId)
    {
        try
        {
            var stocks = await GetDataForGrowthStats(portfolioId);
            var options = new RestClientOptions("http://localhost:8011")
            {
                MaxTimeout = -1,
            };
            var client = new RestClient(options);
            var request = new RestRequest("/api/portfolio-growth", Method.Post);
            request.AddHeader("Content-Type", "application/json");
            var body = JsonConvert.SerializeObject(stocks);
            request.AddStringBody(body, DataFormat.Json);
            RestResponse response = await client.ExecuteAsync(request);
            Console.WriteLine(response.Content);
            var growthResponce = new PortfolioGrowthResponse();
            
            growthResponce.GrowthData = JsonConvert.DeserializeObject<Dictionary<DateTime, decimal>>(response.Content);
            return growthResponce;
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }

    public async Task<PortfolioStatisticForOpt> GetDataForOpt(int portfolioId)
    {
        var data = await _statRepo.GetByPortfolioId(portfolioId);
        return data;
    }
    
    
    
    public ComparisonResult GetMetrics(int portfolioId)
    {
        try
        {
            var statForOpt = GetDataForOpt(portfolioId).Result;
            var base_weights = JsonConvert.DeserializeObject<double[]>(statForOpt.Weights);
            var portfolio_symbols = JsonConvert.DeserializeObject<string[]>(statForOpt.Symbols);
            
            var options = new RestClientOptions("http://localhost:8011")
            {
                MaxTimeout = -1,
            };
            
            var client = new RestClient(options);
            var request = new RestRequest("/api/get-portfolio-opt", Method.Post);
            request.AddHeader("Content-Type", "application/json");
            
            var model = new
            {
                base_weights = base_weights,
                portfolio_symbols = portfolio_symbols
            };
            
            var body = JsonConvert.SerializeObject(model);
            request.AddStringBody(body, DataFormat.Json);
        
            RestResponse response = client.Execute<string>(request);
        
            if(response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception("Error while getting portfolio metrics");
            
            var portfolios = JsonConvert.DeserializeObject<List<Dictionary<string, Dictionary<string, object>>>>(response.Content);

            var portfolioMetricsList = new List<PortfolioMetricsDTO>();

            int counter = 0;
            var weights = new Weights();
            
            foreach (var portfolio in portfolios)
            {
                counter++;
                if (counter == 3)
                {
                    var weightsDict = portfolio["Weights"];
                    weights.BaseWeights = JsonConvert.DeserializeObject<double[]>(weightsDict["BaseWeights"].ToString());
                    weights.OptimizedWeights = JsonConvert.DeserializeObject<Dictionary<string, double>>(weightsDict["OptimizedWeights"].ToString());
                    break;
                }
                
                var portfolioMetrics = new PortfolioMetricsDTO
                {
                    Benchmark = ParseMetrics(portfolio["Benchmark"]),
                    Strategy = ParseMetrics(portfolio["Strategy"])
                };
                
                portfolioMetricsList.Add(portfolioMetrics);
            }
            
            var portfolioComparison = PortfolioComparer.Compare(portfolioMetricsList[0].Strategy, portfolioMetricsList[1].Strategy);
            portfolioComparison.Weights = weights;
            
            return portfolioComparison;
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }
    //
    // public Weights ParseWeghts()
    
    private MetricsDTO ParseMetrics(Dictionary<string, object> metricsDict)
    {
        return new MetricsDTO
        {
            StartPeriod = metricsDict["Start Period"].ToString(),
            EndPeriod = metricsDict["End Period"].ToString(),
            RiskFreeRate = Convert.ToDouble(metricsDict["Risk-Free Rate"]),
            TimeInMarket = Convert.ToDouble(metricsDict["Time in Market"]),
            CumulativeReturn = Convert.ToDouble(metricsDict["Cumulative Return"]),
            CAGR = Convert.ToDouble(metricsDict["CAGR﹪"]),
            Sharpe = Convert.ToDouble(metricsDict["Sharpe"]),
            ProbSharpeRatio = Convert.ToDouble(metricsDict["Prob. Sharpe Ratio"]),
            Sortino = Convert.ToDouble(metricsDict["Sortino"]),
            MaxDrawdown = Convert.ToDouble(metricsDict["Max Drawdown"]),
            LongestDDDays = Convert.ToInt32(metricsDict["Longest DD Days"]),
            VolatilityAnn = Convert.ToDouble(metricsDict["Volatility (ann.)"]),
            InformationRatio = Convert.ToDouble(metricsDict["Information Ratio"]),
            Calmar = Convert.ToDouble(metricsDict["Calmar"]),
            ExpectedDaily = Convert.ToDouble(metricsDict["Expected Daily"]),
            ExpectedMonthly = Convert.ToDouble(metricsDict["Expected Monthly"]),
            ExpectedYearly = Convert.ToDouble(metricsDict["Expected Yearly"]),
            DailyValueAtRisk = Convert.ToDouble(metricsDict["Daily Value-at-Risk"]),
            MaxConsecutiveWins = Convert.ToDouble(metricsDict["Max Consecutive Wins"]),
            MaxConsecutiveLosses = Convert.ToDouble(metricsDict["Max Consecutive Losses"]),
            GainPainRatio = Convert.ToDouble(metricsDict["Gain/Pain Ratio"]),
            GainPain1M = Convert.ToDouble(metricsDict["Gain/Pain (1M)"]),
            PayoffRatio = Convert.ToDouble(metricsDict["Payoff Ratio"]),
            MTD = Convert.ToDouble(metricsDict["MTD"]),
            M3 = Convert.ToDouble(metricsDict["3M"]),
            M6 = Convert.ToDouble(metricsDict["6M"]),
            YTD = Convert.ToDouble(metricsDict["YTD"]),
            Y1 = Convert.ToDouble(metricsDict["1Y"]),
            Y3Ann = Convert.ToDouble(metricsDict["3Y (ann.)"]),
            Y5Ann = Convert.ToDouble(metricsDict["5Y (ann.)"]),
            Y10Ann = Convert.ToDouble(metricsDict["10Y (ann.)"]),
            AllTimeAnn = Convert.ToDouble(metricsDict["All-time (ann.)"]),
            BestDay = Convert.ToDouble(metricsDict["Best Day"]),
            WorstDay = Convert.ToDouble(metricsDict["Worst Day"]),
            BestMonth = Convert.ToDouble(metricsDict["Best Month"]),
            WorstMonth = Convert.ToDouble(metricsDict["Worst Month"]),
            BestYear = Convert.ToDouble(metricsDict["Best Year"]),
            WorstYear = Convert.ToDouble(metricsDict["Worst Year"]),
            AvgDrawdown = Convert.ToDouble(metricsDict["Avg. Drawdown"]),
            AvgDrawdownDays = Convert.ToDouble(metricsDict["Avg. Drawdown Days"]),
            RecoveryFactor = Convert.ToDouble(metricsDict["Recovery Factor"]),
            AvgUpMonth = Convert.ToDouble(metricsDict["Avg. Up Month"]),
            AvgDownMonth = Convert.ToDouble(metricsDict["Avg. Down Month"]),
            WinDays = Convert.ToDouble(metricsDict["Win Days"]),
            WinMonth = Convert.ToDouble(metricsDict["Win Month"]),
            WinQuarter = Convert.ToDouble(metricsDict["Win Quarter"]),
            WinYear = Convert.ToDouble(metricsDict["Win Year"])
        };
    }
}