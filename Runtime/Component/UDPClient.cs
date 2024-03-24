using LiteNetLib;
using System.Net.Sockets;
using System.Net;
using UnityEngine;
using UnityEngine.Events;

namespace Elly.LiteNetLib
{
    [AddComponentMenu("Ellyality/Network/UDP Client")]
    public class UDPClient : MonoBehaviour, INetEventListener
    {
        [Header("Setting")]
        [SerializeField][Tooltip("It will trigger Debug.Log after received events")] bool Log = true;
        [SerializeField][Tooltip("It will be the key it submit to the udp server")] string Key = "TestKey";
        [SerializeField][Tooltip("Server use port")] int Port = 9055;
        [SerializeField][Tooltip("If there is not peer happening, then it will keep sending broadcasting signal out")] bool BroadcastUtilConnect = true;
        [SerializeField][Tooltip("If received server's feedback and client hasn't connect to any server, " +
            "it will trying to connect the remote where it send the feedback")] bool AutoConnectByBroadcastFeedback = true;
        [SerializeField][Tooltip("It will call StartClient")] bool StartAtBegining = true;
        [Header("Config")]
        [SerializeField] bool BroadcastReceiveEnabled = false;
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
            netManager.UpdateTime = 15;
        }

        protected virtual void Start()
        {
            if (StartAtBegining) StartClient();
        }

        protected virtual void Update()
        {
            netManager.PollEvents();
            var peer = netManager.FirstPeer;
            if (peer == null || peer.ConnectionState == ConnectionState.Disconnected)
            {
                netManager.SendBroadcast(new byte[1] { 1 }, Port);
            }
        }

        public void StartClient()
        {
            netManager.BroadcastReceiveEnabled = BroadcastReceiveEnabled;
            netManager.UnconnectedMessagesEnabled = UnconnectedMessagesEnabled;
            netManager.UpdateTime = 15;
            netManager.Start();
            if (Log) Debug.Log($"[LiteNetLib] Client start");
        }

        public void Connect(string address)
        {
            netManager.Connect(address, Port, Key);
            if (Log) Debug.Log($"[LiteNetLib] Trying connect {address}");
        }

        public void Disconnect()
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
            if (AutoConnectByBroadcastFeedback)
            {
                if (messageType == UnconnectedMessageType.BasicMessage && netManager.ConnectedPeersCount == 0 && reader.GetInt() == 1)
                {
                    netManager.Connect(remoteEndPoint, Key);
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
