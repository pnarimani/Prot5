using System;
using SiegeSurvival.Core;
using SiegeSurvival.UI.Widgets;
using UnityEngine;

namespace SiegeSurvival.UI.Panels
{
    public class StatusContainer : MonoBehaviour
    {
        ProgressBarWidget _unrest;
        ProgressBarWidget _morale;
        ProgressBarWidget _sickness;

        GameManager _gm;

        void Awake()
        {
            _unrest = Find("#Unrest");
            _morale = Find("#Morale");
            _sickness = Find("#Sickness");
        }

        void Start()
        {
            _gm = GameManager.Instance;
            _gm.OnStateChanged += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (_gm != null) _gm.OnStateChanged -= Refresh;
        }

        void Refresh()
        {
            var s = _gm.State;
            if (s == null)
                return;

            _unrest.SetValue(s.unrest / 100f);
            _morale.SetValue(s.morale / 100f);
            _sickness.SetValue(s.sickness / 100f);
        }

        ProgressBarWidget Find(string unrest) => this.FindChildRecursive<Transform>(unrest).GetComponentInChildren<ProgressBarWidget>();
    }
}