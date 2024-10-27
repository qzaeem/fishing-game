using System.Collections.Generic;
using UnityEngine;

namespace Fishing.Variables
{
    [CreateAssetMenu(fileName = "Config", menuName = "Variable/ConfigVariable")]
    public class ConfigVariable : Variable<GameConfig>
    {

    }

    [System.Serializable]
    public class GameConfig
    {
        public int maxGameTime;
        public int bulletsPerSecond;
        public int bulletsInPool;
        public int fishesInPool;
        public float lowVolatilityFishSpeed;
        public float mediumVolatilityFishSpeed;
        public float higholatilityFishSpeed;
        public int lowVolatilityFishScore;
        public int mediumVolatilityFishScore;
        public int highVolatilityFishScore;
        public Color lowFishColor;
        public Color mediumFishColor;
        public Color highFishColor;
        public Color noHitColor;
        public List<Vector2> slots;
    }
}
