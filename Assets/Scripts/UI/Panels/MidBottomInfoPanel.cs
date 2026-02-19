using SiegeSurvival.Core;
using SiegeSurvival.UI.Widgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SiegeSurvival.UI.Panels
{
    public class MidBottomInfoPanel : MonoBehaviour
    {
        ProgressBarWidget _siege;
        TextMeshProUGUI _siegeText;
        Button _eventLogButton;
        EventLogPanel _eventLogPanel;
        GameManager _gm;

        bool _inited;

        void Awake()
        {
            _siege = FindBar("#Siege");
            _siegeText = this.FindChildRecursive<TextMeshProUGUI>("#SiegeText");
            _eventLogButton = this.FindChildRecursive<Button>("#EventLogButton");
            _eventLogPanel = EnsureEventLogPanel();

            if (_eventLogButton != null && _eventLogPanel != null)
                _eventLogButton.onClick.AddListener(_eventLogPanel.OpenPopup);
        }

        void Start()
        {
            _gm = GameManager.Instance;
            if (_gm == null)
                return;

            _gm.OnStateChanged += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (_eventLogButton != null && _eventLogPanel != null)
                _eventLogButton.onClick.RemoveListener(_eventLogPanel.OpenPopup);

            if (_gm != null)
                _gm.OnStateChanged -= Refresh;
        }

        void Refresh()
        {
            var s = _gm?.State;
            if (s == null)
                return;

            _siege.SetValue(Mathf.Clamp01(s.siegeIntensity / 6f), _inited);
            _siegeText.text = $"Siege Level: {s.siegeIntensity}/6";
            _inited = true;
        }

        ProgressBarWidget FindBar(string name)
            => this.FindChildRecursive<Transform>(name)?.GetComponentInChildren<ProgressBarWidget>();

        EventLogPanel EnsureEventLogPanel()
        {
            if (transform.root == null)
                return null;

            var popup = transform.root.FindChildRecursive<Transform>("EventLogPopup");
            if (popup != null)
            {
                var existing = popup.GetComponent<EventLogPanel>();
                if (existing != null)
                    return existing;
            }

            var popupGo = MakeRect("EventLogPopup", transform.root);
            StretchToParent(popupGo.GetComponent<RectTransform>());
            popupGo.AddComponent<Image>().color = new Color(0.03f, 0.03f, 0.05f, 0.92f);

            var rootVlg = popupGo.AddComponent<VerticalLayoutGroup>();
            rootVlg.padding = new RectOffset(220, 220, 120, 120);
            rootVlg.childControlWidth = true;
            rootVlg.childControlHeight = true;
            rootVlg.childForceExpandWidth = true;
            rootVlg.childForceExpandHeight = false;
            rootVlg.spacing = 8;
            rootVlg.childAlignment = TextAnchor.UpperCenter;

            var window = MakePanel("Window", popupGo.transform, new Color(0.11f, 0.11f, 0.18f, 0.98f));
            var windowLe = window.AddComponent<LayoutElement>();
            windowLe.flexibleHeight = 1f;
            var windowVlg = window.AddComponent<VerticalLayoutGroup>();
            windowVlg.padding = new RectOffset(16, 16, 16, 16);
            windowVlg.childControlWidth = true;
            windowVlg.childControlHeight = true;
            windowVlg.childForceExpandWidth = true;
            windowVlg.childForceExpandHeight = false;
            windowVlg.spacing = 8;

            var title = MakeTMP("Title", window.transform, "EVENT LOG", 30, new Color(0.9f, 0.8f, 0.35f, 1f));
            title.fontStyle = FontStyles.Bold;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 40;

            var scrollGo = MakePanel("ScrollView", window.transform, new Color(0.07f, 0.07f, 0.11f, 1f));
            var scrollLe = scrollGo.AddComponent<LayoutElement>();
            scrollLe.flexibleHeight = 1f;
            scrollLe.minHeight = 260;

            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 20f;

            var viewport = MakeRect("Viewport", scrollGo.transform);
            StretchToParent(viewport.GetComponent<RectTransform>());
            viewport.AddComponent<Image>().color = Color.clear;
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            var content = MakeRect("Content", viewport.transform);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = Vector2.zero;

            var contentVlg = content.AddComponent<VerticalLayoutGroup>();
            contentVlg.childControlWidth = true;
            contentVlg.childControlHeight = true;
            contentVlg.childForceExpandWidth = true;
            contentVlg.childForceExpandHeight = false;
            contentVlg.spacing = 2;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var logText = MakeTMP("#LogText", content.transform, "", 20, new Color(0.86f, 0.86f, 0.91f, 1f));
            logText.textWrappingMode = TextWrappingModes.Normal;
            logText.alignment = TextAlignmentOptions.TopLeft;
            logText.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

            var closeButtonGo = CreateButton("#CloseButton", window.transform, "CLOSE");
            closeButtonGo.AddComponent<LayoutElement>().preferredHeight = 46;
            var closeButton = closeButtonGo.GetComponent<Button>();

            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = contentRt;

            var panel = popupGo.AddComponent<EventLogPanel>();
            panel.popupRoot = popupGo;
            panel.logText = logText;
            panel.scrollRect = scrollRect;
            panel.closeButton = closeButton;
            return panel;
        }

        static GameObject MakeRect(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        static GameObject MakePanel(string name, Transform parent, Color color)
        {
            var go = MakeRect(name, parent);
            go.AddComponent<Image>().color = color;
            return go;
        }

        static TextMeshProUGUI MakeTMP(string name, Transform parent, string text, float size, Color color)
        {
            var go = MakeRect(name, parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.overflowMode = TextOverflowModes.Overflow;
            return tmp;
        }

        static GameObject CreateButton(string name, Transform parent, string label)
        {
            var go = MakeRect(name, parent);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.22f, 0.38f, 0.58f, 1f);
            var button = go.AddComponent<Button>();
            button.targetGraphic = img;

            var labelGo = MakeRect("#Text", go.transform);
            StretchToParent(labelGo.GetComponent<RectTransform>());
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 22;
            tmp.color = new Color(0.92f, 0.92f, 0.96f, 1f);
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            return go;
        }

        static void StretchToParent(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}