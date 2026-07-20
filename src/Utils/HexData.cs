using UnityEngine;

namespace Pyran.NeuroFTK.Utils
{
    public class HexData
    {
        public static Vector2 GetVec2Pos(HexLand hex)
        {
            Vector3 pos = hex.GetPosition();
            return new Vector2(pos.x, pos.z);
        }
    }
}