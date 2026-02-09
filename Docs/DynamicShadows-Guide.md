# Sistema de Sombras Dinâmicas - Guia Completo

## 📋 Visão Geral

Este sistema substitui as sombras estáticas de sprite por **sombras dinâmicas realistas** que são projetadas pelas cartas na mesa, simulando uma luz real sobre a mesa de jogo.

## ✨ Características

- 🌟 **Sombras Realistas**: As cartas projetam sombras dinâmicas na mesa
- 💡 **Iluminação Física**: Sistema de luz direcional simulando iluminação de cima da mesa
- 🎯 **Projeção Real**: Sombras seguem a posição e rotação das cartas em tempo real
- 📦 **Cartas 3D**: Cartas com espessura física para sombras mais realistas
- ⚙️ **Totalmente Configurável**: Controle completo sobre luz, sombra e aparência

## 🚀 Setup Rápido

### Método 1: Usando o Helper (Recomendado)

1. **Adicionar o Helper à Cena**
   - Crie um GameObject vazio na cena
   - Adicione o componente `DynamicCardShadowsHelper`
   - No Inspector, clique no botão **"Configurar Sombras Dinâmicas Agora"**

2. **Pronto!** O sistema está configurado automaticamente

### Método 2: Setup Manual

1. **Criar TableLightingManager**
   ```
   GameObject → Create Empty → Renomear para "TableLighting"
   Add Component → TableLightingManager
   ```

2. **Configurar as Cartas**
   - Selecione cada prefab/objeto de carta (CardWorldView)
   - No Inspector, encontre a seção **"Physical Lighting - Dynamic Shadows"**
   - Marque **"Use Dynamic Shadows"** = `true`
   - Marque **"Use Physical Card"** = `true`
   - Configure **"Physical Cast Shadows"** = `true`

3. **Aplicar o Material**
   - Certifique-se que o shader `Card/CardWithShadows` está disponível
   - O sistema criará automaticamente o material necessário

## ⚙️ Configurações Detalhadas

### TableLightingManager

Gerencia toda a iluminação e o plano da mesa.

#### Light Settings
- **Light Direction**: Direção da luz (ex: `0.3, -1, 0.2` para luz diagonal de cima)
- **Light Intensity**: Intensidade da luz (padrão: `1.2`)
- **Light Color**: Cor da luz (padrão: branco)

#### Shadow Settings
- **Enable Shadows**: Ativar/desativar sombras
- **Shadow Resolution**: Qualidade das sombras (Low/Medium/High/Very High)
- **Shadow Strength**: Intensidade da sombra (`0-1`, padrão: `0.8`)
- **Shadow Bias**: Ajuste fino para evitar "shadow acne" (padrão: `0.05`)
- **Shadow Normal Bias**: Deslocamento baseado na normal (padrão: `0.4`)

#### Table Plane
- **Table Size**: Tamanho da mesa que recebe sombras (padrão: `20x15`)
- **Table Plane Y**: Altura da mesa (padrão: `-2.5`)
- **Table Color**: Cor da superfície da mesa

#### Ambient Lighting
- **Control Ambient**: Se deve controlar a luz ambiente da cena
- **Ambient Color**: Cor da luz ambiente
- **Ambient Intensity**: Intensidade da luz ambiente

### CardWorldView - Sombras Dinâmicas

#### Configurações Principais
- **Use Dynamic Shadows**: Ativar sombras dinâmicas (marcar para usar o novo sistema)
- **Use Physical Card**: Usar renderização física com mesh 3D
- **Physical Cast Shadows**: A carta projeta sombras
- **Physical Receive Shadows**: A carta recebe sombras (normalmente deixar desmarcado)

#### Configurações Avançadas
- **Card Thickness**: Espessura da carta em unidades (padrão: `0.02`)
  - Maior = sombra mais pronunciada
- **Shadow Casting Mode**: Modo de projeção de sombra
  - `On`: Carta visível e projeta sombra
  - `Shadows Only`: Apenas sombra visível
  - `Off`: Sem sombras
- **Dynamic Shadow Shader**: Shader usado (padrão: `Card/CardWithShadows`)

#### Opções de Fallback
- **Keep Soft Shadow When Physical**: Manter sombra sprite como backup
- **Physical Fallback Shadow Alpha**: Transparência da sombra sprite de backup

## 🎨 Ajustes para Melhor Aparência

### 1. Ajustar Direção da Luz

Para simular uma luz de teto inclinada:
```
Light Direction: (0.3, -1, 0.2)
```

Para luz mais vertical (teto direto):
```
Light Direction: (0, -1, 0)
```

Para luz lateral dramática:
```
Light Direction: (0.7, -1, 0.3)
```

### 2. Ajustar Intensidade das Sombras

**Sombras mais suaves:**
- Shadow Strength: `0.5 - 0.6`
- Ambient Intensity: `0.7 - 0.8`

**Sombras mais intensas:**
- Shadow Strength: `0.8 - 0.9`
- Ambient Intensity: `0.4 - 0.5`

### 3. Ajustar Qualidade das Sombras

**Performance (melhor FPS):**
- Shadow Resolution: `Low` ou `Medium`
- Shadow Bias: `0.1`

**Qualidade (visual melhor):**
- Shadow Resolution: `High` ou `Very High`
- Shadow Bias: `0.02 - 0.05`

### 4. Ajustar Espessura da Carta

**Cartas mais finas (padrão cartão):**
```
Card Thickness: 0.01 - 0.02
```

**Cartas mais grossas (efeito dramático):**
```
Card Thickness: 0.03 - 0.05
```

## 🔧 Troubleshooting

### Problema: Não vejo sombras

**Soluções:**
1. Verifique se `Use Dynamic Shadows` está marcado no CardWorldView
2. Verifique se `Enable Shadows` está marcado no TableLightingManager
3. Certifique-se que há uma luz na cena (TableLightingManager cria automaticamente)
4. Verifique se o plano da mesa está na posição correta

### Problema: Sombras com artefatos/manchas

**Soluções:**
1. Aumente `Shadow Bias` no TableLightingManager (tente `0.05` a `0.1`)
2. Aumente `Shadow Normal Bias` (tente `0.5` a `0.7`)
3. Ajuste a altura da mesa (`Table Plane Y`) para estar abaixo das cartas

### Problema: Performance baixa

**Soluções:**
1. Reduza `Shadow Resolution` para `Low` ou `Medium`
2. Reduza o número de cartas projetando sombras simultaneamente
3. Ajuste `Shadow Near Plane` para um valor maior

### Problema: Sombras muito claras/escuras

**Soluções:**
1. Ajuste `Shadow Strength` (0-1)
2. Ajuste `Light Intensity`
3. Modifique `Ambient Intensity` para controlar a luz ambiente

## 📊 Comparação: Sombras Antigas vs Novas

| Aspecto | Sombras Sprite (Antigas) | Sombras Dinâmicas (Novas) |
|---------|-------------------------|---------------------------|
| **Realismo** | Sombra fixa, não reage à posição | Sombra dinâmica, segue a carta |
| **Direção** | Fixa no código | Controlada pela luz |
| **Projeção** | Sprite escalado | Projeção física real |
| **Performance** | Mais leve | Um pouco mais pesado |
| **Visual** | Simples, 2D | Realista, 3D |
| **Ajustável** | Limitado | Totalmente configurável |

## 💡 Dicas Profissionais

1. **Iluminação Consistente**: Mantenha a direção da luz consistente com outros elementos visuais do jogo

2. **Teste em Jogo**: As configurações podem parecer diferentes em play mode vs edit mode

3. **Ambient Lighting**: Uma boa luz ambiente (0.5-0.7) ajuda a equilibrar as sombras

4. **Mesa Escura**: Uma mesa mais escura (`Table Color` cinza escuro) faz as sombras ficarem mais visíveis

5. **Build Settings**: Para builds finais, ajuste Project Settings → Quality → Shadows para melhor performance

## 🎮 Exemplo de Configuração Recomendada

**Para um visual profissional e balanceado:**

```
TableLightingManager:
  Light Direction: (0.24, -1, 0.15)
  Light Intensity: 1.2
  Shadow Strength: 0.75
  Shadow Resolution: Medium
  Table Color: RGB(26, 38, 31) - verde escuro mesa
  Ambient Intensity: 0.6

CardWorldView:
  Use Dynamic Shadows: true
  Card Thickness: 0.02
  Shadow Casting Mode: On
```

## 📝 Notas Técnicas

- O sistema usa o shader `Card/CardWithShadows` que suporta alpha cutout para sombras
- As cartas são renderizadas como meshes 3D com espessura configurável
- O plano da mesa é criado automaticamente e recebe as sombras
- O sistema é compatível com o sistema de layout de mão existente

## 🔄 Revertendo para Sistema Antigo

Se precisar voltar ao sistema antigo de sombras:

1. Em cada CardWorldView, desmarque `Use Dynamic Shadows`
2. Isso reativará automaticamente o sistema de sombras sprite
3. Você pode desabilitar ou remover o TableLightingManager

---

**Criado em:** Fevereiro 2026  
**Versão:** 1.0  
**Compatibilidade:** Unity 2021.3+
