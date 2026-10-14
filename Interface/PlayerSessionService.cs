using System.Text.Json;

internal sealed class PlayerSessionService
{
    private readonly GameStorage gameStorage;
    private readonly ConsoleUi ui;

    public PlayerSessionService(GameStorage gameStorage, ConsoleUi ui)
    {
        this.gameStorage = gameStorage;
        this.ui = ui;
    }

    public List<Player>? CreateNewPlayers(int playerCount)
    {
        Dictionary<string, PlayerStatistics> results;
        try
        {
            results = gameStorage.LoadResults();
        }
        catch (Exception ex) when (
            ex is IOException ||
            ex is UnauthorizedAccessException ||
            ex is JsonException)
        {
            ui.ShowCenteredMessage(GameMessages.LoadPlayerStatisticsError);
            return null;
        }

        string playerName = ui.AskPlayerNameCentered();
        if (string.IsNullOrWhiteSpace(playerName))
            playerName = GameMessages.DefaultPlayerName;

        List<Player> players = new();
        if (results.TryGetValue(playerName, out PlayerStatistics? stats))
            players.Add(new Player(playerName, stats.Wins, stats.Losses, stats.Pushes));
        else
            players.Add(new Player(playerName));

        for (int i = 2; i <= playerCount; i++)
            players.Add(new Player(GameMessages.PlayerName(i)));

        return players;
    }

    public Player? LoadPlayer()
    {
        Dictionary<string, PlayerStatistics> results;
        try
        {
            results = gameStorage.LoadResults();
        }
        catch (Exception ex) when (
            ex is IOException ||
            ex is UnauthorizedAccessException ||
            ex is JsonException)
        {
            ui.ShowCenteredMessage(GameMessages.LoadSavedResultsError);
            return null;
        }

        if (results.Count == 0)
        {
            ui.ShowCenteredMessage(GameMessages.NoSavedGames);
            return null;
        }

        ui.ShowStatistics(results);
        string? selectedName = ui.SelectSavedPlayer(results);
        if (selectedName is null)
            return null;

        PlayerStatistics stats = results[selectedName];
        return new Player(selectedName, stats.Wins, stats.Losses, stats.Pushes);
    }

    public bool SaveResults(List<Player> players)
    {
        try
        {
            Player player = players[0];
            Dictionary<string, PlayerStatistics> results = new()
            {
                [player.Name] = new PlayerStatistics
                {
                    Wins = player.Wins,
                    Losses = player.Losses,
                    Pushes = player.Pushes
                }
            };
            gameStorage.SaveResults(results);
            ui.ShowStatistics(gameStorage.LoadResults());
            return true;
        }
        catch (Exception ex) when (
            ex is IOException ||
            ex is UnauthorizedAccessException ||
            ex is JsonException)
        {
            return false;
        }
    }

    public bool SaveGame(List<Player> players)
    {
        bool saved = SaveResults(players);
        ui.ShowCenteredMessage(saved
            ? GameMessages.SaveStatisticsSuccess
            : GameMessages.SaveStatisticsError);
        return saved;
    }
}
