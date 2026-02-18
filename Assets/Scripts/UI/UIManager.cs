using SiegeSurvival.Core;
using SiegeSurvival.Data;
using UnityEngine;

namespace SiegeSurvival.UI
{
    /// <summary>
    /// Central UI manager. Subscribes to GameManager events and shows/hides panels.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Panels")]
        public GameObject topBarPanel;
        public GameObject zonePanel;
        public GameObject workerAllocationPanel;
        public GameObject lawsPanel;
        public GameObject emergencyOrdersPanel;
        public GameObject missionsPanel;
        public GameObject evacuationPanel;
        public GameObject dailyReportPanel;
        public GameObject whyAmIDyingPanel;
        public GameObject eventLogPanel;
        public GameObject gameOverPanel;
        public GameObject victoryPanel;
        public GameObject endDayButton;

        private GameManager _gm;

        private void Start()
        {
            _gm = GameManager.Instance;
            if (_gm == null)
            {
                Debug.LogError("UIManager: GameManager.Instance is null!");
                return;
            }

            _gm.OnPhaseChanged += RefreshPanels;
            RefreshPanels();
        }

        private void OnDestroy()
        {
            if (_gm != null) _gm.OnPhaseChanged -= RefreshPanels;
        }

        private void RefreshPanels()
        {
            if (_gm == null) return;
            var phase = _gm.Phase;

            bool isPlayerTurn = phase == GamePhase.PlayerTurn;
            bool isReport = phase == GamePhase.ShowReport;
            bool isOver = phase == GamePhase.GameOver;
            bool isVictory = phase == GamePhase.Victory;

            // Always visible panels
            SetActive(topBarPanel, true);
            SetActive(zonePanel, true);

            // Player turn panels
            SetActive(workerAllocationPanel, isPlayerTurn);
            SetActive(lawsPanel, isPlayerTurn);
            SetActive(emergencyOrdersPanel, isPlayerTurn);
            SetActive(missionsPanel, isPlayerTurn);
            SetActive(evacuationPanel, isPlayerTurn && _gm.CanEvacuate());
            SetActive(endDayButton, isPlayerTurn);
            SetActive(whyAmIDyingPanel, isPlayerTurn || isReport);
            SetActive(eventLogPanel, isPlayerTurn || isReport);

            // Overlay panels
            SetActive(dailyReportPanel, isReport);
            SetActive(gameOverPanel, isOver);
            SetActive(victoryPanel, isVictory);
        }

        private void SetActive(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active)
                go.SetActive(active);
        }

        // Called by End Day button
        public void OnEndDayClicked()
        {
            _gm?.EndDay();
        }

        // Called by Continue button in Daily Report
        public void OnContinueClicked()
        {
            _gm?.ContinueFromReport();
        }

        // Called by Restart button
        public void OnRestartClicked()
        {
            _gm?.StartNewRun();
        }
    }
}
