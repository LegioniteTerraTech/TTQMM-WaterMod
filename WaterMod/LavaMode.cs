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

        public static int DamageUpdateDelay = 30;
        public static float MeltBlocksStrength = 25;
        public static bool DealPainThisFrame = false;

        public static void Initiate()
        {
            var startup = new GameObject("EvilLavaBeast");
            startup.AddComponent<LavaMode>();
            inst = startup.GetComponent<LavaMode>();
            Debug.Log("Water Mod: LavaMode - Initated!");
        }
    }
}
