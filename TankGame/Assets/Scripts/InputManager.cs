using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public PlayerInputs Controls { get; private set; }
    
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(this.gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(this.gameObject);

        ControlsSetup();
    }

    private void ControlsSetup()
    {
        Controls = new PlayerInputs();
        
        Controls.Gameplay.Enable();
    }
    
    public static GameObject Fabricate(string name)
    {
        return new GameObject(name, new[] {typeof(InputManager)});
    }
}
