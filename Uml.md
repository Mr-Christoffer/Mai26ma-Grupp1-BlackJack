# Blackjack Class Diagram and Responsibilities

This document describes the current runtime structure of the console Blackjack game. It
focuses on the responsibilities of the main classes and how they collaborate.

## Class diagram

```mermaid
classDiagram
    class Program {
        <<entry point>>
        +Main()
    }
    class Game {
        -ConsoleUi ui
        -PlayerSessionService playerSessions
        -GameTableUI gameTable
        -RuleSet rules
        -string ruleConfigurationNotice
        +Game()
        +Run()
        -StartNewGame()
        -LoadGame()
        -PlayGame(players)
    }
    class ConsoleUi {
        +ShowMenu(notice) string
        +AskPlayerCountCentered() int
        +AskPlayerNameCentered(playerNumber) string
        +SelectSavedPlayers(results, deletePlayer) List~string~
        +EditRules(current) RuleSet
        +ShowRules()
        +ShowCredits()
        +ShowRules(rules)
        +ShowGameControls()
        +ShowCenteredMessage(message)
        +ShowBlackjackTable(players, dealer)
        +ShowBettingMode(...)
        +BuildGameMenu(...) Panel
        +BuildPlayerActions(...) Panel
        +BuildBlackjackTable(...) Panel
    }
    class GameTableUI {
        -ConsoleUi ui
        -Deck observedDeck
        +GameTableUI(ui)
        +ResetSession()
        +PlayRound(round, saveGame) GameTableAction
        +ShowRoundEndMenu(round, summary, saveGame) GameTableAction
        -DetectRefilledShoe(deck) bool
    }
    class GameTableAction {
        <<enumeration>>
        RoundCompleted
        StartNewRound
        ReturnToMainMenu
        ExitGame
    }
    class PlayerSessionService {
        -GameStorage gameStorage
        -ConsoleUi ui
        +CreateNewPlayers(playerCount) List~Player~
        +LoadPlayers() List~Player~
        +SaveResults(players) bool
        +SaveGame(players) bool
        -DeletePlayer(playerName) bool
    }
    class GameStorage {
        -string filePath
        +GameStorage(filePath)
        +LoadResults() Dictionary
        +SaveResults(results)
        +DeletePlayer(playerName) bool
    }
    class PlayerStatistics {
        +int Wins
        +int Losses
        +int Pushes
        +int Chips
    }
    class RuleSet {
        <<record>>
        +string FileName$
        +int MinDeckCount$
        +int MaxDeckCount$
        +int DefaultReshuffleThreshold$
        +bool DealerHitsSoft17
        +int DeckCount
        +int ReshuffleThreshold
        +bool NaturalBlackjackBeatsThreeCard21
        +Default$
        +LoadFromDefaultLocation(notify)$
        +LoadFromFile(filePath, notify)$
        +Load(filePath)$
        +Validate()
    }
    class Round {
        +int RoundNumber
        +List~Player~ Players
        +Dealer Dealer
        +Deck Deck
        +RuleSet Rules
        +IReadOnlyDictionary Outcomes
        +int StartingHandSize$
        -int MaxCardsPerHand$
        +StartRound()
        +RevealInitialCards()
        +RevealCard(card)
        +EndRound()
        -DetermineWinners()
    }
    class RoundResultFormatter {
        +Format(round)$ string
    }
    class Participant {
        <<abstract>>
        +string Name
        +Hand Hand
        +int Score
        +bool IsBust
        +bool HasBlackjack
        +ReceiveCard(deck, faceUp)
        +NewRound(deck)
        +ToString() string
    }
    class Player {
        +int Wins
        +int Losses
        +int Pushes
        +int GamesPlayed
        -Wallet Wallet
        +int Chips
        +int CurrentBet
        +int InsuranceBet
        +int InsuranceCost
        +bool IsOutOfChips
        +bool CanHit
        +TryPlaceBet(amount) bool
        +TryTakeInsurance() bool
        +SettleBet(outcome)
        +SettleInsurance(dealerHasBlackjack)
        +CancelBet()
        -RecordResult(outcome)
    }
    class Wallet {
        +int StartingChips$
        +int Chips
        +int CurrentBet
        +int InsuranceBet
        +int InsuranceCost
        +bool IsOutOfChips
        +TryPlaceBet(amount) bool
        +TryTakeInsurance() bool
        +SettleBet(outcome)
        +SettleInsurance(dealerHasBlackjack)
        +CancelBet()
    }
    class Dealer {
        +RuleSet Rules
        +bool HoleCardVisible
        +bool ShowsAce
        +ShowHoleCard()
        +ShouldHit() bool
        +NewRound(deck)
    }
    class Deck {
        +RuleSet Rules
        +List~Card~ DeckCards
        +List~Card~ DiscardedCards
        +int CutCardPosition
        +bool CutCardReached
        +CardCounter Counter
        +int CardsPerDeck$
        +CreateDeck()
        +ShuffleDeck()
        +CheckIfNewShuffleIsNeeded(cardsNeeded)
        +DealCard(faceUp) Card
        -PlaceCutCard()
        -EnsureCardAvailable()
    }
    class CardCounter {
        +int RunningCount
        +double TrueCount
        +Count(card, cardsRemaining)
        +Reset()
    }
    class Hand {
        +int BlackjackValue$
        +int AceReduction$
        +List~Card~ CardsOnHand
        +int Score
        +bool IsSoft
        +ReceiveCard(deck, faceUp)
        +DiscardCards() List~Card~
    }
    class Card {
        +Suit Suit
        +Rank Rank
        +IndividualCardValue(card) int
    }
    class Suit {
        <<enumeration>>
        Hearts
        Diamonds
        Clubs
        Spades
    }
    class Rank {
        <<enumeration>>
        Two
        Three
        Four
        Five
        Six
        Seven
        Eight
        Nine
        Ten
        Jack
        Queen
        King
        Ace
    }
    class Outcome {
        <<enumeration>>
        Win
        Loss
        Push
        Blackjack
    }
    class BettingUI {
        <<static>>
        +int Balance$
        +int CurrentBet$
        +int SelectedChip$
        +LoadBalance(balance)$
        +Create(player)$ IRenderable
        +CreateInfoPanel(player)$ IRenderable
        +HandleKey(key, player)$ bool
        +Reset()$
    }
    class CardRenderer {
        +GetRank(rank) string
        +GetSuit(suit) string
        +RenderCard(card) string[]
        +RenderHiddenCard() string[]
        +RenderSmallCard(card) string[]
        +RenderSmallHiddenCard() string[]
    }
    class GameMessages {
        <<static>>
    }

    Program ..> Game : creates and runs
    Game *-- ConsoleUi : coordinates interface
    Game *-- PlayerSessionService : manages saved players
    Game *-- GameTableUI : manages table interaction
    Game ..> GameTableUI : passes save callback
    Game ..> RuleSet : loads and edits
    Game ..> Deck : creates per game
    Game ..> Dealer : creates per game
    Game ..> Round : creates each round
    Game ..> RoundResultFormatter : formats results
    Game ..> GameTableAction : handles actions
    ConsoleUi ..> RuleSet : edits rules
    ConsoleUi ..> PlayerStatistics : displays saved results
    ConsoleUi ..> Player : displays player and table state
    ConsoleUi ..> Dealer : displays dealer and table state
    ConsoleUi ..> BettingUI : renders betting controls
    ConsoleUi ..> CardRenderer : renders cards
    ConsoleUi ..> GameMessages : shows texts
    Game ..> GameMessages : shows texts
    PlayerSessionService ..> GameMessages : shows texts
    PlayerSessionService --> GameStorage : loads, saves, deletes
    PlayerSessionService --> ConsoleUi : prompts and notifications
    PlayerSessionService ..> PlayerStatistics : maps persisted totals
    PlayerSessionService ..> Player : creates and saves players
    GameStorage ..> PlayerStatistics : persists
    GameTableUI --> ConsoleUi : draws table and menus
    GameTableUI ..> Round : handles round interaction
    GameTableUI ..> BettingUI : handles bets
    GameTableUI ..> CardCounter : shows running and true count
    GameTableUI ..> GameTableAction : returns action
    Round --> "1..5" Player : resolves outcomes for
    Round --> Dealer : plays against
    Round --> Deck : deals from
    Round --> RuleSet : applies rules
    Player --|> Participant
    Dealer --|> Participant
    Player *-- Wallet : owns chips and bets
    Player ..> Outcome : records
    Wallet ..> Outcome : pays out by
    Participant *-- Hand : owns
    Hand ..> Deck : receives cards from
    Deck o-- Card : holds cards
    Deck *-- CardCounter : counts dealt cards
    Hand o-- Card : holds dealt cards
    Card --> Suit
    Card --> Rank
    Round ..> Outcome : determines
    RoundResultFormatter ..> Round : formats
    RoundResultFormatter ..> Outcome : describes
    Round ..> CardCounter : counts revealed cards
    BettingUI ..> Player : places bets through
    BettingUI ..> Wallet : uses StartingChips
    PlayerStatistics ..> Wallet : uses StartingChips
```

## Responsibility boundaries

| Class | Responsibility | Should not own |
|---|---|---|
| `Program` | Entry point; creates and runs `Game`. | Game rules, persistence, or console flow. |
| `Game` | Loads rules, routes main-menu choices, creates players and game objects, coordinates rounds, saves results, and decides when a session ends. | Low-level terminal rendering or JSON serialization. |
| `ConsoleUi` | Displays menus, prompts, rules, messages, and table content; reads user input. | Game flow, round resolution, or file access. |
| `GameTableUI` | Handles table interactions such as betting, insurance, player turns, and round-end choices; shows the deck's card count and reports the next action to `Game`. | Persisting results or deciding round outcomes. |
| `PlayerSessionService` | Creates and loads players, maps saved statistics, and coordinates save/delete operations and related messages. | JSON file details or rendering menus. |
| `GameStorage` | Reads and writes player statistics in JSON, maintains the readable text report, and deletes saved players. | Player creation or game logic. |
| `PlayerStatistics` | Represents persisted wins, losses, pushes, and chips. | Game behavior or file operations. |
| `RuleSet` | Loads and validates house rules from `blackjack-rules.json` and provides defaults. | Running a game or displaying the settings menu. |
| `Round` | Deals starting cards, reveals cards to the card counter, plays the dealer turn, determines outcomes, and settles bets and insurance. | Terminal interaction or persistence. |
| `RoundResultFormatter` | Formats a completed round's outcomes for display. | Determining outcomes or saving them. |
| `Participant` | Shares a name and hand, card receiving, scoring state, and round reset behavior. | Player statistics or dealer strategy. |
| `Player` | Tracks a player's results and forwards chips, bets, and insurance to its private `Wallet`. | Console interaction or saved-file management. |
| `Wallet` | Tracks chips, bets, and insurance and settles them based on an outcome. | Statistics, console interaction, or deciding the outcome. |
| `Dealer` | Represents the dealer, reveals the hole card, and decides whether to hit based on the rules. | Choosing player actions or rendering cards. |
| `Deck` | Creates, shuffles, and deals cards, manages discards and the cut card, reshuffles the discard pile if the shoe runs empty (it never creates new cards mid-game), and owns the card counter. | Hand scoring or deciding winners. |
| `CardCounter` | Tracks running and true counts of cards that have become visible. | Changing game outcomes or displaying the count. |
| `Hand` | Holds cards, calculates hand score and softness, and owns `BlackjackValue` and `AceReduction`. | Deciding who wins. |
| `Card` | Stores suit and rank and provides a card's base value. | Scoring a complete hand. |
| `BettingUI` | Renders betting controls and applies betting key input through `Player`. | Owning player balances or resolving payouts. |
| `CardRenderer` | Produces the text representation of visible and hidden cards. | Game logic or card ownership. |
| `GameMessages` | Holds the texts and formats shown to the player. | Game logic or deciding when a message is shown. |

## Runtime flow

1. `Program` creates `Game` and calls `Run()`.
2. `Game` loads a `RuleSet` from `blackjack-rules.json` and creates `ConsoleUi`,
   `PlayerSessionService`, and `GameTableUI`.
3. The player chooses a new or saved game in `ConsoleUi`. `PlayerSessionService` loads saved
   statistics through `GameStorage` and creates the selected `Player` objects.
4. For a game, `Game` creates a `Deck` and `Dealer` with the same rules. For each round it
   creates a `Round` and starts the round, which resets hands, reshuffles if the cut card was
      reached or the shoe cannot cover the worst case for all participants, and deals the
      initial cards face down. The cut card never triggers a reshuffle mid-round.
5. `GameTableUI` handles betting, then flips the cards and calls `Round.RevealInitialCards()`
   so the card counter only counts visible cards. It then handles optional insurance and
   player choices, using `ConsoleUi` and `BettingUI` to display the table and process input.
6. When the table reports that the round is complete, `Game` calls `Round.EndRound()` to play
   the dealer and settle outcomes. `RoundResultFormatter` prepares the summary, and
   `PlayerSessionService` saves updated statistics through `GameStorage`.
7. `GameTableUI` shows the round-end menu. `Game` starts another round, returns to the main
   menu, or exits according to the returned `GameTableAction`.

## Related documentation

See [HouseRules.md](HouseRules.md) for details about the house rules and
[README.md](README.md) for project setup and usage.
