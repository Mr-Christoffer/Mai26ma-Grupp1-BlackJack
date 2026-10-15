public class Game
{
    private readonly ConsoleUi ui = new();
    private readonly PlayerSessionService playerSessions;
    private readonly GameTableUI gameTable;
    private readonly RuleSet rules;
    private readonly string? ruleConfigurationNotice;

    public Game()
    {
        string? notice = null;
        rules = RuleSet.LoadFromDefaultLocation(message => notice = message);
        ruleConfigurationNotice = notice;
        playerSessions = new PlayerSessionService(new GameStorage(), ui);
        gameTable = new GameTableUI(ui);
    }

    public void Run()
    {
        bool running = true;
        bool showRuleNotice = ruleConfigurationNotice is not null;
        while (running)
        {
            string choice = ui.ShowMenu(
                showRuleNotice ? ruleConfigurationNotice : null);
            showRuleNotice = false;
            switch (choice)
            {
                case GameMessages.NewGame:
                    StartNewGame();
                    break;
                case GameMessages.LoadGame:
                    LoadGame();
                    break;
                case GameMessages.ShowRules:
                    ui.ShowRules(rules);
                    break;
                case GameMessages.ShowCredits:
                    ui.ShowCredits();
                    break;
                case GameMessages.Exit:
                    running = false;
                    break;
            }
        }

    }

    private void StartNewGame()
    {
        int playerCount = ui.AskPlayerCountCentered();
        List<Player>? players = playerSessions.CreateNewPlayers(playerCount);
        if (players is not null)
            PlayGame(players);
    }

    private void LoadGame()
    {
        Player? player = playerSessions.LoadPlayer();
        if (player is not null)
            PlayGame(new List<Player> { player });
    }

    private void PlayGame(List<Player> players)
    {
        gameTable.ResetSession();
        Deck deck = new(rules);
        deck.CreateDeck();
        deck.ShuffleDeck();
        Dealer dealer = new(rules);
        int roundNumber = 1;

        while (true)
        {
            Round round = new(roundNumber++, players, dealer, deck, rules);
            round.StartRound();

            GameTableAction action = gameTable.PlayRound(
                round, playerSessions.SaveGame);
            if (action == GameTableAction.RoundCompleted)
            {
                round.EndRound();
                string summary = RoundResultFormatter.Format(round);
                if (!playerSessions.SaveResults(players))
                    summary = GameMessages.AppendSaveResultsError(summary);

                action = gameTable.ShowRoundEndMenu(
                    round, summary, playerSessions.SaveGame);
            }

            if (action != GameTableAction.StartNewRound)
                return;

            // A player who runs out of chips leaves the table.
            players.RemoveAll(player => player.IsOutOfChips);
            if (players.Count == 0)
            {
                ui.ShowCenteredMessage(GameMessages.AllPlayersOutOfChips);
                return;
            }
        }
    }
}
