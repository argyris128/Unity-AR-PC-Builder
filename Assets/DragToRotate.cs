using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

public class DragToRotate : MonoBehaviour
{
    public float rotationSpeed = 100f;
    private Vector2 lastDirection;
    private bool isDragging = false;

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
        Vector3 center = Camera.main.WorldToScreenPoint(transform.position);
        Vector2 currentDir = (screenPos - new Vector2(center.x, center.y)).normalized;

        if (!isDragging)
        {
            lastDirection = currentDir;
            isDragging = true;
            return;
        }

        float angle = Vector2.SignedAngle(lastDirection, currentDir);

        transform.Rotate(Vector3.up, -angle * rotationSpeed * Time.deltaTime, Space.Self);


        lastDirection = currentDir;
    }
}
