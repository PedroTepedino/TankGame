using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour, IHittable 
{
    public PlayerInputs Controller { get; private set; }

    public ATurret CurrentTurret => _currentTurret;
    public float MaxTurretRotation => _maxTurretRotation;
    public HeatManager HeatManager => _heatManager;

    //// Components
    private Mover _mover;
    private Dasher _dasher;
    private TurretRotator _turretRotator;
    private Shooter _shooter;
    private LifeSystem _lifeSystem;
    private HeatManager _heatManager;
    
    //// Properties
    [SerializeField] [InlineEditor(InlineEditorModes.FullEditor)] private ATurret _currentTurret;

    [FoldoutGroup("Attributes")]
    [BoxGroup("Attributes/Components")]
    [SerializeField] private Rigidbody _rigidbody;
    
    
    [BoxGroup("Attributes/Movement")]
    [SerializeField] private float _speed = 1f;
    [BoxGroup("Attributes/Movement")]
    [SerializeField] private float _acceleration = 1f;
    [BoxGroup("Attributes/Movement")]
    [SerializeField] private float _maxRotationAngle = 5f;
    
    [BoxGroup("Attributes/Turret")]
    [SerializeField] private float _maxTurretRotation = 5f;
    
    [BoxGroup("Attributes/Health")]
    [SerializeField] private int _maxHealth = 3;
    
    [BoxGroup("Attributes/Dash")]
    [SerializeField] private LayerMask _collisionMasks;
    [BoxGroup("Attributes/Dash")]
    [SerializeField] private float _dashCoolAmount;
    
    [BoxGroup("Attributes/Heat")]
    [SerializeField] private float _maxHeat = 100f;
    [BoxGroup("Attributes/Heat")]
    [SerializeField] private float _heatRecoverRate = 1f;
    [BoxGroup("Attributes/Heat")] 
    [SerializeField] private float _stunTimeForUnderHeat = 3f;
    private bool _heatStunned = false;
    private Coroutine _heatStunRoutine;
    private WaitForSeconds _waitForSecondsHeatStun;
    
    //// Actions
    public static event Action<LifeSystem> OnPlayerHealthChanged;
    [SerializeField] private UnityEvent _onHeatStun;
    [SerializeField] private UnityEvent _onHeatStunRecover;

    private void Awake()
    {
        Controller = GameManager.Instance.Controls;

        _mover = new Mover(_speed, _acceleration, _maxRotationAngle, this, _rigidbody, this.transform);
        _dasher = new Dasher(0f, 10f, _collisionMasks, _rigidbody, this.Controller.Gameplay.Dash, this.Controller.Gameplay.Move, _dashCoolAmount, 2);
        _turretRotator = new TurretRotator(this);
        _shooter = new Shooter(this);
        _lifeSystem = new LifeSystem(_maxHealth);
        _heatManager = new HeatManager(_maxHeat, _heatRecoverRate);
        
        _waitForSecondsHeatStun = new WaitForSeconds(_stunTimeForUnderHeat);
    }

    private void OnEnable()
    {
        _heatStunned = false;
        _heatManager.HeatHalfWay();

        _lifeSystem.OnHealthChanged += OnHealthChanged;
        _lifeSystem.OnDeath += ListenOnDeath;

        _currentTurret.OnShoot += ListenOnShoot;

        _dasher.OnDashed += ListenOnDash;

        _heatManager.OnOverHeat += ListenOnOverHeat;
        _heatManager.OnHeatEmpty += ListenOnHeatEmpty;
    }

    private void OnDisable()
    {
        _lifeSystem.OnHealthChanged -= OnHealthChanged;
        _lifeSystem.OnDeath -= ListenOnDeath;

        _currentTurret.OnShoot -= ListenOnShoot;

        _dasher.OnDashed -= ListenOnDash;
        
        _heatManager.OnOverHeat -= ListenOnOverHeat;
        _heatManager.OnHeatEmpty -= ListenOnHeatEmpty;
    }

    private void Update()
    {
        if (_heatStunned) return;
        
        _mover.Tick();
        _turretRotator.Tick();
        _shooter.Tick();
        _heatManager.Tick(Time.deltaTime);
    }

    public void Hit()
    {
        _lifeSystem.Damage();
    }

    private void OnHealthChanged(LifeSystem lifeSystem)
    {
        OnPlayerHealthChanged?.Invoke(lifeSystem);
    }

    private void ListenOnShoot(ATurret turret)
    {
        _heatManager.Cool(turret.ValueToCool);
    }

    private void ListenOnDash()
    {
        _heatManager.Cool(_dasher.DashCoolAmount);
    }

    private void ListenOnDeath()
    {
        this.gameObject.SetActive(false);
    }

    private void ListenOnOverHeat()
    {
        Debug.LogWarning($"OverHeated!");
    }

    private void ListenOnHeatEmpty()
    {
        //Debug.LogWarning($"HeatEmpty");    

        if (_heatStunRoutine != null)
            StopCoroutine(_heatStunRoutine);
        
        _heatStunRoutine = StartCoroutine(EndHeatStun());
    }

    private IEnumerator EndHeatStun()
    {
        _heatStunned = true;
        _dasher.Disable();
        _onHeatStun?.Invoke();
        
        yield return _waitForSecondsHeatStun;
            
        _heatStunned = false;
        _dasher.Enable();
        _onHeatStunRecover?.Invoke();
        
        _heatManager.HeatHalfWay();

        _heatStunRoutine = null;
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

        if (_currentTurret == null)
        {
            _currentTurret = this.GetComponentInChildren<ATurret>();
        }
    }
}

public class HeatManager
{
    public float MaxHeat { get; }
    private readonly float _recoverPointsPerSecond; 
    
    public float CurrentHeat { get; private set; } = 0;
    public float HeatPercentage => CurrentHeat / MaxHeat;
    public event Action OnOverHeat;
    public event Action OnHeatEmpty;

    public HeatManager(float maxHeat = 100f, float recoverRatePerSecond = 1f)
    {
        MaxHeat = maxHeat;
        _recoverPointsPerSecond = recoverRatePerSecond;

        CurrentHeat = MaxHeat / 2;
    }

    public void Tick(float deltaTime)
    {
        Heat(_recoverPointsPerSecond * deltaTime);
    }

    public void Heat(float valueToHeat)
    {
        AddHeatValue(valueToHeat);

        if (CurrentHeat > MaxHeat)
        {
            OnOverHeat?.Invoke();
        }
    }
    
    public void Cool(float valueToCool)
    {
        AddHeatValue(-valueToCool);

        if (CurrentHeat < 0)
        {
            OnHeatEmpty?.Invoke();
        }
    }

    private void AddHeatValue(float value)
    {
        CurrentHeat = Mathf.Clamp(CurrentHeat + value, 0f - 1, MaxHeat + 1);
    }

    public void HeatHalfWay()
    {
        CurrentHeat = MaxHeat / 2f;
    }
}

public class Dasher
{
    private readonly float _distance;
    private readonly LayerMask _collisionLayers;
    private readonly float _timeBetweenDashes;
    private readonly float _bodyRadius;
    private readonly Rigidbody _rigidbody;
    private readonly InputAction _dashAction;
    private readonly InputAction _moveDirectionAction;
    
    private Vector2 _moveDirection = Vector2.zero;

    private bool _enabled;
    public float DashCoolAmount { get; }

    public event Action OnDashed;

    //TODO: FIX THIS CLASS
    
    public Dasher(float timeBetweenDashes, float distance, LayerMask collisionLayers, Rigidbody rigidbody, InputAction dashAction, InputAction moveDirectionAction, float dashCoolAmount, float bodyRadius)
    {
        _dashAction = dashAction;
        _moveDirectionAction = moveDirectionAction;
        DashCoolAmount = dashCoolAmount;
        _bodyRadius = bodyRadius;
        _timeBetweenDashes = timeBetweenDashes;
        _distance = distance;
        _collisionLayers = collisionLayers;
        _rigidbody = rigidbody;
        _enabled = true;

        _dashAction.performed += OnDash;

        _moveDirectionAction.started += OnMove;
        _moveDirectionAction.performed += OnMove;
    }

    ~Dasher()
    {
        _dashAction.performed -= OnDash; 
        _moveDirectionAction.started -= OnMove;
        _moveDirectionAction.performed -= OnMove;
    }

    private void OnDash(InputAction.CallbackContext context)
    {
        if (!_enabled) return;
        
        var dashDirection = new Vector3(_moveDirection.x, 0, _moveDirection.y);
        var agentPosition = _rigidbody.transform.position;
        
        var hasHitSomething = Physics.SphereCast(
                origin:agentPosition, 
                radius:_bodyRadius, 
                direction:dashDirection.normalized,
                out RaycastHit hit,
                maxDistance:_distance, 
                layerMask:_collisionLayers
            );
        
        Vector3 DashFinalPosition;
        if (hasHitSomething)
        {
            var position = (hit.point - agentPosition).normalized * (hit.distance - _bodyRadius);
            DashFinalPosition = position + agentPosition + Vector3.up;
        }
        else
        {
            DashFinalPosition = agentPosition + (dashDirection.normalized * _distance) + Vector3.up;
        }

        if (NavMesh.SamplePosition(DashFinalPosition, out NavMeshHit sample, 10.0f, NavMesh.AllAreas))
        {
            _rigidbody.transform.position = sample.position;
            OnDashed?.Invoke();
        }
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        _moveDirection = context.ReadValue<Vector2>();
    }

    public void Enable() => _enabled = true;
    public void Disable() => _enabled = false;
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

    private readonly HeatManager _heatManager;

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
    private readonly Rigidbody _body;
    private readonly Transform _transform;

    private float _currentDotProduct = 0f;
    private float _speedMultiplier = 0f;
    private Vector3 _currentInput = Vector3.zero;
    private Vector3 _lastInput = Vector3.zero;
    private Vector3 _moveDirection = Vector3.forward;
    private Quaternion _currentRotation = Quaternion.identity;


    public Mover(float speed, float acceleration, float maxRotation, Player player, Rigidbody body, Transform transform)
    {
        _speed = speed;
        _acceleration = acceleration;
        _maxRotation = maxRotation;
        _player = player;
        _body = body;
        _transform = transform;

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

            //_agent.Move(_moveDirection * (_speed * _speedMultiplier));

            _body.velocity = _moveDirection * (_speed * _speedMultiplier);
        }
        else
        {
            // _speedMultiplier = Mathf.Lerp( _speedMultiplier,0, Time.deltaTime * _acceleration);
            _speedMultiplier -= Time.deltaTime * _acceleration;
            _speedMultiplier = Mathf.Clamp01(_speedMultiplier);
            
            //_agent.Move(_lastInput * (_speed * _speedMultiplier));
            _body.velocity = _lastInput * (_speed * _speedMultiplier);
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