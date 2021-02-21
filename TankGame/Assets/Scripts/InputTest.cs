using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputTest : MonoBehaviour
{
    private Vector2 _currentMoveInput = Vector2.zero;
    
    private void Awake()
    {
        
    }

    private void OnMoveInput(InputAction.CallbackContext context)
    {
        _currentMoveInput = context.ReadValue<Vector2>();
    }
}
