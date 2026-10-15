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
        Console.Write("\u001b[?25l");
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
        Console.Write("\u001b[?25l");
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
        // Hide the cursor before entering the first name prompt.
        Console.Write("\u001b[?25l");
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

    // Returns null if the player presses Esc.
    public string? AskPlayerNameCentered(int playerNumber)
    {
        var name = new System.Text.StringBuilder();
        bool showError = false;
        // Hide the hardware cursor as well as the ANSI terminal cursor.
        void HideInputCursor()
        {
            try
            {
                if (!Console.IsOutputRedirected)
                    Console.CursorVisible = false;
            }
            catch (IOException) { }
            catch (PlatformNotSupportedException) { }
            Console.Write("\u001b[?25l");
        }

        // Hide the cursor before the first clear and keep it hidden afterwards.
        HideInputCursor();
        AnsiConsole.Clear();
        HideInputCursor();

        try
        {
            while (true)
            {
                int width = Math.Max(1, Console.WindowWidth);
                int height = Math.Max(1, Console.WindowHeight);
                string prompt = $"Ange namn för spelare {playerNumber}:";
                int top = Math.Max(0, height / 2 - 2);
                int left = Math.Max(0, (width - 44) / 2);
                int available = Math.Max(0, width - left - 1);
                int maxNameLength = Math.Max(1,
                    Math.Min(24, Math.Max(1, available - prompt.Length - 1)));

                // Draw all three lines in one terminal write to minimize cursor flashes.
                var frame = new System.Text.StringBuilder("\u001b[?25l\u001b[?2026h");

                void AppendLine(int row, string text, string color)
                {
                    if (row >= height || available == 0)
                        return;

                    string visible = text.Length > available
                        ? text[..available]
                        : text;
                    frame.Append($"\u001b[{row + 1};{left + 1}H\u001b[0K");
                    frame.Append(color);
                    frame.Append(visible);
                    frame.Append("\u001b[0m");
                }

                AppendLine(top, prompt + " " + name, "\u001b[93m");
                AppendLine(top + 1,
                    "Enter: Bekräfta    Esc: Tillbaka till huvudmenyn", "\u001b[90m");
                AppendLine(top + 2,
                    showError ? "Du måste ange ett namn. Försök igen." : "",
                    "\u001b[91m");
                frame.Append("\u001b[?2026l\u001b[?25l");
                Console.Write(frame.ToString());
                HideInputCursor();

                ConsoleKeyInfo key = Console.ReadKey(intercept: true);
                HideInputCursor();
                if (key.Key == ConsoleKey.Escape)
                    return null;
                if (key.Key == ConsoleKey.Enter)
                {
                    string entered = name.ToString().Trim();
                    if (entered.Length > 0)
                        return entered;
                    showError = true;
                }
                else if (key.Key == ConsoleKey.Backspace && name.Length > 0)
                {
                    name.Length--;
                    showError = false;
                }
                else if (!char.IsControl(key.KeyChar) && name.Length < maxNameLength)
                {
                    name.Append(key.KeyChar);
                    showError = false;
                }
            }
        }
        finally
        {
            // The menus also use a hidden cursor.
            HideInputCursor();
        }
    }

    public Spectre.Console.Rendering.IRenderable BuildSavedPlayersMenu(
        List<string> names, Dictionary<string, PlayerStatistics> results,
        int selected, bool confirmingDelete)
    {
        var rows = new List<Spectre.Console.Rendering.IRenderable>
        {
            new Align(new Markup(GameMessages.GameTitleMarkup), HorizontalAlignment.Center),
            new Align(new Markup(GameMessages.GameSubtitleMarkup), HorizontalAlignment.Center),
            new Text(""),
            new Rule("[bold gold1]♠ LADDA SPEL ♠[/]").RuleStyle(Color.Gold1),
            new Align(new Markup($"[grey]SPARADE SPELARE  ({names.Count}/3)[/]"), HorizontalAlignment.Center),
            new Text("")
        };
        for (int i = 0; i < names.Count; i++)
        {
            string label = Markup.Escape(names[i]);
            string line = i == selected
                ? $"[bold white on #961923] ❯  {label}  [/]"
                : $"[white]    {label}[/]";
            rows.Add(new Align(new Markup(line), HorizontalAlignment.Center));
        }
        for (int i = names.Count; i < 3; i++)
            rows.Add(new Align(new Markup("[grey]    — Tom plats —[/]"), HorizontalAlignment.Center));
        PlayerStatistics stats = results[names[selected]];
        rows.Add(new Text(""));
        rows.Add(new Rule("[gold1]SPELARSTATISTIK[/]").RuleStyle(Color.Gold1));
        var table = new Table().NoBorder().HideHeaders();
        table.AddColumn(new TableColumn("").RightAligned());
        table.AddColumn(new TableColumn("").LeftAligned());
        table.AddColumn(new TableColumn("").RightAligned());
        table.AddColumn(new TableColumn("").LeftAligned());
        table.AddRow("[white]Vinster[/]", $"[green1]{stats.Wins}[/]", "[white]Förluster[/]", $"[red]{stats.Losses}[/]");
        table.AddRow("[white]Oavgjorda[/]", $"[gold1]{stats.Pushes}[/]", "[white]Rundor[/]", $"[deepskyblue1]{stats.Wins + stats.Losses + stats.Pushes}[/]");
        rows.Add(new Align(table, HorizontalAlignment.Center));
        rows.Add(new Text(""));
        if (confirmingDelete)
        {
            rows.Add(new Align(new Markup($"[bold red]Ta bort {Markup.Escape(names[selected])} permanent?[/]"), HorizontalAlignment.Center));
            rows.Add(new Align(new Markup("[white]Enter: Ja     Esc: Nej[/]"), HorizontalAlignment.Center));
        }
        else
        {
            rows.Add(new Align(new Markup("[green1]Enter[/] [grey]Ladda spel[/]   [red]Delete[/] [grey]Ta bort[/]"), HorizontalAlignment.Center));
            rows.Add(new Align(new Markup("[grey]↑ / ↓: Välj spelare     Esc: Tillbaka[/]"), HorizontalAlignment.Center));
        }
        rows.Add(new Rule().RuleStyle(Color.Gold1));
        var panel = new Panel(new Rows(rows)).Border(BoxBorder.Rounded)
            .BorderColor(Color.Gold1).Expand();
        int terminalWidth = Math.Max(1, Console.WindowWidth);
        int panelWidth = Math.Min(72, terminalWidth);
        int left = (terminalWidth - panelWidth) / 2;
        return new Padder(panel).PadLeft(left).PadRight(terminalWidth - panelWidth - left);
    }
}
