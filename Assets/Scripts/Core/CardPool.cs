using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct PoolStats
{
    public int worldTotal;
    public int worldActive;
    public int worldPooled;
    public int uiTotal;
    public int uiActive;
    public int uiPooled;
}

/// <summary>
/// Sistema de object pooling para cartas, reduz alocações e melhora performance
/// </summary>
public class CardPool : MonoBehaviour
{
    [Header("Pool Settings")]
    [SerializeField] private CardWorldView worldCardPrefab;
    [SerializeField] private CardView uiCardPrefab;
    [SerializeField] private int initialPoolSize = 20;
    [SerializeField] private int maxPoolSize = 52;
    [SerializeField] private bool expandPool = true;

    private readonly Queue<CardWorldView> _worldCardPool = new Queue<CardWorldView>();
    private readonly Queue<CardView> _uiCardPool = new Queue<CardView>();
    private readonly HashSet<CardWorldView> _activeWorldCards = new HashSet<CardWorldView>();
    private readonly HashSet<CardView> _activeUiCards = new HashSet<CardView>();

    private Transform _worldPoolRoot;
    private Transform _uiPoolRoot;

    public int WorldPoolCount => _worldCardPool.Count;
    public int UiPoolCount => _uiCardPool.Count;
    public int ActiveWorldCards => _activeWorldCards.Count;
    public int ActiveUiCards => _activeUiCards.Count;

    private void Awake()
    {
        CreatePoolRoots();
        PrewarmPool();
    }

    /// <summary>
    /// Cria os containers para objetos poolados
    /// </summary>
    private void CreatePoolRoots()
    {
        var go = new GameObject("WorldCardPool");
        go.transform.SetParent(transform);
        _worldPoolRoot = go.transform;

        go = new GameObject("UiCardPool");
        go.transform.SetParent(transform);
        _uiPoolRoot = go.transform;
    }

    /// <summary>
    /// Pre-aquece o pool criando objetos iniciais
    /// </summary>
    private void PrewarmPool()
    {
        if (worldCardPrefab != null)
        {
            for (int i = 0; i < initialPoolSize; i++)
                CreateWorldCard();
        }

        if (uiCardPrefab != null)
        {
            for (int i = 0; i < initialPoolSize; i++)
                CreateUiCard();
        }

        Debug.Log($"CardPool: Pool inicializado com {initialPoolSize} cartas de cada tipo.");
    }

    /// <summary>
    /// Obtém uma carta 3D do pool
    /// </summary>
    public CardWorldView GetWorldCard()
    {
        CardWorldView card;

        if (_worldCardPool.Count > 0)
        {
            card = _worldCardPool.Dequeue();
        }
        else
        {
            if (!expandPool && _activeWorldCards.Count >= maxPoolSize)
            {
                Debug.LogWarning($"CardPool: Limite de cartas atingido ({maxPoolSize}).");
                return null;
            }
            card = CreateWorldCard(false);
        }

        card.gameObject.SetActive(true);
        _activeWorldCards.Add(card);
        return card;
    }

    /// <summary>
    /// Obtém uma carta UI do pool
    /// </summary>
    public CardView GetUiCard()
    {
        CardView card;

        if (_uiCardPool.Count > 0)
        {
            card = _uiCardPool.Dequeue();
        }
        else
        {
            if (!expandPool && _activeUiCards.Count >= maxPoolSize)
            {
                Debug.LogWarning($"CardPool: Limite de cartas UI atingido ({maxPoolSize}).");
                return null;
            }
            card = CreateUiCard(false);
        }

        card.gameObject.SetActive(true);
        _activeUiCards.Add(card);
        return card;
    }

    /// <summary>
    /// Retorna uma carta 3D ao pool
    /// </summary>
    public void ReturnWorldCard(CardWorldView card)
    {
        if (card == null) return;

        if (!_activeWorldCards.Remove(card))
        {
            Debug.LogWarning($"CardPool: Tentativa de retornar carta que não estava ativa.");
            return;
        }

        card.gameObject.SetActive(false);
        card.transform.SetParent(_worldPoolRoot);
        card.transform.localPosition = Vector3.zero;
        card.transform.localRotation = Quaternion.identity;
        card.transform.localScale = Vector3.one;

        _worldCardPool.Enqueue(card);
    }

    /// <summary>
    /// Retorna uma carta UI ao pool
    /// </summary>
    public void ReturnUiCard(CardView card)
    {
        if (card == null) return;

        if (!_activeUiCards.Remove(card))
        {
            Debug.LogWarning($"CardPool: Tentativa de retornar carta UI que não estava ativa.");
            return;
        }

        card.gameObject.SetActive(false);
        card.transform.SetParent(_uiPoolRoot);
        
        var rt = card.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = Vector2.zero;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
        }

        _uiCardPool.Enqueue(card);
    }

    /// <summary>
    /// Retorna todas as cartas ativas ao pool
    /// </summary>
    public void ReturnAllCards()
    {
        var worldCards = new List<CardWorldView>(_activeWorldCards);
        foreach (var card in worldCards)
            ReturnWorldCard(card);

        var uiCards = new List<CardView>(_activeUiCards);
        foreach (var card in uiCards)
            ReturnUiCard(card);

        Debug.Log($"CardPool: Todas as cartas retornadas ao pool.");
    }

    /// <summary>
    /// Cria uma nova carta 3D
    /// </summary>
    private CardWorldView CreateWorldCard(bool addToPool = true)
    {
        if (worldCardPrefab == null)
        {
            Debug.LogError("CardPool: worldCardPrefab não configurado!");
            return null;
        }

        var card = Instantiate(worldCardPrefab, _worldPoolRoot);
        card.gameObject.SetActive(false);
        card.name = $"WorldCard_{_worldCardPool.Count + _activeWorldCards.Count}";

        if (addToPool)
            _worldCardPool.Enqueue(card);

        return card;
    }

    /// <summary>
    /// Cria uma nova carta UI
    /// </summary>
    private CardView CreateUiCard(bool addToPool = true)
    {
        if (uiCardPrefab == null)
        {
            Debug.LogError("CardPool: uiCardPrefab não configurado!");
            return null;
        }

        var card = Instantiate(uiCardPrefab, _uiPoolRoot);
        card.gameObject.SetActive(false);
        card.name = $"UiCard_{_uiCardPool.Count + _activeUiCards.Count}";

        if (addToPool)
            _uiCardPool.Enqueue(card);

        return card;
    }

    /// <summary>
    /// Limpa completamente o pool
    /// </summary>
    public void ClearPool()
    {
        while (_worldCardPool.Count > 0)
        {
            var card = _worldCardPool.Dequeue();
            if (card != null)
                Destroy(card.gameObject);
        }

        while (_uiCardPool.Count > 0)
        {
            var card = _uiCardPool.Dequeue();
            if (card != null)
                Destroy(card.gameObject);
        }

        _activeWorldCards.Clear();
        _activeUiCards.Clear();

        Debug.Log("CardPool: Pool limpo completamente.");
    }

    /// <summary>
    /// Obtém estatísticas do pool
    /// </summary>
    public PoolStats GetStats()
    {
        return new PoolStats
        {
            worldTotal = _activeWorldCards.Count + _worldCardPool.Count,
            worldActive = _activeWorldCards.Count,
            worldPooled = _worldCardPool.Count,
            uiTotal = _activeUiCards.Count + _uiCardPool.Count,
            uiActive = _activeUiCards.Count,
            uiPooled = _uiCardPool.Count
        };
    }

    private void OnDestroy()
    {
        ClearPool();
    }
}
