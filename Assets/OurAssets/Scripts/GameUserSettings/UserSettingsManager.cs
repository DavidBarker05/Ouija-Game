using System.IO;
using UnityEngine;
using UnityEngine.Audio;

public class UserSettingsManager : MonoBehaviour
{
    public const float MIN_SENS_MULT_UI = 0.5f;
    public const float MAX_SENS_MULT_UI = 2f;

    const string FILE_NAME = "user_settings.json";

    public static UserSettingsManager Instance { get; private set; }

    [SerializeField]
    AudioMixer m_AudioMixer;

    UserSettings m_UserSettings;
    UserSettings m_TempSettings;

    public (int HorizontalResolution, int VerticalResolution) Resolution
    {
        get => (m_UserSettings.HorizontalResolution, m_UserSettings.VerticalResolution);
        set => (m_TempSettings.HorizontalResolution, m_TempSettings.VerticalResolution) = value;
    }

    public int VSyncCount
    {
        get => m_UserSettings.VSyncCount;
        set => m_TempSettings.VSyncCount = value;
    }

    public float MasterVolume
    {
        get => m_UserSettings.MasterVolume;
        set => m_TempSettings.MasterVolume = value;
    }

    public float MusicAmbientVolume
    {
        get => m_UserSettings.MusicAmbientVolume;
        set => m_TempSettings.MusicAmbientVolume = value;
    }

    public float SoundEffectsVolume
    {
        get => m_UserSettings.SoundEffectsVolume;
        set => m_TempSettings.SoundEffectsVolume = value;
    }

    public float MouseHorizontalSensitivityMultiplier
    {
        get => m_UserSettings.MouseHorizontalSensitivityMultiplier;
        set => m_TempSettings.MouseHorizontalSensitivityMultiplier = value;
    }

    public float MouseVerticalSensitivityMultiplier
    {
        get => m_UserSettings.MouseVerticalSensitivityMultiplier;
        set => m_TempSettings.MouseVerticalSensitivityMultiplier = value;
    }

    public float ControllerHorizontalSensitivityMultiplier
    {
        get => m_UserSettings.ControllerHorizontalSensitivityMultiplier;
        set => m_TempSettings.ControllerHorizontalSensitivityMultiplier = value;
    }

    public float ControllerVerticalSensitivityMultiplier
    {
        get => m_UserSettings.ControllerVerticalSensitivityMultiplier;
        set => m_TempSettings.ControllerVerticalSensitivityMultiplier = value;
    }

    string m_Path;

    void Awake()
    {
        if (Instance && Instance != this) Destroy(gameObject);
        else
        {
            Instance = this;
            m_Path = Path.Combine(Application.persistentDataPath, FILE_NAME);
            m_TempSettings = LoadSettings();
            SaveSettings();
            DontDestroyOnLoad(gameObject);
        }
    }

    void Start() => ApplySettings();

    UserSettings LoadSettings()
    {
        if (!File.Exists(m_Path)) return new UserSettings();
        try
        {
            string contents = File.ReadAllText(m_Path);
            UserSettings userSettings = JsonUtility.FromJson<UserSettings>(contents);
            userSettings.HorizontalResolution = Mathf.Max(userSettings.HorizontalResolution, 640);
            userSettings.VerticalResolution = Mathf.Max(userSettings.VerticalResolution, 480);
            userSettings.VSyncCount = Mathf.Clamp(userSettings.VSyncCount, 0, 2);
            userSettings.MasterVolume = Mathf.Clamp01(userSettings.MasterVolume);
            userSettings.MusicAmbientVolume = Mathf.Clamp01(userSettings.MusicAmbientVolume);
            userSettings.SoundEffectsVolume = Mathf.Clamp01(userSettings.SoundEffectsVolume);
            userSettings.MouseHorizontalSensitivityMultiplier = Mathf.Max(userSettings.MouseHorizontalSensitivityMultiplier, 0.01f);
            userSettings.MouseVerticalSensitivityMultiplier = Mathf.Max(userSettings.MouseVerticalSensitivityMultiplier, 0.01f);
            userSettings.ControllerHorizontalSensitivityMultiplier = Mathf.Max(userSettings.ControllerHorizontalSensitivityMultiplier, 0.01f);
            userSettings.ControllerVerticalSensitivityMultiplier = Mathf.Max(userSettings.ControllerVerticalSensitivityMultiplier, 0.01f);
#if UNITY_EDITOR
            Debug.Log("Sucessfully loaded user settings");
#endif
            return userSettings;
        }
        catch
        {
            Debug.LogError($"Error reading \"{m_Path}\"! Using default values");
            return new UserSettings();
        }
    }

    /// <summary>
    /// Update UserSettings to match all the settings that were input into the different fields and save it to storage
    /// </summary>
    public void SaveSettings()
    {
        m_UserSettings = new UserSettings(m_TempSettings);
        ApplySettings();
        try
        {
            string json = JsonUtility.ToJson(m_UserSettings, prettyPrint: true);
            File.WriteAllText(m_Path, json);
        }
        catch
        {
            Debug.LogError("Failed to save user settings");
        }
    }

    void ApplySettings()
    {
        Screen.SetResolution(m_UserSettings.HorizontalResolution, m_UserSettings.VerticalResolution, fullscreen: true);
        QualitySettings.vSyncCount = m_UserSettings.VSyncCount;
        m_AudioMixer.SetFloat("MasterVolume", Mathf.Log10(Mathf.Clamp(m_UserSettings.MasterVolume, 0.0001f, 1f)) * 20f);
        m_AudioMixer.SetFloat("MusicAmbientVolume", Mathf.Log10(Mathf.Clamp(m_UserSettings.MusicAmbientVolume, 0.0001f, 1f)) * 20f);
        m_AudioMixer.SetFloat("SoundEffectsVolume", Mathf.Log10(Mathf.Clamp(m_UserSettings.SoundEffectsVolume, 0.0001f, 1f)) * 20f);
    }

    public void ClearTempSettings() => m_TempSettings = new UserSettings(m_UserSettings);
}
