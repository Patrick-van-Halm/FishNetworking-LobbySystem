using FishNet;
using FishNet.Connection;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class Lobby
{
    private bool _hasStarted;
    private List<NetworkConnection> _clients;
    public UnityEvent OnClientJoin = new();
    public UnityEvent OnClientLeave = new();
    public UnityEvent OnStartLobby = new();

    public Lobby(Scene scene)
    {
        Id = "1";
        Scene = scene;
        _clients = new();
    }

    public bool HasClient(NetworkConnection client) => _clients.Contains(client);

    public void ClientLeft(NetworkConnection client)
    {
        if (!InstanceFinder.IsServer) return;
        if (!HasClient(client)) return;
        _clients.Remove(client);

        if(client == Owner) Owner = _clients.FirstOrDefault();

        OnClientLeave?.Invoke();
    }

    public void ClientJoin(NetworkConnection client)
    {
        if (!InstanceFinder.IsServer) return;
        if (HasClient(client)) return;
        _clients.Add(client);
        
        if (Owner == null) Owner = client;

        OnClientJoin?.Invoke();
    }

    public void StartLobby()
    {
        _hasStarted = true;
        OnStartLobby?.Invoke();
    }

    public bool CanStart => _clients.Count >= LobbyManager.Instance.MinLobbyClients;

    public bool CanJoin => !_hasStarted && !Locked && _clients.Count < LobbyManager.Instance.MaxLobbyClients;

    public bool Locked { get; set; }

    public NetworkConnection[] Clients => _clients.ToArray();

    public string Id { get; private set; }

    public Scene Scene { get; set; }

    public NetworkConnection Owner { get; private set; }
}