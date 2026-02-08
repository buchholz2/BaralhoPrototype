using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gerencia operações do baralho incluindo embaralhamento, compra e controle de pilhas
/// Agora com suporte a coroutines para animações suaves
/// </summary>
public class DeckManager : MonoBehaviour
{
    [Header("Configuração")]
    [SerializeField] private CardSpriteDatabase spriteDatabase;
    [SerializeField] private int initialDrawLimit = 10;
    [SerializeField] private float drawCooldown = 0.25f;

    private Deck _deck;
    private int _drawCount;
    private float _lastDrawTime = -10f;
    private int _initialDeckSize;

    public int RemainingCards => _deck?.Count ?? 0;
    public int DrawCount => _drawCount;
    public int InitialDeckSize => _initialDeckSize;
    
    public event Action<Card> OnCardDrawn;
    public event Action OnDeckEmpty;
    public event Action<int> OnDeckCountChanged;

    /// <summary>
    /// Inicializa um novo baralho e embaralha
    /// </summary>
    public void Initialize(CardSpriteDatabase database = null)
    {
        var db = database ?? spriteDatabase;
        _deck = new Deck(db);
        _deck.Shuffle();
        _drawCount = 0;
        _lastDrawTime = -10f;
        _initialDeckSize = _deck.Count;
        
        OnDeckCountChanged?.Invoke(_deck.Count);
        Debug.Log($"DeckManager: Baralho inicializado com {_deck.Count} cartas.");
    }

    /// <summary>
    /// Tenta comprar uma carta respeitando cooldown e limites
    /// </summary>
    public bool TryDrawCard(out Card card, bool respectLimit = true)
    {
        card = default;

        // Validações
        if (_deck == null || _deck.Count <= 0)
        {
            OnDeckEmpty?.Invoke();
            return false;
        }

        float now = Time.unscaledTime;
        if (now - _lastDrawTime < drawCooldown)
        {
            Debug.Log($"DeckManager: Aguarde {drawCooldown - (now - _lastDrawTime):F2}s para comprar.");
            return false;
        }

        if (respectLimit && initialDrawLimit > 0 && _drawCount >= initialDrawLimit)
        {
            Debug.Log($"DeckManager: Limite de compras atingido ({initialDrawLimit}).");
            return false;
        }

        // Compra bem-sucedida
        card = _deck.Draw();
        _drawCount++;
        _lastDrawTime = now;
        
        OnCardDrawn?.Invoke(card);
        OnDeckCountChanged?.Invoke(_deck.Count);
        
        return true;
    }

    /// <summary>
    /// Compra múltiplas cartas de uma vez
    /// </summary>
    public List<Card> DrawCards(int count, bool respectLimit = true)
    {
        var cards = new List<Card>();
        
        for (int i = 0; i < count; i++)
        {
            if (TryDrawCard(out var card, respectLimit))
                cards.Add(card);
            else
                break;
        }
        
        return cards;
    }

    /// <summary>
    /// Reseta o contador de compras
    /// </summary>
    public void ResetDrawCount()
    {
        _drawCount = 0;
        Debug.Log("DeckManager: Contador de compras resetado.");
    }

    /// <summary>
    /// Calcula a escala visual da pilha baseada nas cartas restantes
    /// </summary>
    public float GetPileScale(float minScale = 0.45f, float maxScale = 1f)
    {
        if (_deck == null || _initialDeckSize <= 0) return minScale;
        float t = Mathf.Clamp01(RemainingCards / (float)_initialDeckSize);
        return Mathf.Lerp(minScale, maxScale, t);
    }

    /// <summary>
    /// Verifica se pode comprar carta (sem realmente comprar)
    /// </summary>
    public bool CanDraw(bool respectLimit = true)
    {
        if (_deck == null || _deck.Count <= 0) return false;
        
        float now = Time.unscaledTime;
        if (now - _lastDrawTime < drawCooldown) return false;
        
        if (respectLimit && initialDrawLimit > 0 && _drawCount >= initialDrawLimit) return false;
        
        return true;
    }

    // ===== COROUTINE METHODS =====

    /// <summary>
    /// Compra múltiplas cartas com delay entre cada uma (coroutine)
    /// Útil para animações de draw sequencial
    /// </summary>
    /// <param name="count">Número de cartas para comprar</param>
    /// <param name="delayBetweenCards">Delay em segundos entre cada carta</param>
    /// <param name="onCardDrawn">Callback chamado ao comprar cada carta</param>
    /// <param name="respectLimit">Se deve respeitar o limite de compras</param>
    public IEnumerator DrawCardsCoroutine(int count, float delayBetweenCards, Action<Card> onCardDrawn = null, bool respectLimit = true)
    {
        for (int i = 0; i < count; i++)
        {
            if (TryDrawCard(out var card, respectLimit))
            {
                onCardDrawn?.Invoke(card);
                
                if (i < count - 1) // Não aguardar após a última carta
                    yield return new WaitForSeconds(delayBetweenCards);
            }
            else
            {
                Debug.LogWarning($"DeckManager: Não foi possível comprar carta {i + 1}/{count}");
                yield break;
            }
        }
    }

    /// <summary>
    /// Embaralha o deck com efeito visual usando coroutine
    /// </summary>
    /// <param name="shuffleIterations">Número de vezes para embaralhar</param>
    /// <param name="delayBetweenShuffle">Delay entre cada iteração</param>
    /// <param name="onShuffleComplete">Callback ao completar o embaralhamento</param>
    public IEnumerator ShuffleCoroutine(int shuffleIterations = 3, float delayBetweenShuffle = 0.1f, Action onShuffleComplete = null)
    {
        if (_deck == null)
        {
            Debug.LogError("DeckManager: Deck não inicializado!");
            yield break;
        }

        for (int i = 0; i < shuffleIterations; i++)
        {
            _deck.Shuffle();
            Debug.Log($"DeckManager: Embaralhamento {i + 1}/{shuffleIterations}");
            
            if (i < shuffleIterations - 1)
                yield return new WaitForSeconds(delayBetweenShuffle);
        }

        onShuffleComplete?.Invoke();
        Debug.Log("DeckManager: Embaralhamento completo!");
    }
}
