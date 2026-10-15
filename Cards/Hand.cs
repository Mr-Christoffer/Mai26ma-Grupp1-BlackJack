class Hand
{
    public const int BlackjackValue = 21;
    public List<Card> CardsOnHand { get; } = [];

    public void ReceiveCard(Deck deck)
    {
        Card recievedCard = deck.DealCard();
        CardsOnHand.Add(recievedCard);
    }
    public List<Card> DiscardCards()
    {
        List<Card> CardsToDiscard = new List<Card>();

        int numberOfCards = CardsOnHand.Count;
        for (int i = 0; i < numberOfCards; i++)
        {
            CardsToDiscard.Add(CardsOnHand[0]);
            CardsOnHand.RemoveAt(0);
        }
        return CardsToDiscard;
    }

    public int Score
    {
        get
        {
            int total = 0;
            int aces = 0;

            foreach (Card card in CardsOnHand)
            {
                total += card.IndividualCardValue(card);
                if (card.Rank == Rank.Ace)
                {
                    aces++;
                }
            }

            while (total > BlackjackValue && aces > 0)
            {
                total -= 10;
                aces--;
            }

            return total;
        }
    }

    public bool IsSoft
    {
        get
        {
            int hardTotal = 0;
            bool hasAce = false;

            foreach (Card card in CardsOnHand)
            {
                if (card.Rank == Rank.Ace)
                {
                    hasAce = true;
                    hardTotal++;
                }
                else
                {
                    hardTotal += card.IndividualCardValue(card);
                }
            }

            return hasAce && hardTotal + 10 <= BlackjackValue;
        }
    }
}
