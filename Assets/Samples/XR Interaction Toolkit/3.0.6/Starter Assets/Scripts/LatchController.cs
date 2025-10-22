using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

public class LatchController : MonoBehaviour
{
    private Transform latch;             // Assign the latch GameObject in inspector
    public float openAngle = -90f;      // X rotation when open
    public float animationSpeed = 200f; // Degrees per second

    private bool isOpen = false;
    private bool isAnimating = false;
    private Quaternion targetRotation;
    public event Action Touched;

    void Start() {
        latch = transform.Find("latch");
    }

    void Update()
    {
        // On single tap (touch or mouse)
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
        {
            Vector2 touchPos = Touchscreen.current.primaryTouch.position.ReadValue();
            TryToggleLatch(touchPos);
        }
        else if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            TryToggleLatch(mousePos);
        }

        // Animate latch rotation
        if (isAnimating)
        {
            latch.localRotation = Quaternion.RotateTowards(latch.localRotation, targetRotation, animationSpeed * Time.deltaTime);

            if (Quaternion.Angle(latch.localRotation, targetRotation) < 0.1f)
            {
                latch.localRotation = targetRotation;
                isAnimating = false;
            }
        }
    }

    void TryToggleLatch(Vector2 screenPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.transform == transform)
            {
                ToggleLatch();
            }
        }
    }

    void ToggleLatch()
    {
        Touched?.Invoke();
        isOpen = !isOpen;
        float targetAngle = isOpen ? openAngle : 90f;
        targetRotation = Quaternion.Euler(targetAngle, latch.localEulerAngles.y, latch.localEulerAngles.z);
        isAnimating = true;
    }
}
