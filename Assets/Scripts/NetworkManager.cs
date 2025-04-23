using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

public class NetworkManager : MonoBehaviour
{
    public AudioSource musicAudioSource;

    public GameObject playerPrefab;
    public GameObject ballPrefab;

    public dfButton createServerButton;
    public dfListbox gamesListbox;
    public dfTextbox newGameNameTextbox;
    public dfLabel whoWonLabel;
    public dfExpressionPropertyBinding createServerConditionBinding;

    public bool RefreshingHostsList { get; private set; }
    public bool GameStarted { get; private set; }

    public event TweenNotification LeaveMainMenu;
    public event TweenNotification WaitingForAnotherPlayer;
    public event TweenNotification GameStarting;

    public event TweenNotification GameEnded;

    public event TweenNotification ServerWon;
    public event TweenNotification ClientWon;

    public event TweenNotification ReturnToMainMenu;

    public int MaxScore
    {
        get { return 10; }
    }

    private const string typeName = "PongMultiOnline";
    private HostData[] hostList;
    public HostData Connected { get; private set; }
    public bool Ended { get; private set; }

	// Use this for initialization

    private float widthLimit = 6f, heightLimit = 4.55f;

	void Start ()
	{
	    Ended = false;

	    RefreshingHostsList = false;
        gamesListbox.Items = new string[0];

	    InvokeRepeating("UpdateGamesListAutomatically", 0.1f, 5f);

        /***/

        if (Application.isEditor)
        {
              //var ball = CreateBall();
              //ball.ServerStart(heightLimit, widthLimit);
        }
        /**/
	}

    void Update()
    {
        if (Network.isServer)
        {
           if (!GameStarted && Network.connections.Length > 0)
           {
               networkView.RPC("StartGame", RPCMode.All);

               var viewID = Network.AllocateViewID();
               var ball = CreateBall(viewID);
               networkView.RPC("CreateBall", RPCMode.Others, viewID);
               ball.ServerStart(heightLimit, widthLimit);
           }
        }
    }

    public void Server_PlayerWon(PlayerType winningPlayer)
    {
        networkView.RPC("GameEnd", RPCMode.All, (int)winningPlayer);
    }

    [RPC]
    private void GameEnd(int winner)
    {
        var winningPlayer = (PlayerType) winner;

        var whoWon = String.Format("The {0} won.", winningPlayer == PlayerType.Server ? "server" : "client");
        whoWonLabel.Text = whoWon;

        StartCoroutine(HomelessMethods.InvokeInSeconds(1.5f, () =>
        {
            Ended = true;
            GameEnded();

            if (winningPlayer == PlayerType.Server)
            {
                ServerWon();
            }
            else
            {
                ClientWon();
            }

        }));

        StartCoroutine(HomelessMethods.InvokeInSeconds(3f, () =>
        {
            Destroy(currentPlayer);
            Connected = null;
            Network.Disconnect();
        }));

        StartCoroutine(WaitForInputToReturnToMenu());
    }

    IEnumerator WaitForInputToReturnToMenu()
    {
        while (!Input.GetKeyDown(KeyCode.Space))
        {
            yield return null;
        }

        //Destroy(currentPlayer);

        /*
        foreach (var player in FindObjectsOfType<Player>())
        {
            Destroy(player.gameObject);
        }
         */

        StartCoroutine(HomelessMethods.Interpolate(1f, 0f, 1f, InterpolationMethods.Lerp, f =>
        {
            musicAudioSource.volume = f;
        }));
        
        //iTween.AudioTo(musicAudioSource.gameObject, 0f, 1.5f, 1f);

        ReturnToMainMenu();

        StartCoroutine(HomelessMethods.InvokeInSeconds(2f, () =>
        {
            Application.LoadLevel(Application.loadedLevel);
        }));

        createServerConditionBinding.enabled = true;
    }

    [RPC]
    Ball2 CreateBall(NetworkViewID viewID)
    {
        var ballObject = (GameObject) Instantiate(ballPrefab, Vector3.zero, Quaternion.identity);
        ballObject.SetActive(true);

        ballObject.GetComponent<NetworkView>().viewID = viewID;

        return ballObject.GetComponent<Ball2>();
    }

    [RPC]
    private void StartGame()
    {
        // Someone else is connected
        Debug.Log("STARTING GAME");
        GameStarted = true;
        GameStarting();

    }

    void UpdateGamesListAutomatically()
    {
        // if (hostList == null || (hostList != null && hostList.Length == 0))
        // {
            RefreshHostList();
        // }
    }

    private void StartServer()
    {
        Debug.Log("Starting Server");
        Network.InitializeServer(2, 25000, !Network.HavePublicAddress());
        MasterServer.RegisterHost(typeName, newGameNameTextbox.Text);
    }

    void OnServerInitialized()
    {
        Network.maxConnections = 1;

        Debug.Log("Server Initializied");
        SpawnPlayer(true);

        if (LeaveMainMenu != null)
        {
            LeaveMainMenu();
        }
    }

    void OnConnectedToServer()
    {
        Debug.Log("Connected players: " + Connected.connectedPlayers);
        Debug.Log("Server Joined");

        SpawnPlayer(false);

        if (LeaveMainMenu != null)
        {
            LeaveMainMenu();
        }
    }

    private GameObject currentPlayer;
    private void SpawnPlayer(bool isPlayerHosting)
    {
        var playerDirection = isPlayerHosting ? -1 : 1;
        var playerPosition = new Vector3(playerDirection*4.75f, 0);
        currentPlayer = (GameObject)Network.Instantiate(playerPrefab, playerPosition, Quaternion.identity, 0);
    }

    private void RefreshHostList()
    {
        RefreshingHostsList = true;
        MasterServer.RequestHostList(typeName);
    }

    void OnMasterServerEvent(MasterServerEvent msEvent)
    {
        if (msEvent == MasterServerEvent.HostListReceived)
        {
            hostList = MasterServer.PollHostList();

            var newHosts = new List<string>();
            foreach (var host in hostList)
            {
                if (host.connectedPlayers == 2)
                {
                    // Server if full
                    continue;
                }

                var tmpIp = "";
                foreach (var t in host.ip) {
                    tmpIp = t + " ";
                }

                newHosts.Add(string.Format("[{0}] {1}", tmpIp, host.gameName));
            }

            gamesListbox.Items = newHosts.ToArray();
            //gamesListbox.Items = Enumerable.Range(0, 100).Select(s => string.Format("{0} Some longish sentence {0}", s)).ToArray();
            if (newHosts.Count == 0)
            {
                gamesListbox.SelectedIndex = -1;
            }

            RefreshingHostsList = false;
        }
    }

    private void JoinServer(HostData hostData)
    {
        if (Connected != null)
        {
            return;
        }

        Connected = hostData;
        Debug.Log("Joining game: " + hostData.gameName);
        var error = Network.Connect(hostData);

        if (error != NetworkConnectionError.NoError)
        {
            Debug.Log("Something went wrong: " + error);
        }
    }

    public void MainPanelFaded()
    {
        if (Network.isServer)
        {
            WaitingForAnotherPlayer();
        }
    }

    public void JoinGameButtonPressed(dfControl ignore, dfMouseEventArgs args)
    {
        var host = hostList[gamesListbox.SelectedIndex];
        if (host.connectedPlayers == 2)
        {
            // Server is full; sorry.
            return;
        }

        JoinServer(host);
    }

    public void CreateGameButtonPressed(dfControl ignore, dfMouseEventArgs args)
    {
        createServerConditionBinding.enabled = false;
        createServerButton.Disable();

        StartServer();
    }

    public void RefreshButtonPressed(dfControl ignore, dfMouseEventArgs args)
    {
        RefreshHostList();
    }
}
