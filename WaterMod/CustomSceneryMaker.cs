using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TerraTechETCUtil;

namespace WaterMod
{
    // Makes some special thingies
    public class CustomSceneryMaker
    {
        private static readonly FieldInfo ResLook = typeof(ResourceDispenser).GetField("m_ResourceSpawnChances", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo ResCount = typeof(ResourceDispenser).GetField("m_TotalChunks", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo ResStages = typeof(ResourceDispenser).GetField("m_DamageStages", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo Res_YL = typeof(ResourceDispenser).GetField("m_MinY", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo Res_YU = typeof(ResourceDispenser).GetField("m_MaxY", BindingFlags.NonPublic | BindingFlags.Instance);

        public static ResourceDispenser Adaranthite;
        public static float healthMulti = 6;

        public static void MakeScenery()
        {
            if (Adaranthite == null)
                MakeAdaranthite();
        }
        private static void MakeAdaranthite()
        {
            Adaranthite = SpawnHelper.GetResourceNodePrefab(SceneryTypes.PlumbiteSeam, "grasslands").GetComponent<ResourceDispenser>();

            ResourceDispenser.DamageStage[] DS = (ResourceDispenser.DamageStage[])ResStages.GetValue(Adaranthite);
            for (int i = 0; i < DS.Length; i++)
            {
                ResourceDispenser.DamageStage ds1 = DS[i];
                ds1.m_Health = ds1.m_Health * healthMulti;
                DS[i] = ds1;
            }
            ResStages.SetValue(Adaranthite, DS);
            ResCount.SetValue(Adaranthite, 6);
            ResourceSpawnChance[] RSC = new ResourceSpawnChance[]
                {
                    new ResourceSpawnChance()
                    {
                        chunkType = ChunkTypes._deprecated_SmallMetalOre,
                        spawnWeight = 1,
                    }
                };
            ResLook.SetValue(Adaranthite, RSC);
            Res_YL.SetValue(Adaranthite, -3f);
            Res_YU.SetValue(Adaranthite, 3);
        }
    }
}
