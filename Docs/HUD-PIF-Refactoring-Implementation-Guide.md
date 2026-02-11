# HUD PIF - Guia de Refatoração e Implementação

## ✅ O QUE FOI FEITO AUTOMATICAMENTE

### Branch Criada
- ✅ Branch `hud-pif-refactor` criada e ativa
- ✅ Backup da cena: `Game_HUD_BACKUP.unity` criado

### Correções de Código

1. **Linhas Nítidas (ChalkLine + ChalkTableDemarcation)**
   - ✅ `cornerVertices` aumentado para 8 (cantos suaves sem serrilhado)
   - ✅ `capVertices` aumentado para 6 (pontas suaves)
   - ✅ `simpleLineOpacity` aumentado para 0.32 (linhas mais visíveis)
   - ✅ `cornerSegments` aumentado para 12 (cantos mais arredondados)
   - ✅ Linhas brancas configuradas automaticamente quando `useSimpleWhiteLines = true`

2. **Bug do Botão de Ordenar - CORRIGIDO**
   - ✅ `ResolveInitialSortMode()` agora NÃO aplica sorting automaticamente quando `initialSortMode = None`
   - ✅ Quando mode é `None`, ambos botões ficam habilitados e sem lock
   - ✅ Mão começa na ordem randômica da distribuição (como deve ser)
   - ✅ PlayerCard conectado ao GameBootstrap (chama `SortSuit()` ou `SortRank()`)

3. **Integração PifHUD**
   - ✅ Criado `PifHUDIntegration.cs` (conecta PifHUD ↔ PifeGameManager)
   - ✅ Adicionado `GetCurrentPlayerIndex()` e `GetPlayerHandCount()` no PifeGameManager
   - ✅ PifHUD.meldBoard agora é propriedade pública acessível

### Scripts Já Existentes (Criados Anteriormente)
- ✅ `PifHUD.cs` - Gerenciador principal do HUD
- ✅ `PlayerCard.cs` - Card de jogador (avatar, nome, pontos, cartas, sort widget)
- ✅ `MeldBoard.cs` - Board de trincas/melds com 4 lanes
- ✅ `RoundSummaryModal.cs` - Modal de fim de rodada
- ✅ `PifHUDSetupTool.cs` - Ferramenta de setup automático

---

## 🎯 TAREFAS NO UNITY EDITOR (VOCÊ PRECISA FAZER)

### ETAPA 1: Configurar ChalkTableDemarcation (Linhas Nítidas)

1. **Abra a cena `Game.unity`** (NÃO o backup)

2. **Encontre o GameObject `ChalkTableDemarcation`** na Hierarchy
   - Provavelmente está como filho de TableRoot ou na raiz

3. **No Inspector, configure:**
   ```
   [Simple White Lines]
   ✓ useSimpleWhiteLines = TRUE
   simpleLineColor = White (255, 255, 255, 255)
   simpleLineOpacity = 0.32

   [Rounded Corners]
   ✓ useRoundedCorners = TRUE
   cornerRadius = 0.5
   cornerSegments = 12

   [Style]
   thickness = 0.03 (ou ajuste para linhas mais finas/grossas)
   opacity = 0.34 (se não estiver usando linhas brancas simples)
   ```

4. **Pressione Play** e verifique se as linhas estão nítidas e brancas
   - Se ainda tiver "grain/textura", desligue `grainStrength = 0`

---

### ETAPA 2: Verificar `initialSortMode` no GameBootstrap

1. **Na Hierarchy, encontre o GameObject com GameBootstrap**
   - Provavelmente chamado `GameManager` ou `Bootstrap`

2. **No Inspector, procure a seção de Sorting:**
   ```
   [Header: Sort Configuration]
   initialSortMode = None  ← DEVE ESTAR EM "None"
   ```

3. **Se estiver em `ByRank` ou `BySuit`**, mude para `None`

4. **Salve a scene** (Ctrl+S)

---

### ETAPA 3: Limpar HUDs Duplicados/Antigos

1. **Na Hierarchy, procure por Canvas duplicados:**
   - Você pode ter vários Canvas (Canvas, Canvas (1), Canvas (2), etc.)
   - Ou múltiplos GameObjects de UI sobrepostos

2. **Estratégia:**
   - **OPÇÃO A (Recomendado):** Desative os Canvas antigos primeiro (checkbox no Inspector)
     - Dê Play e veja se o jogo ainda funciona
     - Se funcionar, DELETE os Canvas antigos
   
   - **OPÇÃO B:** Se não souber qual é o bom, renomeie os Canvas:
     - Canvas → Canvas_OLD_1
     - Canvas (1) → Canvas_OLD_2
     - Depois veja qual é usado no Play

3. **Remova elementos "misteriosos" no canto inferior esquerdo:**
   - Blocos azuis/cinza, painéis de debug, etc.
   - Se não souber o que são, desative primeiro e teste

4. **Deixe SOMENTE UM Canvas principal**
   - Pode ser o Canvas que já existia ou um novo que você vai criar

---

### ETAPA 4: Setup do PifHUD (Ferramenta Automática ou Manual)

#### **OPÇÃO A: Setup Automático com PifHUDSetupTool**

1. **Menu Unity:** `Tools > Pif > Setup Minimal HUD`
   - Isso criará toda hierarquia automaticamente
   - **ATENÇÃO:** Certifique-se de estar na scene `Game.unity` (não no backup)

2. **O que o tool cria:**
   - TopBar (sala, vez, config, sair)
   - PlayerCards (Norte, Oeste, Leste, Você)
   - MeldBoard com 4 lanes
   - RoundSummaryModal (disabled por padrão)

3. **Após executar o tool:**
   - Verifique se o Canvas "PifHUD_Minimal" foi criado
   - Veja se os PlayerCards estão posicionados corretamente
   - Teste no Play Mode

#### **OPÇÃO B: Setup Manual** (se o tool não funcionar)

Siga o documento `Docs/PifHUD-MinimalRefactor-Guide.md`

---

### ETAPA 5: Conectar PifHUD ao PifeGameManager

1. **Encontre o GameObject com `PifeGameManager`**
   - Provavelmente chamado `GameManager` ou `PifeManager`

2. **Adicione o componente `PifHUDIntegration`:**
   - Select o GameObject
   - Inspector: Add Component → `PifHUDIntegration`

3. **Configure as referências no Inspector:**
   ```
   [References]
   pifHUD = (arraste o GameObject com PifHUD)
   gameManager = (arraste o GameObject com PifeGameManager)
   bootstrap = (arraste o GameObject com GameBootstrap)
   ```

4. **Salve a scene** (Ctrl+S)

---

### ETAPA 6: Configurar PlayerCards

1. **No Canvas do PifHUD, encontre os 4 PlayerCards:**
   - PlayerCard_North
   - PlayerCard_West
   - PlayerCard_East
   - PlayerCard_Local (Você)

2. **Para PlayerCard_Local (apenas este):**
   - No Inspector, verifique se `sortWidgetRoot` está ativo
   - Conecte os botões:
     - `sortBySuitButton` → Botão "♣ Naipe"
     - `sortByRankButton` → Botão "123 Valor"

3. **Para os outros PlayerCards (Norte/Oeste/Leste):**
   - `sortWidgetRoot` deve estar DESATIVADO
   - Eles não têm controles de ordenação

---

### ETAPA 7: Configurar MeldBoard (Áreas de Trincas)

1. **No Canvas, encontre o `MeldBoard`**

2. **Verifique se há 4 MeldLanes:**
   - laneNorth (área horizontal acima do centro)
   - laneWest (área vertical à esquerda)
   - laneEast (área vertical à direita)
   - laneLocal (área horizontal abaixo do centro, acima da mão)

3. **Cada MeldLane deve ter:**
   - backgroundLine (Image, alpha 0.05 - quase invisível)
   - contentRoot (onde as cartas aparecem)
   - meldCardPrefab (prefab de carta menor - 0.75x)

4. **Posicionamento sugerido (viewport coordinates):**
   ```
   laneNorth:  anchorMin (0.25, 0.65)  anchorMax (0.75, 0.75)
   laneWest:   anchorMin (0.05, 0.35)  anchorMax (0.20, 0.65)
   laneEast:   anchorMin (0.80, 0.35)  anchorMax (0.95, 0.65)
   laneLocal:  anchorMin (0.25, 0.25)  anchorMax (0.75, 0.35)
   ```

---

## 🧪 CHECKLIST DE VALIDAÇÃO

Antes de dar commit, verifique:

### Linhas Brancas Nítidas
- [ ] Linhas estão brancas (não mais com textura chalk pesada)
- [ ] Linhas têm cantos arredondados suaves
- [ ] Não há "serrilhado" (jagged edges)

### Botões de Ordenar
- [ ] Ao dar Play, NENHUM botão está "locked" (ambos habilitados)
- [ ] Mão do jogador começa em ordem randômica (não ordenada)
- [ ] Ao clicar em "Naipe" ou "Valor", a mão se reorganiza
- [ ] O botão clicado fica desabilitado (interactable=false, alpha menor)
- [ ] Ao clicar no outro modo, o anterior volta a ser habilitado

### HUD Clean
- [ ] Apenas 1 Canvas ativo na scene
- [ ] TopBar discreto no topo (não ocupa muito espaço)
- [ ] 4 PlayerCards visíveis (Norte, Oeste, Leste, Você)
- [ ] "Você" (PlayerCard_Local) é maior e tem SortWidget
- [ ] NÃO tem painel central gigante translúcido
- [ ] NÃO tem caixas/zonas poluidoras de tela
- [ ] NÃO tem elementos "misteriosos" no canto inferior esquerdo

### Áreas de Melds
- [ ] 4 áreas de melds visíveis (Norte, Oeste, Leste, Local)
- [ ] Quando vazias, são quase invisíveis (background alpha ~0.05)
- [ ] Layout claro: Norte e Local horizontais, Oeste e Leste verticais
- [ ] Centro da mesa tem espaço para Monte, Lixo, Vira (não bloqueado por melds)

### Integração
- [ ] PifHUDIntegration está no GameObject do PifeGameManager
- [ ] Referências conectadas (pifHUD, gameManager, bootstrap)
- [ ] Ao dar Play, o TopBar mostra "Sala PIF - Individual"
- [ ] Ao dar Play, o TopBar mostra "Vez: Você" (ou nome do primeiro jogador)

---

## 🔧 TROUBLESHOOTING

### "Linhas ainda serrilhadas"
1. Verifique Quality Settings: `Edit > Project Settings > Quality`
   - Anti Aliasing: 4x MSAA ou 8x MSAA (se disponível)
2. Certifique-se que `cornerVertices` e `capVertices` estão > 0 no ChalkLine
3. Se estiver usando URP, verifique se MSAA está ativo no Pipeline Asset

### "Botões de ordenar começam travados"
1. Verifique `initialSortMode` no GameBootstrap Inspector (deve ser `None`)
2. No código, confirme que `ResolveInitialSortMode()` NÃO chama `ApplySortMode()` quando mode é None
3. Se persistir, delete o GameObject dos botões antigos e use o SortWidget do PlayerCard

### "HUD ainda duplicado/confuso"
1. Procure por múltiplos Canvas na Hierarchy
2. Desative todos menos 1, veja qual funciona
3. Delete os inativos
4. Se necessário, execute `Tools > Pif > Setup Minimal HUD` novamente (APÓS limpar)

### "MeldBoard não aparece"
1. Verifique se as MeldLanes têm RectTransform configurado
2. Verifique se o Canvas Scaler está em "Scale With Screen Size" (1920x1080 reference)
3. Teste chamando `meldBoard.ShowMeldGroup(0, testCards)` manualmente

### "PlayerCard não ordena cartas ao clicar"
1. Verifique se GameBootstrap está na scene
2. Verifique se os botões têm evento conectado (OnClick → OnSortModeSelected)
3. Verifique console: deve aparecer `[PlayerCard] Sort mode changed to: ...`

---

## 📸 CAPTURA DE TELA

Após finalizar:
1. Entre no Play Mode
2. Capture o Game View: `Ctrl + Shift + PrtScn` (Unity screenshot)
3. Ou use menu: `Tools > Screenshot > Capture Game View`
4. Anexe ao commit/PR para validar visualmente

---

## 💾 COMMIT DAS MUDANÇAS

Quando tudo estiver validado:

```bash
git status
git add .
git commit -m "feat(hud): Refatoração completa do HUD PIF

- Linhas brancas nítidas com cantos arredondados (ChalkLine)
- Bug de botão de ordenar corrigido (começa em mode None)
- PifHUD minimalista com 4 PlayerCards
- MeldBoard com 4 lanes para trincas
- PifHUDIntegration conectando PifeGameManager ↔ HUD
- HUDs duplicados removidos
- Layout clean: mesa verde visível, sem poluição visual"

git push origin hud-pif-refactor
```

Depois abra um Pull Request para merge na branch principal.

---

## 📚 DOCUMENTAÇÃO ADICIONAL

- [PifHUD-MinimalRefactor-Guide.md](PifHUD-MinimalRefactor-Guide.md) - Detalhes de implementação
- [ChalkSystem-Guide.md](ChalkSystem-Guide.md) - Sistema de linhas da mesa
- [CardSystem-CompleteGuide.md](CardSystem-CompleteGuide.md) - Sistema de cartas
- [PifeAI-Guide.md](../Scripts/PifeAI-Guide.md) - Sistema de IA do Pife

---

**Desenvolvido para: Pife Individual (4 jogadores, 1 humano + 3 IAs)**
**UI/UX: Minimalista, mesa verde visível, HUD discreto e funcional**
**Data: 2026-02-10**
