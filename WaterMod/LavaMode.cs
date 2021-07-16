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
                Singleton.Manager<ManSFX>.inst.PlayUISFX(ManSFX.UISfxType.Warning);
                warner.SetActive(true);
            }
            else
            {
                QPatch.TheWaterIsLava = false;
                try
                {
                    if (ManNetwork.inst.IsMultiplayer() && ManNetwork.IsHost)
                    {
                        if (NetworkHandler.ServerLava != QPatch.TheWaterIsLava)
                            NetworkHandler.ServerLava = QPatch.TheWaterIsLava;
                    }
                }
                catch { }
            }
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
                    QPatch.TheWaterIsLava = true;
                    try
                    {
                        if (ManNetwork.inst.IsMultiplayer() && ManNetwork.IsHost)
                        {
                            if (NetworkHandler.ServerLava != QPatch.TheWaterIsLava)
                                NetworkHandler.ServerLava = QPatch.TheWaterIsLava;
                        }
                    }
                    catch { }
                    WaterBuoyancy.UpdateLook(WaterBuoyancy.waterLooks[WaterBuoyancy.SelectedLook]);
                    gameObject.SetActive(false);
                }
            }
        }
    }
}
