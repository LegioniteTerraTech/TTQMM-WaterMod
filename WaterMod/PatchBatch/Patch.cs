// https://github.com/fuqunaga/RapidGUI

using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Nuterra.NativeOptions;
using System.IO;
using TerraTechETCUtil;
using UnityEngine.UI;
using System.Collections.Generic;
using static CompoundExpression;




#if !STEAM
using ModHelper.Config;
#else
using ModHelper;
#endif

namespace WaterMod
{
    internal class Patches
    {
        private static bool TryGetOceanVariant(string key, out string value)
        {
            try
            {
                return ManOceanGenerator.SceneryWaterVariants.TryGetValue(key, out value);
            }
            catch { }
            value = null;
            return false;
        }
        //*
        [HarmonyPatch(typeof(TerrainObject))]
        [HarmonyPatch("SpawnFromPrefab", new Type[] { typeof(WorldTile), typeof(Vector3), typeof(Quaternion),
        typeof(float), typeof(IntVector2)})]
        private static class RedirectSpawningIfNeeded
        {
            internal static bool Prefix(TerrainObject __instance, ref WorldTile tile, ref Vector3 pos, 
                ref Quaternion rot, ref float scale, ref IntVector2 cellCoord, ref Transform __result)
            {
                if (WaterGlobals.OceanMan2 && pos.y < ManWater.height &&
                   TryGetOceanVariant(__instance.name, out string newSpawn))
                {
                    try
                    {
                        var item = SpawnHelper.GetSceneryPrefabByName(newSpawn);
                        if (item != null)
                        {
                            __result = item.SpawnFromPrefab(tile, pos, rot, scale, cellCoord);
                            return false;
                        }
                    }
                    catch { }
                }
                return true;
            }
        }

        /*
        [HarmonyPatch(typeof(ResourcePickup))]
        [HarmonyPatch("OnPool")]
        private static class PatchResource
        {
            internal static void Postfix(ResourcePickup __instance)
            {
                var wEffect = __instance.gameObject.AddComponent<WaterObj>();
                wEffect.effectBase = __instance;
                wEffect.effectType = EffectTypes.ResourceChunk;
                wEffect.GetRBody();
            }
        }*/
        //*/

        [HarmonyPatch(typeof(ManWorld))]
        [HarmonyPatch("Reset")]
        internal static class ManWorldPatches
        {
            private static void FlagBiomesUnready()
            {
                try
                {
                    ManOceanGenerator.oceanBiomesAllReady = false;
                }
                catch { }
            }
            internal static void Prefix(ManWorld __instance)
            {
                if (__instance.CurrentBiomeMap != null)
                {
                    DebugWater.Log("Biomes reset");
                    try
                    {
                        if (WaterGlobals.OceanMan2)
                            FlagBiomesUnready();
                    }
                    catch { }
                }
            }
        }
        [HarmonyPatch(typeof(BiomeMap))]
        [HarmonyPatch("GetBiomeDB")]
        internal class AddOceanicBiomes
        {
            private static void UpdateBiomeState(BiomeMap biomeMap)
            {
                try
                {
                    if (WaterGlobals.OceanMan2)
                        ManOceanGenerator.InitiateAndOrEnableOceanicBiomes(biomeMap);
                    else
                        ManOceanGenerator.DisableOceanicBiomes(biomeMap);
                }
                catch (Exception e)
                {
                    ManModGUI.ShowErrorPopup("OceanMan failed - " + e);
                }
            }
            //AddOceanicBiomes
            internal static void Prefix(BiomeMap __instance)
            {
                if (QPatch.IsBiomeInjectorPresent)
                {
                    try
                    {
                        UpdateBiomeState(__instance);
                    }
                    catch { }
                }
            }
        }
    }
}