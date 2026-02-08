# Card System - Complete Usage Guide

Complete documentation for the improved card system based on rygo6/CardExample-Unity patterns.

## Overview

The card system now includes:
- **CardSlot**: Stack-based card organization with automatic positioning
- **HandSlot**: Specialized slot for hand layouts with arc positioning
- **Dealer**: Orchestrates card movements with coroutines and animations
- **DeckManager**: Enhanced with coroutine-based operations
- **CardWorldView**: Smooth movement via TargetTransform, lazy texture loading
- **Singleton<T>**: Generic singleton pattern for managers

---

## 1. CardSlot System

### Basic Slot Setup

```csharp
// Create a basic card slot in scene
GameObject slotObj = new GameObject("DeckSlot");
CardSlot deckSlot = slotObj.AddComponent<CardSlot>();

// Configure
deckSlot.maxCards = 52; // Limit cards in slot (0 = unlimited)
deckSlot.stackOffset = new Vector3(0, 0, 0.01f); // Offset per card in stack
deckSlot.enableRotationJitter = true; // Add slight rotation variation
deckSlot.rotationJitterAmount = 3f; // Degrees of jitter
deckSlot.positionDamp = 0.2f; // Smooth movement damping
deckSlot.rotationDamp = 0.15f; // Smooth rotation damping
```

### Working with Cards

```csharp
// Add card to slot
deckSlot.AddCard(cardView);

// Remove specific card
deckSlot.RemoveCard(cardView);

// Draw top card
CardWorldView topCard = deckSlot.DrawTopCard();

// Get all cards (read-only)
var allCards = deckSlot.GetAllCards();

// Transfer all cards to another slot
deckSlot.TransferAllTo(discardSlot);

// Shuffle cards in slot
deckSlot.Shuffle();

// Clear all cards
deckSlot.Clear();

// Calculate total value
int totalValue = deckSlot.CalculateTotalValue();
```

### Events

```csharp
deckSlot.OnCardAdded += (card) => Debug.Log($"Card added: {card.CardData.name}");
deckSlot.OnCardRemoved += (card) => Debug.Log($"Card removed: {card.CardData.name}");
deckSlot.OnSlotCleared += () => Debug.Log("Slot cleared!");
```

---

## 2. HandSlot (Specialized Hand Layout)

HandSlot extends CardSlot with arc layout for player hands.

```csharp
// Create hand slot
GameObject handObj = new GameObject("PlayerHand");
HandSlot handSlot = handObj.AddComponent<HandSlot>();

// Configure arc layout
handSlot.radius = 11.8f; // Arc radius
handSlot.maxAngle = 19.3f; // Maximum spread angle
handSlot.baseY = -1.94f; // Base Y position
handSlot.spacing = 2f; // Space between cards
handSlot.overlap = 0.75f; // Overlap ratio (0-1)
handSlot.tiltX = 0f; // X-axis tilt
handSlot.baseSortingOrder = 10; // Starting sorting order

// Smoothing
handSlot.useCustomSmoothing = true;
handSlot.smoothTime = 0.2f;
handSlot.rotationSmoothTime = 0.2f;
```

### Hand-Specific Operations

```csharp
// Insert card at specific index
handSlot.InsertCard(card, index: 2);

// Sort by value
handSlot.SortByValue(ascending: true);

// Sort by suit and value
handSlot.SortBySuitAndValue();

// Show drop preview gap
handSlot.SetGapIndex(3); // Show gap at index 3
handSlot.ClearGap(); // Remove gap

// Find drop index from world position
Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
int dropIndex = handSlot.GetDropIndexForPosition(mouseWorldPos);

// Calculate arc Y for X position
float arcY = handSlot.GetArcYForLocalX(localX: 2.5f);
```

---

## 3. Dealer Pattern (Orchestration)

Dealer manages card movements between slots with coroutines.

```csharp
// Setup Dealer
GameObject dealerObj = new GameObject("Dealer");
Dealer dealer = dealerObj.AddComponent<Dealer>();

// Assign slots
dealer.deckSlot = deckSlot;
dealer.discardSlot = discardSlot;
dealer.playerHandSlots.Add(playerHand1);
dealer.playerHandSlots.Add(playerHand2);

// Configure timing
dealer.cardDealDelay = 0.05f; // Delay between each card
dealer.cardShuffleDelay = 0.01f; // Delay during shuffle
dealer.actionCompleteDelay = 0.2f; // Wait after action complete
dealer.dealPositionDamp = 0.15f; // Movement smoothness
dealer.dealRotationDamp = 0.1f; // Rotation smoothness
```

### Dealing Cards

```csharp
// Deal cards to single slot
dealer.DealCards(targetSlot: playerHand1, count: 5, faceUp: true);

// Deal to all players (round-robin)
dealer.DealToAllPlayers(cardsPerPlayer: 5, faceUp: true);

// Transfer cards between slots
dealer.TransferAllCards(fromSlot: discardSlot, toSlot: deckSlot, faceUp: false);

// Shuffle a slot visually
dealer.ShuffleSlot(deckSlot);

// Collect all cards back to deck
dealer.CollectAllCards();

// Stop current action
dealer.StopCurrentAction();

// Check if dealing
if (dealer.IsDealing)
    Debug.Log("Dealer is busy...");
```

### Utility Methods

```csharp
// Count all cards in play
int totalCards = dealer.GetTotalCardCount();

// Clear all slots
dealer.ClearAllSlots();
```

---

## 4. Enhanced DeckManager (With Coroutines)

```csharp
// Initialize deck
deckManager.Initialize(spriteDatabase);

// Draw cards with coroutine (animated)
StartCoroutine(deckManager.DrawCardsCoroutine(
    count: 5,
    delayBetweenCards: 0.1f,
    onCardDrawn: (card) => Debug.Log($"Drew {card.name}"),
    respectLimit: true
));

// Shuffle with animation
StartCoroutine(deckManager.ShuffleCoroutine(
    shuffleIterations: 3,
    delayBetweenShuffle: 0.15f,
    onShuffleComplete: () => Debug.Log("Shuffle done!")
));

// Return cards to deck
deckManager.ReturnCard(card, shuffle: true);
deckManager.ReturnCards(cardList, shuffle: true);

// Return cards with animation
StartCoroutine(deckManager.ReturnCardsCoroutine(
    cards: cardList,
    delayBetweenCards: 0.05f,
    shuffleAtEnd: true
));
```

---

## 5. CardWorldView Enhancements

### Smooth Movement via TargetTransform

Cards now smoothly move to their parent slot positions automatically.

```csharp
// Set movement damping (called automatically by slots)
card.SetMovementDamp(positionDamp: 0.2f, rotationDamp: 0.15f);

// Manual target control (if needed)
if (card.TargetTransform == null)
{
    card.TargetTransform = targetObject.transform;
}

// Movement happens automatically in LateUpdate via SmoothToTargetTransform()
```

### Lazy Texture Loading

Optimize memory by loading/unloading textures based on visibility.

```csharp
// Enable lazy loading in inspector or code
card.enableLazyLoading = true;
card.visibilityAngleThreshold = 60f; // Max angle with camera
card.visibilityDistanceThreshold = 20f; // Max distance

// Textures load/unload automatically in LateUpdate based on camera view
// Useful for large decks or mobile optimization
```

---

## 6. Singleton Pattern

### Creating a Singleton Manager

```csharp
// Persistent across scenes
public class GameManager : Singleton<GameManager>
{
    protected override void Awake()
    {
        base.Awake(); // Important!
        // Your init code here
    }
    
    public void StartGame()
    {
        Debug.Log("Game started!");
    }
}

// Access from anywhere
GameManager.Instance.StartGame();

// Check if exists without creating
if (GameManager.Exists)
    Debug.Log("GameManager is active");
```

### Scene-Only Singleton

```csharp
// Dies when scene changes
public class LevelManager : SingletonSceneOnly<LevelManager>
{
    protected override void Awake()
    {
        base.Awake();
        // Scene-specific init
    }
}

// Usage is identical
LevelManager.Instance.DoSomething();
```

---

## Complete Game Setup Example

```csharp
using UnityEngine;
using System.Collections;

public class CardGameSetup : MonoBehaviour
{
    [Header("References")]
    public CardSpriteDatabase spriteDatabase;
    public DeckManager deckManager;
    public Dealer dealer;
    public CardSlot deckSlot;
    public CardSlot discardSlot;
    public HandSlot player1Hand;
    public HandSlot player2Hand;
    
    [Header("Prefabs")]
    public GameObject cardWorldPrefab;

    private void Start()
    {
        StartCoroutine(SetupGameCoroutine());
    }

    private IEnumerator SetupGameCoroutine()
    {
        Debug.Log("=== Starting Card Game Setup ===");

        // 1. Initialize deck
        deckManager.Initialize(spriteDatabase);
        Debug.Log($"Deck initialized with {deckManager.RemainingCards} cards");

        // 2. Shuffle with animation
        yield return deckManager.ShuffleCoroutine(
            shuffleIterations: 3,
            delayBetweenShuffle: 0.2f,
            onShuffleComplete: () => Debug.Log("Deck shuffled!")
        );

        yield return new WaitForSeconds(0.5f);

        // 3. Create card views and add to deck slot
        for (int i = 0; i < deckManager.RemainingCards; i++)
        {
            if (deckManager.TryDrawCard(out Card cardData, respectLimit: false))
            {
                // Instantiate card view
                GameObject cardObj = Instantiate(cardWorldPrefab, deckSlot.transform.position, Quaternion.identity);
                CardWorldView cardView = cardObj.GetComponent<CardWorldView>();
                
                // Bind card data
                Sprite back = spriteDatabase.GetBackSprite();
                Sprite face = spriteDatabase.GetSprite(cardData.suit, cardData.value);
                cardView.Bind(FindObjectOfType<GameBootstrap>(), cardData, back, face, false);
                
                // Add to deck slot
                deckSlot.AddCard(cardView);
            }
        }

        Debug.Log($"Created {deckSlot.CardCount} card views");

        yield return new WaitForSeconds(1f);

        // 4. Deal cards to players
        Debug.Log("Dealing cards to players...");
        dealer.DealToAllPlayers(cardsPerPlayer: 5, faceUp: true);

        // Wait for dealing to complete
        while (dealer.IsDealing)
            yield return null;

        Debug.Log("=== Game Setup Complete ===");
        Debug.Log($"Player 1 hand: {player1Hand.CardCount} cards, value: {player1Hand.CalculateTotalValue()}");
        Debug.Log($"Player 2 hand: {player2Hand.CardCount} cards, value: {player2Hand.CalculateTotalValue()}");
        Debug.Log($"Deck remaining: {deckSlot.CardCount} cards");
    }

    // Example: Draw a card for player 1
    public void Player1DrawCard()
    {
        if (dealer.IsDealing)
        {
            Debug.Log("Dealer is busy!");
            return;
        }

        dealer.DealCards(player1Hand, count: 1, faceUp: true);
    }

    // Example: Collect all cards and reshuffle
    public void CollectAndReshuffle()
    {
        if (dealer.IsDealing)
            return;

        StartCoroutine(CollectAndReshuffleCoroutine());
    }

    private IEnumerator CollectAndReshuffleCoroutine()
    {
        // Collect all cards back to deck
        dealer.CollectAllCards();
        
        while (dealer.IsDealing)
            yield return null;

        // Shuffle
        dealer.ShuffleSlot(deckSlot);
        
        while (dealer.IsDealing)
            yield return null;

        Debug.Log("All cards collected and reshuffled!");
    }
}
```

---

## Best Practices

### 1. Slot Hierarchy
```
GameBoard (GameObject)
├── DeckSlot (CardSlot)
├── DiscardSlot (CardSlot)
├── Player1Hand (HandSlot)
└── Player2Hand (HandSlot)
```

### 2. Card Movement Flow
```
Deck -> TargetTransform created -> Smooth movement in LateUpdate -> Card reaches position
```

### 3. Using Dealer vs Direct Slot Operations
- **Use Dealer**: For visual gameplay (dealing, shuffling, transferring)
- **Use Slots directly**: For instant setup, testing, or non-animated operations

### 4. Performance Tips
- Enable lazy loading for large card counts (50+ cards)
- Use CardSlot.maxCards to prevent memory issues
- Clear unused slots with `slot.Clear()`
- Reuse card GameObjects instead of destroying/instantiating

### 5. Event-Driven Updates
```csharp
// Subscribe to slot events for UI updates
deckSlot.OnCardRemoved += UpdateDeckCounterUI;
player1Hand.OnCardAdded += UpdateHandValueUI;
deckManager.OnDeckEmpty += ShowGameOverScreen;
```

---

## Migration from Old System

### Old Code
```csharp
// Old HandWorldLayout
handLayout.Apply(cards, instant: false, gapIndex: -1);
```

### New Code
```csharp
// New HandSlot
foreach (var card in cards)
    handSlot.AddCard(card);

// Gap for drag preview
handSlot.SetGapIndex(dropIndex);
```

---

## Debugging Tips

### Enable Gizmos
All slots draw gizmos in Scene view:
- **CardSlot**: Shows card positions and slot bounds
- **HandSlot**: Shows arc curve, card positions, and gap indicator

### Check Card State
```csharp
Debug.Log($"Card locked: {card.IsLayoutLocked}");
Debug.Log($"Parent slot: {card.ParentSlot?.name ?? "None"}");
Debug.Log($"Target transform: {card.TargetTransform?.position}");
```

### Monitor Dealer
```csharp
Debug.Log($"Dealer busy: {dealer.IsDealing}");
Debug.Log($"Total cards: {dealer.GetTotalCardCount()}");
```

---

## API Quick Reference

### CardSlot
- `AddCard(card)` - Add card to slot
- `RemoveCard(card)` - Remove specific card
- `DrawTopCard()` - Remove and return top card
- `GetAllCards()` - Get read-only list
- `TransferAllTo(otherSlot)` - Move all cards
- `Shuffle()` - Randomize order
- `Clear()` - Remove all cards
- `CalculateTotalValue()` - Sum card values

### HandSlot (+ CardSlot methods)
- `InsertCard(card, index)` - Insert at specific position
- `SortByValue(ascending)` - Sort by card value
- `SortBySuitAndValue()` - Sort by suit then value
- `SetGapIndex(index)` / `ClearGap()` - Visual drop preview
- `GetDropIndexForPosition(worldPos)` - Find insertion index
- `GetArcYForLocalX(x)` - Calculate arc position

### Dealer
- `DealCards(slot, count, faceUp)` - Deal to one slot
- `DealToAllPlayers(count, faceUp)` - Round-robin dealing
- `TransferAllCards(from, to, faceUp)` - Move between slots
- `ShuffleSlot(slot)` - Shuffle with animation
- `CollectAllCards()` - Gather all to deck
- `StopCurrentAction()` - Cancel current operation
- `IsDealing` - Check if busy

### DeckManager
- `Initialize(database)` - Setup deck
- `TryDrawCard(out card, respectLimit)` - Draw one card
- `DrawCardsCoroutine(count, delay, callback)` - Animated draw
- `ShuffleCoroutine(iterations, delay, callback)` - Animated shuffle
- `ReturnCard(card, shuffle)` / `ReturnCards(list, shuffle)` - Return cards
- `ReturnCardsCoroutine(list, delay, shuffle)` - Animated return

### CardWorldView
- `Bind(owner, card, back, face, faceUp)` - Initialize card
- `SetMovementDamp(posDamp, rotDamp)` - Configure smoothing
- `SetFaceUp(bool)` - Flip card
- `ParentSlot` - Current slot (or null)
- `TargetTransform` - Movement target (auto-created)

---

## Conclusion

This improved card system provides:
✅ **Organization**: CardSlot/HandSlot for automatic layout  
✅ **Animation**: Dealer pattern with coroutines  
✅ **Smoothness**: TargetTransform-based movement  
✅ **Optimization**: Lazy texture loading  
✅ **Architecture**: Singleton pattern for managers  
✅ **Flexibility**: Event system for custom logic  

All inspired by best practices from rygo6/CardExample-Unity while adapted for this project's architecture!
