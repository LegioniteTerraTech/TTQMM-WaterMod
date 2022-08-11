using System.Collections.Generic;
using UnityEngine;

namespace WaterMod
{
    public class WaterTank : MonoBehaviour
    {
        public Tank tank;
        public Vector3 SubmergeAdditivePos = Vector3.zero;
        public int SubmergeCount = 0;
        public Vector3 SurfaceAdditivePos = Vector3.zero;
        public int SurfaceCount = 0;

        public void Subscribe(Tank tank)
        {
            tank.AttachEvent.Subscribe(AddBlock);
            tank.DetachEvent.Subscribe(RemoveBlock);
            this.tank = tank;
        }

        public void AddGeneralBuoyancy(Vector3 position)
        {
            SubmergeAdditivePos += position;
            SubmergeCount++;
        }

        public void AddSurface(Vector3 position)
        {
            SurfaceAdditivePos += position;
            SurfaceCount++;
        }

        public void AddBlock(TankBlock tankblock, Tank tank)
        {
            tankblock.GetComponent<WaterBlock>().watertank = this;
        }

        public void RemoveBlock(TankBlock tankblock, Tank tank)
        {
            tankblock.GetComponent<WaterBlock>().watertank = null;
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

        public void FixedUpdate()
        {
            if (WaterBuoyancy.WorldMove)
                return; // the world is treadmilling and we must ignore the delayed physics update to prevent fling
            if (SubmergeCount != 0)
            {
                tank.rbody.AddForceAtPosition(Vector3.up * (WaterBuoyancy.Density * 7.5f) * SubmergeCount, SubmergeAdditivePos / SubmergeCount);
                SubmergeAdditivePos = Vector3.zero;
                if (QPatch.TheWaterIsLava)
                    tank.rbody.AddForce(-(tank.rbody.velocity * WaterBuoyancy.SubmergedTankDampening * 3 + (Vector3.up * (tank.rbody.velocity.y * WaterBuoyancy.SubmergedTankDampeningYAddition))) * SubmergeCount, ForceMode.Force);
                else
                    tank.rbody.AddForce(-(tank.rbody.velocity * WaterBuoyancy.SubmergedTankDampening + (Vector3.up * (tank.rbody.velocity.y * WaterBuoyancy.SubmergedTankDampeningYAddition))) * SubmergeCount, ForceMode.Force);
                SubmergeCount = 0;
            }
            if (SurfaceCount != 0)
            {
                if (QPatch.TheWaterIsLava)
                    tank.rbody.AddForceAtPosition(-(tank.rbody.velocity * WaterBuoyancy.SurfaceTankDampening * 3 + (Vector3.up * (tank.rbody.velocity.y * WaterBuoyancy.SurfaceTankDampeningYAddition))) * SurfaceCount, SurfaceAdditivePos / SurfaceCount);
                else
                    tank.rbody.AddForceAtPosition(-(tank.rbody.velocity * WaterBuoyancy.SurfaceTankDampening + (Vector3.up * (tank.rbody.velocity.y * WaterBuoyancy.SurfaceTankDampeningYAddition))) * SurfaceCount, SurfaceAdditivePos / SurfaceCount);
                SurfaceAdditivePos = Vector3.zero;
                SurfaceCount = 0;
            }
        }

        public static void UpdateAllReversed()
        {
            foreach (Tank tank in Singleton.Manager<ManTechs>.inst.CurrentTechs)
            {
                try
                {
                    if (!tank.FirstUpdateAfterSpawn)
                        tank.GetComponent<WaterTank>().InvertedUpdate();
                }
                catch { }
            }
        }

        /// <summary>
        /// Compensate for WorldTreadmill
        /// </summary>
        public void InvertedUpdate()
        {
            if (SubmergeCount != 0)
            {
                tank.rbody.AddForceAtPosition(Vector3.down * (WaterBuoyancy.Density * 7.5f) * SubmergeCount, SubmergeAdditivePos / SubmergeCount);
                SubmergeAdditivePos = Vector3.zero;
                if (QPatch.TheWaterIsLava)
                    tank.rbody.AddForce(-(tank.rbody.velocity * WaterBuoyancy.SubmergedTankDampening * WaterBuoyancy.LavaDampenMulti + (Vector3.down * (tank.rbody.velocity.y * WaterBuoyancy.SubmergedTankDampeningYAddition))) * SubmergeCount, ForceMode.Force);
                else
                    tank.rbody.AddForce(-(tank.rbody.velocity * WaterBuoyancy.SubmergedTankDampening + (Vector3.down * (tank.rbody.velocity.y * WaterBuoyancy.SubmergedTankDampeningYAddition))) * SubmergeCount, ForceMode.Force);
                SubmergeCount = 0;
            }
            if (SurfaceCount != 0)
            {
                if (QPatch.TheWaterIsLava)
                    tank.rbody.AddForceAtPosition(-(tank.rbody.velocity * WaterBuoyancy.SurfaceTankDampening * WaterBuoyancy.LavaDampenMulti + (Vector3.down * (tank.rbody.velocity.y * WaterBuoyancy.SurfaceTankDampeningYAddition))) * SurfaceCount, SurfaceAdditivePos / SurfaceCount);
                else
                    tank.rbody.AddForceAtPosition(-(tank.rbody.velocity * WaterBuoyancy.SurfaceTankDampening + (Vector3.down * (tank.rbody.velocity.y * WaterBuoyancy.SurfaceTankDampeningYAddition))) * SurfaceCount, SurfaceAdditivePos / SurfaceCount);
                SurfaceAdditivePos = Vector3.zero;
                SurfaceCount = 0;
            }
        }
    }
}
