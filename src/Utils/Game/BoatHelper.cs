using System.Collections.Generic;
using System.Linq;
using System.Text;
using GridEditor;
using NeuroSdk.Messages.Outgoing;

namespace Pyran.NeuroFTK.Utils
{
    public class BoatHelper
    {
        public static readonly Dictionary<CharacterOverworld, HexLand> desiredPickupLocations = [];

        public static void HandleBoatHelp(CharacterOverworld cow)
        {
            SendClosestPickupLocation(cow);
        }

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

        public static void SendClosestPickupLocation(CharacterOverworld cow)
        {
            // if not in boat, do nothing, else remove from list
            if (!cow.IsInBoat()) return;
            else desiredPickupLocations.Remove(cow);
            if (desiredPickupLocations.Count == 0) return;
            float closest = float.PositiveInfinity;
            HexLand target = null;
            CharacterOverworld _cow = null;
            foreach (KeyValuePair<CharacterOverworld, HexLand> hex in desiredPickupLocations)
            {
                float dist = HexLand.Distance(cow.GetHexLand(), hex.Value);
                if (dist < closest)
                {
                    closest = dist;
                    target = hex.Value;
                    _cow = hex.Key;
                }
            }
            if (target == null) return;
            Context.Send($"{CharacterData.GetCharacterName(cow)} is in a boat, {CharacterData.GetCharacterName(_cow)} wants to be picked up at {HexData.GetVec2Pos(target)}", true);
        }

        public static string AddBoatTravelContext(HexLand currentHex)
        {
            StringBuilder sb = new();
            HexLand closestBoat = GetClosestBoat(currentHex);
            if (closestBoat != null)
            {
                sb.Append($"the closest boat is at {HexData.GetVec2Pos(closestBoat)}. ");
            }
            HexLand closestPort = GetClosestPort(currentHex);
            if (closestPort != null)
            {
                sb.Append($"the closest port is at {HexData.GetVec2Pos(closestPort)}.");
            }
            return sb.ToString();
        }
    }
}