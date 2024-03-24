using LiteNetLib;
using System.Net;
using System.Net.Sockets;

namespace Elly.LiteNetLib
{
    [System.Serializable]
    public struct ReceiveData
    {
        public NetPeer peer;
        public NetPacketReader reader;
        public byte channelNumber;
        public DeliveryMethod deliveryMethod;
    }

    [System.Serializable]
    public struct ReceiveUnconnectedData
    {
        public IPEndPoint remoteEndPoint;
        public NetPacketReader reader;
        public UnconnectedMessageType messageType;
    }

    [System.Serializable]
    public struct ErrorData
    {
        public IPEndPoint endPoint;
        public SocketError socketError;
    }

    [System.Serializable]
    public struct DisconnectData
    {
        public NetPeer peer;
        public DisconnectInfo disconnectInfo;
    }

    [System.Serializable]
    public struct ConnectData
    {
        public NetPeer peer;
    }
}
