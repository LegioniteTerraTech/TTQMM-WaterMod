using System;
using QModManager.Utility;
using UnityEngine;

namespace WaterMod
{
    public class WaterObj : WaterEffect
    {
        public WaterObj()
        {
            set = WaterBuoyancy.HeightCalc > this.transform.position.y + 1;
        }

        //public TankEffect watertank;
        public byte heartBeat = 0;

        public EffectTypes effectType;
        public Component effectBase;
        public bool isProjectile = false;
        public Rigidbody _rbody;
        public Vector3 initVelocity;
        private bool set;

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

        public override void Stay(byte HeartBeat)
        {
            try
            {
                if (HeartBeat == heartBeat)
                {
                    return;
                }

                heartBeat = HeartBeat;
                {
                    switch (effectType)
                    {
                        case EffectTypes.NormalProjectile:
                            _rbody.velocity *= 1f - (WaterBuoyancy.Density * WaterBuoyancy.BulletDampener);
                            break;

                        case EffectTypes.MissileProjectile:
                            _rbody.velocity *= 1f - (WaterBuoyancy.Density * WaterBuoyancy.MissileDampener);
                            break;

                        case EffectTypes.ResourceChunk:
                            float num2 = WaterBuoyancy.HeightCalc - _rbody.position.y;
                            num2 = num2 * Mathf.Abs(num2) + WaterBuoyancy.SurfaceSkinning;
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
                                _rbody.AddForce(Vector3.up * WaterBuoyancy.Density * num2 * WaterBuoyancy.ResourceBuoyancyMultiplier, ForceMode.Force);
                                _rbody.velocity -= (_rbody.velocity * _rbody.velocity.magnitude * (1f - WaterBuoyancy.Density / 10000f)) * 0.0025f;
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
                Debug.Log("Exception in Stay: " + e.Message + "\n efectType: " + effectType.ToString() + (flag ? "\nRigidbody is null!" : ""));
                if (flag)
                {
                    GetRBody();
                }
            }
        }

        public override void Ent(byte HeartBeat)
        {
            try
            {
                if (set)
                {
                    WaterParticleHandler.SplashAtPos(new Vector3(effectBase.transform.position.x, WaterBuoyancy.HeightCalc, effectBase.transform.position.z), _rbody.velocity.y, -0.25f);
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
                    var managedEvent = ((MissileProjectile)this.effectBase).GetInstanceField("m_BoosterDeactivationEvent") as ManTimedEvents.ManagedEvent;
                    if (managedEvent.TimeRemaining != 0)
                    {
                        managedEvent.Reset(managedEvent.TimeRemaining * 4f);
                    }
                    //((MissileProjectile)this.effectBase).SetInstanceField("m_BoosterDeactivationEvent", managedEvent);
                    _rbody.useGravity = false;
                }
                else
                {
                    initVelocity = _rbody.velocity;
                    _rbody.velocity = initVelocity * (1f / (WaterBuoyancy.Density * WaterBuoyancy.LaserFraction + 1f));
                }
                var managedEvent2 = (this.effectBase as Projectile).GetInstanceField("m_TimeoutDestroyEvent") as ManTimedEvents.ManagedEvent;
                managedEvent2.Reset(managedEvent2.TimeRemaining * destroyMultiplier);

                //(this.effectBase as LaserProjectile).SetInstanceField("m_TimeoutDestroyEvent", managedEvent2);

                return;
            }
            catch (Exception e)
            {
                bool flag = _rbody == null;
                Debug.Log("Exception in Ent: " + e.Message + "\n efectType: " + effectType.ToString() + (flag ? "\nRigidbody is null!" : ""));
                if (flag)
                {
                    GetRBody();
                }
            }
        }

        public override void Ext(byte HeartBeat)
        {
            try
            {
                if (!set)
                {
                    set = true;
                }
                WaterParticleHandler.SplashAtPos(new Vector3(effectBase.transform.position.x, WaterBuoyancy.HeightCalc, effectBase.transform.position.z), _rbody.velocity.y, -0.25f);

                if (effectType < EffectTypes.LaserProjectile)
                {
                    return;
                }
                float destroyMultiplier = 4f;
                if (effectType == EffectTypes.MissileProjectile)
                {
                    destroyMultiplier += 1f;
                    var managedEvent = (this.effectBase as MissileProjectile).GetInstanceField("m_BoosterDeactivationEvent") as ManTimedEvents.ManagedEvent;
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
                {
                    _rbody.velocity = initVelocity * (WaterBuoyancy.Density * 0.025f * WaterBuoyancy.LaserFraction + 1f);
                }
                var managedEvent2 = (this.effectBase as Projectile).GetInstanceField("m_TimeoutDestroyEvent") as ManTimedEvents.ManagedEvent;
                managedEvent2.Reset(managedEvent2.TimeRemaining / destroyMultiplier);
                //(this.effectBase as Projectile).SetInstanceField("m_TimeoutDestroyEvent", managedEvent2);

                return;
            }
            catch (Exception e)
            {
                bool flag = _rbody == null;
                Debug.Log("Exception in Ext: " + e.Message + "\n efectType: " + effectType.ToString() + (flag ? "\nRigidbody is null!" : ""));
                if (flag)
                {
                    GetRBody();
                }
                return;
            }
        }
    }
}
