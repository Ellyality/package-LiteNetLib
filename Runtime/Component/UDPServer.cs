using LiteNetLib;
using LiteNetLib.Utils;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.Events;

namespace Elly.LiteNetLib
{
    [AddComponentMenu("Ellyality/Network/UDP Server")]
    public class UDPServer : MonoBehaviour, INetEventListener
    {
        [Header("Setting")]
        [SerializeField] [Tooltip("It will trigger Debug.Log after received events")] bool Log = true;
        [SerializeField] [Tooltip("If leave empty, it will accept any connection, if not, it will check if key is matching")] string Key = "TestKey";
        [SerializeField] [Tooltip("Server use port")] int Port = 9055;
        [SerializeField] [Tooltip("Send feedback for the broadcast message, let client know where the server is")] bool AutoConnectBroadcastClient = true;
        [SerializeField] [Tooltip("It will call StartServer")] bool StartAtBegining = true;
        [Header("Config")]
        [SerializeField] bool BroadcastReceiveEnabled = true;
        [SerializeField] bool UnconnectedMessagesEnabled = true;
        [Header("Events")]
        [SerializeField] UnityEvent<ReceiveData> ReceivedEvent = new UnityEvent<ReceiveData>();
        [SerializeField] UnityEvent<ReceiveUnconnectedData> ReceiveUnconnectedEvent = new UnityEvent<ReceiveUnconnectedData>();
        [SerializeField] UnityEvent<ErrorData> ErrorEvent = new UnityEvent<ErrorData>();
        [SerializeField] UnityEvent<DisconnectData> DisconnectEvent = new UnityEvent<DisconnectData>();
        [SerializeField] UnityEvent<ConnectData> ConnectEvent = new UnityEvent<ConnectData>();

        protected NetManager netManager { set; get; }
        public int ConnectionCount => netManager != null ? netManager.ConnectedPeersCount : 0;

        protected virtual void Awake()
        {
            netManager = new NetManager(this);
        }

        protected virtual void Start()
        {
            if (StartAtBegining) StartServer();
        }

        protected virtual void Update()
        {
            netManager.PollEvents();
        }
        
        public void StartServer()
        {
            netManager.BroadcastReceiveEnabled = BroadcastReceiveEnabled;
            netManager.UnconnectedMessagesEnabled = UnconnectedMessagesEnabled;
            netManager.Start(Port);
            if (Log) Debug.Log($"[LiteNetLib] Server start");
        }

        public void ShutdownServer()
        {
            netManager.Stop();
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
            if (Log) Debug.Log($"[LiteNetLib] Request");
            if (Key.Length > 0)
            {
                request.AcceptIfKey(Key);
            }
            else
            {
                request.Accept();
            }
        }

        public virtual void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            if (Log) Debug.Log($"[LiteNetLib] Error: {endPoint.Address.ToString()} {socketError}");
            ErrorEvent.Invoke(new ErrorData() { endPoint = endPoint, socketError = socketError });
        }

        public virtual void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            
        }

        public virtual void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            ReceivedEvent.Invoke(new ReceiveData() { peer = peer, reader = reader, channelNumber = channelNumber, deliveryMethod = deliveryMethod });
        }

        public virtual void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            if (Log) Debug.Log($"[LiteNetLib] ReceiveUnconnected: {remoteEndPoint.Address.ToString()} {messageType}");
            ReceiveUnconnectedEvent.Invoke(new ReceiveUnconnectedData() { remoteEndPoint = remoteEndPoint, reader = reader, messageType = messageType });
            if (AutoConnectBroadcastClient)
            {
                if (messageType == UnconnectedMessageType.Broadcast)
                {
                    Debug.Log("[LiteNetLib] Received discovery request. Send discovery response");
                    NetDataWriter resp = new NetDataWriter();
                    resp.Put(1);
                    netManager.SendUnconnectedMessage(resp, remoteEndPoint);
                }
            }
        }

        public virtual void OnPeerConnected(NetPeer peer)
        {
            if (Log) Debug.Log($"[LiteNetLib] Connected: {peer.Id} {peer.Address.ToString()}");
            ConnectEvent.Invoke(new ConnectData() { peer = peer });
        }

        public virtual void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            if (Log) Debug.Log($"[LiteNetLib] Disconnect: {peer.Id} {peer.Address.ToString()} {disconnectInfo.Reason}");
            DisconnectEvent.Invoke(new DisconnectData() { peer = peer, disconnectInfo = disconnectInfo });
        }
    }
}
