using System;
using BeardedManStudios;
using BeardedManStudios.Concurrency;
using BeardedManStudios.Forge.Networking;
using BeardedManStudios.Forge.Networking.Frame;
using BeardedManStudios.Forge.Networking.Unity;
using Dissonance.Datastructures;
using Dissonance.Networking;
using Dissonance.Networking.Client;
using JetBrains.Annotations;

namespace Dissonance.Integrations.ForgeNetworkingRemastered
{
    public class ForgeRemasteredClient
        : BaseClient<ForgeRemasteredServer, ForgeRemasteredClient, ForgeRemasteredPeer>
    {
        private readonly ForgeRemasteredCommsNetwork _net;
        private readonly BMSByte _sendBuffer = new BMSByte();

        private NetWorker _networker;

        private readonly ConcurrentPool<byte[]> _receivedMarshalBuffers = new ConcurrentPool<byte[]>(8, () => new byte[1024]);
        private readonly ConcurrentQueue<ArraySegment<byte>> _receivedMarshalQueue = new ConcurrentQueue<ArraySegment<byte>>();

        public ForgeRemasteredClient([NotNull] ForgeRemasteredCommsNetwork net)
            : base(net)
        {
            _net = net;
        }

        public override void Connect()
        {
            _networker = NetworkManager.Instance.Networker;
            _networker.binaryMessageReceived += ForgeNetworkMessageReceived;

            Connected();
        }

        public override void Disconnect()
        {
            if (_networker != null)
                _networker.binaryMessageReceived -= ForgeNetworkMessageReceived;
            _networker = null;

            base.Disconnect();
        }

        #region receive
        private void ForgeNetworkMessageReceived(NetworkingPlayer player, [NotNull] FrameStream frame, NetWorker networker)
        {
            if (frame.GroupId != _net.VoiceDataChannelToClient && frame.GroupId != _net.SystemMessagesChannelToClient)
                return;

            // This event does not get invoked on the main thread, copy the data into a buffer and queue it up for Dissonance to read later

            // Copy Forge data into a temporary buffer
            var b = _receivedMarshalBuffers.Get();
            var c = Math.Min(1024, frame.StreamData.Size);
            Buffer.BlockCopy(frame.StreamData.byteArr, frame.StreamData.StartPointer, b, 0, c);

            // Enqueue data buffer to be read by Dissonance
            _receivedMarshalQueue.Enqueue(new ArraySegment<byte>(b, 0, c));
        }

        protected override void ReadMessages()
        {
            //messages are received in event handler, no work is needed here
        }

        public override ClientStatus Update()
        {
            // Read packets delivered by forge in the other thread
            ArraySegment<byte> segment;
            while (_receivedMarshalQueue.TryDequeue(out segment))
            {
                NetworkReceivedPacket(segment);

                // Recycle the buffer
                // ReSharper disable once AssignNullToNotNullAttribute
                _receivedMarshalBuffers.Put(segment.Array);
            }

            return base.Update();
        }
        #endregion

        #region send
        private void Send(ArraySegment<byte> packet, int channel, bool reliable)
        {
            if (_net.PreprocessPacketToServer(packet))
                return;

            _sendBuffer.Clear();
            _sendBuffer.BlockCopy(packet.Array, packet.Offset, packet.Count);

            var message = new Binary(
                _networker.Time.Timestep,
                _networker is BaseTCP,
                _sendBuffer,
                Receivers.Server,
                channel,
                _networker is BaseTCP
            );

            var udp = _networker as BaseUDP;
            if (udp != null)
            {
                udp.Send(message, reliable);
            }
            else
            {
                var host = FindHost();

                ((TCPServer)_networker).Send(host.TcpClientHandle, message);
            }
        }

        [NotNull] private static NetworkingPlayer FindHost()
        {
            for (int i = 0; i < NetworkManager.Instance.Networker.Players.Count; i++)
            {
                var p = NetworkManager.Instance.Networker.Players[i];
                if (p.IsHost)
                    return p;
            }

            throw new DissonanceException("Cannot find a host in the Forge networking session");
        }

        protected override void SendUnreliable(ArraySegment<byte> packet)
        {
            Send(packet, _net.SystemMessagesChannelToServer, false);
        }

        protected override void SendReliable(ArraySegment<byte> packet)
        {
            Send(packet, _net.SystemMessagesChannelToServer, true);
        }
        #endregion
    }
}
