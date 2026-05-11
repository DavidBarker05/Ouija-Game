using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CryptexRingButton : MonoBehaviour
{
    [SerializeField]
    Cryptex m_Cryptex;
    [SerializeField, Min(0)]
    int m_RingIndex = 0;
    [SerializeField]
    RingRotation m_RingRotation = RingRotation.PrevLetter;

    public void RotateCryptex() => m_Cryptex.RotateRing(m_RingIndex, m_RingRotation);
}
