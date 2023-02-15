using FishNet;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoStartServer : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        InstanceFinder.ServerManager.StartConnection();
    }
}
