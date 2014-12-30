using UnityEngine;
using System.Collections;

public class MessageHandler : MonoBehaviour {

    public GameObject BotPrefab;

    void OnEnable() {
        NetworkUtils.connection.OnMessage += connection_OnMessage;
        NetworkUtils.connection.OnDisconnect += connection_OnDisconnect;
    }

    // We'll also disconnect our event handlers.
    void OnDisable() {
        NetworkUtils.connection.OnMessage -= connection_OnMessage;
        NetworkUtils.connection.OnDisconnect -= connection_OnDisconnect;
    }

    void connection_OnDisconnect(object sender, string message) {
        Debug.Log("Disconnected from server");
        NetworkUtils.connection = null;

        // Save player object
        NetworkUtils.playerObject.Save();

        // Go back to main menu
        Application.LoadLevel("Lobby");
    }

    void connection_OnMessage(object sender, PlayerIOClient.Message e) {
        // Handle incoming messages
        switch (e.Type) {
            // Server sent us our ID
            case "SetID":
                NetworkUtils.localPlayerID = e.GetInt(0);
                break;
            // Add a player to list of players in the room
            case "UserJoined":
                NetworkUtils.PlayersInRoom.Add(e.GetInt(0), e.GetString(1));
                break;
            // Remove player from list of players
            case "UserLeft":
                NetworkUtils.PlayersInRoom.Remove(e.GetInt(0));

                // Clean up this player's bots
                foreach (ulong botID in BotInfo.botMap.Keys) {
                    Destroy(BotInfo.botMap[botID].gameObject);
                }
                break;
            // Spawn a bot
            case "OnBotSpawned":
                int spawnedBotOwnerID = e.GetInt(0);
                ulong spawnedBotID = e.GetULong(1);
                float spawnedBotPosX = e.GetFloat(2);
                float spawnedBotPosY = e.GetFloat(3);

                GameObject bot = (GameObject)Instantiate(BotPrefab, new Vector3(spawnedBotPosX, 0f, spawnedBotPosY), Quaternion.identity);
                bot.GetComponent<BotInfo>().OwnerID = spawnedBotOwnerID;
                bot.GetComponent<BotInfo>().BotID = spawnedBotID;
                bot.GetComponent<BotInfo>().Register();
                break;
            // Update a bot
            case "UpdateBot":
                ulong updateBotID = e.GetULong(0);
                float updatePosX = e.GetFloat(1);
                float updatePosY = e.GetFloat(2);
                int updateBotHealth = e.GetInt(3);

                if (BotInfo.botMap.ContainsKey(updateBotID)) {
                    BotInfo updateBot = BotInfo.botMap[updateBotID];
                    updateBot.transform.position = new Vector3(updatePosX, 0f, updatePosY);
                    updateBot.SendMessage("SetHealth", updateBotHealth, SendMessageOptions.DontRequireReceiver);
                }
                break;
            // Destroy a bot
            case "BotDied":
                // Kill bot
                ulong killedBotID = e.GetULong(0);

                Debug.Log("Bot died: " + killedBotID);

                BotInfo killedBot = BotInfo.botMap[killedBotID];
                if (killedBot.IsMine) {
                    // Increment lost bots
                    NetworkUtils.playerObject.Set("Deaths", NetworkUtils.playerObject.GetInt("Deaths") + 1);
                }

                // Destroy bot obj
                GameObject.Destroy(killedBot.gameObject);
                break;
            // Local player got a kill
            case "GotKill":
                // Increment kills
                NetworkUtils.playerObject.Set("Kills", NetworkUtils.playerObject.GetInt("Kills") + 1);
                break;
            // One of local player's bots took damage
            case "TookDamage":
                Debug.Log("Taking damage");
                break;
        }
    }
}
