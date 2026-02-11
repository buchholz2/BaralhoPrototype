# 🔧 CORREÇÕES ESPECÍFICAS - RESPOSTA AOS PROBLEMAS DO HUD

**Data:** 2026-02-10  
**Branch:** hud-pif-refactor  
**Scene Principal:** PifTable.unity

---

## 📋 PROBLEMAS IDENTIFICADOS PELO USUÁRIO

1. ❌ **Painel central gigante translúcido** (retângulo grande no meio)
2. ❌ **Áreas de meld virando faixas/caixas grandes** com preenchimento
3. ❌ **PlayerCards não parecem cards de verdade** (só texto + quadradinho)
4. ❌ **SortWidget iniciando travado/selecionado** (deve começar em "Manual")
5. ❌ **TopBar não mostra "Sala PIF - Individual"** (só "Sala PIF")

---

## ✅ FERRAMENTAS CRIADAS PARA CORRIGIR

### 🔧 Tool 1: PifHUDDiagnostic (PRIMEIRO PASSO)
**Menu:** `Tools > Pif > Diagnostic - Show HUD Hierarchy`

**O que faz:**
- Mostra TODA hierarquia do Canvas ativo
- Identifica qual objeto é o "painel central gigante"
- Lista todos os Canvas (detecta duplicados)
- Verifica configurações do ChalkTableDemarcation
- Analisa MeldBoard e PlayerCards

**VOCÊ DEVE EXECUTAR ISSO PRIMEIRO!**

### 🧹 Tool 2: PifHUDCleanup (CORREÇÃO AUTOMÁTICA)
**Menu:** `Tools > Pif > Cleanup and Fix HUD Issues`

**O que faz:**
- Remove/torna invisível o painel central gigante (alpha = 0.01)
- Corrige áreas de meld (alpha = 0.02, sem preenchimento visível)
- Lista Canvas duplicados
- Verifica ChalkTableDemarcation
- Corrige TopBar text para "Sala PIF - Individual"

**EXECUTE DEPOIS DO DIAGNOSTIC!**

---

## 🎯 PASSO A PASSO (ORDEM OBRIGATÓRIA)

### PASSO 1: Diagnóstico
1. Abra Unity Editor
2. Abra a scene **PifTable.unity** (Assets/Scenes/)
3. Menu: `Tools > Pif > Diagnostic - Show HUD Hierarchy`
4. No Console, veja a saída completa
5. **SCREENSHOT DO CONSOLE** (manda pra mim)
6. Procure por linhas com:
   - `⚠️ PAINEL CENTRAL GRANDE: [nome do objeto]`
   - `⚠️ PROBLEMA: Múltiplos Canvas ativos!`

### PASSO 2: Correção Automática
1. Menu: `Tools > Pif > Cleanup and Fix HUD Issues`
2. Clique no botão **"✅ APLICAR TODAS AS CORREÇÕES"**
3. Veja o Console - deve mostrar quantos painéis foram corrigidos
4. Salve a scene (Ctrl+S)

### PASSO 3: Correção Manual (se necessário)
Se o cleanup automático não funcionar 100%, faça manualmente:

#### A) Remover Painel Central Gigante
1. No Console do Diagnostic, anote o **path** do painel (ex: `Canvas/CenterPanel`)
2. Na Hierarchy, navegue até esse objeto
3. No Inspector:
   - Se tem componente `Image`:
     - Mude `Color > Alpha` para **0.01** (quase invisível)
     - Desmarque `Raycast Target`
   - OU: Desative o GameObject inteiro (checkbox)

#### B) Corrigir Áreas de Meld
1. Encontre o GameObject `MeldBoard` na Hierarchy
2. Expanda: verá 4 lanes (North, West, East, Local)
3. Para CADA lane:
   - Se tiver componente `Image`:
     - `Color > Alpha` = **0.02**
     - `Raycast Target` = FALSE
   - Se tiver Panel filho → desativar ou alpha = 0.01
   - Se tiver `Outline` → `Effect Color > Alpha` = **0.08**

#### C) Verificar ChalkTableDemarcation
1. Encontre GameObject `ChalkTableDemarcation` (provavelmente na raiz ou em TableRoot)
2. No Inspector:
   ```
   [Simple White Lines]
   ✓ useSimpleWhiteLines = TRUE
   simpleLineColor = White
   simpleLineOpacity = 0.32

   [Rounded Corners]
   ✓ useRoundedCorners = TRUE
   cornerRadius = 0.5
   cornerSegments = 12
   ```

#### D) Corrigir initialSortMode no GameBootstrap
1. Encontre GameObject com componente `GameBootstrap`
2. No Inspector, procure:
   ```
   [Sort Configuration]
   initialSortMode = None  ← DEVE estar em "None"
   ```
3. Se estiver em `ByRank` ou `BySuit`, mude para `None`

#### E) Verificar TopBar Text
1. Encontre `TopBar/RoomNameText` na Hierarchy
2. No componente `TMP_Text`:
   - Text = **"Sala PIF - Individual"**

### PASSO 4: Testar no Play Mode
1. Salve a scene (Ctrl+S)
2. Pressione **Play**
3. Verifique:
   - [ ] Não tem painel central gigante bloqueando a mesa
   - [ ] Mesa verde visível (feltro de poker)
   - [ ] Áreas de meld quase invisíveis (só contorno fino)
   - [ ] TopBar mostra "Sala PIF - Individual" e "Vez: Você"
   - [ ] Botões de ordenar NÃO estão travados (ambos clicáveis)

### PASSO 5: Screenshot ANTES/DEPOIS
1. **ANTES**: Tire screenshot do GameView com os problemas
2. **DEPOIS**: Aplique correções e tire outro screenshot
3. Manda os 2 pra eu comparar

---

## 🎨 SOBRE OS PLAYERCARDS (Visual Glass)

O PifHUDSetupTool atual cria PlayerCards simples. Para melhorar:

### Opção A: Melhorar Manualmente no Editor
1. Selecione cada PlayerCard na Hierarchy
2. No Background (Image):
   - `Color` = Preto com alpha 0.6-0.7 (glass escuro sutil)
   - `Material` = UI/Default ou um material glass se tiver
3. Avatar:
   - Adicione Mask component (Circle)
   - Isso deixa avatar circular

### Opção B: Criar Prefab PlayerCard Customizado
1. Crie um PlayerCard manualmente com o visual que você quer
2. Salve como Prefab em `Assets/Prefabs/UI/PlayerCard.prefab`
3. Use esse prefab para instanciar os 4 PlayerCards

---

## 📊 CHECKLIST FINAL DE VALIDAÇÃO

Após aplicar todas correções, verifique:

### HUD Limpo
- [ ] Mesa verde visível (nenhum painel gigante bloqueando)
- [ ] Apenas 1 Canvas ativo na scene
- [ ] TopBar discreto (72px alto, fundo alpha ~0.15)
- [ ] TopBar mostra "Sala PIF - Individual" e "Vez: Você"

### Áreas de Meld
- [ ] 4 áreas quase invisíveis (Norte, Oeste, Leste, Local)
- [ ] Quando vazias: background alpha <= 0.03
- [ ] Sem preenchimento (só contorno fino ou nada)
- [ ] Layout: Norte/Local horizontais, Oeste/Leste verticais

### Linhas da Mesa
- [ ] Linhas brancas nítidas
- [ ] Cantos arredondados (não serrilhados)
- [ ] useSimpleWhiteLines = TRUE
- [ ] simpleLineOpacity >= 0.32

### Botões de Ordenar
- [ ] NO PLAY: nenhum botão travado (estado inicial)
- [ ] Ambos clicáveis ao iniciar
- [ ] Ao clicar: o botão fica disabled + opacidade menor
- [ ] O outro continua clicável

### PlayerCards
- [ ] 4 PlayerCards visíveis (Norte, Oeste, Leste, Você)
- [ ] "Você" (Local) tem SortWidget embaixo
- [ ] Outros 3 NÃO têm SortWidget
- [ ] Nome, pontos, contagem de cartas visíveis

---

## 📝 OUTPUTS NECESSÁRIOS (MANDA PRA MIM)

1. **Output do Diagnostic:**
   - Screenshot ou copiar texto completo do Console
   - Preciso ver a hierarquia e os problemas detectados

2. **Screenshot ANTES:**
   - GameView com o painel central gigante
   - Mostrar áreas de meld com preenchimento feio

3. **Screenshot DEPOIS:**
   - GameView limpo, mesa verde visível
   - Áreas de meld quase invisíveis
   - TopBar mostrando "Sala PIF - Individual"

4. **Confirmar Scene:**
   - Qual scene está aberta? (deve ser PifTable.unity)
   - Build Settings: qual scene é a primeira?

---

## 🐛 SE NÃO FUNCIONAR

### Se o Diagnostic não detectar o painel gigante:
- Tire screenshot da Hierarchy completa (expanda o Canvas)
- Tire screenshot do Inspector do objeto suspeito
- Manda pra eu ver exatamente o que é

### Se o Cleanup não funcionar:
- Veja o Console: ele mostra o path do objeto problemático
- Navegue até esse objeto na Hierarchy manualmente
- Desative ou mude alpha = 0.01

### Se ainda tiver múltiplos Canvas:
- Desative TODOS menos 1 (checkbox na Hierarchy)
- Teste no Play com cada um ativo
- Veja qual funciona, delete os outros

---

## 💾 COMMIT APÓS CORREÇÕES

Quando estiver funcionando:

```bash
git add Assets/Scenes/PifTable.unity
git commit -m "fix(hud): Painel central removido + meld areas corrigidas

- Painel central gigante tornado invisível (alpha 0.01)
- Áreas de meld sem preenchimento (alpha 0.02)
- TopBar mostra 'Sala PIF - Individual'
- initialSortMode = None (botões começam desbloqueados)
- ChalkTableDemarcation configurado (linhas brancas nítidas)"

git push origin hud-pif-refactor
```

---

**IMPORTANTE:** Execute o **Diagnostic PRIMEIRO** e manda o output completo pra mim. Preciso ver exatamente qual objeto está causando o problema do painel central.

**Scene correta:** PifTable.unity (NÃO Game.unity)  
**Branch:** hud-pif-refactor  
**Tools disponíveis:** Menu Tools > Pif >
