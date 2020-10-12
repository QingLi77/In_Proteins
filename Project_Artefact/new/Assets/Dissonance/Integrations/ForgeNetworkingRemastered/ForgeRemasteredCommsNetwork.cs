using System;
using System.Collections.Generic;
using BeardedManStudios.Forge.Networking.Unity;
using Dissonance.Datastructures;
using Dissonance.Extensions;
using Dissonance.Networking;
using JetBrains.Annotations;
using UnityEngine;

namespace Dissonance.Integrations.ForgeNetworkingRemastered
{
    public class ForgeRemasteredCommsNetwork
        : BaseCommsNetwork<ForgeRemasteredServer, ForgeRemasteredClient, ForgeRemasteredPeer, Unit, Unit>
    {
        #region fields and properties
        [SerializeField, UsedImplicitly] private int _voiceDataChannelToServer = 57729876;
        [SerializeField, UsedImplicitly] private int _systemMessagesChannelToServer = 57729877;
        [SerializeField, UsedImplicitly] private int _voiceDataChannelToClient = 57729878;
        [SerializeField, UsedImplicitly] private int _systemMessagesChannelToClient = 57729879;

        public int VoiceDataChannelToServer
        {
            get { return _voiceDataChannelToServer; }
        }

        public int SystemMessagesChannelToServer
        {
            get { return _systemMessagesChannelToServer; }
        }

        public int VoiceDataChannelToClient
        {
            get { return _voiceDataChannelToClient; }
        }

        public int SystemMessagesChannelToClient
        {
            get { return _systemMessagesChannelToClient; }
        }

        private readonly ConcurrentPool<byte[]> _loopbackBuffers = new ConcurrentPool<byte[]>(8, () => new byte[1024]);
        private readonly List<ArraySegment<byte>> _loopbackQueue = new List<ArraySegment<byte>>();
        #endregion

        protected override ForgeRemasteredClient CreateClient(Unit connectionParameters)
        {
            return new ForgeRemasteredClient(this);
        }

        protected override ForgeRemasteredServer CreateServer(Unit connectionParameters)
        {
            return new ForgeRemasteredServer(this);
        }

        protected override void Update()
        {
            // Check if Dissonance is ready
            if (IsInitialized)
            {
                var server = NetworkManager.Instance.IsServer;
                var client = true; //FNR has no equivalent to this call: `NetworkManager.Instance.IsClient`

                // Check if FNR is ready
                var networkActive = NetworkManager.Instance.Networker.IsConnected && (server || client);
                if (networkActive)
                {
                    // Check what mode Dissonance is in, if it's not the same as FNR then call the correct method
                    if (Mode.IsServerEnabled() != server || Mode.IsClientEnabled() != client)
                    {
                        // FNR is server and client, so run as a non dedicated host (passing in the correct parameters, in this case nothing)
                        if (server && client)
                            RunAsHost(Unit.None, Unit.None);

                        // FNR is just a server, so run as a dedicated host
                        else if (server)
                            RunAsDedicatedServer(Unit.None);

                        // FNR is just a client, so run as a client
                        else if (client)
                            RunAsClient(Unit.None);
                    }
                }
                else if (Mode != NetworkMode.None)
                {
                    //Network is not active, so make sure Dissonance is not active too
                    Stop();
                }

                //Send looped back packets
                for (var i = 0; i < _loopbackQueue.Count; i++)
                {
                    if (Client != null)
                        Client.NetworkReceivedPacket(_loopbackQueue[i]);

                    // Recycle the packet into the pool of byte buffers
                    // ReSharper disable once AssignNullToNotNullAttribute (Justification: ArraySegment array is not null)
                    _loopbackBuffers.Put(_loopbackQueue[i].Array);
                }
                _loopbackQueue.Clear();
            }

            base.Update();
        }

        internal bool PreprocessPacketToClient(ArraySegment<byte> packet, ForgeRemasteredPeer dest)
        {
            //This should never even be called if this peer is not the host!
            if (Server == null)
                throw Log.CreatePossibleBugException("server packet preprocessing running, but this peer is not a server", "55F5F5D8-8E30-4810-8453-C5D13915118A");

            //If there is no local client (e.g. this is a dedicated server) then there can't possibly be loopback
            var client = Client;
            if (client == null)
                return false;

            //Is this loopback?
            if (!dest.Equals(new ForgeRemasteredPeer(NetworkManager.Instance.Networker.Me)))
                return false;

            //This is loopback!
            
            // Don't immediately deliver the packet, add it to a queue and deliver it next frame. This prevents the local client from executing "within" ...
            // ...the local server which can cause confusing stack traces.
            _loopbackQueue.Add(packet.CopyTo(_loopbackBuffers.Get()));

            return true;
        }

        internal bool PreprocessPacketToServer(ArraySegment<byte> packet)
        {
            //I have no idea if Forge handles loopback. Whether it does or does not isn't important though - it's more
            //efficient to handle the loopback special case directly instead of passing through the entire network system!

            //This should never even be called if this peer is not a client!
            var client = Client;
            if (client == null)
                throw Log.CreatePossibleBugException("client packet processing running, but this peer is not a client", "DC545FE4-4F5E-4DEB-B441-CFC847140477");

            //Is this loopback?
            if (Server == null)
                return false;

            //This is loopback!
            //Since this is loopback destination == source (by definition)
            Server.NetworkReceivedPacket(new ForgeRemasteredPeer(NetworkManager.Instance.Networker.Me), packet);

            return true;
        }
    }
}
