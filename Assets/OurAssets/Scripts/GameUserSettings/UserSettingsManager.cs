using System.IO;
using UnityEngine;
using UnityEngine.Audio;

public class UserSettingsManager : MonoBehaviour
{
    const string FILE_NAME = "user_settings.json";

    public static UserSettingsManager Instance { get; private set; }

    public UserSettings UserSettings { get; private set; }

    [SerializeField]
    AudioMixer m_AudioMixer;

    UserSettings m_TempSettings;
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
        UserSettings = m_TempSettings;
        Screen.SetResolution(UserSettings.HorizontalResolution, UserSettings.VerticalResolution, fullscreen: true);
        QualitySettings.vSyncCount = UserSettings.VSyncCount;
        m_AudioMixer.SetFloat("MasterVolume", Mathf.Log10(Mathf.Clamp(UserSettings.MasterVolume, 0.0001f, 1f)) * 20f);
        m_AudioMixer.SetFloat("MusicAmbientVolume", Mathf.Log10(Mathf.Clamp(UserSettings.MusicAmbientVolume, 0.0001f, 1f)) * 20f);
        m_AudioMixer.SetFloat("SoundEffectsVolume", Mathf.Log10(Mathf.Clamp(UserSettings.SoundEffectsVolume, 0.0001f, 1f)) * 20f);
        try
        {
            string json = JsonUtility.ToJson(UserSettings, prettyPrint: true);
            File.WriteAllText(m_Path, json);
        }
        catch
        {
            Debug.LogError("Failed to save user settings");
        }
    }

    public void ChangeResolution(int width, int height) => (m_TempSettings.HorizontalResolution, m_TempSettings.VerticalResolution) = (width, height);
    public void ChangeMasterVolume(float volume) => m_TempSettings.MasterVolume = volume;
    public void ChangeMusicAmbientVolume(float volume) => m_TempSettings.MusicAmbientVolume = volume;
    public void ChangeSoundEffectsVolume(float volume) => m_TempSettings.SoundEffectsVolume = volume;
}
