using System.Collections.Generic;
using System.Linq;
using GridEditor;

namespace Pyran.NeuroFTK.Utils
{
    public class BoatHelper
    {
        public static readonly List<HexLand> desiredPickupLocations = [];


        public static bool HasActiveBoat()
        {
            return GetAllBoats().Count() > 0;
        }

        public static IEnumerable<HexLand> GetAllBoatHexes()
        {
            return GetAllBoats().Select(boat => boat.m_HexLand);
        }

        public static HexLand GetClosestBoat(HexLand current)
        {
            IEnumerable<MiniHexBoat> boats = GetAllBoats();
            float closest = float.PositiveInfinity;
            HexLand target = null;
            foreach (HexLand hex in boats.Select(boat => boat.m_HexLand))
            {
                float dist = HexLand.Distance(current, hex);
                if (dist < closest)
                {
                    closest = dist;
                    target = hex;
                }
            }
            return target;
        }

        public static IEnumerable<MiniHexBoat> GetAllBoats()
        {
            IEnumerable<MiniHexBoat> boats = FTKHex.Instance.GetPOIList(MiniHexInfo.MiniHexType.Boat).Cast<MiniHexBoat>();
            return boats;
        }

        public static HexLand GetClosestPort(HexLand current)
        {
            IEnumerable<MiniHexUtility> docks = GetAllDocks();
            float closest = float.PositiveInfinity;
            HexLand target = null;
            foreach (HexLand hex in docks.Select(dock => dock.m_HexLand))
            {
                float dist = HexLand.Distance(current, hex);
                if (dist < closest)
                {
                    closest = dist;
                    target = hex;
                }
            }
            return target;
        }

        public static IEnumerable<MiniHexUtility> GetAllDocks()
        {
            IEnumerable<MiniHexUtility> docks = FTKHex.Instance.GetPOIList(MiniHexInfo.MiniHexType.Utility).Cast<MiniHexUtility>();
            return docks.Select(dock => dock.m_ID == FTK_utility.ID.Port).Cast<MiniHexUtility>();
        }
    }
}