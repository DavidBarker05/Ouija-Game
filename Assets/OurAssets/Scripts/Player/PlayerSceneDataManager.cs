using UnityEngine;

public class PlayerSceneDataManager : MonoBehaviour
{
    const string FILE_NAME = "player_scene_data.json";

    public static PlayerSceneDataManager Instance { get; private set; }

    void Awake()
    {
        if (Instance && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    public void SaveSceneData(PlayerCharacter playerCharacter)
    {
        PlayerSceneData playerSceneData = new PlayerSceneData()
        {
            Position = playerCharacter.transform.position,
            EulerAngles = playerCharacter.transform.eulerAngles,
            CameraEulerAngles = playerCharacter.CameraTarget.eulerAngles
        };
        string json = JsonUtility.ToJson(playerSceneData.Serialized, prettyPrint: false);
        TempCacheManager.Instance.WriteFile(FILE_NAME, json);
    }

    public PlayerSceneData LoadPlayerSceneData()
    {
        try
        {
            if (TempCacheManager.Instance.ReadFile(FILE_NAME, out string contents))
                return JsonUtility.FromJson<SerializedPlayerSceneData>(contents).Deserialized;
        }
        catch
        {
            Debug.LogError("Failed to read file contents using default values");
        }
        return null;
    }
}
