using UnityEngine;
using System.Collections.Generic;

public class BotInfo : MonoBehaviour {

	// A map of botId -> bot
    public static Dictionary<ulong, BotInfo> botMap = new Dictionary<ulong, BotInfo>();

    // The player that owns this bot
    public int OwnerID;

    // The ID of this bot
    public ulong BotID;

    // Whether this bot belongs to the local player
    public bool IsMine {
        get {
            return OwnerID == NetworkUtils.localPlayerID;
        }
    }

    public void Register() {
        botMap.Add(this.BotID, this);
    }

    void OnDestroy() {
        botMap.Remove(this.BotID);
    }

    float timer = 0f;
    void Update() {
        if (IsMine) {
            timer += Time.deltaTime;
            if (timer >= 0.1f) {
                // Send update message to server
                NetworkUtils.connection.Send("UpdateBot", BotID, transform.position.x, transform.position.z);
            }
        }
    }
}
