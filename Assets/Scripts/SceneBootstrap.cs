using SiegeSurvival.Core;
using SiegeSurvival.Data;
using UnityEngine;

namespace SiegeSurvival
{
    public class SceneBootstrap : MonoBehaviour
    {
        [Header("Zone Definitions (must assign 5, ordered 0-4 in Inspector)")]
        public ZoneDefinition[] zoneDefinitions;

        void Awake()
        {
            var gmGo = new GameObject("GameManager");
            var gm = gmGo.AddComponent<GameManager>();
            gm.zoneDefinitions = zoneDefinitions;
        }
    }
}