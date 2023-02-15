using FishNet;
using FishNet.Transporting;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class OnConnected : MonoBehaviour
{
    public UnityEvent OnClientConnected;
    // Start is called before the first frame update
    void Start()
    {
        InstanceFinder.ClientManager.OnClientConnectionState += ClientStateChanged;
    }

    private void ClientStateChanged(ClientConnectionStateArgs e)
    {
        if (e.ConnectionState == LocalConnectionState.Started) OnClientConnected?.Invoke();
    }
}
