using FishNet;
using FishNet.Object;
using FishNet.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadGlobalScene : NetworkBehaviour
{
    [SerializeField, Scene] private string _globalScene;

    public override void OnStartServer()
    {
        base.OnStartServer();
        InstanceFinder.SceneManager.LoadGlobalScenes(new(_globalScene));
    }
}
