using UnityEngine;

public abstract class ATurret : MonoBehaviour, IShooter
{
    [SerializeField] protected AProjectile _projectile;
    public abstract void Shoot();
}