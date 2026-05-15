[System.Serializable]
public class UserSettings
{
    public int HorizontalResolution = 1920;
    public int VerticalResolution = 1080;
    public int VSyncCount = 1;
    public float MasterVolume = 1f;
    public float MusicAmbientVolume = 1f;
    public float SoundEffectsVolume = 1f;
    public float MouseHorizontalSensitivityMultiplier = 1f;
    public float MouseVerticalSensitivityMultiplier = 1f;
    public float ControllerHorizontalSensitivityMultiplier = 1f;
    public float ControllerVerticalSensitivityMultiplier = 1f;

    public UserSettings() { }

    public UserSettings(UserSettings other)
    {
        HorizontalResolution = other.HorizontalResolution;
        VerticalResolution = other.VerticalResolution;
        VSyncCount = other.VSyncCount;
        MasterVolume = other.MasterVolume;
        MusicAmbientVolume = other.MusicAmbientVolume;
        SoundEffectsVolume = other.SoundEffectsVolume;
        MouseHorizontalSensitivityMultiplier = other.MouseHorizontalSensitivityMultiplier;
        MouseVerticalSensitivityMultiplier = other.MouseVerticalSensitivityMultiplier;
        ControllerHorizontalSensitivityMultiplier = other.ControllerHorizontalSensitivityMultiplier;
        ControllerVerticalSensitivityMultiplier = other.ControllerVerticalSensitivityMultiplier;
    }
}
