# Fish Networking Lobby System
This is a system that is made to create lobbies and host separate game scenes for a max amount of players.

## Example Project
In this repo there is an example project to show a way to setup this system.

### Scenes

#### Server
This is the scene that starts an instance of the Fish Networking server. It also loads the Networking Essentials scene as a global scene.

#### Networking Essentials
This is the scene that contains all the networked managers that always be used not caring about the lobby scenes.

#### Lobby
The lobby scene that will be used. This has a GameObject called LobbyInstance, this GameObject holds all the data of a lobby (only on server) that is required to operate the lobby.

#### Game
The Game scene that will be used. Just as the Lobby Scene holds this a GameInstance, this is the same as LobbyInstance just having its own state included.

#### Client
This is the scene that will be used to start the client. When the client has joined they can then request a Lobby of the server and will then join that lobby.

## Expanding
You are allowed to expand this project in any way. Have any improvements? Open a pull request!