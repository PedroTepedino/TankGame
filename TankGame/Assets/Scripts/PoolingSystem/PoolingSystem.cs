using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class PoolingSystem : MonoBehaviour
{
    [SerializeField] [InlineEditor(Expanded = true)] private PoolCollection _poolCollection;

    private Dictionary<Enum, ObjectPool> _objectPools;
    
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
        Type type = System.Type.GetType($"{_poolCollection.name}Tags");

        if (type == null)
        {
            Debug.LogError("Enum type not found! Try generating the tags!");
            return;
        }

        var poolList = _poolCollection.PrefabPoolsList.ToList();
        
        _objectPools = Enum.GetValues(type).Cast<Enum>().ToDictionary(t => t, t=>  new ObjectPool(poolList.Find(p => p.Tag == t.ToString())));

        //_objectPools = new Dictionary<string, ObjectPool>();
        
        // foreach (PrefabPool prefabPool in _poolCollection.PrefabPoolsList.Where(prefabPool => prefabPool.Prefab.GetComponent<IPoolableObject>() != null && !_objectPools.ContainsKey(prefabPool.Tag)))
        // {
        //     _objectPools.Add(prefabPool.Tag, new ObjectPool(prefabPool));
        // }
    }
    
    public GameObject SpawnObject(Enum spawnTag, Vector3 position = default, Quaternion rotation = default)
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

    public static GameObject Spawn(Enum spawnTag, Vector3 position = default, Quaternion rotation = default)
    {
        return Instance.SpawnObject(spawnTag, position, rotation);
    }

    [HorizontalGroup]
    [Button("New Pool Collection")]
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

    // public void GetEnumTags()
    // {
    //     if (_poolCollection != null)
    //     {
    //         Type type = System.Type.GetType($"{_poolCollection.name}Tags");
    //         Debug.Log($"{type}");
    //
    //         if (type == null)
    //         {
    //             Debug.LogError("Enum type not found! Try generating the tags!");
    //             return;
    //         }
    //
    //         var poolList = _poolCollection.PrefabPoolsList.ToList();
    //         
    //         Dictionary<Enum, string> test = new Dictionary<Enum, string>();
    //         var values = Enum.GetValues(type).Cast<Enum>().ToDictionary(t => t, t=>  poolList.Find(p => p.Tag == t.ToString()));
    //
    //         Debug.Log($"{values[PoolCollectionTags.BasicBullet].Prefab}");
    //
    //         Selection.activeObject = values[PoolCollectionTags.EnemyBullet].Prefab;
    //
    //         // foreach (var name in values)
    //         // {
    //         //     Debug.Log($"{name}  {name.GetType()}");
    //         //     test.Add(name, $"karai maluco -> {name}");
    //         // }
    //
    //
    //         // Debug.Log(test[PoolCollectionTags.BasicBullet]);
    //         // Debug.Log(test[PoolCollectionTags.EnemyBullet]);
    //     }
    // }

    [HorizontalGroup]
    [Button("Generate Tags")]
    private void CheckForDifference()
    {
        if (_poolCollection == null) return;
        
        Type type = System.Type.GetType($"{_poolCollection.name}Tags");

        if (type == null)
        {
            Debug.LogError("Enum type not found! Try generating the tags!");
            return;
        }

        var poolList = _poolCollection.PrefabPoolsList.ToList();
        var enumList = Enum.GetValues(type).Cast<Enum>().ToList();

        if (poolList.Any(p => enumList.All(t => p.Tag != t.ToString())))
        {
            PoolingHelperFunctions.GenerateEnumTags(_poolCollection);
        }
    }
}

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

internal struct PoolingHelperFunctions
{
    public static void GenerateEnumTags(PoolCollection poolCollection)
    {
        var path = $"{Application.dataPath}/Scripts/PoolingSystem/Tags";
        if (!System.IO.Directory.Exists(path))
        {
            System.IO.Directory.CreateDirectory(path);
        }

        path = $"{path}/TAGS_{poolCollection.name}.cs";

        using (StreamWriter outFile = new StreamWriter(path))
        {
            outFile.WriteLine($"public enum {poolCollection.name}Tags");
            outFile.WriteLine("{");

            foreach (var pool in poolCollection.PrefabPoolsList)
            {
                outFile.WriteLine($"    {pool.Tag},");
            }

            outFile.WriteLine("}");
        }
        
        AssetDatabase.Refresh();
    }
}
