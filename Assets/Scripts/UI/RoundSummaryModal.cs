using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Modal de resumo ao fim de cada rodada
/// Mostra pontuação, histórico e ranking
/// </summary>
public class RoundSummaryModal : MonoBehaviour
{
    [Header("Modal Container")]
    [SerializeField] private GameObject modalPanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button nextRoundButton;
    
    [Header("Summary Content")]
    [SerializeField] private TMP_Text roundNumberText;
    [SerializeField] private TMP_Text winnerText;
    [SerializeField] private Transform playerScoresContainer;
    [SerializeField] private GameObject playerScoreEntryPrefab;
    
    [Header("Round History")]
    [SerializeField] private Transform roundHistoryContainer;
    [SerializeField] private GameObject roundHistoryEntryPrefab;
    
    [Header("Final Ranking (último round)")]
    [SerializeField] private Transform finalRankingContainer;
    [SerializeField] private GameObject rankingEntryPrefab;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
        
        if (nextRoundButton != null)
            nextRoundButton.onClick.AddListener(OnNextRoundClicked);
    }

    public void ShowSummary(RoundSummaryData data)
    {
        if (modalPanel != null)
            modalPanel.SetActive(true);
        
        // Atualiza número da rodada
        if (roundNumberText != null)
            roundNumberText.text = $"Rodada {data.roundNumber}";
        
        // Mostra vencedor da rodada
        if (winnerText != null)
            winnerText.text = $"{data.winnerName} venceu!";
        
        // Preenche pontuação dos jogadores
        PopulatePlayerScores(data.playerScores);
        
        // Preenche histórico de rodadas
        PopulateRoundHistory(data.roundHistory);
        
        // Se é o último round, mostra ranking final
        if (data.isFinalRound)
        {
            PopulateFinalRanking(data.finalRanking);
        }
        else if (finalRankingContainer != null)
        {
            finalRankingContainer.gameObject.SetActive(false);
        }
    }

    public void Hide()
    {
        if (modalPanel != null)
            modalPanel.SetActive(false);
        
        gameObject.SetActive(false);
    }

    private void PopulatePlayerScores(List<PlayerScoreData> scores)
    {
        if (playerScoresContainer == null || playerScoreEntryPrefab == null)
            return;
        
        // Limpa entradas anteriores
        foreach (Transform child in playerScoresContainer)
            Destroy(child.gameObject);
        
        // Cria nova entrada para cada jogador
        foreach (var score in scores)
        {
            GameObject entry = Instantiate(playerScoreEntryPrefab, playerScoresContainer);
            
            // Configura com dados do jogador
            TMP_Text nameText = entry.transform.Find("NameText")?.GetComponent<TMP_Text>();
            TMP_Text scoreText = entry.transform.Find("ScoreText")?.GetComponent<TMP_Text>();
            TMP_Text penaltyText = entry.transform.Find("PenaltyText")?.GetComponent<TMP_Text>();
            
            if (nameText != null)
                nameText.text = score.playerName;
            
            if (scoreText != null)
                scoreText.text = $"{score.totalScore} pts";
            
            if (penaltyText != null)
                penaltyText.text = score.roundPenalty > 0 ? $"+{score.roundPenalty}" : "---";
        }
    }

    private void PopulateRoundHistory(List<RoundHistoryEntry> history)
    {
        if (roundHistoryContainer == null || roundHistoryEntryPrefab == null)
            return;
        
        // Limpa histórico anterior
        foreach (Transform child in roundHistoryContainer)
            Destroy(child.gameObject);
        
        // Cria entrada para cada rodada (estilo placar de boliche)
        foreach (var entry in history)
        {
            GameObject entryObj = Instantiate(roundHistoryEntryPrefab, roundHistoryContainer);
            
            TMP_Text roundText = entryObj.transform.Find("RoundText")?.GetComponent<TMP_Text>();
            TMP_Text winnerText = entryObj.transform.Find("WinnerText")?.GetComponent<TMP_Text>();
            
            if (roundText != null)
                roundText.text = $"R{entry.roundNumber}";
            
            if (winnerText != null)
                winnerText.text = entry.winnerName;
        }
    }

    private void PopulateFinalRanking(List<RankingEntry> ranking)
    {
        if (finalRankingContainer == null || rankingEntryPrefab == null)
            return;
        
        finalRankingContainer.gameObject.SetActive(true);
        
        // Limpa ranking anterior
        foreach (Transform child in finalRankingContainer)
            Destroy(child.gameObject);
        
        // Cria entrada para cada posição (1º, 2º, 3º, 4º)
        for (int i = 0; i < ranking.Count; i++)
        {
            GameObject entry = Instantiate(rankingEntryPrefab, finalRankingContainer);
            
            TMP_Text positionText = entry.transform.Find("PositionText")?.GetComponent<TMP_Text>();
            TMP_Text nameText = entry.transform.Find("NameText")?.GetComponent<TMP_Text>();
            TMP_Text scoreText = entry.transform.Find("ScoreText")?.GetComponent<TMP_Text>();
            
            if (positionText != null)
                positionText.text = $"{i + 1}º";
            
            if (nameText != null)
                nameText.text = ranking[i].playerName;
            
            if (scoreText != null)
                scoreText.text = $"{ranking[i].totalScore} pts";
        }
    }

    private void OnNextRoundClicked()
    {
        Debug.Log("[RoundSummary] Next round button clicked");
        Hide();
        // TODO: Iniciar próxima rodada
    }
}

/// <summary>
/// Dados para exibir no resumo da rodada
/// </summary>
[System.Serializable]
public class RoundSummaryData
{
    public int roundNumber;
    public string winnerName;
    public bool isFinalRound;
    public List<PlayerScoreData> playerScores;
    public List<RoundHistoryEntry> roundHistory;
    public List<RankingEntry> finalRanking; // Só preenchido se isFinalRound = true
}

[System.Serializable]
public class PlayerScoreData
{
    public string playerName;
    public int totalScore;
    public int roundPenalty; // Pontos ganhos/perdidos nesta rodada
}

[System.Serializable]
public class RoundHistoryEntry
{
    public int roundNumber;
    public string winnerName;
}

[System.Serializable]
public class RankingEntry
{
    public string playerName;
    public int totalScore;
}
