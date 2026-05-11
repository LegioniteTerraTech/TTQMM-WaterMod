using System;
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
            if (inst != null)
                return;
            var startup = new GameObject("RemoveScenery");
            inst = startup.AddComponent<RemoveScenery>();
            DebugWater.Log("WaterMod: WaterMod.RemoveScenery - Initated!");
            ManWorld.inst.TileManager.TileLoadedEvent.Subscribe(RemoveTrees);
        }

        private static List<ResourceDispenser> toRemove = new List<ResourceDispenser>();
        private static void RemoveTrees(WorldTile tile)
        {   // 
            //int removed = 0;
            try
            {
                if (WaterGlobals.DestroyTreesInWater && !WaterGlobals.OceanMan2 && 
                    (ManNetwork.IsHost || !ManNetwork.IsNetworked) && tile?.Visibles != null)
                {
                    foreach (var pair in tile.Visibles[(int)ObjectTypes.Scenery])
                    {
                        Visible vis = pair.Value;
                        try
                        {
                            if (vis?.resdisp != null && vis.centrePosition.y < QPatch.WaterHeight)
                            {
                                switch (vis.resdisp.GetSceneryType())
                                {   // lets see here, we remove trees that which exists
                                    case SceneryTypes.ConeTree:
                                    case SceneryTypes.DesertTree:
                                    case SceneryTypes.MountainTree:
                                    case SceneryTypes.ShroomTree:
                                    case SceneryTypes.DeadTree:
                                        toRemove.Add(vis.resdisp);
                                        //removed++;
                                        break;
                                }
                            }
                        }
                        catch { }
                    }
                    //Debug.Log("Water Mod: removed " + removed + " trees from under water");
                }
                foreach (var resdisp in toRemove)
                {
                    try
                    {
                        resdisp?.RemoveFromWorld(false, PermRemove, true, true);
                    }
                    catch { }
                }
            }
            finally
            {
                toRemove.Clear();
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
