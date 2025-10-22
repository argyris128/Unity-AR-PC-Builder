using UnityEngine;

public class CameraLooper : MonoBehaviour
{
    public Transform target;           
    public float maxAngle = 45f;       
    public float speed = 1f;           

    private float startAngle;          
    private float radius;              

    void Start()
    {
        if (target == null) return;

        Vector3 offset = transform.position - target.position;
        radius = offset.magnitude;
        startAngle = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
    }

    void Update()
    {
        if (target == null) return;

        float oscillation = Mathf.Sin(Time.time * speed) * maxAngle;
        float currentAngle = startAngle + oscillation;

        float radians = currentAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Sin(radians), 0, Mathf.Cos(radians)) * radius;

        transform.position = target.position + offset;

        transform.LookAt(target);
    }

}