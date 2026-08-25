using UnityEngine;
using IVH.Core.IntelligentVirtualAgent;
using IVH.Core.ServiceConnector.Gemini.Realtime;

/// Safe project-level bridge to the IVA SDK.
/// Other project scripts should use this class instead of directly
/// accessing GeminiRealtimeWrapper.
[DisallowMultipleComponent]
public class GeminiAgentBridge : MonoBehaviour
{
    [Header("SDK References")]
    [SerializeField] private GeminiLiveAgent liveAgent;
    [SerializeField] private GeminiRealtimeWrapper realtimeWrapper;

    [Tooltip("The AudioSource used by GeminiLiveAgent for speech playback.")]
    [SerializeField] private AudioSource agentAudioSource;

    public bool IsReady =>
        liveAgent != null &&
        realtimeWrapper != null &&
        liveAgent.IsSessionReady() &&
        realtimeWrapper.IsConnected;

    public bool IsSpeaking =>
        agentAudioSource != null && agentAudioSource.isPlaying;

    private void Reset()
    {
        AutoAssignReferences();
    }

    private void Awake()
    {
        AutoAssignReferences();

        if (liveAgent == null)
        {
            Debug.LogError(
                "GeminiAgentBridge: GeminiLiveAgent is not assigned.",
                this);
        }

        if (realtimeWrapper == null)
        {
            Debug.LogError(
                "GeminiAgentBridge: GeminiRealtimeWrapper is not assigned.",
                this);
        }

        if (agentAudioSource == null)
        {
            Debug.LogWarning(
                "GeminiAgentBridge: Agent AudioSource is not assigned. " +
                "Speech-overlap protection will be less reliable.",
                this);
        }
    }

    private void AutoAssignReferences()
    {
        if (liveAgent == null)
        {
            liveAgent = GetComponent<GeminiLiveAgent>();
        }

        if (realtimeWrapper == null)
        {
            realtimeWrapper = GetComponent<GeminiRealtimeWrapper>();
        }
    }

    /// Sends one application instruction to Gemini.
    /// This method refuses to send while the IVA is still speaking.
    public bool TrySendInstruction(string instruction)
    {
        if (string.IsNullOrWhiteSpace(instruction))
        {
            Debug.LogWarning(
                "GeminiAgentBridge: Empty instruction was ignored.",
                this);
            return false;
        }

        if (!IsReady)
        {
            Debug.LogWarning(
                "GeminiAgentBridge: Gemini session is not ready.",
                this);
            return false;
        }

        if (IsSpeaking)
        {
            Debug.LogWarning(
                "GeminiAgentBridge: IVA is still speaking. " +
                "Instruction was not sent.",
                this);
            return false;
        }

        Debug.Log(
            $"<color=yellow>UNITY ¡ú GEMINI</color>\n{instruction}",
            this);

        realtimeWrapper.SendTextMessage(instruction);
        return true;
    }
}