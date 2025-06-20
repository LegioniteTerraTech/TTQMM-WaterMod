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
        public static HashSet<WaterObj> chunks = new HashSet<WaterObj>();
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
            if (QPatch.EnableLooseBlocksFloat)
            {
                foreach (var item in chunks)
                {
                    try
                    {
                        item.RemoteUpdate();
                    }
                    catch { }
                }
            }
        }

        //public byte heartBeat = 0;
        private byte Sleep = 0;

        public EffectTypes effectType;
        public Component effectBase;
        public float initialDrag;
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
            WO.initialDrag = WO._rbody.drag;
            WO.DisableCollideWithWater();
            return WO;
        }
        public static WaterObj Insure(ResourcePickup vis)
        {
            WaterObj WO = vis.GetComponent<WaterObj>();
            if (WO)
            {
                chunks.Add(WO);
                return WO;
            }
            WO = vis.gameObject.AddComponent<WaterObj>();
            WO.effectBase = vis;
            WO.effectType = EffectTypes.ResourceChunk;
            WO._rbody = vis.GetComponent<Rigidbody>();
            WO.initialDrag = Globals.inst.airSpeedDrag;
            WO.DisableCollideWithWater();
            vis.visible.RecycledEvent.Subscribe(WO.OnRecycled);
            //DebugWater.Assert("Tracking chunk");
            chunks.Add(WO);
            return WO;
        }
        public void Reset()
        {
            UpdateAttached(SubState.Above);
        }
        private void OnRecycled(Visible vis)
        {
            var visOurs = ((ResourcePickup)effectBase).visible;
            if (vis == visOurs)
            {
                visOurs.RecycledEvent.Unsubscribe(OnRecycled);
                visOurs.ConditionalUpdater.FixedUpdateEvent.Unsubscribe(RemoteFixedUpdate);
                chunks.Remove(this);
            }
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

        internal void SetDrag(bool active)
        {
            try
            {
                if (active)
                {
                    switch (effectType)
                    {
                        case EffectTypes.NormalProjectile:
                            if (QPatch.TheWaterIsLava)
                                _rbody.drag = initialDrag + (WaterGlobals.Density * WaterGlobals.BulletDampener * 3);
                            else
                                _rbody.drag = initialDrag + (WaterGlobals.Density * WaterGlobals.BulletDampener);
                            break;

                        case EffectTypes.MissileProjectile:
                            if (QPatch.TheWaterIsLava)
                                _rbody.drag = initialDrag + (WaterGlobals.Density * WaterGlobals.MissileDampener * 3);
                            else
                                _rbody.drag = initialDrag + (WaterGlobals.Density * WaterGlobals.MissileDampener);
                            break;

                        case EffectTypes.ResourceChunk:
                            if (QPatch.TheWaterIsLava)
                                _rbody.drag = initialDrag + (1f - WaterGlobals.Density * 3 / 10000f);
                            else
                                _rbody.drag = initialDrag + (1f - WaterGlobals.Density / 10000f);
                            break;

                        default:
                            break;
                    }
                }
                else
                    _rbody.drag = initialDrag;
            }
            catch (Exception e)
            {
                bool flag = _rbody == null;
                DebugWater.Log("Exception in SetDrag: " + e.Message + "\n efectType: " + effectType.ToString() + (flag ? "\nRigidbody is null!" : ""));
                if (flag)
                {
                    GetRBody();
                }
            }
        }

        public override void Stay() //Stay(byte HeartBeat)
        { }
        private void RemoteFixedUpdate()
        {
            try
            {
                if (ManWater.WorldMove)
                    return; // the world is treadmilling and we must ignore the delayed physics update to prevent fling
                if (_rbody.position.y - ManWater.HeightCalc > ManWater.minBlockSleepHeight && _rbody.velocity.Approximately(Vector3.zero, 0.25f))
                {
                    Sleep++;
                    if (Sleep > 16)
                        _rbody.Sleep();
                }
                else
                {
                    Sleep = 0;
                    float Submerge = ManWater.HeightCalc - _rbody.position.y;
                    Submerge = Submerge * Mathf.Abs(Submerge) + WaterGlobals.SurfaceSkinning;
                    if (Submerge >= -0.5f)
                    {
                        if (Submerge > 1.5f)
                            Submerge = 1.5f;

                        if (Submerge < -0.1f)
                            Submerge = -0.1f;
                        _rbody.AddForce(Vector3.up * WaterGlobals.Density * Submerge * WaterGlobals.ResourceBuoyancyMultiplier, ForceMode.Force);
                    }
                }
            }
            catch (Exception e)
            {
                bool flag = _rbody == null;
                //DebugWater.Log("Exception in RemoteFixedUpdate: " + e.Message + "\n efectType: " + effectType.ToString() + (flag ? "\nRigidbody is null!" : ""));
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
                if (!_rbody)
                    GetRBody();
                if (set)
                {
                    if (_rbody)
                        WaterParticleHandler.SplashAtPos(new Vector3(effectBase.transform.position.x, ManWater.HeightCalc + WaterParticleHandler.offsetHeightSplash, effectBase.transform.position.z), _rbody.velocity.y, -0.25f);
                }
                else
                    set = true;
                SetDrag(true);

                ManTimedEvents.ManagedEvent managedEvent;
                switch (effectType)
                {
                    case EffectTypes.ResourceChunk:
                        ((ResourcePickup)effectBase).visible.ConditionalUpdater.FixedUpdateEvent.Subscribe(RemoteFixedUpdate);
                        break;
                    case EffectTypes.NormalProjectile:
                        break;
                    case EffectTypes.LaserProjectile:
                        // Laser debuff
                        initVelocity = _rbody.velocity;
                        _rbody.velocity = initVelocity * (1f / (WaterGlobals.Density * WaterGlobals.LaserFraction + 1f));

                        //  Erad laser
                        //(effectBase as LaserProjectile).HandleCollision(null, gameObject.transform.position, null, true);

                        //(effectBase as LaserProjectile).SetInstanceField("m_TimeoutDestroyEvent", managedEvent2);
                        managedEvent = (ManTimedEvents.ManagedEvent)ProjectileEvent.GetValue(effectBase as Projectile);
                        managedEvent.Reset(managedEvent.TimeRemaining * 4);
                        break;
                    case EffectTypes.MissileProjectile:
                        managedEvent = (ManTimedEvents.ManagedEvent)BoosterEvent.GetValue((MissileProjectile)this.effectBase);
                        if (managedEvent.TimeRemaining != 0)
                        {
                            managedEvent.Reset(managedEvent.TimeRemaining * 4f);
                        }
                        //((MissileProjectile)this.effectBase).SetInstanceField("m_BoosterDeactivationEvent", managedEvent);
                        _rbody.useGravity = false;
                        managedEvent = (ManTimedEvents.ManagedEvent)ProjectileEvent.GetValue(effectBase as Projectile);
                        managedEvent.Reset(managedEvent.TimeRemaining * 5);
                        break;
                    default:
                        throw new NotImplementedException(effectType + " is not implemented");
                }
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
                if (!_rbody)
                    GetRBody();
                if (!set)
                {
                    set = true;
                }
                if (_rbody)
                    WaterParticleHandler.SplashAtPos(new Vector3(effectBase.transform.position.x, ManWater.HeightCalc + WaterParticleHandler.offsetHeightSplash, effectBase.transform.position.z), _rbody.velocity.y, -0.25f);

                SetDrag(false);

                ManTimedEvents.ManagedEvent managedEvent;
                switch (effectType)
                {
                    case EffectTypes.ResourceChunk:
                        ((ResourcePickup)effectBase).visible.ConditionalUpdater.FixedUpdateEvent.Unsubscribe(RemoteFixedUpdate);
                        break;
                    case EffectTypes.NormalProjectile:
                        break;
                    case EffectTypes.LaserProjectile:
                        // Laser debuff
                        _rbody.velocity = initVelocity * (WaterGlobals.Density * 0.025f * WaterGlobals.LaserFraction + 1f);
                        managedEvent = (ManTimedEvents.ManagedEvent)ProjectileEvent.GetValue(this.effectBase as Projectile);
                        managedEvent.Reset(managedEvent.TimeRemaining / 4);
                        //(this.effectBase as Projectile).SetInstanceField("m_TimeoutDestroyEvent", managedEvent);
                        break;
                    case EffectTypes.MissileProjectile:
                        managedEvent = (ManTimedEvents.ManagedEvent)BoosterEvent.GetValue(this.effectBase as MissileProjectile);
                        if (managedEvent.TimeRemaining == 0f)
                        {
                            _rbody.useGravity = true;
                        }
                        else
                        {
                            managedEvent.Reset(managedEvent.TimeRemaining * .25f);
                        }
                        //(this.effectBase as MissileProjectile).SetInstanceField("m_BoosterDeactivationEvent", managedEvent);
                        managedEvent = (ManTimedEvents.ManagedEvent)ProjectileEvent.GetValue(this.effectBase as Projectile);
                        managedEvent.Reset(managedEvent.TimeRemaining / 5);
                        //(this.effectBase as Projectile).SetInstanceField("m_TimeoutDestroyEvent", managedEvent);
                        break;
                }
            }
            catch //(Exception e)
            {
                bool flag = _rbody == null;
                //Debug.Log("Exception in Ext: " + e.Message + "\n efectType: " + effectType.ToString() + (flag ? "\nRigidbody is null!" : ""));
                if (flag)
                {
                    GetRBody();
                }
            }
        }
    }
}
