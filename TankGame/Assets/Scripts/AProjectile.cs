using Sirenix.OdinInspector;
using UnityEngine;

public enum ProjectileType
{
    PLAYER,
    ENEMY,
}

public abstract class AProjectile : MonoBehaviour, IPoolableObject
{
    [SerializeField, EnumToggleButtons, OnValueChanged("OnProjectileTypeChanged")] 
    protected ProjectileType _projectileType;
    
    public abstract void Fire();

    public abstract void OnSpawn();

    protected void OnProjectileTypeChanged()
    {
        GameObject obj = this.gameObject;
        obj.layer = _projectileType switch
        {
            ProjectileType.PLAYER => LayerMask.NameToLayer("PlayerProjectiles"),
            ProjectileType.ENEMY => LayerMask.NameToLayer("EnemyProjectiles"),
            _ => (obj = this.gameObject).layer
        };
    }
}