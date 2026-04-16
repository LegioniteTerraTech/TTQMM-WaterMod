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



#if !STEAM
using ModHelper.Config;
#else
using ModHelper;
#endif

namespace WaterMod
{
    internal class Patches
    {
        private static Dictionary<Tank, ForceGizmo> CoBForceGizmos = new Dictionary<Tank, ForceGizmo>();
        [HarmonyPatch(typeof(TankBeam), "SetForceGizmosActive")]
        [HarmonyPriority(-400)]
        private static class AddNewGizmoTank
        {
            internal static void Prefix(TankBeam __instance, bool state, Tank ___tank, bool ___m_ForceGizmosActive)
            {
                if (state != ___m_ForceGizmosActive)
                {
                    if (state)
                    {
                        if (!CoBForceGizmos.ContainsKey(___tank))
                        {
                            CoBForceGizmos.Add(___tank, ForceGizmo.SpawnForceGizmo(___tank.trans, 
                                new Color(0.5f, 0.5f, 0.5f), true, 2f / (WaterGlobals.Density * 7.5f)));
                            var waterTank = WaterTank.Insure(___tank);

                            waterTank.AppliedForceCenter = waterTank.AppliedForceCenterThisFrame;
                            waterTank.AppliedForceDirection = waterTank.AppliedForceDirectionThisFrame;
                        }
                    }
                    else
                    {
                        if (CoBForceGizmos.TryGetValue(___tank, out var giz))
                        {
                            giz.Recycle();
                            CoBForceGizmos.Remove(___tank);
                        }
                    }
                }
            }
        }
        [HarmonyPatch(typeof(TankBeam), "UpdateForceGizmos")]
        [HarmonyPriority(-400)]
        private static class AddNewGizmoTank2
        {
            internal static void Prefix(TankBeam __instance, Tank ___tank, bool ___m_ForceGizmosActive)
            {
                if (!___m_ForceGizmosActive)
                    return;
                if (CoBForceGizmos.TryGetValue(___tank, out var giz) && giz != null)
                {
                    var waterTank = WaterTank.Insure(___tank);

                    waterTank.AppliedForceCenter = Vector3.Lerp(waterTank.AppliedForceCenter, 
                        waterTank.AppliedForceCenterThisFrame, Time.deltaTime * 4f);
                    waterTank.AppliedForceDirection = Vector3.Lerp(waterTank.AppliedForceDirection,
                        waterTank.AppliedForceDirectionThisFrame, Time.deltaTime * 4f);
                    giz.SetForceVector(waterTank.AppliedForceCenter, waterTank.AppliedForceDirection);
                }
            }
        }




        [HarmonyPatch(typeof(Tank), "OnSpawn")]
        [HarmonyPriority(-69)]
        private static class AbsoluteAssertWaterTank
        {
            internal static void Postfix(Tank __instance)
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
        private static class AllowAnchorInWater
        {
            internal static bool Prefix(ModuleAnchor __instance, ref Collider col, ref bool __result)
            {
                if (col.gameObject.layer == ManWater.WaterLayer || col.gameObject.GetComponent<ManWater>())
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(TOD_Sky), "UpdateCelestials")]
        [HarmonyPriority(-69)]
        private static class DarknessEffect
        {
            internal static void Prefix()
            {
                if (ManWater.CameraSubmerged)
                    ManWater.UpdateDarkness();
            }
        }
        [HarmonyPatch(typeof(ManTimeOfDay), "LerpCloudData")]
        [HarmonyPriority(-133700)]
        private static class CloudsEffect
        {
            private static FieldInfo sky = typeof(ManTimeOfDay).GetField("m_Sky", BindingFlags.Instance | BindingFlags.NonPublic);
            private static FieldInfo cloudflare = typeof(ManTimeOfDay).GetField("m_TargetCloudParams", BindingFlags.Instance | BindingFlags.NonPublic);
            internal static void Prefix(ManTimeOfDay __instance)
            {
                if (ManWater.CameraSubmerged)
                {
                    //TOD_CloudParameters cloudsInst = (TOD_CloudParameters)cloudflare.GetValue(__instance);
                    var cloudsInst = (sky.GetValue(ManTimeOfDay.inst) as TOD_Sky).Clouds;
                    cloudsInst.Opacity = 0.001f;
                    cloudsInst.Coverage = 0.001f;
                    cloudsInst.Brightness = 0;
                    cloudsInst.Attenuation = 0;
                    cloudsInst.Saturation = 0;
                    cloudsInst.Sharpness = 0;
                    cloudsInst.Size = 1; 
                    cloudsInst.Scattering = 0;
                }
            }
        }

        /*
        [HarmonyPatch(typeof(TerrainObject))]
        [HarmonyPatch("SpawnFromPrefab", new Type[] { typeof(WorldTile), typeof(Vector3), typeof(Quaternion),
        typeof(float), typeof(IntVector2)})]
        private static class RedirectSpawningIfNeeded
        {
            internal static bool Prefix(TerrainObject __instance, ref WorldTile tile, ref Vector3 pos, 
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
        private static class TankInsureBBeamUnderwater
        {
            private static FieldInfo hBase = typeof(TankBeam).GetField("m_HoverBase", BindingFlags.Instance | BindingFlags.NonPublic);
            internal static void Postfix(TankBeam __instance)
            {
                Vector3 pos = (Vector3)hBase.GetValue(__instance);
                if (pos.y < ManWater.HeightCalc)
                    hBase.SetValue(__instance, pos.SetY(ManWater.HeightCalc));
            }
        }

        [HarmonyPatch(typeof(ManLooseBlocks))]
        [HarmonyPatch("DoSpawnTankBlock")]
        private static class TankBlockSpawned
        {
            internal static void Postfix(ref TankBlock __result)
            {
                if (__result != null)
                    WaterBlock.Insure(__result);
            }
        }
        [HarmonyPatch(typeof(TankBlock))]
        [HarmonyPatch("OnRecycle")]
        private static class TankBlockRecycle
        {
            internal static void Postfix(TankBlock __instance)
            {
                try
                {
                    if (__instance != null)
                        WaterBlock.Insure(__instance).TryRemoveSurface();
                }
                catch { }
            }
        }

        [HarmonyPatch(typeof(Projectile), "Fire")]
        private static class PatchProjectileSpawn
        {
            internal static void Postfix(Projectile __instance)
            {
                WaterObj.Insure(__instance).Reset();
            }
        }

        [HarmonyPatch(typeof(MissileProjectile), "Fire")]
        private static class PatchMissile
        {
            internal static void Prefix(MissileProjectile __instance)
            {
                WaterObj.Insure(__instance).Reset();
            }
        }

        [HarmonyPatch(typeof(LaserProjectile), "Fire")]
        private static class PatchLaser
        {
            internal static void Prefix(LaserProjectile __instance)
            {
                WaterObj.Insure(__instance).Reset();
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
        [HarmonyPatch(typeof(ManLooseBlocks))]
        [HarmonyPatch("DoSpawnResourcePickup")]
        private static class PatchResourceSpawn
        {
            internal static void Postfix(ResourcePickup __result)
            {
                if (__result != null)
                    WaterObj.Insure(__result).Reset();
            }
        }


        [HarmonyPatch(typeof(TileManager))]
        [HarmonyPatch("Init")]
        private static class PatchTiles
        {
            internal static void Postfix(TileManager __instance)
            {
                RemoveScenery.Sub();
            }
        }

        [HarmonyPatch(typeof(ManWorld))]
        [HarmonyPatch("Reset")]
        internal static class ManWorldPatches
        {
            internal static void Prefix(ManWorld __instance)
            {
                if (__instance.CurrentBiomeMap != null)
                {
                    DebugWater.Log("Biomes reset");
                    ManOceanGenerator.ready = false;
                }
            }
        }
        [HarmonyPatch(typeof(BiomeMap))]
        [HarmonyPatch("GetBiomeDB")]
        internal class AddOceanicBiomes
        {
            //AddOceanicBiomes
            internal static void Prefix(BiomeMap __instance)
            {
                if (QPatch.OceanMan2)
                    ManOceanGenerator.AddOceanicBiomes(__instance);
                else
                    ManOceanGenerator.RemoveOceanicBiomes(__instance);
            }
        }
    }
}