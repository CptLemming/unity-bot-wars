using UnityEngine;
using System.Collections.Generic;

using PlayerIOClient;

public class NetworkUtils : MonoBehaviour {

    public static Client client;
    public static Connection connection;
    public static DatabaseObject playerObject;
    public static int localPlayerID;
    public static Dictionary<int, string> PlayersInRoom = new Dictionary<int, string>();
}
