// https://github.com/fuqunaga/RapidGUI

using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Nuterra.NativeOptions;
using System.IO;
using TerraTechETCUtil;

#if !STEAM
using ModHelper.Config;
#else
using ModHelper;
#endif

namespace WaterMod
{
    internal class Patches
    {
        [HarmonyPatch(typeof(Tank), "OnSpawn")]
        [HarmonyPriority(-69)]
        static class AbsoluteAssertWaterTank
        {
            static void Postfix(Tank __instance)
            {
                try
                {
                    WaterTank.Insure(__instance).OnSpawnRemote();
                }
                catch (Exception e)
                {
                    DebugWater.Log("AbsoluteAssertWaterTank failed - " + e);
                }
            }
        }
        [HarmonyPatch(typeof(ModuleAnchor))]
        [HarmonyPatch("IsColliderBlockingAnchor")]
        private class AllowAnchorInWater
        {
            private static bool Prefix(ModuleAnchor __instance, ref Collider col, ref bool __result)
            {
                if (col.gameObject.layer == ManWater.WaterLayer || col.gameObject.GetComponent<ManWater>())
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(ModuleLight), "OnAttached")]
        [HarmonyPriority(-69)]
        static class LightsCheckup
        {
            static void Postfix(ModuleLight __instance)
            {
                if (__instance.block?.tank)
                {
                    WaterTank.Insure(__instance.block.tank).OnSplashdown.Add(__instance);
                }
            }
        }
        [HarmonyPatch(typeof(ModuleLight), "OnDetached")]
        [HarmonyPriority(-69)]
        static class LightsCheckup2
        {
            static void Prefix(ModuleLight __instance)
            {
                if (__instance.block?.tank)
                {
                    WaterTank.Insure(__instance.block.tank).OnSplashdown.Remove(__instance);
                }
            }
        }
        [HarmonyPatch(typeof(ModuleLight), "EnableLights")]
        [HarmonyPriority(-69)]
        static class LightsWhenDark
        {
            static void Prefix(ModuleLight __instance, ref bool enable)
            {
                if (__instance.block?.tank && WaterTank.Insure(__instance.block.tank).SubmergeAmount > 0)
                    enable = true;
            }
        }
        [HarmonyPatch(typeof(ManTimeOfDay), "UpdateBiomeColours")]
        [HarmonyPriority(-69)]
        static class DarknessEffect
        {
            static void Postfix(ManTimeOfDay __instance)
            {
                if (ManWater.CameraSubmerged)
                    ManWater.UpdateDarkness();
            }
        }
        
        /*
        [HarmonyPatch(typeof(TerrainObject))]
        [HarmonyPatch("SpawnFromPrefab", new Type[] { typeof(WorldTile), typeof(Vector3), typeof(Quaternion),
        typeof(float), typeof(IntVector2)})]
        private class RedirectSpawningIfNeeded
        {
            private static bool Prefix(TerrainObject __instance, ref WorldTile tile, ref Vector3 pos, 
                ref Quaternion rot, ref float scale, ref IntVector2 cellCoord, ref Transform __result)
            {
                if (QPatch.DestroyTreesInWater && pos.y < ManWater.height && OceanFormer.ObjectTypesWaterVariants.TryGetValue(__instance.name, out string newSpawn))
                {
                    try
                    {
                        var item = SpawnHelper.GetResourceNodePrefab(newSpawn);
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
        */
        [HarmonyPatch(typeof(TankBeam))]
        [HarmonyPatch("SetHoverBase")]
        private class TankInsureBBeamUnderwater
        {
            private static FieldInfo hBase = typeof(TankBeam).GetField("m_HoverBase", BindingFlags.Instance | BindingFlags.NonPublic);
            private static void Postfix(TankBeam __instance)
            {
                Vector3 pos = (Vector3)hBase.GetValue(__instance);
                if (pos.y < ManWater.heightCalc)
                    hBase.SetValue(__instance, pos.SetY(ManWater.heightCalc));
            }
        }

        [HarmonyPatch(typeof(ManLooseBlocks))]
        [HarmonyPatch("DoSpawnTankBlock")]
        private class TankBlockSpawned
        {
            private static void Postfix(ref TankBlock __result)
            {
                WaterBlock.Insure(__result);
            }
        }
        [HarmonyPatch(typeof(TankBlock))]
        [HarmonyPatch("OnRecycle")]
        private class TankBlockRecycle
        {
            private static void Postfix(TankBlock __instance)
            {
                try
                {
                    WaterBlock.Insure(__instance).TryRemoveSurface();
                }
                catch { }
            }
        }

        [HarmonyPatch(typeof(Projectile), "Fire")]
        private class PatchProjectileSpawn
        {
            private static void Postfix(Projectile __instance)
            {
                WaterObj.Insure(__instance).Reset();
            }
        }

        [HarmonyPatch(typeof(MissileProjectile), "Fire")]
        private class PatchMissile
        {
            private static void Prefix(MissileProjectile __instance)
            {
                WaterObj.Insure(__instance).Reset();
            }
        }

        [HarmonyPatch(typeof(LaserProjectile), "Fire")]
        private class PatchLaser
        {
            private static void Prefix(LaserProjectile __instance)
            {
                WaterObj.Insure(__instance).Reset();
            }
        }

        /*
        [HarmonyPatch(typeof(ResourcePickup))]
        [HarmonyPatch("OnPool")]
        private class PatchResource
        {
            private static void Postfix(ResourcePickup __instance)
            {
                var wEffect = __instance.gameObject.AddComponent<WaterObj>();
                wEffect.effectBase = __instance;
                wEffect.effectType = EffectTypes.ResourceChunk;
                wEffect.GetRBody();
            }
        }*/
        [HarmonyPatch(typeof(ResourceManager))]
        [HarmonyPatch("SpawnResource")]
        private class PatchResourceSpawn
        {
            private static void Postfix(ResourcePickup __instance)
            {
                if (__instance != null)
                    WaterObj.Insure(__instance).Reset();
            }
        }


        [HarmonyPatch(typeof(TileManager))]
        [HarmonyPatch("Init")]
        private class PatchTiles
        {
            private static void Postfix(TileManager __instance)
            {
                RemoveScenery.Sub();
            }
        }

        [HarmonyPatch(typeof(ManWorld))]
        [HarmonyPatch("Reset")]
        internal static class ManWorldPatches
        {
            private static void Prefix(ManWorld __instance)
            {
                if (__instance.CurrentBiomeMap != null)
                {
                    DebugWater.Log("Biomes reset");
                    OceanFormer.ready = false;
                }
            }
        }
        [HarmonyPatch(typeof(BiomeMap))]
        [HarmonyPatch("GetBiomeDB")]
        internal class AddOceanicBiomes
        {
            //AddOceanicBiomes
            private static void Prefix(BiomeMap __instance)
            {
                if (QPatch.OceanMan)
                    OceanFormer.AddOceanicBiomes(__instance);
                else
                    OceanFormer.RemoveOceanicBiomes(__instance);
            }
        }
    }
}