using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace WaterMod.PatchBatch
{
    internal class GlobalPatches
    {
        private static class ManLooseBlocksPatches
        {
            internal static Type target = typeof(ManLooseBlocks);
            /// <summary> TankBlockSpawned </summary>
            [HarmonyPriority(-69)]
            internal static void DoSpawnTankBlock_Postfix(ref TankBlock __result)
            {
                if (__result != null)
                    WaterBlock.Insure(__result);
            }
            /// <summary> PatchResourceSpawn </summary>
            [HarmonyPriority(-69)]
            internal static void DoSpawnResourcePickup_Postfix(ResourcePickup __result)
            {
                if (__result != null)
                    WaterObj.Insure(__result).Reset();
            }
        }

        private static class TankPatches
        {
            internal static Type target = typeof(Tank);
            /// <summary> AbsoluteAssertWaterTank </summary>
            [HarmonyPriority(-69)]
            internal static void OnSpawn_Postfix(Tank __instance)
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
        private static class TankBeamPatches
        {
            internal static Type target = typeof(TankBeam);
            /// <summary> TankInsureBBeamUnderwater </summary>
            [HarmonyPriority(-69)]
            internal static void SetHoverBase_Postfix(TankBeam __instance, ref Vector3 ___m_HoverBase)
            {
                if (___m_HoverBase.y < ManWater.HeightCalc)
                    ___m_HoverBase.y = ManWater.HeightCalc;
            }

            private static Dictionary<Tank, ForceGizmo> CoBForceGizmos = new Dictionary<Tank, ForceGizmo>();
            /// <summary> AddNewGizmoTank </summary>
            [HarmonyPriority(-69)]
            internal static void SetForceGizmosActive_Prefix(TankBeam __instance, bool state, Tank ___tank, bool ___m_ForceGizmosActive)
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
            /// <summary> AddNewGizmoTank2 </summary>
            [HarmonyPriority(-69)]
            internal static void UpdateForceGizmos_Prefix(TankBeam __instance, Tank ___tank, bool ___m_ForceGizmosActive)
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

        private static class TankBlockPatches
        {
            internal static Type target = typeof(TankBlock);
            /// <summary> TankBlockRecycle </summary>
            [HarmonyPriority(-69)]
            internal static void OnRecycle_Postfix(TankBlock __instance)
            {
                if (__instance != null)
                    WaterBlock.Insure(__instance).TryRemoveSurface();
            }
        }

        private static class ModuleAnchorPatches
        {
            internal static Type target = typeof(ModuleAnchor);
            /// <summary> AllowAnchorInWater </summary>
            [HarmonyPriority(-69)]
            internal static bool IsColliderBlockingAnchor_Prefix(ModuleAnchor __instance, ref Collider col, ref bool __result)
            {
                if (col.gameObject.layer == ManWater.WaterLayer || col.gameObject.GetComponent<ManWater>())
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }


        private static class ProjectilePatches
        {
            internal static Type target = typeof(Projectile);
            /// <summary> PatchProjectileSpawn </summary>
            [HarmonyPriority(-69)]
            internal static void Fire_Postfix(Projectile __instance)
            {
                WaterObj.Insure(__instance).Reset();
            }
        }
        private static class MissileProjectilePatches
        {
            internal static Type target = typeof(MissileProjectile);
            /// <summary> PatchMissile </summary>
            [HarmonyPriority(-69)]
            internal static void Fire_Postfix(MissileProjectile __instance)
            {
                WaterObj.Insure(__instance).Reset();
            }
        }
        private static class LaserProjectilePatches
        {
            internal static Type target = typeof(LaserProjectile);
            /// <summary> PatchLaser </summary>
            [HarmonyPriority(-69)]
            internal static void Fire_Postfix(LaserProjectile __instance)
            {
                WaterObj.Insure(__instance).Reset();
            }
        }
    }
}
