using UnityEngine;
using UnityEngine.UI;

namespace SiegeSurvival.UI.Widgets
{
    public class ProgressBarWidget : MonoBehaviour
    {
        RectMask2D _fill, _lag;

        float _lagTimer;

        void Awake()
        {
            _fill = this.FindChildRecursive<RectMask2D>("#Fill");
            _lag = this.FindChildRecursive<RectMask2D>("#DecreaseLag");
            _lag.padding = default;
            _fill.padding = default;
        }

        void Update()
        {
            if (_lagTimer > 0)
            {
                _lagTimer -= Time.deltaTime;
                return;
            }

            var width = _lag.rectTransform.rect.width;
            var fillAmount = width - _fill.padding.z;
            var p = _lag.padding;
            p.z = Mathf.Lerp(p.z, _fill.padding.z, Time.deltaTime * 5);
            p.x = fillAmount;
            _lag.padding = p;
        }

        public void SetValue(float value, bool animated = true)
        {
            var width = _lag.rectTransform.rect.width;
            var padding = _fill.padding;
            padding.z = width * (1 - value);
            _fill.padding = padding;

            if (!animated)
            {
                var p = _lag.padding;
                p.z = _fill.padding.z;
                p.x = width * value;
                _lag.padding = p;
            }
        }
    }
}