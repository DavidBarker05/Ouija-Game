using UnityEngine;

public static class OwnUtils
{
    public static void Exit(int exitCode = 0)
    {
#if UNITY_EDITOR
        if (exitCode != 0) Debug.LogError($"Application exited with code {exitCode}");
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit(exitCode);
    }
}
