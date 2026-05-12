using UnityEngine;

namespace OurAssets.Scripts.Chat
{
    public class OuijaGateResponseResolver : MonoBehaviour, IOuijaGateResponseResolver
    {
        public string GetGatedResponseText(string responseId) => responseId switch
        {
            "spirit_name" => SpiritName,
            _ => string.Empty
        };

        string SpiritName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SpiritNameManager.Instance.SpiritName)) SpiritNameManager.Instance.StartNewGame();
				return SpiritNameManager.Instance.SpiritName;
			}
        }
    }
}
