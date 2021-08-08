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
                    tank.GetComponent<WaterTank>().InvertedUpdate();
                }
                catch { }
            }
        }

        public void InvertedUpdate()
        {
            if (SubmergeCount != 0)
            {
                tank.rbody.AddForceAtPosition(Vector3.down * (WaterBuoyancy.Density * 7.5f) * SubmergeCount, SubmergeAdditivePos / SubmergeCount);
                SubmergeAdditivePos = Vector3.zero;
                if (QPatch.TheWaterIsLava)
                    tank.rbody.AddForce(-(tank.rbody.velocity * WaterBuoyancy.SubmergedTankDampening * 3 + (Vector3.down * (tank.rbody.velocity.y * WaterBuoyancy.SubmergedTankDampeningYAddition))) * SubmergeCount, ForceMode.Force);
                else
                    tank.rbody.AddForce(-(tank.rbody.velocity * WaterBuoyancy.SubmergedTankDampening + (Vector3.down * (tank.rbody.velocity.y * WaterBuoyancy.SubmergedTankDampeningYAddition))) * SubmergeCount, ForceMode.Force);
                SubmergeCount = 0;
            }
            if (SurfaceCount != 0)
            {
                if (QPatch.TheWaterIsLava)
                    tank.rbody.AddForceAtPosition(-(tank.rbody.velocity * WaterBuoyancy.SurfaceTankDampening * 3 + (Vector3.down * (tank.rbody.velocity.y * WaterBuoyancy.SurfaceTankDampeningYAddition))) * SurfaceCount, SurfaceAdditivePos / SurfaceCount);
                else
                    tank.rbody.AddForceAtPosition(-(tank.rbody.velocity * WaterBuoyancy.SurfaceTankDampening + (Vector3.down * (tank.rbody.velocity.y * WaterBuoyancy.SurfaceTankDampeningYAddition))) * SurfaceCount, SurfaceAdditivePos / SurfaceCount);
                SurfaceAdditivePos = Vector3.zero;
                SurfaceCount = 0;
            }
        }
    }
}
