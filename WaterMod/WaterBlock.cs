using System.Collections.Generic;
using System;
using UnityEngine;
using FMODUnity;
using System.Linq;
using System.Reflection;
using TerraTechETCUtil;

namespace WaterMod
{
    /*
     * NEW Plans: Optimize water - MOSTLY DID with lazier strats
     * Make a 3D matrix of 
     * 
     * 
     * 
     */
    internal enum SubState
    {
        Above,
        Float,
        Below,
    }
    internal enum BlockSpecial
    {
        None,
        Anchor,
        Props,
        Hollow,
        Wheels,
    }
    internal class WaterBlock : WaterEffect
    {

        private static FieldInfo wheelsAllGet = typeof(ManWheels).GetField("m_WheelState", BindingFlags.Instance | BindingFlags.NonPublic);
        private static Type wheelsState = typeof(ManWheels).GetNestedType("AttachedWheelState", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo wheelVelo = null; 

        private static FieldInfo wheelsGet = typeof(ModuleWheels).GetField("m_Wheels", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo wheelID = typeof(ManWheels.Wheel).GetField("attachedID", BindingFlags.Instance | BindingFlags.NonPublic);
        private static HashSet<WaterBlock> submergedLone = new HashSet<WaterBlock>();
        private static Array wheelsAll;

        public static void InvertPrevForces()
        {
            DebugWater.Log("Firing InvertPrevForces");
            if (Singleton.Manager<ManSpawn>.inst.IsTechSpawning)
                return; // Do not invert on world load!
            processing = true;
            try
            {
                int firedCount = 0;
                foreach (WaterBlock block in submergedLone)
                {
                    if (block.TankBlock.rbody != null)
                    {
                        block.ApplySeparateForce(true);
                    }
                    firedCount++;
                }
                DebugWater.Log("Undid " + firedCount + " forces");
            }
            catch (Exception e)
            {
                DebugWater.Log("Error on handling invert forces - " + e);
            }
            processing = false;
        }
        public static void MassApplyForces()
        {
            processing = true;
            try
            {
                int firedCount = 0;
                foreach (WaterBlock block in submergedLone)
                {
                    if (block.TankBlock.rbody != null)
                    {
                        block.ApplySeparateForce();
                    }
                    firedCount++;
                }
                DebugWater.Log("Redid " + firedCount + " forces");
            }
            catch
            {
                DebugWater.Log("Error on handling return forces");
            }
            processing = false;
        }


        public TankBlock TankBlock;
        public WaterTank watertank;
        public BlockSpecial Special;
        public MonoBehaviour[] componentEffects;
        public List<ManWheels.Wheel> wheelTracker;
        public Vector3[] initVelocities;
        public SurfacePool.Item particles = null;
        public SubState AttachedSubState = SubState.Above;
        bool surfaceExist = false;
        public float radius;
        private byte heartBeat = 0;
        private byte Sleep = 0;

        internal static float cachedFloatVal = WaterGlobals.Density * 5f;

        private static bool processing = false;

        internal static WaterBlock Insure(TankBlock block)
        {
            WaterBlock WB = block.GetComponent<WaterBlock>();
            if (WB)
                return WB;
            WB = block.gameObject.AddComponent<WaterBlock>();
            WB.TankBlock = block;
            block.AttachedEvent.Subscribe(WB.OnAttach);
            block.DetachingEvent.Subscribe(WB.OnDetach);
            switch (block.BlockCategory)
            {
                case BlockCategories.Wheels:
                    if (wheelsAll == null)
                    {
                        wheelVelo = wheelsState.GetField("angularVelocity", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        wheelsAll = (Array)wheelsAllGet.GetValue(ManWheels.inst);
                        if (wheelsAll == null)
                            throw new NullReferenceException("wheelsAll failed to fetch");
                    }
                    var wheels = block.GetComponent<ModuleWheels>();
                    if (wheels != null)
                    {
                        WB.Special = BlockSpecial.Wheels;
                        WB.wheelTracker = (List<ManWheels.Wheel>)wheelsGet.GetValue(wheels);
                        WB.componentEffects = new MonoBehaviour[1];
                        WB.componentEffects[0] = wheels;
                        WB.initVelocities = new Vector3[1];
                        //float Circumfrence = wheels.m_WheelParams.radius * Mathf.PI * 2;
                        //float surfArea = Mathf.Tan(wheels.m_WheelParams.thicknessAngular * 0.0175f);
                        float surfForce = wheels.m_WheelParams.radius * wheels.m_TorqueParams.torqueCurveMaxTorque / 360;
                        float wheelStress = wheels.m_TorqueParams.torqueCurveDrive.Evaluate(0.25f);
                        float MaxRPM = wheels.m_TorqueParams.torqueCurveMaxRpm;
                        //float wheelTop = Circumfrence * MaxRPM * 0.017f * wheelStress;
                        float wheelTop = wheels.m_WheelParams.radius * MaxRPM * wheelStress;
                        float wheelTopAvg = wheelTop;
                        for (int i = 0; i < 30; i++)
                        {
                            wheelTop = wheels.m_WheelParams.radius * MaxRPM * wheels.m_TorqueParams.torqueCurveDrive.Evaluate(Mathf.Clamp01(wheelTop / MaxRPM));
                            wheelTopAvg += wheelTop;
                        }
                        WB.initVelocities[0] = new Vector3(wheels.m_WheelParams.radius, Mathf.Max(surfForce * ManWater.WheelWaterForceMultiplier
                            / WB.wheelTracker.Count, ManWater.MinWheelMaxForce), Mathf.Clamp(wheelTopAvg / (Mathf.Clamp(wheels.m_WheelParams.radius * 
                            2, 0.5f, 2) * 45 * WB.wheelTracker.Count), 0, 75));
                    }
                    break;
                case BlockCategories.Flight:
                    var component = block.GetComponentsInChildren<FanJet>(true);
                    if (component != null)
                    {
                        WB.Special = BlockSpecial.Props;
                        WB.componentEffects = new MonoBehaviour[component.Length];
                        WB.initVelocities = new Vector3[component.Length];
                        for (int i = 0; i < component.Length; i++)
                        {
                            var comp = component[i];
                            WB.componentEffects[i] = comp;
                            WB.initVelocities[i] = new Vector3(
                                (float)RawTechBase.thrustRate.GetValue(comp), 
                                (float)RawTechBase.fanThrustRateRev.GetValue(comp), 0f);
                        }
                    }
                    break;
                default:
                    WB.Special = block.GetComponent<ModuleAnchor>() ? BlockSpecial.Anchor : BlockSpecial.None;
                    if (WB.Special == BlockSpecial.None)
                    {
                       var BD =  BlockIndexer.GetBlockDetails(block.BlockType);
                        if (BD.IsBasic && block.visible.damageable.DamageableType ==
                            ManDamage.DamageableType.Standard)
                            WB.Special = BlockSpecial.Hollow;
                    }
                    break;
            }
            return WB;
        }
        internal void OnAttach()
        {
            TryRemoveSurface();
            watertank = WaterTank.Insure(TankBlock.tank);
            TankBlock.IgnoreCollision(ManWater.seaCol, true);
            radius = TankBlock.BlockCellBounds.extents.magnitude + 0.75f;
            UpdateAttached(SubState.Above);
        }
        internal void OnDetach()
        {
            TryRemoveSurface();
            TankBlock.IgnoreCollision(ManWater.seaCol, false);
            watertank = null;
        }
        public void PlaySplashSFX()
        {
            if (!TankBlock.rbody || ManWater.blockSFXCount > 5)
                return;
            ManWater.blockSFXCount++;
            Vector3 exts = TankBlock.BlockCellBounds.size;
            float extent = Mathf.Max(exts.x, exts.y, exts.z);
            if (extent < 6)
            {
                if (ManWater.SplashSmall != null)
                {
                    ManWater.SplashSmall.Volume = Mathf.Clamp01(Mathf.Abs(TankBlock.rbody.velocity.y * 0.025f));
                    ManWater.SplashSmall.Play(true, TankBlock.centreOfMassWorld);
                }
            }
            else if (extent < 18)
            {
                if (ManWater.SplashMedium != null)
                {
                    ManWater.SplashMedium.Volume = Mathf.Clamp01(Mathf.Abs(TankBlock.rbody.velocity.y * 0.025f));
                    ManWater.SplashMedium.Play(true, TankBlock.centreOfMassWorld);
                }
            }
            else
            {
                if (ManWater.SplashLarge != null)
                {
                    ManWater.SplashLarge.Volume = Mathf.Clamp01(Mathf.Abs(TankBlock.rbody.velocity.y * 0.025f));
                    ManWater.SplashLarge.Play(true, TankBlock.centreOfMassWorld);
                }
            }
        }
        private void SurfaceUpdate()
        {
            if (WaterParticleHandler.UseParticleEffects)
            {
                if (ManWater.CameraSubmerged)
                {
                    TryRemoveSurface(true);
                }
                else
                {
                    if (particles == null || !particles.Using)
                        particles = SurfacePool.GetFromPool(radius);

                    if (particles == null)
                        return;
                    particles.enabled = true;
                    Vector3 e = TankBlock.centreOfMassWorld;
                    particles.UpdatePos(e.SetY(ManWater.HeightCalc));
                    surfaceExist = true;
                }
            }
            if (Special == BlockSpecial.Wheels)
                ApplyForceWheel();
        }
        private void SubmergeUpdate()
        {
            if (WaterParticleHandler.UseParticleEffects)
            {
                if (ManWater.CameraSubmerged)
                {
                    if (particles == null || !particles.Using)
                        particles = SurfacePool.GetFromPool(radius);

                    if (particles == null)
                        return;
                    particles.enabled = true;
                    var e = TankBlock.centreOfMassWorld + (UnityEngine.Random.insideUnitSphere * radius);
                    particles.UpdatePos(e);
                    surfaceExist = true;
                    particles.SetRate(Special == BlockSpecial.Props ? 3 : 1);
                }
                else
                {
                    TryRemoveSurface(true);
                }
            }
        }

        public void TryRemoveSurface(bool immedeate = false)
        {
            if (surfaceExist && particles != null)
            {
                SurfacePool.ReturnToPool(particles, immedeate);
                particles = null;
                surfaceExist = false;
            }
        }

        /// <summary>
        /// Extra special props - The props tend to perform better in the water, and in reverse too!
        /// </summary>
        public static Dictionary<BlockTypes, KeyValuePair<float, float>> SpecialProps = new Dictionary<BlockTypes, KeyValuePair<float, float>>()
        {
            { BlockTypes.GSOFan_221,
                new KeyValuePair<float, float>(2, 2)},
            { BlockTypes.VEN_Prop_Nose_Big_662,
                new KeyValuePair<float, float>(4, 8.25f)},
            { BlockTypes.VENPropSmall_111,
                new KeyValuePair<float, float>(1.5f, 2.5f)},
            { BlockTypes.VENNoseProp_331,
                new KeyValuePair<float, float>(1.25f, 2)},
            { BlockTypes.HE_Prop_331,
                new KeyValuePair<float, float>(1.5f, 3)},
            { BlockTypes.GC_Prop_665,
                new KeyValuePair<float, float>(4.5f, 7.75f)},
        };
        public void ApplyForceWheel()
        {
            Rigidbody Rbody = TankBlock.tank?.rbody;
            if (Rbody)
            {
                ModuleWheels wheels = componentEffects[0] as ModuleWheels;
                float wheelRad = initVelocities[0].x;
                float ForceApproximate = initVelocities[0].y;
                float SpeedLimit = initVelocities[0].z;
                float preval = ManWater.HeightCalc + wheelRad;
                for (int i = 0; i < wheelTracker.Count; i++)
                {
                    ManWheels.Wheel item = wheelTracker[i];
                    if (item == null)
                        throw new NullReferenceException("wheelTracker[" + i + "]");
                    Transform trans = item.wheelGeometry;
                    if (trans == null)
                        throw new NullReferenceException("wheelGeometry[" + i + "]");
                    float num = (preval - trans.position.y) / wheelRad;
                    if (num >= 2f && preval < trans.position.y + ManWater.SmallWheelWaterBuff)
                        num = 1f;
                    if (num > 0.1f && num < 2)
                    {
                        num = Mathf.Clamp(num, 0.1f, 1f);
                        int ID = (int)wheelID.GetValue(item);
                        object structInline = wheelsAll.GetValue(ID);
                        if (structInline == default)
                            throw new NullReferenceException("structInline[" + i + "]");
                        float angVelo = (float)wheelVelo.GetValue(structInline);
                        Vector3 forceHeading = Vector3.Cross(trans.right.SetY(0), Vector3.up);
                        Quaternion projection = Quaternion.LookRotation(forceHeading);
                        Quaternion projectionOrigin = Quaternion.Inverse(projection);
                        Vector3 veloAligned = projectionOrigin * Rbody.GetPointVelocity(trans.position);
                        float forceApprox;
                        float counterforce;
                        if (angVelo > 0)
                        {
                            counterforce = Mathf.Clamp01(veloAligned.z / SpeedLimit);
                            if (counterforce > 0)
                            {
                                if (counterforce == 1)
                                    forceApprox = 0;
                                else
                                    forceApprox = angVelo * num * ForceApproximate *
                                    wheels.m_TorqueParams.torqueCurveDrive.Evaluate(counterforce);
                            }
                            else
                                forceApprox = angVelo * num * ForceApproximate;
                        }
                        else
                        {
                            counterforce = Mathf.Clamp01(-veloAligned.z / SpeedLimit);
                            if (counterforce > 0)
                            {
                                if (counterforce == 1)
                                    forceApprox = 0;
                                else
                                    forceApprox = angVelo * num * ForceApproximate *
                                        wheels.m_TorqueParams.torqueCurveDrive.Evaluate(counterforce);
                            }
                            else
                                forceApprox = angVelo * num * ForceApproximate;
                        }
                        Rbody.AddForceAtPosition(forceHeading * forceApprox, trans.position, ForceMode.Force);
                    }
                }
            }
        }
        public void ApplyMultipliersFanJet()
        {
            float buffFactor = WaterGlobals.FanJetMultiplier;
            float buffFactorR = WaterGlobals.FanJetMultiplier;
            if (SpecialProps.TryGetValue(TankBlock.BlockType, out var special))
            {
                buffFactor *= special.Key;
                buffFactorR *= special.Value;
            }
            float extentsY = TankBlock.BlockCellBounds.extents.y;
            float preval = ManWater.HeightCalc + extentsY;
            for (int i = 0; componentEffects.Length > i; i++)
            {
                float num2 = (preval - componentEffects[i].transform.position.y) / extentsY + 0.1f;
                if (num2 > 0.1f)
                {
                    if (num2 > 1f)
                        num2 = 1f;
                    FanJet component = componentEffects[i] as FanJet;
                    Vector3 setter = initVelocities[i];
                    RawTechBase.thrustRate.SetValue(component, setter.x * (num2 * buffFactor + 1));
                    RawTechBase.fanThrustRateRev.SetValue(component, setter.y * (num2 * buffFactorR + 1));
                }
            }
        }

        public void ResetMultiplierFanJet()
        {
            for (int i = 0; componentEffects.Length > i; i++)
            {
                FanJet component = componentEffects[i] as FanJet;
                Vector3 setter = initVelocities[i];
                RawTechBase.thrustRate.SetValue(component, setter.x);
                RawTechBase.fanThrustRateRev.SetValue(component, setter.y);
            }
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
                if (TankBlock.rbody == null || TankBlock.rbody.IsSleeping())
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
                DebugWater.Log((watertank == null ? "WaterTank is null..." + (TankBlock.tank == null ? " And so is the tank" : "The tank is not") : "WaterTank exists") + (TankBlock.rbody == null ? "\nTankBlock Rigidbody is null" : "\nWhat?") + (TankBlock.IsAttached ? "\nThe block appears to be attached" : "\nThe block is not attached"));
            }
        }
        public void UpdateAttached(SubState state)
        {
            if (state != AttachedSubState)
            {
                switch (AttachedSubState)
                {
                    case SubState.Above:
                        switch (state)
                        {
                            case SubState.Float:
                                //TankBlock.visible.EnableOutlineGlow(true, cakeslice.Outline.OutlineEnableReason.ScriptHighlight);
                                Enter();
                                break;
                            case SubState.Below:
                                //TankBlock.visible.EnableOutlineGlow(false, cakeslice.Outline.OutlineEnableReason.ScriptHighlight);
                                Enter();
                                break;
                        }
                        break;
                    case SubState.Float:
                        switch (state)
                        {
                            case SubState.Above:
                                //TankBlock.visible.EnableOutlineGlow(false, cakeslice.Outline.OutlineEnableReason.ScriptHighlight);
                                Exit();
                                break;
                            case SubState.Below:
                                //TankBlock.visible.EnableOutlineGlow(false, cakeslice.Outline.OutlineEnableReason.ScriptHighlight);
                                //TryRemoveSurface(false);
                                break;
                        }
                        break;
                    case SubState.Below:
                        switch (state)
                        {
                            case SubState.Above:
                                //TankBlock.visible.EnableOutlineGlow(false, cakeslice.Outline.OutlineEnableReason.ScriptHighlight);
                                Exit();
                                break;
                            case SubState.Float:
                                //TankBlock.visible.EnableOutlineGlow(true, cakeslice.Outline.OutlineEnableReason.ScriptHighlight);
                                //TryRemoveSurface(false);
                                break;
                        }
                        break;
                }
                AttachedSubState = state;
            }
        }

        public void ApplySeparateForce(bool invert = false)
        {
            Vector3 centerOfMass = TankBlock.centreOfMassWorld;
            ApplyDamageIfLava(centerOfMass);
            if (!QPatch.EnableLooseBlocksFloat)
                return;
            Vector3 velo = TankBlock.rbody.velocity;
            if (centerOfMass.y - ManWater.HeightCalc > ManWater.minimumBlockSleepHeight && velo.Approximately(Vector3.zero, 0.25f))
            {
                Sleep++;
                if (Sleep > 16)
                    TankBlock.rbody.Sleep();
            }
            else
                Sleep = 0;
            //Vector3 vector = TankBlock.centreOfMassWorld;
            float Submerge = ManWater.HeightCalc - TankBlock.centreOfMassWorld.y;
            Submerge = Submerge * Mathf.Abs(Submerge) + WaterGlobals.SurfaceSkinning;
            if (Submerge > 1.5f)
            {
                TryRemoveSurface(invert);
                Submerge = 1.5f;
                SubmergeUpdate();
            }
            else if (Submerge < -0.2f)
            {
                Submerge = -0.2f;
            }
            else
            {
                SurfaceUpdate();
            }
            Vector3 counterForce = -(velo * ManWater.SurfaceBlockDampening + (Vector3.up *
                        (velo.y * ManWater.SubmergedBlockDampeningYAddition))) * Submerge;
            if (invert)
            {
                TankBlock.rbody.AddForce(Vector3.down * (Submerge * cachedFloatVal * TankBlock.filledCells.Length));
                TankBlock.rbody.AddForce(-counterForce, ForceMode.Acceleration);
            }
            else
            {
                TankBlock.rbody.AddForce(Vector3.up * (Submerge * cachedFloatVal * TankBlock.filledCells.Length));
                TankBlock.rbody.AddForce(counterForce, ForceMode.Acceleration);
            }
        }

        public void ApplyConnectedForceFullySubmerged()
        {
            IntVector3[] intVector = TankBlock.filledCells;
            int CellCount = intVector.Length;
            if (Special >= BlockSpecial.Hollow)
            {
                float bouy = watertank.FloationMode ? ManWater.BasicBlockFloatAssistMulti : 1;
                for (int CellIndex = 0; CellIndex < CellCount; CellIndex++)
                {
                    watertank.AddScaledBuoyancy(transform.TransformPoint(intVector[CellIndex]), bouy);
                    //TryRemoveSurface(invert);
                }
            }
            else
            {
                for (int CellIndex = 0; CellIndex < CellCount; CellIndex++)
                {
                    watertank.AddGeneralBuoyancy(transform.TransformPoint(intVector[CellIndex]));
                    //TryRemoveSurface(invert);
                }
            }
            switch (Special)
            {
                case BlockSpecial.None:
                    ApplyDamageIfLava(TankBlock.centreOfMassWorld);
                    break;
                case BlockSpecial.Anchor: 
                    // anchors are invulnerable to lava
                    break;
                case BlockSpecial.Wheels:
                    ApplyDamageIfLava(TankBlock.centreOfMassWorld);
                    break;
                case BlockSpecial.Props:
                    ApplyMultipliersFanJet();
                    ApplyDamageIfLava(TankBlock.centreOfMassWorld);
                    break;
                default:
                    break;
            }
        }
        public void ApplyConnectedForce()
        {
            IntVector3[] intVector = TankBlock.filledCells;
            int CellCount = intVector.Length;
            int subCount = 0;
            bool partialSub = false;
            float bouy = 1;
            if (Special >= BlockSpecial.Hollow && watertank.FloationMode)
                bouy = ManWater.BasicBlockFloatAssistMulti;
            for (int CellIndex = 0; CellIndex < CellCount; CellIndex++)
            {
                IntVector3 vector = transform.TransformPoint(intVector[CellIndex]);
                float Submerge = ManWater.HeightCalc - vector.y;
                Submerge = Submerge * Mathf.Abs(Submerge) + WaterGlobals.SurfaceSkinning;
                if (Submerge >= -0.5f)
                {
                    partialSub = true;
                    if (Submerge > 1.5f)
                    {
                        if (Special >= BlockSpecial.Hollow)
                        {
                            watertank.AddScaledBuoyancy(vector, bouy);
                        }
                        else
                        {
                            watertank.AddGeneralBuoyancy(vector);
                        }
                        //TryRemoveSurface(invert);
                        subCount++;
                    }
                    else if (Submerge < -0.2f)
                    {
                        watertank.AddScaledBuoyancy(vector, -0.2f / 1.5f);// * cachedFloatVal);
                    }
                    else
                    {
                        watertank.AddSurface(vector);
                        watertank.AddScaledBuoyancy(vector, (Submerge * bouy) / 1.5f);// * cachedFloatVal);
                    }
                }
            }
            SurfaceUpdate();
            switch (Special)
            {
                case BlockSpecial.None:
                    if (partialSub)
                        ApplyDamageIfLava(TankBlock.centreOfMassWorld);
                    break;
                case BlockSpecial.Anchor:
                    // anchors are invulnerable to lava
                    break;
                case BlockSpecial.Wheels:
                    if (partialSub)
                        ApplyDamageIfLava(TankBlock.centreOfMassWorld);
                    break;
                case BlockSpecial.Props:
                    ApplyMultipliersFanJet();
                    if (partialSub)
                        ApplyDamageIfLava(TankBlock.centreOfMassWorld);
                    break;
                default:
                    break;
            }
        }

        private void ApplyDamageIfLava(Vector3 vector)
        {
            if (QPatch.TheWaterIsLava && ManNetwork.IsHost)
            {
                if (LavaMode.DealPainThisFrame)
                {
                    TankBlock.damage.MultiplayerFakeDamagePulse();
                    float Submerge = ManWater.HeightCalc - vector.y;
                    Submerge = Submerge * Mathf.Abs(Submerge) + WaterGlobals.SurfaceSkinning;
                    if (Submerge > 1.5f)
                        Submerge = 1.5f;
                    else if (Submerge < -0.2f)
                        Submerge = -0.2f;

                    Singleton.Manager<ManDamage>.inst.DealDamage(TankBlock.GetComponent<Damageable>(), 
                        LavaMode.MeltBlocksStrength * Submerge * LavaMode.DamageUpdateDelay,
                        ManDamage.DamageType.Fire, LavaMode.inst);
                }
            }
        }


        public override void Ent(byte HeartBeat)
        {
            submergedLone.Add(this);
            if (TankBlock.rbody)
                TankBlock.rbody.angularDrag = 0.2f;
            PlaySplashSFX();
            Enter();
        }
        public void Enter()
        {
            SurfaceUpdate();
            try
            {
                var val = TankBlock.centreOfMassWorld;
                WaterParticleHandler.SplashAtPos(new Vector3(val.x, ManWater.HeightCalc + WaterParticleHandler.offsetHeightSplash, val.z),
                    (TankBlock.tank != null ? watertank.tank.rbody.GetPointVelocity(val).y : TankBlock.rbody.velocity.y),
                    TankBlock.BlockCellBounds.extents.magnitude);
            }
            catch { }
        }

        public override void Ext(byte HeartBeat)
        {
            PlaySplashSFX();
            Exit();
            if (TankBlock.rbody)
                TankBlock.rbody.angularDrag = 0f;
            submergedLone.Remove(this);
        }
        public void Exit()
        {
            TryRemoveSurface();
            try
            {
                if (Special == BlockSpecial.Props)
                    ResetMultiplierFanJet();

                var val = TankBlock.centreOfMassWorld;
                WaterParticleHandler.SplashAtPos(new Vector3(val.x, ManWater.HeightCalc + WaterParticleHandler.offsetHeightSplash, val.z),
                    (TankBlock.tank != null ? watertank.tank.rbody.GetPointVelocity(val).y : TankBlock.rbody.velocity.y),
                    radius);
            }
            catch { }
        }
    }
}
