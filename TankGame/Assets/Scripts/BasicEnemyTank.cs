using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

public class BasicEnemyTank : MonoBehaviour, IHittable, IPoolableObject
{
    [SerializeField] /*[OnValueChanged("UpdateStopDistance")]*/ private float _shootingDistance = 3f;
    [SerializeField] private float _minShootingDistance = 2f;
    public float ShootingDistance { get => _shootingDistance; set => _shootingDistance = value; }

    [SerializeField] private int _maxHealth;
    
    [SerializeField] private float _turretRotationSpeed = 2f;

    private float _timer = 0f;
    
    [SerializeField] private NavMeshAgent _agent;
    [SerializeField] private ATurret _turret;

    private LifeSystem _lifeSystem;

    // private StateMachine _stateMachine;

    private static Player _currentPlayer = null;
    public static Player CurrentPlayer => _currentPlayer;

    private void Awake()
    {
        _lifeSystem = new LifeSystem(_maxHealth);
    }

    private void OnEnable()
    {
        _lifeSystem.OnDeath += ListenOnDeath;
    }

    private void OnDisable()
    {
        _lifeSystem.OnDeath += ListenOnDeath;
    }

    private void Update()
    {
        // _stateMachine.Tick();

        if (_currentPlayer == null)
        {
            TryGetPlayer();
        }
        else
        {
            var rayDirection = _currentPlayer.transform.position - this.transform.position;
            if (Physics.Raycast(this.transform.position,
                (rayDirection).normalized, 
                layerMask: LayerMask.GetMask("Default"), 
                maxDistance: rayDirection.magnitude))
            {
                _agent.stoppingDistance = _minShootingDistance;
            }
            else
            {
                TryShot();
                _agent.stoppingDistance = _shootingDistance;
            }
            
            _agent.SetDestination(_currentPlayer.transform.position);
            
            RotateTurretToPlayerDirection();    
        }

        if (_timer > 0)
            _timer -= Time.deltaTime;
    }

    private void TryShot()
    {
        if (_turret == null) return;

        if (_timer <= 0f)
        {
            _turret.Shoot();
            _timer = _turret.TimeBetweenShots;
        }
    }

    private void RotateTurretToPlayerDirection()
    {
        if (_turret == null)  return;

        var targetRotation = Quaternion.LookRotation(_currentPlayer.transform.position - this.transform.position);
        targetRotation.x = 0f;
        targetRotation.z = 0f;
        
        _turret.transform.rotation = Quaternion.RotateTowards(targetRotation, _turret.transform.rotation, _turretRotationSpeed);
    }

    public void Hit()
    {
        _lifeSystem.Damage();
    }

    private void ListenOnDeath()
    {
        this.gameObject.SetActive(false);
    }
    
    public void OnSpawn()
    {
        _lifeSystem.FullHeal();
    }

    private void OnValidate()
    {
        if (_agent == null)
        {
            _agent = this.GetComponent<NavMeshAgent>();
        }

        if (_turret == null)
        {
            _turret = this.GetComponentInChildren<ATurret>();
        }
    }

    private bool TryGetPlayer()
    {
        if (_currentPlayer != null)
        {
            return true;
        }

        _currentPlayer = FindObjectOfType<Player>();
        return _currentPlayer != null;
    }
    
#if UNITY_EDITOR

    private void UpdateStopDistance()
    {
        _agent.stoppingDistance = _shootingDistance;
    }
#endif
}

#if UNITY_EDITOR

[CustomEditor(typeof(BasicEnemyTank))]
public class BasicEnemyTankEditor : OdinEditor
{
    private void OnSceneGUI()
    {
        BasicEnemyTank enemy = target as BasicEnemyTank;
        
        EditorGUI.BeginChangeCheck();
        
        Handles.color = Color.red;
        var newRadius = Handles.RadiusHandle(Quaternion.identity, enemy.transform.position, enemy.ShootingDistance);

        Handles.zTest = CompareFunction.Less;

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(enemy, "enemy radius changed");

            enemy.ShootingDistance = newRadius;
        }
    }
}

#endif