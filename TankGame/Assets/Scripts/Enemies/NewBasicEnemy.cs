using Pathfinding;
using Sirenix.OdinInspector;
using UnityEngine;
using System;

public class NewBasicEnemy : MonoBehaviour
{
    private StateMachine _ai;

    [BoxGroup("Parameters")] 
    [SerializeField] private float _playerStopDistance = 10f;
    [BoxGroup("Parameters")] 
    [SerializeField] private float _turretRotationSpeed = 2f;

    [BoxGroup("Shooting Parameters")] 
    [SerializeField] private float _timeBetweenShots = 5f;
    [BoxGroup("Shooting Parameters")] 
    [SerializeField] private LayerMask _shootingMask;

    [SerializeField] private ATurret _turret;
    
    [SerializeField] private AIDestinationSetter _destinationSetter;
    [SerializeField] private RichAI _richAI;
    [SerializeField] private Seeker _seeker;

    [SerializeField] private Transform _tankBase;

    private Player _player;

    private float _timer = 0f;

    private EnemyTurretRotator _turretRotator;
    
    private void Awake()
    {
        _player = FindObjectOfType<Player>();
        
        SetUpAi();

        _turretRotator = new EnemyTurretRotator(_turret, _player, _turretRotationSpeed);
    }

    private void SetUpAi()
    {
        _ai = new StateMachine();
        
        ChaseTargetFromADistanceState chaseTarget =
            new ChaseTargetFromADistanceState(_player.transform, _richAI, _destinationSetter, CheckPlayerInRange);

        EnemyShooterState shootingState = new EnemyShooterState( _turret, _player, _shootingMask);

        _ai.SetState(chaseTarget);
        
        _ai.AddTransition(chaseTarget, shootingState, () => _timer <= 0f);
        _ai.AddTransition(shootingState, chaseTarget,  () => _timer > 0f);
    }

    private void OnEnable()
    {
        _timer = _timeBetweenShots;
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        
        _turretRotator.Tick();
        
        _ai.Tick();

        if (_timer < 0f)
        {
            _timer = _timeBetweenShots;
        }
    }
    

    private bool CheckPlayerInRange()
    {
        if (!AstarPath.active.data.recastGraph.Linecast(this.transform.position, _player.transform.position) 
            && _player != null 
            && Vector3.Distance(this.transform.position, _player.transform.position) < _playerStopDistance)
        {
            return true;
        }

        return false;
    }

    private void OnValidate()
    {
        if (_destinationSetter == null)
        {
            _destinationSetter = this.GetComponent<AIDestinationSetter>();
        }

        if (_richAI == null)
        {
            _richAI = this.GetComponent<RichAI>();
        }

        if (_turret == null)
        {
            _turret = this.GetComponentInChildren<ATurret>();
        }
        
        if (_seeker == null)
        {
            _seeker = this.GetComponentInChildren<Seeker>();
        }
    }
}

public class EnemyTurretRotator
{
    private readonly ATurret _turret;
    private readonly Player _player;
    private readonly float _turretRotationSpeed;

    public EnemyTurretRotator(ATurret turret, Player player, float turretRotationSpeed)
    {
        _turret = turret;
        _player = player;
        _turretRotationSpeed = turretRotationSpeed;
    }

    public void Tick()
    {
        var targetRotation = Quaternion.LookRotation(_player.transform.position - _turret.transform.position);
        targetRotation.x = 0f;
        targetRotation.z = 0f;
        
        _turret.transform.rotation = Quaternion.RotateTowards(targetRotation, _turret.transform.rotation, _turretRotationSpeed);
    }
}

public class EnemyShooterState : IState
{
    private readonly ATurret _turret;
    private readonly Player _player;
    private readonly LayerMask _shootingMask;

    private readonly RaycastHit[] _raycastHits;

    public EnemyShooterState(ATurret turret, Player player, LayerMask shootingMask)
    {
        _turret = turret;
        _player = player;
        _shootingMask = shootingMask;
        _raycastHits = new RaycastHit[5];
    }

    public void Tick()
    {
        
    }

    public void OnEnter()
    {
        var position = _turret.transform.position;
        int objectCount = Physics.RaycastNonAlloc(position,
            (_player.transform.position - position).normalized, _raycastHits, _shootingMask);

        if (objectCount <= 0)
        {
            _turret.Shoot();
        }
    }

    public void OnExit()
    {
        
    }
}

public class ChaseTargetFromADistanceState : IState
{
    private readonly Transform _target;
    private readonly RichAI _richAI;
    private readonly AIDestinationSetter _destinationSetter;
    private readonly Func<bool> _stoppingStatement;

    public ChaseTargetFromADistanceState(Transform target, RichAI richAI, AIDestinationSetter destinationSetter, Func<bool> stoppingStatement)
    {
        _target = target;
        _richAI = richAI;
        _destinationSetter = destinationSetter;
        _stoppingStatement = stoppingStatement;
    }
    
    public void Tick()
    {
        if (_stoppingStatement.Invoke())
        {
            _richAI.isStopped = true;
        }
        else
        {
            _richAI.isStopped = false;
        }
    }

    public void OnEnter()
    {
        _destinationSetter.target = _target;
    }

    public void OnExit()
    {
        
    }
}

public class StunState : IState
{
    public void Tick()
    {
        
    }

    public void OnEnter()
    {
        
    }

    public void OnExit()
    {
        
    }
}