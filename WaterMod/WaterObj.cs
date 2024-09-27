using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
#if !STEAM
using QModManager.Utility;
#endif

namespace WaterMod
{
    internal class WaterObj : WaterEffectFast
    {
        public WaterObj()
        {
            set = ManWater.HeightCalc > this.transform.position.y + 1;
        }

        public static Dictionary<int, WeaponRound> projs;
        public static void RemoteFixedUpdateAll()
        {
            if (projs == null)
                projs = (Dictionary<int, WeaponRound>)typeof(ManCombat.Projectiles).GetField("s_WeaponRoundLookup",
                    BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            foreach (var item in projs.Values)
            {
                try
                {
                    if (item is Projectile proj)
                        Insure(proj).RemoteUpdate();
                }
                catch { }
            }
        }

        //public TankEffect watertank;
        public byte heartBeat = 0;

        public EffectTypes effectType;
        public Component effectBase;
        public bool isProjectile = false;
        public Rigidbody _rbody;
        public Vector3 initVelocity;
        private bool set;

        public static WaterObj Insure(Projectile vis)
        {
            WaterObj WO = vis.GetComponent<WaterObj>();
            if (WO)
                return WO;
            WO = vis.gameObject.AddComponent<WaterObj>();
            WO.effectBase = vis;

            if (vis.GetComponent<MissileProjectile>())
                WO.effectType = EffectTypes.MissileProjectile;
            else if (vis.GetComponent<LaserProjectile>())
                WO.effectType = EffectTypes.LaserProjectile;
            else
                WO.effectType = EffectTypes.NormalProjectile;
            WO._rbody = vis.rbody;
            WO.DisableCollideWithWater();
            return WO;
        }
        public static WaterObj Insure(ResourcePickup vis)
        {
            WaterObj WO = vis.GetComponent<WaterObj>();
            if (WO)
                return WO;
            WO = vis.gameObject.AddComponent<WaterObj>();
            WO.effectBase = vis;
            WO.effectType = EffectTypes.ResourceChunk;
            WO._rbody = vis.rbody;
            WO.DisableCollideWithWater();
            return WO;
        }
        public void Reset()
        {
            UpdateAttached(SubState.Above);
        }

        public void GetRBody()
        {
            switch (effectType)
            {
                case EffectTypes.ResourceChunk:
                    _rbody = ((ResourcePickup)effectBase).rbody;
                    break;

                case EffectTypes.LaserProjectile:
                case EffectTypes.MissileProjectile:
                case EffectTypes.NormalProjectile:
                    _rbody = ((Projectile)effectBase).GetComponent<Rigidbody>();
                    if (_rbody == null)
                    {
                        _rbody = (effectBase as Projectile).GetComponentInParent<Rigidbody>();
                        if (_rbody == null)
                        {
                            _rbody = (effectBase as Projectile).GetComponentInChildren<Rigidbody>();
                        }
                    }
                    break;
            }
        }

        public override void Stay() //Stay(byte HeartBeat)
        {
            try
            {
                /*
                if (HeartBeat == heartBeat)
                {
                    return;
                }

                heartBeat = HeartBeat;*/
                {
                    switch (effectType)
                    {
                        case EffectTypes.NormalProjectile:
                            if (QPatch.TheWaterIsLava)
                                _rbody.velocity *= 1f - (WaterGlobals.Density * WaterGlobals.BulletDampener * 3);
                            else
                                _rbody.velocity *= 1f - (WaterGlobals.Density * WaterGlobals.BulletDampener);
                            break;

                        case EffectTypes.MissileProjectile:
                            if (QPatch.TheWaterIsLava)
                                _rbody.velocity *= 1f - (WaterGlobals.Density * WaterGlobals.MissileDampener * 3);
                            else
                                _rbody.velocity *= 1f - (WaterGlobals.Density * WaterGlobals.MissileDampener);
                            break;

                        case EffectTypes.ResourceChunk:
                            if (QPatch.EnableLooseBlocksFloat)
                            {
                                if (ManWater.WorldMove)
                                    return; // the world is treadmilling and we must ignore the delayed physics update to prevent fling
                                float num2 = ManWater.HeightCalc - _rbody.position.y;
                                num2 = num2 * Mathf.Abs(num2) + WaterGlobals.SurfaceSkinning;
                                if (num2 >= -0.5f)
                                {
                                    if (num2 > 1.5f)
                                    {
                                        num2 = 1.5f;
                                    }

                                    if (num2 < -0.1f)
                                    {
                                        num2 = -0.1f;
                                    }
                                    Vector3 velo = Vector3.up * WaterGlobals.Density * num2 * WaterGlobals.ResourceBuoyancyMultiplier;
                                    if (QPatch.TheWaterIsLava)
                                        velo -= (_rbody.velocity * _rbody.velocity.magnitude * (1f - WaterGlobals.Density / 10000f)) * 0.0075f;
                                    else
                                        velo -= (_rbody.velocity * _rbody.velocity.magnitude * (1f - WaterGlobals.Density / 10000f)) * 0.0025f;
                                    _rbody.AddForce(velo, ForceMode.Force);
                                }
                            }
                            break;

                        default:
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                bool flag = _rbody == null;
                DebugWater.Log("Exception in Stay: " + e.Message + "\n efectType: " + effectType.ToString() + (flag ? "\nRigidbody is null!" : ""));
                if (flag)
                {
                    GetRBody();
                }
            }
        }

        private static FieldInfo BoosterEvent = typeof(MissileProjectile).GetField("m_BoosterDeactivationEvent",
            BindingFlags.NonPublic | BindingFlags.Instance),
            ProjectileEvent = typeof(MissileProjectile).GetField("m_TimeoutDestroyEvent",
            BindingFlags.NonPublic | BindingFlags.Instance);
        public override void Enter() //Ent(byte HeartBeat)
        {
            try
            {
                if (set)
                {
                    WaterParticleHandler.SplashAtPos(new Vector3(effectBase.transform.position.x, ManWater.HeightCalc + WaterParticleHandler.offsetHeightSplash, effectBase.transform.position.z), _rbody.velocity.y, -0.25f);
                }
                else
                {
                    set = true;
                }
                if (effectType < EffectTypes.LaserProjectile)
                {
                    return;
                }
                float destroyMultiplier = 4f;
                if (effectType == EffectTypes.MissileProjectile)
                {
                    destroyMultiplier += 1f;
                    var managedEvent = (ManTimedEvents.ManagedEvent)BoosterEvent.GetValue((MissileProjectile)this.effectBase);
                    if (managedEvent.TimeRemaining != 0)
                    {
                        managedEvent.Reset(managedEvent.TimeRemaining * 4f);
                    }
                    //((MissileProjectile)this.effectBase).SetInstanceField("m_BoosterDeactivationEvent", managedEvent);
                    _rbody.useGravity = false;
                }
                else
                {   // Laser debuff
                    initVelocity = _rbody.velocity;
                    _rbody.velocity = initVelocity * (1f / (WaterGlobals.Density * WaterGlobals.LaserFraction + 1f));

                    //  Erad laser
                    //(effectBase as LaserProjectile).HandleCollision(null, gameObject.transform.position, null, true);

                    //(effectBase as LaserProjectile).SetInstanceField("m_TimeoutDestroyEvent", managedEvent2);
                }
                var managedEvent2 = (ManTimedEvents.ManagedEvent)ProjectileEvent.GetValue(this.effectBase as Projectile);
                managedEvent2.Reset(managedEvent2.TimeRemaining * destroyMultiplier);

                //(this.effectBase as LaserProjectile).SetInstanceField("m_TimeoutDestroyEvent", managedEvent2);

                return;
            }
            catch //(Exception e)
            {
                bool flag = _rbody == null;
                // It's null for the following reasons:
                //    Held in anchored (solid, not sky anchor) tractor beam
                //    stale on the ground for too long
                //    Quieted it for now as log became busy
                //Debug.Log("Exception in Ent: " + e.Message + "\n efectType: " + effectType.ToString() + (flag ? "\nRigidbody is null!" : ""));
                if (flag)
                {
                    GetRBody();
                }
            }
        }

        public override void Exit() //Ext(byte HeartBeat)
        {
            try
            {
                if (!set)
                {
                    set = true;
                }
                WaterParticleHandler.SplashAtPos(new Vector3(effectBase.transform.position.x, ManWater.HeightCalc + WaterParticleHandler.offsetHeightSplash, effectBase.transform.position.z), _rbody.velocity.y, -0.25f);

                if (effectType < EffectTypes.LaserProjectile)
                {
                    return;
                }
                float destroyMultiplier = 4f;
                if (effectType == EffectTypes.MissileProjectile)
                {
                    destroyMultiplier += 1f;
                    var managedEvent = (ManTimedEvents.ManagedEvent)BoosterEvent.GetValue(this.effectBase as MissileProjectile);
                    if (managedEvent.TimeRemaining == 0f)
                    {
                        _rbody.useGravity = true;
                    }
                    else
                    {
                        managedEvent.Reset(managedEvent.TimeRemaining * .25f);
                    }
                    //(this.effectBase as MissileProjectile).SetInstanceField("m_BoosterDeactivationEvent", managedEvent);
                }
                else
                {   // Laser debuff
                    _rbody.velocity = initVelocity * (WaterGlobals.Density * 0.025f * WaterGlobals.LaserFraction + 1f);
                }
                var managedEvent2 = (ManTimedEvents.ManagedEvent)ProjectileEvent.GetValue(this.effectBase as Projectile);
                managedEvent2.Reset(managedEvent2.TimeRemaining / destroyMultiplier);
                //(this.effectBase as Projectile).SetInstanceField("m_TimeoutDestroyEvent", managedEvent2);

                return;
            }
            catch //(Exception e)
            {
                bool flag = _rbody == null;
                //Debug.Log("Exception in Ext: " + e.Message + "\n efectType: " + effectType.ToString() + (flag ? "\nRigidbody is null!" : ""));
                if (flag)
                {
                    GetRBody();
                }
                return;
            }
        }
    }
}
