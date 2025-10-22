using System;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Utilities;
using UnityEngine.SceneManagement;

namespace UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets
{
    /// <summary>
    /// Behavior with an API for spawning objects from a given set of prefabs.
    /// </summary>
    public class ObjectSpawner : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The camera that objects will face when spawned. If not set, defaults to the main camera.")]
        Camera m_CameraToFace;

        /// <summary>
        /// The camera that objects will face when spawned. If not set, defaults to the <see cref="Camera.main"/> camera.
        /// </summary>
        public Camera cameraToFace
        {
            get
            {
                EnsureFacingCamera();
                return m_CameraToFace;
            }
            set => m_CameraToFace = value;
        }

        [SerializeField]
        [Tooltip("The list of prefabs available to spawn.")]
        List<GameObject> m_ObjectPrefabs = new List<GameObject>();

        /// <summary>
        /// The list of prefabs available to spawn.
        /// </summary>
        public List<GameObject> objectPrefabs
        {
            get => m_ObjectPrefabs;
            set => m_ObjectPrefabs = value;
        }

        public List<GameObject> spawnedObjects;

        [SerializeField]
        [Tooltip("Optional prefab to spawn for each spawned object. Use a prefab with the Destroy Self component to make " +
            "sure the visualization only lives temporarily.")]
        GameObject m_SpawnVisualizationPrefab;

        /// <summary>
        /// Optional prefab to spawn for each spawned object.
        /// </summary>
        /// <remarks>Use a prefab with <see cref="DestroySelf"/> to make sure the visualization only lives temporarily.</remarks>
        public GameObject spawnVisualizationPrefab
        {
            get => m_SpawnVisualizationPrefab;
            set => m_SpawnVisualizationPrefab = value;
        }

        [SerializeField]
        [Tooltip("The index of the prefab to spawn. If outside the range of the list, this behavior will select " +
            "a random object each time it spawns.")]
        int m_SpawnOptionIndex = -1;

        /// <summary>
        /// The index of the prefab to spawn. If outside the range of <see cref="objectPrefabs"/>, this behavior will
        /// select a random object each time it spawns.
        /// </summary>
        /// <seealso cref="isSpawnOptionRandomized"/>
        public int spawnOptionIndex
        {
            get => m_SpawnOptionIndex;
            set => m_SpawnOptionIndex = value;
        }

        /// <summary>
        /// Whether this behavior will select a random object from <see cref="objectPrefabs"/> each time it spawns.
        /// </summary>
        /// <seealso cref="spawnOptionIndex"/>
        /// <seealso cref="RandomizeSpawnOption"/>
        public bool isSpawnOptionRandomized => m_SpawnOptionIndex < 0 || m_SpawnOptionIndex >= m_ObjectPrefabs.Count;

        [SerializeField]
        [Tooltip("Whether to only spawn an object if the spawn point is within view of the camera.")]
        bool m_OnlySpawnInView = true;

        /// <summary>
        /// Whether to only spawn an object if the spawn point is within view of the <see cref="cameraToFace"/>.
        /// </summary>
        public bool onlySpawnInView
        {
            get => m_OnlySpawnInView;
            set => m_OnlySpawnInView = value;
        }

        [SerializeField]
        [Tooltip("The size, in viewport units, of the periphery inside the viewport that will not be considered in view.")]
        float m_ViewportPeriphery = 0.15f;

        /// <summary>
        /// The size, in viewport units, of the periphery inside the viewport that will not be considered in view.
        /// </summary>
        public float viewportPeriphery
        {
            get => m_ViewportPeriphery;
            set => m_ViewportPeriphery = value;
        }

        [SerializeField]
        [Tooltip("When enabled, the object will be rotated about the y-axis when spawned by Spawn Angle Range, " +
            "in relation to the direction of the spawn point to the camera.")]
        bool m_ApplyRandomAngleAtSpawn = true;

        /// <summary>
        /// When enabled, the object will be rotated about the y-axis when spawned by <see cref="spawnAngleRange"/>
        /// in relation to the direction of the spawn point to the camera.
        /// </summary>
        public bool applyRandomAngleAtSpawn
        {
            get => m_ApplyRandomAngleAtSpawn;
            set => m_ApplyRandomAngleAtSpawn = value;
        }

        [SerializeField]
        [Tooltip("The range in degrees that the object will randomly be rotated about the y axis when spawned, " +
            "in relation to the direction of the spawn point to the camera.")]
        float m_SpawnAngleRange = 45f;

        /// <summary>
        /// The range in degrees that the object will randomly be rotated about the y axis when spawned, in relation
        /// to the direction of the spawn point to the camera.
        /// </summary>
        public float spawnAngleRange
        {
            get => m_SpawnAngleRange;
            set => m_SpawnAngleRange = value;
        }

        [SerializeField]
        [Tooltip("Whether to spawn each object as a child of this object.")]
        bool m_SpawnAsChildren;

        /// <summary>
        /// Whether to spawn each object as a child of this object.
        /// </summary>
        public bool spawnAsChildren
        {
            get => m_SpawnAsChildren;
            set => m_SpawnAsChildren = value;
        }

        int[] spawnCounter = new int[10];

        /// <summary>
        /// Event invoked after an object is spawned.
        /// </summary>
        /// <seealso cref="TrySpawnObject"/>
        public event Action<GameObject> objectSpawned;


        public GameObject motherboardButton;
        public GameObject ramButton;
        public GameObject hddButton;


        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        void Awake()
        {
            EnsureFacingCamera();
        }

        void EnsureFacingCamera()
        {
            if (m_CameraToFace == null)
                m_CameraToFace = Camera.main;
        }

        public Transform FindChildWithTag(Transform parent, string tag)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                if (parent.GetChild(i).CompareTag(tag))
                {
                    return parent.GetChild(i);
                }
            }

            return null;
        }

        /// <summary>
        /// Sets this behavior to select a random object from <see cref="objectPrefabs"/> each time it spawns.
        /// </summary>
        /// <seealso cref="spawnOptionIndex"/>
        /// <seealso cref="isSpawnOptionRandomized"/>
        public void RandomizeSpawnOption()
        {
            m_SpawnOptionIndex = -1;
        }

        /// <summary>
        /// Attempts to spawn an object from <see cref="objectPrefabs"/> at the given position. The object will have a
        /// yaw rotation that faces <see cref="cameraToFace"/>, plus or minus a random angle within <see cref="spawnAngleRange"/>.
        /// </summary>
        /// <param name="spawnPoint">The world space position at which to spawn the object.</param>
        /// <param name="spawnNormal">The world space normal of the spawn surface.</param>
        /// <returns>Returns <see langword="true"/> if the spawner successfully spawned an object. Otherwise returns
        /// <see langword="false"/>, for instance if the spawn point is out of view of the camera.</returns>
        /// <remarks>
        /// The object selected to spawn is based on <see cref="spawnOptionIndex"/>. If the index is outside
        /// the range of <see cref="objectPrefabs"/>, this method will select a random prefab from the list to spawn.
        /// Otherwise, it will spawn the prefab at the index.
        /// </remarks>
        /// <seealso cref="objectSpawned"/>
        public bool TrySpawnObject(Vector3 spawnPoint, Vector3 spawnNormal)
        {
            if (m_OnlySpawnInView)
            {
                var inViewMin = m_ViewportPeriphery;
                var inViewMax = 1f - m_ViewportPeriphery;
                var pointInViewportSpace = cameraToFace.WorldToViewportPoint(spawnPoint);
                if (pointInViewportSpace.z < 0f || pointInViewportSpace.x > inViewMax || pointInViewportSpace.x < inViewMin ||
                    pointInViewportSpace.y > inViewMax || pointInViewportSpace.y < inViewMin)
                {
                    return false;
                }
            }

            var objectIndex = isSpawnOptionRandomized ? Random.Range(0, m_ObjectPrefabs.Count) : m_SpawnOptionIndex;

            Scene currentScene = SceneManager.GetActiveScene();


            if (objectIndex != 4 && objectIndex != 5)
            {
                if (spawnCounter[objectIndex] > 0)
                {
                    return false;
                }
            }
            else if (currentScene.buildIndex == 1)
            {
                if (objectIndex == 4)
                {
                    if (spawnCounter[objectIndex] > 1 || ramButton.activeInHierarchy == false)
                    {
                        return false;
                    }
                }

                if (objectIndex == 5)
                {
                    if (spawnCounter[objectIndex] > 1 || hddButton.activeInHierarchy == false)
                    {
                        return false;
                    }
                }
            }

            var newObject = Instantiate(m_ObjectPrefabs[objectIndex]);
            spawnedObjects.Add(newObject);

            if (m_SpawnAsChildren)
                newObject.transform.parent = transform;

            newObject.transform.position = spawnPoint;
            EnsureFacingCamera();

            var facePosition = m_CameraToFace.transform.position;
            var forward = facePosition - spawnPoint;
            BurstMathUtility.ProjectOnPlane(forward, spawnNormal, out var projectedForward);
            newObject.transform.rotation = Quaternion.LookRotation(projectedForward, spawnNormal);

            if (m_ApplyRandomAngleAtSpawn)
            {
                //var randomRotation = Random.Range(-m_SpawnAngleRange, m_SpawnAngleRange);
                var randomRotation = m_SpawnAngleRange;

                if (currentScene.buildIndex == 2)
                    newObject.SetActive(true);

                if (objectIndex == 0)     // tower
                {
                    if (currentScene.buildIndex == 1)
                    {
                        newObject.transform.Rotate(Vector3.up, randomRotation);
                        motherboardButton.SetActive(true);
                        var hintMobo = newObject.transform.Find("hintMobo");
                        hintMobo.gameObject.SetActive(false);
                    }
                    else if (currentScene.buildIndex == 2)
                    {
                        objectPrefabs[1] = FindChildWithTag(objectPrefabs[0].transform, "Motherboard").gameObject;
                        objectPrefabs[2] = FindChildWithTag(objectPrefabs[1].transform, "CPU").gameObject;
                        objectPrefabs[3] = FindChildWithTag(objectPrefabs[1].transform, "Cooler").gameObject;
                        objectPrefabs[4] = FindChildWithTag(objectPrefabs[1].transform, "RAM").gameObject;
                        objectPrefabs[5] = FindChildWithTag(objectPrefabs[0].transform, "HDD").gameObject;
                        objectPrefabs[6] = FindChildWithTag(objectPrefabs[0].transform, "PSU").gameObject;
                        objectPrefabs[7] = FindChildWithTag(objectPrefabs[0].transform, "GPU").gameObject;

                        newObject.transform.Rotate(Vector3.up, randomRotation);
                        motherboardButton.SetActive(true);
                    }
                }
                else if (objectIndex == 1)    // mobo
                {
                    newObject.transform.rotation = Quaternion.Euler(90f, spawnedObjects[0].transform.eulerAngles.y, -90f);
                    Vector3 pos = spawnedObjects[0].transform.position;
                    Vector3 camPos = m_CameraToFace.transform.position + m_CameraToFace.transform.forward / 2;
                    camPos.y = FindInactiveObjectByName("hintMobo").transform.position.y;
                    newObject.transform.position = camPos;
                    newObject.transform.SetParent(spawnedObjects[0].transform);
                    newObject.GetComponent<DragToMove>().enabled = false;
                    newObject.GetComponent<LatchController>().enabled = false;

                }
                else if (objectIndex == 2)  // cpu
                {
                    newObject.transform.rotation = Quaternion.Euler(-90f, spawnedObjects[0].transform.eulerAngles.y, -90f);
                    Vector3 pos = spawnedObjects[0].transform.position;
                    Vector3 camPos = m_CameraToFace.transform.position + m_CameraToFace.transform.forward / 2;
                    camPos.y = FindInactiveObjectByName("hint_cpu").transform.position.y;
                    newObject.transform.position = camPos;
                    newObject.transform.SetParent(spawnedObjects[0].transform);
                }
                else if (objectIndex == 3)  // cooler
                {
                    newObject.transform.rotation = Quaternion.Euler(0f, spawnedObjects[0].transform.eulerAngles.y, -90f);
                    Vector3 pos = spawnedObjects[0].transform.position;
                    Vector3 camPos = m_CameraToFace.transform.position + m_CameraToFace.transform.forward / 2;
                    camPos.y = FindInactiveObjectByName("hint_cooler").transform.position.y;
                    newObject.transform.position = camPos;
                    newObject.transform.SetParent(spawnedObjects[1].transform);
                }
                else if (objectIndex == 4)  // ram
                {
                    newObject.transform.rotation = Quaternion.Euler(0f, spawnedObjects[0].transform.eulerAngles.y, -90f);
                    Vector3 pos = spawnedObjects[0].transform.position;
                    Vector3 camPos = m_CameraToFace.transform.position + m_CameraToFace.transform.forward / 2;
                    camPos.y = FindInactiveObjectByName("hint_ram1").transform.position.y;
                    newObject.transform.position = camPos;
                    //newObject.transform.SetParent(spawnedObjects[1].transform);
                }
                else if (objectIndex == 5)  // hdd
                {
                    newObject.transform.rotation = Quaternion.Euler(0f, spawnedObjects[0].transform.eulerAngles.y - 90f, 0f);
                    Vector3 pos = spawnedObjects[0].transform.position;
                    Vector3 camPos = m_CameraToFace.transform.position + m_CameraToFace.transform.forward / 2;

                    if (currentScene.buildIndex == 1)
                        camPos.y = FindInactiveObjectByName("hint_hdd").transform.position.y;
                    else if (currentScene.buildIndex == 2)
                        camPos.y = FindInactiveObjectByTag("hint_hdd").transform.position.y;

                    newObject.transform.position = camPos;
                    newObject.transform.SetParent(spawnedObjects[0].transform);
                }
                else if (objectIndex == 6)  // psu
                {
                    var hint_psu = FindInactiveObjectByName("hint_psu");
                    newObject.transform.rotation = Quaternion.Euler(hint_psu.transform.eulerAngles.x, hint_psu.transform.eulerAngles.y, hint_psu.transform.eulerAngles.z);
                    Vector3 pos = spawnedObjects[0].transform.position;
                    Vector3 camPos = m_CameraToFace.transform.position + m_CameraToFace.transform.forward / 2;
                    camPos.y = hint_psu.transform.position.y;
                    newObject.transform.position = camPos;
                    newObject.transform.SetParent(spawnedObjects[0].transform);
                }
                else if (objectIndex == 7)  // gpu
                {
                    var hint_gpu = FindInactiveObjectByName("hint_gpu");
                    newObject.transform.localScale = hint_gpu.transform.localScale;
                    newObject.transform.rotation = Quaternion.Euler(hint_gpu.transform.eulerAngles.x, hint_gpu.transform.eulerAngles.y, hint_gpu.transform.eulerAngles.z);
                    Vector3 pos = spawnedObjects[0].transform.position;
                    Vector3 camPos = m_CameraToFace.transform.position + m_CameraToFace.transform.forward / 2;
                    camPos.y = hint_gpu.transform.position.y;
                    newObject.transform.position = camPos;
                    newObject.transform.SetParent(spawnedObjects[0].transform);
                }

            }

            if (m_SpawnVisualizationPrefab != null)
            {
                var visualizationTrans = Instantiate(m_SpawnVisualizationPrefab).transform;
                visualizationTrans.position = spawnPoint;
                visualizationTrans.rotation = newObject.transform.rotation;
            }

            spawnCounter[objectIndex]++;
            objectSpawned?.Invoke(newObject);
            return true;
        }
        GameObject FindInactiveObjectByName(string name)
        {
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

            foreach (GameObject obj in allObjects)
            {
                if (obj.name == name && !obj.hideFlags.HasFlag(HideFlags.NotEditable) && !obj.hideFlags.HasFlag(HideFlags.HideAndDontSave))
                {
                    return obj;
                }
            }

            return null;
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
}


