using UnityEngine;

public static class BootStrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void InputSetup()
    {
        if (!Object.FindObjectOfType<InputManager>())
        {
            InputManager.Fabricate("[InputManager]");
        }
    }
}
