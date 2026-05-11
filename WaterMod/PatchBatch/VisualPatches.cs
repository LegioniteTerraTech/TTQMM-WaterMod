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
    internal class VisualPatches
    {
        private static class TOD_SkyPatches
        {
            internal static Type target = typeof(TOD_Sky);
            /// <summary> DarknessEffect </summary>
            [HarmonyPriority(-69)]
            internal static void UpdateCelestials_Prefix()
            {
                if (ManWater.CameraSubmerged)
                    ManWater.UpdateDarkness();
            }
        }

        private static class ManTimeOfDayPatches
        {
            internal static Type target = typeof(ManTimeOfDay);

            private static FieldInfo cloudflare = typeof(ManTimeOfDay).GetField("m_TargetCloudParams", BindingFlags.Instance | BindingFlags.NonPublic);
            /// <summary> CloudsEffect </summary>
            [HarmonyPriority(-133700)]
            internal static void LerpCloudData_Prefix(ManTimeOfDay __instance, TOD_Sky ___m_Sky)
            {
                if (ManWater.CameraSubmerged)
                {
                    //TOD_CloudParameters cloudsInst = (TOD_CloudParameters)cloudflare.GetValue(__instance);
                    var cloudsInst = ___m_Sky.Clouds;
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

        private static class CameraManagerPatches
        {
            internal static Type target = typeof(CameraManager);
            /// <summary> EnforceSeaFogEffect </summary>
            [HarmonyPriority(-69)]
            internal static void UpdateGraphicOption_Prefix()
            {
                if (ManWater.UseDynamicFog)
                    ManWater.AdjustFog();
            }
        }
    }
}
