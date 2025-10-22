using UnityEngine;
using System;
using System.Collections.Generic;

public class SnapToTarget : MonoBehaviour
{
    private GameObject computerCase;
    private Transform snapTarget;
    public float snapThreshold = 0.1f;
    public MonoBehaviour dragToMoveScript;

    private bool isSnapped = false;
    public event Action Snapped;

    void Awake()
    {
        computerCase = GameObject.FindWithTag("ComputerCase");
        snapTarget = computerCase.transform.Find("hintMobo");
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
