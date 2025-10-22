using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Onboarding goal to be achieved as part of the <see cref="GoalManager"/>.
/// </summary>
public struct Goal
{
    /// <summary>
    /// Goal state this goal represents.
    /// </summary>
    public GoalManager.OnboardingGoals CurrentGoal;

    /// <summary>
    /// This denotes whether a goal has been completed.
    /// </summary>
    public bool Completed;

    /// <summary>
    /// Creates a new Goal with the specified <see cref="GoalManager.OnboardingGoals"/>.
    /// </summary>
    /// <param name="goal">The <see cref="GoalManager.OnboardingGoals"/> state to assign to this Goal.</param>
    public Goal(GoalManager.OnboardingGoals goal)
    {
        CurrentGoal = goal;
        Completed = false;
    }
}

/// <summary>
/// The GoalManager cycles through a list of Goals, each representing
/// an <see cref="GoalManager.OnboardingGoals"/> state to be completed by the user.
/// </summary>
public class GoalManager : MonoBehaviour
{
    /// <summary>
    /// State representation for the onboarding goals for the GoalManager.
    /// </summary>
    public enum OnboardingGoals
    {
        /// <summary>
        /// Current empty scene
        /// </summary>
        Empty,

        /// <summary>
        /// Find/scan for AR surfaces
        /// </summary>
        FindSurfaces,

        /// <summary>
        /// Tap a surface to spawn an object
        /// </summary>
        TapSurface,

        /// <summary>
        /// Show movement hints
        /// </summary>
        Hints,

        /// <summary>
        /// Show scale and rotate hints
        /// </summary>
        Scale,
        Prompt,
        PlaceMotherboard,
        ScrewMotherboard,
        CPU_Latch,
        RotateCPU,
        PlaceCPU,
        PlaceCooler,
        ScrewCooler,
        RotateRAM,
        PlaceRAM,
        PlaceHDD,
        PlacePSU,
        ScrewPSU,
        PlaceGPU,
        ScrewGPU,
        ChooseBuild
    }

    /// <summary>
    /// Individual step instructions to show as part of a goal.
    /// </summary>
    [Serializable]
    public class Step
    {
        /// <summary>
        /// The GameObject to enable and show the user in order to complete the goal.
        /// </summary>
        [SerializeField]
        public GameObject stepObject;

        /// <summary>
        /// The text to display on the button shown in the step instructions.
        /// </summary>
        [SerializeField]
        public string buttonText;

        /// <summary>
        /// This indicates whether to show an additional button to skip the current goal/step.
        /// </summary>
        [SerializeField]
        public bool includeSkipButton;
    }

    [Tooltip("List of Goals/Steps to complete as part of the user onboarding.")]
    [SerializeField]
    List<Step> m_StepList = new List<Step>();

    /// <summary>
    /// List of Goals/Steps to complete as part of the user onboarding.
    /// </summary>
    public List<Step> stepList
    {
        get => m_StepList;
        set => m_StepList = value;
    }

    [Tooltip("Object Spawner used to detect whether the spawning goal has been achieved.")]
    [SerializeField]
    ObjectSpawner m_ObjectSpawner;

    /// <summary>
    /// Object Spawner used to detect whether the spawning goal has been achieved.
    /// </summary>
    public ObjectSpawner objectSpawner
    {
        get => m_ObjectSpawner;
        set => m_ObjectSpawner = value;
    }

    [Tooltip("The greeting prompt Game Object to show when onboarding begins.")]
    [SerializeField]
    GameObject m_GreetingPrompt;

    /// <summary>
    /// The greeting prompt Game Object to show when onboarding begins.
    /// </summary>
    public GameObject greetingPrompt
    {
        get => m_GreetingPrompt;
        set => m_GreetingPrompt = value;
    }

    [Tooltip("The Options Button to enable once the greeting prompt is dismissed.")]
    [SerializeField]
    GameObject m_OptionsButton;

    /// <summary>
    /// The Options Button to enable once the greeting prompt is dismissed.
    /// </summary>
    public GameObject optionsButton
    {
        get => m_OptionsButton;
        set => m_OptionsButton = value;
    }

    [Tooltip("The Create Button to enable once the greeting prompt is dismissed.")]
    [SerializeField]
    GameObject m_CreateButton;

    /// <summary>
    /// The Create Button to enable once the greeting prompt is dismissed.
    /// </summary>
    public GameObject createButton
    {
        get => m_CreateButton;
        set => m_CreateButton = value;
    }

    [Tooltip("The AR Template Menu Manager object to enable once the greeting prompt is dismissed.")]
    [SerializeField]
    ARTemplateMenuManager m_MenuManager;

    /// <summary>
    /// The AR Template Menu Manager object to enable once the greeting prompt is dismissed.
    /// </summary>
    public ARTemplateMenuManager menuManager
    {
        get => m_MenuManager;
        set => m_MenuManager = value;
    }

    const int k_NumberOfSurfacesTappedToCompleteGoal = 1;

    Queue<Goal> m_OnboardingGoals;
    Coroutine m_CurrentCoroutine;
    Goal m_CurrentGoal;
    bool m_AllGoalsFinished;
    int m_SurfacesTapped;
    int m_CurrentGoalIndex = 0;

    public Button continueButton;

    public GameObject desktopButton;
    public GameObject cpuButton;
    public GameObject coolerButton;
    public GameObject ramButton;
    public GameObject psuButton;
    public GameObject hddButton;
    public GameObject gpuButton;
    private int cableCounter = 0;
    public Button cableContinue;
    private GameObject pcBuild;
    public GameObject chooseBuildMenu;
    public event Action<GameObject> pcBuildFound;
    public GameObject StepMenu;
    public Button SkipButton;
    public Button FinishButton;
    public TMP_Text StepText;
    public TMP_Text StepTextLong;
    public TMP_Text StepText2;
    public TMP_Text StepTextLong2;

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

    void Awake()
    {
        StartCoaching();
    }

    void Update()
    {
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame && !m_AllGoalsFinished && (m_CurrentGoal.CurrentGoal == OnboardingGoals.FindSurfaces || m_CurrentGoal.CurrentGoal == OnboardingGoals.Hints || m_CurrentGoal.CurrentGoal == OnboardingGoals.Scale))
        {
            if (m_CurrentCoroutine != null)
            {
                StopCoroutine(m_CurrentCoroutine);
            }
            CompleteGoal();
        }

        if (SceneManager.GetActiveScene().buildIndex == 2)
        {
            if (m_CurrentGoalIndex == 1)
            {
                StepMenu.SetActive(true);
                StepText.text = "Tower";
                StepTextLong.text = "Tap to place";
                StepText2.text = "Πύργος";
                StepTextLong2.text = "Πάτα για τοποθέτηση";
                SkipButton.interactable = false;
            }
            else if (m_CurrentGoalIndex == 2)
            {
                SkipButton.interactable = true;
                m_ObjectSpawner.spawnOptionIndex = 1;
                StepText.text = "Motherboard";
                StepTextLong.text = "Tap to place";
                StepText2.text = "Μητρική πλακέτα";
                StepTextLong2.text = "Πάτα για τοποθέτηση";
            }
            else if (m_CurrentGoalIndex == 3)
            {
                StepTextLong.text = "Drag to move";
                StepTextLong2.text = "Σύρε για μετακίνηση";
            }
            else if (m_CurrentGoalIndex == 4)
            {
                StepTextLong.text = "Drag to screw";
                StepTextLong2.text = "Σύρε για βίδωμα";
            }
            else if (m_CurrentGoalIndex == 5)
            {
                var mobo = m_ObjectSpawner.spawnedObjects[1];
                mobo.SetActive(true);
                Destroy(GameObject.FindWithTag("Screwdriver"));
                m_ObjectSpawner.spawnOptionIndex = 2;
                StepText.text = "CPU";
                StepTextLong.text = "Tap to place";
                StepText2.text = "Επεξεργαστής";
                StepTextLong2.text = "Πάτα για τοποθέτηση";
            }
            else if (m_CurrentGoalIndex == 6)
            {
                StepTextLong.text = "Drag to rotate";
                StepTextLong2.text = "Σύρε για περιστροφή";
            }
            else if (m_CurrentGoalIndex == 7)
            {
                StepTextLong.text = "Drag to move";
                StepTextLong2.text = "Σύρε για μετακίνηση";
            }
            else if (m_CurrentGoalIndex == 8)
            {
                var cpu = m_ObjectSpawner.spawnedObjects[2];
                cpu.SetActive(true);
                m_ObjectSpawner.spawnOptionIndex = 3;
                StepText.text = "Cooler";
                StepTextLong.text = "Tap to place";
                StepText2.text = "Ψύκτρα";
                StepTextLong2.text = "Πάτα για τοποθέτηση";
            }
            else if (m_CurrentGoalIndex == 9)
            {
                StepTextLong.text = "Drag to move";
                StepTextLong2.text = "Σύρε για μετακίνηση";
            }
            else if (m_CurrentGoalIndex == 10)
            {
                StepTextLong.text = "Drag to screw";
                StepTextLong2.text = "Σύρε για βίδωμα";
            }
            else if (m_CurrentGoalIndex == 11)
            {
                m_ObjectSpawner.spawnOptionIndex = 4;
                StepText.text = "RAM";
                StepTextLong.text = "Tap to place (first)";
                StepText2.text = "Μνήμη RAM";
                StepTextLong2.text = "Πάτα για τοποθέτηση (πρώτη)";
                Destroy(GameObject.FindWithTag("Screwdriver"));
                var cooler = m_ObjectSpawner.spawnedObjects[3];
                cooler.SetActive(true);
                FindInactiveObjectByName("hint_ram1").tag = "hint_ram";
                FindInactiveObjectByName("hint_ram2").tag = "Untagged";
            }
            else if (m_CurrentGoalIndex == 12)
            {
                m_ObjectSpawner.spawnOptionIndex = 0;
                StepTextLong.text = "Drag to rotate";
                StepTextLong2.text = "Σύρε για περιστροφή";
            }
            else if (m_CurrentGoalIndex == 13)
            {
                var ram = m_ObjectSpawner.spawnedObjects[4];
                ram.GetComponent<DragToMove>().enabled = true;
                StepTextLong.text = "Drag to move";
                StepTextLong2.text = "Σύρε για μετακίνηση";
            }
            else if (m_CurrentGoalIndex == 14)
            {
                m_ObjectSpawner.spawnOptionIndex = 4;
                StepTextLong.text = "Tap to place (second)";
                StepTextLong2.text = "Πάτα για τοποθέτηση (δεύτερη)";
                FindInactiveObjectByName("hint_ram1").tag = "Untagged";
                FindInactiveObjectByName("hint_ram2").tag = "hint_ram";
            }
            else if (m_CurrentGoalIndex == 15)
            {
                m_ObjectSpawner.spawnOptionIndex = 0;
                StepTextLong.text = "Drag to rotate";
                StepTextLong2.text = "Σύρε για περιστροφή";
            }
            else if (m_CurrentGoalIndex == 16)
            {
                var ram = m_ObjectSpawner.spawnedObjects[5];
                ram.GetComponent<DragToMove>().enabled = true;
                StepTextLong.text = "Drag to move";
                StepTextLong2.text = "Σύρε για μετακίνηση";
            }
            else if (m_CurrentGoalIndex == 17)
            {
                m_ObjectSpawner.spawnOptionIndex = 5;
                StepText.text = "HDD";
                StepTextLong.text = "Tap to place (first)";
                StepText2.text = "Σκληρός δίσκος";
                StepTextLong2.text = "Πάτα για τοποθέτηση (πρώτος)";
                m_ObjectSpawner.spawnedObjects[0].transform.Find("hint_hdd1").tag = "hint_hdd";
                m_ObjectSpawner.spawnedObjects[0].transform.Find("hint_hdd2").tag = "Untagged";

            }
            else if (m_CurrentGoalIndex == 18)
            {

                m_ObjectSpawner.spawnOptionIndex = 0;
                StepTextLong.text = "Drag to move";
                StepTextLong2.text = "Σύρε για μετακίνηση";
            }
            else if (m_CurrentGoalIndex == 19)
            {
                m_ObjectSpawner.spawnOptionIndex = 5;
                StepText.text = "HDD";
                StepTextLong.text = "Tap to place (second)";
                StepText2.text = "Σκληρός δίσκος";
                StepTextLong2.text = "Πάτα για τοποθέτηση (δεύτερος)";
                m_ObjectSpawner.spawnedObjects[0].transform.Find("hint_hdd1").tag = "Untagged";
                m_ObjectSpawner.spawnedObjects[0].transform.Find("hint_hdd2").tag = "hint_hdd";

            }
            else if (m_CurrentGoalIndex == 20)
            {
                m_ObjectSpawner.spawnOptionIndex = 0;
                StepTextLong.text = "Drag to move";
                StepTextLong2.text = "Σύρε για μετακίνηση";
            }
            else if (m_CurrentGoalIndex == 21)
            {
                var hdd1 = m_ObjectSpawner.spawnedObjects[6];
                hdd1.SetActive(true);
                var hdd2 = m_ObjectSpawner.spawnedObjects[7];
                hdd2.SetActive(true);
                m_ObjectSpawner.spawnOptionIndex = 6;
                StepText.text = "PSU";
                StepTextLong.text = "Tap to place";
                StepText2.text = "Τροφοδοτικό";
                StepTextLong2.text = "Πάτα για τοποθέτηση";
            }
            else if (m_CurrentGoalIndex == 22)
            {
                m_ObjectSpawner.spawnOptionIndex = 0;
                StepTextLong.text = "Drag to move";
                StepTextLong2.text = "Σύρε για μετακίνηση";
            }
            else if (m_CurrentGoalIndex == 23)
            {
                StepTextLong.text = "Drag to screw";
                StepTextLong2.text = "Σύρε για βίδωμα";
            }
            else if (m_CurrentGoalIndex == 24)
            {
                var psu = m_ObjectSpawner.spawnedObjects[8];
                psu.SetActive(true);
                m_ObjectSpawner.spawnOptionIndex = 7;
                Destroy(GameObject.FindWithTag("Screwdriver"));
                StepText.text = "GPU";
                StepTextLong.text = "Tap to place";
                StepText2.text = "Κάρτα γραφικών";
                StepTextLong2.text = "Πάτα για τοποθέτηση";
            }
            else if (m_CurrentGoalIndex == 25)
            {
                m_ObjectSpawner.spawnOptionIndex = 0;
                StepTextLong.text = "Drag to move";
                StepTextLong2.text = "Σύρε για μετακίνηση";
            }
            else if (m_CurrentGoalIndex == 26)
            {
                StepTextLong.text = "Drag to screw";
                StepTextLong2.text = "Σύρε για βίδωμα";
            }
            else if (m_CurrentGoalIndex == 27)
            {
                m_ObjectSpawner.spawnOptionIndex = 0;
                Destroy(GameObject.FindWithTag("Screwdriver"));
                var gpu = m_ObjectSpawner.spawnedObjects[9];
                gpu.SetActive(true);
                StepText.text = "Done!";
                StepTextLong.text = "";
                StepText2.text = "Τέλος!";
                StepTextLong2.text = "";
                SkipButton.gameObject.SetActive(false);
                FinishButton.gameObject.SetActive(true);
            }
        }
    }

    public void SkipStepFunc()
    {
        if (m_CurrentGoalIndex == 2)
        {
            var mobo = m_ObjectSpawner.spawnedObjects[0].transform.Find("hintMobo");
            mobo.gameObject.SetActive(true);
            mobo.GetComponent<DragToMove>().enabled = false;
            mobo.GetComponent<SnapToTarget>().enabled = false;
            mobo.GetComponent<LatchController>().enabled = false;
            m_ObjectSpawner.spawnedObjects.Add(mobo.gameObject);
            m_ObjectSpawner.spawnOptionIndex = 0;
            SkipSteps(3);
            Destroy(GameObject.FindWithTag("Screwdriver"));
        }
        else if (m_CurrentGoalIndex == 3)
        {
            m_ObjectSpawner.spawnedObjects[1].transform.position = m_ObjectSpawner.spawnedObjects[0].transform.Find("hintMobo").transform.position;
            SkipSteps(0);
        }
        else if (m_CurrentGoalIndex == 4)
        {
            CompleteGoal();
        }
        else if (m_CurrentGoalIndex == 5)
        {
            var cpu = m_ObjectSpawner.spawnedObjects[1].transform.Find("hint_cpu");
            cpu.gameObject.SetActive(true);
            cpu.GetComponent<DragToMove>().enabled = false;
            cpu.GetComponent<SnapToTargetCPU>().enabled = false;
            cpu.GetComponent<DragToRotate>().enabled = false;
            cpu.GetComponent<SnapToRotation>().enabled = false;
            m_ObjectSpawner.spawnedObjects.Add(cpu.gameObject);
            m_ObjectSpawner.spawnOptionIndex = 0;
            SkipSteps(2);
        }
        else if (m_CurrentGoalIndex == 6)
        {
            var cpu = m_ObjectSpawner.spawnedObjects[2];
            cpu.GetComponent<DragToMove>().enabled = false;
            cpu.GetComponent<DragToRotate>().enabled = false;
            cpu.transform.rotation = m_ObjectSpawner.spawnedObjects[1].transform.Find("hint_cpu").rotation;
            cpu.transform.position = m_ObjectSpawner.spawnedObjects[1].transform.Find("hint_cpu").position;
        }
        else if (m_CurrentGoalIndex == 7)
        {
            var cpu = m_ObjectSpawner.spawnedObjects[2];
            cpu.GetComponent<DragToMove>().enabled = false;
            cpu.transform.position = m_ObjectSpawner.spawnedObjects[1].transform.Find("hint_cpu").position;
        }
        else if (m_CurrentGoalIndex == 8)
        {
            var cooler = m_ObjectSpawner.spawnedObjects[1].transform.Find("hint_cooler");
            cooler.gameObject.SetActive(true);
            cooler.GetComponent<DragToMove>().enabled = false;
            cooler.GetComponent<SnapToTargetCooler>().enabled = false;
            m_ObjectSpawner.spawnedObjects.Add(cooler.gameObject);
            m_ObjectSpawner.spawnOptionIndex = 0;
            SkipSteps(3);
        }
        else if (m_CurrentGoalIndex == 9)
        {
            var cooler = m_ObjectSpawner.spawnedObjects[3];
            cooler.GetComponent<DragToMove>().enabled = false;
            cooler.transform.position = m_ObjectSpawner.spawnedObjects[1].transform.Find("hint_cooler").position;
            CompleteGoal();
        }
        else if (m_CurrentGoalIndex == 10)
        {
            Destroy(GameObject.FindWithTag("Screwdriver"));
            CompleteGoal();
        }
        else if (m_CurrentGoalIndex == 11)
        {
            var ram1 = m_ObjectSpawner.spawnedObjects[1].transform.Find("hint_ram1");
            ram1.gameObject.SetActive(true);
            m_ObjectSpawner.spawnedObjects.Add(ram1.gameObject);
            var ram2 = m_ObjectSpawner.spawnedObjects[1].transform.Find("hint_ram2");
            ram2.gameObject.SetActive(true);
            m_ObjectSpawner.spawnedObjects.Add(ram2.gameObject);
            SkipSteps(6);
        }
        else if (m_CurrentGoalIndex == 12)
        {
            var ram1 = m_ObjectSpawner.spawnedObjects[4];
            ram1.transform.rotation = m_ObjectSpawner.spawnedObjects[1].transform.Find("hint_ram1").rotation;
            ram1.transform.position = m_ObjectSpawner.spawnedObjects[1].transform.Find("hint_ram1").position;

            var ram2 = m_ObjectSpawner.spawnedObjects[1].transform.Find("hint_ram2");
            ram2.gameObject.SetActive(true);
            m_ObjectSpawner.spawnedObjects.Add(ram2.gameObject);
            SkipSteps(3);
        }
        else if (m_CurrentGoalIndex == 13)
        {
            var ram1 = m_ObjectSpawner.spawnedObjects[4];
            ram1.transform.position = m_ObjectSpawner.spawnedObjects[1].transform.Find("hint_ram1").position;

            var ram2 = m_ObjectSpawner.spawnedObjects[1].transform.Find("hint_ram2");
            ram2.gameObject.SetActive(true);
            m_ObjectSpawner.spawnedObjects.Add(ram2.gameObject);
            SkipSteps(3);
        }
        else if (m_CurrentGoalIndex == 14)
        {
            var ram2 = m_ObjectSpawner.spawnedObjects[1].transform.Find("hint_ram2");
            ram2.gameObject.SetActive(true);
            m_ObjectSpawner.spawnedObjects.Add(ram2.gameObject);
            SkipSteps(3);
        }
        else if (m_CurrentGoalIndex == 15)
        {
            var ram2 = m_ObjectSpawner.spawnedObjects[5];
            ram2.transform.rotation = m_ObjectSpawner.spawnedObjects[1].transform.Find("hint_ram2").rotation;
            ram2.transform.position = m_ObjectSpawner.spawnedObjects[1].transform.Find("hint_ram2").position;
        }
        else if (m_CurrentGoalIndex == 16)
        {
            var ram2 = m_ObjectSpawner.spawnedObjects[5];
            ram2.transform.position = m_ObjectSpawner.spawnedObjects[1].transform.Find("hint_ram2").position;
        }
        else if (m_CurrentGoalIndex == 17)
        {
            var hdd1 = m_ObjectSpawner.spawnedObjects[0].transform.Find("hint_hdd1");
            hdd1.gameObject.SetActive(true);
            hdd1.GetComponent<DragToMove>().enabled = false;
            m_ObjectSpawner.spawnedObjects.Add(hdd1.gameObject);

            var hdd2 = m_ObjectSpawner.spawnedObjects[0].transform.Find("hint_hdd2");
            hdd2.gameObject.SetActive(true);
            hdd2.GetComponent<DragToMove>().enabled = false;
            m_ObjectSpawner.spawnedObjects.Add(hdd2.gameObject);
            SkipSteps(4);
        }
        else if (m_CurrentGoalIndex == 18)
        {
            var hdd1 = m_ObjectSpawner.spawnedObjects[6];
            hdd1.transform.position = m_ObjectSpawner.spawnedObjects[0].transform.Find("hint_hdd1").position;

            var hdd2 = m_ObjectSpawner.spawnedObjects[0].transform.Find("hint_hdd2");
            hdd2.gameObject.SetActive(true);
            hdd2.GetComponent<DragToMove>().enabled = false;
            m_ObjectSpawner.spawnedObjects.Add(hdd2.gameObject);
            SkipSteps(2);
        }
        else if (m_CurrentGoalIndex == 19)
        {
            var hdd2 = m_ObjectSpawner.spawnedObjects[0].transform.Find("hint_hdd2");
            hdd2.gameObject.SetActive(true);
            hdd2.GetComponent<DragToMove>().enabled = false;
            m_ObjectSpawner.spawnedObjects.Add(hdd2.gameObject);
            SkipSteps(2);
        }
        else if (m_CurrentGoalIndex == 20)
        {
            var hdd2 = m_ObjectSpawner.spawnedObjects[7];
            hdd2.transform.position = m_ObjectSpawner.spawnedObjects[0].transform.Find("hint_hdd2").position;
        }
        else if (m_CurrentGoalIndex == 21)
        {
            var psu = m_ObjectSpawner.spawnedObjects[0].transform.Find("hint_psu");
            psu.gameObject.SetActive(true);
            psu.GetComponent<DragToMove>().enabled = false;
            m_ObjectSpawner.spawnedObjects.Add(psu.gameObject);
            SkipSteps(3);
        }
        else if (m_CurrentGoalIndex == 22)
        {
            var psu = m_ObjectSpawner.spawnedObjects[8];
            psu.transform.position = m_ObjectSpawner.spawnedObjects[0].transform.Find("hint_psu").position;
            SkipSteps(1);
        }
        else if (m_CurrentGoalIndex == 23)
        {
            CompleteGoal();
        }
        else if (m_CurrentGoalIndex == 24)
        {
            var gpu = m_ObjectSpawner.spawnedObjects[0].transform.Find("hint_gpu");
            gpu.gameObject.SetActive(true);
            gpu.GetComponent<DragToMove>().enabled = false;
            m_ObjectSpawner.spawnedObjects.Add(gpu.gameObject);
            SkipSteps(3);
        }
        else if (m_CurrentGoalIndex == 25)
        {
            var gpu = m_ObjectSpawner.spawnedObjects[9];
            gpu.transform.position = m_ObjectSpawner.spawnedObjects[0].transform.Find("hint_gpu").position;
            SkipSteps(1);
        }
        else if (m_CurrentGoalIndex == 26)
        {
            CompleteGoal();
        }
    }

    void SkipSteps(int num)
    {
        for (int i = 0; i < num; i++)
        {
            SkipGoal();
        }
        PreprocessGoal();
    }

    void SkipGoal()
    {      
        if (m_CurrentGoal.CurrentGoal == OnboardingGoals.TapSurface)
            m_ObjectSpawner.objectSpawned -= OnObjectSpawned;

        m_CurrentGoal.Completed = true;
        m_CurrentGoalIndex++;
        if (m_OnboardingGoals.Count > 0)
        {
            m_CurrentGoal = m_OnboardingGoals.Dequeue();
            m_StepList[m_CurrentGoalIndex - 1].stepObject.SetActive(false);
            m_StepList[m_CurrentGoalIndex].stepObject.SetActive(true);
        }
        else
        {
            m_StepList[m_CurrentGoalIndex - 1].stepObject.SetActive(false);
            m_AllGoalsFinished = true;
            return;
        }
    }

    void CompleteGoal()
    {      
        if (m_CurrentGoal.CurrentGoal == OnboardingGoals.TapSurface)
            m_ObjectSpawner.objectSpawned -= OnObjectSpawned;

        m_CurrentGoal.Completed = true;
        m_CurrentGoalIndex++;
        if (m_OnboardingGoals.Count > 0)
        {
            m_CurrentGoal = m_OnboardingGoals.Dequeue();
            m_StepList[m_CurrentGoalIndex - 1].stepObject.SetActive(false);
            m_StepList[m_CurrentGoalIndex].stepObject.SetActive(true);
        }
        else
        {
            m_StepList[m_CurrentGoalIndex - 1].stepObject.SetActive(false);
            m_AllGoalsFinished = true;
            return;
        }

        PreprocessGoal();
    }

    void PreprocessGoal()
    {
        Scene currentScene = SceneManager.GetActiveScene();

        if (m_CurrentGoal.CurrentGoal == OnboardingGoals.FindSurfaces)
        {
            desktopButton.SetActive(true);
            m_CurrentCoroutine = StartCoroutine(WaitUntilNextCard(5f));
        }
        else if (m_CurrentGoal.CurrentGoal == OnboardingGoals.Hints && currentScene.buildIndex == 1)
        {
            if (currentScene.buildIndex == 1)
            {
                if (m_CurrentGoalIndex == 7)
                {
                    var mobo = GameObject.FindWithTag("Motherboard");
                    mobo.GetComponent<DragToMove>().enabled = true;
                    var hintMobo = FindInactiveObjectByName("hintMobo");
                    hintMobo.SetActive(true);
                }
                if (m_CurrentGoalIndex == 21)
                {
                    var cpu = GameObject.FindWithTag("CPU");
                    cpu.GetComponent<DragToMove>().enabled = true;
                    var hintCPU = FindInactiveObjectByName("hint_cpu");
                    hintCPU.SetActive(true);
                }
                if (m_CurrentGoalIndex == 30)
                {
                    var cooler = GameObject.FindWithTag("Cooler");
                    cooler.GetComponent<DragToMove>().enabled = true;
                    var hintCooler = FindInactiveObjectByName("hint_cooler");
                    hintCooler.SetActive(true);
                }
                if (m_CurrentGoalIndex == 43)
                {
                    var ram = GameObject.FindWithTag("RAM");
                    ram.GetComponent<DragToMove>().enabled = true;
                    var hintram = FindInactiveObjectByName("hint_ram1");
                    hintram.tag = "hint_ram";
                    hintram.SetActive(true);
                }
                if (m_CurrentGoalIndex == 48)
                {
                    var ram = GameObject.FindWithTag("RAM");
                    ram.GetComponent<DragToMove>().enabled = true;
                    var hintram = FindInactiveObjectByName("hint_ram2");
                    hintram.tag = "hint_ram";
                    hintram.SetActive(true);
                }
                if (m_CurrentGoalIndex == 41)
                {
                    ramButton.SetActive(false);
                }
                if (m_CurrentGoalIndex == 54)
                {
                    var hdd = GameObject.FindWithTag("HDD");
                    hdd.GetComponent<DragToMove>().enabled = true;
                    var hintHDD = FindInactiveObjectByName("hint_hdd");
                    hintHDD.tag = "hint_hdd";
                    hintHDD.SetActive(true);

                    hddButton.SetActive(false);
                }
                if (m_CurrentGoalIndex == 57)
                {
                    var hdd = GameObject.FindWithTag("HDD");
                    var hintHDD = FindInactiveObjectByName("hint_hdd2");
                    Vector3 pos = hdd.transform.position;
                    pos.y = hintHDD.transform.position.y;
                    hdd.transform.position = pos;
                    hdd.GetComponent<DragToMove>().enabled = true;

                    hintHDD.tag = "hint_hdd";
                    hintHDD.SetActive(true);
                }
                if (m_CurrentGoalIndex == 66)
                {
                    var psu = GameObject.FindWithTag("PSU");
                    psu.GetComponent<DragToMove>().enabled = true;
                    var hintPSU = FindInactiveObjectByName("hint_psu");
                    hintPSU.SetActive(true);
                }

                if (m_CurrentGoalIndex == 75)
                {
                    var gpu = GameObject.FindWithTag("GPU");
                    gpu.GetComponent<DragToMove>().enabled = true;
                    var hintGPU = FindInactiveObjectByName("hint_gpu");
                    hintGPU.SetActive(true);
                }
            }
            m_CurrentCoroutine = StartCoroutine(WaitUntilNextCard(6f));
        }
        else if (m_CurrentGoal.CurrentGoal == OnboardingGoals.Scale)
        {
            m_CurrentCoroutine = StartCoroutine(WaitUntilNextCard(8f));
        }
        else if (m_CurrentGoal.CurrentGoal == OnboardingGoals.TapSurface)
        {
            m_SurfacesTapped = 0;
            m_ObjectSpawner.objectSpawned += OnObjectSpawned;

            if (currentScene.buildIndex == 1)
            {
                if (m_CurrentGoalIndex == 18)
                {
                    cpuButton.SetActive(true);
                }
                if (m_CurrentGoalIndex == 29)
                {
                    coolerButton.SetActive(true);
                }
                if (m_CurrentGoalIndex == 40 || m_CurrentGoalIndex == 45)
                {
                    ramButton.SetActive(true);
                }
                if (m_CurrentGoalIndex == 53 || m_CurrentGoalIndex == 56)
                {
                    hddButton.SetActive(true);
                }
                if (m_CurrentGoalIndex == 65)
                {
                    psuButton.SetActive(true);
                }
                if (m_CurrentGoalIndex == 74)
                {
                    gpuButton.SetActive(true);
                }
            }
        }
            else if (m_CurrentGoal.CurrentGoal == OnboardingGoals.PlaceMotherboard)
            {
                var mobo = GameObject.FindWithTag("Motherboard");
                SnapToTarget snapScript = mobo.GetComponent<SnapToTarget>();

                mobo.GetComponent<DragToMove>().enabled = true;

                snapScript.Snapped += () =>
                {
                    CompleteGoal();
                };

            }
            else if (m_CurrentGoal.CurrentGoal == OnboardingGoals.ScrewMotherboard)
            {
                int counter = 1;
                var screwdriver = Instantiate(m_StepList[m_CurrentGoalIndex].stepObject);
                var screw = screwdriver.transform.Find("Screw_Cross");
                var mobo = GameObject.FindWithTag("Motherboard");
                screwdriver.transform.position = mobo.transform.Find("hole1").transform.position - mobo.transform.Find("hole1").transform.forward * 0.185f;
                screwdriver.transform.rotation = Quaternion.Euler(mobo.transform.eulerAngles.x - 90f, mobo.transform.eulerAngles.y - 180f, mobo.transform.eulerAngles.z);

                ScrewdriverController screwScript = screwdriver.GetComponent<ScrewdriverController>();

                screwScript.Screwed += () =>
                {
                    var hole = mobo.transform.Find("hole" + counter);
                    var newScrew = Instantiate(screw, screwdriver.transform);
                    newScrew.transform.position = screw.transform.position;
                    newScrew.transform.rotation = screw.transform.rotation;
                    newScrew.transform.localScale = screw.transform.localScale;
                    newScrew.transform.SetParent(hole.transform);

                    counter++;

                    if (counter <= 9) // 9
                    {
                        var newhole = mobo.transform.Find("hole" + counter);
                        screwdriver.transform.position = newhole.transform.position - newhole.transform.forward * 0.185f;
                    }
                    else
                    {
                        Destroy(screwdriver);
                        CompleteGoal();
                    }

                };

            }
            else if (m_CurrentGoal.CurrentGoal == OnboardingGoals.CPU_Latch)
            {
                var mobo = GameObject.FindWithTag("Motherboard");
                LatchController latchScript = mobo.GetComponent<LatchController>();
                latchScript.enabled = true;

                latchScript.Touched += () =>
                {
                    StartCoroutine(WaitForSecondsToComplete(0.8f));
                };
            }
            else if (m_CurrentGoal.CurrentGoal == OnboardingGoals.RotateCPU)
            {
                var cpu = GameObject.FindWithTag("CPU");
                DragToRotate rotateScript = cpu.GetComponent<DragToRotate>();
                SnapToRotation snapScript = cpu.GetComponent<SnapToRotation>();
                rotateScript.enabled = true;
                snapScript.enabled = true;

                snapScript.Snapped += () =>
                {
                    var mobo = GameObject.FindWithTag("Motherboard");
                    cpu.transform.SetParent(mobo.transform);
                    CompleteGoal();

                };
            }
            else if (m_CurrentGoal.CurrentGoal == OnboardingGoals.PlaceCPU)
            {
                var cpu = GameObject.FindWithTag("CPU");

                SnapToTargetCPU snapScript = cpu.GetComponent<SnapToTargetCPU>();
                snapScript.enabled = true;

                DragToMove moveScript = cpu.GetComponent<DragToMove>();
                moveScript.enabled = true;

                snapScript.Snapped += () =>
                {
                    CompleteGoal();
                };
            }
            else if (m_CurrentGoal.CurrentGoal == OnboardingGoals.PlaceCooler)
            {
                var cooler = GameObject.FindWithTag("Cooler");

                SnapToTargetCooler snapScript = cooler.GetComponent<SnapToTargetCooler>();
                snapScript.enabled = true;

                snapScript.Snapped += () =>
                {
                    CompleteGoal();
                };
            }
            else if (m_CurrentGoal.CurrentGoal == OnboardingGoals.ScrewCooler)
            {
                int counter = 0;
                var screwdriver = Instantiate(m_StepList[m_CurrentGoalIndex].stepObject);
                var screw = screwdriver.transform.Find("Screw_Cross");
                screw.gameObject.SetActive(false);
                var cooler = GameObject.FindWithTag("Cooler");
                screwdriver.transform.position = cooler.transform.Find("hole1").transform.position + cooler.transform.Find("hole1").transform.up * 0.175f;
                screwdriver.transform.rotation = Quaternion.Euler(cooler.transform.eulerAngles.x, cooler.transform.eulerAngles.y - 90f, cooler.transform.eulerAngles.z);

                ScrewdriverController screwScript = screwdriver.GetComponent<ScrewdriverController>();

                screwScript.Screwed += () =>
                {
                    counter++;

                    if (counter <= 7) // 7
                    {
                        var newhole = cooler.transform.Find("hole" + (counter % 4 + 1));
                        screwdriver.transform.position = newhole.transform.position + newhole.transform.up * 0.175f;
                    }
                    else
                    {
                        Destroy(screwdriver);
                        CompleteGoal();
                    }
                };
            }
            else if (m_CurrentGoal.CurrentGoal == OnboardingGoals.RotateRAM)
            {
                var ram = GameObject.FindWithTag("RAM");
                DragToRotate rotateScript = ram.GetComponent<DragToRotate>();
                SnapToRotation snapScript = ram.GetComponent<SnapToRotation>();
                rotateScript.enabled = true;
                snapScript.enabled = true;

                snapScript.Snapped += () =>
                {
                    var mobo = GameObject.FindWithTag("Motherboard");
                    ram.transform.SetParent(mobo.transform);
                    CompleteGoal();
                };
            }
            else if (m_CurrentGoal.CurrentGoal == OnboardingGoals.PlaceRAM)
            {
                var ram = GameObject.FindWithTag("RAM");
                ram.tag = "Untagged";

                SnapToTargetRAM snapScript = ram.GetComponent<SnapToTargetRAM>();
                snapScript.enabled = true;

                snapScript.Snapped += () =>
                {
                    CompleteGoal();
                };
            }
            else if (m_CurrentGoal.CurrentGoal == OnboardingGoals.PlaceHDD)
            {
                var hdd = GameObject.FindWithTag("HDD");
                hdd.tag = "Untagged";

                SnapToTargetHDD snapScript = hdd.GetComponent<SnapToTargetHDD>();

                hdd.GetComponent<DragToMove>().enabled = true;

                snapScript.Snapped += () =>
                {
                    CompleteGoal();
                };

            }
            else if (m_CurrentGoal.CurrentGoal == OnboardingGoals.PlacePSU)
            {
                var psu = GameObject.FindWithTag("PSU");

                SnapToTargetPSU snapScript = psu.GetComponent<SnapToTargetPSU>();
                snapScript.enabled = true;

                snapScript.Snapped += () =>
                {
                    CompleteGoal();
                };
            }
            else if (m_CurrentGoal.CurrentGoal == OnboardingGoals.ScrewPSU)
            {
                int counter = 1;
                var screwdriver = Instantiate(m_StepList[m_CurrentGoalIndex].stepObject);
                var screw = screwdriver.transform.Find("Screw_Cross");
                screw.gameObject.SetActive(true);
                var psu = GameObject.FindWithTag("PSU");
                screwdriver.transform.position = psu.transform.Find("hole1").transform.position + psu.transform.Find("hole1").transform.right * 0.18f;
                screwdriver.transform.rotation = Quaternion.Euler(psu.transform.eulerAngles.x, psu.transform.eulerAngles.y - 180f, psu.transform.eulerAngles.z);

                ScrewdriverController screwScript = screwdriver.GetComponent<ScrewdriverController>();

                screwScript.Screwed += () =>
                {
                    var hole = psu.transform.Find("hole" + counter);
                    var newScrew = Instantiate(screw, screwdriver.transform);
                    newScrew.transform.position = screw.transform.position;
                    newScrew.transform.rotation = screw.transform.rotation;
                    newScrew.transform.localScale = screw.transform.localScale;
                    newScrew.transform.SetParent(hole.transform);

                    counter++;

                    if (counter <= 4) // 4
                    {
                        var newhole = psu.transform.Find("hole" + counter);
                        screwdriver.transform.position = newhole.transform.position + newhole.transform.right * 0.18f;
                    }
                    else
                    {
                        Destroy(screwdriver);
                        CompleteGoal();
                    }
                };
            }
            else if (m_CurrentGoal.CurrentGoal == OnboardingGoals.PlaceGPU)
            {
                var gpu = GameObject.FindWithTag("GPU");

                SnapToTargetGPU snapScript = gpu.GetComponent<SnapToTargetGPU>();
                snapScript.enabled = true;

                snapScript.Snapped += () =>
                {
                    CompleteGoal();
                };
            }
            else if (m_CurrentGoal.CurrentGoal == OnboardingGoals.ScrewGPU)
            {
                int counter = 1;
                var screwdriver = Instantiate(m_StepList[m_CurrentGoalIndex].stepObject);
                var screw = screwdriver.transform.Find("Screw_Cross");
                screw.gameObject.SetActive(true);
                var gpu = GameObject.FindWithTag("GPU");
                screwdriver.transform.position = gpu.transform.Find("hole_gpu").transform.position + gpu.transform.Find("hole_gpu").transform.forward * 0.18f;
                screwdriver.transform.rotation = Quaternion.Euler(gpu.transform.eulerAngles.x + 90f, gpu.transform.eulerAngles.y - 180f, gpu.transform.eulerAngles.z);

                ScrewdriverController screwScript = screwdriver.GetComponent<ScrewdriverController>();

                screwScript.Screwed += () =>
                {
                    var hole = gpu.transform.Find("hole_gpu");
                    var newScrew = Instantiate(screw, screwdriver.transform);
                    newScrew.transform.position = screw.transform.position;
                    newScrew.transform.rotation = screw.transform.rotation;
                    newScrew.transform.localScale = screw.transform.localScale;
                    newScrew.transform.SetParent(hole.transform);
                    Destroy(screwdriver);
                    CompleteGoal();
                };
            }
    }

    public IEnumerator Wait(float seconds)
    {
        yield return new WaitForSeconds(seconds);
    }

    public IEnumerator WaitForSecondsToComplete(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        var mobo = GameObject.FindWithTag("Motherboard");
        LatchController latchScript = mobo.GetComponent<LatchController>();
        latchScript.enabled = false;
        CompleteGoal();
    }

    /// <summary>
    /// Tells the Goal Manager to wait for a specific number of seconds before completing
    /// the goal and showing the next card.
    /// </summary>
    /// <param name="seconds">The number of seconds to wait before showing the card.</param>
    /// <returns>Returns an IEnumerator for the current coroutine running.</returns>
    public IEnumerator WaitUntilNextCard(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        if (!Pointer.current.press.wasPressedThisFrame)
        {
            m_CurrentCoroutine = null;
            CompleteGoal();
        }
    }

    /// <summary>
    /// Forces the completion of the current goal and moves to the next.
    /// </summary>
    public void ForceCompleteGoal()
    {
        CompleteGoal();
    }

    void OnObjectSpawned(GameObject spawnedObject)
    {
        m_SurfacesTapped++;
        if (m_CurrentGoal.CurrentGoal == OnboardingGoals.TapSurface && m_SurfacesTapped >= k_NumberOfSurfacesTappedToCompleteGoal)
        {
            CompleteGoal();
        }
    }

    public void CableChange(string tag)
    {
        var cable = FindInactiveObjectByName(tag);
        if (cable.activeInHierarchy)
        {
            cable.SetActive(false);
            cableCounter--;
        }
        else
        {
            cable.SetActive(true);
            cableCounter++;
        }

        if (cableCounter >= 4)
        {
            cableContinue.interactable = true;
        }
        else
        {
            cableContinue.interactable = false;
        }
    }


    public void ButtonChange(string tag)
    {
        var b = GameObject.FindWithTag(tag);
        Button button = b.GetComponent<Button>();
        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        Color green = new Color(0.270f, 0.708f, 0.330f, 1.000f);
        Color red = new Color(0.737f, 0.247f, 0.212f, 1.000f);

        if (button.image.color == red)
        {
            button.image.color = green;
            buttonText.text = "C" + buttonText.text.Substring(4);
        }
        else
        {
            button.image.color = red;
            buttonText.text = "Disc" + buttonText.text.Substring(1);
        }
    }



    /// <summary>
    /// Triggers a restart of the onboarding/coaching process.
    /// </summary>
    public void StartCoaching()
    {
        if (m_OnboardingGoals != null)
        {
            m_OnboardingGoals.Clear();
        }

        m_OnboardingGoals = new Queue<Goal>();

        Scene currentScene = SceneManager.GetActiveScene();

        if (!m_AllGoalsFinished && currentScene.buildIndex == 1)
        {
            var findSurfaceGoal = new Goal(OnboardingGoals.FindSurfaces);
            m_OnboardingGoals.Enqueue(findSurfaceGoal);
        }

        int startingStep = m_AllGoalsFinished ? 1 : 0;

        var tapSurfaceGoal = new Goal(OnboardingGoals.TapSurface);
        var translateHintsGoal = new Goal(OnboardingGoals.Hints);
        //var scaleHintsGoal = new Goal(OnboardingGoals.Scale);
        var rotateHintsGoal = new Goal(OnboardingGoals.Hints);
        var mobo_prompt = new Goal(OnboardingGoals.Prompt);
        var place_mobo = new Goal(OnboardingGoals.PlaceMotherboard);
        var screw_mobo = new Goal(OnboardingGoals.ScrewMotherboard);

        var cpu_prompt = new Goal(OnboardingGoals.Prompt);
        var cpu_latch = new Goal(OnboardingGoals.CPU_Latch);

        var rotate_cpu = new Goal(OnboardingGoals.RotateCPU);
        var place_cpu = new Goal(OnboardingGoals.PlaceCPU);

        var cooler_prompt = new Goal(OnboardingGoals.Prompt);
        var place_cooler = new Goal(OnboardingGoals.PlaceCooler);
        var screw_cooler = new Goal(OnboardingGoals.ScrewCooler);

        var ram_prompt = new Goal(OnboardingGoals.Prompt);
        var rotate_ram = new Goal(OnboardingGoals.RotateRAM);
        var place_ram = new Goal(OnboardingGoals.PlaceRAM);

        var psu_prompt = new Goal(OnboardingGoals.Prompt);
        var hdd_prompt = new Goal(OnboardingGoals.Prompt);
        var place_hdd = new Goal(OnboardingGoals.PlaceHDD);
        var place_psu = new Goal(OnboardingGoals.PlacePSU);
        var screw_psu = new Goal(OnboardingGoals.ScrewPSU);

        var gpu_prompt = new Goal(OnboardingGoals.Prompt);
        var place_gpu = new Goal(OnboardingGoals.PlaceGPU);
        var screw_gpu = new Goal(OnboardingGoals.ScrewGPU);

        var connect_cables = new Goal(OnboardingGoals.Prompt);

        var choose_build = new Goal(OnboardingGoals.ChooseBuild);

        if (currentScene.buildIndex == 1)
        {
            m_OnboardingGoals.Enqueue(tapSurfaceGoal);
            m_OnboardingGoals.Enqueue(tapSurfaceGoal);
            //m_OnboardingGoals.Enqueue(scaleHintsGoal);
            //m_OnboardingGoals.Enqueue(rotateHintsGoal);

            //mobo
            m_OnboardingGoals.Enqueue(mobo_prompt);
            m_OnboardingGoals.Enqueue(mobo_prompt);
            m_OnboardingGoals.Enqueue(mobo_prompt);
            m_OnboardingGoals.Enqueue(mobo_prompt);
            m_OnboardingGoals.Enqueue(translateHintsGoal);
            m_OnboardingGoals.Enqueue(place_mobo);
            m_OnboardingGoals.Enqueue(mobo_prompt);
            m_OnboardingGoals.Enqueue(screw_mobo);
            m_OnboardingGoals.Enqueue(mobo_prompt);

            //cpu
            m_OnboardingGoals.Enqueue(cpu_prompt);
            m_OnboardingGoals.Enqueue(cpu_prompt);
            m_OnboardingGoals.Enqueue(cpu_prompt);
            m_OnboardingGoals.Enqueue(cpu_prompt);
            m_OnboardingGoals.Enqueue(cpu_latch);
            m_OnboardingGoals.Enqueue(cpu_prompt);
            m_OnboardingGoals.Enqueue(tapSurfaceGoal);
            m_OnboardingGoals.Enqueue(translateHintsGoal);
            m_OnboardingGoals.Enqueue(rotate_cpu);
            m_OnboardingGoals.Enqueue(translateHintsGoal);
            m_OnboardingGoals.Enqueue(place_cpu);
            m_OnboardingGoals.Enqueue(cpu_prompt);
            m_OnboardingGoals.Enqueue(cpu_latch);

            //cooler
            m_OnboardingGoals.Enqueue(cooler_prompt);
            m_OnboardingGoals.Enqueue(cooler_prompt);
            m_OnboardingGoals.Enqueue(cooler_prompt);
            m_OnboardingGoals.Enqueue(cooler_prompt);
            m_OnboardingGoals.Enqueue(tapSurfaceGoal);
            m_OnboardingGoals.Enqueue(translateHintsGoal);
            m_OnboardingGoals.Enqueue(place_cooler);
            m_OnboardingGoals.Enqueue(cooler_prompt);
            m_OnboardingGoals.Enqueue(screw_cooler);
            m_OnboardingGoals.Enqueue(cooler_prompt);

            //ram
            m_OnboardingGoals.Enqueue(ram_prompt);
            m_OnboardingGoals.Enqueue(ram_prompt);
            m_OnboardingGoals.Enqueue(ram_prompt);
            m_OnboardingGoals.Enqueue(ram_prompt);
            m_OnboardingGoals.Enqueue(ram_prompt);

            m_OnboardingGoals.Enqueue(tapSurfaceGoal);
            m_OnboardingGoals.Enqueue(translateHintsGoal);
            m_OnboardingGoals.Enqueue(rotate_ram);
            m_OnboardingGoals.Enqueue(translateHintsGoal);
            m_OnboardingGoals.Enqueue(place_ram);

            m_OnboardingGoals.Enqueue(tapSurfaceGoal);
            m_OnboardingGoals.Enqueue(translateHintsGoal);
            m_OnboardingGoals.Enqueue(rotate_ram);
            m_OnboardingGoals.Enqueue(translateHintsGoal);
            m_OnboardingGoals.Enqueue(place_ram);
            m_OnboardingGoals.Enqueue(ram_prompt);

            //hdd
            m_OnboardingGoals.Enqueue(hdd_prompt);
            m_OnboardingGoals.Enqueue(hdd_prompt);
            m_OnboardingGoals.Enqueue(tapSurfaceGoal);
            m_OnboardingGoals.Enqueue(translateHintsGoal);
            m_OnboardingGoals.Enqueue(place_hdd);
            m_OnboardingGoals.Enqueue(tapSurfaceGoal);
            m_OnboardingGoals.Enqueue(translateHintsGoal);
            m_OnboardingGoals.Enqueue(place_hdd);
            m_OnboardingGoals.Enqueue(hdd_prompt);

            //psu
            m_OnboardingGoals.Enqueue(psu_prompt);
            m_OnboardingGoals.Enqueue(psu_prompt);
            m_OnboardingGoals.Enqueue(psu_prompt);
            m_OnboardingGoals.Enqueue(psu_prompt);
            m_OnboardingGoals.Enqueue(psu_prompt);
            m_OnboardingGoals.Enqueue(tapSurfaceGoal);
            m_OnboardingGoals.Enqueue(translateHintsGoal);
            m_OnboardingGoals.Enqueue(place_psu);
            m_OnboardingGoals.Enqueue(screw_psu);
            m_OnboardingGoals.Enqueue(psu_prompt);

            //gpu
            m_OnboardingGoals.Enqueue(gpu_prompt);
            m_OnboardingGoals.Enqueue(gpu_prompt);
            m_OnboardingGoals.Enqueue(gpu_prompt);
            m_OnboardingGoals.Enqueue(gpu_prompt);
            m_OnboardingGoals.Enqueue(tapSurfaceGoal);
            m_OnboardingGoals.Enqueue(translateHintsGoal);
            m_OnboardingGoals.Enqueue(place_gpu);
            m_OnboardingGoals.Enqueue(gpu_prompt);
            m_OnboardingGoals.Enqueue(screw_gpu);
            m_OnboardingGoals.Enqueue(gpu_prompt);

            //cables
            m_OnboardingGoals.Enqueue(connect_cables);

            m_CurrentGoal = m_OnboardingGoals.Dequeue();
            m_AllGoalsFinished = false;
            m_CurrentGoalIndex = startingStep;

            m_GreetingPrompt.SetActive(false);
            m_OptionsButton.SetActive(false);
            m_CreateButton.SetActive(true);
            m_MenuManager.enabled = true;
        }
        else if (currentScene.buildIndex == 2)
        {
            m_OnboardingGoals.Enqueue(choose_build);
            m_OnboardingGoals.Enqueue(tapSurfaceGoal);
            m_OnboardingGoals.Enqueue(tapSurfaceGoal);
            m_OnboardingGoals.Enqueue(place_mobo);
            m_OnboardingGoals.Enqueue(screw_mobo);
            m_OnboardingGoals.Enqueue(tapSurfaceGoal);
            m_OnboardingGoals.Enqueue(rotate_cpu);
            m_OnboardingGoals.Enqueue(place_cpu);
            m_OnboardingGoals.Enqueue(tapSurfaceGoal);
            m_OnboardingGoals.Enqueue(place_cooler);
            m_OnboardingGoals.Enqueue(screw_cooler);
            m_OnboardingGoals.Enqueue(tapSurfaceGoal);
            m_OnboardingGoals.Enqueue(rotate_ram);
            m_OnboardingGoals.Enqueue(place_ram);
            m_OnboardingGoals.Enqueue(tapSurfaceGoal);
            m_OnboardingGoals.Enqueue(rotate_ram);
            m_OnboardingGoals.Enqueue(place_ram);
            m_OnboardingGoals.Enqueue(tapSurfaceGoal);
            m_OnboardingGoals.Enqueue(place_hdd);
            m_OnboardingGoals.Enqueue(tapSurfaceGoal);
            m_OnboardingGoals.Enqueue(place_hdd);
            m_OnboardingGoals.Enqueue(tapSurfaceGoal);
            m_OnboardingGoals.Enqueue(place_psu);
            m_OnboardingGoals.Enqueue(screw_psu);
            m_OnboardingGoals.Enqueue(tapSurfaceGoal);
            m_OnboardingGoals.Enqueue(place_gpu);
            m_OnboardingGoals.Enqueue(screw_gpu);
            m_OnboardingGoals.Enqueue(choose_build);

            m_CurrentGoal = m_OnboardingGoals.Dequeue();
            m_AllGoalsFinished = false;
            m_CurrentGoalIndex = startingStep;

            m_GreetingPrompt.SetActive(false);
            m_OptionsButton.SetActive(false);
            m_CreateButton.SetActive(false);
            m_MenuManager.enabled = true;
        }

        for (int i = startingStep; i < m_StepList.Count; i++)
        {
            if (i == startingStep)
            {
                m_StepList[i].stepObject.SetActive(true);
                PreprocessGoal();
            }
            else
            {
                m_StepList[i].stepObject.SetActive(false);
            }
        }

    }

    public void ChooseBuild(GameObject obj)
    {
        pcBuild = obj;
        chooseBuildMenu.SetActive(false);
        m_ObjectSpawner.objectPrefabs[0] = pcBuild;
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    

    public void ExitApp()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
