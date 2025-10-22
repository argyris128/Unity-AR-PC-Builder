using UnityEngine;
using System;

public class SnapToRotation : MonoBehaviour
{
    public float targetRotation = 0f;

    public float rotationTolerance = 5f;

    public bool triggerOnce = true;

    public event Action Snapped;

    private bool hasSnapped = false;
    public MonoBehaviour dragToRotateScript;
    public bool targetX, targetY;

    void Update()
    {
        float current = 0f;
        if (targetX)
            current = transform.localEulerAngles.x;
        else if (targetY)
            current = transform.localEulerAngles.y;

        float angleDifference = Mathf.DeltaAngle(current, targetRotation);

        if (Mathf.Abs(angleDifference) <= rotationTolerance)
        {
            if (!hasSnapped || !triggerOnce)
            {
                if(targetX)
                    transform.rotation = Quaternion.Euler(targetRotation, transform.eulerAngles.y, transform.eulerAngles.z);
                else if(targetY)
                    transform.rotation = Quaternion.Euler(transform.eulerAngles.x, targetRotation, transform.eulerAngles.z);
                dragToRotateScript.enabled = false;
                hasSnapped = true;
                Snapped?.Invoke();
            }
        }
        else
        {
            if (!triggerOnce)
            {
                hasSnapped = false; // Reset if triggerOnce is false
            }
        }
    }
}
