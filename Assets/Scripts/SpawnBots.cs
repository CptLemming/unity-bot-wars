using UnityEngine;
using System.Collections;

public class SpawnBots : MonoBehaviour {

    public GameObject BotPrefab;

    public Transform[] SpawnPoints;

    private int lastGenatedBotID = 0;

    IEnumerator Start() {
        // 3 second prepare time
        yield return new WaitForSeconds(3f);

        foreach (Transform spawn in SpawnPoints) {
            ulong botID = AllocateBotID();

            GameObject bot = (GameObject)Instantiate(BotPrefab, new Vector3(spawn.position.x, 0f, spawn.position.z), Quaternion.identity);
            bot.GetComponent<BotInfo>().OwnerID = NetworkUtils.localPlayerID;
            bot.GetComponent<BotInfo>().BotID = botID;

            // Send spawn message to server
            NetworkUtils.connection.Send("SpawnBot", botID, spawn.position.x, spawn.position.z);
        }
    }

    ulong AllocateBotID() {
        // Here, we will generate a unique network ID for one of our bots
        // To do this without server intervention, the ID will be based on the Player ID
        ulong id = (ulong)lastGenatedBotID++;
        id |= ((ulong)NetworkUtils.localPlayerID << 4);

        return id;
    }
}
