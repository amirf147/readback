namespace ReadBack.Core.Models;

public class FilterContext
{
    public AppSettings Settings { get; init; }
    public Dictionary<string, object> Items { get; } = new();

    public FilterContext(AppSettings settings)
    {
        Settings = settings;
    }
}
