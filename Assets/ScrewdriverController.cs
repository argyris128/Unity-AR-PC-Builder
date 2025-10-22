using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

public class ScrewdriverController : MonoBehaviour
{
    public float rotationSpeed = 100f;
    public float inwardSpeed = 0.0063f;
    private Vector2 lastDirection;
    private bool isDragging = false;
    public event Action Screwed;

    void Update()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            Vector2 touchPos = Touchscreen.current.primaryTouch.position.ReadValue();
            HandleRotation(touchPos);
        }
        else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            HandleRotation(mousePos);
        }
        else
        {
            isDragging = false;
        }
    }

    void HandleRotation(Vector2 screenPos)
    {
        // Get the screwdriver's position on screen
        Vector3 screenCenter = Camera.main.WorldToScreenPoint(transform.position);
        Vector2 currentDir = (screenPos - new Vector2(screenCenter.x, screenCenter.y)).normalized;

        if (!isDragging)
        {
            lastDirection = currentDir;
            isDragging = true;
            return;
        }

        float angle = Vector2.SignedAngle(lastDirection, currentDir);

        // Only accept clockwise motion
        if (angle > 0)
        {
            // Rotate screwdriver clockwise
            transform.Rotate(Vector3.forward, -angle * rotationSpeed * Time.deltaTime, Space.Self);

            // Move forward in local space
            transform.position += transform.forward * inwardSpeed * Time.deltaTime;
        }

        lastDirection = currentDir;
    }



    void OnTriggerEnter()
    {
        Screwed?.Invoke();  // a screw is done being screwed
    }
}
