using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using TerraTechETCUtil;

namespace WaterMod
{
#if STEAM
    public class KickStartWaterMod : ModBase
    {
        string modName = "Water Mod + Lava";

        bool isInit = false;
        bool firstInit = false;
        public override bool HasEarlyInit()
        {
            DebugWater.Log("Water Mod: CALLED");
            return true;
        }

        // IDK what I should init here...
        public override void EarlyInit()
        {
            DebugWater.Log("Water Mod: CALLED EARLYINIT");
            if (isInit)
                return;
            try
            {
                TerraTechETCUtil.ModStatusChecker.EncapsulateSafeInit(modName, QPatch.Main);
            }
            catch { }
            isInit = true;
        }
        public override void Init()
        {
            DebugWater.Log("Water Mod: CALLED INIT");
            if (isInit)
                return;
            try
            {
                TerraTechETCUtil.ModStatusChecker.EncapsulateSafeInit(modName, QPatch.Main);
            }
            catch { }
            isInit = true;
        }
        public override void DeInit()
        {
            if (!isInit)
                return;
            //isInit = false;
        }
    }
#endif
}
