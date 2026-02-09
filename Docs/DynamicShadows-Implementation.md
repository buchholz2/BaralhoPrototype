# Sistema de Sombras Dinâmicas - Resumo das Implementações

## 📦 Arquivos Criados

### 1. **TableLightingManager.cs**
Gerenciador central do sistema de iluminação e sombras.

**Funcionalidades:**
- Cria e gerencia uma luz direcional para iluminar a mesa
- Configura automaticamente as propriedades de sombra do Unity
- Cria um plano de mesa para receber as sombras projetadas
- Controla a iluminação ambiente da cena

**Localização:** `Assets/Scripts/World/TableLightingManager.cs`

---

### 2. **CardWithShadows.shader**
Shader personalizado para renderização das cartas com suporte a sombras.

**Funcionalidades:**
- Surface Shader com suporte a transparência (alpha cutout)
- Pass especial para projeção de sombras (ShadowCaster)
- Renderização otimizada para cartas com texturas
- Suporte completo ao sistema de iluminação do Unity

**Localização:** `Assets/Shaders/CardWithShadows.shader`

---

### 3. **DynamicCardShadowsHelper.cs**
Script auxiliar para configuração rápida do sistema.

**Funcionalidades:**
- Setup automático com um clique
- Encontra e configura todas as cartas na cena
- Interface amigável no Inspector
- Validações e mensagens de ajuda

**Localização:** `Assets/Scripts/World/DynamicCardShadowsHelper.cs`

---

### 4. **DynamicShadows-Guide.md**
Documentação completa do sistema.

**Conteúdo:**
- Guia de setup passo a passo
- Explicação detalhada de todas as configurações
- Troubleshooting
- Dicas de otimização
- Exemplos de configurações

**Localização:** `Docs/DynamicShadows-Guide.md`

---

## 🔧 Modificações em Arquivos Existentes

### CardWorldView.cs

**Novas Configurações Adicionadas:**
```csharp
[Header("Physical Lighting - Dynamic Shadows")]
[SerializeField] private bool useDynamicShadows = true;
[SerializeField] private float cardThickness = 0.02f;
[SerializeField] private ShadowCastingMode shadowCastingMode = ShadowCastingMode.On;
```

**Novos Métodos:**
- `InitializeDynamicShadows()` - Inicializa o sistema de sombras dinâmicas
- `Build3DCardMesh()` - Cria mesh 3D com espessura para sombras realistas

**Modificações:**
- `Awake()` - Agora chama InitializeDynamicShadows() se ativado
- `ConfigurePhysicalRendering()` - Suporte melhorado para sombras dinâmicas
- `UpdatePhysicalSprite()` - Usa mesh 3D quando sombras dinâmicas estão ativas

---

## 🎯 Como o Sistema Funciona

### Fluxo de Renderização

1. **Inicialização**
   ```
   CardWorldView.Awake()
   └─> InitializeDynamicShadows()
       ├─> Procura TableLightingManager
       ├─> Cria material com shader CardWithShadows
       ├─> Ativa renderização física
       └─> Desativa sombra sprite antiga
   ```

2. **Renderização de Frame**
   ```
   Unity Render Pipeline
   └─> Renderiza cartas com shader CardWithShadows
       ├─> Surface pass (carta visível)
       └─> ShadowCaster pass (sombra projetada)
           └─> Projeta na mesa (TablePlane)
   ```

3. **Projeção de Sombra**
   ```
   Luz Direcional
   └─> Direção configurada em TableLightingManager
       └─> Unity calcula projeção automática
           └─> Sombra aparece no TablePlane
   ```

### Diferenças Técnicas

#### Sistema Antigo (Sombras Sprite)
```
Carta (Sprite) → Cria GameObject "Shadow"
                → Copia sprite original
                → Aplica cor escura
                → Posiciona com offset fixo
                → Escala e rotaciona manualmente
```

#### Sistema Novo (Sombras Dinâmicas)
```
Carta (Mesh 3D) → Unity Lighting System
                → Shader projeta sombra
                → Mesa recebe projeção
                → Atualização automática pelo Unity
                → Segue posição/rotação/altura da carta
```

---

## 📊 Vantagens do Novo Sistema

### ✅ Realismo
- Sombras projetadas fisicamente corretas
- Direção baseada em fonte de luz real
- Escala e intensidade baseadas em distância
- Responde a altura da carta automaticamente

### ✅ Performance
- Unity otimiza projeção de sombras internamente
- Menos cálculos manuais por frame
- Cache de sombras quando possível
- Batching automático de shadow casters

### ✅ Flexibilidade
- Ajuste de luz em tempo real
- Configuração centralizada
- Fácil de ajustar para diferentes estilos visuais
- Compatível com iluminação global

### ✅ Manutenibilidade
- Código mais limpo e organizado
- Sistema modular e desacoplado
- Usa features nativas do Unity
- Bem documentado

---

## 🚀 Como Usar (Quick Start)

### Para Usuários
1. Adicione `DynamicCardShadowsHelper` a um GameObject vazio
2. Clique em "Configurar Sombras Dinâmicas Agora"
3. Pronto! As cartas agora usam sombras dinâmicas

### Para Desenvolvedores
```csharp
// Em qualquer script que cria cartas:
var cardView = cardObject.GetComponent<CardWorldView>();

// As sombras dinâmicas são ativadas automaticamente se:
// useDynamicShadows = true (padrão)

// Para configurar manualmente:
Material cardMaterial = new Material(Shader.Find("Card/CardWithShadows"));
cardView.ConfigurePhysicalRendering(true, cardMaterial);
```

---

## ⚙️ Configurações Recomendadas

### Para Visual Realista
```
TableLightingManager:
  lightDirection = (0.3, -1, 0.2)
  lightIntensity = 1.2
  shadowStrength = 0.8
  shadowResolution = Medium/High
  tableColor = Verde escuro mesa (RGB: 26, 38, 31)

CardWorldView:
  useDynamicShadows = true
  cardThickness = 0.02
  shadowCastingMode = On
```

### Para Melhor Performance
```
TableLightingManager:
  shadowResolution = Low/Medium
  shadowBias = 0.1
  
CardWorldView:
  cardThickness = 0.01  // menos espessura = menos cálculos
```

### Para Visual Estilizado
```
TableLightingManager:
  shadowStrength = 0.6  // sombras mais suaves
  ambientIntensity = 0.8  // mais luz ambiente
  lightDirection = (0, -1, 0)  // luz direta de cima
```

---

## 🔍 Detalhes de Implementação

### Mesh 3D da Carta
O método `Build3DCardMesh()` cria um cubo achatado:
- 8 vértices (frente + trás)
- 12 triângulos (6 faces)
- Espessura configurável
- UVs mapeados para textura da carta

Isso permite que a carta projete uma sombra realista com volume real.

### Shader de Sombras
O `CardWithShadows.shader` tem dois SubShaders:
1. **Surface Shader** - Renderiza a carta visível
2. **ShadowCaster Pass** - Projeta a sombra

O alpha cutout garante que partes transparentes não projetem sombra.

### Sistema de Iluminação
O `TableLightingManager` usa:
- `LightType.Directional` - Para simular luz de teto
- `LightShadows.Soft` - Para sombras suaves
- Material Standard no plano da mesa - Para receber sombras corretamente

---

## 📝 Notas Importantes

1. **Compatibilidade**: O sistema antigo de sombras sprite ainda funciona se `useDynamicShadows = false`

2. **Performance**: Sombras dinâmicas são mais pesadas. Para mobile, considere:
   - Shadow Resolution: Low
   - Limitar número de cartas simultâneas
   - Usar shadowCastingMode = ShadowsOnly em cartas não visíveis

3. **Renderização**: O sistema usa o pipeline de renderização padrão do Unity. Para URP/HDRP, ajustes podem ser necessários.

4. **Cache**: Os meshes das cartas são cacheados (`s_spriteMeshCache`) para evitar recriação.

---

## 🎓 Para Aprender Mais

- **Unity Manual**: Shadow Casting
- **Unity Shader Reference**: Surface Shaders
- **Unity Lighting**: Directional Lights
- Ver `DynamicShadows-Guide.md` para guia completo do usuário

---

**Status:** ✅ Implementado e Testado  
**Versão:** 1.0  
**Data:** Fevereiro 2026
