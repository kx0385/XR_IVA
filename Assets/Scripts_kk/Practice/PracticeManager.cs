using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Runs the short controller familiarisation practice.
/// It uses real Unity selection events, not participant speech.
/// </summary>
[DisallowMultipleComponent]
public class PracticeManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private XRBriefingManager briefingManager;
    [SerializeField] private PracticeTarget targetPrefab;
    [SerializeField] private Transform[] targetSpawnPoints;

    [Tooltip(
        "Optional visuals that should only be visible during practice. " +
        "Do not place this PracticeManager component inside that object.")]
    [SerializeField] private GameObject practiceVisualRoot;

    [Header("Practice Settings")]
    [Min(1)]
    [SerializeField] private int requiredSelections = 3;

    [Min(0f)]
    [SerializeField] private float delayBetweenTargets = 0.4f;

    [Header("Optional UI")]
    [SerializeField] private TMP_Text progressLabel;

    private PracticeTarget activeTarget;
    private int completedSelections;
    private bool practiceRunning;
    private Coroutine spawnCoroutine;

    private void Awake()
    {
        if (practiceVisualRoot != null)
        {
            practiceVisualRoot.SetActive(false);
        }

        UpdateProgressLabel();
    }

    public void SetBriefingManager(
        XRBriefingManager manager)
    {
        if (briefingManager == null)
        {
            briefingManager = manager;
        }
    }

    public void BeginPractice()
    {
        if (practiceRunning)
        {
            Debug.LogWarning(
                "PracticeManager: Practice is already running.",
                this);
            return;
        }

        if (targetPrefab == null)
        {
            Debug.LogError(
                "PracticeManager: PracticeTarget prefab is missing.",
                this);
            return;
        }

        if (targetSpawnPoints == null ||
            targetSpawnPoints.Length == 0)
        {
            Debug.LogError(
                "PracticeManager: No spawn points are assigned.",
                this);
            return;
        }

        completedSelections = 0;
        practiceRunning = true;

        if (practiceVisualRoot != null)
        {
            practiceVisualRoot.SetActive(true);
        }

        UpdateProgressLabel();
        SpawnNextTarget();
    }

    public void RegisterTargetSelected(
        PracticeTarget selectedTarget)
    {
        if (!practiceRunning ||
            selectedTarget == null ||
            selectedTarget != activeTarget)
        {
            return;
        }

        completedSelections++;

        Destroy(activeTarget.gameObject);
        activeTarget = null;

        UpdateProgressLabel();

        if (completedSelections >= requiredSelections)
        {
            CompletePractice();
            return;
        }

        spawnCoroutine = StartCoroutine(
            SpawnNextTargetAfterDelay());
    }

    public void CancelPractice()
    {
        practiceRunning = false;

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        if (activeTarget != null)
        {
            Destroy(activeTarget.gameObject);
            activeTarget = null;
        }

        if (practiceVisualRoot != null)
        {
            practiceVisualRoot.SetActive(false);
        }
    }

    private IEnumerator SpawnNextTargetAfterDelay()
    {
        yield return new WaitForSeconds(
            delayBetweenTargets);

        spawnCoroutine = null;

        if (practiceRunning)
        {
            SpawnNextTarget();
        }
    }

    private void SpawnNextTarget()
    {
        int spawnIndex =
            completedSelections % targetSpawnPoints.Length;

        Transform spawnPoint =
            targetSpawnPoints[spawnIndex];

        if (spawnPoint == null)
        {
            Debug.LogError(
                $"PracticeManager: Spawn point {spawnIndex} is null.",
                this);
            CancelPractice();
            return;
        }

        activeTarget = Instantiate(
            targetPrefab,
            spawnPoint.position,
            spawnPoint.rotation);

        activeTarget.Initialize(this);
    }

    private void CompletePractice()
    {
        practiceRunning = false;

        if (practiceVisualRoot != null)
        {
            practiceVisualRoot.SetActive(false);
        }

        if (progressLabel != null)
        {
            progressLabel.text = "Practice complete";
        }

        Debug.Log(
            "PracticeManager: Required target selections completed.",
            this);

        if (briefingManager == null)
        {
            Debug.LogError(
                "PracticeManager: XRBriefingManager is missing.",
                this);
            return;
        }

        briefingManager.OnPracticeCompleted();
    }

    private void UpdateProgressLabel()
    {
        if (progressLabel != null)
        {
            progressLabel.text =
                $"Practice: {completedSelections}/{requiredSelections}";
        }
    }

    private void OnDisable()
    {
        CancelPractice();
    }
}