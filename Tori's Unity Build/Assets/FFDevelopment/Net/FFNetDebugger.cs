using UnityEngine;
using System.Collections;

public class FFNetDebugger : FFComponent {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {

        var text = gameObject.GetOrAddComponent<TextMesh>();
        if (text)
        {
            if (FFServer.isClient) // Client is server
            {
                text.text = "FFNetDebugger " +
                        "\nServerName:" + FFServer.serverName +
                        "\nConnection Ready: " + FFClient.isReady +
                        "\nTCPIsConnected: " + FFClient.clientTCPIsConnected +
                        "\nTime (Client) : " + FFSystem.time +
                        "\nTime (Server): " + FFServer.serverTime +
                        "\nPing: " + FFClient.serverPing +
                        "\nSerWatDialation: " + FFClient.serverWatchDialation +
                        "\nFFclientWatTime: " + FFSystem.clientWatchTime +
                        "\nFFClientIPEndpoint: " + FFClient.ClientIPEndPoint +
                        "\nFFServerIPEndpoint: " + FFClient.ServerIPEndPoint +
                        "\n" +
                        "\nFFNetObjects: " + FFServer.NetObjectsCreated.Count;
            }
            else // client is not the server
            {
                text.text = "FFNetDebugger " +
                        "\nServerName: :" + FFClient.serverName +
                        "\nConnection Ready: " + FFClient.isReady +
                        "\nTCPIsConnected: " + FFClient.clientTCPIsConnected +
                        "\nTime: " + FFSystem.time +
                        "\nPing: " + FFClient.serverPing +
                        "\nSerWatDialation: " + FFClient.serverWatchDialation +
                        "\nFFclientWatTime: " + FFSystem.clientWatchTime +
                        "\nFFClientIPEndpoint: " + FFClient.ClientIPEndPoint +
                        "\nFFServerIPEndpoint: " + FFClient.ServerIPEndPoint;
            }
        }


        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.M))
        {
            var stats = FFPrivate.FFMessageSystem.GetStats();
            foreach (string stat in stats)
            {
                Debug.Log(stat);
            }
        }
	}
}
