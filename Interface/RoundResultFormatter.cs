internal static class RoundResultFormatter
{
    public static string Format(Round round)
    {
        List<string> messages = new();
        foreach (Player player in round.Players)
        {
            string outcome = round.Outcomes[player] switch
            {
                Outcome.Blackjack => GameMessages.PlayerBlackjackOutcome,
                Outcome.Win when round.Dealer.IsBust => GameMessages.DealerBustOutcome,
                Outcome.Win => GameMessages.WinOutcome,
                Outcome.Lose when player.IsBust => GameMessages.PlayerBustOutcome,
                Outcome.Lose when round.Dealer.HasBlackjack =>
                    GameMessages.DealerBlackjackOutcome,
                Outcome.Lose => GameMessages.LossOutcome,
                Outcome.Push => GameMessages.PushOutcome,
                _ => throw new InvalidOperationException(
                    $"Round {round.RoundNumber} did not resolve player '{player.Name}'.")
            };
            string headline = GameMessages.OutcomeHeadline(
                outcome, soloPlayer: round.Players.Count == 1);

            if (round.Players.Count == 1)
                messages.Add(GameMessages.SoloResult(
                    headline, player.Score, round.Dealer.Score));
            else
                messages.Add(GameMessages.NamedResult(
                    player.Name, headline, player.Score, round.Dealer.Score));
        }

        return string.Join(Environment.NewLine, messages);
    }
}
