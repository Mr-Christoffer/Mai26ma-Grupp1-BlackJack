using System;
using System.Collections.Generic;
using Spectre.Console;
using Spectre.Console.Rendering;

public partial class ConsoleUi
{
    private readonly string[] menuOptions = GameMessages.MainMenuOptions;

    // Main menu with arrow-key navigation.
    public string ShowMenu(string? notice = null)
    {
        int selectedOption = 0;
        string choice = "";
        AnsiConsole.Clear();
        AnsiConsole.Live(CreateMenu(selectedOption, notice)).Start(context =>
        {
            context.Refresh();
            while (true)
            {
                ConsoleKey key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.UpArrow)
                    selectedOption = (selectedOption - 1 + menuOptions.Length) % menuOptions.Length;
                else if (key == ConsoleKey.DownArrow)
                    selectedOption = (selectedOption + 1) % menuOptions.Length;
                else if (key == ConsoleKey.Enter)
                {
                    choice = menuOptions[selectedOption];
                    break;
                }
                else continue;

                context.UpdateTarget(CreateMenu(selectedOption, notice));
                context.Refresh();
            }
        });
        return choice;
    }

    // Player count selection, 1–5 players.
    public int AskPlayerCountCentered()
    {
        int selectedOption = 0;
        int playerCount = 1;
        AnsiConsole.Clear();
        AnsiConsole.Live(CreatePlayerCountMenu(selectedOption)).Start(context =>
        {
            context.Refresh();
            while (true)
            {
                ConsoleKey key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.UpArrow)
                    selectedOption = (selectedOption + 4) % 5;
                else if (key == ConsoleKey.DownArrow)
                    selectedOption = (selectedOption + 1) % 5;
                else if (key == ConsoleKey.Enter)
                {
                    playerCount = selectedOption + 1;
                    break;
                }
                else continue;

                context.UpdateTarget(CreatePlayerCountMenu(selectedOption));
                context.Refresh();
            }
        });
        return playerCount;
    }

    private IRenderable CreatePlayerCountMenu(int selectedOption)
    {
        List<IRenderable> rows = new()
        {
            new Align(new Markup(GameMessages.GameTitleMarkup), HorizontalAlignment.Center),
            new Align(new Markup(GameMessages.GameSubtitleMarkup), HorizontalAlignment.Center),
            new Text(""),
            new Rule(GameMessages.PlayerCountTitleMarkup).RuleStyle(Color.Gold1),
            new Text("")
        };
        for (int i = 0; i < 5; i++)
        {
            string label = string.Format(GameMessages.PlayerCountFormat, i + 1).PadRight(17);
            string line = i == selectedOption
                ? $"[bold white on #961923] ❯ {label} [/]"
                : $"[white]   {label} [/]";
            rows.Add(new Align(new Markup(line), HorizontalAlignment.Center));
            rows.Add(new Text(""));
        }
        rows.Add(new Rule().RuleStyle(Color.Gold1));
        rows.Add(new Align(new Markup(GameMessages.MenuNavigationMarkup), HorizontalAlignment.Center));
        return CenterMenuPanel(rows);
    }

    private IRenderable CreateMenu(int selectedOption, string? notice)
    {
        List<IRenderable> rows = new()
        {
            new Align(new Markup(GameMessages.GameTitleMarkup), HorizontalAlignment.Center),
            new Align(new Markup(GameMessages.GameSubtitleMarkup), HorizontalAlignment.Center),
            new Text(""),
            new Rule(GameMessages.MainMenuTitleMarkup).RuleStyle(Color.Gold1),
            new Text("")
        };
        if (notice is not null)
        {
            rows.Add(new Align(
                new Markup($"[bold yellow]{Markup.Escape(notice)}[/]"),
                HorizontalAlignment.Center));
            rows.Add(new Text(""));
        }

        for (int i = 0; i < menuOptions.Length; i++)
        {
            string option = Markup.Escape(menuOptions[i]);
            string paddedOption = option.PadRight(21);
            string line = i == selectedOption
                ? $"[bold white on #961923] ❯ {paddedOption} [/]"
                : $"[white]   {paddedOption} [/]";
            rows.Add(new Align(new Markup(line), HorizontalAlignment.Center));
            rows.Add(new Text(""));
        }
        rows.Add(new Rule().RuleStyle(Color.Gold1));
        rows.Add(new Align(new Markup(GameMessages.MenuNavigationMarkup), HorizontalAlignment.Center));
        return CenterMenuPanel(rows);
    }

    private IRenderable CenterMenuPanel(List<IRenderable> rows)
    {
        var panel = new Panel(new Rows(rows))
            .Border(BoxBorder.Rounded).BorderColor(Color.Gold1).Expand();
        int terminalWidth = Math.Max(1, Console.WindowWidth);
        int menuWidth = Math.Min(68, terminalWidth);
        int leftPadding = (terminalWidth - menuWidth) / 2;
        return new Padder(panel).PadLeft(leftPadding)
            .PadRight(terminalWidth - menuWidth - leftPadding);
    }

    // Builds the horizontal Hit, Stand and Bet buttons.
    public Panel BuildPlayerActions(
        string[] options,
        int selectedOption,
        bool sideMenuActive,
        bool roundFinished,
        bool bettingMode)
    {
        var actions = new Grid();
        for (int i = 0; i < options.Length; i++)
            actions.AddColumn(new GridColumn().NoWrap().PadRight(2));
        var buttons = new Spectre.Console.Rendering.IRenderable[options.Length];
        for (int i = 0; i < options.Length; i++)
        {
            string label = Markup.Escape(options[i]);
            bool selected = !sideMenuActive &&
                            !roundFinished && i == selectedOption;
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

    // Shows the menu, saved statistics, live bet and controls on the right.
    internal Panel BuildGameMenu(string[] menuOptions, int selectedOption, List<Player> players, bool sideMenuActive, bool roundFinished, string[] playerStatuses)
    {
        var content = new List<Spectre.Console.Rendering.IRenderable>
        {
            new Align(new Markup(GameMessages.GameMenuTitleMarkup), HorizontalAlignment.Center)
        };
        for (int i = 0; i < menuOptions.Length; i++)
        {
            string label = Markup.Escape(menuOptions[i]);
            content.Add(new Markup((sideMenuActive || roundFinished) && i == selectedOption
                ? $"[bold white on #961923] ❯ {label} [/]"
                : $"[white]   {label}[/]"));
        }
        content.Add(new Rule("[gold1]POÄNG[/]").RuleStyle(Color.Gold1));
        content.Add(new Markup("[grey]SPELARE     P   RESULTAT[/]"));
        for (int i = 0; i < players.Count; i++)
        {
            Player player = players[i];
            string name = player.Name.Length > 10 ? player.Name[..9] + "…" : player.Name;
            string status = playerStatuses[i];
            string statusColor = status == "TJOCK" || status == "FÖRLORADE" ? "red" :
                status == "DIN TUR" || status == "VANN" ? "green1" :
                status == "OAVGJORT" ? "gold1" : "grey";
            content.Add(new Markup(
                $"[white]{Markup.Escape(name.PadRight(10))}[/] " +
                $"[gold1]{player.Score.ToString().PadLeft(2)}[/] " +
                $"[{statusColor}]{Markup.Escape(status.PadLeft(9))}[/]"));
        }
        Player main = players[0];
        content.Add(new Rule("[gold1]STATISTIK[/]").RuleStyle(Color.Gold1));
        content.Add(new Markup($"[green1]V: {main.Wins}[/]  [red]F: {main.Losses}[/]  [gold1]O: {main.Pushes}[/]"));
        content.Add(new Markup($"[white]Rundor: {main.GamesPlayed}[/]"));
        content.Add(new Rule("[gold1]SPELMARKER[/]").RuleStyle(Color.Gold1));
        content.Add(new Markup($"[white]Saldo:[/] [green1]{main.Chips} kr[/]"));
        content.Add(new Markup($"[white]Aktuell insats:[/] [gold1]{main.CurrentBet} kr[/]"));
        content.Add(new Rule("[gold1]STYRNING[/]").RuleStyle(Color.Gold1));
        content.Add(new Markup("[grey]← →  Välj handling[/]"));
        content.Add(new Markup("[grey]↑ ↓  Spelmeny[/]"));
        content.Add(new Markup("[grey]Enter  Bekräfta[/]"));
        content.Add(new Markup("[grey]Vid insats: ← → välj mark[/]"));
        content.Add(new Markup("[grey]Enter satsa, Esc tillbaka[/]"));
        return new Panel(new Rows(content))
            .Border(BoxBorder.Rounded).BorderColor(Color.Gold1).Expand();
    }

    public string AskPlayerNameCentered(int playerNumber)
    {
        AnsiConsole.Clear();
        string prompt = string.Format(
            GameMessages.PlayerNamePromptFormat, playerNumber);
        int width = Math.Max(1, Console.WindowWidth);
        int height = Math.Max(1, Console.WindowHeight);
        int left = Math.Max(0, (width - prompt.Length - 16) / 2);
        int top = Math.Max(0, height / 2);
        try
        {
            Console.SetCursorPosition(left, top);
        }
        catch (ArgumentOutOfRangeException)
        {
            // Use the current cursor position if the terminal is resized.
        }
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write(prompt);
        Console.ResetColor();
        return (Console.ReadLine() ?? "").Trim();
    }

}
