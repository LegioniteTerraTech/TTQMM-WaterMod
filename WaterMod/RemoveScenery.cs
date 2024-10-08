﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WaterMod
{
    public class RemoveScenery : MonoBehaviour
    {   //  Remove trees that are in the water
        //    May not be optimized, please let me know if it's laggy 
        //    and I will rebuild this on a less update-heavy arrangement 

        internal static bool PermRemove = true;
        internal static RemoveScenery inst;
        private static int clock = 0;

        public static void Initiate()
        {   // 
            var startup = new GameObject("RemoveScenery");
            startup.AddComponent<RemoveScenery>();
            inst = startup.GetComponent<RemoveScenery>();
            DebugWater.Log("WaterMod: ResSpawnOverride - Initated!");
        }
        public static void Sub()
        { 
            //Singleton.Manager<ManWorld>.inst.TileManager.TilePopulatedEvent.Subscribe(RemoveTrees);
        }

        public static void RemoveTrees(WorldTile tile)
        {   // 
            //int removed = 0;
            if (QPatch.DestroyTreesInWater && (ManNetwork.IsHost || !ManNetwork.IsNetworked) && tile.HasReachedLoadState(WorldTile.State.Populated))
            {
                foreach (var pair in tile.Visibles[3])
                {
                    Visible vis = pair.Value;
                    try
                    {
                        if (vis.resdisp.IsNotNull() && vis.centrePosition.y < QPatch.WaterHeight)
                        {
                            switch (vis.resdisp.GetSceneryType())
                            {   // lets see here, we remove trees that which exists
                                case SceneryTypes.ConeTree:
                                case SceneryTypes.DesertTree:
                                case SceneryTypes.MountainTree:
                                case SceneryTypes.ShroomTree:
                                case SceneryTypes.DeadTree:
                                    vis.resdisp.RemoveFromWorld(false, PermRemove, true, true);
                                    clock = 0;
                                    //removed++;
                                    break;
                            }
                        }
                    }
                    catch { }
                }
                //Debug.Log("Water Mod: removed " + removed + " trees from under water");
            }
        }

        /*
        private static void EradicateSelectRes()
        {   // 
            //int removed = 0;
            foreach (Visible vis in Singleton.Manager<ManVisible>.inst.VisiblesTouchingRadius(Singleton.cameraTrans.position, 500, new Bitfield<ObjectTypes>()))
            {
                try
                {
                    if (vis.resdisp.IsNotNull() && vis.centrePosition.y < QPatch.WaterHeight)
                    {
                        switch (vis.resdisp.GetSceneryType())
                        {   // lets see here, we remove trees that which exists
                            case SceneryTypes.ConeTree:
                            case SceneryTypes.DesertTree:
                            case SceneryTypes.MountainTree:
                            case SceneryTypes.ShroomTree:
                                vis.resdisp.RemoveFromWorld(false, PermRemove, true, true);
                                //removed++;
                                break;
                        }
                    }
                }
                catch { }
            }
            //Debug.Log("Water Mod: removed " + removed);
        }

        public void Update()
        {   // 
            if (QPatch.DestroyTreesInWater && (ManNetwork.IsHost || !ManNetwork.IsNetworked))
            {
                if (clock > 100)
                {
                    EradicateSelectRes();
                    clock = 0;
                }
                clock++;
            }
        }
        */
    }
}
