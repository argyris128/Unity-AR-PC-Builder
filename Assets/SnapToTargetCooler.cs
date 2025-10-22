using UnityEngine;
using System;
using System.Collections.Generic;

public class SnapToTargetCooler : MonoBehaviour
{
    private GameObject motherboard;
    private Transform snapTarget;
    public float snapThreshold = 0.1f;
    public MonoBehaviour dragToMoveScript;

    private bool isSnapped = false;
    public event Action Snapped;

    void Awake()
    {
        motherboard = GameObject.FindWithTag("Motherboard");
        snapTarget = motherboard.transform.Find("hint_cooler");
    }

    void Update()
    {
        if (isSnapped || snapTarget == null || dragToMoveScript == null)
            return;

        float distance = Vector3.Distance(transform.localPosition, snapTarget.localPosition);

        if (distance < snapThreshold && gameObject.activeInHierarchy)
        {
            transform.localPosition = snapTarget.localPosition;

            // transform.rotation = snapTarget.rotation;

            dragToMoveScript.enabled = false;
            snapTarget.gameObject.SetActive(false);
            isSnapped = true;
            Snapped?.Invoke();
        }
    }
}
