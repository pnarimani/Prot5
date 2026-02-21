using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SiegeSurvival
{
    public class DayReportPopup : MonoBehaviour
    {
        TextMeshProUGUI _title;
        TextMeshProUGUI _description;

        void Awake()
        {
            var closeButton = this.FindChildRecursive<Button>("#Close");
            _title = this.FindChildRecursive<TextMeshProUGUI>("#Title"); 
            _description = this.FindChildRecursive<TextMeshProUGUI>("#Description"); 
            closeButton.onClick.AddListener(() => Destroy(gameObject));
        }
    }
}
