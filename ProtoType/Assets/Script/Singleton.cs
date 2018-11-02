using UnityEngine;

public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    #region Inspector

    [SerializeField] bool dontDestroyOnLoad = false;

    #endregion

    public static T Instance { get; private set; }
	
    protected virtual void Awake()
    {
        if (Instance == null)
        {
            Instance = gameObject.GetComponent<T>();
            if (dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }     
    }

    protected virtual void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}

public class Singleton<T> where T : new()
{
    static T instance;
    public static T Instance
    {
        get
        {
            if (instance == null)
                instance = new T();
            return instance;
        }
    }         
}