using System.Collections.Generic;
using FTKItemName;

namespace Pyran.NeuroFTK.Utils
{
    public class ItemData
    {
        public static Dictionary<string, string> GetAllItemData(List<FTKItem> items)
        {
            Dictionary<string, string> data = [];
            foreach (FTKItem item in items)
            {
                data.Merge(GetItemData(item));
            }
            return data;
        }

        public static Dictionary<string, string> GetItemData(FTKItem item)
        {
            Dictionary<string, string> data = [];
            string id = GetItemName(item);
            string description = GetItemDescription(item);
            data[id] = description;
            return data;
        }

        public static string GetItemName(FTKItem item)
        {
            return "";
        }

        public static string GetItemDescription(FTKItem item)
        {
            return "";
        }
    }
}