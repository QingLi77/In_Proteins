using System;
using BeardedManStudios.Forge.Networking;

namespace Dissonance.Integrations.ForgeNetworkingRemastered
{
    public struct ForgeRemasteredPeer
        : IEquatable<ForgeRemasteredPeer>
    {
        public readonly NetworkingPlayer NetworkingPlayer;

        public ForgeRemasteredPeer(NetworkingPlayer networkingPlayer)
        {
            NetworkingPlayer = networkingPlayer;
        }

        public bool Equals(ForgeRemasteredPeer other)
        {
            return NetworkingPlayer.Equals(other.NetworkingPlayer);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (obj.GetType() != GetType())
                return false;
            return Equals((ForgeRemasteredPeer)obj);
        }

        public override int GetHashCode()
        {
            return NetworkingPlayer.GetHashCode();
        }

        public static bool operator ==(ForgeRemasteredPeer left, ForgeRemasteredPeer right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ForgeRemasteredPeer left, ForgeRemasteredPeer right)
        {
            return !Equals(left, right);
        }
    }
}
