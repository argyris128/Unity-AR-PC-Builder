using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class LanguageManager : MonoBehaviour
{
    public int lang = 0;
    public static LanguageManager instance;
    public TMP_Dropdown dropdown;
    public GameObject settingsButton;
    public GameObject settingsMenu;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public void ChangeLanguage(int index)
    {
        if (instance.lang == 0)
        {
            instance.lang = 1;
            SetLanguage();
        }
        else
        {
            instance.lang = 0;
            SetLanguage();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Scene currentScene = SceneManager.GetActiveScene();

        if (currentScene.buildIndex != 0)
        {
            settingsButton = FindInactiveObjectByTag("SettingsButton");
            settingsMenu = FindInactiveObjectByTag("SettingsMenu");            
        }
        
        dropdown = FindInactiveObjectByTag("Dropdown").GetComponent<TMP_Dropdown>();

        if (dropdown != null)
        {
            dropdown.onValueChanged.RemoveAllListeners();

            if (instance.lang == 0)
                dropdown.value = 0;
            else
                dropdown.value = 1;

            dropdown.onValueChanged.AddListener(instance.ChangeLanguage);
            dropdown.RefreshShownValue();
        }

        SetLanguage();
    }

    public void SetLanguage()
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

        if (instance.lang == 0)
        {
            foreach (GameObject obj in allObjects)
            {
                if (obj.CompareTag("English") && !obj.hideFlags.HasFlag(HideFlags.NotEditable) && !obj.hideFlags.HasFlag(HideFlags.HideAndDontSave))
                {
                    obj.SetActive(true);
                }
                else if (obj.CompareTag("Greek") && !obj.hideFlags.HasFlag(HideFlags.NotEditable) && !obj.hideFlags.HasFlag(HideFlags.HideAndDontSave))
                {
                    obj.SetActive(false);
                }
            }
        }
        else
        {
            foreach (GameObject obj in allObjects)
            {
                if (obj.CompareTag("English") && !obj.hideFlags.HasFlag(HideFlags.NotEditable) && !obj.hideFlags.HasFlag(HideFlags.HideAndDontSave))
                {
                    obj.SetActive(false);
                }
                else if (obj.CompareTag("Greek") && !obj.hideFlags.HasFlag(HideFlags.NotEditable) && !obj.hideFlags.HasFlag(HideFlags.HideAndDontSave))
                {
                    obj.SetActive(true);
                }
            }
        }
    }

    public void OpenSettings()
    {
        instance.settingsButton.SetActive(false);
        instance.settingsMenu.SetActive(true);
    }

    public void CloseSettings()
    {
        instance.settingsButton.SetActive(true);
        instance.settingsMenu.SetActive(false);
    }

    GameObject FindInactiveObjectByTag(string name)
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            if (obj.CompareTag(name) && !obj.hideFlags.HasFlag(HideFlags.NotEditable) && !obj.hideFlags.HasFlag(HideFlags.HideAndDontSave))
            {
                if (obj.scene.name != null)
                {
                    return obj;
                }
            }
        }

        return null;
    }
}
