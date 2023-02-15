using FishNet.Object;

public abstract class BaseLobbyInstance : NetworkBehaviour
{
    public Lobby Lobby { get; private set; }

    [Server]
    public virtual void Use(Lobby lobby)
    {
        // Sets the lobby when lobby is created
        Lobby = lobby;
        Lobby.OnClientLeave.AddListener(ClientLeftLobby);
    }

    [Server]
    protected virtual void ClientLeftLobby()
    {
        // If client lobby and lobby is now empty mark lobby for cleanup
        if (Lobby == null) return;
        if (Lobby.Clients.Length == 0) LobbyManager.Instance.CleanupLobby(Lobby);
    }
}
