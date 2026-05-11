using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace WaterMod
{
    internal class MainPatches
    {// The NEW crash handler with useful mod-crash-related information

        internal static class ModuleLightPatches
        {
            internal static Type target = typeof(ModuleLight);
            /// <summary> LightsCheckup </summary>
            [HarmonyPriority(-69)]
            internal static void OnAttached_Postfix(ModuleLight __instance)
            {   
                if (__instance?.block?.tank)
                {
                    WaterTank.Insure(__instance.block.tank).OnSplashdown.Add(__instance);
                    //DebugWater.Log("Add one(1) light");
                }
            }
            /// <summary> LightsCheckup2 </summary>
            [HarmonyPriority(-69)]
            internal static void OnDetached_Postfix(ModuleLight __instance)
            {
                if (__instance?.block?.tank)
                {
                    WaterTank.Insure(__instance.block.tank).OnSplashdown.Remove(__instance);
                    //DebugWater.Log("Subtract one(1) light");
                }
            }
            /// <summary> LightsWhenDark </summary>
            [HarmonyPriority(-69)]
            internal static void EnableLights_Prefix(ModuleLight __instance, ref bool enable)
            {
                try
                {
                    if (__instance != null && __instance.block != null && __instance.block.tank != null)
                    {
                        var Tank = WaterTank.Insure(__instance.block.tank);
                        if (Tank != null && Tank.SubmergedThisUpdate)
                            enable = true;
                    }
                }
                catch { }
            }
        }
    }
}
