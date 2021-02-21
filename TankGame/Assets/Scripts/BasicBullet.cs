using System;
using UnityEngine;

public class BasicBullet : AProjectile
{
    private Vector3 _shootDirection = Vector3.zero;

    [SerializeField] private float _speed = 1f;

    [SerializeField] private float _timeToDeSpawn = 5f;
    private float _timer;

    [SerializeField] private Rigidbody _rigidbody;

    public override void OnSpawn()
    {
        _timer = _timeToDeSpawn;
    }

    public override void Fire()
    {
        _rigidbody.AddForce(this.transform.forward * _speed, ForceMode.VelocityChange);
    }

    private void Update()
    {
        _timer -= Time.deltaTime;

        if (_timer < 0f)
        {
            Explode();
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        other.gameObject.GetComponent<IHittable>()?.Hit();
        
        Explode();
    }

    private void Explode()
    {
        this.gameObject.SetActive(false);
    }

    private void OnValidate()
    {
        if (_rigidbody == null)
        {
            _rigidbody = this.GetComponent<Rigidbody>();
        }
    }
}