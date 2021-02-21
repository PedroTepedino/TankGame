using UnityEngine;

public abstract class AProjectile : MonoBehaviour, IPoolableObject
{
    public abstract void Fire();

    public abstract void OnSpawn();
}