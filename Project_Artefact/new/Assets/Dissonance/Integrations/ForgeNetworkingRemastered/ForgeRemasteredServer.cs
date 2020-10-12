using System;
using System.Collections.Generic;
using BeardedManStudios;
using BeardedManStudios.Concurrency;
using BeardedManStudios.Forge.Networking;
using BeardedManStudios.Forge.Networking.Frame;
using BeardedManStudios.Forge.Networking.Unity;
using Dissonance.Datastructures;
using Dissonance.Networking;
using Dissonance.Networking.Server;
using JetBrains.Annotations;

namespace Dissonance.Integrations.ForgeNetworkingRemastered
{
    public class ForgeRemasteredServer
        : BaseServer<ForgeRemasteredServer, ForgeRemasteredClient, ForgeRemasteredPeer>
    {
        private readonly ForgeRemasteredCommsNetwork _net;
        private readonly BMSByte _sendBuffer = new BMSByte();

        private NetWorker _networker;

        private readonly ConcurrentPool<byte[]> _receivedMarshalBuffers = new ConcurrentPool<byte[]>(8, () => new byte[1024]);
        private readonly ConcurrentQueue<KeyValuePair<ForgeRemasteredPeer, ArraySegment<byte>>> _receivedMarshalQueue = new ConcurrentQueue<KeyValuePair<ForgeRemasteredPeer, ArraySegment<byte>>>();

        private readonly ConcurrentQueue<ForgeRemasteredPeer> _playerDisconnectedMarshalQueue = new ConcurrentQueue<ForgeRemasteredPeer>();

        public ForgeRemasteredServer(ForgeRemasteredCommsNetwork net)
        {
            _net = net;
        }

        public override void Connect()
        {
            _networker = NetworkManager.Instance.Networker;
            _networker.binaryMessageReceived += ForgeNetworkMessageReceived;
            _networker.playerDisconnected += ForgePlayerDisconnected;

            if (!(_networker is BaseUDP))
                Log.Error("Forge Networking Remastered client is not a UDP client! Dissonance Voice will still function but quality will be severly degraded");

            base.Connect();
        }

        private void ForgePlayerDisconnected(NetworkingPlayer player, NetWorker networker)
        {
            _playerDisconnectedMarshalQueue.Enqueue(new ForgeRemasteredPeer(player));
        }

        public override void Disconnect()
        {
            base.Disconnect();
            _networker.binaryMessageReceived -= ForgeNetworkMessageReceived;
            _networker.playerDisconnected -= ForgePlayerDisconnected;
        }

        #region receive
        private void ForgeNetworkMessageReceived(NetworkingPlayer player, [NotNull] FrameStream frame, NetWorker networker)
        {
            if (frame.GroupId != _net.VoiceDataChannelToServer && frame.GroupId != _net.SystemMessagesChannelToServer)
                return;

            // This event does not get invoked on the main thread, copy the data into a buffer and queue it up for Dissonance to read later

            // Copy Forge data into a temporary buffer
            var b = _receivedMarshalBuffers.Get();
            var c = Math.Min(1024, frame.StreamData.Size);
            Buffer.BlockCopy(frame.StreamData.byteArr, frame.StreamData.StartPointer, b, 0, c);

            // Enqueue data buffer to be read by Dissonance
            _receivedMarshalQueue.Enqueue(new KeyValuePair<ForgeRemasteredPeer, ArraySegment<byte>>(new ForgeRemasteredPeer(player), new ArraySegment<byte>(b, 0, c)));
        }

        protected override void ReadMessages()
        {
            //messages are received in event handler, no work is needed here
        }

        public override ServerState Update()
        {
            // Read player disconnection events delivered by forge in the other thread
            ForgeRemasteredPeer disc;
            while (_playerDisconnectedMarshalQueue.TryDequeue(out disc))
                ClientDisconnected(disc);

            // Read packets delivered by forge in the other thread
            KeyValuePair<ForgeRemasteredPeer, ArraySegment<byte>> data;
            while (_receivedMarshalQueue.TryDequeue(out data))
            {
                NetworkReceivedPacket(data.Key, data.Value);

                // Recycle the buffer
                // ReSharper disable once AssignNullToNotNullAttribute
                _receivedMarshalBuffers.Put(data.Value.Array);
            }

            return base.Update();
        }
        #endregion

        #region send
        private void Send(ForgeRemasteredPeer peer, ArraySegment<byte> packet, int channel, bool reliable)
        {
            if (_net.PreprocessPacketToClient(packet, peer))
                return;

            _sendBuffer.Clear();
            _sendBuffer.BlockCopy(packet.Array, packet.Offset, packet.Count);

            var message = new Binary(
                _networker.Time.Timestep,
                _networker is BaseTCP,
                _sendBuffer,
                Receivers.Target,
                channel,
                _networker is BaseTCP
            );

            var udp = _networker as UDPServer;
            if (udp != null)
                udp.Send(peer.NetworkingPlayer, message, reliable);
            else
                ((TCPServer)_networker).Send(peer.NetworkingPlayer.TcpClientHandle, message);
        }

        protected override void SendUnreliable(ForgeRemasteredPeer connection, ArraySegment<byte> packet)
        {
            Send(connection, packet, _net.SystemMessagesChannelToClient, false);
        }

        protected override void SendReliable(ForgeRemasteredPeer connection, ArraySegment<byte> packet)
        {
            Send(connection, packet, _net.SystemMessagesChannelToClient, true);
        }
        #endregion
    }
}
