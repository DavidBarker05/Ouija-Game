using System;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Whisper;
using Whisper.Utils;

namespace OurAssets.Scripts.Chat
{
    /// <summary>
    /// Drives the on-screen Ouija chat UI: a text input the player can type into,
    /// a button that toggles voice recording (overwriting the field with whisper.unity
    /// transcription when it stops), and a send button that hands the final message
    /// to the <see cref="OuijaAiOrchestrator"/>.
    /// </summary>
    public class OuijaPlayerInputController : MonoBehaviour
    {
        [Header("AI")]
        [SerializeField] private OuijaAiOrchestrator orchestrator;

        [Header("Text Input")]
        [SerializeField] private TMP_InputField messageInputField;
        [Tooltip("Maximum number of characters the player can type or transcribe into the message box. 0 means unlimited.")]
        [Min(0)] [SerializeField] private int maxMessageCharacters = 200;
        [SerializeField] private Button sendButton;
        [Tooltip("Clear the input field after a message is successfully sent.")]
        [SerializeField] private bool clearInputAfterSend = true;

        [Header("Voice Input")]
        [SerializeField] private WhisperManager whisperManager;
        [SerializeField] private MicrophoneRecord microphoneRecord;
        [SerializeField] private Button recordButton;
        [Tooltip("Maximum length of a single voice recording in seconds. The recording will stop automatically once this elapses.")]
        [Min(1)] [SerializeField] private int maxRecordingSeconds = 30;

        [Header("Voice Input - Visuals (Optional)")]
        [SerializeField] private TMP_Text recordButtonLabel;
        [SerializeField] private string idleRecordLabel = "Record";
        [SerializeField] private string activeRecordLabel = "Stop";
        [SerializeField] private Image recordButtonIndicator;
        [SerializeField] private Color idleIndicatorColor = Color.white;
        [SerializeField] private Color recordingIndicatorColor = Color.red;
        [SerializeField] private TMP_Text statusLabel;
        [Tooltip("Optional countdown label that shows the remaining recording time (in whole seconds).")]
        [SerializeField] private TMP_Text recordingCountdownLabel;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        /// <summary>Raised on the main thread once the orchestrator returns an AI reply.</summary>
        public event Action<string> AiResponseReceived;

        /// <summary>Raised on the main thread when a send fails.</summary>
        public event Action<Exception> SendFailed;

        private CancellationTokenSource _sendCts;
        private bool _isSending;
        private bool _isTranscribing;
        private float _recordingEndRealtime;

        private void Awake()
        {
            ApplyCharacterLimit();
            ApplyRecordingLimit();
        }

        private void OnEnable()
        {
            if (sendButton != null)
            {
                sendButton.onClick.AddListener(OnSendClicked);
            }

            if (recordButton != null)
            {
                recordButton.onClick.AddListener(OnRecordClicked);
            }

            if (messageInputField != null)
            {
                messageInputField.onValueChanged.AddListener(OnInputChanged);
                messageInputField.onSubmit.AddListener(OnInputSubmitted);
            }

            if (microphoneRecord != null)
            {
                microphoneRecord.OnRecordStop += OnMicrophoneRecordStop;
            }

            UpdateRecordVisuals(false);
            UpdateCountdownLabel(maxRecordingSeconds);
            RefreshSendButtonInteractable();
            RefreshRecordButtonInteractable();
            SetStatus(string.Empty);
        }

        private void OnDisable()
        {
            if (sendButton != null)
            {
                sendButton.onClick.RemoveListener(OnSendClicked);
            }

            if (recordButton != null)
            {
                recordButton.onClick.RemoveListener(OnRecordClicked);
            }

            if (messageInputField != null)
            {
                messageInputField.onValueChanged.RemoveListener(OnInputChanged);
                messageInputField.onSubmit.RemoveListener(OnInputSubmitted);
            }

            if (microphoneRecord != null)
            {
                microphoneRecord.OnRecordStop -= OnMicrophoneRecordStop;
                if (microphoneRecord.IsRecording)
                {
                    microphoneRecord.StopRecord();
                }
            }

            CancelOngoingSend();
        }

        private void OnValidate()
        {
            // Keep the linked components in sync with inspector edits during editor play.
            ApplyCharacterLimit();
            ApplyRecordingLimit();
        }

        private void Update()
        {
            if (microphoneRecord == null || !microphoneRecord.IsRecording)
            {
                return;
            }

            float remaining = Mathf.Max(0f, _recordingEndRealtime - Time.realtimeSinceStartup);
            UpdateCountdownLabel(Mathf.CeilToInt(remaining));

            // MicrophoneRecord auto-stops when the underlying clip wraps (loop = false), but
            // we also enforce the cap explicitly so the serialized value always wins even if
            // the user changed maxLengthSec elsewhere or the clip length differs.
            if (remaining <= 0f)
            {
                microphoneRecord.StopRecord();
            }
        }

        private void ApplyCharacterLimit()
        {
            if (messageInputField == null)
            {
                return;
            }

            messageInputField.characterLimit = Mathf.Max(0, maxMessageCharacters);
        }

        private void ApplyRecordingLimit()
        {
            if (microphoneRecord == null)
            {
                return;
            }

            microphoneRecord.maxLengthSec = Mathf.Max(1, maxRecordingSeconds);
            microphoneRecord.loop = false;
        }

        private void OnInputChanged(string _)
        {
            RefreshSendButtonInteractable();
        }

        private void OnInputSubmitted(string _)
        {
            if (CanSend())
            {
                OnSendClicked();
            }
        }

        private void OnRecordClicked()
        {
            if (microphoneRecord == null)
            {
                Debug.LogWarning($"{nameof(OuijaPlayerInputController)}: no MicrophoneRecord assigned.");
                return;
            }

            if (_isTranscribing)
            {
                return;
            }

            if (microphoneRecord.IsRecording)
            {
                microphoneRecord.StopRecord();
                return;
            }

            ApplyRecordingLimit();
            microphoneRecord.StartRecord();

            if (!microphoneRecord.IsRecording)
            {
                SetStatus("Couldn't start microphone.");
                return;
            }

            _recordingEndRealtime = Time.realtimeSinceStartup + maxRecordingSeconds;
            UpdateRecordVisuals(true);
            UpdateCountdownLabel(maxRecordingSeconds);
            SetStatus("Recording...");
            RefreshSendButtonInteractable();
        }

        private async void OnMicrophoneRecordStop(AudioChunk recordedAudio)
        {
            UpdateRecordVisuals(false);
            UpdateCountdownLabel(maxRecordingSeconds);

            if (recordedAudio.Data == null || recordedAudio.Data.Length == 0)
            {
                SetStatus("No audio captured.");
                RefreshSendButtonInteractable();
                return;
            }

            if (whisperManager == null)
            {
                Debug.LogWarning($"{nameof(OuijaPlayerInputController)}: no WhisperManager assigned; ignoring audio.");
                SetStatus("Speech-to-text unavailable.");
                RefreshSendButtonInteractable();
                return;
            }

            _isTranscribing = true;
            SetStatus("Transcribing...");
            RefreshRecordButtonInteractable();
            RefreshSendButtonInteractable();

            try
            {
                WhisperResult result = await whisperManager.GetTextAsync(
                    recordedAudio.Data,
                    recordedAudio.Frequency,
                    recordedAudio.Channels);

                string text = result?.Result?.Trim() ?? string.Empty;

                if (string.IsNullOrEmpty(text))
                {
                    SetStatus("Didn't catch that.");
                }
                else
                {
                    SetInputFieldText(text);
                    SetStatus(string.Empty);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"Whisper transcription failed: {exception.Message}");
                SetStatus("Transcription failed.");
            }
            finally
            {
                _isTranscribing = false;
                RefreshRecordButtonInteractable();
                RefreshSendButtonInteractable();
            }
        }

        private void OnSendClicked()
        {
            if (!CanSend())
            {
                return;
            }

            string message = (messageInputField != null ? messageInputField.text : string.Empty).Trim();
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            _ = SendMessageAsync(message);
        }

        private async Task SendMessageAsync(string message)
        {
            if (orchestrator == null)
            {
                Debug.LogError($"{nameof(OuijaPlayerInputController)}: no OuijaAiOrchestrator assigned.");
                return;
            }

            CancelOngoingSend();
            _sendCts = new CancellationTokenSource();
            CancellationToken token = _sendCts.Token;

            _isSending = true;
            SetStatus("Sending...");
            RefreshSendButtonInteractable();
            RefreshRecordButtonInteractable();

            try
            {
                string aiText = await orchestrator.SendPlayerMessageToOuijaAsync(message, token);

                if (clearInputAfterSend && messageInputField != null)
                {
                    SetInputFieldText(string.Empty);
                }

                SetStatus(string.Empty);

                if (enableDebugLogs)
                {
                    Debug.Log($"Ouija reply: {aiText}");
                }

                AiResponseReceived?.Invoke(aiText);
            }
            catch (OperationCanceledException)
            {
                SetStatus("Cancelled.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Send to Ouija failed: {exception.Message}");
                SetStatus("Send failed.");
                SendFailed?.Invoke(exception);
            }
            finally
            {
                _isSending = false;
                RefreshSendButtonInteractable();
                RefreshRecordButtonInteractable();
            }
        }

        private bool CanSend()
        {
            if (_isSending || _isTranscribing)
            {
                return false;
            }

            if (microphoneRecord != null && microphoneRecord.IsRecording)
            {
                return false;
            }

            if (messageInputField == null)
            {
                return false;
            }

            return !string.IsNullOrWhiteSpace(messageInputField.text);
        }

        private void RefreshSendButtonInteractable()
        {
            if (sendButton != null)
            {
                sendButton.interactable = CanSend();
            }
        }

        private void RefreshRecordButtonInteractable()
        {
            if (recordButton == null)
            {
                return;
            }

            // Allow tapping the record button to stop an active recording even while sending,
            // but don't allow starting a new one mid-send or mid-transcription.
            bool isRecording = microphoneRecord != null && microphoneRecord.IsRecording;
            recordButton.interactable = isRecording || (!_isSending && !_isTranscribing);
        }

        private void UpdateRecordVisuals(bool isRecording)
        {
            if (recordButtonLabel != null)
            {
                recordButtonLabel.text = isRecording ? activeRecordLabel : idleRecordLabel;
            }

            if (recordButtonIndicator != null)
            {
                recordButtonIndicator.color = isRecording ? recordingIndicatorColor : idleIndicatorColor;
            }
        }

        private void UpdateCountdownLabel(int seconds)
        {
            if (recordingCountdownLabel == null)
            {
                return;
            }

            recordingCountdownLabel.text = Mathf.Max(0, seconds).ToString();
        }

        private void SetStatus(string message)
        {
            if (statusLabel != null)
            {
                statusLabel.text = message ?? string.Empty;
            }
        }

        private void SetInputFieldText(string text)
        {
            if (messageInputField == null)
            {
                return;
            }

            // Respect the configured character limit when whisper returns a long transcription.
            string clamped = text ?? string.Empty;
            if (maxMessageCharacters > 0 && clamped.Length > maxMessageCharacters)
            {
                clamped = clamped.Substring(0, maxMessageCharacters);
            }

            messageInputField.text = clamped;
            messageInputField.caretPosition = clamped.Length;
        }

        private void CancelOngoingSend()
        {
            if (_sendCts == null)
            {
                return;
            }

            try
            {
                if (!_sendCts.IsCancellationRequested)
                {
                    _sendCts.Cancel();
                }
            }
            catch (ObjectDisposedException)
            {
                // already disposed - nothing to do
            }
            finally
            {
                _sendCts.Dispose();
                _sendCts = null;
            }
        }
    }
}
