using System;
using System.Collections.Generic;
using Spectre.Console;
using Spectre.Console.Rendering;
public partial class ConsoleUi
{
    private static readonly Color TableGreen = new Color(7, 80, 46);
    private static readonly Color TableGold = Color.Gold1;
    internal Panel BuildBlackjackTable(List<Player> players, Dealer dealer, int tableHeight = 0, string? notice = null, string[]? playerStatuses = null, bool cardsFaceDown = false)
    {
        List<IRenderable> tableContent = new();
        if (notice != null)
        {
            tableContent.Add(new Align(new Markup($"[bold black on gold1]  ★ {Markup.Escape(notice)} ★  [/]"), HorizontalAlignment.Center));
            tableContent.Add(new Align(new Markup("[gold1]╰──── ♠ BLACKJACK 21 ♠ ────╯[/]"), HorizontalAlignment.Center));
        }
        else
        {
            tableContent.Add(new Align(new Markup("[gold1]╭──────── MARKERSTÄLL ────────╮[/]"), HorizontalAlignment.Center));
            tableContent.Add(new Align(new Markup("[white]● ●[/]  [red]● ●[/]  [green]● ●[/]  [blue]● ●[/]  [yellow]● ●[/]"), HorizontalAlignment.Center));
        }
        tableContent.Add(new Rule("[gold1]GIVARE[/]").RuleStyle(TableGold));
        AddRenderedCards(tableContent, dealer.Hand.CardsOnHand, !dealer.HoleCardVisible, cardsFaceDown);
        // Fixed-width seat layout: the center never shrinks when other players join.
        // 10 + 10 + 35 + 10 + 10 = 75 terminal cells; fits beside the menu.
        tableContent.Add(BuildPlayerRow(players, playerStatuses, cardsFaceDown));
        tableContent.Add(BettingUI.Create());
        int innerHeight = tableHeight > 0 ? Math.Max(1, tableHeight - 2) : 0;
        var greenTable = new GreenTableBackground(new Rows(tableContent), TableGreen, innerHeight);
        return new Panel(greenTable)
            .Header("[gold1] ♠ BLACKJACK ♠ [/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(TableGold)
            .Expand();
    }
    private static string GetHandType(Player player)
    {
        if (player.Score != 21) return "";
        if (player.HasBlackjack) return "NATURAL BLACKJACK";
        int total = 0, aces = 0;
        foreach (Card card in player.Hand.CardsOnHand)
        {
            total += card.IndividualCardValue(card);
            if (card.Rank == Rank.Ace) aces++;
        }
        while (total > 21 && aces > 0) { total -= 10; aces--; }
        return aces > 0 ? "MJUK BLACKJACK" : "HÅRD BLACKJACK";
    }
    private const int SideSeatWidth = 10;
    private const int AliceSeatWidth = 35;
    private IRenderable BuildPlayerRow(List<Player> players, string[]? playerStatuses, bool cardsFaceDown)
    {
        // In solo mode, center each card line in the same table width as the dealer.
        // The fixed five-seat grid is only needed for multiplayer.
        if (players.Count == 1)
            return BuildSoloPlayerRow(players[0], playerStatuses, cardsFaceDown);

        int[] order = { 1, 2, 0, 3, 4 };
        int[] widths = { SideSeatWidth, SideSeatWidth, AliceSeatWidth,
                         SideSeatWidth, SideSeatWidth };
        List<string[]> columns = new();
        for (int seat = 0; seat < order.Length; seat++)
        {
            int index = order[seat];
            int width = widths[seat];
            string[] lines = new string[8];
            for (int r = 0; r < lines.Length; r++)
                lines[r] = new string(' ', width);
            if (index < players.Count)
            {
                Player player = players[index];
                string name = player.Name.ToUpperInvariant();
                string handType = cardsFaceDown ? "" : GetHandType(player);
                string label = index == 0 && handType.Length > 0 ? $"{name} · {handType}" : name;
                if (label.Length > width) label = label[..width];
                lines[0] = CenterText(Markup.Escape(label), label.Length, width, "bold gold1");
                if (index == 0)
                {
                    List<string> fan = BuildFanCardLines(player.Hand.CardsOnHand, cardsFaceDown);
                    for (int r = 0; r < 7; r++)
                        lines[r + 1] = CenterMarkup(fan[r], GetFanWidth(player.Hand.CardsOnHand.Count), width);
                }
                else
                {
                    string cards = BuildCompactCards(player.Hand.CardsOnHand, width, cardsFaceDown);
                    lines[1] = CenterMarkup(cards, CompactVisibleWidth(player.Hand.CardsOnHand, width, cardsFaceDown), width);
                    if (handType.Length > 0)
                    {
                        string shortType = player.HasBlackjack ? "NATURAL BJ" :
                            handType.StartsWith("MJUK") ? "MJUK BJ" : "HÅRD BJ";
                        lines[2] = CenterText(shortType, shortType.Length, width, "bold gold1");
                    }
                }
            }
            columns.Add(lines);
        }
        List<IRenderable> output = new();
        for (int r = 0; r < 8; r++)
        {
            string line = "";
            for (int seat = 0; seat < columns.Count; seat++)
                line += columns[seat][r];
            output.Add(new Markup(line));
        }
        // Status is shown under each player's cards, without moving the seats.
        string statusRow = "";
        for (int seat = 0; seat < order.Length; seat++)
        {
            int index = order[seat];
            int width = widths[seat];
            string status = playerStatuses != null && index < players.Count && index < playerStatuses.Length
                ? playerStatuses[index]
                : "";
            if (status.Length > width) status = status[..width];
            string style = status switch
            {
                "DIN TUR" => "bold black on gold1",
                "TJOCK" => "bold red",
                "FÖRLORADE" => "bold red",
                "STANNAT" => "bold white",
                "BLACKJACK" => "bold gold1",
                "21 POÄNG" => "bold gold1",
                "VANN" => "bold green1",
                "OAVGJORT" => "bold gold1",
                "KLAR" => "grey",
                _ => "grey"
            };
            statusRow += CenterText(Markup.Escape(status), status.Length, width, style);
        }
        output.Add(new Markup(statusRow));

        return new Align(new Rows(output), HorizontalAlignment.Center);
    }
    private IRenderable BuildSoloPlayerRow(Player player, string[]? playerStatuses, bool cardsFaceDown)
    {
        // Use the same 75-cell reference width as the dealer's card row.
        // This keeps both card edges aligned in solo mode.
        const int seatingWidth = 2 * SideSeatWidth + AliceSeatWidth + 2 * SideSeatWidth;

        string name = player.Name.ToUpperInvariant();
        string handType = cardsFaceDown ? "" : GetHandType(player);
        string label = handType.Length > 0 ? $"{name} · {handType}" : name;

        List<IRenderable> rows = new()
        {
            new Markup(CenterText(Markup.Escape(label), label.Length, seatingWidth, "bold gold1"))
        };

        int cardWidth = GetFanWidth(player.Hand.CardsOnHand.Count);
        foreach (string line in BuildFanCardLines(player.Hand.CardsOnHand, cardsFaceDown))
            rows.Add(new Markup(CenterMarkup(line, cardWidth, seatingWidth)));

        string status = playerStatuses is { Length: > 0 } ? playerStatuses[0] : "";
        string style = status switch
        {
            "DIN TUR" => "bold black on gold1",
            "TJOCK" or "FÖRLORADE" => "bold red",
            "BLACKJACK" or "21 POÄNG" or "OAVGJORT" => "bold gold1",
            "VANN" => "bold green1",
            "STANNAT" => "bold white",
            _ => "grey"
        };
        rows.Add(new Markup(CenterText(Markup.Escape(status), status.Length, seatingWidth, style)));
        return new Align(new Rows(rows), HorizontalAlignment.Center);
    }

    private static string CenterMarkup(string markup, int visibleWidth, int width)
    {
        int left = Math.Max(0, (width - visibleWidth) / 2);
        int right = Math.Max(0, width - visibleWidth - left);
        return new string(' ', left) + markup + new string(' ', right);
    }
    private static string CenterText(string text, int visibleWidth, int width, string style) =>
        CenterMarkup($"[{style}]{text}[/]", visibleWidth, width);
    // Other players use small card labels. Keep the line within 13 cells.
    private string BuildCompactCards(List<Card> cards, int width, bool cardsFaceDown = false)
    {
        if (cards.Count == 0)
            return "[grey]Inga kort[/]";
        List<string> labels = new();
        int used = 0;
        for (int i = 0; i < cards.Count; i++)
        {
            string label = cardsFaceDown ? "▣" : GetCardLabel(cards[i]);
            int extra = label.Length + (used > 0 ? 1 : 0);
            if (used + extra > width - (i < cards.Count - 1 ? 3 : 0))
                break;
            if (used > 0)
            {
                labels.Add(" ");
                used++;
            }
            labels.Add(cardsFaceDown ? "[gold1 on #172A60]▣[/]" : $"[{GetCardColor(cards[i])} on white]{label}[/]");
            used += label.Length;
        }
        int displayed = 0;
        foreach (Card card in cards)
        {
            int next = (cardsFaceDown ? 1 : GetCardLabel(card).Length) + (displayed > 0 ? 1 : 0);
            if (CompactPrefixWidth(cards, displayed + 1, cardsFaceDown) > used)
                break;
            displayed++;
        }
        if (displayed < cards.Count)
            labels.Add($" [gold1]+{cards.Count - displayed}[/]");
        return string.Concat(labels);
    }
    private int CompactVisibleWidth(List<Card> cards, int width, bool cardsFaceDown = false)
    {
        if (cards.Count == 0) return 8;
        int used = 0, count = 0;
        foreach (Card card in cards)
        {
            int add = (cardsFaceDown ? 1 : GetCardLabel(card).Length) + (count > 0 ? 1 : 0);
            if (used + add > width - (count < cards.Count - 1 ? 3 : 0)) break;
            used += add;
            count++;
        }
        return used + (count < cards.Count ? 2 + (cards.Count - count).ToString().Length : 0);
    }
    private int CompactPrefixWidth(List<Card> cards, int count, bool cardsFaceDown = false)
    {
        int result = 0;
        for (int i = 0; i < count; i++)
            result += (cardsFaceDown ? 1 : GetCardLabel(cards[i]).Length) + (i > 0 ? 1 : 0);
        return result;
    }
    // Two full-size cards. Later cards overlap horizontally with a thin
    // dark border at each visible edge; the newest card is full-size.
    private List<string> BuildFanCardLines(List<Card> cards, bool cardsFaceDown = false)
    {
        List<string> result = new();
        if (cards.Count == 0)
        {
            result.Add("[grey]Inga kort[/]");
            for (int r = 1; r < 7; r++) result.Add("        ");
            return result;
        }
        CardRenderer renderer = new();
        int start = Math.Max(0, cards.Count - 5);
        List<string[]> rendered = new();
        for (int i = start; i < cards.Count; i++)
            rendered.Add(cardsFaceDown ? renderer.RenderHiddenCard() : renderer.RenderCard(cards[i]));
        for (int row = 0; row < 7; row++)
        {
            string line = rendered[0][row];
            if (rendered.Count == 2)
                line += "  " + rendered[1][row];
            else if (rendered.Count > 2)
            {
                line += "  ";
                for (int i = 1; i < rendered.Count - 1; i++)
                    line += cardsFaceDown ? "[gold1 on #172A60]│◇ [/]" : BuildCardEdge(cards[start + i], row);
                line += rendered[rendered.Count - 1][row];
            }
            result.Add(line);
        }
        return result;
    }
    private static int GetFanWidth(int cardCount)
    {
        if (cardCount == 0) return 8;
        if (cardCount == 1) return 11;
        if (cardCount == 2) return 24;
        return 24 + 3 * (Math.Min(cardCount, 5) - 2);
    }
    // Three visible cells per covered card, including its dark outline.
    private string BuildCardEdge(Card card, int row)
    {
        string rank = GetRankLabel(card);
        string suit = GetSuitLabel(card);
        string color = GetCardColor(card);
        string text = row switch
        {
            0 => "╭──",
            1 => "│" + rank.PadRight(2),
            2 => "│" + suit + " ",
            6 => "╰──",
            _ => "│  "
        };
        return $"[{color} on white]{text}[/]";
    }
    internal void ShowBlackjackTable(List<Player> players, Dealer dealer)
    {
        AnsiConsole.Write(BuildBlackjackTable(players, dealer));
    }
    private void AddRenderedCards(List<IRenderable> content, List<Card> cards, bool hideSecondCard, bool hideAllCards = false)
    {
        if (cards.Count == 0)
        {
            content.Add(new Markup("[grey]Inga kort[/]"));
            return;
        }
        CardRenderer renderer = new();
        List<string[]> renderedCards = new();
        for (int i = 0; i < cards.Count; i++)
            renderedCards.Add(hideAllCards || (hideSecondCard && i == 1)
                ? renderer.RenderHiddenCard()
                : renderer.RenderCard(cards[i]));
        // Match the fixed 75-cell player seating grid so the dealer's
        // first and second cards line up with player 1's cards.
        const int seatingWidth = 2 * SideSeatWidth + AliceSeatWidth + 2 * SideSeatWidth;
        int cardsWidth = renderedCards.Count * 11 + Math.Max(0, renderedCards.Count - 1) * 2;
        List<IRenderable> alignedRows = new();
        for (int row = 0; row < 7; row++)
        {
            string cardLine = string.Join("  ", renderedCards.Select(card => card[row]));
            alignedRows.Add(new Markup(CenterMarkup(cardLine, cardsWidth, seatingWidth)));
        }
        content.Add(new Align(new Rows(alignedRows), HorizontalAlignment.Center));
    }
    private string GetRankLabel(Card card) => card.Rank switch
    {
        Rank.Ace => "A", Rank.King => "K", Rank.Queen => "Q", Rank.Jack => "J",
        Rank.Ten => "10", Rank.Nine => "9", Rank.Eight => "8", Rank.Seven => "7",
        Rank.Six => "6", Rank.Five => "5", Rank.Four => "4", Rank.Three => "3",
        Rank.Two => "2", _ => "?"
    };
    private string GetSuitLabel(Card card) => card.Suit switch
    {
        Suit.Hearts => "♥", Suit.Diamonds => "♦", Suit.Clubs => "♣",
        Suit.Spades => "♠", _ => "?"
    };
    private string GetCardLabel(Card card) => GetRankLabel(card) + GetSuitLabel(card);
    private string GetCardColor(Card card) =>
        card.Suit == Suit.Hearts || card.Suit == Suit.Diamonds ? "red" : "black";
}
internal sealed class GreenTableBackground : Renderable
{
    private readonly IRenderable content;
    private readonly Color background;
    private readonly int minimumRows;
    public GreenTableBackground(IRenderable content, Color background, int minimumRows = 0)
    {
        this.content = content;
        this.background = background;
        this.minimumRows = minimumRows;
    }
    protected override Measurement Measure(RenderOptions options, int maxWidth) =>
        content.Measure(options, maxWidth);
    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        Style greenStyle = new Style(background: background);
        int width = Math.Max(1, maxWidth), currentWidth = 0, finishedRows = 0;
        bool atLineStart = true;
        foreach (Segment segment in content.Render(options, maxWidth))
        {
            if (segment.IsLineBreak)
            {
                if (currentWidth < width)
                    yield return new Segment(new string(' ', width - currentWidth), greenStyle);
                yield return segment;
                finishedRows++;
                currentWidth = 0;
                atLineStart = true;
            }
            else
            {
                Style original = segment.Style;
                Style finalStyle = original.Background == Color.Default || original.Background == Color.Black
                    ? original.Combine(greenStyle)
                    : original;
                yield return new Segment(segment.Text, finalStyle);
                currentWidth += segment.CellCount();
                atLineStart = false;
            }
        }
        if (!atLineStart)
        {
            if (currentWidth < width)
                yield return new Segment(new string(' ', width - currentWidth), greenStyle);
            finishedRows++;
        }
        // The content may already end with a line break. Do not add a second
        // one before the first padding row, or an unpainted black stripe appears.
        bool needsLineBreak = !atLineStart;
        while (finishedRows < minimumRows)
        {
            if (needsLineBreak)
                yield return Segment.LineBreak;
            yield return new Segment(new string(' ', width), greenStyle);
            finishedRows++;
            needsLineBreak = true;
        }
    }
}
