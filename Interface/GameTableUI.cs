using Spectre.Console;

internal enum GameTableAction
{
    RoundCompleted,
    StartNewRound,
    ReturnToMainMenu
}

internal sealed class GameTableUI
{
    private const int TableHeight = 27;
    private const int BlackjackNoticeDurationMs = 2400;

    private readonly ConsoleUi ui;
    private bool tableScreenActive;

    public GameTableUI(ConsoleUi ui)
    {
        this.ui = ui;
    }

    public void ResetSession()
    {
        CloseTableScreen();
        BettingUI.Reset();
    }

    public GameTableAction PlayRound(
        Round round,
        Func<List<Player>, bool> saveGame)
    {
        List<Player> players = round.Players;
        string[] statuses = Enumerable.Repeat("VÄNTAR", players.Count).ToArray();
        int currentPlayerIndex = 0;
        int selectedGameOption = 0;
        int selectedMenuOption = 0;
        bool sideMenuActive = false;
        bool bettingMode = true;
        bool betsComplete = false;
        bool pendingBlackjackAdvance = false;
        bool playing = true;
        GameTableAction action = GameTableAction.ReturnToMainMenu;
        string? notice = $"{players[0].Name.ToUpperInvariant()}: SATSA FÖRST – ENTER LÄGGER MARKER, ESC BEKRÄFTAR";
        DateTime noticeUntil = DateTime.MinValue;
        statuses[0] = "SATSAR";

        void FinishBetting()
        {
            betsComplete = true;
            bettingMode = false;
            currentPlayerIndex = 0;
            selectedGameOption = 0;
            notice = null;

            // Cards were dealt already; revealing them is handled by BuildTable.
            for (int i = 0; i < players.Count; i++)
                statuses[i] = "VÄNTAR";

            pendingBlackjackAdvance = players[0].HasBlackjack;
            statuses[0] = pendingBlackjackAdvance ? "BLACKJACK" : "DIN TUR";
            if (pendingBlackjackAdvance)
            {
                notice = NaturalBlackjackNotice(players[0]);
                noticeUntil = DateTime.UtcNow.AddMilliseconds(BlackjackNoticeDurationMs);
            }
        }

        void AdvancePlayer()
        {
            currentPlayerIndex++;
            selectedGameOption = 0;

            while (currentPlayerIndex < players.Count)
            {
                Player nextPlayer = players[currentPlayerIndex];
                if (nextPlayer.HasBlackjack)
                {
                    statuses[currentPlayerIndex] = "BLACKJACK";
                    notice = NaturalBlackjackNotice(nextPlayer);
                    noticeUntil = DateTime.UtcNow.AddMilliseconds(
                        BlackjackNoticeDurationMs);
                    pendingBlackjackAdvance = true;
                    return;
                }

                statuses[currentPlayerIndex] = "DIN TUR";
                return;
            }

            action = GameTableAction.RoundCompleted;
            playing = false;
        }

        ui.CheckWidth(TableHeight);
        OpenTableScreen();
        try
        {
            while (playing)
            {
                bool saveRequested = false;
                {
                        DrawTable(BuildTable(
                            round, statuses, notice, selectedGameOption,
                            selectedMenuOption, sideMenuActive,
                            roundFinished: false, bettingMode: bettingMode, cardsFaceDown: !betsComplete));
                        while (playing && !saveRequested)
                        {

                            int timeout = pendingBlackjackAdvance
                                ? Math.Max(1, (int)(noticeUntil - DateTime.UtcNow)
                                    .TotalMilliseconds)
                                : -1;
                            ConsoleKey key = ReadTableKey(timeout);
                            if (pendingBlackjackAdvance)
                            {
                                if (DateTime.UtcNow < noticeUntil)
                                    continue;

                                notice = null;
                                pendingBlackjackAdvance = false;
                                AdvancePlayer();
                                if (playing)
                                    DrawTable(BuildTable(
                                        round, statuses, notice, selectedGameOption,
                                        selectedMenuOption, sideMenuActive,
                                        roundFinished: false, bettingMode: bettingMode, cardsFaceDown: !betsComplete));
                                continue;
                            }

                            if (key == ConsoleKey.NoName)
                                continue;

                            if (!betsComplete)
                            {
                                Player bettingPlayer = players[currentPlayerIndex];
                                if (key == ConsoleKey.LeftArrow ||
                                    key == ConsoleKey.RightArrow ||
                                    key == ConsoleKey.Enter ||
                                    key == ConsoleKey.Backspace)
                                {
                                    BettingUI.HandleKey(key, bettingPlayer);
                                }
                                else if (key == ConsoleKey.Escape ||
                                         key == ConsoleKey.UpArrow ||
                                         key == ConsoleKey.DownArrow)
                                {
                                    // Confirm the current player's bet before proceeding.
                                    if (bettingPlayer.CurrentBet > 0)
                                    {
                                        statuses[currentPlayerIndex] = "SATSAT";
                                        currentPlayerIndex++;
                                        if (currentPlayerIndex == players.Count)
                                            FinishBetting();
                                        else
                                        {
                                            statuses[currentPlayerIndex] = "SATSAR";
                                            notice = $"{players[currentPlayerIndex].Name.ToUpperInvariant()}: SATSA FÖRST – ENTER LÄGGER MARKER, ESC BEKRÄFTAR";
                                        }
                                    }
                                    else
                                        notice = "DU MÅSTE SATSA MINST EN MARKER FÖRST!";
                                }

                                DrawTable(BuildTable(
                                    round, statuses, notice, selectedGameOption,
                                    selectedMenuOption, sideMenuActive,
                                    roundFinished: false, bettingMode: bettingMode, cardsFaceDown: !betsComplete));
                                continue;
                            }

                            if (bettingMode)
                            {
                                if (key == ConsoleKey.Escape ||
                                    key == ConsoleKey.UpArrow ||
                                    key == ConsoleKey.DownArrow)
                                    bettingMode = false;
                                else if (key == ConsoleKey.LeftArrow ||
                                         key == ConsoleKey.RightArrow ||
                                         key == ConsoleKey.Enter ||
                                         key == ConsoleKey.Backspace)
                                    BettingUI.HandleKey(key, players[currentPlayerIndex]);

                                if (bettingMode)
                                {
                                    DrawTable(BuildTable(
                                        round, statuses, notice, selectedGameOption,
                                        selectedMenuOption, sideMenuActive,
                                        roundFinished: false, bettingMode: bettingMode, cardsFaceDown: !betsComplete));
                                    continue;
                                }
                            }

                            switch (key)
                            {
                                case ConsoleKey.Escape:
                                    sideMenuActive = false;
                                    selectedGameOption = 0;
                                    break;
                                case ConsoleKey.LeftArrow:
                                    sideMenuActive = false;
                                    selectedGameOption = Math.Max(
                                        0, selectedGameOption - 1);
                                    break;
                                case ConsoleKey.RightArrow:
                                    sideMenuActive = false;
                                    selectedGameOption = Math.Min(
                                        GameMessages.GameOptions.Length - 1,
                                        selectedGameOption + 1);
                                    break;
                                case ConsoleKey.UpArrow:
                                    if (!sideMenuActive)
                                    {
                                        sideMenuActive = true;
                                        selectedMenuOption = 0;
                                    }
                                    else
                                    {
                                        selectedMenuOption =
                                            (selectedMenuOption - 1 +
                                             GameMessages.InGameMenuOptions.Length) %
                                            GameMessages.InGameMenuOptions.Length;
                                    }
                                    break;
                                case ConsoleKey.DownArrow:
                                    if (sideMenuActive)
                                    {
                                        if (selectedMenuOption <
                                            GameMessages.InGameMenuOptions.Length - 1)
                                        {
                                            selectedMenuOption++;
                                        }
                                        else
                                        {
                                            sideMenuActive = false;
                                            selectedGameOption = 0;
                                        }
                                    }
                                    break;
                                case ConsoleKey.Enter:
                                    if (sideMenuActive)
                                    {
                                        if (selectedMenuOption == 0)
                                        {
                                            action = GameTableAction.StartNewRound;
                                            playing = false;
                                        }
                                        else if (selectedMenuOption == 1)
                                        {
                                            action = GameTableAction.ReturnToMainMenu;
                                            playing = false;
                                        }
                                        else if (selectedMenuOption == 2)
                                        {
                                            saveRequested = true;
                                        }
                                    }
                                    else if (selectedGameOption == 2)
                                    {
                                        notice = "INSATSEN ÄR REDAN BEKRÄFTAD FÖR DENNA RUNDA.";
                                    }
                                    else
                                    {
                                        Player player = players[currentPlayerIndex];
                                        if (player.CurrentBet <= 0)
                                        {
                                            notice = "DU MÅSTE SATSA INNAN DU KAN SPELA!";
                                            break;
                                        }
                                        if (selectedGameOption == 0)
                                            player.ReceiveCard(round.Deck);

                                        if (selectedGameOption == 1 ||
                                            player.IsBust ||
                                            player.Score == Participant.BlackjackValue)
                                        {
                                            statuses[currentPlayerIndex] =
                                                GetTurnStatus(player, selectedGameOption);
                                            AdvancePlayer();
                                        }
                                    }
                                    break;
                            }

                            if (playing && !saveRequested)
                                DrawTable(BuildTable(
                                    round, statuses, notice, selectedGameOption,
                                    selectedMenuOption, sideMenuActive,
                                    roundFinished: false, bettingMode: bettingMode, cardsFaceDown: !betsComplete));
                        }
                    }

                if (saveRequested && saveGame(players))
                {
                    action = GameTableAction.ReturnToMainMenu;
                    playing = false;
                }
            }
        }
        catch
        {
            CloseTableScreen();
            throw;
        }
        finally
        {
            // Keep the same terminal screen for the result and the next round.
            if (action == GameTableAction.ReturnToMainMenu)
                CloseTableScreen();
        }

        return action;
    }

    public GameTableAction ShowRoundEndMenu(
        Round round,
        string summary,
        Func<List<Player>, bool> saveGame)
    {
        List<Player> players = round.Players;
        string[] statuses = players
            .Select(player => OutcomeStatus(round.Outcomes[player]))
            .ToArray();
        int selectedMenuOption = 0;
        GameTableAction action = GameTableAction.ReturnToMainMenu;
        string? notice = summary.Replace(
            Environment.NewLine, "  •  ", StringComparison.Ordinal);

        ui.CheckWidth(TableHeight);
        OpenTableScreen();
        try
        {
            bool finished = false;
            while (!finished)
            {
                bool saveRequested = false;
                {
                        DrawTable(BuildTable(
                            round, statuses, notice, selectedGameOption: 0,
                            selectedMenuOption: selectedMenuOption,
                            sideMenuActive: true,
                            roundFinished: true, bettingMode: false));
                        while (!finished && !saveRequested)
                        {

                            ConsoleKey key = ReadTableKey();
                            if (key == ConsoleKey.UpArrow)
                            {
                                selectedMenuOption =
                                    (selectedMenuOption - 1 +
                                     GameMessages.InGameMenuOptions.Length) %
                                    GameMessages.InGameMenuOptions.Length;
                            }
                            else if (key == ConsoleKey.DownArrow)
                            {
                                selectedMenuOption =
                                    (selectedMenuOption + 1) %
                                    GameMessages.InGameMenuOptions.Length;
                            }
                            else if (key == ConsoleKey.Enter)
                            {
                                if (selectedMenuOption == 0)
                                {
                                    action = GameTableAction.StartNewRound;
                                    finished = true;
                                }
                                else if (selectedMenuOption == 1)
                                {
                                    action = GameTableAction.ReturnToMainMenu;
                                    finished = true;
                                }
                                else if (selectedMenuOption == 2)
                                {
                                    saveRequested = true;
                                }
                            }

                            if (!finished && !saveRequested)
                            {
                                DrawTable(BuildTable(
                                    round, statuses, notice, selectedGameOption: 0,
                                    selectedMenuOption: selectedMenuOption,
                                    sideMenuActive: true,
                                    roundFinished: true, bettingMode: false));
                            }
                        }
                    }

                if (saveRequested && saveGame(players))
                {
                    action = GameTableAction.ReturnToMainMenu;
                    finished = true;
                }
            }
        }
        catch
        {
            CloseTableScreen();
            throw;
        }
        finally
        {
            // Starting a new round redraws in place instead of flashing.
            if (action != GameTableAction.StartNewRound)
                CloseTableScreen();
        }

        return action;
    }

    // Use a fixed screen instead of Spectre.Live, which can move the cursor
    // and cause flicker when its display region is recreated.
    private void OpenTableScreen()
    {
        if (tableScreenActive)
            return;

        Console.Write("\u001b[?1049h\u001b[?25l\u001b[H\u001b[2J");
        Console.Out.Flush();
        tableScreenActive = true;
    }

    private void CloseTableScreen()
    {
        if (!tableScreenActive)
            return;

        Console.Write("\u001b[?25h\u001b[?1049l");
        Console.Out.Flush();
        tableScreenActive = false;
    }

    private static void DrawTable(Layout layout)
    {
        // Synchronize the frame where supported; do not clear between frames.
        Console.Write("\u001b[?2026h\u001b[H\u001b[?25l");
        try
        {
            AnsiConsole.Write(layout);
        }
        finally
        {
            Console.Write("\u001b[?2026l\u001b[?25l");
            Console.Out.Flush();
        }
    }

    private Layout BuildTable(
        Round round,
        string[] statuses,
        string? notice,
        int selectedGameOption,
        int selectedMenuOption,
        bool sideMenuActive,
        bool roundFinished,
        bool bettingMode,
        bool cardsFaceDown = false)
    {
        const int tableHeight = TableHeight - 3;
        var layout = new Layout("Root")
            .Size(TableHeight)
            .SplitColumns(
                new Layout("Table").Ratio(4),
                new Layout("Menu").Ratio(1));
        layout["Table"].SplitRows(
            new Layout("BlackjackTable").Size(tableHeight),
            new Layout("PlayerActions").Size(3));
        layout["BlackjackTable"].Update(
            ui.BuildBlackjackTable(
                round.Players, round.Dealer, tableHeight, notice, statuses, cardsFaceDown));
        layout["PlayerActions"].Update(ui.BuildPlayerActions(
            GameMessages.GameOptions, selectedGameOption,
            sideMenuActive, roundFinished, bettingMode));

        layout["Menu"].SplitRows(
            new Layout("MenuContent").Size(TableHeight - 3),
            new Layout("MenuSpace").Ratio(1));
        layout["MenuContent"].Update(ui.BuildGameMenu(
            GameMessages.InGameMenuOptions, selectedMenuOption,
            round.Players, sideMenuActive, roundFinished, statuses));
        layout["MenuSpace"].Update(new Text(""));
        return layout;
    }

    private ConsoleKey ReadTableKey(int timeoutMs = -1)
    {
        DateTime until = timeoutMs < 0
            ? DateTime.MaxValue
            : DateTime.UtcNow.AddMilliseconds(timeoutMs);

        while (true)
        {
            if (Console.WindowWidth < ConsoleUi.MinWidth ||
                Console.WindowHeight < ConsoleUi.MinHeight)
            {
                ui.CheckWidth();
                Console.Write("\u001b[?25l");
                return ConsoleKey.NoName;
            }

            if (Console.KeyAvailable)
                return Console.ReadKey(true).Key;
            if (DateTime.UtcNow >= until)
                return ConsoleKey.NoName;

            Thread.Sleep(35);
        }
    }

    private static string GetTurnStatus(Player player, int selectedGameOption)
    {
        if (player.IsBust)
            return "TJOCK";
        if (player.HasBlackjack)
            return "BLACKJACK";
        if (player.Score == Participant.BlackjackValue)
            return "21 POÄNG";
        return selectedGameOption == 1 ? "STANNAT" : "KLAR";
    }

    private static string OutcomeStatus(Outcome outcome) => outcome switch
    {
        Outcome.Blackjack or Outcome.Win => "VANN",
        Outcome.Lose => "FÖRLORADE",
        Outcome.Push => "OAVGJORT",
        _ => "KLAR"
    };

    private static string NaturalBlackjackNotice(Player player) =>
        $"{player.Name.ToUpperInvariant()} – NATURAL BLACKJACK!";
}
