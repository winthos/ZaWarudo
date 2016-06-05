using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System;
using FFNetEvents;
using FFPrivate;

//////////////////////////////////////////////////////
// Author: Micah Rust
// Data: 12/07/2015
// Purpose: FFServer is a singleton Component which
//  runs all of the server's connections, message
//  routing, and disconnects
///////////////////////////////////////////////////////

public struct Ser_StartupEvent
{
}
public struct Ser_ShutdownEvent
{
}
public struct Ser_UpdateEvent
{
    public float serverTime;
    public float deltaTime;
}
public struct Ser_FixedUpdateEvent
{
    public float serverTime;
    public float deltaTime;
}
public struct Ser_LateUpdateEvent
{
    public float serverTime;
    public float deltaTime;
}
public class Ser_ClientConnectedEvent
{
    public string clientName;
    public int clientNumber;
}
public class Ser_ClientDisconnectedEvent
{
    public string clientName;
    public int clientNumber;
}

public class FFServer : MonoBehaviour
{
    #region Monobehaviour Singleton
    private static FFServer singleton = null;
    public static void GetReady()
    {
        if (singleton == null)
        {
            GameObject newFFServer;
            newFFServer = new GameObject("FFServer");
            singleton = newFFServer.AddComponent<FFServer>();
            newFFServer.AddComponent<FFAction>();
        }
    }
    private FFServer()
    {
        if (singleton != null)
        {
            Debug.LogError("Error created a second Singleton of FFNetServer");
            return;
        }
        singleton = this;
    }
    void OnDestroy()
    {
        /*------------------------*/
        // Close Server Connections
        /*------------------------*/
        try
        {
            if (_TCPSocket != null && _TCPSocket.IsBound && _TCPSocket.Connected)
            {
                _TCPSocket.BeginDisconnect(false, new AsyncCallback(DisconnectSocketCallbackTCP), _TCPSocket);
            }
        }
        catch (Exception exp) { Debug.LogError("Error in FFServer OnDestroy() : " + exp.Message); }

        try
        {
            if (_UDPClient != null)
            {
                _UDPClient.Close();
            }
        }
        catch (Exception exp) { Debug.LogError("Error in FFServer OnDestroy() : " + exp.Message); }
        /*------------------------*/
        // Close Client Connections
        /*------------------------*/
        try
        {
            foreach (var clientData in _clientDataList)
            {
                // TCP connection
                try
                {
                    clientData.clientSocketTCP.socket.BeginDisconnect(false,
                        new AsyncCallback(DisconnectClientSocketCallbackTCP), clientData.clientSocketTCP);
                }
                catch (Exception exp) { Debug.LogError("Error in FFServer OnDestroy() : " + exp.Message); }

                // UDP Connection
                try
                {
                    clientData.clientSocketUDP.clientData = null;
                    clientData.clientSocketUDP.socket = null;
                    clientData.clientSocketUDP.udpClient = null;
                    clientData.clientSocketUDP.udpEndPointLocal = null;
                    clientData.clientSocketUDP.udpEndPointRemote = null;
                    clientData.clientSocketUDP = null;
                }
                catch (Exception exp) { Debug.LogError("Error in FFServer OnDestroy() : " + exp.Message); }

            }

            _clientData.Clear();
            _clientDataList.Clear();

            // Close shared UDPClient
            _UDPClient.Close();
            _UDPClient = null;
            
        }
        catch (Exception exp) { Debug.LogError("Error in FFServer OnDestroy() : " + exp.Message); }

        FFLocalEvents.TimeChangeEvent TCE;
        TCE.newCurrentTime = FFSystem.time;
        FFMessage<FFLocalEvents.TimeChangeEvent>.SendToLocal(TCE);

        Debug.Log("FFServer Destroyed");
        singleton = null;
    }
    #endregion

    #region ServerData
    private Dictionary<string, List<BaseMessageInspector>> _messageInspectors = new Dictionary<string, List<BaseMessageInspector>>();

    private Socket _TCPSocket; // for joining server
    private UdpClient _UDPClient; // shared by everyone

    private long _clientCount = 0;
    private bool _sendToLocal = true;
    private string _serverName;
    private DateTime _serverStartTime;
    private int _portNumber = 0;
    private System.Diagnostics.Stopwatch _serverWatch = new System.Diagnostics.Stopwatch();


    public int serverPortNumber { get { return _portNumber; } }
    public static bool isClient
    { get { return singleton != null && !singleton._sendToLocal; } }
    public static bool isLocal
    { get { return singleton != null && !singleton._sendToLocal; } }
    public static DateTime serverStartTime
    {
        get
        {
            if (singleton == null) return new DateTime(0);
            else return singleton._serverStartTime;
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
    public static float serverTime
    {
        get
        {
            if (singleton == null) return 0.0f;
            else return (float)((double)(singleton._serverWatch.ElapsedMilliseconds) / 1000.0);
        }
    }
    #endregion

    #region ClientData
    private Dictionary<long, ClientData> _clientData = new Dictionary<long, ClientData>();
    private List<ClientData> _clientDataList = new List<ClientData>();

    public class ClientData
    {
        public ClientData(long clientId, ClientSocket tcpClientSocket, ClientSocket udpClientSocket)
        {
            this.clientId = clientId;
            // All server Clients use the same socket to send and recived
            this.clientSocketTCP = tcpClientSocket;
            this.clientSocketUDP = udpClientSocket;

            this.clientSocketTCP.clientData = this;
            this.clientSocketUDP.clientData = this;
        }
        public ClientSocket clientSocketTCP;
        public ClientSocket clientSocketUDP;
        public long clientId;   // invalid From UDPClientSocket reference due to UDPClientSockets shareing...
        public Guid clientGuid; // invalid From UDPClientSocket reference due to UDPClientSockets shareing...
        public string clientName = "ClientName_default (ServerSide)";  // invalid From UDPClientSocket reference due to UDPClientSockets shareing...
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
            long senderId = packet.senderId; // Client Id
            packet.senderId = 0; // from Server, id: 0

            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            FFSystem.InitBinaryFormatter(bf);
            FFPacket<MessageType>.Encrypt(ref packet.message);

            /*------------------------------*/
            // Get client Data
            ClientData cData;
            // Valid Client Ids: 1 - MaxLong
            if (singleton._clientData.TryGetValue(senderId, out cData))
            {
            }
            else if (senderId == -1) // -1 is for UnInitialized IDs
            {
                // Un Initialized ID will not be able to send UDP Packets to the correct client, but TCP should work.
                cData = clientData;
                Debug.Log("Recieved Uninitialized SenderId"); // debug
            }
            else // Bad ID
            {
                Debug.LogError("Error in FFServer.ClientSocket.SendNetPacket<" + typeof(MessageType).ToString() + ">(FFPacket<" + typeof(MessageType).ToString() + ">, bool " +
                    "\nTried to Send a packet, but we couldn't find the senderId's ClientData." +
                    "\nSenderId: " + senderId);
                return;
            }
            /*------------------------------*/

            bf.Serialize(ms, packet);
            byte[] packetData = ms.GetBuffer();
            ms.Flush();
            ms.Dispose();

            try
            {
                if (varifiedPacket == true)
                {
                    //Debug.Log("Server Sent Packet via TCP of type :" + typeof(MessageType).ToString()); // debug
                    cData.clientSocketTCP.socket.BeginSend(packetData, 0, packetData.Length, SocketFlags.None, new AsyncCallback(SendCallbackTCP), cData.clientSocketTCP);
                }
                else
                {
                    //Debug.Log("Server Sent Packet via UDP of type :" + typeof(MessageType).ToString()); // debug
                    cData.clientSocketUDP.udpClient.Send(packetData, packetData.Length, cData.clientSocketUDP.udpEndPointRemote);
                }
            }
            catch (Exception exp)
            {
                Debug.LogError("Error in FFServer.ClientSocket.SendNetPacket<" + typeof(MessageType).ToString() + ">(FFPacket<" + typeof(MessageType).ToString() + ">, bool)" +
                    "\nException: " + exp.Message +
                    "\nClient ID: " + clientData.clientId +
                    "\nClient Name: " + clientData.clientName);
                return;
            }
        }
    }
    #endregion

    #region GameData
    private long _netIdCounter = 0;
    /// <summary>
    /// List of data to recreate all netObjectsCurrentlyCreated
    /// TODO ADD an event passed to server for objects destroyed.
    /// </summary>
    private List<NetObjectCreatedEvent> _netObjectsCreated = new List<NetObjectCreatedEvent>();
    public static long GetNewNetId
    {
        get
        {
            if (singleton == null) return -1;
            else return Interlocked.Increment(ref singleton._netIdCounter);
        }
    }
    public static List<NetObjectCreatedEvent> NetObjectsCreated
    {
        get
        {
            if (singleton == null) return null;
            else return singleton._netObjectsCreated; 
        }
    }
    #endregion

    #region Startup
    public static void StartServer(int portNumber, string serverName, bool serverIsClient)
    {
        FFServer.GetReady();
        if (singleton._TCPSocket != null ||
            singleton._UDPClient != null)
        {
            Debug.LogError("ERROR, Tried to make a second server with an existing one, Delete the old one first");
            return;
        }

        AddMessageInterceptors();

        if (serverIsClient)
        {
            singleton._sendToLocal = false;
        }

        singleton._serverName = serverName;
        singleton._portNumber = portNumber;


        #region SocketStart
        try
        {
            // Setup TCP
            singleton._TCPSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            singleton._TCPSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            singleton._TCPSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, false);
            singleton._TCPSocket.Bind(new IPEndPoint(IPAddress.Any, portNumber)); // TODO change to loopback?
            singleton._TCPSocket.Listen(250);
            singleton._TCPSocket.BeginAccept(new AsyncCallback(AcceptCallback), singleton._TCPSocket);

            singleton._serverStartTime = DateTime.Now.ToUniversalTime();
            singleton._serverWatch.Reset();
            singleton._serverWatch.Start();

            Debug.Log("Server Started..." +
                "\nportNumber:" + portNumber + " serverName:" + serverName);
        }
        catch (Exception exp)
        {
            Debug.Log(exp.Message);
        }
        #endregion

        Ser_StartupEvent e;
        FFMessage<Ser_StartupEvent>.SendToLocal(e);
    }

    // TODO, maybe make this better... Somehow...
    private static void AddMessageInterceptors()
    {
        // Self assigned...
        new PlayerDiedVarifier();
        new ClientSyncReply();
        new ClientConnectedReply();
        new NetObjectCreatedHandler();
        new NetObjectDestroyedHandler();
    }

    public static void AddMessageInterceptor(BaseMessageInspector inspector)
    {
        string eventType = inspector.eventName;
        List<BaseMessageInspector> messageInspectorList;
        if (singleton._messageInspectors.TryGetValue(eventType, out messageInspectorList))
        {
            messageInspectorList.Add(inspector);
        }
        else
        {
            var newMessageInspectors = new List<BaseMessageInspector>();
            singleton._messageInspectors.Add(eventType, newMessageInspectors);
            newMessageInspectors.Add(inspector);
        }

    }
    public static void RemoveMessageInspector(BaseMessageInspector inspector)
    {
        string eventType = inspector.eventName;
        singleton._messageInspectors.Remove(eventType);
    }
    #endregion

    #region SeverAsyncCallbacks
    private static void AcceptCallback(IAsyncResult AR)
    {
        Socket serverSocket = (Socket)AR.AsyncState;
        try
        {
            long id = Interlocked.Increment(ref singleton._clientCount);

            // TCP Socket
            ClientSocket clientSocketTCP;
            clientSocketTCP = new ClientSocket(serverSocket.EndAccept(AR));
            clientSocketTCP.socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            clientSocketTCP.socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, false);

            // UDP Socket IPEndpoints
            IPEndPoint endpointfromLocal = clientSocketTCP.socket.LocalEndPoint as IPEndPoint;
            IPEndPoint endpointfromRemote = clientSocketTCP.socket.RemoteEndPoint as IPEndPoint;
            IPEndPoint udpClientEndpointLocal = new IPEndPoint(endpointfromLocal.Address, endpointfromLocal.Port);
            IPEndPoint udpClientEndpointRemote = new IPEndPoint(endpointfromRemote.Address, endpointfromRemote.Port);


            ClientSocket clientSocketUDP;
            bool beginUDPListen = false;
            if (singleton._UDPClient == null) // At first connection, setup UDP for all clients
            {
                singleton._UDPClient = new UdpClient();
                singleton._UDPClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, false);
                singleton._UDPClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                singleton._UDPClient.Client.EnableBroadcast = true;
                singleton._UDPClient.Client.Bind(udpClientEndpointLocal);

                // UDP begin recieve
                beginUDPListen = true;
            }

            clientSocketUDP = new ClientSocket(singleton._UDPClient.Client);
            clientSocketUDP.udpClient = singleton._UDPClient;

            // Connect Data and sockets together
            ClientData clientData = new ClientData(id, clientSocketTCP, clientSocketUDP);
            clientData.clientSocketUDP.udpEndPointLocal = udpClientEndpointLocal;
            clientData.clientSocketUDP.udpEndPointRemote = udpClientEndpointRemote;

            // Add client Data to group
            singleton._clientData.Add(id, clientData);
            singleton._clientDataList.Add(clientData);

            // UDP Begin Recieve
            if (beginUDPListen)
                singleton._UDPClient.BeginReceive(new AsyncCallback(RecieveCallbackUDP), clientData.clientSocketUDP);

            // TCP begin Recieve
            clientSocketTCP.socket.BeginReceive(clientSocketTCP.recieveDataBuffer, 0, clientSocketTCP.recieveDataBuffer.Length,
                SocketFlags.None, new AsyncCallback(RecieveCallbackTCP), clientSocketTCP);

            // Continue accepting other clients
            serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), serverSocket);

            Debug.Log("ClientConnected to Server" +
                "\nIPEndPointLocal: " + udpClientEndpointLocal +
                "\nIPEndPointRemote: " + udpClientEndpointRemote +
                "\nClient ID: " + clientData.clientId);
        }
        catch (Exception exp)
        {
            Debug.LogError("Error in AcceptCallback (Server)" +
                "\nException: " + exp.Message);

            try
            {
                // Continue accepting other clients
                serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), serverSocket);
            }
            catch (Exception exp1)
            {
                Debug.LogError("Error in to Continue accepting Clients (Server)" +
                    "\nException: " + exp1.Message);
            }
        }
    }
    private static void RecieveCallbackUDP(IAsyncResult AR)
    {
        ClientSocket clientSocket = (ClientSocket)AR.AsyncState;
        try
        {
            byte[] packetData = clientSocket.udpClient.EndReceive(AR, ref clientSocket.udpEndPointLocal);

            // continue recieving packets
            clientSocket.udpClient.BeginReceive(new AsyncCallback(RecieveCallbackUDP), clientSocket);

            if (packetData.Length != 0)
                ProcessRecievedPacket(clientSocket, packetData);
            //Debug.Log("RecieveCallbackUDPMessages (Server)"); // debug
        }
        catch (Exception exp)
        {
            Debug.LogError("Error in RecieveCallbackUDPMessages (server)" +
                "\nException: " + exp.Message +
                "\nClient ID: " + clientSocket.clientData.clientId +
                "\nClient Name: " + clientSocket.clientData.clientName +
                "\nUDPEndpointLocal: " + clientSocket.udpEndPointLocal +
                "\nUDPEndPointRemote: " + clientSocket.udpEndPointRemote);
        }
    }
    private static void RecieveCallbackTCP(IAsyncResult AR)
    {
        SocketError socketError = SocketError.Success;
        ClientSocket clientSocket = (ClientSocket)AR.AsyncState;
        //Debug.Log("RecieveCallbackTCP (Server)"); //debug
        try
        {
            if (clientSocket.socket.Connected == false)
            {
                Debug.Log("RecieveCallbackTCP Connection Lost (Server)" +
                        "\nClient ID: " + clientSocket.clientData.clientId +
                        "\nClient Name: " + clientSocket.clientData.clientName);
                return;
            }

            int recieved = clientSocket.socket.EndReceive(AR, out socketError);

            if (recieved != 0 && socketError == SocketError.Success)
            {
                byte[] recieveSendBuffer = new byte[recieved];
                Array.Copy(clientSocket.recieveDataBuffer, recieveSendBuffer, recieved);

                // continue recieving packets
                clientSocket.socket.BeginReceive(clientSocket.recieveDataBuffer, 0, clientSocket.recieveDataBuffer.Length,
                    SocketFlags.None, new AsyncCallback(RecieveCallbackTCP), clientSocket);

                ProcessRecievedPacket(clientSocket, recieveSendBuffer);
                //Debug.Log("RecieveCallbackTCP (Server)"); //debug
            }
            else
            {
                if (socketError != SocketError.Success)
                {
                    Debug.LogError("RecieveCallbackTCP SocketShutdown (Server)" +
                        "\nSocketError: " + socketError +
                        "\nClient ID: " + clientSocket.clientData.clientId +
                        "\nClient Name: " + clientSocket.clientData.clientName);

                    RemoveClient(clientSocket);
                }
                else
                {
                    Debug.Log("RecieveCallbackTCP SocketShutdown (Server)" +
                        "\nClient ID: " + clientSocket.clientData.clientId +
                        "\nClient Name: " + clientSocket.clientData.clientName);

                    RemoveClient(clientSocket);
                }
            }
        }
        catch (Exception exp)
        {
            if (socketError != SocketError.Success)
            {
                Debug.LogError("Error in RecieveCallbackTCP (server)" +
                    "\nException: " + exp.Message +
                    "\nSocketError: " + socketError +
                    "\nClient ID: " + clientSocket.clientData.clientId +
                    "\nClient Name: " + clientSocket.clientData.clientName);

                RemoveClient(clientSocket);
            }
            else
            {
                Debug.LogError("Error in RecieveCallbackTCP (server)" +
                    "\nException: " + exp.Message +
                    "\nClient ID: " + clientSocket.clientData.clientId +
                    "\nClient Name: " + clientSocket.clientData.clientName);

                RemoveClient(clientSocket);
            }
        }
    }
    private static void RemoveClient(ClientSocket clientSocket)
    {
        singleton._clientData.Remove(clientSocket.clientData.clientId);
        singleton._clientDataList.Remove(clientSocket.clientData);

        Action<ClientSocket> cleanClientSocket = delegate(ClientSocket cSocket)
        {
            cSocket.clientData = null;
            cSocket.recieveDataBuffer = null;
            cSocket.socket = null;
            cSocket.udpClient = null;
            cSocket.udpEndPointLocal = null;
            cSocket.udpEndPointRemote = null;
        };

        ClientData data = clientSocket.clientData;
        cleanClientSocket(data.clientSocketTCP);
        cleanClientSocket(data.clientSocketUDP);
    }
    private static void ProcessRecievedPacket(ClientSocket clientSocket, byte[] packetData)
    {
        MemoryStream msSendBuffer = new MemoryStream(packetData);
        BinaryFormatter bf = new BinaryFormatter();
        FFSystem.InitBinaryFormatter(bf);

        FFBasePacket packet;
        try
        {
            packet = (FFBasePacket)bf.Deserialize(msSendBuffer); // if packed cannot
            msSendBuffer.Dispose();
        }
        catch (Exception exp)
        {
            Debug.Log("FFPacket not deserializable (Server)" +
                "\nException: " + exp.Message +
                "\nPacket Sise: " + packetData.Length);
            return;
        }

        #region MessageInspectors
        List<BaseMessageInspector> inspectors;
        uint sendToOtherClients = 0; // if any inspectors return a non-zero number, don't distribute to others

        if (singleton._messageInspectors.TryGetValue(packet.messageType, out inspectors))
        {
            foreach (var insp in inspectors)
            {
                try
                {
                    sendToOtherClients |= insp.Inspect(clientSocket, packet);
                }
                catch (Exception exp)
                {
                    Debug.LogError("Error in messageInspector: " + insp.ToString() +
                        "\nException: " + exp.Message);
                }
            }
        }
        #endregion

        #region RelayMessages
        try
        {
            // if none of the inspectors return a non-zero number the message will be relayed to all other clients
            if (sendToOtherClients == 0)
            {
                // inspectors can modify packects to be send to all other clients, need to re-serialize this
                MemoryStream msBroadcast = new MemoryStream();
                bf.Serialize(msBroadcast, packet);
                byte[] broadcastPacketData = msBroadcast.ToArray();
                msBroadcast.Flush();
                msBroadcast.Dispose();

                // Other Clients
                if (clientSocket.udpClient == null) //TCP
                {
                    // Send out packet to others
                    foreach (var cDataEntry in singleton._clientDataList)
                    {
                        ClientSocket sendToClientSocket = cDataEntry.clientSocketTCP;
                        if (clientSocket.Equals(sendToClientSocket) == false)
                        {
                            sendToClientSocket.socket.BeginSend(broadcastPacketData, 0, broadcastPacketData.Length, SocketFlags.None,
                                new AsyncCallback(SendCallbackTCP), sendToClientSocket);
                        }
                    }
                }
                else if (clientSocket.udpClient != null) //UDP
                {
                    ClientData cData;
                    if (singleton._clientData.TryGetValue(packet.senderId, out cData))
                    {
                        // Send out packet to others
                        foreach (var cDataEntry in singleton._clientDataList)
                        {
                            if (cData.clientId != cDataEntry.clientId)
                            {
                                /*Debug.Log("Sending Data to: " + cDataEntry.clientSocketUDP.udpEndPointRemote +
                                    "\nClient Id: " + cDataEntry.clientId +
                                    "\nPacket.instructions: " + packet.packetInstructions +
                                    "\nPacket.entry: " + packet.entry +
                                    "\nPacket.messageType: " + packet.messageType +
                                    "\nPacket.senderId: " + packet.senderId);*/
                                // debug

                                //Debug.Log("Send UDP packet to " + sendToClientSocket.udpEndPointRemote);
                                cDataEntry.clientSocketUDP.udpClient.Send(broadcastPacketData, broadcastPacketData.Length, cDataEntry.clientSocketUDP.udpEndPointRemote);
                            }
                            /*else // debug
                            {
                                Debug.Log("NOT SENDING TO: " + cDataEntry.clientSocketUDP.udpEndPointRemote +
                                    "\nClient Id: " + cDataEntry.clientId +
                                    "\nPacket.instructions: " + packet.packetInstructions +
                                    "\nPacket.entry: " + packet.entry +
                                    "\nPacket.messageType: " + packet.messageType +
                                    "\nPacket.senderId: " + packet.senderId); //debug
                            }*/
                            //debug
                        }
                    }
                    else
                    {
                        Debug.LogError("Error in ProcessingRecievedPackets" +
                            "\nCould not find the ClientData connected to the senderId." +
                            "\nPacket could not be relayed");
                    }
                }
                else // Socket Data is screwed up...
                {
                    Debug.LogError("Error in ProcessingRecievedPackets" +
                            "\nClientData connected to ClientSocket is messed up...?");
                }

                // Send to Local, because this Unity instance doesn't have a local client to listen through packet.
                // This will be done if we are a dedicated server
                if (singleton._sendToLocal)
                {
                    FFSystem.DisbatchPacket(packet);
                }
            }
        }
        catch (Exception exp)
        {
            Debug.LogError("Exception Error in ProcessingRecievedPackets" +
                "\nException: " + exp.Message +
                "\nPacketData.Instructions: " + packet.packetInstructions +
                "\nPacketData.entry: " + packet.entry +
                "\nPacketData.MessageType: " + packet.messageType);
        }
        #endregion
    }
    private static void SendCallbackTCP(IAsyncResult AR)
    {
        ClientSocket clientSocket = (ClientSocket)AR.AsyncState;
        try
        {
            clientSocket.socket.EndSend(AR);
            //Debug.Log("SendcallbackTCP (Server) finished"); //debug
        }
        catch (Exception exp)
        {
            Debug.LogError("Error in SendCallbackTCP (Server)" +
                "\nException: " + exp.Message +
                "\nClient ID: " + clientSocket.clientData.clientId +
                "\nClient Name: " + clientSocket.clientData.clientName);
        }
    }
    /* Send is quicker than adding a Callback, so this is depricated
    private static void SendCallbackUDP(IAsyncResult AR)
    {
        ClientSocket clientSocket = (ClientSocket)AR.AsyncState;
        try
        {
            clientSocket.udpClient.EndSend(AR);
            //Debug.Log("SendcallbackUDP (Server) finished"); // debug
        }
        catch (Exception exp)
        {
            Debug.LogError("Error in SendCallbackUDP (Server)" +
                "\nException: " + exp.Message +
                "\nClient ID: " + clientSocket.clientData.clientId +
                "\nClient Name: " + clientSocket.clientData.clientName +
                "\nUDPEndpointLocal: " + clientSocket.udpEndPointLocal +
                "\nUDPEndPointRemote: " + clientSocket.udpEndPointRemote);
        }
    }*/
    private static void DisconnectClientSocketCallbackTCP(IAsyncResult AR)
    {
        try
        {
            ClientSocket clientSocket = (ClientSocket)AR.AsyncState;
            clientSocket.socket.EndDisconnect(AR);
            clientSocket.clientData = null;
            clientSocket.socket = null;
            clientSocket.udpEndPointLocal = null;
            clientSocket.udpEndPointRemote = null;
        }
        catch (Exception exp)
        {
            Debug.LogError("Error in async FFServer DisconnectClientSocketCallbackTCP: " + exp.Message);
        }
    }
    private static void DisconnectSocketCallbackTCP(IAsyncResult AR)
    {
        try
        {
            Socket socket = (Socket)AR.AsyncState;
            socket.EndDisconnect(AR);
        }
        catch (Exception exp)
        {
            Debug.LogError("Error in async FFServer DisconnectSocketCallbackTCP: " + exp.Message);
        }
    }
    #endregion

    #region ServerMessages
    // Update is called once per frame
    void Update()
    {
        Ser_UpdateEvent e;
        e.deltaTime = Time.deltaTime;
        e.serverTime = serverTime;
        FFMessage<Ser_UpdateEvent>.SendToLocal(e);
    }

    void LateUpdate()
    {
        Ser_LateUpdateEvent e;
        e.deltaTime = Time.deltaTime;
        e.serverTime = serverTime;
        FFMessage<Ser_LateUpdateEvent>.SendToLocal(e);
    }

    void FixedUpdate()
    {
        Ser_FixedUpdateEvent e;
        e.deltaTime = Time.deltaTime;
        e.serverTime = serverTime;
        FFMessage<Ser_FixedUpdateEvent>.SendToLocal(e);
    }
    #endregion
}
