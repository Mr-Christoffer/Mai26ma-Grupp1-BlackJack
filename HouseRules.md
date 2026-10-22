# House Rules

The game's house rules are collected in the `RuleSet` class. It contains rules for the dealer,
the deck, and round outcomes. `Game` loads the rules at startup and passes the same rule set
to `Dealer`, `Deck`, and `Round`.

## Changing the rules

Rules can be changed in the game's settings menu. Use the up and down arrows to select a rule,
the left and right arrows or Enter to change its value, and Esc to go back. Changes made in
the menu take effect in the next game but are not saved when the program exits. The menu is
in Swedish and has four rows: dealer on soft 17, number of decks (2-8, one step at a time),
cut card (steps of 5, from 25 up to a quarter of the shoe) and natural blackjack.

For permanent changes, edit `blackjack-rules.json` directly. The program first looks in the
current working directory and then beside the built application. The file is copied beside the
application during the build. Restart the game to load changes from the JSON file; rebuilding
is not required.

| JSON property | Description | Default |
| --- | --- | --- |
| `DealerHitsSoft17` | Whether the dealer takes a card on a soft 17. | `false` (dealer stands) |
| `DeckCount` | Number of decks used. Valid values are 2–8. | `6` (the shipped `blackjack-rules.json` sets `2`) |
| `ReshuffleThreshold` | Lower bound for where the cut card is placed (see below). The value must be at least 25 and cannot exceed the total number of cards (`DeckCount` x 52). | `25` |
| `NaturalBlackjackBeatsThreeCard21` | Whether a natural blackjack beats a 21 made with three cards. | `true` |

The defaults in the code are: dealer stands on soft 17, six decks, cut card no later than 25
cards remaining, and a natural blackjack beats a three-card 21. The shipped
`blackjack-rules.json` uses two decks, and that file is what the game normally loads.

If the JSON file is missing, cannot be read, or contains invalid JSON or values, a Swedish
notice is displayed on the main menu and the game uses the default rules. Property names in the
file are not case sensitive.

## Cut card and reshuffling

After each shuffle the cut card is placed at a random position between `ReshuffleThreshold`
cards remaining and a quarter of the shoe remaining (if the threshold is larger than a quarter,
it is placed at the threshold). When the number of cards left reaches that position the cut card
is reached.

The shoe is reshuffled at the start of the next round when the cut card has been reached, or when
the remaining cards cannot cover the worst case (11 cards per participant, players plus dealer).
Reaching the cut card mid-round does not trigger a reshuffle. A reshuffle puts the discard pile
back into the shoe.

New cards are never created during a game. If the shoe runs empty mid-round, the discard pile is
reshuffled, and the game throws an error if every card is in play.

## Tests

The house rules are covered by xUnit tests in the `Tests` folder. See the Tests section of
[README.md](README.md) for how to run them.

Main files: `Configuration/RuleSet.cs`, `Cards/Deck.cs`, `GameLogic/Round.cs`,
`blackjack-rules.json`, `Tests/`.