# Sistema de Sombras Realistas - Guia de Configuração

## Visão Geral

Implementação de sombras realistas usando meshes 3D e iluminação física do Unity, inspirado em projetos profissionais de cartas 3D.

## Componentes Criados

### 1. **CardWorldView** - Mesh 3D
- Adicionado método `Build3DCardMesh()` que cria cartas com espessura real
- Campo `physicalUse3DMesh` para ativar meshes 3D
- Campo `physical3DThickness` para controlar espessura da carta (padrão: 0.002)
- Mantém compatibilidade total com o sistema de sprites existente

### 2. **TableLighting**
- Gerencia luz direcional automaticamente
- Cria plano da mesa para receber sombras
- Métodos para configurar iluminação em runtime
- Integrado com configurações do GameBootstrap

### 3. **IsometricCameraSetup**
- Configura câmera em perspectiva isométrica
- Ângulo padrão: 35° para visão superior
- Permite ajustes em runtime
- Mantém compatibilidade com controles existentes

## Como Ativar

### Passo 1: Habilitar no GameBootstrap
No Inspector do GameBootstrap, configure:

```
usePhysicalLighting = true
```

### Passo 2: Configurar CardWorldView (Prefab ou Instância)
No Inspector dos CardWorldView, configure:

```
Physical Lighting:
  ├─ Use Physical Card = true
  ├─ Physical Use 3D Mesh = true
  ├─ Physical 3D Thickness = 0.002
  ├─ Physical Cast Shadows = true
  ├─ Physical Receive Shadows = false
  └─ Physical Shadow Only = false (para ver a carta E a sombra)
```

### Passo 3: Ajustar Qualidade de Sombras
Em `Edit > Project Settings > Quality`:
```
Shadows:
  ├─ Shadow Quality = All
  ├─ Shadow Resolution = Medium Resolution
  ├─ Shadow Distance = 50
  └─ Shadow Cascades = No Cascades
```

## Parâmetros de Configuração

### GameBootstrap
- `usePhysicalLighting`: Ativa/desativa sistema completo
- `physicalLightEuler`: Ângulo da luz (padrão: 75°, 15°, 0°)
- `physicalLightIntensity`: Intensidade da luz (padrão: 1.2)
- `physicalLightShadows`: Tipo de sombra (Soft recomendado)
- `physicalShadowStrength`: Força da sombra (0.8)
- `physicalTableTint`: Cor da mesa

### CardWorldView
- `physicalUse3DMesh`: Usar mesh 3D ao invés de sprite flat
- `physical3DThickness`: Espessura da carta em unidades (0.001 - 0.02)
- `physicalCastShadows`: Carta projeta sombra
- `physicalReceiveShadows`: Carta recebe sombras de outras cartas
- `physicalShadowOnly`: Mostra apenas a sombra (útil para debug)

### TableLighting
- `lightEulerAngles`: Direção da luz
- `lightIntensity`: Intensidade
- `shadowType`: Tipo de sombra (Soft/Hard)
- `planeSize`: Tamanho do plano da mesa
- `tableColor`: Cor da superfície da mesa

### IsometricCameraSetup
- `cameraAngleX`: Ângulo vertical (10-60°)
- `cameraAngleY`: Ângulo horizontal (-45 a 45°)
- `cameraDistance`: Distância da câmera
- `fieldOfView`: Campo de visão

## Comparação: Antes vs Depois

### Antes (Sombras Sprite)
- ✓ Leve e rápido
- ✓ Funciona em qualquer plataforma
- ✗ Sombras "fake" (sprite duplicado)
- ✗ Sem perspectiva realista
- ✗ Sombra sempre quadrada

### Depois (Sombras Físicas 3D)
- ✓ Sombras realistas projetadas
- ✓ Perspectiva 3D com profundidade
- ✓ Sombras mudam com ângulo da carta
- ✓ Espessura visual da carta
- ✗ Requer hardware com suporte a sombras
- ✗ Levemente mais pesado

## Troubleshooting

### Sombras não aparecem
1. Verificar `Quality Settings > Shadows = All`
2. Verificar `physicalCastShadows = true` nas cartas
3. Verificar que há um plano (mesa) para receber sombras
4. Aumentar `Shadow Distance` em Quality Settings

### Cartas aparecem muito escuras
- Aumentar `physicalLightIntensity` no GameBootstrap
- Ajustar `physicalShadowStrength` (reduzir para sombras mais claras)

### Câmera está muito distante/perto
- Ajustar `cameraDistance` no IsometricCameraSetup
- Ou ajustar `fieldOfView` para zoom in/out

### Performance está lenta
- Reduzir `Shadow Resolution` em Quality Settings
- Desativar `Physical Receive Shadows` nas cartas
- Usar `Hard Shadows` ao invés de `Soft Shadows`

## Revertendo para Sistema Antigo

Para voltar ao sistema de sombras sprite:

1. No GameBootstrap: `usePhysicalLighting = false`
2. Ou nos CardWorldView: `usePhysicalCard = false`

O sistema antigo permanece intacto e funcional.

## Estrutura Técnica

```
GameBootstrap
    ├─ SetupPhysicalLighting()
    │   ├─ Cria TableLighting
    │   ├─ Configura IsometricCameraSetup
    │   └─ Aplica configuração em todas as cartas
    │
    └─ BindWorldCard()
        └─ ConfigurePhysicalRendering() se usePhysicalLighting

CardWorldView
    ├─ Build3DCardMesh() - Cria mesh com espessura
    ├─ BuildMeshFromSprite() - Usa contorno do sprite
    ├─ BuildRoundedRectMesh() - Quad arredondado
    └─ ConfigurePhysicalRendering() - Ativa/desativa physical

TableLighting
    ├─ SetupDirectionalLight()
    └─ SetupTablePlane()

IsometricCameraSetup
    └─ ApplyCameraConfiguration()
```

## Notas de Desenvolvimento

- Todas as classes existentes foram preservadas
- Sistema é opt-in (precisa ativar explicitamente)
- Compatibilidade total com lógica atual de jogo
- Sombras sprite continuam funcionando normalmente

---

**Autor**: Sistema de Sombras Realistas v1.0  
**Data**: 2026-02-09
