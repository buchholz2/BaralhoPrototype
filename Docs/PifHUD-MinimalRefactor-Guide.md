# HUD Minimalista do Pife - Refatoração Completa

## 📋 O QUE FOI FEITO

### Arquivos Criados

1. **PifHUD.cs** - Gerenciador principal do HUD
   - Controla TopBar, PlayerCards, MeldBoard
   - Gerencia turno atual e highlights
   - Interface para atualizar pontos e cartas

2. **PlayerCard.cs** - Card visual de jogador
   - Mostra avatar, nome, pontos, cantidad de cartas
   - Highlight quando é o turno do jogador
   - SortWidget integrado (apenas jogador local)
   - Ordenar começa em "None" (desativado)

3. **MeldBoard.cs** - Board de trincas/jogos baixados
   - 4 Lanes (Norte, Oeste, Leste, Local)
   - Praticamente invisível quando vazio
   - Lanes aparecem sutilmente quando há trincas
   - Cartas na mesa em escala menor (0.75x)

4. **RoundSummaryModal.cs** - Modal de fim de rodada
   - Pontuação por jogador
   - Histórico de rodadas (estilo boliche)
   - Ranking final (1º, 2º, 3º, 4º)
   - Desativado por padrão

5. **PifHUDSetupTool.cs** - Ferramenta de Editor
   - Cria toda estrutura automaticamente
   - Menu: Tools > Pif > Setup Minimal HUD

## 🎨 LAYOUT MINIMALISTA

### Princípios Seguidos:
✅ Mesa verde é o background - SEM painéis/caixas gigantes
✅ Apenas elementos essenciais visíveis
✅ MeldBoard só aparece quando há trincas baixadas
✅ TopBar discreto (72px, fundo alpha 0.15)
✅ PlayerCards pequenos e informativos
✅ Ordenar integrado no card do jogador local

### Estrutura Visual:

```
┌─────────────────────────────────────────────────┐
│  TopBar (discreto)                              │
│  [Sala PIF]    [Vez: Jogador]    [Config][Sair]│
├─────────────────────────────────────────────────┤
│                                                 │
│              [PlayerCard Norte]                 │
│                                                 │
│  [PlayerCard            [Monte] [Lixo]         │
│   Oeste]                                   [East│
│                  (centro limpo)             Card│
│                                                ]│
│         MeldBoard (4 lanes quase invisíveis)    │
│         - Lance Norte (trincas de IA 1)         │
│         - Lane Oeste (trincas de IA 2)          │
│         - Lane Leste (trincas de IA 3)          │
│         - Lane Local (suas trincas)             │
│                                                 │
│  [PlayerCard Você]                              │
│  Avatar | Nome                                  │
│  0 pts | 9 cartas                               │
│  Ordenar: [♣ Naipe] [123 Valor]                │
│                                                 │
│         (Mão do jogador aqui embaixo)           │
└─────────────────────────────────────────────────┘
```

## 🔧 COMO USAR

### Instalação Automática:

1. **Abra a scene PifTable.unity**
   `Assets/Scenes/PifTable.unity`

2. **Execute o setup automático:**
   - Menu superior: `Tools > Pif > Setup Minimal HUD`
   - Isso criará toda estrutura do HUD automaticamente

3. **Configuração do Canvas:**
   - Canvas Scaler: Scale With Screen Size
   - Reference: 1920x1080
   - Match: 0.5
   - Safe margins: 64px (x), 48px (y)

### Verificação Pós-Setup:

1. **Desativar HUDs antigos:**
   - Na Hierarchy, procure por Canvas duplicados
   - Deixe SOMENTE o Canvas com "PifHUD_Minimal" ativo
   - Desative ou delete os antigos

2. **Remover artefato debug:**
   - Procure qualquer objeto azul/cinza no canto inferior esquerdo
   - Geralmente é um debug panel ou old UI element
   - Delete ou desative

3. **Testar no Play:**
   - Pressione Play
   - Deve aparecer apenas: TopBar + PlayerCards + Centro limpo
   - MeldBoard lanes devem estar praticamente invisíveis

## 📦 COMPONENTES DETALHADOS

### PifHUD (Gerenciador Principal)

**Referências Públicas:**
- `roomNameText` - Texto "Sala PIF"
- `currentTurnText` - Texto "Vez: Jogador"
- `playerCardNorth/West/East/Local` - Os 4 PlayerCards
- `drawPileRoot/discardPileRoot` - Raízes do monte e lixo
- `meldBoard` - Board de trincas
- `roundSummaryModal` - Modal de resumo

**Métodos Principais:**
```csharp
SetRoomName(string name)
SetCurrentTurn(int playerIndex) // 0=Local, 1=Norte, 2=Oeste, 3=Leste
UpdatePlayerScore(int playerIndex, int score)
UpdatePlayerCardCount(int playerIndex, int count)
ShowRoundSummary(RoundSummaryData data)
```

### PlayerCard (Card de Jogador)

**Elementos:**
- Avatar (Image placeholder)
- NameText (nome do jogador)
- ScoreText ("0 pts")
- CardCountText ("9 cartas")
- HighlightOutline (brilha quando é o turno)
- SortWidget (apenas jogador local)

**Métodos:**
```csharp
Initialize(string name, int score, int cardCount, bool isLocal)
SetScore(int score)
SetCardCount(int count)
SetHighlight(bool highlighted)
SetAvatarSprite(Sprite sprite)
```

**Sort Widget:**
- Começa em "None" (nenhum botão selecionado)
- Ao clicar em um modo:
  - Ele fica visual "selecionado" + disabled
  - O outro fica habilitado (pode trocar)
- Estados: None, BySuit (♣ Naipe), ByRank (123 Valor)

### MeldBoard (Board de Trincas)

**Estrutura:**
- 4 MeldLanes (Norte, Oeste, Leste, Local)
- Cada lane pode ter múltiplos MeldGroups
- MeldGroup = conjunto de cartas sobrepostas

**Métodos:**
```csharp
ShowMeldGroup(int playerIndex, List<Card> cards)
ClearPlayerMelds(int playerIndex)
ClearAllMelds()
```

**Visual:**
- Background ultra-sutil (alpha 0.05) quando vazio
- Aumenta para alpha 0.12 quando há trincas
- Cartas em escala 0.75x
- Overlap horizontal de 70%

### RoundSummaryModal (Modal de Resumo)

**Conteúdo:**
- Número da rodada
- Vencedor
- Pontuação de todos jogadores
- Histórico de rodadas
- Ranking final (apenas última rodada)

**Uso:**
```csharp
RoundSummaryData data = new RoundSummaryData
{
    roundNumber = 1,
    winnerName = "Você",
    isFinalRound = false,
    playerScores = new List<PlayerScoreData>(),
    roundHistory = new List<RoundHistoryEntry>(),
    finalRanking = new List<RankingEntry>() // Só se isFinalRound = true
};

pifHUD.ShowRoundSummary(data);
```

## 🎯 INTEGRAÇÃO COM GAMEBOOTSTRAP

### Conectar PifHUD ao GameBootstrap existente:

1. **No GameBootstrap.cs, adicionar referência:**
```csharp
[Header("UI")]
public PifHUD pifHUD;
```

2. **Atualizar turno:**
```csharp
void SetCurrentPlayer(int playerIndex)
{
    if (pifHUD != null)
        pifHUD.SetCurrentTurn(playerIndex);
}
```

3. **Atualizar contagem de cartas:**
```csharp
void OnCardDrawn(int playerIndex)
{
    int cardCount = GetPlayerHandCount(playerIndex);
    if (pifHUD != null)
        pifHUD.UpdatePlayerCardCount(playerIndex, cardCount);
}
```

4. **Mostrar trincas baixadas:**
```csharp
void OnPlayerShowsMeld(int playerIndex, List<Card> cards)
{
    if (pifHUD != null && pifHUD.meldBoard != null)
        pifHUD.meldBoard.ShowMeldGroup(playerIndex, cards);
}
```

## ⚠️ CHECKLIST OBRIGATÓRIO

Antes de considerar completo:

- [ ] Apenas 1 Canvas ativo na scene
- [ ] Sem painel central gigante translúcido
- [ ] TopBar discreto e funcional
- [ ] 4 PlayerCards criados e posicionados corretamente
- [ ] Monte e Lixo centralizados
- [ ] MeldBoard com 4 lanes (quase invisível quando vazio)
- [ ] Sort Widget no PlayerCard local (começa None)
- [ ] RoundSummary modal existe mas está desativado
- [ ] Artefato debug removido (objeto azul/cinza canto inferior)
- [ ] GameView limpo e minimalista no Play

## 🐛 TROUBLESHOOTING

### "Não consigo ver o HUD no Play"
- Verifique se PifHUD_Minimal está ativo na Hierarchy
- Confirme que o Canvas tem Canvas Scaler configurado
- Verifique se há outros Canvas ativos escondendo o novo

### "MeldBoard está muito visível mesmo vazio"
- Ajuste alpha do backgroundLine para 0.05 ou menos
- Verifique se as lanes têm Image component com color alpha baixo

### "Sort buttons não funcionam"
- Certifique-se de que os buttons têm referência ao método OnSortModeSelected
- Verifique se o GameBootstrap tem método de sorting implementado

### "PlayerCards não aparecem"
- Confirme que os anchors estão corretos
- LocalPlayer: anchor (64, 64) em pixels
- Norte: anchor (0.5, 1) normalizado
- Oeste: anchor (0, 0.5) normalizado
- Leste: anchor (1, 0.5) normalizado

## 📝 PRÓXIMOS PASSOS

1. **Conectar com GameBootstrap:**
   - Adicionar campo `public PifHUD pifHUD;`
   - Conectar eventos de mudança de turno
   - Conectar eventos de draw/discard de cartas

2. **Implementar lógica de sorting:**
   - Adicionar método no GameBootstrap para ordenar mão
   - Conectar com PlayerCard.OnSortModeSelected

3. **Criar prefab de carta para MeldBoard:**
   - Criar prefab menor para cartas na mesa
   - Escala 0.75x da carta normal
   - Conectar sprite database

4. **Integrar com IA de Pife:**
   - Conectar PifHUD com PifeGameManager
   - Atualizar PlayerCards quando IA joga
   - Mostrar trincas no MeldBoard quando baixadas

5. **Polish visual:**
   - Adicionar avatars/sprites para PlayerCards
   - Animações suaves de highlight
   - Transições ao mostrar trincas

## 📸 SCREENSHOT

Para gerar screenshot do GameView:
1. Entre no Play Mode
2. Selecione Game tab
3. Capture: `Ctrl + Shift + PrtScn` (Unity screenshot)
4. Ou use menu: `Tools > Screenshot > Capture Game View`

---

**Desenvolvido seguindo princípios de UI/UX minimalista**
**Mesa verde como background, zero painéis gigantes, máxima funcionalidade**
