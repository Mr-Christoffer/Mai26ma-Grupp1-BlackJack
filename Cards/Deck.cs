class Deck
{
    public const int CardsPerDeck = 52;
    private const int SuitCount = 4;
    private const int RanksPerSuit = 13;
    private const int QuarterDivisor = 4;
    
    public RuleSet Rules { get; }
    public List<Card> DeckCards { get; } = [];
    public List<Card> DiscardedCards { get; } = [];
    public int CutCardPosition { get; private set; }
    public bool CutCardReached { get; private set; }
    public CardCounter Counter { get; } = new();

    public Deck(RuleSet? rules = null)
    {
        Rules = rules ?? RuleSet.Default;
        Rules.Validate();
    }

    public void CreateDeck()
    {
        for (int i = 0; i < Rules.DeckCount; i++)
        {
            for (int s = 0; s < SuitCount; s++)
            {
                for (int r = 0; r < RanksPerSuit; r++)
                {
                    Card card = new((Suit)s, (Rank)r);
                    DeckCards.Add(card);
                }
            }
        }
    }

    private void PlaceCutCard()
    {
        int lastQuarter = DeckCards.Count / QuarterDivisor;
        int min = Math.Min(Rules.ReshuffleThreshold, DeckCards.Count);
        int max = Math.Max(min, lastQuarter);
        CutCardPosition = Random.Shared.Next(min, max + 1);
        CutCardReached = false;
    }

    public void ShuffleDeck()
    {
        if (DiscardedCards.Count > 0)
        {
            int nrOfCardsToTransfer = DiscardedCards.Count;
            for (int i = 0; i < nrOfCardsToTransfer; i++)
            {
                DeckCards.Add(DiscardedCards[0]);
                DiscardedCards.RemoveAt(0);
            }
        }
        for (int i = DeckCards.Count - 1; i > 0; i--)
        {
            int number = Random.Shared.Next(0, i + 1);
            Card TempCard = DeckCards[number];
            DeckCards[number] = DeckCards[i];
            DeckCards[i] = TempCard;
        }
        PlaceCutCard();
        Counter.Reset();

    }
    public void CheckIfNewShuffleIsNeeded(int cardsNeeded = 0)
    {
        if (cardsNeeded < 0)
            throw new ArgumentOutOfRangeException(nameof(cardsNeeded));

        if (CutCardReached || DeckCards.Count < cardsNeeded)
        {
            ShuffleDeck();
        }
    }
    // Guarantees DealCard never fails: reuse the discard pile when the shoe is empty.
    // Never creates new cards: a real shoe is only refilled by reshuffling the discard pile.
    private void EnsureCardAvailable()
    {
        if (DeckCards.Count > 0)
            return;
 
        ShuffleDeck();
        if (DeckCards.Count == 0)
            throw new InvalidOperationException(
                "The shoe is empty and every card is in play. Use more decks or fewer players.");
    }

    public Card DealCard(bool faceUp = true)
    {
        EnsureCardAvailable();
        Card dealCard = DeckCards[0];
        DeckCards.RemoveAt(0);
        if (DeckCards.Count <= CutCardPosition)
            CutCardReached = true;
        if (faceUp)
        {
            Counter.Count(dealCard, DeckCards.Count);
        }
        return dealCard;
    }
}