using Unity.Netcode;
using UnityEngine;

public class NetworkSingleton<T> : NetworkBehaviour where T : Component {
    private static T _instance;
    public static T Instance {
        get {
            if (_instance == null) {
                var objs = FindObjectsOfType(typeof(T)) as T[];
                if (objs.Length > 0)
                    _instance = objs[0];

                if (objs.Length > 1)
                    Debug.LogError("More than one " + typeof(T).Name + " objects exist within the scene");

                if (_instance == null) {
                    GameObject go = new GameObject();
                    go.name = string.Format("_{0}", typeof(T).Name);
                    _instance = go.AddComponent<T>();
                }
            }
            return _instance;
        }
    }
}
