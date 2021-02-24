using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour, IHittable 
{
    public PlayerInputs Controller { get; private set; }
    public NavMeshAgent Agent => _agent;
    public float Speed => _speed;
    public float Acceleration => _acceleration;
    public float MaxRotationAngle => _maxRotationAngle;
    public ATurret CurrentTurret => _currentTurret;
    public float MaxTurretRotation => _maxTurretRotation;

    //// Components
    private Mover _mover;
    private TurretRotator _turretRotator;
    private Shooter _shooter;
    private LifeSystem _lifeSystem;
    ////
    
    [SerializeField] private ATurret _currentTurret;

    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private NavMeshAgent _agent;

    [SerializeField] private float _speed = 1f;
    [SerializeField] private float _acceleration = 1f;
    [SerializeField] private float _maxRotationAngle = 5f;
    
    [SerializeField] private float _maxTurretRotation = 5f;

    [SerializeField] private int _maxHealth = 3;
    
    //// Actions

    public static event Action<LifeSystem> OnPlayerHealthChanged;
    
    private void Awake()
    {
        Controller = GameManager.Instance.Controls;

        _mover = new Mover(this);
        _turretRotator = new TurretRotator(this);
        _shooter = new Shooter(this);
        _lifeSystem = new LifeSystem(_maxHealth);
    }

    private void OnEnable()
    {
        _lifeSystem.OnHealthChanged += OnHealthChanged;
        _lifeSystem.OnDeath += ListenOnDeath;
    }

    private void OnDisable()
    {
        _lifeSystem.OnHealthChanged -= OnHealthChanged;
        _lifeSystem.OnDeath -= ListenOnDeath;
    }

    private void Update()
    {
        _mover.Tick();
        _turretRotator.Tick();
        _shooter.Tick();
    }
    
    public void Hit()
    {
        _lifeSystem.Damage();
    }
    
    private void OnHealthChanged(LifeSystem lifeSystem)
    {
        OnPlayerHealthChanged?.Invoke(lifeSystem);
    }

    private void ListenOnDeath()
    {
        this.gameObject.SetActive(false);
    }

    private void OnValidate()
    {
        if (_rigidbody == null)
        {
            _rigidbody = this.GetComponent<Rigidbody>();
        }

        if (_rigidbody != null)
        {
            _rigidbody.constraints = (RigidbodyConstraints) 80; // Freeze rotation X & Z

            _rigidbody.isKinematic = true;
        }

        if (_agent == null)
        {
            _agent = this.GetComponent<NavMeshAgent>();
        }

        if (_currentTurret == null)
        {
            _currentTurret = this.GetComponentInChildren<ATurret>();
        }
    }
}

public class LifeSystem
{
    public int MaxHealth { get; }
    public int CurrentHealth { get; private set; }

    private bool IsDead => CurrentHealth <= 0;
    
    public event Action<LifeSystem> OnHealthChanged;
    public event Action OnDeath;
    
    public LifeSystem(int maxHealth)
    {
        MaxHealth = maxHealth;
        CurrentHealth = maxHealth;
    }

    public void Damage(int damage = 1)
    {
        CurrentHealth = AddClampHealth(-damage);

        if (IsDead)
        {
            Die();    
        }

        OnHealthChanged?.Invoke(this);
    }

    public void Heal(int healAmount = 1)
    {
        CurrentHealth = AddClampHealth(healAmount);

        OnHealthChanged?.Invoke(this);
    }

    private void Die()
    {
        OnDeath?.Invoke();
    }

    public void FullHeal() => Heal(MaxHealth);

    private int AddClampHealth(int addedValue) => Mathf.Clamp(CurrentHealth + addedValue,0, MaxHealth);
}

public class Shooter
{
    private readonly Player _player;
    private readonly InputAction _shootAction;

    private ATurret _currentTurret;

    private float _timer;
    
    public Shooter(Player player)
    {
        _player = player;
        _shootAction = _player.Controller.Gameplay.Shoot;
        
        _currentTurret = _player.CurrentTurret;
    }

    public void Tick()
    {
        if (_shootAction.phase == InputActionPhase.Started)
        {
            if (_currentTurret != null)
            {
                if (_timer <= 0f)
                {
                    _currentTurret.Shoot();
                    _timer = _currentTurret.TimeBetweenShots;
                }
            }
            else
            {
                _currentTurret = _player.CurrentTurret;
            }
        }
        
        _timer -= Time.deltaTime;
        _timer = Mathf.Clamp(_timer, 0, _currentTurret.TimeBetweenShots);
    }
}

public class TurretRotator
{
    private readonly Player _player;

    private readonly float _maxRotationSpeed;
    
    private readonly InputAction _aimAction;
    private readonly InputAction _moveAction;

    private ATurret _currentTurret;
    private Quaternion _currentRotation;

    public TurretRotator(Player player)
    {
        _player = player;
        _currentTurret = _player.CurrentTurret;
        _maxRotationSpeed = _player.MaxTurretRotation;

        _aimAction = _player.Controller.Gameplay.Aim;
        _moveAction = _player.Controller.Gameplay.Move;
    }

    public void Tick()
    {
        if (_currentTurret == null)
        {
            _currentTurret = _player.CurrentTurret;
            return;
        }
        
        var input = _aimAction.ReadValue<Vector2>();

        
        if (input.magnitude > 0.1f)
        {
            _currentRotation = Quaternion.Euler(0, Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg, 0); 
        }
        else
        {
            var moveInput = _moveAction.ReadValue<Vector2>();

            if (moveInput.magnitude < 0.1f) return;
            
            _currentRotation = Quaternion.Euler(0, Mathf.Atan2(moveInput.x, moveInput.y) * Mathf.Rad2Deg,0 );
        }

        _currentTurret.transform.rotation =
            Quaternion.RotateTowards(_currentTurret.transform.rotation, _currentRotation, _maxRotationSpeed);
    }
}

public class Mover
{
    private readonly float _speed;
    private readonly float _acceleration;
    private readonly float _maxRotation;
    private readonly Player _player;
    private readonly NavMeshAgent _agent;
    private readonly Transform _transform;

    private float _currentDotProduct = 0f;
    private float _speedMultiplier = 0f;
    private Vector3 _currentInput = Vector3.zero;
    private Vector3 _lastInput = Vector3.zero;
    private Vector3 _moveDirection = Vector3.forward;
    private Quaternion _currentRotation = Quaternion.identity;


    public Mover(Player player)
    {
        _player = player;
        _transform = player.transform;
        _agent = player.Agent;
        _speed = player.Speed;
        _acceleration = player.Acceleration;
        _maxRotation = player.MaxRotationAngle;

        _player.Controller.Gameplay.Move.started += OnMoveInput;
        _player.Controller.Gameplay.Move.performed += OnMoveInput;
        _player.Controller.Gameplay.Move.canceled += OnMoveInput;
    }

    ~Mover()
    {
        _player.Controller.Gameplay.Move.started -= OnMoveInput;
        _player.Controller.Gameplay.Move.performed -= OnMoveInput;
        _player.Controller.Gameplay.Move.canceled -= OnMoveInput;
    }

    public void Tick()
    {
        if (_currentInput.magnitude >= 0.1f)
        {
            // _speedMultiplier = Mathf.Lerp(_speedMultiplier, 1, Time.deltaTime * _acceleration);

            if (Vector3.Dot(_lastInput, _currentInput) < 0)
            {
                _speedMultiplier = 0f;
            }

            _speedMultiplier += Time.deltaTime * _acceleration;
            _speedMultiplier = Mathf.Clamp01(_speedMultiplier);

            _lastInput = _currentInput;

            var forward = _transform.forward;
            _moveDirection = _currentDotProduct > 0 ? forward : -forward;

            _agent.Move(_moveDirection * (_speed * _speedMultiplier));
        }
        else
        {
            // _speedMultiplier = Mathf.Lerp( _speedMultiplier,0, Time.deltaTime * _acceleration);
            _speedMultiplier -= Time.deltaTime * _acceleration;
            _speedMultiplier = Mathf.Clamp01(_speedMultiplier);
            
            _agent.Move(_lastInput * (_speed * _speedMultiplier));
        }
        
        _transform.rotation = Quaternion.RotateTowards(_transform.rotation,_currentRotation,_maxRotation);
    }

    private void OnMoveInput(InputAction.CallbackContext context)
    {
        var aux = context.ReadValue<Vector2>();

        _currentInput.x = aux.x;
        _currentInput.z = aux.y;

        _currentInput.Normalize();

        if (_currentInput.magnitude > 0.1f)
        {
            _currentDotProduct = Vector3.Dot(_transform.forward, _currentInput);

            var tempVector = _currentDotProduct > 0 ? aux : -aux;
            _currentRotation = Quaternion.Euler(0, Mathf.Atan2(tempVector.x, tempVector.y) * Mathf.Rad2Deg, 0);
        }
    }
}