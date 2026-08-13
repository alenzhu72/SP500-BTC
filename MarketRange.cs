namespace MarketFloat;

public enum MarketRange
{
    Day,
    Week,
    Month,
    Year
}

public static class MarketRangeExtensions
{
    public static string YahooRange(this MarketRange range) => range switch
    {
        MarketRange.Day => "1d",
        MarketRange.Week => "5d",
        MarketRange.Month => "1mo",
        MarketRange.Year => "1y",
        _ => "1d"
    };

    public static string YahooInterval(this MarketRange range) => range switch
    {
        MarketRange.Day => "5m",
        MarketRange.Week => "30m",
        MarketRange.Month => "90m",
        MarketRange.Year => "1d",
        _ => "5m"
    };

    public static int FredPoints(this MarketRange range) => range switch
    {
        MarketRange.Day => 2,
        MarketRange.Week => 6,
        MarketRange.Month => 23,
        MarketRange.Year => 253,
        _ => 2
    };

    public static int CoinbaseGranularity(this MarketRange range) => range switch
    {
        MarketRange.Day => 300,
        MarketRange.Week => 3600,
        MarketRange.Month => 21600,
        MarketRange.Year => 86400,
        _ => 300
    };
}
