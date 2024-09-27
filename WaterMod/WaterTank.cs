using System;
using System.Reflection;
using System.Collections.Generic;
using TerraTechETCUtil;
using UnityEngine;
using Steamworks;

namespace WaterMod
{
    public class WaterTank : MonoBehaviour
    {
        public static HashSet<WaterTank> All = new HashSet<WaterTank>();
        public Tank tank;
        public Vector3 SubmergeAdditivePos = Vector3.zero;
        public float SubmergeAmount = 0;
        public Vector3 SurfaceAdditivePos = Vector3.zero;
        public int SurfaceCount = 0;
        public int SurfaceCountPrev = 0;
        private bool TouchWaterPrev = false;
        internal bool FloationMode = true;

        public List<ModuleLight> OnSplashdown = new List<ModuleLight>();

        public static WaterTank Insure(Tank tank)
        {
            WaterTank WT = tank.GetComponent<WaterTank>();
            if (WT)
                return WT;
            WT = tank.gameObject.AddComponent<WaterTank>();
            WT.Subscribe(tank);
            WT.OnSpawnRemote();
            return WT;
        }
        public AudioInst SplashSmall;
        public AudioInst SplashMedium;
        public AudioInst SplashLarge;
        public AudioInst WaterNoises;
        public float WaterNoisesPitch;
        private void Subscribe(Tank tank)
        {
            this.tank = tank;
            if (ManWater.SplashSmall != null)
            {
                SplashSmall = ManWater.SplashSmall.Copy();
                SplashSmall.PositionFunc = GetPos;
            }
            if (ManWater.SplashMedium != null)
            {
                SplashMedium = ManWater.SplashMedium.Copy();
                SplashMedium.PositionFunc = GetPos;
            }
            if (ManWater.SplashLarge != null)
            {
                SplashLarge = ManWater.SplashLarge.Copy();
                SplashLarge.PositionFunc = GetPos;
            }
            tank.control.driveControlEvent.Subscribe(OnDriveControl);
            DebugWater.Log("Registered a Tank");
        }
        private void OnDriveControl(TankControl.ControlState state)
        {
            if (state.InputMovement.y > 0.1f)
                FloationMode = true;
            else if (state.InputMovement.y < -0.1f)
                FloationMode = false;
        }
        public void OnSpawnRemote()
        {
            if (All.Add(this))
            {
                tank.TankRecycledEvent.Subscribe(OnRecycled);
                DebugWater.Log("Tank Spawned");
            }
        }
        public void OnRecycled(Tank tank)
        {
            if (All.Remove(this))
            {
                DebugWater.Log("Tank Recycled");
                tank.TankRecycledEvent.Unsubscribe(OnRecycled);
            }
        }
        private float lastTime = 0;
        private Vector3 posCached = default;
        public Vector3 GetPos()
        {
            if (lastTime < Time.time && tank.rbody)
            {
                lastTime = Time.time;
                posCached = tank.WorldCenterOfMass;
            }
            return posCached;
        }

        public void AddGeneralBuoyancy(Vector3 position)
        {
            SubmergeAdditivePos += position;
            SubmergeAmount++;//+= 0.9375f;
        }
        public void AddScaledBuoyancy(Vector3 position, float scale)
        {
            SubmergeAdditivePos += position * scale;
            SubmergeAmount += scale;
        }

        public void AddSurface(Vector3 position)
        {
            SurfaceAdditivePos += position;
            SurfaceCount++;
        }

        public void PlaySplashSFX()
        {
            Vector3 exts = tank.blockBounds.size;
            float extent = Mathf.Max(exts.x, exts.y, exts.z);
            if (extent < 6)
            {
                if (SplashSmall != null)
                {
                    SplashSmall.Volume = Mathf.Clamp01(Mathf.Abs(speedVol));
                    SplashSmall.Play();
                }
            }
            else if (extent < 18)
            {
                if (SplashMedium != null)
                {
                    SplashMedium.Volume = Mathf.Clamp01(Mathf.Abs(speedVol));
                    SplashMedium.Play();
                }
            }
            else
            {
                if (SplashLarge != null)
                {
                    SplashLarge.Volume = Mathf.Clamp01(Mathf.Abs(speedVol));
                    SplashLarge.Play();
                }
            }
        }
        public float speedAvg = 0;
        public float speedVol = 0;
        public void MaintainTraversalNoise()
        {
            AudioInst Audio;
            if (QPatch.OnlyPlayerWaterTraverseSFX)
            {
                if (!tank.PlayerFocused || ManWater.WaterNoises == null)
                    return;
                if (ManWater.WaterNoises.PositionFunc == null)
                {
                    ManWater.WaterNoises.PositionFunc = GetPos;
                    ManWater.WaterNoises.Volume = 0.2f;
                    return;
                }
                Audio = ManWater.WaterNoises;
            }
            else
            {
                if (WaterNoises == null)
                {
                    if (ManWater.WaterNoises == null)
                        return;
                    WaterNoises = ManWater.WaterNoises.Copy();
                    WaterNoises.PositionFunc = GetPos;
                    WaterNoises.Volume = 0.2f;
                    return;
                }
                Audio = WaterNoises;
            }
            if (tank.rbody)
                speedAvg = Mathf.Max(Mathf.Abs(tank.rbody.velocity.x), Mathf.Abs(tank.rbody.velocity.y), Mathf.Abs(tank.rbody.velocity.z));
            else
                speedAvg = 0f;
            bool doPlay = SubmergeAmount > 0 && speedAvg > 3f && !ManPauseGame.inst.IsPaused;
            if (doPlay)
            {
                Audio.Resume();
                if (!Audio.IsPlaying)
                    Audio.PlayFromBeginning();
                Vector3 exts = tank.blockBounds.size;
                float extentFactor = Mathf.Max(exts.x, exts.y, exts.z) + 10;
                speedVol = tank.rbody.velocity.magnitude * 0.025f;
                Audio.Volume = ManWater.WaterSound * Mathf.Clamp(speedVol * extentFactor * 0.0176f, 0.01f, 0.5f) * (ManWater.CameraSubmerged ? 0.76f : 1);
                Audio.Pitch = Mathf.Clamp(0.1f + speedVol * 0.5f, 0.1f, 1.0f);
                Audio.Range = Mathf.Clamp(10f + speedVol * extentFactor * 5f, 10f, 120f);
            }
            else
            {
                if (SubmergeAmount > 0)
                    tank.beam.SetHoverBase();
                if (!Audio.IsPaused)
                    Audio.Pause();
            }
        }
        private static object[] nothing = new object[0];
        private static MethodInfo lightCheck = typeof(ModuleLight).GetMethod("RefreshLightsActive", BindingFlags.Instance | BindingFlags.NonPublic);
        public void RemoteFixedUpdate()
        {
            if (ManWater.WorldMove || tank.rbody == null)
                return; // the world is treadmilling and we must ignore the delayed physics update to prevent fling
            foreach (var ite in tank.blockman.IterateBlocks())
            {
                try
                {
                    WaterBlock item = WaterBlock.Insure(ite);
                    float heightDelta = item.transform.TransformPoint(item.TankBlock.CentreOfMass).y - ManWater.HeightCalc;
                    if (heightDelta > item.radius)
                    {
                        item.UpdateAttached(SubState.Above);
                    }
                    else if (heightDelta > -item.radius)
                    {
                        item.UpdateAttached(SubState.Float);
                        item.ApplyConnectedForce();
                    }
                    else
                    {
                        item.UpdateAttached(SubState.Below);
                        item.ApplyConnectedForceFullySubmerged();
                    }
                }
                catch (Exception e){ DebugWater.Log(e); }
            }
            MaintainTraversalNoise();
            int bCount = tank.blockman.blockCount;
            if (bCount == 0)
                return;
            Vector3 Velo = tank.rbody.velocity;
            // Vector3 Velo = tank.rbody.velocity - (Physics.gravity * Time.fixedDeltaTime);
            bool touchWater = SubmergeAmount > 0 || SurfaceCount > 0;
            if (touchWater != TouchWaterPrev)
            {
                TouchWaterPrev = touchWater;
                PlaySplashSFX();
            }

            if (SubmergeAmount > 0)
            {
                Vector3 ForceCenter = SubmergeAdditivePos / SubmergeAmount;
                Vector3 ForceLift = Vector3.up * (WaterGlobals.Density * 7.5f) * SubmergeAmount;
                tank.rbody.AddForceAtPosition(ForceLift, ForceCenter);
                SubmergeAdditivePos = Vector3.zero;
                /*
                Vector3 ForceAppF;
                if (QPatch.TheWaterIsLava)
                {
                    ForceAppF = -(Velo * ManWater.ApplyLava(WaterGlobals.SubmergedTankDampening) + (Vector3.up *
                        (Velo.y * ManWater.ApplyLava(WaterGlobals.SubmergedTankDampeningYAddition)))) * (SubmergeAmount / bCount);
                }
                else
                {
                    ForceAppF = -(Velo * WaterGlobals.SubmergedTankDampening + (Vector3.up *
                        (Velo.y * WaterGlobals.SubmergedTankDampeningYAddition))) * (SubmergeAmount / bCount);
                }
                tank.rbody.AddForceAtPosition(ForceAppF, ForceCenter, ForceMode.Acceleration);
                */
                Vector3 inertia = Velo * tank.rbody.mass;
                Quaternion projection = Quaternion.LookRotation(Velo.normalized);
                Quaternion projectionOrigin = Quaternion.Inverse(projection);
                Vector3 force;
                if (QPatch.TheWaterIsLava)
                {
                    force = -(Velo * WaterGlobals.SubmergedTankDampening * 3 + (Vector3.up *
                        (Velo.y * WaterGlobals.SubmergedTankDampeningYAddition))) * SubmergeAmount;
                }
                else
                {
                    force = -(Velo * WaterGlobals.SubmergedTankDampening + (Vector3.up *
                        (Velo.y * WaterGlobals.SubmergedTankDampeningYAddition))) * SubmergeAmount;
                }
                Vector3 forceAligned = projectionOrigin * force;
                Vector3 inertiaAligned = projectionOrigin * inertia;
                if (forceAligned.z + inertiaAligned.z < 0)
                {
                    forceAligned.z = -inertiaAligned.z;
                    force = projection * forceAligned;
                }
                tank.rbody.AddForce(force, ForceMode.Force);
                //tank.rbody.AddForceAtPosition(force, ForceCenter, ForceMode.Force);
                SubmergeAmount = 0;
            }
            if (SurfaceCount != 0)
            {
                if (SurfaceCountPrev == 0)
                {
                    foreach (var item in OnSplashdown)
                        lightCheck.Invoke(item, nothing);
                }
                SurfaceCountPrev = SurfaceCount;
                /*
                Vector3 ForceAppF;
                if (QPatch.TheWaterIsLava)
                {
                    ForceAppF = -(Velo * ManWater.ApplyLava(WaterGlobals.SurfaceTankDampening) + (Vector3.up *
                        (Velo.y * ManWater.ApplyLava(WaterGlobals.SurfaceTankDampeningYAddition)))) * (SurfaceCount / bCount);
                }
                else
                {
                    ForceAppF = -(Velo * WaterGlobals.SurfaceTankDampening + (Vector3.up *
                        (Velo.y * WaterGlobals.SurfaceTankDampeningYAddition))) * (SurfaceCount / bCount);
                }
                tank.rbody.AddForceAtPosition(ForceAppF, SurfaceAdditivePos / SurfaceCount, ForceMode.Acceleration);
                */
                Vector3 force;
                Vector3 forceOrigin = SurfaceAdditivePos / SurfaceCount;
                if (QPatch.TheWaterIsLava)
                    force = -(Velo * WaterGlobals.SurfaceTankDampening * 3 + (Vector3.up *
                        (Velo.y * WaterGlobals.SurfaceTankDampeningYAddition))) * SurfaceCount;
                else
                    force = -(Velo * WaterGlobals.SurfaceTankDampening + (Vector3.up *
                        (Velo.y * WaterGlobals.SurfaceTankDampeningYAddition))) * SurfaceCount;
                /*
                Velo = tank.rbody.GetPointVelocity(forceOrigin);
                Vector3 inertia = Velo * tank.rbody.mass;
                Quaternion projection = Quaternion.LookRotation(Velo.normalized);
                Quaternion projectionOrigin = Quaternion.Inverse(projection);
                Vector3 forceAligned = projectionOrigin * force;
                Vector3 inertiaAligned = projectionOrigin * inertia;
                if (forceAligned.z + inertiaAligned.z < 0)
                {
                    forceAligned.z = -inertiaAligned.z;
                    force = projection * forceAligned;
                }
                */
                tank.rbody.AddForceAtPosition(force, forceOrigin);


                SurfaceAdditivePos = Vector3.zero;
                SurfaceCount = 0;
            }
            else
            {
                if (SurfaceCountPrev != 0)
                {
                    foreach (var item in OnSplashdown)
                        lightCheck.Invoke(item, nothing);
                }
                SurfaceCountPrev = 0;
            }
        }

        public static void UpdateAllReversed()
        {
            foreach (Tank tank in Singleton.Manager<ManTechs>.inst.CurrentTechs)
            {
                try
                {
                    if (!tank.FirstUpdateAfterSpawn)
                        WaterTank.Insure(tank).InvertedUpdate();
                }
                catch { }
            }
        }

        /// <summary>
        /// Compensate for WorldTreadmill
        /// </summary>
        public void InvertedUpdate()
        {
            if (SubmergeAmount != 0)
            {
                tank.rbody.AddForceAtPosition(Vector3.down * (WaterGlobals.Density * 7.5f) * SubmergeAmount, SubmergeAdditivePos / SubmergeAmount);
                SubmergeAdditivePos = Vector3.zero;
                if (QPatch.TheWaterIsLava)
                    tank.rbody.AddForce(-(tank.rbody.velocity * WaterGlobals.SubmergedTankDampening * WaterGlobals.LavaDampenMulti + (Vector3.down * (tank.rbody.velocity.y * WaterGlobals.SubmergedTankDampeningYAddition))) * SubmergeAmount, ForceMode.Force);
                else
                    tank.rbody.AddForce(-(tank.rbody.velocity * WaterGlobals.SubmergedTankDampening + (Vector3.down * (tank.rbody.velocity.y * WaterGlobals.SubmergedTankDampeningYAddition))) * SubmergeAmount, ForceMode.Force);
                SubmergeAmount = 0;
            }
            if (SurfaceCount != 0)
            {
                if (QPatch.TheWaterIsLava)
                    tank.rbody.AddForceAtPosition(-(tank.rbody.velocity * WaterGlobals.SurfaceTankDampening * WaterGlobals.LavaDampenMulti + (Vector3.down * (tank.rbody.velocity.y * WaterGlobals.SurfaceTankDampeningYAddition))) * SurfaceCount, SurfaceAdditivePos / SurfaceCount);
                else
                    tank.rbody.AddForceAtPosition(-(tank.rbody.velocity * WaterGlobals.SurfaceTankDampening + (Vector3.down * (tank.rbody.velocity.y * WaterGlobals.SurfaceTankDampeningYAddition))) * SurfaceCount, SurfaceAdditivePos / SurfaceCount);
                SurfaceAdditivePos = Vector3.zero;
                SurfaceCount = 0;
            }
        }


        /*
        private int allBlocksBelow = 0;
        // Get ALL blocks below water level
        public int UpdateBlocksBelow()
        {
            int allBlocksBelow = 0;

            BlockManager.TableCache TC = tank.blockman.GetTableCacheForPlacementCollection();
            List<IntVector3> allBlockPos = new List<IntVector3>();
            float upperRange = WaterBuoyancy.HeightCalc + 0.7071f;
            float lowerRange = WaterBuoyancy.HeightCalc - 0.7071f;
            List<TankBlock> blocksAtWaterline = new List<TankBlock>();

            for (int stepX = 0; stepX < TC.size; stepX++)
            {
                for (int stepZ = 0; stepZ < TC.size; stepZ++)
                {
                    for (int stepZ = 0; stepZ < TC.size; stepZ++)
                    {
                        Vector3 worldPos = transform.TransformPoint(new Vector3(stepX, stepY, stepZ));
                        if (worldPos.y > upperRange ||
                            !TC.blockTable[stepX, stepY, stepZ])
                        {
                            continue;
                        }
                        else if (worldPos.y < lowerRange)
                        {
                            allBlocksBelow++;
                        }
                        else
                        {
                            TankBlock.
                            public void ApplyConnectedForce(bool invert = false)
                            blocksAtWaterline++;
                        }
                    }
                }
            }
            foreach (TankBlock TB in TankBlock)
            transform.up;
            WaterBuoyancy.HeightCalc
        }
        private int RunWithinWater(BlockManager.TableCache TC, Vector3 pos)
        {
            int octPartition = 2;
            int blocksBelowWaterline = 0;
            if (BranchSize == 1)
            {
                Vector3 worldPos = transform.TransformPoint(new Vector3(stepX, stepY, stepZ));
                if (worldPos.y > upperRange ||
                    !TC.blockTable[stepX, stepY, stepZ])
                {
                    continue;
                }
                else if (worldPos.y < lowerRange)
                {
                    blocksBelowWaterline++;
                }
                else
                {
                    TankBlock.
                            public void ApplyConnectedForce(bool invert = false)
                            blocksAtWaterline++;
                }
            }

            List<IntVector3> allBlockPos = new List<IntVector3>();
            float distDev = (float)TC.size * 1.732f;
            float upperRange = WaterBuoyancy.HeightCalc + distDev;
            float lowerRange = WaterBuoyancy.HeightCalc - distDev;
            List<TankBlock> blocksAtWaterline = new List<TankBlock>();
            Vector3 worldPos = pos;
            if (worldPos < )
            {
            }
            else
            {   // We are not in water 
            }
        }
        private List<TankBlock> RunWithinWaterBranch(BlockManager.TableCache TC, Vector3 pos, int BranchSize, int partitions)
        {
            int blocksBelowWaterline = 0;
            if (BranchSize < partitions)
            {
                Vector3 worldPos = transform.TransformPoint(new Vector3(stepX, stepY, stepZ));
                if (worldPos.y > upperRange ||
                    !TC.blockTable[stepX, stepY, stepZ])
                {
                    continue;
                }
                else if (worldPos.y < lowerRange)
                {
                    blocksBelowWaterline++;
                }
                else
                {
                    TankBlock.
                            public void ApplyConnectedForce(bool invert = false)
                            blocksAtWaterline++;
                }
            }
            List<IntVector3> allBlockPos = new List<IntVector3>();
            float distDev = (float)TC.size * 1.732f;
            float upperRange = WaterBuoyancy.HeightCalc + distDev;
            float lowerRange = WaterBuoyancy.HeightCalc - distDev;
            List<TankBlock> blocksAtWaterline = new List<TankBlock>();
            float partitionOffset = (TC.size / partitions) / 2;
            float partitionSpacing = BranchSize / partitions;

            for (int stepX = 0; stepX < partitions; stepX++)
            {
                for (int stepY = 0; stepY < partitions; stepY++)
                {
                    for (int stepZ = 0; stepZ < partitions; stepZ++)
                    {
                        float localPosX = partitionOffset + (partitionSpacing * stepX);
                        float localPosY = partitionOffset + (partitionSpacing * stepY);
                        float localPosZ = partitionOffset + (partitionSpacing * stepZ);
                        Vector3 worldPos = transform.TransformPoint(new Vector3(localPosX, localPosY, localPosZ));
                        if (worldPos.y > upperRange ||
                            !TC.blockTable[stepX, stepY, stepZ])
                        {
                            continue;
                        }
                        else if (worldPos.y < lowerRange)
                        {
                            blocksBelowWaterline++;
                        }
                        else
                        {
                            TankBlock.
                            public void ApplyConnectedForce(bool invert = false)
                        }
                    }
                }
            }
        }

        private int UpdateBlocksBelowTree(IntVector3 partition)
        {
            int blocksBelowWaterline = 0;

            BlockManager.TableCache TC = tank.blockman.GetTableCacheForPlacementCollection();
            List<IntVector3> allBlockPos = new List<IntVector3>();
            float upperRange = WaterBuoyancy.HeightCalc + 0.7071f;
            float lowerRange = WaterBuoyancy.HeightCalc - 0.7071f;
            List<TankBlock> blocksAtWaterline = new List<TankBlock>();

            for (int stepX = 0; stepX < TC.size; stepX++)
            {
                for (int stepZ = 0; stepZ < TC.size; stepZ++)
                {
                    for (int stepZ = 0; stepZ < TC.size; stepZ++)
                    {
                        Vector3 worldPos = transform.TransformPoint(new Vector3(stepX, stepY, stepZ));
                        if (worldPos.y > upperRange ||
                            !TC.blockTable[stepX, stepY, stepZ])
                        {
                            continue;
                        }
                        else if (worldPos.y < lowerRange)
                        {
                            blocksBelowWaterline++;
                        }
                        else
                        {
                            TankBlock.
                            public void ApplyConnectedForce(bool invert = false)
                            blocksAtWaterline++;
                        }
                    }
                }
            }
            foreach (TankBlock TB in TankBlock)
                transform.up;
            WaterBuoyancy.HeightCalc
        }

        // Get Blocks that are partially floating
        public int EvaluateBuoyency()
        {
            transform.up;
            WaterBuoyancy.HeightCalc
        }
        */

    }
}
