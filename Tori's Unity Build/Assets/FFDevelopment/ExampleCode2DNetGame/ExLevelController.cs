using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;


public struct JoinServerEvent
{
    public string playerName;
    public System.Net.IPAddress serverIpAddress;
}

public struct StartNewServerEvent
{
    public string serverName;
    public System.Net.IPAddress serverIpAddress;
}

public struct ChangeLevelStateEvent
{
    public ExLevelController.LevelState newState;
}

public struct GetIPAddressEvent
{
    public System.Net.IPAddress ip;
    public bool isLocalIP; // else public
}

public class ExLevelController : FFComponent
{
    private FFAction.ActionSequence DisconnectSequence;

    public enum LevelState
    {
        In_Menu,
        In_Game,
    }

    #region MenuUIData
    public Transform InputField_PlayerName;
    public Transform InputField_ServerIPAddress;
    public Transform InputButton_StartNewServer;
    public Transform InputButton_JoinServer;
    public Transform InputButton_QuitGame;
    public Transform TextField_IPAddresses;
    public Transform TextField_Notice;

    InputField _inputField_playerName;
    InputField _inputField_serverIPAddress;
    Button _inputButton_startNewServer;
    Button _inputButton_joinServer;
    Button _inputButton_quitGame;
    Text _textField_iPAddresses;
    Text _textField_notice;
    #endregion

    #region GameUIData

    public Transform InputButton_LeaveServer;
    public Transform TextField_Wave;
    public Transform TextField_PlayerNames;

    Button _inputButton_leaveServer;
    Text _textField_wave;
    Text _textField_playerNames;

    #endregion

    #region Server/Player Data
    System.Net.IPAddress _serverIPAddress = null;
    string _playerName = null;

    System.Net.IPAddress _localIP = null;
    System.Net.IPAddress _publicIP = null;

    #endregion

    #region Start/OnDestroy
    void Start () {
        // Events
        FFMessage<ChangeLevelStateEvent>.Connect(OnChangeLevelStateEvent);
        FFMessage<GetIPAddressEvent>.Connect(OnGetIPAddressEvent);

        // Sequence
        DisconnectSequence = action.Sequence();

        // MenuUI
        if (InputField_PlayerName == null ||
            InputField_ServerIPAddress == null ||
            InputButton_StartNewServer == null ||
            InputButton_JoinServer == null ||
            TextField_IPAddresses == null ||
            TextField_Notice == null)
        {
            Debug.LogError("Error, ExLevelController is missing a refernce to a MenuUI object");
            return;
        }

        // GameUI
        if(InputButton_LeaveServer == null ||
           TextField_Wave == null ||
           TextField_PlayerNames == null)
        {

            Debug.LogError("Error, ExLevelController is missing a refernce to a GameUI object");
            return;
        }

        // Setup Text Input Events from UI
        // MenuUI
        _inputField_playerName = InputField_PlayerName.GetComponent<InputField>();
        _inputField_serverIPAddress = InputField_ServerIPAddress.GetComponent<InputField>();
        _inputButton_startNewServer = InputButton_StartNewServer.GetComponent<Button>();
        _inputButton_joinServer = InputButton_JoinServer.GetComponent<Button>();
        _inputButton_quitGame = InputButton_QuitGame.GetComponent<Button>();
        _textField_iPAddresses = TextField_IPAddresses.GetComponent<Text>();
        _textField_notice = TextField_Notice.GetComponent<Text>();

        // GameUI
        _inputButton_leaveServer = InputButton_LeaveServer.GetComponent<Button>();
        _textField_wave = TextField_Wave.GetComponent<Text>();
        _textField_playerNames = TextField_PlayerNames.GetComponent<Text>();

        // ------------------- Connect to events -------------------------------
        
        // MenuUI
	    _inputField_playerName.onEndEdit.AddListener(OnSubmitPlayerName);
        _inputField_serverIPAddress.onEndEdit.AddListener(OnSubmitServerIPAddress);
        _inputButton_startNewServer.onClick.AddListener(OnClickStartNewServer);
        _inputButton_joinServer.onClick.AddListener(OnClickJoinServer);
        _inputButton_quitGame.onClick.AddListener(OnClickQuitGame);

        // GameUI
        _inputButton_leaveServer.onClick.AddListener(OnClickLeaveServer);

        // Set Text fields
        
        FFClient.GetLocalIPEventStart();
        FFClient.GetPublicIPEventStart();

        // Set Level State
        ChangeLevelStateEvent CLSE;
        CLSE.newState = LevelState.In_Menu;
        FFMessage<ChangeLevelStateEvent>.SendToLocal(CLSE);
	}
    void OnDestroy()
    {
        FFMessage<ChangeLevelStateEvent>.Disconnect(OnChangeLevelStateEvent);
        FFMessage<GetIPAddressEvent>.Disconnect(OnGetIPAddressEvent);
    }
    #endregion

    private void OnGetIPAddressEvent(GetIPAddressEvent e)
    {
        if (e.isLocalIP) // Local IP
        {
            _localIP = e.ip;

            string text = "Local IP: ";
            if (_localIP != null)
            {
                text += _localIP;
            }
            text += "\nPublic IP: ";
            if (_publicIP != null)
            {
                text += _publicIP;
            }
            _textField_iPAddresses.text = text;
        }
        else // Public IP
        {
            _publicIP = e.ip;
            string text = "Local IP: ";
            if (_localIP != null)
            {
                text += _localIP;
            }
            text += "\nPublic IP: ";
            if (_publicIP != null)
            {
                text += _publicIP;
            }
            _textField_iPAddresses.text = text;

        }
    }
    private void OnChangeLevelStateEvent(ChangeLevelStateEvent e)
    {
        switch (e.newState)
        {
            case LevelState.In_Game:
                MenuUISetActive(false);
                GameUISetActive(true);
                CreatePlayer();
                break;

            case LevelState.In_Menu:
                MenuUISetActive(true);
                GameUISetActive(false);
                break;

            default:
                Debug.LogError("ExLevelController was changed to an invalid state");
                break;
        }
    }
    
    void CreatePlayer()
    {
        // TODO add start position to Player Spawn
        GameObject.Instantiate(FFResource.Load_Prefab("Player"));
    }
    void MenuUISetActive(bool active)
    {
        // Input Fields
        _inputField_playerName.gameObject.SetActive(active);
        _inputField_serverIPAddress.gameObject.SetActive(active);

        // Buttons
        _inputButton_startNewServer.gameObject.SetActive(active);
        _inputButton_joinServer.gameObject.SetActive(active);
        _inputButton_quitGame.gameObject.SetActive(active);

        // TextFields
        _textField_iPAddresses.gameObject.SetActive(active);
        _textField_notice.gameObject.SetActive(active);
    }
    void GameUISetActive(bool active)
    {
        // Buttons
        _inputButton_leaveServer.gameObject.SetActive(active);

        // Text Fields
        _textField_wave.gameObject.SetActive(active);
        _textField_playerNames.gameObject.SetActive(active);
    }

    #region MenuUIEvents

    private void OnClickQuitGame()
    {
        Debug.Log("QuitGameButton Pressed");
        Application.Quit();
    }
    private void OnClickJoinServer()
    {
        if(_serverIPAddress != null && _playerName != null && _playerName.Length > 0)
        {
            JoinServerEvent e;
            e.serverIpAddress = _serverIPAddress;
            e.playerName = _playerName;
            FFMessage<JoinServerEvent>.SendToLocal(e);

            FFClient.StartClient(_serverIPAddress.ToString(), 6532, _playerName);

            ChangeLevelStateEvent CLSE;
            CLSE.newState = LevelState.In_Game;
            FFMessage<ChangeLevelStateEvent>.SendToLocal(CLSE);
        }
        else
        {
            if (_serverIPAddress == null)
                NoticeValidIPAddress();
            if (_playerName == null)
                NoticeAddPlayerName();
        }

        Debug.Log("Clicked JoinServer"); // debug 
    }

    private void OnClickStartNewServer()
    {
        if(_playerName != null && _playerName.Length > 0)
        {
            StartNewServerEvent SNSE;
            string ipAddress = FFClient.GetLocalIP().ToString();
            SNSE.serverIpAddress = System.Net.IPAddress.Parse(ipAddress);
            SNSE.serverName = _playerName;
            FFMessage<StartNewServerEvent>.SendToLocal(SNSE);

#if UNITY_WEBPLAYER
            // Start + Connect to local Server
            FFServer.StartServer(80, _playerName + "'s Server", true);
            FFClient.StartClient(ipAddress, 80, _playerName);
#endif

#if UNITY_STANDALONE
            // Start + Connect to local Server
            FFServer.StartServer(6532, _playerName + "'s Server", true);
            FFClient.StartClient(ipAddress, 6532, _playerName);
#endif
            ChangeLevelStateEvent CLSE;
            CLSE.newState = LevelState.In_Game;
            FFMessage<ChangeLevelStateEvent>.SendToLocal(CLSE);
        }
        else
        {
            NoticeAddPlayerName();
        }
        Debug.Log("Clicked StartNewServer"); // debug 
    }

    private void OnSubmitServerIPAddress(string arg0)
    {
        try
        {
            _serverIPAddress = System.Net.IPAddress.Parse(arg0);
        }
        catch (Exception exp)
        {
            Debug.Log("Bad IpAddress.  " + exp.Message);
            _serverIPAddress = null;
            NoticeValidIPAddress();
        }

        Debug.Log("Submitted ServerIpAddress"); // debug
    }

    private void OnSubmitPlayerName(string arg0)
    {
        if (arg0.Length > 32)
            Debug.Log("Name is too long");
        else
        {
            _playerName = arg0;
            Debug.Log("Submitted PlayerName"); // debug
        }
    }
    #endregion

    #region GameUIEvents

    private void OnClickLeaveServer()
    {
        ChangeLevelStateEvent CLSE;
        CLSE.newState = LevelState.In_Menu;
        FFMessage<ChangeLevelStateEvent>.SendToLocal(CLSE);

        // 1.0 seconds to make sure everything is finished in the ChangelevelState event
        // before we close down networking
        DisconnectSequence.Delay(0.5);
        DisconnectSequence.Sync();
        DisconnectSequence.Call(DestroyClientAndServer);
    }

    private void DestroyClientAndServer()
    {
        // if server, close server
        var ffclientgo = GameObject.Find("FFClient"); // Client Connection
        var ffservergo = GameObject.Find("FFServer"); // Server Connection
        var ffSystemgo = GameObject.Find("FFSystem"); // Local Net GameObject References

        if (ffclientgo && ffclientgo.GetComponent<FFClient>()) // Local Server in which we are connected to
        {
            Destroy(ffclientgo);
        }

        if (ffservergo && ffservergo.GetComponent<FFServer>())
        {
            Destroy(ffservergo);
        }

        if(ffSystemgo && ffSystemgo.GetComponent<FFSystem>())
        {
            Destroy(ffSystemgo);
        }
    }

    #endregion

    #region Notice
    public void NoticeAddPlayerName()
    {
        _textField_notice.text = "Please Enter a name";
        ShowNotice();
    }

    public void NoticeValidIPAddress()
    {
        _textField_notice.text = "Please Enter a valid IP Address.";
        ShowNotice();
    }

    void ShowNotice()
    {
        _textField_notice.color = Color.cyan;
        var action = TextField_Notice.GetOrAddComponent<FFAction>();
        action.ClearAllSequences();
        var seq = action.Sequence();
        seq.Delay(3.5);
        seq.Sync();
        seq.Call(HideNotice);
    }

    void HideNotice()
    {
        _textField_notice.color = new Color(0, 0, 0, 0);
    }
    #endregion

}
