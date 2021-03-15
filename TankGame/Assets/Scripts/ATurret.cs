using System;
using Sirenix.OdinInspector;
using UnityEngine;

public abstract class ATurret : MonoBehaviour, IShooter
{
    [SerializeField] [EnumToggleButtons] protected PoolCollectionTags _projectileType;
    
    [SerializeField] private float _timeBetweenShots;
    public float TimeBetweenShots => _timeBetweenShots;

    [SerializeField] private int _valueToCool = 10;
    public int ValueToCool => _valueToCool;

    public event Action<ATurret> OnShoot;

    public virtual void Shoot()
    {
        OnShoot?.Invoke(this);
    }
}