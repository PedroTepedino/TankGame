using System;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

[Serializable]
[CreateAssetMenu(fileName = "NewPoolCollection", menuName = "ObjectTypes/PoolCollection", order = 1)]
public class PoolCollection : ScriptableObject
{
    [ValidateInput("CheckSameTags", "Different Tags cannot be equal!")]
    [ListDrawerSettings(Expanded = true)]
    public PrefabPool[] PrefabPoolsList;

    private bool CheckSameTags()
    {
        if (PrefabPoolsList != null && PrefabPoolsList.Length > 1)
        {
            for (int i = 0; i < PrefabPoolsList.Length; i++)
            {
                for (int j = i + 1; j < PrefabPoolsList.Length; j++)
                {
                    if (string.Compare(PrefabPoolsList[i].Tag, PrefabPoolsList[j].Tag, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        return false;
                    }
                }
            }
        }
            
        return true;
    }
}

[Serializable]
public class PrefabPool 
{
    [HorizontalGroup("Non-Listed", 80)] [PreviewField(80, ObjectFieldAlignment.Left)] [ValidateInput("GameObjectCheck",  "MISSING IPoolableObject !")] [HideLabel] [AssetsOnly]
    public GameObject Prefab;
    [HorizontalGroup("Non-Listed")] [BoxGroup("Non-Listed/Properties", false)] [ValidateInput("TagCheck", "Tag Cannot be Null")]
    public string Tag;
    [HorizontalGroup("Non-Listed")] [BoxGroup("Non-Listed/Properties", false)] [ValidateInput("ObjCountCheck", "Object count cannot be negative!")]
    public int ObjCount = 1;
    [HorizontalGroup("Non-Listed")] [BoxGroup("Non-Listed/Properties", false)]
    public bool IsExpandable = true;

    private bool GameObjectCheck() => Prefab != null && Prefab.GetComponent<IPoolableObject>() != null; 
    private bool TagCheck() => Tag.Length > 0;
    private bool ObjCountCheck() => ObjCount >= 0;
}