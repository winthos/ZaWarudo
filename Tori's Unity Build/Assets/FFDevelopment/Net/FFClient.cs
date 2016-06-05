using UnityEngine;

using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;

using FFPrivate;
using FFNetEvents;


//////////////////////////////////////////////////////
// Author: Micah Rust
// Data: 9/10/2015
// Purpose: FFClient and FFServer are still not 100%
//      working as of this release. They should be
//      fixed in the near future.
///////////////////////////////////////////////////////

//private static float timeoutTime = 30.0f; // TODO Maybe

namespace FFNetEvents
{
    [Serializable]
    public struct ClientConnectedEvent
    {
        public DateTime serverStartTime;
        public float serverTime; // in seconds
        public float clientSendTime; // in seconds
        public long clientId;
        public Guid clientGuid;
        public string clientName;
        public string serverName;
    }

    /// <summary>
    /// Gets the data on all of the current clients connect to server TODO: Inspector and sender
    /// </summary>
    [Serializable]
    public class DataOnAllConnectedClientEvent
    {
        List<long> clientIds;
        List<string> clientNames;
    }
    [Serializable]
    public struct ClientConnectionReadyEvent
    {
        public long clientId;
        public double clientTime;
    }
    [Serializable]
    public struct ClientConnectionEndedEvent
    {
        public long clientId;
        public double clientTime;
    }
    [Serializable]
    public struct ClieintConnectionSuspended
    {
    }
    [Serializable]
    public struct ClientSyncTimeEvent
    {
        public bool isDistortionSyncEvent;
        public double clientSendTime;
        public double serverLifeTime; // in seconds
    }
    [Serializable]
    public struct TextMessageEvent
    {
        public string message;
    }
    [Serializable]
    public class GameObjectNetIdRecievedEvent
    {
        public int gameInstanceId;
        public long netId;
    }
}

public class FFClient : MonoBehaviour
{
    public const int BUFFERSIZE = 65536;

    #region Monobehaviour Singleton
    private static FFClient _singleton = null;
    private static FFClient singleton
    {
        get { return _singleton; }
    }
    public static void GetReady()
    {
        if(singleton == null)
        {
            GameObject newFFClient;
            newFFClient = new GameObject("FFClient");
            _singleton = newFFClient.AddComponent<FFClient>();
            FFMessage<ClientConnectedEvent>.Connect(OnClientConnected);
            FFMessage<ClientSyncTimeEvent>.Connect(OnClientSyncTime);
        }
    }
    private FFClient()
    {
        if (singleton != null)
        {
            Debug.LogError("Error created a second Singleton of FFNetServer");
            return;
        }
        _singleton = this;
    }
    void OnDestroy()
    {
        Debug.Log("FFClient Destroyed");
        _singleton = null;
        EndSendThread();

        try
        {
            if (_clientData.clientSocketTCP != null && _clientData.clientSocketTCP.socket != null && _clientData.clientSocketTCP.socket.IsBound)
            {
                _clientData.clientSocketTCP.socket.BeginDisconnect(false, new AsyncCallback(DisconnectCallback), _clientData.clientSocketTCP.socket);
                _clientData.clientSocketTCP.socket = null;
                _clientData.clientSocketTCP.clientData = null;
                _clientData.clientSocketTCP = null;
            }
        }
        catch (Exception exp) { Debug.Log("Error in FFServer OnDestroy() : " + exp.Message); }

        try
        {
            if (_clientData.clientSocketUDP != null && _clientData.clientSocketUDP.udpClient != null)
            {
                _clientData.clientSocketUDP.udpClient.Close();
                _clientData.clientSocketUDP.udpClient = null;
                _clientData.clientSocketUDP.udpEndPointLocal = null;
                _clientData.clientSocketUDP.udpEndPointRemote = null;
                _clientData.clientSocketUDP.socket = null;
                _clientData.clientSocketUDP.clientData = null;
                _clientData.clientSocketUDP = null;
                
            }
        }
        catch (Exception exp) { Debug.Log("Error in FFServer OnDestroy() : " + exp.Message); }

        FFLocalEvents.TimeChangeEvent TCE;
        TCE.newCurrentTime = FFSystem.time;
        FFMessage<FFLocalEvents.TimeChangeEvent>.SendToLocal(TCE);

        FFMessage<ClientConnectedEvent>.Disconnect(OnClientConnected);
        FFMessage<ClientSyncTimeEvent>.Disconnect(OnClientSyncTime);
    }
    private static void DisconnectCallback(IAsyncResult AR)
    {
        try
        {
            Socket socket = (Socket)AR.AsyncState;
            socket.EndDisconnect(AR);
        }
        catch (Exception exp)
        {
            Debug.Log("Error in FFClient async DisconnectCallback: " + exp.Message);
        }
    }
    #endregion

    #region ClientData
    public class ClientData
    {
        public ClientSocket clientSocketTCP;
        public ClientSocket clientSocketUDP;
        public long clientId;
        public Guid clientGuid;
        public string clientName = "ClientName_default (ClientSide)";
        public bool IsOrBeingConnected = false;
    }
    public class ClientSocket
    {
        public ClientSocket(Socket _socket)
        {
            this.socket = _socket;
        }
        public UdpClient udpClient;
        public IPEndPoint udpEndPointLocal;
        public IPEndPoint udpEndPointRemote;
        public Socket socket;
        public byte[] recieveDataBuffer = new byte[BUFFERSIZE];
        public const int BUFFERSIZE = 65536;
        public ClientData clientData = null;

        // reciprical functionity on FFServer.ClientSocket which probably shouldn't be used in FFClient
        /*
        public void SendMessage<MessageType>(MessageType message, bool varifiedPacket)
        {
            FFPacket<MessageType> packet =
                new FFPacket<MessageType>(FFPacketInstructionFlags.Message,
                    typeof(MessageType).ToString(), message);

            SendNetPacket(packet, varifiedPacket);
        }

        public void SendMessageBoardEntry<MessageType>(MessageType message, string entry, bool varifiedPacket)
        {
            FFPacket<MessageType> packet =
                new FFPacket<MessageType>(FFPacketInstructionFlags.MessageBoardEntry,
                    typeof(MessageType).ToString(), message, entry);

            SendNetPacket(packet, varifiedPacket);
        }

        public void SendGameObject<MessageType>(MessageType message, ulong netId, bool varifiedPacket)
        {
            FFPacket<MessageType> packet =
                new FFPacket<MessageType>(FFPacketInstructionFlags.MessageBoardGameObjectSend,
                    typeof(MessageType).ToString(), message, netId.ToString());

            SendNetPacket(packet, varifiedPacket);
        }

        public void SendGameDownObject<MessageType>(MessageType message, ulong netId, bool varifiedPacket)
        {
            FFPacket<MessageType> packet =
                new FFPacket<MessageType>(FFPacketInstructionFlags.MessageBoardGameObjectSendDown,
                    typeof(MessageType).ToString(), message, netId.ToString());

            SendNetPacket(packet, varifiedPacket);
        }

        public void SendGameUpObject<MessageType>(MessageType message, ulong netId, bool varifiedPacket)
        {
            FFPacket<MessageType> packet =
                new FFPacket<MessageType>(FFPacketInstructionFlags.MessageBoardGameObjectSendUp,
                    typeof(MessageType).ToString(), message, netId.ToString());

            SendNetPacket(packet, varifiedPacket);
        }

        public void SendGameAllConnectedObject<MessageType>(MessageType message, ulong netId, bool varifiedPacket)
        {
            FFPacket<MessageType> packet =
                new FFPacket<MessageType>(FFPacketInstructionFlags.MessageBoardGameObjectSendToAllConnected,
                    typeof(MessageType).ToString(), message, netId.ToString());

            SendNetPacket(packet, varifiedPacket);
        }

        public void SendNetPacket<MessageType>(FFPacket<MessageType> packet, bool varifiedPacket)
        {
            FFPacket<MessageType>.Encrypt(ref packet.message);
            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            FFSystem.InitBinaryFormatter(bf);
            FFPacket<MessageType>.Encrypt(ref packet.message);

            bf.Serialize(ms, packet);
            byte[] packetData = ms.ToArray();

            try
            {
                if (varifiedPacket == true)
                {
                    //Debug.Log("Client Sent Packet via TCP of type :" + typeof(MessageType).ToString()); // debug
                    clientData.clientSocketTCP.socket.BeginSend(packetData, 0, packetData.Length, SocketFlags.None, new AsyncCallback(SendCallbackTCP), clientData.clientSocketTCP);
                }
                else
                {
                    //Debug.Log("Client Sent Packet via UDP of type :" + typeof(MessageType).ToString()); // debug
                    clientData.clientSocketUDP.udpClient.BeginSend(packetData, packetData.Length, clientData.clientSocketUDP.udpEndPointRemote, new AsyncCallback(SendCallbackUDP), clientData.clientSocketUDP);
                }
            }
            catch (Exception exp)
            {
                Debug.LogError("Error in FFClient.ClientSocket.SendNetPacket<" + typeof(MessageType).ToString() + ">(FFPacket<" + typeof(MessageType).ToString() + ">, bool)" +
                    "\nException: " + exp.Message +
                    "\nClient ID: " + clientData.clientId +
                    "\nClient Name: " + clientData.clientName);
                return;
            }
        }
         */
    }

    // Time Sync Data
    private DateTime _serverStartDateTime;
    private double _serverLifeTime = 0;
    // average difference over a timeframe between this client and the server
    private double _serverWatchDialation = 0;
    // the dialation between local clock and server clock over a PingSyncCycle
    public static double serverWatchDialation
    {
        get {
            if (singleton == null) return 0.0;
            else return singleton._serverWatchDialation;
        }
    }
    private const double _serverWatchDialationLimits = 0.1; // +-100 ms max dialation
    private double _serverPingTime = 0;
    private double _DialationSyncCycle = 10.0;
    private float _PingCycle = 1.0f;
    

    // Send Queues
    private Queue<FFBasePacket> _packetsToSendViaTCP = new Queue<FFBasePacket>(128);
    private Queue<FFBasePacket> _packetsToSendViaUDP = new Queue<FFBasePacket>(128);

    // Threads
    private Thread _sendThread;

    // FFNet Client Data
    private ClientData _clientData = new ClientData();
    
    private IPEndPoint _ipEndPoint;
    private string _ipAddress;
    private string _serverName = "Server_default (ClientSide)";
    private int _port = 6532;
    private long _clientId = -1;
    private bool _ready = false;

    #endregion
    
    #region ClientData Getters
    public static long clientId
    {
        get
        {
            return singleton._clientId;
        }
    }
    public static bool isReady
    {
        get
        {
            if (singleton == null) return false;
            else return singleton._ready;
        }
    }
    public static double clientTime
    {
        get
        {
            if (singleton == null) return FFSystem.clientWatchTime;
            else return clientTimeNoDistortion +
                  ((FFSystem.clientWatchTime / singleton._DialationSyncCycle) * singleton._serverWatchDialation);
        }
    }
    public static bool clientTCPIsConnected
    {
        get {
            if (singleton != null &&
                singleton._clientData != null &&
                singleton._clientData.clientSocketTCP != null &&
                singleton._clientData.clientSocketTCP.socket != null &&
                singleton._clientData.clientSocketTCP.socket.Connected)
            {
                return true;
            }
            else
            {
                return false;
            }
          }
    }
    public static IPEndPoint ServerIPEndPoint
    {
        get
        {
            if (singleton != null &&
                singleton._clientData != null &&
                singleton._clientData.clientSocketUDP != null &&
                singleton._clientData.clientSocketUDP.udpEndPointRemote != null)
            {
                return singleton._clientData.clientSocketUDP.udpEndPointRemote;
            }
            return null;
        }
    }
    public static IPEndPoint ClientIPEndPoint
    {
        get
        {
            if (singleton != null &&
                singleton._clientData != null &&
                singleton._clientData.clientSocketUDP != null &&
                singleton._clientData.clientSocketUDP.udpEndPointLocal != null)
            {
                return singleton._clientData.clientSocketUDP.udpEndPointLocal;
            }
            return null;
        }
    }
    private static double clientTimeNoDistortion
    {
        get
        {
            if(singleton  == null) return FFSystem.clientWatchTime;
            else return singleton._serverLifeTime + FFSystem.clientWatchTime;
        }
    }
    

    public static double serverPing
    {
        get
        {
            if (singleton == null) return 0;
            else return singleton._serverPingTime;
        }
    }
    /// <summary>
    /// Last recieved expected server time
    /// </summary>
    public static double serverTime
    {
        get 
        {
            if (singleton == null) return 0.0;
            else return singleton._serverLifeTime + singleton._serverPingTime;
        }
    }
    public static string serverName
    {
        get
        {
            if (singleton == null) return "Server Not Created";
            else return singleton._serverName;
        }
    }
    public static DateTime serverStartTime
    {
        get
        {
            return singleton._serverStartDateTime;
        }
    }
    public static double serverRoundTrip
    {
        get
        {
            return singleton._serverPingTime * 2;
        }
    }
    #endregion

    #region Startup
    public static void StartClient(string ipAddress, int port, string clientName)
    {
        FFClient.GetReady();
        FFSystem.GetReady();
        FFMessageSystem.GetReady();

        if ((singleton._clientData.IsOrBeingConnected = true &&
             singleton._clientData.clientSocketTCP != null && singleton._clientData.clientSocketTCP.socket != null && singleton._clientData.clientSocketTCP.socket.IsBound) &&
             singleton._clientData.clientSocketUDP != null && singleton._clientData.clientSocketUDP.udpClient != null)
             return;

        singleton._clientData.IsOrBeingConnected = true;
        singleton._clientData.clientName = clientName;
        singleton._ipAddress = ipAddress;
        singleton._port = port;

        singleton._ipEndPoint = new IPEndPoint(IPAddress.Parse(singleton._ipAddress), singleton._port);
        
        try
        {
            // TCP
            singleton._clientData.clientSocketTCP = new ClientSocket(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));
            singleton._clientData.clientSocketTCP.socket.BeginConnect(singleton._ipEndPoint, new AsyncCallback(ConnectCallback), singleton._clientData);
            singleton._clientData.clientSocketTCP.clientData = singleton._clientData;
                        
            Debug.Log("Connecting to Server..." +
                "\nIPEndPoint: " + singleton._ipEndPoint);
        }
        catch (Exception e)
        {
            Debug.Log("Client Failed to beging connecting to Server" + 
                "\nException: " + e.Message);
        }
    }
    #endregion

    #region IP Gettters
    // IP Getters
    // Local
    public static IPAddress GetLocalIP()
    {
        IPHostEntry host;
        IPAddress localIP = null;
        host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                localIP = ip;
            }
        }
        return localIP;
    }
    public static void GetLocalIPEventStart()
    {
        var thread = new Thread(GetLocalIPEventRun);
        thread.Start();
    }
    private static void GetLocalIPEventRun()
    {
        try
        {
            IPHostEntry host;
            IPAddress localIP = null;
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip;
                }
            }

            GetIPAddressEvent GIAE;
            GIAE.isLocalIP = true;
            GIAE.ip = localIP;
            // Because Unity doesn't allow anything to touch game objects with anything besides the main thread
            // this event will be disbatched via FFSystem through FFMessageSystem.
            FFSystem.DisbatchMessage<GetIPAddressEvent>(GIAE);
        }
        catch (Exception exp)
        {
            Debug.LogError("Error in GetPublicIPBegin" +
                "\nException: " + exp.Message);
        }
    }
    // Public
    public static void GetPublicIPEventStart()
    {
        var thread = new Thread(GetPublicIPEventRun);
        thread.Start();
    }
    private static void GetPublicIPEventRun()
    {
        try
        {
            //Debug.Log("Start GetPulicIPEventRun"); // debug
            string webResponce = "";
            WebRequest request = WebRequest.Create("http://ipinfo.io/ip");

            //Debug.Log("Request"); // debug
            using (WebResponse response = request.GetResponse()) // May not work always.
            {
                //Debug.Log("Responce"); //debug
                using (StreamReader stream = new StreamReader(response.GetResponseStream()))
                {
                    webResponce = stream.ReadToEnd();
                }
            }

            /* Get Ip address from responce which is of format
            XX.XXX.XXX.XX <- 1 following space
            */
            IPAddress publicIP = IPAddress.Parse(webResponce.Substring(0, webResponce.Length - 1));

            GetIPAddressEvent GIAE;
            GIAE.isLocalIP = false;
            GIAE.ip = publicIP;
            // Because Unity doesn't allow anything to touch game objects with anything besides the main thread
            // this event will be disbatched via FFSystem through FFMessageSystem.
            FFSystem.DisbatchMessage<GetIPAddressEvent>(GIAE);
        }
        catch (Exception exp)
        {
            Debug.LogError("Error in GetPublicIPBegin" +
                "\nException: " + exp.Message);
        }
    }
    
    #endregion

    #region Runtime Procedures
    
    private static void SendClientSyncTimeEvent(bool isDistorsionSync)
    {
        ClientSyncTimeEvent e;
        e.serverLifeTime = -1;
        e.isDistortionSyncEvent = isDistorsionSync;
        e.clientSendTime = clientTimeNoDistortion;

        FFPacket<ClientSyncTimeEvent> packet = new FFPacket<ClientSyncTimeEvent>(
            FFPacketInstructionFlags.Message | FFPacketInstructionFlags.Immediate,
            typeof(ClientSyncTimeEvent).ToString(), e);

        FFMessageSystem.SendToNet<ClientSyncTimeEvent>(packet, false);
    }


    #endregion

    #region NetEvents
    private static void OnClientConnected(ClientConnectedEvent e)
    {
        if (e.clientGuid == singleton._clientData.clientGuid)
        {
            double localwatchtime = FFSystem.clientWatchTime;
            FFSystem.ResetClientWatch();

            // Set ping ( 0 < ping < 1.0 )
            double calculatedPingTime = Math.Min(Math.Max((localwatchtime - e.clientSendTime) / 2.0, 0.0), 1.0);
            singleton._serverPingTime = (calculatedPingTime + singleton._serverPingTime * 4.0) / 5.0;

            singleton._clientId = e.clientId;
            singleton._serverLifeTime = e.serverTime + singleton._serverPingTime;
            singleton._serverStartDateTime = e.serverStartTime;
            singleton._serverName = e.serverName;

            Debug.Log("Retrieved id: " + singleton._clientId
                    + " from Server with start time of: " + e.serverStartTime
                    + ", and a server life time of: " + e.serverTime);

            singleton._ready = true;
            ClientConnectionReadyEvent CCRE;
            CCRE.clientId = singleton._clientId;
            CCRE.clientTime = clientTime;
            FFMessage<ClientConnectionReadyEvent>.SendToLocal(CCRE);

            FFLocalEvents.TimeChangeEvent TCE;
            TCE.newCurrentTime = FFSystem.time;
            FFMessage<FFLocalEvents.TimeChangeEvent>.SendToLocal(TCE);
        }
    }
    private static void OnClientSyncTime(ClientSyncTimeEvent e)
    {
        if(e.isDistortionSyncEvent)
        {
            // Set ping
            double calculatedPingTime = Math.Max((clientTimeNoDistortion - e.clientSendTime) / 2.0, 0.0);
            singleton._serverPingTime = (calculatedPingTime + singleton._serverPingTime * 3.0) / 4.0;

            double calculatedServerWatchDialation = Math.Min(Math.Max(-_serverWatchDialationLimits,
                (clientTimeNoDistortion) - e.serverLifeTime),
                _serverWatchDialationLimits);

            FFSystem.ResetClientWatch();
            singleton._serverLifeTime = e.serverLifeTime + singleton._serverPingTime;

            // _serverWatchDialation average
            singleton._serverWatchDialation = (calculatedServerWatchDialation + singleton._serverWatchDialation) / 2.0;
            //_serverWatchDialation = calculatedServerWatchDialation; // debug (non-averaged)

            Debug.Log("ClientSyncTimeEvent (Dialation Event)" +
                "\nCalculated Dialation: " + calculatedServerWatchDialation +
                "\nCalculated Ping: " + calculatedPingTime);  // debug
        }
        else
        {
            // Set ping
            double calculatedPingTime = Math.Max(clientTimeNoDistortion - e.clientSendTime, 0.0);
            singleton._serverPingTime = (calculatedPingTime + singleton._serverPingTime * 4.0) / 5.0;
            //_serverPingTime = calculatedPingTime;

            /*Debug.Log("ClientSyncTimeEvent" +
                "\nCalculated Ping: " + calculatedPingTime); */ //debug
        }


    }
    
    #endregion

    #region Commands/Interface
    private static void SendClientConnectedEvent(string clientName)
    {
        singleton._clientData.clientGuid = Guid.NewGuid();
        ClientConnectedEvent e = new ClientConnectedEvent();
        e.clientId = -1;
        e.serverTime = -1;
        e.serverStartTime = new DateTime();
        e.serverName = null;
        e.clientName = clientName;
        e.clientGuid = singleton._clientData.clientGuid;
        e.clientSendTime = (float)FFSystem.clientWatchTime;

        FFMessageSystem.SendMessageToNet<ClientConnectedEvent>(e, true);

    }
    public static void DisconnectNetClient()
    {
        if (singleton._clientData.clientSocketTCP != null && singleton._clientData.clientSocketTCP.socket != null)
        {
            singleton._clientData.clientSocketTCP.socket.Disconnect(true);
            singleton._clientData.clientSocketTCP.socket = null;
            singleton._clientData.clientSocketUDP.clientData = null;
            singleton._clientData.clientSocketTCP = null;
        }

        if (singleton._clientData.clientSocketUDP != null && singleton._clientData.clientSocketUDP.udpClient != null)
        {
            singleton._clientData.clientSocketUDP.udpClient.Close();
            singleton._clientData.clientSocketUDP.udpClient = null;
            singleton._clientData.clientSocketUDP.udpEndPointLocal = null;
            singleton._clientData.clientSocketUDP.socket = null;
            singleton._clientData.clientSocketUDP.clientData = null;
            singleton._clientData.clientSocketUDP = null;
        }
    }
    #endregion

    #region Send
    private static void StartSendThread()
    {
        if (singleton._sendThread == null)
        {
            singleton._sendThread = new Thread(SendThreadLoop);
            singleton._sendThread.Start();
        }
    }
    private static void EndSendThread()
    {
        if (singleton != null && singleton._sendThread != null)
        {
            singleton._sendThread.Abort();
            singleton._sendThread.Join(100);
            singleton._sendThread = null;
        }
    }
    private static void SendThreadLoop()
    {
        BinaryFormatter bf = new BinaryFormatter();
        MemoryStream ms = new MemoryStream();
        FFSystem.InitBinaryFormatter(bf);

        uint PingCycle = (uint)(singleton._PingCycle * 1000.0f);
        uint DialationSyncCycle = (uint)(singleton._DialationSyncCycle * 1000.0f);
        // Sync Counters (in milliseconds)
        uint DialationSyncTimeCounter = DialationSyncCycle;
        uint PingTimeCounter = 0;

        System.Diagnostics.Stopwatch sendThreadWatch = new System.Diagnostics.Stopwatch();
        sendThreadWatch.Start();

        while(true)
        {
            int cyclePacketCount = 0;

            // sync Client if needed
            if (FFClient.isReady)
            {
                if (sendThreadWatch.ElapsedMilliseconds >= PingTimeCounter)
                {
                    PingTimeCounter += PingCycle;
                    SendClientSyncTimeEvent(false);
                    //Debug.Log("SendThread sent a ClientSyncTimeEvent w/o Dialation"); //debug
                }
            
                if (sendThreadWatch.ElapsedMilliseconds >= DialationSyncTimeCounter)
                {
                    DialationSyncTimeCounter += DialationSyncCycle;
                    SendClientSyncTimeEvent(true);
                    //Debug.Log("SendThread sent a ClientSyncTimeEvent w/ Dialation"); //debug
                }
            }

            while (singleton._packetsToSendViaTCP.Count != 0)
            {
                ms = new MemoryStream();
                FFBasePacket packet = singleton._packetsToSendViaTCP.Dequeue();
                bf.Serialize(ms, packet);

                byte[] packetData = ms.GetBuffer();
                ms.Flush();
                ms.Dispose();

                SendPacketTCP(packetData);
                ++cyclePacketCount;
                //Debug.Log("SendThead Sent a TCP packet"); // debug
            }

            while (singleton._packetsToSendViaUDP.Count != 0)
            {
                ms = new MemoryStream();
                FFBasePacket packet = singleton._packetsToSendViaUDP.Dequeue();
                bf.Serialize(ms, packet);

                byte[] packetData = ms.GetBuffer();
                ms.Flush();
                ms.Dispose();

                SendPacketUDP(packetData);
                ++cyclePacketCount;
                //Debug.Log("SendThead Sent a UDP packet"); // debug
            }


            if (cyclePacketCount == 0) // if we didn't send anything
                Thread.Sleep(1); // Throw on CPU thead queue
        }
    }

    public static void SendPacket(FFBasePacket basepacket, bool varified)
    {
        // Assign Sender ID
        basepacket.senderId = singleton._clientId; // 0 is for server, -1 Uninitialized, (1-MaxLong)

        if((basepacket.packetInstructions & FFPacketInstructionFlags.Immediate).Equals(FFPacketInstructionFlags.Immediate))
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, basepacket);
            byte[] packetData = ms.ToArray();
            ms.Flush();
            ms.Dispose();

            if (varified)
            {
                SendPacketTCP(packetData);
            }
            else
            {
                SendPacketUDP(packetData);
            }
        }
        else
        {
            //Debug.Log("SendPacket");  // debug
            if (varified)
            {
                AddVerifiedPacketToSend(basepacket);
            }
            else
            {
                AddUnverifiedPacketToSend(basepacket);
            }
        }
    }
    private static void AddVerifiedPacketToSend(FFBasePacket packet)
    {
        singleton._packetsToSendViaTCP.Enqueue(packet);
    }
    private static void AddUnverifiedPacketToSend(FFBasePacket packet)
    {
        singleton._packetsToSendViaUDP.Enqueue(packet);
    }


    private static void SendPacketUDP(byte[] packetData)
    {
        if (singleton._clientData.clientSocketUDP != null &&
            singleton._clientData.clientSocketUDP.udpClient != null)
        {
            try
            {
                singleton._clientData.clientSocketUDP.udpClient.Send(packetData, packetData.Length, singleton._clientData.clientSocketUDP.udpEndPointRemote);
                //Debug.Log("Client has Sent Packet Via UDP");// debug
            }
            catch (Exception exp)
            {
                Debug.LogError("Error in SendPacketUDP (Client)" +
                    "\nException: " + exp.Message);
            }
        }
        
    }
    private static void SendPacketTCP(byte[] packetData)
    {
        if (singleton._clientData.clientSocketTCP != null &&
            singleton._clientData.clientSocketTCP.socket != null &&
            singleton._clientData.clientSocketTCP.socket.IsBound)
        {
            try
            {
                singleton._clientData.clientSocketTCP.socket.BeginSend(packetData, 0, packetData.Length, SocketFlags.None,
                    new AsyncCallback(SendCallbackTCP), singleton._clientData.clientSocketTCP);
                //Debug.Log("Client has Sent Packet Via TCP");// debug
            }
            catch (Exception exp) {
                Debug.LogError("Error in SendPacketTCP (Client)" +
                    "\nException: " + exp.Message); }
        }
    }
    #endregion

    #region AsyncCallbacks
    private static void ConnectCallback(IAsyncResult AR)
    {
        try
        {
            // TCP
            ClientData clientData = (ClientData)AR.AsyncState;
            clientData.clientSocketTCP.socket.EndConnect(AR);

            clientData.clientSocketTCP.socket.BeginReceive(clientData.clientSocketTCP.recieveDataBuffer, 0, clientData.clientSocketTCP.recieveDataBuffer.Length,
                SocketFlags.None, new AsyncCallback(RecieveCallbackTCP), clientData.clientSocketTCP);

            // UDP
            UdpClient udpClient = new UdpClient();
            // Settings
            
            IPEndPoint endpointTCPRemote = clientData.clientSocketTCP.socket.RemoteEndPoint as IPEndPoint;
            IPEndPoint endpointTCPLocal = clientData.clientSocketTCP.socket.LocalEndPoint as IPEndPoint;

            singleton._clientData.clientSocketUDP = new ClientSocket(udpClient.Client);
            singleton._clientData.clientSocketUDP.udpEndPointRemote = new IPEndPoint(endpointTCPRemote.Address, endpointTCPRemote.Port);
            singleton._clientData.clientSocketUDP.udpEndPointLocal = new IPEndPoint(endpointTCPLocal.Address, endpointTCPLocal.Port);
            singleton._clientData.clientSocketUDP.udpClient = udpClient;
            singleton._clientData.clientSocketUDP.clientData = singleton._clientData;
            singleton._clientData.clientSocketUDP.udpClient.Client.Bind(singleton._clientData.clientSocketUDP.udpEndPointLocal);
            singleton._clientData.clientSocketUDP.udpClient.EnableBroadcast = true; // may need to change to Options route...
            
            StartSendThread();

            clientData.clientSocketUDP.udpClient.BeginReceive(new AsyncCallback(RecieveCallbackUDP), clientData.clientSocketUDP);

            SendClientConnectedEvent(clientData.clientName);

            Debug.Log("Connected to Server!" +
                    "\nClientEndPointLocal: " + singleton._clientData.clientSocketUDP.udpEndPointLocal +
                    "\nClientEndPointRemote: " + singleton._clientData.clientSocketUDP.udpEndPointRemote);
        }
        catch(Exception e)
        {
            Debug.LogError("Client failed to Connect to Server in ConnectCallback" +
                "\nException: " + e.Message);
        }
    }
    private static void RecieveCallbackTCP(IAsyncResult AR)
    {
        SocketError socketError = SocketError.Success;
        ClientSocket clientSocket = (ClientSocket)AR.AsyncState;
        try
        {
            int recieved = clientSocket.socket.EndReceive(AR, out socketError);
            if (recieved != 0 && socketError == SocketError.Success)
            {
                byte[] packetData = new byte[recieved];
                Array.Copy(clientSocket.recieveDataBuffer, packetData, recieved);

                // continue recieving packets
                clientSocket.socket.BeginReceive(clientSocket.recieveDataBuffer, 0, clientSocket.recieveDataBuffer.Length, SocketFlags.None, new AsyncCallback(RecieveCallbackTCP), clientSocket);

                FFBasePacket packet;
                try
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    FFSystem.InitBinaryFormatter(bf);
                    MemoryStream ms = new MemoryStream(packetData);
                    packet = (FFBasePacket)bf.Deserialize(ms);
                    ms.Dispose();
                }
                catch (Exception exp)
                {
                    Debug.Log("FFPacket not deserializable (Client TCP)" + 
                        "\nException: " + exp.Message +
                        "\nPacket Sise: " + packetData.Length);
                    return;
                }

                FFSystem.DisbatchPacket(packet);
                //Debug.Log("Recieve CallbackTCP (Client)"); //debug
            }
            else
            {

                if (socketError != SocketError.Success)
                    Debug.LogError("RecieveCallbackTCP SocketShutdown (Client)" +
                        "\nSocketError: " + socketError +
                        "\nClient ID: " + clientSocket.clientData.clientId +
                        "\nClient Name: " + clientSocket.clientData.clientName);
                else
                    Debug.Log("RecieveCallbackTCP SocketShutdown (Client)" +
                        "\nClient ID: " + clientSocket.clientData.clientId +
                        "\nClient Name: " + clientSocket.clientData.clientName);
            }
        }
        catch (Exception exp)
        {
            if (socketError != SocketError.Success)
                Debug.LogError("Error in RecieveCallbackTCP (Client)" +
                    "\nException: " + exp.Message +
                    "\nSocketError: " + socketError +
                    "\nClient ID: " + clientSocket.clientData.clientId +
                    "\nClient Name: " + clientSocket.clientData.clientName);
            else
                Debug.LogError("Error in RecieveCallbackTCP (Client)" +
                    "\nException: " + exp.Message +
                    "\nClient ID: " + clientSocket.clientData.clientId +
                    "\nClient Name: " + clientSocket.clientData.clientName);
        }
    }
    private static void RecieveCallbackUDP(IAsyncResult AR)
    {
        ClientSocket clientSocket = (ClientSocket)AR.AsyncState;
        try
        {
            // Problem part! this is what locks it up when play/stop/play
            byte[] packetData = clientSocket.udpClient.EndReceive(AR, ref clientSocket.udpEndPointRemote);

            // continue recieving packets
            clientSocket.udpClient.BeginReceive(new AsyncCallback(RecieveCallbackUDP), clientSocket);

            FFBasePacket packet;
            try
            {
                BinaryFormatter bf = new BinaryFormatter();
                FFSystem.InitBinaryFormatter(bf);
                MemoryStream ms = new MemoryStream((byte[])packetData);
                packet = (FFBasePacket)bf.Deserialize(ms);
                ms.Dispose();
            }
            catch (Exception exp)
            {
                Debug.Log("FFPacket not deserializable (Client UDP)" +
                    "\nException: " + exp.Message +
                    "\nPacket Sise: " + packetData.Length);
                return;
            }

            FFSystem.DisbatchPacket(packet);
            //Debug.Log("Recieve CallbackUDP (Client)"); // debug
        }
        catch (Exception exp)
        {
            Debug.LogError("Error in RecieveCallbackUDP (Client)" +
                "\nException: " + exp.Message +
                "\nClient ID: " + clientSocket.clientData.clientId +
                "\nClient Name: " + clientSocket.clientData.clientName +
                "\nUDPEndpointLocal: " + clientSocket.udpEndPointLocal +
                "\nUDPEndPointRemote: " + clientSocket.udpEndPointRemote);
        }
    }
    private static void SendCallbackTCP(IAsyncResult AR)
    {
        ClientSocket clientSocket = (ClientSocket)AR.AsyncState;
        try
        {
            clientSocket.socket.EndSend(AR);
            //Debug.Log("SendcallbackTCP (Client) finished"); //debug
        }
        catch (Exception exp)
        {
            Debug.LogError("Error in SendCallbackTCP (Client)" +
                "\nException: " + exp.Message +
                "\nClient ID: " + clientSocket.clientData.clientId +
                "\nClient Name: " + clientSocket.clientData.clientName);
        }
    }
    private static void SendCallbackUDP(IAsyncResult AR)
    {
        ClientSocket clientSocket = (ClientSocket)AR.AsyncState;
        try
        {
            clientSocket.udpClient.EndSend(AR);
            //Debug.Log("SendcallbackUDP (Client) finished"); //debug
        }
        catch (Exception exp)
        {
            Debug.LogError("Error in SendCallbackUDP (Client)" +
                "\nException: " + exp.Message +
                "\nClient ID: " + clientSocket.clientData.clientId +
                "\nClient Name: " + clientSocket.clientData.clientName +
                "\nUDPEndpointLocal: " + clientSocket.udpEndPointLocal +
                "\nUDPEndPointRemote: " + clientSocket.udpEndPointRemote);
        }
    }
    #endregion

}