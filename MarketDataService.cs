using System.Net.Http;
using System.Text.Json;
using System.Globalization;

namespace MarketFloat;

public sealed class MarketDataService
{
    private readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(12)
    };

    public MarketDataService()
    {
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("MarketFloat/1.0");
    }

    public async Task<MarketData> LoadAsync(string symbol, string displayName, MarketRange range, CancellationToken token)
    {
        try
        {
            return await LoadYahooAsync(symbol, displayName, range, token);
        }
        catch when (!token.IsCancellationRequested)
        {
            return symbol == "BTC-USD"
                ? await LoadCoinbaseAsync(range, token)
                : await LoadFredAsync(range, token);
        }
    }

    private async Task<MarketData> LoadYahooAsync(string symbol, string displayName, MarketRange range, CancellationToken token)
    {
        var encoded = Uri.EscapeDataString(symbol);
        var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{encoded}?range={range.YahooRange()}&interval={range.YahooInterval()}&includePrePost=false";
        using var response = await _http.GetAsync(url, token);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(token);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: token);

        var result = json.RootElement.GetProperty("chart").GetProperty("result")[0];
        var meta = result.GetProperty("meta");
        var closes = result.GetProperty("indicators").GetProperty("quote")[0].GetProperty("close")
            .EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.Number)
            .Select(x => x.GetDouble())
            .Where(double.IsFinite)
            .ToArray();

        if (closes.Length == 0) throw new System.IO.InvalidDataException($"{symbol} 没有返回行情数据");

        var price = ReadNumber(meta, "regularMarketPrice") ?? closes[^1];
        var previousClose = ReadNumber(meta, "chartPreviousClose")
                            ?? ReadNumber(meta, "previousClose")
                            ?? closes[0];
        var currency = meta.TryGetProperty("currency", out var c) ? c.GetString() ?? "USD" : "USD";
        var marketTime = ReadNumber(meta, "regularMarketTime");
        var updated = marketTime is > 0
            ? DateTimeOffset.FromUnixTimeSeconds((long)marketTime.Value).ToLocalTime()
            : DateTimeOffset.Now;

        return new MarketData(symbol, displayName, currency, price, previousClose, closes, updated);
    }

    private async Task<MarketData> LoadFredAsync(MarketRange range, CancellationToken token)
    {
        const string url = "https://fred.stlouisfed.org/graph/fredgraph.csv?id=SP500";
        var csv = await _http.GetStringAsync(url, token);
        var rows = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Skip(1)
            .Select(line => line.Trim().Split(','))
            .Where(parts => parts.Length == 2 && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            .Select(parts => new
            {
                Date = DateTime.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date) ? date : DateTime.Now,
                Value = double.Parse(parts[1], CultureInfo.InvariantCulture)
            })
            .TakeLast(range.FredPoints())
            .ToArray();
        if (rows.Length < 2) throw new System.IO.InvalidDataException("标普 500 备用数据暂不可用");
        return new MarketData("^GSPC", "S&P 500", "USD", rows[^1].Value, rows[^2].Value,
            rows.Select(x => x.Value).ToArray(), rows[^1].Date);
    }

    private async Task<MarketData> LoadCoinbaseAsync(MarketRange range, CancellationToken token)
    {
        var url = $"https://api.exchange.coinbase.com/products/BTC-USD/candles?granularity={range.CoinbaseGranularity()}";
        using var response = await _http.GetAsync(url, token);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(token);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: token);
        var candles = json.RootElement.EnumerateArray()
            .Select(row => new
            {
                Time = row[0].GetInt64(),
                Close = row[4].GetDouble()
            })
            .OrderBy(x => x.Time)
            .ToArray();
        if (candles.Length < 2) throw new System.IO.InvalidDataException("BTC/USD 备用数据暂不可用");
        var recent = candles.TakeLast(Math.Min(288, candles.Length)).ToArray();
        return new MarketData("BTC-USD", "Bitcoin", "USD", recent[^1].Close, recent[0].Close,
            recent.Select(x => x.Close).ToArray(), DateTimeOffset.FromUnixTimeSeconds(recent[^1].Time).ToLocalTime());
    }

    private static double? ReadNumber(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetDouble()
            : null;
}
