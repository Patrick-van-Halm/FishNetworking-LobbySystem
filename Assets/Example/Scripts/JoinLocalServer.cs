using FishNet;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JoinLocalServer : MonoBehaviour
{
    public void Join()
    {
        InstanceFinder.ClientManager.StartConnection();
    }
}
