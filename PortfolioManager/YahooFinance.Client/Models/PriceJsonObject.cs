namespace YahooFinance.Client.Models;

internal class PriceJsonObject
{
    public class Data
    {
        public int start { get; set; }
        public int limit { get; set; }
        public int count { get; set; }
        public string previous { get; set; }
        public string next { get; set; }
        public Price[] result { get; set; } 
    }

    public class Price
    {
        public double? open { get; set; }
        public double? high { get; set; }
        public double? low { get; set; }
        public double? close { get; set; }
        public double? volume { get; set; }
        public double? Dividends { get; set; }
        public double? StockSplits { get; set; }
    }
    
    public class Root
    {
        public Data Data { get; set; }
        public string Message { get; set; }
        public int Status { get; set; }
    }
}