using System.Collections.Generic;

namespace Pyran.NeuroFTK.Utils
{
    public class CustomHexSort(CharacterOverworld cow) : IComparer<HexLand>
    {
        public int Compare(HexLand x, HexLand y)
        {
            int compare1 = x.GetLocationDisplayValue(cow).CompareTo(y.GetLocationDisplayValue(cow));
            if (compare1 == 0)
            {
                return HexLand.Distance(cow.m_HexLand, x).CompareTo(HexLand.Distance(cow.m_HexLand, y));
            }
            else return compare1;
        }
    }
}