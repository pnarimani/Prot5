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

        class ResourceItemView
        {
            public readonly TextMeshProUGUI CurrentAmount, Projection;

            public ResourceItemView(GameObject instance)
            {
                CurrentAmount = instance.FindChildRecursive<TextMeshProUGUI>("#Text");
                Projection = instance.FindChildRecursive<TextMeshProUGUI>("#Projection");
            }
        }
    }
}