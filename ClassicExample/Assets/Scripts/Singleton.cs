using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    [SerializeField]
    public static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = (T)FindObjectOfType(typeof(T));
                if (instance == null)
                {
                    instance = new GameObject(typeof(T).ToString()).AddComponent<T>();

                    if (instance.transform)
                        instance.transform.position = Vector3.zero;
                }
            }
            return instance;
        }
    }

    protected T FindInstance()
    {
        if (instance == null)
        {
            instance = (T)FindObjectOfType(typeof(T));
            if (instance == null)
            {
                instance = new GameObject(typeof(T).ToString()).AddComponent<T>();

                if (instance.transform)
                    instance.transform.position = Vector3.zero;
            }
        }

        return instance;
    }

    protected void Awake()
    {
        FindInstance();
        DontDestroyOnLoad(this);
    }
}