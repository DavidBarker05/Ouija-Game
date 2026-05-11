using System.IO;
using UnityEngine;

public class TempCacheManager : MonoBehaviour
{
    static TempCacheManager s_Instance;
    public static TempCacheManager Instance
    {
        get
        {
            if (!s_Instance)
            {
                GameObject go = new GameObject(nameof(TempCacheManager));
                s_Instance = go.AddComponent<TempCacheManager>();
                DontDestroyOnLoad(go);
            }
            return s_Instance;
        }
    }

	void Awake()
	{
        if (s_Instance && s_Instance != this) Destroy(gameObject);
        else
        {
            s_Instance = this;
            DontDestroyOnLoad(gameObject);
        }
	}

    // Delete all our temp files in the temp cache folder on quit
	void OnApplicationQuit()
	{
        string[] tempFiles = Directory.GetFiles(Application.temporaryCachePath, "*", SearchOption.AllDirectories); // Get all the temp files located in our temp cache folder
        foreach (string tempFile in tempFiles) File.Delete(tempFile); // Delete every single file in our temp cache folder
	}

    public bool ReadFile(string path, out string contents)
    {
        path = path.Trim();
        if (string.IsNullOrWhiteSpace(path))
        {
            contents = string.Empty;
            return false;
        }
        if (!path.StartsWith(Application.temporaryCachePath)) path = Path.Combine(Application.temporaryCachePath, path);
        if (!File.Exists(path))
        {
            contents = string.Empty;
            return false;
        }
        contents = File.ReadAllText(path);
        return true;
    }

    public void WriteFile(string path, string contents)
	{
		path = path.Trim();
		if (string.IsNullOrWhiteSpace(path))
		{
			contents = string.Empty;
			return;
		}
		if (!path.StartsWith(Application.temporaryCachePath)) path = Path.Combine(Application.temporaryCachePath, path);
        File.WriteAllText(path, contents);
	}

    public void DeleteFile(string path)
	{
		path = path.Trim();
        if (string.IsNullOrWhiteSpace(path)) return;
		if (!path.StartsWith(Application.temporaryCachePath)) path = Path.Combine(Application.temporaryCachePath, path);
		if (File.Exists(path)) File.Delete(path);
    }
}
