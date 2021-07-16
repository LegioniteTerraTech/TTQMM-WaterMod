using UnityEngine;

namespace WaterMod
{
    public class WaterBlock : WaterEffect
    {
        public WaterTank watertank;
        public bool isFanJet;
        public MonoBehaviour componentEffect;
        public Vector3 initVelocity;
        public SurfacePool.Item surface = null;
        bool surfaceExist = false;
        private byte heartBeat = 0;
        public TankBlock TankBlock;

        private void Surface()
        {
            if (WaterParticleHandler.UseParticleEffects)
            {
                if (surface == null || !surface.Using)
                {
                    surface = SurfacePool.GetFromPool();
                }

                if (surface == null)
                {
                    return;
                }
                var e = TankBlock.centreOfMassWorld;
                surface.UpdatePos(new Vector3(e.x, WaterBuoyancy.HeightCalc, e.z));
                surfaceExist = true;
            }
        }

        public void TryRemoveSurface()
        {
            if (surfaceExist && surface != null)
            {
                SurfacePool.ReturnToPool(surface);
                surface = null;
                surfaceExist = false;
            }
        }

        public void ApplyMultiplierFanJet()
        {
            float num2 = (WaterBuoyancy.HeightCalc - componentEffect.transform.position.y + TankBlock.BlockCellBounds.extents.y) / TankBlock.BlockCellBounds.extents.y + 0.1f;
            if (num2 > 0.1f)
            {
                if (num2 > 1f)
                {
                    num2 = 1f;
                }
                FanJet component = (componentEffect as FanJet);
                component.force = initVelocity.x * (num2 * WaterBuoyancy.FanJetMultiplier + 1);
                component.backForce = initVelocity.y * (num2 * WaterBuoyancy.FanJetMultiplier + 1);
            }
        }

        public void ResetMultiplierFanJet()
        {
            FanJet component = (componentEffect as FanJet);
            component.force = initVelocity.x;
            component.backForce = initVelocity.y;
        }

        public override void Stay(byte HeartBeat)
        {
            if (heartBeat == HeartBeat)
            {
                return;
            }

            heartBeat = HeartBeat;
            try
            {
                if (TankBlock.tank != null)
                {
                    if (watertank == null || watertank.tank != TankBlock.tank)
                    {
                        watertank = TankBlock.tank.GetComponent<WaterTank>();
                    }
                    ApplyConnectedForce();
                    return;
                }
                if (TankBlock.rbody == null)
                {
                    TryRemoveSurface();
                }
                else
                {
                    ApplySeparateForce();
                }
            }
            catch
            {
                Debug.Log((watertank == null ? "WaterTank is null..." + (TankBlock.tank == null ? " And so is the tank" : "The tank is not") : "WaterTank exists") + (TankBlock.rbody == null ? "\nTankBlock Rigidbody is null" : "\nWhat?") + (TankBlock.IsAttached ? "\nThe block appears to be attached" : "\nThe block is not attached"));
            }
        }

        public void ApplySeparateForce()
        {
            //Vector3 vector = TankBlock.centreOfMassWorld;
            float Submerge = WaterBuoyancy.HeightCalc - TankBlock.centreOfMassWorld.y - TankBlock.BlockCellBounds.extents.y;
            Submerge = Submerge * Mathf.Abs(Submerge) + WaterBuoyancy.SurfaceSkinning;
            if (Submerge > 1.5f)
            {
                TryRemoveSurface();
                Submerge = 1.5f;
            }
            else if (Submerge < -0.2f)
            {
                Submerge = -0.2f;
            }
            else
            {
                Surface();
            }
            TankBlock.rbody.AddForce(Vector3.up * (Submerge * WaterBuoyancy.Density * 5f));
            ApplyDamageIfLava(TankBlock.centreOfMassWorld);
        }

        public void ApplyConnectedForce()
        {
            IntVector3[] intVector = TankBlock.filledCells;
            int CellCount = intVector.Length;
            if (CellCount == 1)
            {
                ApplyConnectedForce_Internal(TankBlock.centreOfMassWorld);
            }
            else
            {
                for (int CellIndex = 0; CellIndex < CellCount; CellIndex++)
                {
                    ApplyConnectedForce_Internal(transform.TransformPoint(intVector[CellIndex].x, intVector[CellIndex].y, intVector[CellIndex].z));
                }
            }
            if (this.isFanJet)
            {
                ApplyMultiplierFanJet();
            }
            ApplyDamageIfLava(TankBlock.centreOfMassWorld);
        }

        private void ApplyDamageIfLava(Vector3 vector)
        {
            if (QPatch.TheWaterIsLava)
            {
                TankBlock.damage.MultiplayerFakeDamagePulse();
                if (LavaMode.DealPainThisFrame)
                {
                    float Submerge = WaterBuoyancy.HeightCalc - vector.y;
                    Submerge = Submerge * Mathf.Abs(Submerge) + WaterBuoyancy.SurfaceSkinning;
                    if (Submerge > 1.5f)
                        Submerge = 1.5f;
                    else if (Submerge < -0.2f)
                        Submerge = -0.2f;

                    Singleton.Manager<ManDamage>.inst.DealDamage(TankBlock.GetComponent<Damageable>(), LavaMode.MeltBlocksStrength * Submerge * Time.deltaTime * LavaMode.DamageUpdateDelay, ManDamage.DamageType.Fire, LavaMode.inst);
                }
            }
        }

        private void ApplyConnectedForce_Internal(Vector3 vector)
        {
            float Submerge = WaterBuoyancy.HeightCalc - vector.y;
            Submerge = Submerge * Mathf.Abs(Submerge) + WaterBuoyancy.SurfaceSkinning;
            if (Submerge >= -0.5f)
            {
                if (Submerge > 1.5f)
                {
                    watertank.AddGeneralBuoyancy(vector);
                    TryRemoveSurface();
                    return;
                }
                else if (Submerge < -0.2f)
                {
                    Submerge = -0.2f;
                }
                else
                {
                    Surface();
                    watertank.AddSurface(vector);
                }
                watertank.tank.rbody.AddForceAtPosition(Vector3.up * (Submerge * WaterBuoyancy.Density * 5f), vector);
            }
        }

        public override void Ent(byte HeartBeat)
        {
            Surface();
            try
            {
                var val = TankBlock.centreOfMassWorld;
                WaterParticleHandler.SplashAtPos(new Vector3(val.x, WaterBuoyancy.HeightCalc, val.z), (TankBlock.tank != null ? watertank.tank.rbody.GetPointVelocity(val).y : TankBlock.rbody.velocity.y), TankBlock.BlockCellBounds.extents.magnitude);
            }
            catch { }
        }

        public override void Ext(byte HeartBeat)
        {
            TryRemoveSurface();
            try
            {
                if (isFanJet)
                {
                    ResetMultiplierFanJet();
                }

                var val = TankBlock.centreOfMassWorld;
                WaterParticleHandler.SplashAtPos(new Vector3(val.x, WaterBuoyancy.HeightCalc, val.z), (TankBlock.tank != null ? watertank.tank.rbody.GetPointVelocity(val).y : TankBlock.rbody.velocity.y), TankBlock.BlockCellBounds.extents.magnitude);
            }
            catch { }
        }
    }
}
