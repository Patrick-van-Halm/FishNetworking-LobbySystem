using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyUI : MonoBehaviour
{
    public void StartLobby()
    {
        LobbyManager.Instance.StartLobby();
    }
}
