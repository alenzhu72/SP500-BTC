namespace MarketFloat;

public sealed record MarketData(
    string Symbol,
    string Name,
    string Currency,
    double Price,
    double PreviousClose,
    IReadOnlyList<double> Points,
    DateTimeOffset UpdatedAt)
{
    public double Change => Price - PreviousClose;
    public double ChangePercent => PreviousClose == 0 ? 0 : Change / PreviousClose * 100;
}
