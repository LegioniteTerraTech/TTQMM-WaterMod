using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TerraTechETCUtil;
using UnityEngine;

namespace WaterMod
{
    public class LavaMode : MonoBehaviour
    {
        public static LavaMode inst;
        internal static GameObject warner;

        public static float DamageUpdateDelay = 1.5f;
        public static float MeltBlocksStrength = 25;
        public static bool DealPainThisFrame = false;

        private static bool WarnLavaMP = false;

        public static void Initiate()
        {
            var startup = new GameObject("EvilLavaBeast");
            startup.AddComponent<LavaMode>();
            inst = startup.GetComponent<LavaMode>();
            DebugWater.Log("WaterMod: LavaMode - Initated!");
            warner = new GameObject();
            warner.AddComponent<LavaDeathWarningGUI>();
            warner.gameObject.SetActive(false);
        }

        public static void ThrowLavaDeathWarning()
        {
            if (WaterGlobals.WantsLava && !QPatch.TheWaterIsLava)
            {
                WarnLavaMP = ManNetwork.inst.IsMultiplayer() && !ManNetwork.IsHost;
                Singleton.Manager<ManSFX>.inst.PlayUISFX(ManSFX.UISfxType.MissionFailed);
                warner.SetActive(true);
                if (WarnLavaMP)
                {
                    WaterGlobals.WantsLava = false;
                }
            }
            else
            {
                if (!WaterGlobals.WantsLava)
                    QPatch.theWaterIsLava = false;
                ManWater.UpdateNetworkedWaterIfNeeded();
            }
        }
        public static void ScreamLava()
        {
            Singleton.Manager<ManSFX>.inst.PlayMiscLoopingSFX(ManSFX.MiscSfxType.CabDetachKlaxon);
            UIHelpersExt.BigF5broningBannerMP("LAVA HAS BEEN ACTIVATED", false);
            inst.Invoke("ShutTheFrontDoor", 2.6f);
        }
        public void ShutTheFrontDoor()
        {
            Singleton.Manager<ManSFX>.inst.StopMiscLoopingSFX(ManSFX.MiscSfxType.CabDetachKlaxon);
        }
        internal class LavaDeathWarningGUI : MonoBehaviour
        {
            private Rect Window = new Rect(Display.main.renderingWidth / 2 - 100, (Display.main.renderingHeight - 75) / 2, 200, 155);

            private void OnGUI()
            {
                try
                {
                    Window = new Rect(Display.main.renderingWidth / 2 - 100, (Display.main.renderingHeight - 75) / 2, 200, 155);
                    Window = TerraTechETCUtil.AltUI.Window(29587435, Window, GUIWindow, "Enable Hazardous Lava");
                }
                catch { }
            }

            private void GUIWindow(int ID)
            {
                if (!WarnLavaMP)
                {
                    GUILayout.Label("<b>--------Warning--------</b>");
                    GUILayout.Label("<b><color=#f23d3dff>>  THIS WILL MELT TECHS  <</color></b>");
                    if (GUILayout.Button("Stay Safe", TerraTechETCUtil.AltUI.ButtonGreen))
                    {
                        Singleton.Manager<ManSFX>.inst.PlayUISFX(ManSFX.UISfxType.Back);
                        WaterGlobals.WantsLava = false;
                        gameObject.SetActive(false);
                        try
                        {
                            ManWater._inst.SetLavaValue(false);
                        }
                        catch { }
                    }
                    if (GUILayout.Button("<b>All Must\nPerish</b>", AltUI.ButtonRed))
                    {
                        Singleton.Manager<ManSFX>.inst.PlayUISFX(ManSFX.UISfxType.Enter);
                        ScreamLava();
                        QPatch.theWaterIsLava = true;
                        ManWater.UpdateNetworkedWaterIfNeeded();
                        ManWater.UpdateLook();
                        SurfacePool.UpdateAllActiveParticles();
                        try
                        {
                            ManWater._inst.Save();
                        }
                        catch { }
                        gameObject.SetActive(false);
                    }
                }
                else
                {
                    GUILayout.Label("<b>  Only the server host  </b>");
                    GUILayout.Label("<b>    can summon lava.    </b>");
                    if (GUILayout.Button("<b>Okay</b>"))
                    {
                        Singleton.Manager<ManSFX>.inst.PlayUISFX(ManSFX.UISfxType.InfoClose);
                        WaterGlobals.WantsLava = false;
                        gameObject.SetActive(false);
                        try
                        {
                            ManWater._inst.SetLavaValue(false);
                        }
                        catch { }
                    }
                }
            }
        }
    }
}
