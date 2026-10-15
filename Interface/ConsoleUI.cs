using System;
using System.Collections.Generic;
using Spectre.Console;
using Spectre.Console.Rendering;

public partial class ConsoleUi
{
    // Minimum space for the full Blackjack table and its side menu.
    public const int MinWidth = 100;
    public const int MinHeight = 30;

    // Clears the screen and scrollback in terminals that support ANSI escapes.
    public void HardClear(bool check = true)
    {
        if (check)
            CheckWidth();

        Console.Write("\x1b[2J\x1b[3J\x1b[H");
    }

    // Waits until the terminal has enough columns AND rows for the table.
    public void CheckWidth(int requiredHeight = MinHeight)
    {
        if (Console.IsOutputRedirected || Console.IsInputRedirected)
            return;

        bool warningShown = false;
        while (Console.WindowWidth < MinWidth || Console.WindowHeight < requiredHeight)
        {
            warningShown = true;
            HardClear(false);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("TERMINALEN ÄR FÖR LITEN!");
            Console.ResetColor();
            Console.WriteLine($"Nuvarande storlek: {Console.WindowWidth} kolumner x {Console.WindowHeight} rader");
            Console.WriteLine($"Minsta storlek: {MinWidth} kolumner x {requiredHeight} rader");
            Console.WriteLine("Förstora terminalen och tryck sedan Enter för att fortsätta...");
            Console.ReadLine();
        }

        if (warningShown)
            HardClear(false);
    }

    // Displays the centered Blackjack header.
    public void ShowHeader()
    {
        var header = new Panel(new Rows(
            new Align(new Markup("[bold gold1]♠ BLACKJACK 21 ♠[/]"), HorizontalAlignment.Center),
            new Align(new Markup("[italic green3]Tur kan inte kompileras.[/]"), HorizontalAlignment.Center)))
            .Border(BoxBorder.Rounded).BorderColor(Color.Gold1).Expand();
        AnsiConsole.Write(CenterPanel(header));
    }

    // Centers a panel at up to 68 terminal columns.
    private static IRenderable CenterPanel(Panel panel)
    {
        int terminalWidth = Math.Max(1, Console.WindowWidth);
        int panelWidth = Math.Min(68, terminalWidth);
        int left = (terminalWidth - panelWidth) / 2;
        return new Padder(panel).PadLeft(left).PadRight(terminalWidth - panelWidth - left);
    }

    // Displays a centered message with a return instruction.
    public void ShowCenteredMessage(string message)
    {
        AnsiConsole.Clear();
        var panel = new Panel(new Rows(
            new Align(new Markup("[bold gold1]♠ BLACKJACK 21 ♠[/]"), HorizontalAlignment.Center),
            new Align(new Markup("[italic green3]Tur kan inte kompileras.[/]"), HorizontalAlignment.Center),
            new Text(""),
            new Rule().RuleStyle(Color.Gold1),
            new Text(""),
            new Align(new Markup($"[bold white]{Markup.Escape(message)}[/]"), HorizontalAlignment.Center),
            new Text(""),
            new Align(new Markup("[grey]Tryck på valfri tangent för att återgå.[/]"), HorizontalAlignment.Center)))
            .Border(BoxBorder.Rounded).BorderColor(Color.Gold1).Expand();
        AnsiConsole.Write(CenterPanel(panel));
        Console.ReadKey(true);
    }

    // Displays a message without waiting for input.
    public void ShowMessage(string message)
    {
        AnsiConsole.MarkupLine($"[white]{Markup.Escape(message)}[/]");
    }

    // Displays the game actions.
    public string AskMove()
    {
        return AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("[bold gold1]Vad vill du göra?[/]")
            .PageSize(3)
            .HighlightStyle(new Style(foreground: Color.White, background: new Color(0, 100, 50)))
            .AddChoices("♠ Köpa", "♥ Stanna", "♦ Satsa"));
    }

    // Opens betting mode when the player selects Bet.
    internal string AskMove(List<Player> players, Dealer dealer)
    {
        while (true)
        {
            string choice = AskMove();
            if (choice == "♦ Satsa")
            {
                ShowBettingMode(players, dealer);
                continue;
            }
            return choice;
        }
    }

    // Displays the round result.
    public void ShowResult(string result)
    {
        var panel = new Panel(new Align(
            new Markup($"[bold gold1]{Markup.Escape(result)}[/]"),
            HorizontalAlignment.Center))
            .Border(BoxBorder.Rounded).BorderColor(Color.Gold1).Expand();
        AnsiConsole.Write(CenterPanel(panel));
    }

    // Displays cumulative player statistics loaded from the results file.
    public void ShowStatistics(IReadOnlyDictionary<string, PlayerStatistics> results)
    {
        AnsiConsole.Clear();
        ShowHeader();

        if (results.Count == 0)
        {
            ShowMessage("Det finns inga sparade resultat.");
        }
        else
        {
            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Gold1);

            table.AddColumn("[gold1]Spelare[/]");
            table.AddColumn("[green]Vinster[/]");
            table.AddColumn("[red]Förluster[/]");
            table.AddColumn("[yellow]Oavgjorda[/]");
            table.AddColumn("[blue]Rundor[/]");

            foreach (var (name, statistics) in results)
            {
                int roundsPlayed = statistics.Wins + statistics.Losses + statistics.Pushes;
                table.AddRow(
                    Markup.Escape(name),
                    statistics.Wins.ToString(),
                    statistics.Losses.ToString(),
                    statistics.Pushes.ToString(),
                    roundsPlayed.ToString());
            }

            AnsiConsole.Write(table);
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]Tryck på valfri tangent för att återgå.[/]");
        Console.ReadKey(true);
    }

    // Displays the Blackjack rules.
    public void ShowRules(RuleSet? ruleSet = null)
    {
        RuleSet rules = ruleSet ?? RuleSet.Default;
        string softSeventeenRule = rules.DealerHitsSoft17
            ? "Givaren köper på mjuk 17."
            : "Givaren stannar på mjuk 17.";
        string naturalVersusThreeCardRule = rules.NaturalBlackjackBeatsThreeCard21
            ? "Naturlig 21 slår 21 med tre kort."
            : "Naturlig 21 ger oavgjort mot 21 med tre kort.";

        AnsiConsole.Clear();
        Console.Write("\u001b[?25l");
        var rulesPanel = new Panel(new Rows(
            new Align(new Markup("[bold gold1]♠ BLACKJACK 21 ♠[/]"), HorizontalAlignment.Center),
            new Align(new Markup("[italic green3]Tur kan inte kompileras.[/]"), HorizontalAlignment.Center),
            new Rule("[gold1]♥ SPELREGLER ♥[/]").RuleStyle(Color.Gold1),
            new Markup(
                "[bold gold1]MÅL[/]\n" +
                "[white]Kom så nära 21 som möjligt utan att gå över.\nDin hand ska vara högre än givarens.[/]\n\n" +
                "[bold gold1]KORTENS VÄRDE[/]\n" +
                "[white]2–10 = Kortets värde\nJ, Q, K = 10 poäng\nEss = 1 eller 11 poäng[/]\n\n" +
                "[bold gold1]SPELETS GÅNG[/]\n" +
                "[white]Varje spelare får två öppna kort.\nGivaren får ett öppet och ett dolt kort.\nEss + tiokort på de två första korten är naturlig tjugoett.[/]\n\n" +
                "[bold gold1]DIN TUR[/]\n" +
                "[white]♠ Köpa – Ta ett nytt kort.\n♥ Stanna – Avsluta din tur.\n♦ Satsa – Välj och placera marker.\nÖver 21 innebär att du spräcker och förlorar.[/]\n\n" +
                "[bold gold1]GIVARENS TUR[/]\n" +
                $"[white]Givaren visar sitt dolda kort.\nGivaren köper på 16 eller lägre.\nGivaren stannar på hård 17 eller högre.\n{softSeventeenRule}[/]\n\n" +
                "[bold gold1]VINST OCH FÖRLUST[/]\n" +
                $"[white]Givaren spräcker = spelaren vinner, om spelaren inte redan har spräckt.\nAnnars vinner högsta handen.\nLika poäng innebär oavgjort.\n{naturalVersusThreeCardRule}\nTvå naturliga 21 ger oavgjort.[/]\n\n" +
                "[bold gold1]HUSREGLER[/]\n" +
                $"[white]Antal lekar: {rules.DeckCount}.\nBlanda om när färre än {rules.ReshuffleThreshold} kort återstår.[/]"),
            new Text(""),
            new Align(new Markup("[grey]Tryck på valfri tangent för att återgå.[/]"), HorizontalAlignment.Center)))
            .Border(BoxBorder.Rounded).BorderColor(Color.Gold1).Expand();
        AnsiConsole.Write(CenterPanel(rulesPanel));
        Console.Write("\u001b[?25l");
        Console.ReadKey(true);
        Console.Write("\u001b[?25l");
    }

    // Displays centered contributors.
    public void ShowCredits()
    {
        AnsiConsole.Clear();
        Console.Write("\u001b[?25l"); // Dölj terminalmarkören.

        // Align names and roles in two columns.
        var credits = new Grid();
        credits.AddColumn(new GridColumn().NoWrap().PadRight(6));
        credits.AddColumn(new GridColumn().NoWrap());
        credits.AddRow(new Markup("[white]Alice[/]"), new Markup("[gold1]Interface[/]"));
        credits.AddRow(new Markup("[white]Sofia[/]"), new Markup("[gold1]GameStorage[/]"));
        credits.AddRow(new Markup("[white]Christoffer[/]"), new Markup("[gold1]Cards[/]"));
        credits.AddRow(new Markup("[white]Niclas[/]"), new Markup("[gold1]Participant[/]"));

        var panel = new Panel(new Rows(
            new Align(new Markup("[bold gold1]♠ BLACKJACK 21 ♠[/]"), HorizontalAlignment.Center),
            new Align(new Markup("[italic green3]Tur kan inte kompileras.[/]"), HorizontalAlignment.Center),
            new Rule("[gold1]♣ MEDVERKANDE ♣[/]").RuleStyle(Color.Gold1),
            new Text(""),
            new Align(credits, HorizontalAlignment.Center),
            new Text(""),
            new Rule().RuleStyle(Color.Gold1),
            new Align(new Markup("[grey]Tryck på valfri tangent för att återgå.[/]"), HorizontalAlignment.Center)))
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Gold1)
            .Expand();

        AnsiConsole.Write(CenterPanel(panel));
        Console.Write("\u001b[?25l"); // Håll markören dold även efter att panelen ritats.
        Console.ReadKey(true);
        Console.Write("\u001b[?25l");
    }

    // Original player-count method kept for compatibility.
    public int AskPlayerCount()
    {
        AnsiConsole.Clear();
        ShowHeader();
        string choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("[bold gold1]♠ Hur många spelare? ♠[/]")
            .PageSize(5)
            .HighlightStyle(new Style(foreground: Color.White, background: new Color(0, 100, 50)))
            .AddChoices("1 spelare", "2 spelare", "3 spelare", "4 spelare", "5 spelare"));
        return int.Parse(choice.Split(' ')[0]);
    }
}
