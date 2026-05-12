using System.Collections.Generic;
using OurAssets.Scripts.Chat;
using TMPro;
using UnityEngine;

public enum BoardResponse
{
	Yes, No,
	A, B, C, D, E, F, G, H, I, J, K, L, M,
	N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
	Num1, Num2, Num3, Num4, Num5, Num6, Num7, Num8, Num9, Num0,
	Goodbye
}

[System.Serializable]
public struct BoardResponsePosition
{
    public BoardResponse BoardResponse;
    public Transform PositionOnBoard;
}

// David - NB: If any AI works on this file, follow the naming conventions used in this file for this file, any other
// file you can use the naming convention that you normally use like seen in OuijaAiOrchestrator.cs, but follow my
// naming convention in here. Also, for files like this where I created them, follow industry standard then of saying
// who contributed to what, and since I will be the one reading your code pls add a short comment explaining what your
// code does pls. Don't break these rules, they are what I follow whenever I work on someone else's code and it makes
// moderation easier in uni and also just makes code easier to understand.
public class OuijaBoard : MonoBehaviour
{
    [SerializeField]
    OuijaPlayerInputController m_OuijaPlayerInputController; // David added
    [SerializeField, Tooltip("The object that moves showing which letter was said by the spirit")]
    GameObject m_Planchette; // David added
    [SerializeField]
	BoardResponsePosition[] m_BoardResponsePositionsOnBoard; // David added
    [SerializeField, Range(0.001f, 1f), Tooltip("The time it takes for the planchette to move between letters")]
    float m_PlanchetteTravelTime = 0.25f; // David added
    [SerializeField, Range(0.001f, 1f), Tooltip("The time that the planchette will stay on a letter")]
    float m_PlancehtteWaitTime = 0.1f; // David added
    [SerializeField]
    TMP_Text m_ResponseDisplayText; // David added
    [SerializeField]
    Player m_Player; // David added
    [SerializeField]
    FirstPersonCharacter m_FirstPersonCharacter; // David added

    // David added
    Dictionary<BoardResponse, Transform> m_BoardResponsePositions = new Dictionary<BoardResponse, Transform>()
    {
        { BoardResponse.Yes,    null }, { BoardResponse.No,     null }, { BoardResponse.A,          null },
        { BoardResponse.B,      null }, { BoardResponse.C,      null }, { BoardResponse.D,          null },
        { BoardResponse.E,      null }, { BoardResponse.F,      null }, { BoardResponse.G,          null },
        { BoardResponse.H,      null }, { BoardResponse.I,      null }, { BoardResponse.J,          null },
        { BoardResponse.K,      null }, { BoardResponse.L,      null }, { BoardResponse.M,          null },
		{ BoardResponse.N,      null }, { BoardResponse.O,      null }, { BoardResponse.P,          null },
		{ BoardResponse.Q,      null }, { BoardResponse.R,      null }, { BoardResponse.S,          null },
		{ BoardResponse.T,      null }, { BoardResponse.U,      null }, { BoardResponse.V,          null },
		{ BoardResponse.W,      null }, { BoardResponse.X,      null }, { BoardResponse.Y,          null },
		{ BoardResponse.Z,      null }, { BoardResponse.Num1,   null }, { BoardResponse.Num2,       null },
		{ BoardResponse.Num3,   null }, { BoardResponse.Num4,   null }, { BoardResponse.Num5,       null },
		{ BoardResponse.Num6,   null }, { BoardResponse.Num7,   null }, { BoardResponse.Num8,       null },
		{ BoardResponse.Num9,   null }, { BoardResponse.Num0,   null }, { BoardResponse.Goodbye,    null }
	};
    
    public bool DisplayingResponse { get; private set; } = false; // David added

    string m_CurrentText = string.Empty; // David added
    float m_CurrentTime = 0f; // David added
    bool m_bWait = false; // David added
    Vector3 m_PlanchetteStartingPosition = Vector3.zero; // David added
    int m_CurrentCharacter = -1; // David added
    BoardResponse m_CurrentResponse; // David added

	void Awake() => PopulateDictionaryWithStartingValues();

	void OnDestroy()
	{
        // David - If the controller exists then remove the method from the action
        // we check if it exists because it might not have been assigned in the first
        // place or maybe it got destroyed before this and we don't want to do this
        // on a null object
		if (m_OuijaPlayerInputController) m_OuijaPlayerInputController.AiResponseReceived -= StartDisplayingResponse;
	}

	void Start()
    {
		// David - If the controller exists then add the method to the action we check
        // if it exists because it might not have been assigned in the first place or
        // maybe it got destroyed before this and we don't want to do this on a null
        // object
		if (m_OuijaPlayerInputController) m_OuijaPlayerInputController.AiResponseReceived += StartDisplayingResponse;
    }

    void Update()
    {
        if (!DisplayingResponse) return;
        if (!m_BoardResponsePositions[m_CurrentResponse])
        {
            Debug.LogError($"No Transform for {m_CurrentResponse}");
            return;
        }
        m_CurrentTime += Time.deltaTime;
        if (!m_bWait)
        {
            float time01 = Mathf.Clamp01(m_CurrentTime / m_PlanchetteTravelTime);
            m_Planchette.transform.position = Vector3.Lerp(m_PlanchetteStartingPosition, m_BoardResponsePositions[m_CurrentResponse].position, time01);
            if (time01 < 1f) return; // David - If haven't finished moving then return
            m_bWait = true;
            m_CurrentTime = 0f;
            m_PlanchetteStartingPosition = m_BoardResponsePositions[m_CurrentResponse].position;
			int charsToDisplay = m_CurrentResponse switch
			{
				BoardResponse.Yes => 3, // David - Length of "yes"
				BoardResponse.No => 2, // David - Length of "no"
				BoardResponse.Goodbye => 7, // David - Length of "goodbye"
				_ => (m_CurrentCharacter < m_CurrentText.Length - 1 ? (m_CurrentText[m_CurrentCharacter + 1] == ' ' ? 2 : 1) : 1) // David - If next character is a space go to character after space, otherwise go to next character
			};
			m_CurrentCharacter += charsToDisplay;
            if (m_ResponseDisplayText) m_ResponseDisplayText.maxVisibleCharacters = m_CurrentCharacter;
            if (m_CurrentCharacter < m_CurrentText.Length) m_CurrentResponse = CharacterToResponse(m_CurrentText[m_CurrentCharacter]);
        }
        else if (m_CurrentTime >= m_PlancehtteWaitTime) // David - Waiting and wait time finished
        {
            m_bWait = false;
            m_CurrentTime = 0f;
			DisplayingResponse = m_CurrentCharacter < m_CurrentText.Length;
            if (m_CurrentResponse == BoardResponse.Goodbye)
            {
				m_OuijaPlayerInputController.gameObject.SetActive(false);
				m_Player.ChangeCharacter(m_FirstPersonCharacter);
            }
		}
    }

    // David - populate the dictionary which is easy to get the positions for a
    // response, with the values from the array which is not easy to get the
    // position of a response
    void PopulateDictionaryWithStartingValues()
    {
        foreach (BoardResponsePosition brp in m_BoardResponsePositionsOnBoard)
        {
            // David - If already have value in dictionary or transform is null continue
            if (m_BoardResponsePositions[brp.BoardResponse] || !brp.PositionOnBoard) continue;
            m_BoardResponsePositions[brp.BoardResponse] = brp.PositionOnBoard;
        }
    }

    // David - Start displaying AI response
    void StartDisplayingResponse(string response)
    {
        m_CurrentText = response.Trim().ToUpper();
        if (string.IsNullOrEmpty(m_CurrentText)) return;
        DisplayingResponse = true;
        m_bWait = false;
        m_PlanchetteStartingPosition = m_Planchette.transform.position;
		m_CurrentCharacter = 0;
		if (m_CurrentText == "YES") m_CurrentResponse = BoardResponse.Yes;
        else if (m_CurrentText == "NO") m_CurrentResponse = BoardResponse.No;
        else if (m_CurrentText == "GOODBYE" || m_CurrentText == "GOOD BYE") m_CurrentResponse = BoardResponse.Goodbye;
        else m_CurrentResponse = CharacterToResponse(m_CurrentText[0]);
        if (!m_ResponseDisplayText) return;
        m_ResponseDisplayText.text = m_CurrentText;
        m_ResponseDisplayText.maxVisibleCharacters = 0;
    }

    // David - Convert a char to a BoardResponse value
    BoardResponse CharacterToResponse(char character) => character switch
    {
        'A' =>  BoardResponse.A,    'B' => BoardResponse.B,
		'C' =>  BoardResponse.C,    'D' => BoardResponse.D,
		'E' =>  BoardResponse.E,    'F' => BoardResponse.F,
		'G' =>  BoardResponse.G,    'H' => BoardResponse.H,
		'I' =>  BoardResponse.I,    'J' => BoardResponse.J,
		'K' =>  BoardResponse.K,    'L' => BoardResponse.L,
		'M' =>  BoardResponse.M,    'N' => BoardResponse.N,
		'O' =>  BoardResponse.O,    'P' => BoardResponse.P,
		'Q' =>  BoardResponse.Q,    'R' => BoardResponse.R,
		'S' =>  BoardResponse.S,    'T' => BoardResponse.T,
		'U' =>  BoardResponse.U,    'V' => BoardResponse.V,
		'W' =>  BoardResponse.W,    'X' => BoardResponse.X,
		'Y' =>  BoardResponse.Y,    'Z' => BoardResponse.Z,
        '1' =>  BoardResponse.Num1, '2' => BoardResponse.Num2,
		'3' =>  BoardResponse.Num3, '4' => BoardResponse.Num4,
		'5' =>  BoardResponse.Num5, '6' => BoardResponse.Num6,
		'7' =>  BoardResponse.Num7, '8' => BoardResponse.Num8,
		'9' =>  BoardResponse.Num9, '0' => BoardResponse.Num0,
		_ => throw new System.ArgumentException($"'{character}' is not a valid character for the board")
    };
}
