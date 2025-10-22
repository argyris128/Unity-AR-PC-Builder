using UnityEngine;
using System;
using System.Collections.Generic;

public class SnapToTargetRAM : MonoBehaviour
{
    public GameObject snapTarget;
    public float snapThreshold = 0.1f;
    public MonoBehaviour dragToMoveScript;

    private bool isSnapped = false;
    public event Action Snapped;

    void Update()
    {
        snapTarget = FindInactiveObjectByTag("hint_ram");

        if (isSnapped || snapTarget == null || dragToMoveScript == null)
            return;

        float distance = Vector3.Distance(transform.localPosition, snapTarget.transform.localPosition);

        if (distance < snapThreshold && gameObject.activeInHierarchy)
        {
            transform.localPosition = snapTarget.transform.localPosition;

            // transform.rotation = snapTarget.rotation;

            dragToMoveScript.enabled = false;
            snapTarget.tag = "Untagged";
            snapTarget.SetActive(false);
            isSnapped = true;
            Snapped?.Invoke();
        }
    }
    
    GameObject FindInactiveObjectByTag(string name)
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            if (obj.CompareTag(name) && !obj.hideFlags.HasFlag(HideFlags.NotEditable) && !obj.hideFlags.HasFlag(HideFlags.HideAndDontSave))
            {
                return obj;
            }
        }

        return null;
    }

}


