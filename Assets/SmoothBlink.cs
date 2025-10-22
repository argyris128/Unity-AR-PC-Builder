using UnityEngine;

public class SmoothBlink : MonoBehaviour
{
    public float blinkSpeed = 1f; // Speed of blinking
    private Material mat;
    private Color baseColor;

    void Start()
    {
        mat = GetComponent<Renderer>().material;
        baseColor = mat.color;
    }

    void Update()
    {
        float metallic = (Mathf.Sin(Time.time * blinkSpeed) + 1f) / 2f;
        mat.SetFloat("_Metallic", metallic);
    }
}
