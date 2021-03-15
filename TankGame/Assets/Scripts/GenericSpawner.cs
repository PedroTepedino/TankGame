using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Sirenix.OdinInspector;
using UnityEditor.Experimental;
using UnityEngine;

public class GenericSpawner : MonoBehaviour
{
    [SerializeField] [EnumToggleButtons] private PoolCollectionTags _tag;
    
    [SerializeField] private float _timeBetweenSpawns = 5f;
    private float _timer = 0f;

    [SerializeField] private int _spawnedObjectsCount = 1;

    void Update()
    {
        if (_timer <= 0)
        {
            PoolingSystem.Spawn(_tag, this.transform.position, Quaternion.identity);
            _timer = _timeBetweenSpawns;
        }
        else
        {
            _timer -= Time.deltaTime;
        }
    }
}

