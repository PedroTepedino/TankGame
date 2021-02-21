using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; } = null;
    
    private PlayerInputs _controls;

    public PlayerInputs Controls => _controls;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(this.gameObject);
            return;
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        
        _controls = new PlayerInputs();
        
        _controls.Gameplay.Enable();
    }
}