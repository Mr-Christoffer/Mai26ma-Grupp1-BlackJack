using Spectre.Console;
using Spectre.Console.Rendering;

internal enum GameTableAction
{
    RoundCompleted,
    StartNewRound,
    ReturnToMainMenu
}

internal sealed class GameTableUI
{
    private readonly ConsoleUi ui;

    public GameTableUI(ConsoleUi ui)
    {
        this.ui = ui;
    }

    public GameTableAction PlayRound(Round round, Func<List<Player>, bool> saveGame)
    {
        List<Player> players = round.Players;
        string[] gameOptions = GameMessages.GameOptions;
        string[] menuOptions = GameMessages.InGameMenuOptions;
        int selectedGameOption = 0;
        int selectedMenuOption = 0;
        bool sideMenuActive = false;
        bool bettingMode = false;
        int currentPlayerIndex = 0;
        bool playing = true;
        GameTableAction action = GameTableAction.ReturnToMainMenu;

        AnsiConsole.Clear();
        try
        {
            Console.CursorVisible = false;
            while (playing)
            {
                Console.SetCursorPosition(0, 0);
                const int totalHeight = 27;
                int tableHeight = totalHeight - 3;
                var layout = new Layout("Root")
                    .Size(totalHeight)
                    .SplitColumns(
                        new Layout("Table").Ratio(4),
                        new Layout("Menu").Ratio(1));
                layout["Table"].SplitRows(
                    new Layout("BlackjackTable").Size(tableHeight),
                    new Layout("PlayerActions").Size(3));
                layout["BlackjackTable"].Update(
                    ui.BuildBlackjackTable(players, round.Dealer, tableHeight));
                layout["PlayerActions"].Update(
                    BuildPlayerActions(
                        gameOptions, selectedGameOption,
                        sideMenuActive, bettingMode));

                int menuHeight = Math.Min(totalHeight - 3, 15 + players.Count);
                layout["Menu"].SplitRows(
                    new Layout("MenuContent").Size(menuHeight),
                    new Layout("MenuSpace").Ratio(1));
                layout["MenuContent"].Update(
                    BuildGameMenu(menuOptions, selectedMenuOption,
                        players, sideMenuActive));
                layout["MenuSpace"].Update(new Text(""));
                AnsiConsole.Write(layout);
                Console.Write("\u001b[J");

                ConsoleKey key = Console.ReadKey(true).Key;
                if (bettingMode)
                {
                    if (key == ConsoleKey.Escape)
                        bettingMode = false;
                    else if (key == ConsoleKey.LeftArrow ||
                             key == ConsoleKey.RightArrow ||
                             key == ConsoleKey.Enter)
                        BettingUI.HandleKey(key);
                    continue;
                }

                switch (key)
                {
                    case ConsoleKey.LeftArrow:
                        sideMenuActive = false;
                        selectedGameOption = Math.Max(0, selectedGameOption - 1);
                        break;
                    case ConsoleKey.RightArrow:
                        sideMenuActive = false;
                        selectedGameOption = Math.Min(
                            gameOptions.Length - 1, selectedGameOption + 1);
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
                                (selectedMenuOption - 1 + menuOptions.Length)
                                % menuOptions.Length;
                        }
                        break;
                    case ConsoleKey.DownArrow:
                        if (sideMenuActive)
                        {
                            if (selectedMenuOption < menuOptions.Length - 1)
                                selectedMenuOption++;
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
                            else if (selectedMenuOption == 2 && saveGame(players))
                            {
                                action = GameTableAction.ReturnToMainMenu;
                                playing = false;
                            }
                        }
                        else if (selectedGameOption == 2)
                        {
                            bettingMode = true;
                        }
                        else
                        {
                            Player currentPlayer = players[currentPlayerIndex];
                            if (selectedGameOption == 0)
                                currentPlayer.ReceiveCard(round.Deck);

                            if (selectedGameOption == 1 ||
                                currentPlayer.IsBust ||
                                currentPlayer.Score == Participant.BlackjackValue)
                            {
                                if (currentPlayerIndex < players.Count - 1)
                                {
                                    currentPlayerIndex++;
                                    selectedGameOption = 0;
                                }
                                else
                                {
                                    action = GameTableAction.RoundCompleted;
                                    playing = false;
                                }
                            }
                        }
                        break;
                }
            }
        }
        finally
        {
            Console.CursorVisible = true;
            AnsiConsole.Clear();
        }

        return action;
    }

    public GameTableAction ShowRoundEndMenu(
        string summary,
        List<Player> players,
        Func<List<Player>, bool> saveGame)
    {
        string[] options = GameMessages.RoundEndMenuOptions;
        int selected = 0;

        while (true)
        {
            AnsiConsole.Clear();
            bool confirmed = false;
            AnsiConsole.Live(BuildRoundEndPanel(summary, options, selected))
                .Start(context =>
                {
                    context.Refresh();
                    while (!confirmed)
                    {
                        ConsoleKey key = Console.ReadKey(true).Key;
                        if (key == ConsoleKey.UpArrow)
                            selected = (selected - 1 + options.Length) % options.Length;
                        else if (key == ConsoleKey.DownArrow)
                            selected = (selected + 1) % options.Length;
                        else if (key == ConsoleKey.Enter)
                        {
                            confirmed = true;
                            break;
                        }
                        else
                            continue;

                        context.UpdateTarget(
                            BuildRoundEndPanel(summary, options, selected));
                        context.Refresh();
                    }
                });

            if (selected == 0)
                return GameTableAction.StartNewRound;
            if (selected == 1)
                return GameTableAction.ReturnToMainMenu;
            if (saveGame(players))
                return GameTableAction.ReturnToMainMenu;
        }
    }

    private static IRenderable BuildRoundEndPanel(
        string summary, string[] options, int selected)
    {
        var rows = new List<IRenderable>
        {
            new Align(new Markup(GameMessages.GameTitleMarkup), HorizontalAlignment.Center),
            new Align(new Markup(GameMessages.GameSubtitleMarkup), HorizontalAlignment.Center),
            new Rule(GameMessages.ResultTitleMarkup).RuleStyle(Color.Gold1),
            new Text("")
        };
        foreach (string result in summary.Split('\n'))
        {
            string color = GameMessages.IsLossOutcome(result) ? "red" :
                           GameMessages.IsWinOutcome(result) ? "green1" : "gold1";
            rows.Add(new Align(
                new Markup($"[{color}]{Markup.Escape(result)}[/]"),
                HorizontalAlignment.Center));
        }

        rows.Add(new Text(""));
        rows.Add(new Align(
            new Markup($"[bold gold1]{GameMessages.NewRoundPrompt}[/]"),
            HorizontalAlignment.Center));
        rows.Add(new Text(""));
        for (int i = 0; i < options.Length; i++)
        {
            string label = Markup.Escape(options[i]);
            string line = i == selected
                ? $"[bold white on #961923] ❯ {label} [/]"
                : $"[white]   {label} [/]";
            rows.Add(new Align(new Markup(line), HorizontalAlignment.Center));
            rows.Add(new Text(""));
        }

        rows.Add(new Rule().RuleStyle(Color.Gold1));
        rows.Add(new Align(
            new Markup(GameMessages.RoundEndNavigationMarkup),
            HorizontalAlignment.Center));
        var panel = new Panel(new Rows(rows))
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Gold1)
            .Expand();
        int terminalWidth = Math.Max(1, Console.WindowWidth);
        int panelWidth = Math.Min(68, terminalWidth);
        int left = (terminalWidth - panelWidth) / 2;
        return new Padder(panel)
            .PadLeft(left)
            .PadRight(terminalWidth - panelWidth - left);
    }

    private static Panel BuildPlayerActions(
        string[] options,
        int selectedOption,
        bool sideMenuActive,
        bool bettingMode)
    {
        var actions = new Grid();
        for (int i = 0; i < options.Length; i++)
            actions.AddColumn(new GridColumn().NoWrap().PadRight(2));

        var buttons = new IRenderable[options.Length];
        for (int i = 0; i < options.Length; i++)
        {
            string label = Markup.Escape(options[i]);
            bool selected = !sideMenuActive && i == selectedOption;
            string highlight = bettingMode && i == 2
                ? "[bold black on gold1]"
                : "[bold white on #961923]";
            buttons[i] = new Markup(selected
                ? $"{highlight} ❯ {label} [/]"
                : $"[white]   {label}[/]");
        }

        actions.AddRow(buttons);
        return new Panel(new Align(actions, HorizontalAlignment.Center))
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Gold1)
            .Expand();
    }

    private static Panel BuildGameMenu(
        string[] menuOptions,
        int selectedOption,
        List<Player> players,
        bool sideMenuActive)
    {
        List<IRenderable> content = new()
        {
            new Align(
                new Markup(GameMessages.GameMenuTitleMarkup),
                HorizontalAlignment.Center),
            new Text("")
        };

        string[] shortOptions = GameMessages.InGameMenuOptions;
        for (int i = 0; i < menuOptions.Length; i++)
        {
            string label = Markup.Escape(
                i < shortOptions.Length ? shortOptions[i] : menuOptions[i]);
            content.Add(new Markup(sideMenuActive && i == selectedOption
                ? $"[bold white on #961923] ❯ {label} [/]"
                : $"[white]   {label}[/]"));
            content.Add(new Text(""));
        }

        content.Add(new Rule(GameMessages.PointsTitleMarkup).RuleStyle(Color.Gold1));
        var scoreTable = new Table().NoBorder().HideHeaders().Expand();
        scoreTable.AddColumn(new TableColumn("").LeftAligned());
        scoreTable.AddColumn(new TableColumn("").RightAligned());
        foreach (Player player in players)
        {
            scoreTable.AddRow(
                GameMessages.ScoreMarkup(Markup.Escape(player.Name)),
                GameMessages.PlayerScoreMarkup(player.Score));
        }

        content.Add(scoreTable);
        content.Add(new Text(""));
        content.Add(new Rule(GameMessages.ChipsTitleMarkup).RuleStyle(Color.Gold1));
        var moneyTable = new Table().NoBorder().HideHeaders().Expand();
        moneyTable.AddColumn(new TableColumn("").LeftAligned());
        moneyTable.AddColumn(new TableColumn("").RightAligned());
        moneyTable.AddRow(
            GameMessages.BalanceMarkup,
            GameMessages.Balance(BettingUI.Balance));
        moneyTable.AddRow(
            GameMessages.CurrentBetMarkup,
            GameMessages.CurrentBet(BettingUI.CurrentBet));
        content.Add(moneyTable);

        return new Panel(new Rows(content))
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Gold1)
            .Expand();
    }
}
