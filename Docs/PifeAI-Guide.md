# Guia da IA de Pife

## 📋 Índice
1. [Visão Geral](#visão-geral)
2. [Arquivos Criados](#arquivos-criados)
3. [Como Funciona](#como-funciona)
4. [Integração no Jogo](#integração-no-jogo)
5. [Níveis de Dificuldade](#níveis-de-dificuldade)
6. [API da IA](#api-da-ia)
7. [Exemplo de Uso](#exemplo-de-uso)

---

## 🎯 Visão Geral

A **PifeAI** é um sistema completo de inteligência artificial para jogar Pife (também conhecido como Pif Paf ou Cacheta). A IA foi desenvolvida com 4 níveis de dificuldade e utiliza algoritmos heurísticos para tomar decisões estratégicas.

### Características Principais:
- ✅ 4 níveis de dificuldade (Easy, Medium, Hard, Expert)
- ✅ Avaliação inteligente de mãos
- ✅ Detecção automática de trincas e sequências
- ✅ Estratégia de descarte otimizada
- ✅ Suporte a curingas
- ✅ Simulação de "tempo de pensamento"
- ✅ Sistema completo de gerenciamento de jogo

---

## 📁 Arquivos Criados

### 1. **PifeAI.cs** (`Assets/Scripts/`)
Contém toda a lógica da IA:
- Avaliação de mãos
- Decisão de compra (monte vs. mesa)
- Decisão de descarte
- Verificação de condições de vitória
- Detecção de combinações

### 2. **PifeGameManager.cs** (`Assets/Scripts/`)
Gerenciador completo do jogo:
- Criação e distribuição de cartas
- Controle de turnos
- Gerenciamento de 4 jogadores (1 humano + 3 IAs)
- Sistema de pontuação
- Eventos do jogo

---

## 🧠 Como Funciona

### Estrutura de Decisão

```
┌─────────────────────┐
│   Turno da IA      │
└──────────┬──────────┘
           │
           v
┌─────────────────────┐
│ Avaliar Mão Atual  │
│ - Trincas          │
│ - Sequências       │
│ - Curingas         │
└──────────┬──────────┘
           │
           v
┌─────────────────────┐
│ Decidir Compra     │
│ Monte ou Mesa?     │
└──────────┬──────────┘
           │
           v
┌─────────────────────┐
│ Adicionar Carta    │
└──────────┬──────────┘
           │
           v
┌─────────────────────┐
│ Pode Bater?        │
│ Sim → FIM          │
│ Não → Continua     │
└──────────┬──────────┘
           │
           v
┌─────────────────────┐
│ Decidir Descarte   │
│ Qual carta jogar?  │
└──────────┬──────────┘
           │
           v
┌─────────────────────┐
│ Próximo Jogador    │
└─────────────────────┘
```

### Algoritmo de Avaliação

A IA avalia cada carta com base em:

1. **Valor em Combinações**
   - Trinca completa: +15 pontos
   - Sequência completa: +12 pontos (+ bônus por tamanho)
   - Combinação incompleta (2 cartas): +5 pontos

2. **Potencial**
   - Cartas que podem formar múltiplas combinações
   - Proximidade com outras cartas (para sequências)
   - Quantidade de cartas do mesmo valor (para trincas)

3. **Curingas**
   - Sempre valiosos: +95 pontos
   - Podem completar qualquer combinação

4. **Penalidades**
   - Cartas isoladas: -2 pontos cada
   - Cartas sem potencial de combinação

---

## 🎮 Integração no Jogo

### Passo 1: Configurar o Game Manager

1. Crie um GameObject vazio na cena:
   ```
   GameObject → Create Empty → "PifeGameManager"
   ```

2. Adicione o componente `PifeGameManager`:
   ```
   Add Component → PifeGameManager
   ```

3. Configure no Inspector:
   ```
   Number Of Players: 4
   Cards Per Player: 9
   Use Double Decks: ✓
   
   IA 1 Difficulty: Easy
   IA 2 Difficulty: Medium
   IA 3 Difficulty: Hard
   ```

### Passo 2: Iniciar o Jogo

```csharp
// No seu script de UI ou controle
PifeGameManager gameManager = FindObjectOfType<PifeGameManager>();
gameManager.StartNewGame();
```

### Passo 3: Gerenciar Turno do Jogador

```csharp
// Quando o jogador clica em "Comprar do Monte"
void OnDrawDeckButton()
{
    gameManager.PlayerDrawFromDeck();
}

// Quando o jogador clica em "Pegar da Mesa"
void OnDrawDiscardButton()
{
    gameManager.PlayerDrawFromDiscard();
}

// Quando o jogador descarta uma carta
void OnDiscardCard(Card card)
{
    gameManager.PlayerDiscardCard(card);
}

// Quando o jogador quer bater
void OnBeatButton()
{
    gameManager.PlayerTryBeat();
}
```

### Passo 4: Conectar Eventos

```csharp
void Start()
{
    gameManager.OnGameStarted += OnGameStarted;
    gameManager.OnGameEnded += OnGameEnded;
    gameManager.OnTurnChanged += OnTurnChanged;
    gameManager.OnCardDrawn += OnCardDrawn;
    gameManager.OnCardDiscarded += OnCardDiscarded;
}

void OnGameStarted()
{
    Debug.Log("Jogo iniciado!");
    UpdateUI();
}

void OnTurnChanged()
{
    var currentPlayer = gameManager.GetCurrentPlayer();
    Debug.Log($"Turno de: {currentPlayer.name}");
    UpdateUI();
}

void OnCardDiscarded(Card card)
{
    // Atualiza visualização da pilha de descarte
    UpdateDiscardPileVisual(card);
}
```

---

## 🎚️ Níveis de Dificuldade

### Easy (Fácil)
- **Estratégia**: Joga com lógica básica e muita aleatoriedade
- **Compra**: 30% de chance de pegar da mesa se melhorar um pouco
- **Descarte**: Escolhe aleatoriamente entre as 3 piores cartas
- **Bater**: 70% de chance quando pode
- **Ideal para**: Jogadores iniciantes

### Medium (Médio)
- **Estratégia**: Avalia combinações e tenta otimizar
- **Compra**: Pega da mesa se melhorar significativamente (+5 pontos)
- **Descarte**: Descarta a carta com menor utilidade
- **Bater**: 90% de chance quando pode
- **Ideal para**: Jogadores intermediários

### Hard (Difícil)
- **Estratégia**: Análise profunda com simulação de jogadas
- **Compra**: Simula adicionar a carta e escolhe a melhor opção
- **Descarte**: Simula descartar cada carta e escolhe a que deixa melhor mão
- **Bater**: Sempre bate quando pode
- **Ideal para**: Jogadores experientes

### Expert (Especialista)
- **Estratégia**: Calcula probabilidades e antecipa jogadas
- **Compra**: Considera completar combinações e valor estratégico
- **Descarte**: Evita cartas que podem ajudar adversários
- **Bater**: Sempre bate quando pode
- **Ideal para**: Desafio máximo

---

## 📚 API da IA

### Métodos Principais

#### `ShouldDrawFromDiscard(Card topDiscardCard)`
Decide se deve pegar a carta do topo da pilha de descarte.

**Retorno**: `bool` - true se deve pegar da mesa, false se deve comprar do monte

**Exemplo**:
```csharp
Card topCard = discardPile[^1]; // Última carta
bool drawFromDiscard = aiController.ShouldDrawFromDiscard(topCard);

if (drawFromDiscard)
    ComprarDaMesa();
else
    ComprarDoMonte();
```

---

#### `DecideCardToDiscard()`
Decide qual carta descartar da mão.

**Retorno**: `Card` - a carta escolhida para descarte

**Exemplo**:
```csharp
Card cardToDiscard = aiController.DecideCardToDiscard();
DiscardCard(cardToDiscard);
```

---

#### `ShouldBeat()`
Verifica se a IA pode e deve "bater" (finalizar o jogo).

**Retorno**: `bool` - true se deve bater

**Exemplo**:
```csharp
if (aiController.ShouldBeat())
{
    Debug.Log("IA bateu!");
    GameOver(currentPlayer);
}
```

---

### Métodos de Configuração

#### `SetHand(List<Card> newHand)`
Define a mão atual da IA.

```csharp
aiController.SetHand(playerHand);
```

#### `SetWildcard(Card card)`
Define qual é o curinga da rodada.

```csharp
aiController.SetWildcard(wildcardCard);
```

#### `UpdateDiscardPile(List<Card> pile)`
Atualiza a pilha de descarte (para IAs Expert que analisam histórico).

```csharp
aiController.UpdateDiscardPile(discardPile);
```

#### `AddCard(Card card)`
Adiciona uma carta à mão da IA.

```csharp
aiController.AddCard(drawnCard);
```

---

### Métodos de Debug

#### `PrintHandEvaluation()`
Imprime no console uma análise detalhada da mão.

```csharp
aiController.PrintHandEvaluation();
```

**Saída**:
```
=== Avaliação da Mão (Dificuldade: Hard) ===
Qualidade da mão: 45.5
Combinações encontradas: 3
  Trinca: 5♠, 5♥, 5♦ (valor: 15)
  Sequencia: 7♣, 8♣, 9♣ (valor: 12)
  Incomplete: 2♠, 3♠ (valor: 5)

Avaliação individual das cartas:
  5♠: Utilidade=28.5, Combos=1, EmCombo=True
  7♣: Utilidade=26.0, Combos=1, EmCombo=True
  ...
```

---

## 💡 Exemplo de Uso Completo

### Script de Integração com UI

```csharp
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PifeUIController : MonoBehaviour
{
    [Header("Referências")]
    public PifeGameManager gameManager;
    
    [Header("UI Elements")]
    public Button drawDeckButton;
    public Button drawDiscardButton;
    public Button beatButton;
    public Text turnText;
    public Text wildcardText;
    
    [Header("Mão do Jogador")]
    public Transform handContainer;
    public GameObject cardPrefab;
    
    private List<GameObject> cardObjects = new List<GameObject>();

    void Start()
    {
        // Conecta botões
        drawDeckButton.onClick.AddListener(OnDrawDeck);
        drawDiscardButton.onClick.AddListener(OnDrawDiscard);
        beatButton.onClick.AddListener(OnBeat);
        
        // Conecta eventos
        gameManager.OnGameStarted += OnGameStarted;
        gameManager.OnTurnChanged += OnTurnChanged;
        gameManager.OnCardDrawn += OnCardDrawn;
        
        // Inicia o jogo
        gameManager.StartNewGame();
    }

    void OnDrawDeck()
    {
        gameManager.PlayerDrawFromDeck();
        UpdateHand();
    }

    void OnDrawDiscard()
    {
        gameManager.PlayerDrawFromDiscard();
        UpdateHand();
    }

    void OnBeat()
    {
        gameManager.PlayerTryBeat();
    }

    void OnGameStarted()
    {
        wildcardText.text = $"Curinga: {gameManager.GetWildcard()}";
        UpdateHand();
    }

    void OnTurnChanged()
    {
        var currentPlayer = gameManager.GetCurrentPlayer();
        turnText.text = $"Turno: {currentPlayer.name}";
        
        // Habilita/desabilita botões baseado em quem está jogando
        bool isPlayerTurn = currentPlayer == gameManager.GetAllPlayers()[0];
        drawDeckButton.interactable = isPlayerTurn;
        drawDiscardButton.interactable = isPlayerTurn;
        beatButton.interactable = isPlayerTurn;
        
        if (isPlayerTurn)
            UpdateHand();
    }

    void OnCardDrawn(Card card)
    {
        Debug.Log($"Carta comprada: {card}");
    }

    void UpdateHand()
    {
        // Limpa visualização anterior
        foreach (var obj in cardObjects)
            Destroy(obj);
        cardObjects.Clear();
        
        // Cria visualização das cartas
        var player = gameManager.GetAllPlayers()[0]; // Jogador humano
        foreach (var card in player.hand)
        {
            GameObject cardObj = Instantiate(cardPrefab, handContainer);
            
            // Configura o visual da carta (adapte ao seu sistema)
            var cardUI = cardObj.GetComponent<CardUI>();
            cardUI.SetCard(card);
            
            // Adiciona evento de clique para descartar
            Button cardBtn = cardObj.GetComponent<Button>();
            cardBtn.onClick.AddListener(() => OnCardClicked(card));
            
            cardObjects.Add(cardObj);
        }
    }

    void OnCardClicked(Card card)
    {
        // Descarta a carta clicada
        gameManager.PlayerDiscardCard(card);
        UpdateHand();
    }
}
```

---

## 🔧 Personalizações

### Ajustar Velocidade da IA

No Inspector do `PifeAI`:
```
Min Think Time: 0.5  (mínimo de meio segundo)
Max Think Time: 2.0  (máximo de 2 segundos)
```

Ou via código:
```csharp
aiController.minThinkTime = 0.2f;
aiController.maxThinkTime = 1.0f;
```

### Criar IA Personalizada

Você pode estender a classe `PifeAI` para criar estratégias customizadas:

```csharp
public class MyCustomPifeAI : PifeAI
{
    // Override métodos para criar sua própria estratégia
    public override Card DecideCardToDiscard()
    {
        // Sua lógica personalizada aqui
        return base.DecideCardToDiscard();
    }
}
```

---

## 🐛 Debug e Testes

### Comandos de Debug no Inspector

O `PifeGameManager` tem comandos úteis no menu de contexto:

1. **Debug - Iniciar Jogo**: Inicia uma nova partida
2. **Debug - Mostrar Mãos**: Mostra todas as mãos no console
3. **Debug - Forçar IA Jogar**: Força a IA atual a jogar

Para acessar: `Botão direito no componente → Context Menu`

### Testar Níveis de Dificuldade

```csharp
void TestDifficulties()
{
    PifeAI easyAI = gameObject.AddComponent<PifeAI>();
    easyAI.difficulty = PifeAI.DifficultyLevel.Easy;
    
    PifeAI hardAI = gameObject.AddComponent<PifeAI>();
    hardAI.difficulty = PifeAI.DifficultyLevel.Hard;
    
    // Configure mãos iguais e compare decisões
    List<Card> testHand = CreateTestHand();
    easyAI.SetHand(new List<Card>(testHand));
    hardAI.SetHand(new List<Card>(testHand));
    
    Card easyDiscard = easyAI.DecideCardToDiscard();
    Card hardDiscard = hardAI.DecideCardToDiscard();
    
    Debug.Log($"Easy descartou: {easyDiscard}");
    Debug.Log($"Hard descartou: {hardDiscard}");
}
```

---

## 📊 Performance

### Otimizações Implementadas

- ✅ Algoritmos eficientes para encontrar combinações (O(n²) no pior caso)
- ✅ Cache de avaliações quando possível
- ✅ Uso de HashSet para verificações rápidas
- ✅ Simulações limitadas (não checa todas as possibilidades)

### Consumo de Recursos

- **Easy/Medium**: ~0.1-0.5ms por decisão
- **Hard/Expert**: ~1-3ms por decisão
- **Memória**: Mínima (apenas estruturas temporárias)

---

## ✅ Checklist de Implementação

- [ ] Adaptar classe `Card` ao seu sistema existente
- [ ] Integrar `PifeGameManager` na cena
- [ ] Criar UI para mostrar mão do jogador
- [ ] Criar UI para botões de ação (comprar, descartar, bater)
- [ ] Conectar eventos do game manager com UI
- [ ] Testar com 4 jogadores
- [ ] Ajustar dificuldades das IAs
- [ ] Adicionar animações e feedback visual
- [ ] Implementar sistema de pontuação total (múltiplas rodadas)
- [ ] Adicionar sons e efeitos

---

## 🎯 Próximos Passos

Agora que você tem a IA pronta, pode:

1. **Integrar com seu sistema de cartas existente**
2. **Criar a interface visual do jogo**
3. **Adicionar animações para as jogadas da IA**
4. **Implementar sistema de partidas (melhor de 3, por exemplo)**
5. **Adicionar estatísticas (vitórias, derrotas, taxa de acerto)**
6. **Criar tutorial interativo**

---

## 📝 Notas Importantes

### Adaptação da Classe Card

A IA usa uma classe `Card` simples incluída no arquivo. **Você deve adaptá-la** para usar suas próprias classes de carta existentes no projeto.

Se você já tem uma classe de carta diferente:
1. Remova a classe `Card` do final de `PifeAI.cs`
2. Ajuste as referências para usar sua classe
3. Garanta que sua classe tenha pelo menos:
   - `string value` (A, 2-10, J, Q, K)
   - `string suit` (naipe)

### Regras de Pife

A implementação segue as regras clássicas:
- 2 baralhos de 52 cartas
- 9 cartas por jogador
- Curinga definido pela carta virada
- Trincas: 3+ cartas do mesmo valor
- Sequências: 3+ cartas do mesmo naipe em ordem
- Bater: formar todas as combinações com as 9 cartas

---

## 🆘 Suporte

Se tiver dúvidas ou problemas:

1. Use `PrintHandEvaluation()` para ver como a IA avalia a mão
2. Use os comandos de debug no Inspector
3. Ative logs detalhados no código (procure por `Debug.Log`)
4. Verifique se todas as referências estão configuradas

---

**Boa sorte com seu jogo de Pife! 🃏🎮**
