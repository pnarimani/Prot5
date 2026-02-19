using SiegeSurvival.Core;
using SiegeSurvival.Data.Runtime;
using SiegeSurvival.UI.Widgets;
using UnityEngine;

namespace SiegeSurvival.UI.Panels
{
    public class MyZonePanel : MonoBehaviour
    {
        [SerializeField] ZoneWidget[] _zoneWidgets;

        GameManager _gm;

        void Start()
        {
            _gm = GameManager.Instance;
            if (_gm == null) return;

            _gm.OnStateChanged += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (_gm != null) _gm.OnStateChanged -= Refresh;
        }

        void Refresh()
        {
            var s = _gm?.State;
            if (s?.zones == null || s.zones.Length == 0) return;

            if (_zoneWidgets == null || _zoneWidgets.Length == 0)
                _zoneWidgets = GetComponentsInChildren<ZoneWidget>(true);

            ZoneState activePerimeter = s.GetActivePerimeter();
            int count = Mathf.Min(_zoneWidgets.Length, s.zones.Length);

            for (int i = 0; i < count; i++)
            {
                ZoneWidget widget = _zoneWidgets[i];
                ZoneState zone = s.zones[i];
                if (widget == null || zone == null) continue;

                widget.Init(zone.definition);
                widget.Refresh(zone, zone == activePerimeter, GetProjectedProduction(s, zone));
            }
        }

        int GetProjectedProduction(GameState state, ZoneState zone)
        {
            if (zone.isLost || state.TotalPopulation <= 0) return 0;

            int totalProjected =
                ResourceProjectionCalculator.GetFoodProduction(state) +
                ResourceProjectionCalculator.GetWaterProduction(state) +
                ResourceProjectionCalculator.GetMaterialsProduction(state) +
                ResourceProjectionCalculator.GetFuelProduction(state);

            float populationShare = zone.currentPopulation / (float)state.TotalPopulation;
            return Mathf.Max(0, Mathf.RoundToInt(totalProjected * populationShare));
        }
    }
}
