 class Deck
{
     public RuleSet Rules { get; }
     public List<Card> DeckCards { get; } = [];
    public List<Card> DiscardedCards { get; } = [];
    public List<Hand> ListOfHands { get; } = [];
    public int RunningCount;
    public double TrueCount;

    public Deck(RuleSet? rules = null)
    {
        Rules = rules ?? RuleSet.Default;
        Rules.Validate();
    }

    public Hand CreateHand()
    {
        Hand hand = new Hand();
        ListOfHands.Add(hand);
        return hand;
    }

    public void CreateDeck()
    {
        for (int i = 0; i < Rules.DeckCount; i++)
        {
            for (int s = 0; s < 4; s++)
            {
                for (int r = 0; r < 13; r++)
                {
                    Card card = new((Suit)s, (Rank)r);
                    DeckCards.Add(card);
                }
            }
        }

    }
    public void CardCounter()
    {
        Rank rank = DeckCards[0].Rank;
        if (rank <= Rank.Six)
        {
            RunningCount++;
        }
        else if (rank >= Rank.Ten)
        {
            RunningCount--;
        }
        double decksRemaining = DeckCards.Count / 52.0;
        double rawTrueCount = (double)RunningCount / decksRemaining;
        TrueCount = Math.Round(rawTrueCount, 2, MidpointRounding.AwayFromZero);

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
        RunningCount = 0;
        TrueCount = 0;

    }
    public void CheckIfNewShuffleIsNeeded(int cardsNeeded = 0)
    {
        if (cardsNeeded < 0)
            throw new ArgumentOutOfRangeException(nameof(cardsNeeded));

        if (DeckCards.Count < Math.Max(Rules.ReshuffleThreshold, cardsNeeded))
        {
            ShuffleDeck();
        }
    }
    public Card DealCard()
    {
        CardCounter();
        Card dealCard = DeckCards[0];
        DeckCards.RemoveAt(0);
        return dealCard;
    }
    public void ForceRetriveAllCards()
    {
        for (int i = 0; i < ListOfHands.Count; i++)
        {
            ReciveDiscardedCard(ListOfHands[i]);
        }
    }
    public void ReciveDiscardedCard(Hand hand)
    {
        List<Card> recievedDiscardedCard = hand.DiscardCards();
        int numberOfCards = recievedDiscardedCard.Count();
        for (int i = 0; i < numberOfCards; i++)
        {
            DiscardedCards.Add(recievedDiscardedCard[0]);
            recievedDiscardedCard.RemoveAt(0);
        }
    }


}