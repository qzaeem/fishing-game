using UnityEngine;
using Unity.Netcode;

public static class ExtensionMethods 
{
    public static bool LocalClientIsOwner(this NetworkBehaviour networkBehaviour)
    {
        if (NetworkManager.Singleton == null) return true;

        return networkBehaviour.IsOwner && networkBehaviour.OwnerClientId == NetworkManager.Singleton.LocalClientId;
    }
}
