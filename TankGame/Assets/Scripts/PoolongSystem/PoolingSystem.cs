using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class ObjectPool
{
    public readonly string Tag;
    public readonly GameObject Prefab;
    public readonly bool IsExpandable ;

    public Queue<GameObject> Pool;

    public ObjectPool(PrefabPool _prefab)
    {
        Tag = _prefab.Tag;
        Prefab = _prefab.Prefab;
        IsExpandable = _prefab.IsExpandable;

        Pool = new Queue<GameObject>();
        
        PopulatePool(_prefab.ObjCount);
    }

    private void PopulatePool(int initialObjectCount = 0)
    {
        for (int i = 0; i < initialObjectCount; i++)
        {
            var obj = CreateObject();
            
            obj.SetActive(false);
            
            Pool.Enqueue(obj);
        }
    }

    public GameObject GetObject()
    {
        GameObject obj = null;
        
        if (Pool.Peek().activeInHierarchy)
        {
            obj = IsExpandable ? CreateObject() : Pool.Dequeue();
        }
        else
        {
            obj = Pool.Dequeue();
        }

        Pool.Enqueue(obj);
        
        return obj;
    }

    private GameObject CreateObject()
    {
        var obj = Object.Instantiate(Prefab);
        
        Object.DontDestroyOnLoad(obj);

        return obj;
    }
}

public class PoolingSystem : MonoBehaviour
{
    [SerializeField] [InlineEditor(Expanded = true)] private PoolCollection _poolCollection;

    private Dictionary<string, ObjectPool> _objectPools;

    public static PoolingSystem Instance { get; private set; } = null;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
            CreatePools();   
        }
    }

    private void CreatePools()
    {
        _objectPools = new Dictionary<string, ObjectPool>();

        foreach (PrefabPool prefabPool in _poolCollection.PrefabPoolsList.Where(prefabPool => prefabPool.Prefab.GetComponent<IPoolableObject>() != null && !_objectPools.ContainsKey(prefabPool.Tag)))
        {
            _objectPools.Add(prefabPool.Tag, new ObjectPool(prefabPool));
        }
    }
    
    public GameObject SpawnObject(string spawnTag, Vector3 position = default, Quaternion rotation = default)
    {
        if (!_objectPools.ContainsKey(spawnTag))
            return null;

        var obj =  _objectPools[spawnTag].GetObject();

        obj.transform.position = position;
        obj.transform.rotation = rotation;
        
        obj.GetComponent<IPoolableObject>().OnSpawn();
        
        obj.SetActive(true);

        return obj;
    }

    [Button("New")]
    public void CreateNewPoolCollection()
    {
        var asset = ScriptableObject.CreateInstance<PoolCollection>();

        if (!AssetDatabase.IsValidFolder("Assets/PoolCollections"))
        {
            AssetDatabase.CreateFolder("Assets", "PoolCollections");
        }

        var uniqueFileName = AssetDatabase.GenerateUniqueAssetPath("Assets/PoolCollections/PoolCollection.asset");
        
        AssetDatabase.CreateAsset(asset, uniqueFileName);
        AssetDatabase.SaveAssets();
        
        EditorUtility.FocusProjectWindow();

        //Selection.activeObject = asset;

        this._poolCollection = asset;
        
        CleanScriptableObjectsInstances();
    }


    private static void CleanScriptableObjectsInstances()
    {
        var instances = FindObjectsOfType<PoolCollection>();

        if (instances.Length > 0)
        {
            Debug.Log($"{instances.Length} PoolCollection instances found on scene!");
        }

        foreach (var pool in instances)
        {
            Destroy(pool);
        }
    }

    // private void OnValidate()
    // {
    //     string[] paths;
    //     int prefabsCount = HelperFunctions.GetAssetTypeCount<PrefabPool>(out paths,typeFilter: null, new []{"Assets/PoolObjects"});
    //
    //     if (prefabsCount != _poolCollection.PrefabPoolsList.Length)
    //     {
    //         _poolCollection.PrefabPoolsList = HelperFunctions.FindAssetsOfType<PrefabPool>(paths);
    //     }
    // }
}
