using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class SimpleVRFittsTask : MonoBehaviour
{
    [Header("UI References")]

    [SerializeField]
    private GameObject fittsPanel;

    [SerializeField]
    private RectTransform targetArea;

    [SerializeField]
    private Button targetButton;

    [SerializeField]
    private TMP_Text progressText;


    [Header("Experiment Settings")]

    [Tooltip("Number of targets in the formal pointing task.")]
    [SerializeField]
    private int numberOfTargets = 10;

    [Tooltip(
        "Target sizes used to simulate different pointing difficulties.")]
    [SerializeField]
    private float[] targetSizes =
    {
        90f,
        70f,
        50f
    };

    [Tooltip(
        "Minimum space between the target and the edge of TargetArea.")]
    [SerializeField]
    private float edgePadding = 20f;

    [Tooltip(
        "Preferred minimum distance between consecutive targets.")]
    [SerializeField]
    private float minimumMoveDistance = 150f;


    [Header("Completion")]

    [Tooltip(
        "Called after the participant selects all formal targets.")]
    [SerializeField]
    private UnityEvent onExperimentCompleted;


    private RectTransform targetRect;

    private int currentTargetIndex;

    private bool experimentRunning;

    private Vector2 previousPosition;


    private void Awake()
    {
        // ---------------------------------------------
        // Get TargetButton RectTransform
        // ---------------------------------------------

        if (targetButton != null)
        {
            targetRect =
                targetButton.GetComponent<RectTransform>();

            targetButton.onClick.AddListener(
                OnTargetClicked);
        }
        else
        {
            Debug.LogError(
                "SimpleVRFittsTask: TargetButton is not assigned.",
                this);
        }


        // ---------------------------------------------
        // Hide experiment UI at startup
        // ---------------------------------------------

        if (fittsPanel != null)
        {
            fittsPanel.SetActive(false);
        }
    }


    /// <summary>
    /// Starts the formal VR pointing task.
    /// Called by XRBriefingManager.
    /// </summary>
    public void BeginExperiment()
    {
        // ---------------------------------------------
        // Validate references
        // ---------------------------------------------

        if (fittsPanel == null)
        {
            Debug.LogError(
                "SimpleVRFittsTask: FittsPanel is not assigned.",
                this);

            return;
        }

        if (targetArea == null)
        {
            Debug.LogError(
                "SimpleVRFittsTask: TargetArea is not assigned.",
                this);

            return;
        }

        if (targetButton == null)
        {
            Debug.LogError(
                "SimpleVRFittsTask: TargetButton is not assigned.",
                this);

            return;
        }

        if (progressText == null)
        {
            Debug.LogError(
                "SimpleVRFittsTask: ProgressText is not assigned.",
                this);

            return;
        }


        if (numberOfTargets < 1)
        {
            Debug.LogError(
                "SimpleVRFittsTask: Number of targets must be at least 1.",
                this);

            return;
        }


        Debug.Log(
            "<color=green>FORMAL POINTING TASK STARTED</color>",
            this);


        // ---------------------------------------------
        // Show experiment panel
        // ---------------------------------------------

        fittsPanel.SetActive(true);


        // ---------------------------------------------
        // Reset experiment state
        // ---------------------------------------------

        currentTargetIndex = 0;

        experimentRunning = true;

        previousPosition = Vector2.zero;


        // Show target again in case the experiment
        // has already been completed once.
        targetButton.gameObject.SetActive(true);


        // Make sure RectTransform sizes have been updated.
        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            targetArea);


        // ---------------------------------------------
        // Create first target
        // ---------------------------------------------

        MoveTarget();

        UpdateProgress();
    }


    /// <summary>
    /// Called automatically when the participant
    /// selects the TargetButton.
    /// </summary>
    private void OnTargetClicked()
    {
        if (!experimentRunning)
        {
            return;
        }


        Debug.Log(
            $"Formal target selected: " +
            $"{currentTargetIndex + 1}/{numberOfTargets}",
            this);


        // Current target completed
        currentTargetIndex++;


        // ---------------------------------------------
        // Check completion
        // ---------------------------------------------

        if (currentTargetIndex >= numberOfTargets)
        {
            FinishExperiment();

            return;
        }


        // ---------------------------------------------
        // Show next target
        // ---------------------------------------------

        MoveTarget();

        UpdateProgress();
    }


    /// <summary>
    /// Changes target size and moves it to a
    /// new random position inside TargetArea.
    /// </summary>
    private void MoveTarget()
    {
        if (targetRect == null ||
            targetArea == null)
        {
            return;
        }


        // =================================================
        // 1. Select target size
        // =================================================

        float targetSize = 70f;


        if (targetSizes != null &&
            targetSizes.Length > 0)
        {
            int sizeIndex =
                currentTargetIndex %
                targetSizes.Length;

            targetSize =
                targetSizes[sizeIndex];
        }


        targetRect.sizeDelta =
            new Vector2(
                targetSize,
                targetSize);


        // =================================================
        // 2. Calculate TargetArea bounds
        // =================================================

        Rect area =
            targetArea.rect;


        float halfTarget =
            targetSize / 2f;


        float minX =
            area.xMin +
            edgePadding +
            halfTarget;


        float maxX =
            area.xMax -
            edgePadding -
            halfTarget;


        float minY =
            area.yMin +
            edgePadding +
            halfTarget;


        float maxY =
            area.yMax -
            edgePadding -
            halfTarget;


        // TargetArea is too small
        if (minX >= maxX ||
            minY >= maxY)
        {
            Debug.LogWarning(
                "SimpleVRFittsTask: TargetArea is too small " +
                "for the current target size.",
                this);


            targetRect.anchoredPosition =
                Vector2.zero;


            previousPosition =
                Vector2.zero;


            return;
        }


        // =================================================
        // 3. Generate a new random target position
        // =================================================

        Vector2 newPosition =
            Vector2.zero;


        for (int attempt = 0;
             attempt < 30;
             attempt++)
        {
            float x =
                Random.Range(
                    minX,
                    maxX);


            float y =
                Random.Range(
                    minY,
                    maxY);


            newPosition =
                new Vector2(
                    x,
                    y);


            // First target can appear anywhere.
            if (currentTargetIndex == 0)
            {
                break;
            }


            float distance =
                Vector2.Distance(
                    newPosition,
                    previousPosition);


            // Prefer a position that is not too close
            // to the previous target.
            if (distance >= minimumMoveDistance)
            {
                break;
            }
        }


        // =================================================
        // 4. Apply position
        // =================================================

        targetRect.anchoredPosition =
            newPosition;


        previousPosition =
            newPosition;
    }


    /// <summary>
    /// Updates Target 1 / 10 text.
    /// </summary>
    private void UpdateProgress()
    {
        if (progressText == null)
        {
            return;
        }


        progressText.text =
            $"Target {currentTargetIndex + 1} / {numberOfTargets}";
    }


    /// <summary>
    /// Called after the final target is selected.
    /// </summary>
    private void FinishExperiment()
    {
        experimentRunning = false;


        Debug.Log(
            "<color=green>FORMAL POINTING TASK COMPLETED</color>",
            this);


        // ---------------------------------------------
        // Hide target
        // ---------------------------------------------

        if (targetButton != null)
        {
            targetButton.gameObject.SetActive(false);
        }


        // ---------------------------------------------
        // Show completion message
        // ---------------------------------------------

        if (progressText != null)
        {
            progressText.text =
                "Experiment Completed\n\n" +
                "Thank you.\n" +
                "Please remove the headset and " +
                "complete the questionnaire.";
        }


        // ---------------------------------------------
        // Notify XRBriefingManager
        // ---------------------------------------------

        onExperimentCompleted?.Invoke();
    }


    /// <summary>
    /// Can be used to hide the whole Fitts panel.
    /// </summary>
    public void HideExperiment()
    {
        experimentRunning = false;


        if (fittsPanel != null)
        {
            fittsPanel.SetActive(false);
        }
    }


    /// <summary>
    /// Optional restart function for testing.
    /// </summary>
    public void RestartExperiment()
    {
        BeginExperiment();
    }


    private void OnDestroy()
    {
        if (targetButton != null)
        {
            targetButton.onClick.RemoveListener(
                OnTargetClicked);
        }
    }
}