using System.Collections;
using UnityEngine;

// Static helper class to enable anything to use Unity's
// Monobehaviour coroutines
public class StaticCoroutine : MonoBehaviour {
    static StaticCoroutine _instance = null;
    static StaticCoroutine instance
    {
        get {
            return _instance = _instance ?? new GameObject().AddComponent<StaticCoroutine>();
        }
    }

    public static Coroutine Start(IEnumerator func)
    {
        return instance.StartCoroutine(func);
    }

    public static void Stop(Coroutine c)
    {
        instance.StopCoroutine(c);
    }
}
