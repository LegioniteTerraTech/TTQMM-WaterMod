using System;
using HarmonyLib;

namespace WaterMod
{
    static class NetworkHandler
    {
        static UnityEngine.Networking.NetworkInstanceId Host;
        static bool HostExists = false;

        const TTMsgType WaterChange = (TTMsgType)228;
        const TTMsgType WaterTypeChange = (TTMsgType)2263;
        const TTMsgType WaterSettingsChange = (TTMsgType)2264;

        private static float serverWaterHeight = -1000f;
        private static bool serverLava = false;

        public static float ServerWaterHeight
        {
            get { return serverWaterHeight; }
            set
            {
                serverWaterHeight = value;
                TryBroadcastNewHeight(serverWaterHeight);
            }
        }
        public static bool ServerLava
        {
            get { return serverLava; }
            set
            {
                serverLava = value;
                TryBroadcastLavaState(serverLava);
            }
        }

        public class WaterChangeMessage : UnityEngine.Networking.MessageBase
        {
            public WaterChangeMessage() { }
            public WaterChangeMessage(float Height)
            {
                this.Height = Height;
            }
            public override void Deserialize(UnityEngine.Networking.NetworkReader reader)
            {
                this.Height = reader.ReadSingle();
            }

            public override void Serialize(UnityEngine.Networking.NetworkWriter writer)
            {
                writer.Write(this.Height);
            }

            public float Height;
        }

        public static void TryBroadcastNewHeight(float Water)
        {
            if (HostExists) try
                {
                    Singleton.Manager<ManNetwork>.inst.SendToAllClients(WaterChange, new WaterChangeMessage(Water), Host);
                    Console.WriteLine("Sent new water level to all");
                }
                catch { Console.WriteLine("Failed to send new water level..."); }
        }
        public static void OnClientChangeWaterHeight(UnityEngine.Networking.NetworkMessage netMsg)
        {
            var reader = new WaterChangeMessage();
            netMsg.ReadMessage(reader);
            serverWaterHeight = reader.Height;
            Console.WriteLine("Received new water level, changing to " + serverWaterHeight.ToString());
        }

        public class LavaStateMessage : UnityEngine.Networking.MessageBase
        {
            public LavaStateMessage() { }
            public LavaStateMessage(bool isLava)
            {
                this.IsLava = isLava;
            }
            public override void Deserialize(UnityEngine.Networking.NetworkReader reader)
            {
                this.IsLava = reader.ReadBoolean();
            }

            public override void Serialize(UnityEngine.Networking.NetworkWriter writer)
            {
                writer.Write(this.IsLava);
            }

            public bool IsLava;
        }
        public static void TryBroadcastLavaState(bool isLava)
        {
            if (HostExists) try
                {
                    Singleton.Manager<ManNetwork>.inst.SendToAllClients(WaterTypeChange, new LavaStateMessage(isLava), Host);
                    Console.WriteLine("Sent new lava state to all");
                }
                catch { Console.WriteLine("Failed to send lava state..."); }
        }
        public static void OnClientChangeLavaState(UnityEngine.Networking.NetworkMessage netMsg)
        {
            var reader = new LavaStateMessage();
            netMsg.ReadMessage(reader);
            serverLava = reader.IsLava;
            if (serverLava)
            {
                LavaMode.ScreamLava();
                //Singleton.Manager<ManSFX>.inst.PlayUISFX(ManSFX.UISfxType.PayloadIncoming);
            }
            ManWater.UpdateLook();
            Console.WriteLine("Received new lava state, changing to " + serverLava.ToString());
        }

        public class WaterSettingsMessage : UnityEngine.Networking.MessageBase
        {
            public WaterSettingsMessage() { }
            /// <summary>
            /// This SENDS it from <see cref="WaterGlobals"/>
            /// </summary>
            public WaterSettingsMessage(bool doSendAlways)
            {
                SimulateProjectiles = WaterGlobals.SimulateProjectiles;
                EnableLooseBlocksFloat = WaterGlobals.EnableLooseBlocksFloat;
                OceanMan2 = WaterGlobals.OceanMan2;
                DestroyTreesInWater = WaterGlobals.DestroyTreesInWater;
                Density = WaterGlobals.Density;
                FanJetMultiplier = WaterGlobals.FanJetMultiplier;
                ResourceBuoyancyMultiplier = WaterGlobals.ResourceBuoyancyMultiplier;
                BulletDampener = WaterGlobals.BulletDampener;
                LaserFraction = WaterGlobals.LaserFraction;
                MissileDampener = WaterGlobals.MissileDampener;
                SurfaceSkinning = WaterGlobals.SurfaceSkinning;
                SubmergedTankDampening = WaterGlobals.SubmergedTankDampening;
                SubmergedTankDampeningYAddition = WaterGlobals.SubmergedTankDampeningYAddition;
                SurfaceTankDampening = WaterGlobals.SurfaceTankDampening;
                SurfaceTankDampeningYAddition = WaterGlobals.SurfaceTankDampeningYAddition;
                RainWeightMultiplier = WaterGlobals.RainWeightMultiplier;
                RainDrainMultiplier = WaterGlobals.RainDrainMultiplier;
                FloodChangeClamp = WaterGlobals.FloodChangeClamp;
                AbyssDepth = WaterGlobals.AbyssDepth;
                LavaDampenMulti = WaterGlobals.LavaDampenMulti;
                WheelWaterForceMultiplier = WaterGlobals.WheelWaterForceMultiplier;
            }
            public void ApplyThisToClient()
            {
                WaterGlobals.SimulateProjectiles = SimulateProjectiles;
                WaterGlobals.EnableLooseBlocksFloat = EnableLooseBlocksFloat;
                WaterGlobals.OceanMan2 = OceanMan2;
                WaterGlobals.DestroyTreesInWater = DestroyTreesInWater;
                WaterGlobals.Density = Density;
                WaterBlock.cachedFloatVal = Density * 5f;
                WaterGlobals.FanJetMultiplier = FanJetMultiplier;
                WaterGlobals.ResourceBuoyancyMultiplier = ResourceBuoyancyMultiplier;
                WaterGlobals.BulletDampener = BulletDampener;
                WaterGlobals.LaserFraction = LaserFraction;
                WaterGlobals.MissileDampener = MissileDampener;
                WaterGlobals.SurfaceSkinning = SurfaceSkinning;
                WaterGlobals.SubmergedTankDampening = SubmergedTankDampening;
                WaterGlobals.SubmergedTankDampeningYAddition = SubmergedTankDampeningYAddition;
                WaterGlobals.SurfaceTankDampening = SurfaceTankDampening;
                WaterGlobals.SurfaceTankDampeningYAddition = SurfaceTankDampeningYAddition;
                WaterGlobals.RainWeightMultiplier = RainWeightMultiplier;
                WaterGlobals.RainDrainMultiplier = RainDrainMultiplier;
                WaterGlobals.FloodChangeClamp = FloodChangeClamp;
                WaterGlobals.AbyssDepth = AbyssDepth;
                WaterGlobals.LavaDampenMulti = LavaDampenMulti;
                WaterGlobals.WheelWaterForceMultiplier = WheelWaterForceMultiplier;
            }
            /// <summary> SERVER SETTINGS </summary>
            public bool SimulateProjectiles = true;
            /// <summary> SERVER SETTINGS </summary>
            public bool EnableLooseBlocksFloat = true;
            /// <summary> SERVER SETTINGS </summary>
            public bool OceanMan2 = false;
            /// <summary> SERVER SETTINGS </summary>
            public bool DestroyTreesInWater = false;

            public float Density = 8,
                FanJetMultiplier = 1.75f,
                ResourceBuoyancyMultiplier = 1.2f,
                BulletDampener = 1E-06f,
                LaserFraction = 0.275f,
                MissileDampener = 0.012f,
                SurfaceSkinning = 0.25f,
                SubmergedTankDampening = 0.4f,
                SubmergedTankDampeningYAddition = 0f,
                SurfaceTankDampening = 0f,
                SurfaceTankDampeningYAddition = 1f,
                RainWeightMultiplier = 0.06f,
                RainDrainMultiplier = 0.06f,
                FloodChangeClamp = 0.002f,
                AbyssDepth = 50f,
                LavaDampenMulti = 3,
                WheelWaterForceMultiplier = 0.45f;
        }
        public static void TryBroadcastSettingsState()
        {
            if (HostExists) try
                {
                    Singleton.Manager<ManNetwork>.inst.SendToAllClients(WaterTypeChange, new WaterSettingsMessage(true), Host);
                    Console.WriteLine("Sent new settings state to all");
                }
                catch { Console.WriteLine("Failed to send settings state..."); }
        }
        public static void OnClientChangeSettingsState(UnityEngine.Networking.NetworkMessage netMsg)
        {
            var reader = new WaterSettingsMessage();
            netMsg.ReadMessage(reader);
            reader.ApplyThisToClient();
            if (serverLava)
            {
                LavaMode.ScreamLava();
                //Singleton.Manager<ManSFX>.inst.PlayUISFX(ManSFX.UISfxType.PayloadIncoming);
            }
            ManWater.UpdateLook();
            Console.WriteLine("Received new lava state, changing to " + serverLava.ToString());
        }

        public static class Patches
        {
            //[HarmonyPatch(typeof(ManLooseBlocks), "RegisterMessageHandlers")]
            //static class CreateWaterHooks
            //{
            //    static void Postfix
            //}

            [HarmonyPatch(typeof(NetPlayer), "OnRecycle")]
            static class OnRecycle
            {
                static void Postfix(NetPlayer __instance)
                {
                    if (__instance.isServer || __instance.isLocalPlayer)
                    {
                        serverWaterHeight = -1000f;
                        serverLava = false;
                        Console.WriteLine("Discarded " + __instance.netId.ToString() + " and reset server water level");
                        HostExists = false;
                    }
                }
            }

            [HarmonyPatch(typeof(NetPlayer), "OnStartClient")]
            static class OnStartClient
            {
                static void Postfix(NetPlayer __instance)
                {
                    Singleton.Manager<ManNetwork>.inst.SubscribeToClientMessage(__instance.netId, WaterChange, new ManNetwork.MessageHandler(OnClientChangeWaterHeight));
                    Singleton.Manager<ManNetwork>.inst.SubscribeToClientMessage(__instance.netId, WaterTypeChange, new ManNetwork.MessageHandler(OnClientChangeLavaState));
                    Singleton.Manager<ManNetwork>.inst.SubscribeToClientMessage(__instance.netId, WaterSettingsChange, new ManNetwork.MessageHandler(OnClientChangeSettingsState));
                    Console.WriteLine("Subscribed " + __instance.netId.ToString() + " to water level updates from host. Sending current level");
                    TryBroadcastSettingsState();
                    TryBroadcastNewHeight(serverWaterHeight);
                    TryBroadcastLavaState(serverLava);
                }
            }

            [HarmonyPatch(typeof(NetPlayer), "OnStartServer")]
            static class OnStartServer
            {
                static void Postfix(NetPlayer __instance)
                {
                    if (!HostExists)
                    {
                        serverWaterHeight = -1000f;
                        serverLava = false;
                        //Singleton.Manager<ManNetwork>.inst.SubscribeToServerMessage(__instance.netId, WaterChange, new ManNetwork.MessageHandler(OnServerChangeWaterHeight));
                        Console.WriteLine("Host started, hooked water level broadcasting to " + __instance.netId.ToString());
                        Host = __instance.netId;
                        HostExists = true;
                    }
                }
            }
        }
    }
}
