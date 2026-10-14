using Spectre.Console;

public partial class ConsoleUi
{
    public string AskPlayerNameCentered()
    {
        AnsiConsole.Clear();
        const string prompt = GameMessages.EnterPlayerNamePrompt;
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

    public string? SelectSavedPlayer(
        IReadOnlyDictionary<string, PlayerStatistics> results)
    {
        List<string> names = new(results.Keys);
        names.Sort(StringComparer.OrdinalIgnoreCase);
        int selected = 0;
        bool cancelled = false;

        AnsiConsole.Clear();
        AnsiConsole.Live(BuildSavedPlayersMenu(names, results, selected))
            .Start(context =>
            {
                context.Refresh();
                while (true)
                {
                    ConsoleKey key = Console.ReadKey(true).Key;
                    if (key == ConsoleKey.UpArrow)
                        selected = (selected - 1 + names.Count) % names.Count;
                    else if (key == ConsoleKey.DownArrow)
                        selected = (selected + 1) % names.Count;
                    else if (key == ConsoleKey.Escape)
                    {
                        cancelled = true;
                        break;
                    }
                    else if (key == ConsoleKey.Enter)
                        break;
                    else
                        continue;

                    context.UpdateTarget(
                        BuildSavedPlayersMenu(names, results, selected));
                    context.Refresh();
                }
            });

        return cancelled ? null : names[selected];
    }

    private static Spectre.Console.Rendering.IRenderable BuildSavedPlayersMenu(
        List<string> names,
        IReadOnlyDictionary<string, PlayerStatistics> results,
        int selected)
    {
        List<Spectre.Console.Rendering.IRenderable> rows = new()
        {
            new Align(
                new Markup(GameMessages.GameTitleMarkup),
                HorizontalAlignment.Center),
            new Align(
                new Markup(GameMessages.GameSubtitleMarkup),
                HorizontalAlignment.Center),
            new Text(""),
            new Rule(GameMessages.SavedPlayersTitleMarkup).RuleStyle(Color.Gold1),
            new Text("")
        };

        for (int i = 0; i < names.Count; i++)
        {
            string name = names[i];
            PlayerStatistics stats = results[name];
            string label = GameMessages.SavedPlayerStatistics(
                name, stats.Wins, stats.Losses, stats.Pushes);
            string escaped = Markup.Escape(label);
            string line = i == selected
                ? $"[bold white on #961923] ❯ {escaped} [/]"
                : $"[white]   {escaped}[/]";
            rows.Add(new Align(new Markup(line), HorizontalAlignment.Center));
            rows.Add(new Text(""));
        }

        rows.Add(new Rule().RuleStyle(Color.Gold1));
        rows.Add(new Align(
            new Markup(GameMessages.SavedPlayersNavigationMarkup),
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
}
