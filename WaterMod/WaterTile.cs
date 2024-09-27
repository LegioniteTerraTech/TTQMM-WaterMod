using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace WaterMod
{
    internal class WaterTile
    {
        internal static float SubTileScale = ManWorld.inst.TileSize / 2;
        internal static float DistFromCamToLOD0 = ManWorld.inst.TileSize;
        private static Queue<WaterTile> pool = new Queue<WaterTile>();
        private static SubTile waterTilePrefab;


        private SubTile tilesPP;
        private SubTile tilesNP;
        private SubTile tilesPN;
        private SubTile tilesNN;
        internal SubTile this[int val]
        {
            get
            {
                switch (val)
                {
                    case 0:
                        return tilesPP;
                    case 1:
                        return tilesNP;
                    case 2:
                        return tilesPN;
                    case 3:
                        return tilesNN;
                    default:
                        throw new IndexOutOfRangeException("val");
                }
            }
            set
            {
                switch (val)
                {
                    case 0:
                        tilesPP = value;
                        break;
                    case 1:
                        tilesNP = value;
                        break;
                    case 2:
                        tilesPN = value;
                        break;
                    case 3:
                        tilesNN = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException("val");
                }
            }
        }
        internal SubTile this[int valX, int valY]
        {
            get
            {
                switch (valX)
                {
                    case 0:
                        switch (valY)
                        {
                            case 0:
                                return tilesPP;
                            case 1:
                                return tilesPN;
                            default:
                                throw new IndexOutOfRangeException("valY");
                        }
                    case 1:
                        switch (valY)
                        {
                            case 0:
                                return tilesNP;
                            case 1:
                                return tilesNN;
                            default:
                                throw new IndexOutOfRangeException("valY");
                        }
                    default:
                        throw new IndexOutOfRangeException("valX");
                }
            }
            set
            {
                switch (valX)
                {
                    case 0:
                        switch (valY)
                        {
                            case 0:
                                tilesPP = value;
                                break;
                            case 1:
                                tilesPN = value;
                                break;
                            default:
                                throw new IndexOutOfRangeException("valY");
                        }
                        break;
                    case 1:
                        switch (valY)
                        {
                            case 0:
                                tilesNP = value;
                                break;
                            case 1:
                                tilesNN = value;
                                break;
                            default:
                                throw new IndexOutOfRangeException("valY");
                        }
                        break;
                    default:
                        throw new IndexOutOfRangeException("valX");
                }
            }
        }
        internal int Length = 4;
        public IEnumerator<SubTile> GetEnumerator()
        {
            yield return tilesPP;
            yield return tilesNP;
            yield return tilesPN;
            yield return tilesNN;
        }


        public static WaterTile CreateWaterTile(IntVector2 tilePos)
        {
            if (waterTilePrefab == null)
            {
                waterTilePrefab = new GameObject("waterSubTile").AddComponent<SubTile>();
                var MC = waterTilePrefab.GetComponent<MeshCollider>();
                if (MC)
                    UnityEngine.Object.Destroy(MC);
                waterTilePrefab.gameObject.AddComponent<MeshRenderer>();
                waterTilePrefab.gameObject.AddComponent<MeshFilter>();
                //Let's make Renderer do it's job
                //  We will re-prioritize it's graphics to work like intended
                Renderer render = waterTilePrefab.GetComponent<Renderer>();
                render.sortingOrder = -1;//Make the water appear correctly
                render.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                render.allowOcclusionWhenDynamic = false;
                //render.realtimeLightmapIndex = false;
                render.motionVectorGenerationMode =  MotionVectorGenerationMode.Camera;
                //render.allowOcclusionWhenDynamic = false;//
                render.receiveShadows = false;//
                render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;//
                render.rendererPriority = -2000;//
                //render.sortingLayerID = 0;//
                //render.sortingLayerName = "Default";//
                //render.rendererPriority = 0;//
                //render.renderingLayerMask = 1;//

                var trans = waterTilePrefab.transform;
                trans.rotation = Quaternion.identity;
                trans.localScale = new Vector3(ManWorld.inst.TileSize / 20, 0.075f, ManWorld.inst.TileSize / 20);

                waterTilePrefab.CreatePool(64);
                waterTilePrefab.gameObject.SetActive(false);
            }
            WaterTile WT;
            if (pool.Any())
            {
                WT =  pool.Dequeue();
                WT.Init(tilePos);
                return WT;
            }

            WT = new WaterTile();
            WT.Init(tilePos);
            return WT;
        }


        private WaterTile Init(IntVector2 tilePos)
        {
            Vector3 pos = ManWorld.inst.TileManager.CalcTileCentreScene(tilePos).SetY(ManWater.HeightCalc);
            tilesPP = waterTilePrefab.Spawn(pos + new Vector3(-SubTileScale / 2, 0, -SubTileScale / 2), Quaternion.identity).Init();
            tilesNP = waterTilePrefab.Spawn(pos + new Vector3(SubTileScale / 2, 0, -SubTileScale / 2), Quaternion.identity).Init();
            tilesPN = waterTilePrefab.Spawn(pos + new Vector3(-SubTileScale / 2, 0, SubTileScale / 2), Quaternion.identity).Init();
            tilesNN = waterTilePrefab.Spawn(pos + new Vector3(SubTileScale / 2, 0, SubTileScale / 2), Quaternion.identity).Init();

            /*
            foreach (var tile in this)
                tile.Init();
            */
            return this;
        }
        internal void Recycle()
        {
            for (int i = 0; i < Length; i++)
            {
                this[i].Recycle();
                this[i] = null;
            }
            pool.Enqueue(this);
        }

        internal void UpdateTileHeight(float height)
        {
            foreach (var tile in this)
                tile.UpdateTileHeight(height);
        }
        internal void UpdateTileTreadmill(IntVector3 vec)
        {
            foreach (var tile in this)
                tile.UpdateTileTreadmill(vec);
        }
        internal void UpdateLook()
        {
            foreach (var tile in this)
                tile.UpdateLook();
        }
        internal class SubTile : MonoBehaviour
        {
            private Transform trans;
            private Renderer rend;
            private MeshFilter mesher;

            internal bool IsClose = false;
            internal SubTile Init()
            {
                if (trans != null)
                {
                    UpdateLook();
                    return this;
                }
                trans = transform;
                rend = GetComponent<Renderer>();
                mesher = GetComponent<MeshFilter>();
                UpdateLook();
                return this;
            }
            internal void UpdateTileHeight(float height)
            {
                trans.position = trans.position.SetY(height);
            }
            internal void UpdateTileTreadmill(IntVector3 vec)
            {
                trans.position += vec;
            }
            internal void UpdateLook()
            {
                ManWater.WaterLook look;
                if (ManWater.IsActive)
                {
                    if (!rend.enabled)
                        rend.enabled = true;
                    IsClose = Singleton.playerPos.Approximately(trans.position, DistFromCamToLOD0);
                    if (IsClose && !ManWater.CameraSubmerged)
                        look = ManWater.waterLooks[ManWater.SelectedLook];
                    else
                    {
                        int fetched = ManWater.waterLooks[ManWater.SelectedLook].lowResFallback;
                        look = ManWater.waterLooks[fetched];
                    }
                    if (QPatch.TheWaterIsLava)
                        rend.material = look.materialLava;
                    else
                        rend.material = look.material;
                    mesher.mesh = look.mesh;
                    trans.localScale = new Vector3(ManWorld.inst.TileSize * look.scale, 0.075f, ManWorld.inst.TileSize * look.scale);
                }
                else
                {
                    if (rend.enabled)
                        rend.enabled = false;
                }
            }
        }
    }
}
