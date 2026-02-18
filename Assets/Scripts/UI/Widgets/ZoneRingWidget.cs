using SiegeSurvival.Core;
using SiegeSurvival.Data.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace SiegeSurvival.UI.Widgets
{
    /// <summary>
    /// Simple concentric rectangle visualization of the 5 zones.
    /// Keep = innermost, Outer Farms = outermost.
    /// Color-coded by status: blue=perimeter, green=intact, grey=lost.
    /// </summary>
    public class ZoneRingWidget : MonoBehaviour
    {
        [Header("Zone Rings (outermost to innermost: index 0=Farms, 4=Keep)")]
        public Image[] zoneRings; // must be 5

        [Header("Colors")]
        public Color perimeterColor = new Color(0.2f, 0.4f, 1.0f, 0.8f);
        public Color intactColor = new Color(0.2f, 0.8f, 0.2f, 0.6f);
        public Color lostColor = new Color(0.2f, 0.2f, 0.2f, 0.4f);

        [Header("Integrity Fill")]
        public Image[] integrityFills; // optional: fill images for integrity visualization

        private GameManager _gm;

        private void Start()
        {
            _gm = GameManager.Instance;
            _gm.OnStateChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_gm != null) _gm.OnStateChanged -= Refresh;
        }

        private void Refresh()
        {
            if (_gm == null || _gm.State == null) return;
            var s = _gm.State;
            var perim = s.GetActivePerimeter();

            for (int i = 0; i < 5 && i < zoneRings.Length; i++)
            {
                if (zoneRings[i] == null) continue;
                var zone = s.zones[i];

                if (zone.isLost)
                {
                    zoneRings[i].color = lostColor;
                }
                else if (zone == perim)
                {
                    zoneRings[i].color = perimeterColor;
                }
                else
                {
                    zoneRings[i].color = intactColor;
                }

                // Integrity fill
                if (integrityFills != null && i < integrityFills.Length && integrityFills[i] != null)
                {
                    float fill = zone.isLost ? 0f : zone.currentIntegrity / (float)zone.definition.baseIntegrity;
                    integrityFills[i].fillAmount = Mathf.Clamp01(fill);
                }
            }
        }
    }
}
