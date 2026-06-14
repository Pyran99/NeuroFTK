using System.Collections.Generic;

namespace Pyran.NeuroFTK.Utils
{
    public class ItemData
    {
        public static Dictionary<string, string> GetAllItemData()
        {
            Dictionary<string, string> data = [];
            string id = GetItemName();
            string description = GetItemDescription();
            data[id] = description;
            return data;
        }

        public static Dictionary<string, string> GetItemData()
        {
            Dictionary<string, string> data = [];
            string id = GetItemName();
            string description = GetItemDescription();
            data[id] = description;
            return data;
        }

        public static string GetItemName()
        {
            return "";
        }

        public static string GetItemDescription()
        {
            return "";
        }
    }
}