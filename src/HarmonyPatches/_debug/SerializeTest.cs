using System.Collections.Generic;
using GridEditor;
using UnityEngine;

namespace Pyran.NeuroFTK.HarmonyPatches
{
    public sealed class SerializeTest
    {
        public readonly string Name;
        public readonly int Level;
        public readonly int Xp;
        public readonly string Health;
        public readonly int Gold;
        public readonly int PipeItemLevel;
        public readonly Dictionary<Vector2, string> HexTiles;

        public static SerializeTest Calculate(CharacterOverworld cow) => new(cow);

        private SerializeTest(CharacterOverworld cow)
        {
            Plugin.Logger.LogWarning("Create serialize test");
            Name = cow.m_CharacterStats.m_CharacterName;
            Level = cow.m_CharacterStats.m_PlayerLevel;
            Xp = cow.m_CharacterStats.m_PlayerXP;
            Health = cow.m_CharacterStats.GetHealthDisplayString();
            Gold = cow.m_CharacterStats.m_Gold;
            FTK_pipe pipe = FTK_pipeDB.GetDB().GetEntry(cow.m_CharacterStats.GetPipe());
            PipeItemLevel = (int)pipe.m_PipeItem;
            HexTiles = [];
            Vector3 itemPos;
            Vector2 pos;
            foreach (HexLand hex in OverworldMovement.tiles)
            {
                itemPos = hex.GetPosition();
                pos = new(itemPos.x, itemPos.z);
                HexTiles.Add(pos, hex.GetLocationDisplayValue(cow));
            }
        }

        private void EmptyFunc()
        {
            int i;
        }
    }
}