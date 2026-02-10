using UnityEngine;
using Pif.UI;

namespace Pif.Gameplay.PIF
{
    [DisallowMultipleComponent]
    public class PifGameManager : MonoBehaviour
    {
        [Header("Runtime Modules")]
        [SerializeField] private bool autoBootstrapLayout = true;

        private PifRules _rules;
        private PifTurnFlow _turnFlow;
        private PifMeldValidator _meldValidator;

        public PifRules Rules => _rules;
        public PifTurnFlow TurnFlow => _turnFlow;
        public PifMeldValidator MeldValidator => _meldValidator;

        public void EnsureRuntimeDependencies()
        {
            _rules ??= new PifRules();
            _turnFlow ??= new PifTurnFlow();
            _meldValidator ??= new PifMeldValidator();

            if (!autoBootstrapLayout)
                return;

            BoardLayoutPif layout = GetComponent<BoardLayoutPif>();
            if (layout != null)
                layout.ApplyLayoutNow();
        }

        private void Awake()
        {
            EnsureRuntimeDependencies();
        }
    }
}
