using FishNet;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyAsServer : MonoBehaviour
{
    void Start()
    {
        if (InstanceFinder.IsServerOnly) Destroy(gameObject);
    }
}
