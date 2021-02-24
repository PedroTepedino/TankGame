using Sirenix.OdinInspector;
using UnityEngine;

public abstract class ATurret : MonoBehaviour, IShooter
{
    [SerializeField] [EnumToggleButtons] protected PoolCollectionTags _projectileType;

    [SerializeField] private float _timeBetweenShots;
    public float TimeBetweenShots => _timeBetweenShots;

    public abstract void Shoot();
}