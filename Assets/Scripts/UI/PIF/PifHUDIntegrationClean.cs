using System.Collections.Generic;
using UnityEngine;

namespace PIF.UI
{
    /// <summary>
    /// Integrates PifHUDClean with PifeGameManager and GameBootstrap.
    /// </summary>
    [RequireComponent(typeof(PifHUDClean))]
    public class PifHUDIntegrationClean : MonoBehaviour
    {
        private PifHUDClean _hud;
        private PifeGameManager _gameManager;
        private GameBootstrap _bootstrap;

        [Header("Auto-Connect")]
        [SerializeField] private bool findManagersOnStart = true;
        [SerializeField] private GameObject meldCardPrefab;

        private void Awake()
        {
            _hud = GetComponent<PifHUDClean>();
        }

        private void Start()
        {
            if (findManagersOnStart)
            {
                FindManagers();
            }

            ConnectToGameManager();
            ConnectToBootstrap();
        }

        private void OnDestroy()
        {
            DisconnectFromGameManager();
        }

        private void FindManagers()
        {
            if (_gameManager == null)
            {
                _gameManager = FindFirstObjectByType<PifeGameManager>();
                if (_gameManager == null)
                {
                    Debug.LogWarning("[PifHUDIntegrationClean] PifeGameManager not found in scene.");
                }
            }

            if (_bootstrap == null)
            {
                _bootstrap = FindFirstObjectByType<GameBootstrap>();
                if (_bootstrap == null)
                {
                    Debug.LogWarning("[PifHUDIntegrationClean] GameBootstrap not found in scene.");
                }
            }
        }

        private void ConnectToGameManager()
        {
            if (_gameManager == null)
                return;

            _gameManager.OnTurnChanged += HandleTurnChanged;
            _gameManager.OnGameStarted += HandleGameStarted;
            _gameManager.OnGameEnded += HandleGameEnded;

            Debug.Log("[PifHUDIntegrationClean] Connected to PifeGameManager");
        }

        private void DisconnectFromGameManager()
        {
            if (_gameManager == null)
                return;

            _gameManager.OnTurnChanged -= HandleTurnChanged;
            _gameManager.OnGameStarted -= HandleGameStarted;
            _gameManager.OnGameEnded -= HandleGameEnded;
        }

        private void ConnectToBootstrap()
        {
            if (_bootstrap == null)
                return;

            UpdateAllHandCounts();
        }

        private void HandleGameStarted()
        {
            Debug.Log("[PifHUDIntegrationClean] Game started.");

            for (int i = 0; i < 4; i++)
            {
                _hud.UpdatePlayerCardCount(i, 9);
                _hud.UpdatePlayerScore(i, 0);
            }

            if (_gameManager != null)
            {
                _hud.SetCurrentTurn(_gameManager.GetCurrentPlayerIndex());
            }
        }

        private void HandleTurnChanged()
        {
            if (_gameManager == null)
                return;

            int currentPlayerIndex = _gameManager.GetCurrentPlayerIndex();
            Debug.Log($"[PifHUDIntegrationClean] Turn changed to player {currentPlayerIndex}");

            _hud.SetCurrentTurn(currentPlayerIndex);
            UpdateAllHandCounts();
        }

        private void HandleGameEnded()
        {
            Debug.Log("[PifHUDIntegrationClean] Game ended.");
            UpdateAllHandCounts();
        }

        private void UpdateAllHandCounts()
        {
            if (_gameManager == null)
                return;

            for (int i = 0; i < 4; i++)
            {
                int cardCount = _gameManager.GetPlayerHandCount(i);
                _hud.UpdatePlayerCardCount(i, cardCount);
            }
        }

        public void ShowPlayerMeld(int playerIndex, Card[] meldCards)
        {
            if (meldCards == null || meldCards.Length == 0)
                return;

            PifMeldArea meldArea = _hud.GetMeldArea(playerIndex);
            if (meldArea != null)
            {
                if (meldCardPrefab == null)
                {
                    Debug.LogWarning("[PifHUDIntegrationClean] meldCardPrefab not set. Using fallback cards.");
                }
                meldArea.AddMeld(new List<Card>(meldCards), meldCardPrefab);
            }
        }

        public void ClearPlayerMelds(int playerIndex)
        {
            PifMeldArea meldArea = _hud.GetMeldArea(playerIndex);
            if (meldArea != null)
            {
                meldArea.ClearMelds();
            }
        }

        public void ClearAllMelds()
        {
            for (int i = 0; i < 4; i++)
            {
                ClearPlayerMelds(i);
            }
        }
    }
}
