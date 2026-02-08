# Revisao Tecnica - 2026-02-07

## Escopo
Revisao estatica do projeto (sem executar testes/jogo), com foco em:
- consistencia de fluxo de cartas (destaque, compra, descarte)
- organizacao de codigo
- riscos de manutencao

## Ajustes aplicados nesta revisao
1. Estado de destaque fixo agora e limpo quando a mao muda por compra.
- Arquivo: `Assets/Scripts/UI/GameBootstrap.cs`
- Metodo: `DrawFromPile()`

2. Estado de destaque fixo agora e limpo ao recriar a mao inicial world.
- Arquivo: `Assets/Scripts/UI/GameBootstrap.cs`
- Metodo: `SpawnInitialWorldHand()`

3. Estado de destaque fixo agora e limpo em qualquer descarte world.
- Arquivo: `Assets/Scripts/UI/GameBootstrap.cs`
- Metodo: `DiscardWorldCard(...)`

4. Limpeza de fluxo no descarte em `CardWorldView`.
- Removida chamada redundante de `NotifyWorldDragEnd(...)` no caminho de descarte.
- Arquivo: `Assets/Scripts/World/CardWorldView.cs`

5. Regra de descarte por elevacao aplicada no drag world.
- `discardMinLift` agora e respeitado antes de aceitar descarte ao soltar.
- Evita descarte acidental quando a carta cruza a zona de descarte sem subir o suficiente.
- Arquivo: `Assets/Scripts/World/CardWorldView.cs`

6. Higiene de cache no layout world.
- Limpeza dos caches de suavizacao (`_velocities`, `_rotVelocities`) para cartas removidas/destruidas.
- Evita acumulacao silenciosa apos muitos ciclos de compra/descarte.
- Arquivo: `Assets/Scripts/World/HandWorldLayout.cs`

## Achados da revisao (sem alterar comportamento)
1. Classe muito grande:
- `Assets/Scripts/UI/GameBootstrap.cs` (~3000 linhas)
- Recomendacao: quebrar em classes parciais por dominio (`Setup`, `WorldFlow`, `SortUI`, `Lighting`).

2. Classe grande de interacao:
- `Assets/Scripts/World/CardWorldView.cs` (~900+ linhas)
- Recomendacao: extrair blocos (`Shadow`, `PhysicalMesh`, `Input/Drag`) para componentes auxiliares.

3. Uso extensivo de `GameObject.Find(...)` em setup:
- Nao e critico em runtime continuo (maioria roda em setup), mas aumenta fragilidade de nome de objeto.
- Recomendacao: migrar gradualmente para referencias serializadas + validacao no inspector.

4. Muitos defaults forçados em runtime dentro de `Start()`:
- Facilita bootstrap rapido, mas reduz previsibilidade do inspector.
- Recomendacao: mover forcas para um preset explicito (ScriptableObject) e aplicar por opcao.

## Organizacao de pastas (situacao atual)
- Estrutura principal esta coerente: `Core`, `UI`, `World`.
- Ferramentas auxiliares estao concentradas em `Tools/Research`.
- Foi criado `Docs/` para registrar auditorias tecnicas e decisoes.

## Proxima rodada sugerida (quando voce voltar)
1. Testar interacao real do novo fluxo de destaque + duplo clique.
2. Validar visual final de sombra em diferentes resolucoes.
3. Priorizar refatoracao parcial de `GameBootstrap` sem mudar gameplay.
