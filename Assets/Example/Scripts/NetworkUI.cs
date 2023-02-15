using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkUI : MonoBehaviour
{
    public void RequestLobby()
    {
        LobbyManager.Instance.RequestLobby();
    }
}
