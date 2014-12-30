using UnityEngine;
using System.Collections.Generic;

using PlayerIOClient;

public class ConnectScreen : MonoBehaviour {

    string playerName = "Player";

    bool connecting = false;

    public bool UseDevServer = true;

    int botsKilled = 0;
    int botsLost = 0;

    void Connect() {
        // We need some monobeviour in the scene for player.io to work, but it can be any monobahaviour so we'll just put our NetworkUtils component on it.
        if (GameObject.Find("_playerIO") == null) {
            GameObject go = new GameObject("_playerIO");
            go.AddComponent<NetworkUtils>();
            DontDestroyOnLoad(go);
            PlayerIO.UnityInit(go.GetComponent<NetworkUtils>());
        }

        PlayerIO.Connect("botwar-fkrsnzsev0ui0ihlrk2bg", "public", playerName, null, null, delegate(Client client) {
            Debug.Log("Connected");

            // Store client for later retrieval
            NetworkUtils.client = client;

            // Load player object
            client.BigDB.LoadMyPlayerObject(delegate(DatabaseObject playerObj) {
                // Store player object for later retrieval
                NetworkUtils.playerObject = playerObj;

                // Read state from player object
                botsKilled = playerObj.GetInt("Kills", 0);
                botsLost = playerObj.GetInt("Deaths", 0);
            }, delegate(PlayerIOError error) {
                Debug.Log("Failed loading player object: " + error.Message);
            });

            if (UseDevServer) {
                client.Multiplayer.DevelopmentServer = new ServerEndpoint("127.0.0.1", 8184);
            }
        }, delegate(PlayerIOError error) {
            Debug.Log("Failed to connect: " + error.Message);
        });
    }

    void JoinRoom() {
        NetworkUtils.client.Multiplayer.CreateJoinRoom("$service-room$", "GameRoom", true, null, new Dictionary<string, string> { }, delegate(Connection connection) {
            Debug.Log("Connected to room");
            NetworkUtils.connection = connection;

            // Load gameplay scene
            Application.LoadLevel("GameplayScene");
        }, delegate(PlayerIOError error) {
            Debug.Log("Failed to join room: " + error.Message);
        });
    }

    void OnGUI() {
        if (!connecting) {
            if (NetworkUtils.playerObject != null) {
                GUILayout.Label("Enemy Bots Destroyed: " + botsKilled);
                GUILayout.Label("Bots Lost: " + botsLost);
                if (GUILayout.Button("Play", GUILayout.Width(100f))) {
                    // Join random room
                    JoinRoom();
                }
            } else {
                playerName = GUILayout.TextField(playerName, GUILayout.Width(200f));
                if (GUILayout.Button("Connect", GUILayout.Width(200f))) {
                    Connect();
                }
            }
        } else {
            GUILayout.Label("Connecting...");
        }
    }
}
