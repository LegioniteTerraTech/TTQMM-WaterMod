using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace WaterMod
{
    public class LavaMode : MonoBehaviour
    {
        public static LavaMode inst;
        internal static GameObject warner;

        public static int DamageUpdateDelay = 30;
        public static float MeltBlocksStrength = 25;
        public static bool DealPainThisFrame = false;

        private static bool WarnLavaMP = false;

        public static void Initiate()
        {
            var startup = new GameObject("EvilLavaBeast");
            startup.AddComponent<LavaMode>();
            inst = startup.GetComponent<LavaMode>();
            Debug.Log("WaterMod: LavaMode - Initated!");
            warner = new GameObject();
            warner.AddComponent<LavaDeathWarningGUI>();
            warner.gameObject.SetActive(false);
        }

        public static void ThrowLavaDeathWarning()
        {
            if (QPatch.WantsLava && !QPatch.TheWaterIsLava)
            {
                WarnLavaMP = ManNetwork.inst.IsMultiplayer() && !ManNetwork.IsHost;
                Singleton.Manager<ManSFX>.inst.PlayUISFX(ManSFX.UISfxType.MissionFailed);
                warner.SetActive(true);
                if (WarnLavaMP)
                {
                    QPatch.WantsLava = false;
                }
            }
            else
            {
                if (!QPatch.WantsLava)
                    QPatch.theWaterIsLava = false;
                try
                {
                    if (ManNetwork.inst.IsMultiplayer() && ManNetwork.IsHost)
                    {
                        if (NetworkHandler.ServerLava != QPatch.theWaterIsLava)
                            NetworkHandler.ServerLava = QPatch.theWaterIsLava;
                    }
                }
                catch { }
            }
        }
        public static void ScreamLava()
        {
            Singleton.Manager<ManSFX>.inst.PlayMiscLoopingSFX(ManSFX.MiscSfxType.CabDetachKlaxon);
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
                    Window = GUI.Window(29587435, Window, GUIWindow, "Enable Hazardous Lava");
                }
                catch { }
            }

            private void GUIWindow(int ID)
            {
                if (!WarnLavaMP)
                {
                    GUILayout.Label("<b>--------Warning--------</b>");
                    GUILayout.Label("<b><color=#f23d3dff>>  THIS WILL MELT TECHS  <</color></b>");
                    if (GUI.Button(new Rect(20, 75, 80, 60), "Stay Safe"))
                    {
                        Singleton.Manager<ManSFX>.inst.PlayUISFX(ManSFX.UISfxType.Back);
                        QPatch.WantsLava = false;
                        gameObject.SetActive(false);
                        try
                        {
                            QPatch.makeDeath.Value = false;
                        }
                        catch { }
                    }
                    if (GUI.Button(new Rect(100, 75, 80, 60), "<color=#f23d3dff>All Must\nPerish</color>"))
                    {
                        Singleton.Manager<ManSFX>.inst.PlayUISFX(ManSFX.UISfxType.Enter);
                        ScreamLava();
                        QPatch.theWaterIsLava = true;
                        try
                        {
                            if (ManNetwork.inst.IsMultiplayer() && ManNetwork.IsHost)
                            {
                                if (NetworkHandler.ServerLava != QPatch.theWaterIsLava)
                                    NetworkHandler.ServerLava = QPatch.theWaterIsLava;
                            }
                        }
                        catch { }
                        WaterBuoyancy.UpdateLook(WaterBuoyancy.waterLooks[WaterBuoyancy.SelectedLook]);
                        gameObject.SetActive(false);
                    }
                }
                else
                {
                    GUILayout.Label("<b>  Only the server host  </b>");
                    GUILayout.Label("<b>    can summon lava.    </b>");
                    if (GUI.Button(new Rect(60, 75, 80, 60), "<b>Okay</b>"))
                    {
                        Singleton.Manager<ManSFX>.inst.PlayUISFX(ManSFX.UISfxType.InfoClose);
                        QPatch.WantsLava = false;
                        gameObject.SetActive(false);
                        try
                        {
                            QPatch.makeDeath.Value = false;
                        }
                        catch { }
                    }
                }
            }
        }
    }
}
