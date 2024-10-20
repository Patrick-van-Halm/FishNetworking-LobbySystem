using FishNet;
using FishNet.Object;
using FishNet.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using GameKit.Dependencies.Utilities.Types;

public class LoadGlobalScene : NetworkBehaviour
{
    [SerializeField, GameKit.Dependencies.Utilities.Types.Scene] private string _globalScene;

    public override void OnStartServer()
    {
        base.OnStartServer();
        InstanceFinder.SceneManager.LoadGlobalScenes(new(_globalScene));
    }
}
