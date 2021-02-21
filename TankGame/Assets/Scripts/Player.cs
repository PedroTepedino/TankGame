using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public PlayerInputs Controller { get; private set; }
    public NavMeshAgent Agent => _agent;
    public float Speed => _speed;
    public float Acceleration => _acceleration;
    public float MaxRotationAngle => _maxRotationAngle;
    public ATurret CurrentTurret => _currentTurret;
    public float MaxTurretRotation => _maxTurretRotation;

    private Mover _mover;
    private TurretRotator _turretRotator;
    
    [SerializeField] private ATurret _currentTurret;

    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private NavMeshAgent _agent;

    [SerializeField] private float _speed = 1f;
    [SerializeField] private float _acceleration = 1f;
    [SerializeField] private float _maxRotationAngle = 5f;
    
    [SerializeField] private float _maxTurretRotation = 5f;
    
    private void Awake()
    {
        Controller = GameManager.Instance.Controls;

        _mover = new Mover(this);
        _turretRotator = new TurretRotator(this);
    }
    
    private void Update()
    {
        _mover.Tick();
        _turretRotator.Tick();
    }

    private void OnValidate()
    {
        if (_rigidbody == null)
        {
            _rigidbody = this.GetComponent<Rigidbody>();
        }

        if(_rigidbody != null)
        {
            _rigidbody.constraints = (RigidbodyConstraints) 80; // Freeze rotation X & Z

            _rigidbody.isKinematic = true;
        }

        if (_agent == null)
        {
            _agent = this.GetComponent<NavMeshAgent>();
        }
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

            _moveDirection = _currentDotProduct > 0 ? _transform.forward : -_transform.forward;

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