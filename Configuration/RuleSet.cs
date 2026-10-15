using System.Text.Json;

public sealed record RuleSet
{
    public const string FileName = "blackjack-rules.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public bool DealerHitsSoft17 { get; init; }
    public int DeckCount { get; init; } = 6;
    public int ReshuffleThreshold { get; init; } = 15;
    public bool NaturalBlackjackBeatsThreeCard21 { get; init; } = true;

    public static RuleSet Default => new();

    public static RuleSet LoadFromDefaultLocation(Action<string>? notify = null)
    {
        string currentDirectoryPath = Path.Combine(
            Environment.CurrentDirectory, FileName);
        if (File.Exists(currentDirectoryPath))
            return LoadFromFile(currentDirectoryPath, notify);

        string applicationDirectoryPath = Path.Combine(
            AppContext.BaseDirectory, FileName);
        if (File.Exists(applicationDirectoryPath))
            return LoadFromFile(applicationDirectoryPath, notify);

        return LoadFromFile(currentDirectoryPath, notify);
    }

    public static RuleSet LoadFromFile(string filePath, Action<string>? notify = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
        {
            notify?.Invoke(
                $"Rules file '{filePath}' was not found. Using default rules.");
            return Default;
        }

        return Load(filePath);
    }

    public static RuleSet Load(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        string json = File.ReadAllText(filePath);
        RuleSet rules = JsonSerializer.Deserialize<RuleSet>(json, JsonOptions)
            ?? throw new JsonException(
                $"Rules file '{filePath}' did not contain a rule set.");
        rules.Validate();
        return rules;
    }

    public void Validate()
    {
        if (DeckCount is < 1 or > 8)
            throw new ArgumentOutOfRangeException(
                nameof(DeckCount), "Deck count must be between 1 and 8.");
        if (ReshuffleThreshold < 1 || ReshuffleThreshold > DeckCount * 52)
            throw new ArgumentOutOfRangeException(
                nameof(ReshuffleThreshold),
                "Reshuffle threshold must be between 1 and the total number of cards.");
    }
}
