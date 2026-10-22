using System.Text.Json;

public sealed record RuleSet
{
    public const string FileName = "blackjack-rules.json";
    public const int MinDeckCount = 2;
    public const int MaxDeckCount = 8;
    private const int DefaultDeckCount = 6;
    public const int DefaultReshuffleThreshold = 25;


    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public bool DealerHitsSoft17 { get; init; }
    public int DeckCount { get; init; } = DefaultDeckCount;
    public int ReshuffleThreshold { get; init; } = DefaultReshuffleThreshold;
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

    public static RuleSet LoadFromFile(
     string filePath,
     Action<string>? notify = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            notify?.Invoke(
                $"Regelfilen '{filePath}' kunde inte hittas. Standardregler används.");
            return Default;
        }

        try
        {
            return Load(filePath);
        }
        catch (JsonException)
        {
            notify?.Invoke(
                "Regelfilen innehåller ogiltig JSON. Kontrollera filens format. Standardregler används.");
            return Default;
        }
        catch (ArgumentOutOfRangeException ex)
        {
            notify?.Invoke(
                $"Regelfilen innehåller ogiltiga värden: {ex.Message} Standardregler används.");
            return Default;
        }
        catch (IOException)
        {
            notify?.Invoke(
                "Regelfilen kunde inte läsas. Kontrollera att filen går att öppna. Standardregler används.");
            return Default;
        }
        catch (UnauthorizedAccessException)
        {
            notify?.Invoke(
                "Åtkomst till regelfilen nekades. Kontrollera filens behörigheter. Standardregler används.");
            return Default;
        }
    }

    public static RuleSet Load(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        string json = File.ReadAllText(filePath);
        RuleSet rules = JsonSerializer.Deserialize<RuleSet>(json, JsonOptions)
            ?? throw new JsonException(
                $"Regelfilen '{filePath}' innehöll ingen giltig regeluppsättning.");
        rules.Validate();
        return rules;
    }

    public void Validate()
    {
        if (DeckCount is < MinDeckCount or > MaxDeckCount)
            throw new ArgumentOutOfRangeException(
                nameof(DeckCount),
                $"Antalet kortlekar måste vara mellan {MinDeckCount} och {MaxDeckCount}.");
        if (ReshuffleThreshold < DefaultReshuffleThreshold || ReshuffleThreshold > DeckCount * Deck.CardsPerDeck)
            throw new ArgumentOutOfRangeException(
                nameof(ReshuffleThreshold),
                $"Blandningsgränsen måste vara minst {DefaultReshuffleThreshold} och får inte överstiga det totala antalet kort.");
    }
}
