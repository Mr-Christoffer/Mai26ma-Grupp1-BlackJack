public class Game
{
    private readonly ConsoleUi ui = new();
    private readonly PlayerSessionService playerSessions;
    private readonly GameTableUI gameTable;

    public Game()
    {
        playerSessions = new PlayerSessionService(new GameStorage(), ui);
        gameTable = new GameTableUI(ui);
    }

    public void Run()
    {
        bool running = true;
        while (running)
        {
            string choice = ui.ShowMenu();
            switch (choice)
            {
                case GameMessages.NewGame:
                    StartNewGame();
                    break;
                case GameMessages.LoadGame:
                    LoadGame();
                    break;
                case GameMessages.ShowRules:
                    ui.ShowRules();
                    break;
                case GameMessages.ShowCredits:
                    ui.ShowCredits();
                    break;
                case GameMessages.Exit:
                    running = false;
                    break;
            }
        }

        Console.Clear();
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
        Deck deck = new();
        deck.CreateDeck();
        deck.ShuffleDeck();
        Dealer dealer = new();
        int roundNumber = 1;

        while (true)
        {
            Round round = new(roundNumber++, players, dealer, deck);
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
                    summary, players, playerSessions.SaveGame);
            }

            if (action != GameTableAction.StartNewRound)
                return;
        }
    }
}
