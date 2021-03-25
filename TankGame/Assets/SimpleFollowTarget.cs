using System;
using UnityEngine;
using UnityEngine.AI;

public class SimpleFollowTarget : MonoBehaviour
{
    [SerializeField] private float _timeToUpdate = 0.5f;
    private float _timer = 0f;

    [SerializeField] private NavMeshAgent _navMeshAgent;

    [SerializeField] private Transform _target;

    private void OnEnable()
    {
        _timer = 0f;
    }

    private void Update()
    {
        _timer += Time.deltaTime;

        if (_timer >= _timeToUpdate)
        {
            _timer = 0f;
            
            if (NavMesh.SamplePosition(_target.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                _navMeshAgent.SetDestination(hit.position);
            }
        }
    }

    private void OnValidate()
    {
        if (_navMeshAgent == null)
        {
            _navMeshAgent = this.GetComponent<NavMeshAgent>();
        }
    }
}
