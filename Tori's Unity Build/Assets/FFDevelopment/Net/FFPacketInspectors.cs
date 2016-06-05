using UnityEngine;
using System.Collections;
using FFNetEvents;
using FFPrivate;
using System.Threading;
using System;
using System.Collections.Generic;


//////////////////////////////////////////////////////
// Author: Micah Rust
// Data: 9/10/2015
// Purpose: FFClient and FFServer are still not 100%
//      working as of this release. They should be
//      fixed in the near future.
///////////////////////////////////////////////////////

#region BaseInspector
public class BaseMessageInspector
{
    protected BaseMessageInspector(){}
    protected string _eventName;
    protected System.Type _eventType;
    public virtual uint Inspect(FFServer.ClientSocket clientSocket, FFBasePacket incommingPacket)
    {
        Debug.LogError("Error, BaseMessageInspector called!");
        // return zero to to have this inspector pass this message
        // return positive number if message shouldn't be relayed to all other clients
        return 1; // do not send to other clients
    }
    public string eventName
    {
        get { return _eventName; }
    }
    public System.Type eventType
    {
        get { return _eventType; }
    }
}

/// <summary>
/// To add a MessageInspector on the RelayServer create a new class
/// of with the desired MessageInspector<desiredType> inhereted
/// override the Inspector function and add the class into the
/// AddMessageInspectors in FFNetServer.cs
/// </summary>
public class MessageInspector<EventType> : BaseMessageInspector
{
    protected MessageInspector()
    {
        _eventType = typeof(EventType);
        _eventName = _eventType.ToString();
        FFServer.AddMessageInterceptor(this);
    }
    public override uint Inspect(FFServer.ClientSocket clientSocket, FFBasePacket incommingPacket)
    {
        Debug.LogError("Error, MessageInspector of type " + eventName + " called!");
        // return zero to to have this inspector pass this message
        // return positive number if message shouldn't be relayed to all other clients
        return 1; // do not send to other clients
    }
}

#endregion

// Example MessageInspector
public class PlayerDiedVarifier : MessageInspector<PlayerDiedEvent>
{
    public override uint Inspect(FFServer.ClientSocket clientSocket, FFBasePacket incommingPacket)
    { 
        // Example cast
        //FFPacket<PlayerDiedEvent> packet = (FFPacket<PlayerDiedEvent>)incommingPacket;

        return 1; // return positive number if message shouldn't be relayed to all other clients
    }
}

public class ClientConnectedReply : MessageInspector<ClientConnectedEvent>
{
    public override uint Inspect(FFServer.ClientSocket clientSocket, FFBasePacket incommingPacket)
    {
        FFPacket<ClientConnectedEvent> packet = (FFPacket<ClientConnectedEvent>)incommingPacket;

        clientSocket.clientData.clientName = packet.message.clientName;
        clientSocket.clientData.clientGuid = packet.message.clientGuid;

        packet.message.clientId = clientSocket.clientData.clientId;
        packet.message.serverTime = FFServer.serverTime;
        packet.message.serverStartTime = FFServer.serverStartTime;
        packet.message.serverName = FFServer.serverName;

        clientSocket.SendMessage<ClientConnectedEvent>(packet.message, true);

        // make a copy so that it doesn't get invalidated if another thread deletes something
        var netObjects = new List<NetObjectCreatedEvent>(FFServer.NetObjectsCreated);
        foreach (var netobj in netObjects)
        {
            clientSocket.SendMessage<NetObjectCreatedEvent>(netobj, true);
        }

        return 0;
    }
}

public class NetObjectCreatedHandler : MessageInspector<NetObjectCreatedEvent>
{
    public override uint Inspect(FFServer.ClientSocket clientSocket, FFBasePacket incommingPacket)
    {
        FFPacket<NetObjectCreatedEvent> packet = (FFPacket<NetObjectCreatedEvent>)incommingPacket;

        long netid = FFServer.GetNewNetId;
        GameObjectNetIdRecievedEvent reply = new GameObjectNetIdRecievedEvent();
        reply.gameInstanceId = packet.message.gameObjectInstanceId;
        packet.message.gameObjectNetId = netid;
        reply.netId = netid;
        
        // TODO make this run off of netId on local server/dedicated version
        // SAVE NewObjectCreatedEvents so they can be sent to new clients later
        FFServer.NetObjectsCreated.Add(packet.message);


        clientSocket.SendMessage<GameObjectNetIdRecievedEvent>(reply, true);
        return 0;
    }
}

public class NetObjectDestroyedHandler : MessageInspector<NetObjectDestroyedEvent>
{
    public override uint Inspect(FFServer.ClientSocket clientSocket, FFBasePacket incommingPacket)
    {
        //Debug.Log("NetObjectDestroyedEvent Packet Recieved"); // debug
        FFPacket<NetObjectDestroyedEvent> packet = (FFPacket<NetObjectDestroyedEvent>)incommingPacket;

        // Remove copy of the object which exists for new clients to create the object
        // TODO: Low-Prio, Optimize for O(N), Needs custom List, very low prio.
        var list = FFServer.NetObjectsCreated;
        list.Remove(list.Find(x => x.gameObjectNetId == packet.message.gameObjectNetId));

        return 0;
    }
}

public class ClientSyncReply : MessageInspector<ClientSyncTimeEvent>
{
    public override uint Inspect(FFServer.ClientSocket clientSocket, FFBasePacket incommingPacket)
    {
        FFPacket<ClientSyncTimeEvent> packet = (FFPacket<ClientSyncTimeEvent>)incommingPacket;
        packet.message.serverLifeTime = FFServer.serverTime;
        clientSocket.SendNetPacket<ClientSyncTimeEvent>(packet, false);
        return 1; // return zero to to have this inspector pass this message
    }
}

