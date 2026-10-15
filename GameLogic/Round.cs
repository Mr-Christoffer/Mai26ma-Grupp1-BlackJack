class Round
{
    private bool started;
    private bool ended;
    private readonly Dictionary<Player, Outcome> outcomes = new();

    public int RoundNumber { get; }
    public List<Player> Players { get; }
    public Dealer Dealer { get; }
    public Deck Deck { get; }
    public RuleSet Rules { get; }
    public IReadOnlyDictionary<Player, Outcome> Outcomes => outcomes;

    public Round(
        int roundNumber,
        List<Player> players,
        Dealer dealer,
        Deck deck,
        RuleSet? rules = null)
    {
        if (roundNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(roundNumber));
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(dealer);
        ArgumentNullException.ThrowIfNull(deck);
        if (players.Count == 0)
            throw new ArgumentException("A round must have at least one player.", nameof(players));
        if (players.Exists(player => player is null))
            throw new ArgumentException("Players cannot contain null entries.", nameof(players));

        Rules = rules ?? deck.Rules;
        Rules.Validate();
        if (Rules != deck.Rules || Rules != dealer.Rules)
            throw new ArgumentException(
                "The round, dealer, and deck must use the same rule set.",
                nameof(rules));

        RoundNumber = roundNumber;
        Players = players;
        Dealer = dealer;
        Deck = deck;
    }

    public void StartRound()
    {
        if (started)
            throw new InvalidOperationException("This round has already started.");

        foreach (Player player in Players)
            player.NewRound(Deck);
        Dealer.NewRound(Deck);
        Deck.CheckIfNewShuffleIsNeeded(2 * (Players.Count + 1));

        for (int cardNumber = 0; cardNumber < 2; cardNumber++)
        {
            foreach (Player player in Players)
                player.ReceiveCard(Deck);
            Dealer.ReceiveCard(Deck);
        }

        started = true;
    }

    public void EndRound()
    {
        if (!started)
            throw new InvalidOperationException("Start the round before ending it.");
        if (ended)
            throw new InvalidOperationException("This round has already ended.");

        Dealer.ShowHoleCard();
        bool anyPlayerStillIn = Players.Exists(player => !player.IsBust);
        if (anyPlayerStillIn)
        {
            while (Dealer.ShouldHit())
                Dealer.ReceiveCard(Deck);
        }

        DetermineWinners();
        ended = true;
    }

    private void DetermineWinners()
    {
        foreach (Player player in Players)
        {
            Outcome outcome;
            bool playerHasNatural = player.HasBlackjack;
            bool dealerHasNatural = Dealer.HasBlackjack;
            bool playerHasThreeCard21 =
                player.Hand.CardsOnHand.Count == 3 &&
                player.Score == Hand.BlackjackValue;
            bool dealerHasThreeCard21 =
                Dealer.Hand.CardsOnHand.Count == 3 &&
                Dealer.Score == Hand.BlackjackValue;

            if (player.IsBust)
                outcome = Outcome.Lose;
            else if ((playerHasNatural && dealerHasThreeCard21) ||
                     (dealerHasNatural && playerHasThreeCard21))
            {
                if (Rules.NaturalBlackjackBeatsThreeCard21)
                    outcome = playerHasNatural ? Outcome.Blackjack : Outcome.Lose;
                else
                    outcome = Outcome.Push;
            }
            else if (dealerHasNatural && !playerHasNatural)
                outcome = Outcome.Lose;
            // A natural blackjack is checked before a dealer bust so it always pays 3:2.
            else if (playerHasNatural && !dealerHasNatural)
                outcome = Outcome.Blackjack;
            else if (Dealer.IsBust)
                outcome = Outcome.Win;
            else if (player.Score > Dealer.Score)
                outcome = Outcome.Win;
            else if (player.Score < Dealer.Score)
                outcome = Outcome.Lose;
            else
                outcome = Outcome.Push;

            player.SettleBet(outcome);
            outcomes[player] = outcome;
        }
    }
}
