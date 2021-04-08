using UnityEngine;
using UnityEngine.InputSystem;

public class InputTest : MonoBehaviour
{
    Vector2 currentInput = Vector2.zero;
    private Camera _camera;
    [SerializeField] private float _angleRotation = 10f;

    private InputAction _mousePosition;

    private void Awake()
    {
        _camera = UnityEngine.Camera.main;
        //_mousePosition = GameManager.Instance.Controls.Gameplay.MouseTest;
    }

    private void Update()
    {
        currentInput = _mousePosition.ReadValue<Vector2>();

        Plane playerPlane = new Plane(Vector3.up, this.transform.position);
        Ray ray = _camera.ScreenPointToRay(currentInput);
        float hitDist = 0.0f;

        if (playerPlane.Raycast(ray, out hitDist))
        {
            Vector3 targetPoint = ray.GetPoint(hitDist);
            
            Quaternion targetRotation = Quaternion.LookRotation(targetPoint - this.transform.position);
            targetRotation.x = 0;
            targetRotation.z = 0;

            this.transform.rotation = Quaternion.RotateTowards(this.transform.rotation, targetRotation, _angleRotation);
            //this.transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 7f * Time.deltaTime);
            
        }
    }
}
