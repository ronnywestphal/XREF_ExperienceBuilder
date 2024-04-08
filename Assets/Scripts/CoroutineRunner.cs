using System.Collections;
using UnityEngine;

public class CoroutineRunner : MonoBehaviour
{
    private static CoroutineRunner instance;

    public static CoroutineRunner Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<CoroutineRunner>();
                if (instance == null)
                {
                    GameObject gameObject = new GameObject("CoroutineRunner");
                    instance = gameObject.AddComponent<CoroutineRunner>();
                }
            }
            return instance;
        }
    }

    public new Coroutine StartCoroutine(IEnumerator routine)
    {
        return base.StartCoroutine(routine);
    }

    public static void DestroyInstance()
    {
        if (instance != null)
        {
            DestroyImmediate(instance.gameObject);
            instance = null;
        }
    }
}