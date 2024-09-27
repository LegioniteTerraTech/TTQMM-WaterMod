using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace WaterMod
{
    internal class WaterEffect : MonoBehaviour
    {
        public virtual void Ent(byte HeartBeat)
        {
        }

        public virtual void Ext(byte HeartBeat)
        {
        }

        public virtual void Stay(byte HeartBeat)
        {
        }
    }
    internal class WaterEffectFast : MonoBehaviour
    {
        protected WaterEffectFast()
        {
            trans = transform;
        }
        private Transform trans;
        private SubState VarSubState = SubState.Above;
        protected void RemoteUpdate()
        {
            float height = trans.position.y;
            if (height> ManWater.HeightCalc)
            {
                UpdateAttached(SubState.Above);
            }
            else if (height > ManWater.HeightCalc)
            {
                UpdateAttached(SubState.Float);
            }
            else
            {
                UpdateAttached(SubState.Below);
            }
        }
        protected void UpdateAttached(SubState state)
        {
            if (state != VarSubState)
            {
                switch (VarSubState)
                {
                    case SubState.Above:
                        switch (state)
                        {
                            case SubState.Float:
                                Enter();
                                break;
                            case SubState.Below:
                                Enter();
                                break;
                        }
                        break;
                    case SubState.Float:
                        switch (state)
                        {
                            case SubState.Above:
                                Exit();
                                break;
                            case SubState.Below:
                                break;
                        }
                        break;
                    case SubState.Below:
                        switch (state)
                        {
                            case SubState.Above:
                                Exit();
                                break;
                            case SubState.Float:
                                break;
                        }
                        break;
                }
                VarSubState = state;
            }
        }
        public void DisableCollideWithWater()
        {
            foreach (var item in GetComponentsInChildren<Collider>(true))
                Physics.IgnoreCollision(ManWater.seaCol, item);
        }
        public virtual void Enter()
        {
        }

        public virtual void Exit()
        {
        }

        public virtual void Stay()
        {
        }
    }
}
