using SiegeSurvival.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SiegeSurvival.UI.Panels
{
    public class ResourcesPanel : MonoBehaviour
    {
        [SerializeField] GameObject _prefab;
        [SerializeField] Transform _parent;

        [SerializeField] Sprite _foodSprite, _waterSprite, _fuelSprite, _medsSprite, _materialSprite;

        ResourceItemView _food, _water, _fuel, _meds, _material;
        GameManager _gm;

        void Start()
        {
            _food = Spawn(_foodSprite);
            _water = Spawn(_waterSprite);
            _fuel = Spawn(_fuelSprite);
            _meds = Spawn(_medsSprite);
            _material = Spawn(_materialSprite);

            _gm = GameManager.Instance;
            _gm.OnStateChanged += Refresh;
            Refresh();

            transform.RebuildAllLayouts();
        }

        void OnDestroy()
        {
            if (_gm != null) _gm.OnStateChanged -= Refresh;
        }

        void Refresh()
        {
            var s = _gm.State;
            if (s == null) return;

            _food.CurrentAmount.text = s.food.ToString();
            _water.CurrentAmount.text = s.water.ToString();
            _fuel.CurrentAmount.text = s.fuel.ToString();
            _meds.CurrentAmount.text = s.medicine.ToString();
            _material.CurrentAmount.text = s.materials.ToString();

            // Projected amounts (net change per day)
            var foodNet = ResourceProjectionCalculator.GetFoodNetChange(s);
            var waterNet = ResourceProjectionCalculator.GetWaterNetChange(s);
            var fuelNet = ResourceProjectionCalculator.GetFuelNetChange(s);
            var medsNet = ResourceProjectionCalculator.GetMedicineNetChange(s);
            var materialNet = ResourceProjectionCalculator.GetMaterialsNetChange(s);

            SetProjection(_food, foodNet);
            SetProjection(_water, waterNet);
            SetProjection(_fuel, fuelNet);
            SetProjection(_meds, medsNet);
            SetProjection(_material, materialNet);

            _food.SetTooltip(BuildFoodTooltip(s, foodNet));
            _water.SetTooltip(BuildWaterTooltip(s, waterNet));
            _fuel.SetTooltip(BuildFuelTooltip(s, fuelNet));
            _meds.SetTooltip(BuildMedicineTooltip(s, medsNet));
            _material.SetTooltip(BuildMaterialsTooltip(s, materialNet));

            _food.Projection.rectTransform.RebuildAllLayouts();
            _water.Projection.rectTransform.RebuildAllLayouts();
            _fuel.Projection.rectTransform.RebuildAllLayouts();
            _meds.Projection.rectTransform.RebuildAllLayouts();
            _material.Projection.rectTransform.RebuildAllLayouts();
        }

        void SetProjection(ResourceItemView item, int value)
        {
            item.Projection.text = ResourceProjectionCalculator.FormatNetChange(value);
            // Dark green for positive, dark red for negative (suitable for white background)
            item.Projection.color = value >= 0 ? new Color(0f, 0.5f, 0f) : new Color(0.7f, 0f, 0f);
        }

        ResourceItemView Spawn(Sprite s)
        {
            var instance = Instantiate(_prefab, _parent, false);
            instance.FindChildRecursive<Image>("#Icon").sprite = s;
            return new ResourceItemView(instance);
        }

        string BuildFoodTooltip(GameState state, int netChange)
        {
            return $"Food\n" +
                   $"Current: {state.food}\n" +
                   $"Projected: {ResourceProjectionCalculator.FormatNetChange(netChange)}/day\n" +
                   "Feeds the population. If Food reaches 0, morale drops and unrest pressure rises.";
        }

        string BuildWaterTooltip(GameState state, int netChange)
        {
            return $"Water\n" +
                   $"Current: {state.water}\n" +
                   $"Projected: {ResourceProjectionCalculator.FormatNetChange(netChange)}/day\n" +
                   "Needed daily by all citizens. At 0 Water, morale drops and food consumption increases.";
        }

        string BuildFuelTooltip(GameState state, int netChange)
        {
            return $"Fuel\n" +
                   $"Current: {state.fuel}\n" +
                   $"Projected: {ResourceProjectionCalculator.FormatNetChange(netChange)}/day\n" +
                   "Used for city operation. At 0 Fuel: sickness rises, morale falls, unrest rises, and food production is reduced.";
        }

        string BuildMedicineTooltip(GameState state, int netChange)
        {
            return $"Medicine\n" +
                   $"Current: {state.medicine}\n" +
                   $"Projected: {ResourceProjectionCalculator.FormatNetChange(netChange)}/day\n" +
                   "Consumed by Clinic Staff to reduce sickness. No medicine means clinic workers cannot provide treatment.";
        }

        string BuildMaterialsTooltip(GameState state, int netChange)
        {
            return $"Materials\n" +
                   $"Current: {state.materials}\n" +
                   $"Projected: {ResourceProjectionCalculator.FormatNetChange(netChange)}/day\n" +
                   "Used for repairs and key actions. Repair teams consume materials each day to restore perimeter integrity.";
        }

        class ResourceItemView
        {
            public readonly TextMeshProUGUI CurrentAmount, Projection;
            readonly TooltipMaker _tooltip;

            public ResourceItemView(GameObject instance)
            {
                CurrentAmount = instance.FindChildRecursive<TextMeshProUGUI>("#Text");
                Projection = instance.FindChildRecursive<TextMeshProUGUI>("#Projection");
                if (!instance.TryGetComponent<TooltipMaker>(out var tooltip))
                    tooltip = instance.AddComponent<TooltipMaker>();
                _tooltip = tooltip;
            }

            public void SetTooltip(string tooltip)
            {
                _tooltip.SetTooltip(tooltip);
            }
        }
    }
}
