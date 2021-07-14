using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WaterMod
{
    public class ResSpawnOverride : MonoBehaviour
    {   //  Remove trees that are in the water
        //    May not be optimized, please let me know if it's laggy 
        //    and I will rebuild this on a less update-heavy arrangement 

        private static ResSpawnOverride inst;
        private static int clock = 0;

        public static void Initiate()
        {   // 
            var startup = new GameObject("ResSpawnOverride");
            startup.AddComponent<ResSpawnOverride>();
            inst = startup.GetComponent<ResSpawnOverride>();
            Debug.Log("Water Mod: ResSpawnOverride - Initated!");
        }

        private static void EradicateSelectRes()
        {   // 
            int removed = 0;
            foreach (Visible vis in Singleton.Manager<ManVisible>.inst.VisiblesTouchingRadius(Singleton.cameraTrans.position, 2500, new Bitfield<ObjectTypes>()))
            {
                if (vis.resdisp.IsNotNull() && vis.centrePosition.y < WaterMod.QPatch.WaterHeight)
                {
                    switch (vis.resdisp.GetSceneryType())
                    {   // lets see here, we remove trees that which exists
                        case SceneryTypes.ConeTree:
                        case SceneryTypes.DesertTree:
                        case SceneryTypes.MountainTree:
                        case SceneryTypes.ShroomTree:
                            vis.resdisp.RemoveFromWorld(false, true, true, true);
                            removed++;
                            break;
                    }
                }
            }
            //Debug.Log("Water Mod: removed " + removed);
        }

        public void Update()
        {   // 
            if (QPatch.DestroyTreesInWater)
            {
                if (clock > 100)
                {
                    EradicateSelectRes();
                    clock = 0;
                }
                clock++;
            }
        }
    }
}
