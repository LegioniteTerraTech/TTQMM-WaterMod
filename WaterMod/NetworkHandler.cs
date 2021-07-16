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
            WaterBuoyancy.UpdateLook(WaterBuoyancy.waterLooks[WaterBuoyancy.SelectedLook]);
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
                    Console.WriteLine("Subscribed " + __instance.netId.ToString() + " to water level updates from host. Sending current level");
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
