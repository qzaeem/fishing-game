using UnityEngine;

namespace Fishing.Variables
{
    [CreateAssetMenu(menuName = "Variable/HitFishParams")]
    public class HitFishVariable : Variable<HitFishParams>
    {
        
    }

    public class HitFishParams
    {
        public uint fishId;
        public uint bulletId;
    }
}
