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

            var p = _lag.padding;
            p.z = Mathf.Lerp(p.z, _fill.padding.z, Time.deltaTime * 5);
            _lag.padding = p;
        }

        public void SetValue(float value)
        {
            var width = _lag.rectTransform.rect.width;
            var padding = _fill.padding;
            padding.z = width * value;
            _fill.padding = padding;
        }
    }
}