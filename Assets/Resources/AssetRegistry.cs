using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AssetRegistry", menuName = "AssetRegistry")]
public class AssetRegistry : ScriptableObject
{
    private static AssetRegistry _instance;
    public static AssetRegistry Instance {
        get {
            if (_instance == null)
                _instance = Resources.Load<AssetRegistry>("AssetRegistry");
            return _instance;
        }
    }

    public GameObject healingOrbPrefab;
    public GameObject arrowPrefab;
}