using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Board central que mostra as trincas/jogos baixados por cada jogador
/// 4 Lanes (Norte, Oeste, Leste, Local) - praticamente invisível quando vazio
/// </summary>
public class MeldBoard : MonoBehaviour
{
    [Header("Lane Roots")]
    [SerializeField] private MeldLane laneNorth;
    [SerializeField] private MeldLane laneWest;
    [SerializeField] private MeldLane laneEast;
    [SerializeField] private MeldLane laneLocal;
    
    [Header("Meld Card Settings")]
    [SerializeField] private float meldCardScale = 0.75f;
    [SerializeField] private float meldCardOverlap = 0.7f; // Sobreposição horizontal (0-1)
    [SerializeField] private float meldGroupSpacing = 8f; // Espaço entre grupos

    public void ShowMeldGroup(int playerIndex, List<Card> cards)
    {
        MeldLane lane = GetLane(playerIndex);
        if (lane != null)
        {
            lane.AddMeldGroup(cards, meldCardScale, meldCardOverlap, meldGroupSpacing);
        }
    }

    public void ClearPlayerMelds(int playerIndex)
    {
        MeldLane lane = GetLane(playerIndex);
        if (lane != null)
        {
            lane.ClearAllGroups();
        }
    }

    public void ClearAllMelds()
    {
        if (laneNorth != null) laneNorth.ClearAllGroups();
        if (laneWest != null) laneWest.ClearAllGroups();
        if (laneEast != null) laneEast.ClearAllGroups();
        if (laneLocal != null) laneLocal.ClearAllGroups();
    }

    private MeldLane GetLane(int playerIndex)
    {
        return playerIndex switch
        {
            0 => laneLocal,
            1 => laneNorth,
            2 => laneWest,
            3 => laneEast,
            _ => null
        };
    }
}

/// <summary>
/// Uma lane (trilha) que contém múltiplos grupos de trincas
/// Começa quase invisível e só aparece quando há trincas
/// </summary>
public class MeldLane : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Image backgroundLine; // Linha guia ultra-sutil (opcional)
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private HorizontalLayoutGroup layoutGroup;
    
    [Header("Card Prefab")]
    [SerializeField] private GameObject meldCardPrefab; // Prefab de carta para exibir na mesa
    
    private readonly List<MeldGroup> _groups = new();

    private void Awake()
    {
        // Começa com background quase invisível
        if (backgroundLine != null)
        {
            Color c = backgroundLine.color;
            c.a = 0.05f; // Ultra-sutil
            backgroundLine.color = c;
        }
        
        if (layoutGroup == null && contentRoot != null)
            layoutGroup = contentRoot.GetComponent<HorizontalLayoutGroup>();
    }

    public void AddMeldGroup(List<Card> cards, float scale, float overlap, float spacing)
    {
        if (cards == null || cards.Count == 0)
            return;
        
        // Cria um novo grupo de cartas
        GameObject groupObj = new GameObject($"MeldGroup_{_groups.Count}");
        groupObj.transform.SetParent(contentRoot, false);
        
        MeldGroup group = groupObj.AddComponent<MeldGroup>();
        group.Initialize(cards, meldCardPrefab, scale, overlap);
        
        _groups.Add(group);
        
        // Torna o background um pouco mais visível quando há grupos
        UpdateBackgroundVisibility();
    }

    public void ClearAllGroups()
    {
        foreach (var group in _groups)
        {
            if (group != null)
                Destroy(group.gameObject);
        }
        _groups.Clear();
        
        UpdateBackgroundVisibility();
    }

    private void UpdateBackgroundVisibility()
    {
        if (backgroundLine != null)
        {
            // Fica mais visível quando há grupos, mas ainda sutil
            Color c = backgroundLine.color;
            c.a = _groups.Count > 0 ? 0.12f : 0.05f;
            backgroundLine.color = c;
        }
    }
}

/// <summary>
/// Um grupo de cartas (trinca ou sequência) dentro de uma lane
/// </summary>
public class MeldGroup : MonoBehaviour
{
    private readonly List<GameObject> _cardObjects = new();

    public void Initialize(List<Card> cards, GameObject cardPrefab, float scale, float overlap)
    {
        if (cardPrefab == null)
        {
            Debug.LogWarning("[MeldGroup] Card prefab is null!");
            return;
        }
        
        // Cria layout horizontal com overlap
        HorizontalLayoutGroup layout = gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childAlignment = TextAnchor.MiddleCenter;
        
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        if (rectTransform == null)
            rectTransform = gameObject.AddComponent<RectTransform>();
        
        // Instancia as cartas
        for (int i = 0; i < cards.Count; i++)
        {
            GameObject cardObj = Instantiate(cardPrefab, transform);
            cardObj.transform.localScale = Vector3.one * scale;
            
            // TODO: Configurar sprite da carta baseado em cards[i]
            // Por enquanto apenas placeholder
            
            _cardObjects.Add(cardObj);
            
            // Aplica overlap (espaçamento negativo entre cartas)
            if (i > 0)
            {
                RectTransform cardRect = cardObj.GetComponent<RectTransform>();
                if (cardRect != null)
                {
                    // Usa LayoutElement para controlar espaçamento
                    LayoutElement layoutElement = cardObj.AddComponent<LayoutElement>();
                    layoutElement.ignoreLayout = false;
                    
                    // Offset negativo para sobrepor
                    float cardWidth = cardRect.rect.width * scale;
                    layout.spacing = -cardWidth * (1f - overlap);
                }
            }
        }
    }

    private void OnDestroy()
    {
        foreach (var cardObj in _cardObjects)
        {
            if (cardObj != null)
                Destroy(cardObj);
        }
        _cardObjects.Clear();
    }
}
