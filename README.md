# Mai26ma Grupp 1 - Blackjack

A console-based Blackjack (twenty-one) game for one to five players playing against a dealer.
The project focuses on a playable round-based game, correct hand scoring, and saving player
statistics between program runs.

## Features

- Play repeated rounds with one to five named players.
- Load each returning player's wins, losses, and pushes.
- Deal from a configurable number of decks and reshuffle below the configured card threshold.
- Configure whether the dealer hits on soft 17 and whether a natural blackjack beats a
  three-card 21.
- Save cumulative player statistics as JSON for the game and as a readable `player-results.txt`
  report for players.

The house rules are read from `blackjack-rules.json` in the working directory, or from beside
the application if the working-directory file is absent. The file is copied beside the built
application and can be edited without recompiling. If no file is found, the game displays a
notice on its main menu and uses the defaults: dealer stands on soft 17, six decks,
reshuffle below 15 remaining cards, and natural blackjack beats a three-card 21.

The JSON properties are `DealerHitsSoft17` (boolean), `DeckCount` (1–8),
`ReshuffleThreshold` (1 through the configured total deck size), and
`NaturalBlackjackBeatsThreeCard21` (boolean). Invalid JSON or values are reported as errors
rather than silently ignored. Edit these values in `blackjack-rules.json` and restart the game
for the changes to take effect; rebuilding is not required.

This is the basic game: it does not use insurance, doubling down, or splitting pairs.

## Project responsibilities

- `Program.cs` starts the application.
- `Game` coordinates the main menu, player sessions, and round lifecycle.
- `GameTableUI` renders the active table and handles gameplay navigation.
- `ConsoleUI` handles shared console screens and saved-player selection.
- `PlayerSessionService` creates and loads players and coordinates statistics persistence.
- `RoundResultFormatter` formats the outcomes resolved by a round.
- `Player` and `Dealer` represent the people playing; `Participant` contains behavior shared by both.
- `Deck`, `Hand`, and `Card` handle cards, dealing, and hand contents.
- `GameStorage` loads and saves player statistics in the user's local
application-data folder.
On macOS, the save will typically be under ~/Library/Application Support/Mai26ma-Grupp1-BlackJack; on Windows, it’s under %LOCALAPPDATA%\Mai26ma-Grupp1-BlackJack.

See [Uml.md](Uml.md) for the class diagram and more detail about each class's responsibility.
The project's detailed requirements and user stories are in [UserStories.md](UserStories.md).

## Run

From the project directory, run:

```text
dotnet run
```

Player statistics are saved as `player-results.json` and summarized in `player-results.txt` under
the user's local application-data folder (`%LOCALAPPDATA%\Mai26ma-Grupp1-BlackJack` on Windows).
The text report contains cumulative totals from previous sessions as well as the latest saved
results. If the game finds an older `player-results.json` in its current working directory and no
app-data results exist yet, it imports those results into the new location without removing the
older file. Results are local to one computer and are not automatically synchronized.

## Language

Class, method, and variable names are in English. Console prompts and messages are currently in
English; Swedish terminology and user stories may also appear in the project documentation.
