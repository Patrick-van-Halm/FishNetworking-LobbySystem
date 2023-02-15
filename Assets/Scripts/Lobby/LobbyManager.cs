using FishNet;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class LobbyManager : SingletonNetworkBehaviour<LobbyManager>
{
    [Header("Scene Settings")]
    [SerializeField, Min(1)] private int _maxPooledScenes = 1;
    [FishNet.Utility.Scene, SerializeField] private string _lobbyScene;
    [FishNet.Utility.Scene, SerializeField] private string _gameScene;

    [Header("Lobby Settings")]
    [Min(1)] public int MaxLobbyClients = 1;
    [Min(1)] public int MinLobbyClients = 1;

    private readonly List<Lobby> _lobbies = new();
    private readonly List<Scene> _pooledLobbyScenes = new();
    private readonly List<Scene> _pooledGameScenes = new();

    /// <summary>
    /// SERVER ONLY:
    /// Finds the Lobby that a specific client is in, returns null if not found any.
    /// </summary>
    /// <param name="client">The client that is searched for</param>
    /// <returns>The Lobby the client is in</returns>
    [Server]
    public Lobby FindLobbyOfClient(NetworkConnection client) => _lobbies.FirstOrDefault(x => x.HasClient(client));

    protected override void Awake()
    {
        base.Awake();
        if(!ValidateScene(_lobbyScene, "Lobby Scene"))
        {
            enabled = false;
            return;
        }

        if(!ValidateScene(_gameScene, "Game Scene"))
        {
            enabled = false;
            return;
        }
    }

    private bool ValidateScene(string sceneName, string sceneType)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError($"{sceneType} doesn't have a value!");
            return false;
        }

        return true;
    }

    [Server]
    public override void OnStartServer()
    {
        base.OnStartServer();
        LobbyScenePooling();
        GameScenePooling();

        InstanceFinder.ServerManager.OnRemoteConnectionState += RemoteConnectionStateChanged;
    }

    [Server]
    private void RemoteConnectionStateChanged(NetworkConnection client, RemoteConnectionStateArgs args)
    {
        // Update the clients lobby that they have disconnected and left.
        if (args.ConnectionState != RemoteConnectionState.Stopped) return;

        Lobby lobby = FindLobbyOfClient(client);
        if (lobby == null) return;

        lobby.ClientLeft(client);
    }

    #region Lobby Scene Pooling
    [Server]
    private void LobbyScenePooling()
    {
        // Always load the minimum amount of pooled scenes so it doesn't take long to start a lobby
        if (_pooledLobbyScenes.Count == _maxPooledScenes) return;
        InstanceFinder.SceneManager.OnLoadEnd += LobbyScenePooling_SceneLoadEnd;
        for (int i = _pooledLobbyScenes.Count; i < _maxPooledScenes; i++)
            InstanceFinder.SceneManager.LoadConnectionScenes(new SceneLoadData
            {
                SceneLookupDatas = new SceneLookupData[]
                    {
                    new(_lobbyScene)
                    },
                Options = new LoadOptions()
                {
                    AllowStacking = true,
                    AutomaticallyUnload = false
                }
            });
    }

    [Server]
    private void LobbyScenePooling_SceneLoadEnd(SceneLoadEndEventArgs args)
    {
        // If a scene is newly loaded check if its a lobby scene and if it's not already in the pooled list then add it
        if (args.LoadedScenes.Length != 1) return;
        if (args.LoadedScenes[0].path != _lobbyScene) return;
        if (_pooledLobbyScenes.Contains(args.LoadedScenes[0])) return;
        if (args.LoadedScenes[0].GetRootGameObjects().FirstOrDefault(g => g.GetComponent<BaseLobbyInstance>()) == null) return;
        _pooledLobbyScenes.Add(args.LoadedScenes[0]);

        if (_pooledLobbyScenes.Count == _maxPooledScenes)
            InstanceFinder.SceneManager.OnLoadEnd -= LobbyScenePooling_SceneLoadEnd;
    }
    #endregion

    #region Lobby Joining
    /// <summary>
    /// SERVER RPC:
    /// Request an open lobby, if not found any then create a new one. Then move the Client to Lobby, and let Client load current active lobby scene.
    /// </summary>
    /// <param name="client">Executing client (Leave empty)</param>
    [ServerRpc(RequireOwnership = false)]
    public void RequestLobby(NetworkConnection client = null)
    {
        // Request a quick lobby not caring about any settings. Creates a new one if one is not available.
        Lobby lobby;
        if (_lobbies.Count == 0)
            lobby = CreateNewLobby();
        else
        {
            lobby = _lobbies.Find(l => l.CanJoin);
            if (lobby == null) lobby = CreateNewLobby();
        }

        lobby.ClientJoin(client);
        LoadLobbySceneForClient(lobby, client);
    }

    [Server]
    private void LoadLobbySceneForClient(Lobby lobby, NetworkConnection client)
    {
        // Loads the current lobby scene for a client
        SceneLoadData sld = new SceneLoadData(lobby.Scene);
        sld.Options.AllowStacking = true;
        sld.Options.LocalPhysics = LocalPhysicsMode.Physics3D;
        sld.ReplaceScenes = ReplaceOption.All;
        sld.MovedNetworkObjects = new NetworkObject[] { client.Objects.ElementAt(0) };
        sld.PreferredActiveScene = new SceneLookupData(lobby.Scene);
        InstanceFinder.SceneManager.LoadConnectionScenes(client, sld);
    }

    [Server]
    private void LoadLobbySceneForClientsInLobby(Lobby lobby)
    {
        // Loads the current lobby scene for all clients in the lobby
        SceneLoadData sld = new SceneLoadData(lobby.Scene);
        sld.Options.AllowStacking = true;
        sld.Options.LocalPhysics = LocalPhysicsMode.Physics3D;
        sld.ReplaceScenes = ReplaceOption.All;
        sld.MovedNetworkObjects = lobby.Clients.Select(c => c.Objects.ElementAt(0)).ToArray();
        sld.PreferredActiveScene = new SceneLookupData(lobby.Scene);
        InstanceFinder.SceneManager.LoadConnectionScenes(lobby.Clients, sld);
    }
    #endregion

    #region Lobby Management
    /// <summary>
    /// SERVER RPC:
    /// Starts the Lobby for all clients in lobby, only able to be executed by the Lobby owner
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void StartLobby(NetworkConnection sender = null)
    {
        Lobby lobby = FindLobbyOfClient(sender);
        if(lobby == null) return;
        if (sender != lobby.Owner) return;
        SwitchToGameScene(lobby);
    }

    /// <summary>
    /// SERVER ONLY:
    /// Creates a new Lobby and restarts Pooling for lobby scenes
    /// </summary>
    /// <returns>Newly created Lobby</returns>
    [Server]
    public Lobby CreateNewLobby()
    {
        // Create a new lobby with the pooled lobby scene if one exists. Always runs the Pooling afterwards to make sure there are enough scenes loaded
        Lobby lobby = null;
        if (_pooledLobbyScenes.Count > 0)
        {
            lobby = new(_pooledLobbyScenes[0]);
            _lobbies.Add(lobby);
            lobby.Scene.GetRootGameObjects().Select(g => g.GetComponent<BaseLobbyInstance>()).FirstOrDefault(s => s).Use(lobby);

            _pooledLobbyScenes.RemoveAt(0);
        }
        LobbyScenePooling();
        return lobby;
    }

    /// <summary>
    /// SERVER ONLY:
    /// Cleans up a Lobby and unloads the current active scene.
    /// </summary>
    /// <param name="lobby">The Lobby to cleanup</param>
    [Server]
    public void CleanupLobby(Lobby lobby)
    {
        // Removes and unloads a lobby from the server
        Scene lobbyScene = lobby.Scene;
        _lobbies.Remove(lobby);
        StartCoroutine(CoroUnloadScene(lobbyScene));
    }

    private IEnumerator CoroUnloadScene(Scene scene)
    {
        // Wait until all clients are out of the scene and then unload the scene
        yield return new WaitUntil(() => !scene.GetRootGameObjects().Any(g => g.GetComponent<NetworkObject>().IsClient));
        InstanceFinder.SceneManager.UnloadConnectionScenes(new(scene));
    }
    #endregion

    #region Lobby Switch To Game
    /// <summary>
    /// SERVER ONLY:
    /// Switches the Lobby to the Game scene, it unloads the current Lobby scene and restarts Pooling for Game Scenes
    /// </summary>
    /// <param name="lobby">The target Lobby</param>
    [Server]
    public void SwitchToGameScene(Lobby lobby)
    {
        // Switch the lobby to the game scene and runs the game scene pooling
        lobby.StartLobby();
        Scene currentScene = lobby.Scene;
        GameInstance game = null;
        if (_pooledGameScenes.Count > 0)
        {
            lobby.Scene = _pooledGameScenes[0];
            _pooledGameScenes.RemoveAt(0);
            game = lobby.Scene.GetRootGameObjects().Select(g => g.GetComponent<GameInstance>()).FirstOrDefault(s => s);
            game.Use(lobby);

            LoadLobbySceneForClientsInLobby(lobby);
        }
        GameScenePooling();
        StartCoroutine(CoroUnloadScene(currentScene));
        StartCoroutine(ValidateAllClientsInGameScene(lobby, game));
    }

    private IEnumerator ValidateAllClientsInGameScene(Lobby lobby, GameInstance game)
    {
        if (!game) yield break;
        yield return new WaitUntil(() =>
        {
            bool containsAllClients = true;
            foreach (var client in lobby.Clients)
            {
                if (client.FirstObject == null)
                {
                    containsAllClients = false;
                    break;
                }

                if (!client.FirstObject.isActiveAndEnabled)
                {
                    containsAllClients = false;
                    break;
                }

                if (!client.Scenes.Contains(lobby.Scene))
                {
                    containsAllClients = false;
                    break;
                }

                if (!lobby.Scene.GetRootGameObjects().Any(g => g == client.FirstObject.gameObject))
                {
                    containsAllClients = false;
                    break;
                }
            }
            return containsAllClients;
        });

        game.Ready();
    }
    #endregion

    #region Game Scene Pooling
    [Server]
    private void GameScenePooling()
    {
        // Game scene pooling like the lobby scene pooling
        if (_pooledGameScenes.Count == _maxPooledScenes) return;
        InstanceFinder.SceneManager.OnLoadEnd += GameScenePooling_SceneLoadEnd;
        for (int i = _pooledGameScenes.Count; i < _maxPooledScenes; i++)
            InstanceFinder.SceneManager.LoadConnectionScenes(new SceneLoadData
            {
                SceneLookupDatas = new SceneLookupData[]
                    {
                    new(_gameScene)
                    },
                Options = new LoadOptions()
                {
                    LocalPhysics = LocalPhysicsMode.Physics3D,
                    AllowStacking = true,
                    AutomaticallyUnload = false
                }
            });
    }

    [Server]
    private void GameScenePooling_SceneLoadEnd(SceneLoadEndEventArgs args)
    {
        // Checks if the newly loaded scene is a game scene and adds it to the pooled game scenes
        if (args.LoadedScenes.Length != 1) return;
        if (args.LoadedScenes[0].path != _gameScene) return;
        if (_pooledGameScenes.Contains(args.LoadedScenes[0])) return;
        if (args.LoadedScenes[0].GetRootGameObjects().FirstOrDefault(g => g.GetComponent<BaseLobbyInstance>()) == null) return;
        _pooledGameScenes.Add(args.LoadedScenes[0]);

        if (_pooledGameScenes.Count == _maxPooledScenes)
            InstanceFinder.SceneManager.OnLoadEnd -= GameScenePooling_SceneLoadEnd;
    }
    #endregion
}