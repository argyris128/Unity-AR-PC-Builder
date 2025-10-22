using UnityEngine;
using UnityEngine.InputSystem;

public class DragToMove : MonoBehaviour
{
    private Vector2 startPosition;
    private Vector2 currentPosition;
    private bool isDragging = false;

    public float moveSpeed = 3f;

    // Input Actions
    private InputAction pressAction;
    private InputAction positionAction;

    void OnEnable()
    {
        // Create input actions
        pressAction = new InputAction(type: InputActionType.Button, binding: "<Pointer>/press");
        positionAction = new InputAction(type: InputActionType.Value, binding: "<Pointer>/position");

        pressAction.performed += ctx => StartDragging();
        pressAction.canceled += ctx => StopDragging();

        pressAction.Enable();
        positionAction.Enable();
    }

    void OnDisable()
    {
        pressAction.Disable();
        positionAction.Disable();
    }

    void Update()
    {
        if (isDragging)
        {
            currentPosition = positionAction.ReadValue<Vector2>();
            Vector2 delta = currentPosition - startPosition;

            float dx = delta.x / Screen.width;
            float dy = delta.y / Screen.height;

            Vector3 moveRight = Camera.main.transform.right;
            Vector3 moveUp = Camera.main.transform.forward;

            moveRight.y = 0;
            moveUp.y = 0;
            moveRight.Normalize();
            moveUp.Normalize();

            Vector3 move = (moveRight * dx + moveUp * dy) * moveSpeed;

            transform.position += move * moveSpeed * Time.deltaTime;

            // Update the start position for continuous movement
            startPosition = currentPosition;
        }
    }

    void StartDragging()
    {
        startPosition = positionAction.ReadValue<Vector2>();
        isDragging = true;
    }

    void StopDragging()
    {
        isDragging = false;
    }
}
