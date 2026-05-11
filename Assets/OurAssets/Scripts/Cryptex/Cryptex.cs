using UnityEngine;

[System.Serializable]
public struct CrypexRing
{
    public Transform Transform;
    public char Letter;
}

[System.Serializable]
public enum RingNextLetterRotation
{
    PosX,
    NegX,
    PosY,
    NegY,
    PosZ,
    NegZ
}

[System.Serializable]
public enum RingRotation
{
    PrevLetter,
    NextLetter
}

public class Cryptex : MonoBehaviour
{
    const float LETTER_ROTATION = 360f / 26f; // 26 letters per 360 degrees

    [SerializeField]
    CrypexRing[] m_CryptexRings;
    [SerializeField]
    RingNextLetterRotation m_RingUpRotationAxis = RingNextLetterRotation.PosX;

    public void RotateRing(int index, RingRotation rotation)
    {
        if (m_CryptexRings == null || index >= m_CryptexRings.Length) throw new System.ArgumentOutOfRangeException("Index out of range");
        int rot = rotation == RingRotation.PrevLetter ? -1 : (rotation == RingRotation.NextLetter ? 1 : 0);
		float angle = LETTER_ROTATION * rot;
        Vector3 eulerRotation = m_RingUpRotationAxis switch
        {
			RingNextLetterRotation.PosX => new Vector3(angle, 0f, 0f),
			RingNextLetterRotation.NegX => new Vector3(-angle, 0f, 0f),
			RingNextLetterRotation.PosY => new Vector3(0f, angle, 0f),
			RingNextLetterRotation.NegY => new Vector3(0f, -angle, 0f),
			RingNextLetterRotation.PosZ => new Vector3(0f, 0f, angle),
			RingNextLetterRotation.NegZ => new Vector3(0f, 0f, -angle),
			_ => throw new System.NotSupportedException($"{m_RingUpRotationAxis} is not supported")
        };
        m_CryptexRings[index].Transform.localEulerAngles += eulerRotation;
        m_CryptexRings[index].Letter = (char)Mathf.Clamp(char.ToUpper(m_CryptexRings[index].Letter) + rot, 'A', 'Z');
	}
}
