using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingletonNetworkBehaviour<T> : NetworkBehaviour where T : NetworkBehaviour
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance && Instance != this as T)
        {
            Destroy(this);
            return;
        }

        if (Instance == null) Instance = this as T;
    }
}
