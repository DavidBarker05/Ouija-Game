using System;
using TMPro;
using UnityEngine;

public class StoryGeneratorScreen : MonoBehaviour
{
    [SerializeField]
    TMP_Text m_ProgressText;
    [SerializeField]
    GameObject m_StoryPanel;
    [SerializeField]
    TMP_Text m_StoryText;

    async void OnEnable()
    {
        Progress<string> progress = new Progress<string>(step => m_ProgressText.text = step);
        string story = await GameManager.Instance.StartNewGame(progress);
        m_ProgressText.gameObject.SetActive(false);
        m_StoryText.text = story;
        m_StoryPanel.SetActive(true);
    }
}
