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

        public static bool IsPoiComplete(MiniHexInfo poi)
        {
            if (poi == null) return true;
            if (poi.m_Deactivated) return true;
            return false;
        }
    }
}