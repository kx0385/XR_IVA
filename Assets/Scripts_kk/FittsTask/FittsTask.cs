using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/*
 * VR/AR Interaction Accuracy Research Prototype
 * Based on a Fitts' Law target-selection experiment.
 *
 * Purpose:
 * - Desktop simulation of VR/AR controller/hand target selection.
 * - Measures target acquisition time, error/miss rate, target distance, target width,
 *   index of difficulty, and throughput.
 * - Designed for quick classroom demonstration and lab-report data collection.
 */

namespace FittsTask
{
    public enum CursorCenter
    {
        Center,
        TopLeft
    }

    public enum TaskType
    {
        OneDimensional,
        TwoDimensional
    }

    public enum SelectionMethod
    {
        MouseButton,
        DwellTime
    }

    [Serializable]
    public class TrialResult
    {
        public int trialNumber;
        public int repetition;
        public int targetIndex;
        public float targetWidth;
        public float targetDistance;
        public float movementTime;
        public bool success;
        public int missesBeforeHit;
        public float endpointError;
        public float indexOfDifficulty;
        public float throughput;
    }

    public class FittsTask : MonoBehaviour
    {
        [Header("General Settings")]
        public Canvas screenCanvas;
        public GameObject backgroundPanel;
        public Sprite targetDiscSprite;
        public Sprite targetRectSprite;
        public Sprite cursorSprite;
        public int cursorSize = 18;
        public CursorCenter cursorCenter = CursorCenter.Center;

        [Header("VR/AR Research Presentation")]
        public string researchTitle = "VR/AR Interaction Accuracy Research";
        public string researchSubtitle = "Desktop simulation of target selection performance for VR/AR interfaces";
        public bool showResearchHUD = true;

        [Header("Experimental Settings")]
        public int SubjectID = 1;
        public string Condition = "Desktop VR/AR simulation";
        public string Task = "2D target selection accuracy test";
        public string Group = "Student prototype";

        [Header("Fitts Task Settings")]
        public TaskType fittsTaskType = TaskType.TwoDimensional;
        public SelectionMethod taskSelectionMethod = SelectionMethod.MouseButton;
        public int dwellTime = 0;

        [SerializeField] private int numberOfTrials = 15;

        public int NumberOfTrials
        {
            get
            {
                numberOfTrials = Mathf.Max(7, Mathf.Min(53, numberOfTrials | 1));
                return numberOfTrials;
            }
            set
            {
                numberOfTrials = Mathf.Max(7, Mathf.Min(53, value | 1));
            }
        }

        public int numberOfRepetitions = 3;
        public int[] amplitudes = new int[] { 180, 260, 340 };
        public int[] widths = new int[] { 28, 42, 64 };

        public bool randomizeTargetConditions = true;
        public bool renderCursorOnCanvas = true;
        public bool showAmplitudeTrials = true;
        public bool audioFeedback = true;
        public bool mouseOverHighlight = true;

        [Header("Colors")]
        public Color backgroundColor = new Color(0.02f, 0.04f, 0.09f, 0.88f);
        public Color foregroundColor = new Color(0.45f, 0.55f, 0.65f, 0.75f);
        public Color targetColor = new Color(1.0f, 0.12f, 0.09f, 1.0f);
        public Color buttonDownColor = new Color(0.1f, 0.7f, 1.0f, 1.0f);
        public Color mouseOverColor = new Color(1.0f, 0.9f, 0.2f, 1.0f);
        public Color cursorColor = Color.white;

        [Header("Logging")]
        public bool saveEvents = true;
        public string eventsLogPath = @"Results/FittsLogging/Events";
        public bool saveMovements = false;
        public string movementLogPath = @"Results/FittsLogging/Movements";
        public bool saveEvaluation = true;
        public string evalationLogPath = @"Results/FittsLogging/Evaluation";

        private readonly List<GameObject> targetList = new List<GameObject>();
        private readonly List<Vector2> targetCenters = new List<Vector2>();
        private readonly List<TrialResult> results = new List<TrialResult>();

        private GameObject cursor;
        private Text titleText;
        private Text hudText;
        private Text instructionText;
        private Text resultText;

        private RectTransform canvasRect;
        private RectTransform currentTargetRect;
        private AudioSource audioBeep;

        private int currentTargetIndex;
        private int currentRepetitionIndex;
        private int currentWidth;
        private int currentAmplitude;
        private int successfulSelections;
        private int missedSelections;
        private int missesThisTrial;
        private bool experimentFinished;

        private Vector2 previousTargetCenter;
        private float trialStartTime;
        private string lastExportFolder = "";

        private StreamWriter eventLogWriter;
        private StreamWriter evaluationLogWriter;

        private void OnValidate()
        {
            NumberOfTrials = numberOfTrials;
            if (numberOfRepetitions < 1) numberOfRepetitions = 1;
        }

        private void Start()
        {
            Application.targetFrameRate = 90;
            audioBeep = GetComponent<AudioSource>();

            EnsureSceneObjects();
            InitializeLogging();
            BuildInterface();
            StartExperiment();
        }

        private void Update()
        {
            if (experimentFinished)
            {
                if (Input.GetKeyDown(KeyCode.S))
                {
                    StartExperiment();
                }
                return;
            }

            UpdateCursor();

            if (Input.GetKeyDown(KeyCode.S))
            {
                StartExperiment();
                return;
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                renderCursorOnCanvas = !renderCursorOnCanvas;
                if (cursor != null) cursor.SetActive(renderCursorOnCanvas);
                Cursor.visible = !renderCursorOnCanvas;
            }

            if (Input.GetMouseButtonDown(0))
            {
                HandleMouseClick();
            }

            if (saveMovements && eventLogWriter != null)
            {
                Vector2 mouse = GetMouseLocalPosition();
                eventLogWriter.WriteLine($"{Timestamp()};MOVE;{SubjectID};{Condition};{Task};{currentRepetitionIndex + 1};{currentTargetIndex + 1};{mouse.x:F2};{mouse.y:F2};");
            }

            UpdateHUD();
        }

        private void EnsureSceneObjects()
        {
            if (screenCanvas == null)
            {
                GameObject canvasObject = new GameObject("InteractionCanvas");
                screenCanvas = canvasObject.AddComponent<Canvas>();
                screenCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            canvasRect = screenCanvas.GetComponent<RectTransform>();

            if (backgroundPanel == null)
            {
                GameObject panel = new GameObject("BackgroundPanel");
                panel.transform.SetParent(screenCanvas.transform, false);
                Image image = panel.AddComponent<Image>();
                image.color = backgroundColor;
                RectTransform rect = panel.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.08f, 0.08f);
                rect.anchorMax = new Vector2(0.92f, 0.92f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                backgroundPanel = panel;
            }

            Image backgroundImage = backgroundPanel.GetComponent<Image>();
            if (backgroundImage == null) backgroundImage = backgroundPanel.AddComponent<Image>();
            backgroundImage.color = backgroundColor;
        }

        private void BuildInterface()
        {
            ClearChildrenExceptBackground();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            titleText = CreateText("Title", font, 28, TextAnchor.UpperLeft, new Vector2(30, -24), new Vector2(920, 90));
            titleText.text = $"<b>{researchTitle}</b>\n<size=17>{researchSubtitle}</size>";

            hudText = CreateText("HUD", font, 18, TextAnchor.UpperRight, new Vector2(-30, -24), new Vector2(680, 190));

            instructionText = CreateText("Instructions", font, 19, TextAnchor.LowerLeft, new Vector2(30, 24), new Vector2(1050, 90));
            instructionText.text = "Instruction: click the highlighted red target as quickly and accurately as possible.  S = restart, C = cursor toggle.";

            resultText = CreateText("FinalResults", font, 28, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(1200, 520));
            resultText.enabled = false;

            CreateCursor();
        }

        private void ClearChildrenExceptBackground()
        {
            List<Transform> toDelete = new List<Transform>();
            foreach (Transform child in screenCanvas.transform)
            {
                if (backgroundPanel != null && child.gameObject == backgroundPanel) continue;
                toDelete.Add(child);
            }

            foreach (Transform child in toDelete)
            {
                Destroy(child.gameObject);
            }

            targetList.Clear();
            targetCenters.Clear();
        }

        private Text CreateText(string name, Font font, int size, TextAnchor alignment, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(screenCanvas.transform, false);

            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.supportRichText = true;

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = sizeDelta;

            if (alignment == TextAnchor.UpperLeft || alignment == TextAnchor.LowerLeft)
            {
                rect.anchorMin = new Vector2(0, alignment == TextAnchor.UpperLeft ? 1 : 0);
                rect.anchorMax = rect.anchorMin;
                rect.pivot = new Vector2(0, alignment == TextAnchor.UpperLeft ? 1 : 0);
            }
            else if (alignment == TextAnchor.UpperRight)
            {
                rect.anchorMin = new Vector2(1, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.pivot = new Vector2(1, 1);
            }

            rect.anchoredPosition = anchoredPosition;
            return text;
        }

        private void CreateCursor()
        {
            cursor = new GameObject("VR_AR_Simulated_Cursor");
            cursor.transform.SetParent(screenCanvas.transform, false);
            Image image = cursor.AddComponent<Image>();
            image.sprite = cursorSprite;
            image.color = cursorColor;

            RectTransform rect = cursor.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(cursorSize, cursorSize);
            cursor.SetActive(renderCursorOnCanvas);
            Cursor.visible = !renderCursorOnCanvas;
        }

        private void StartExperiment()
        {
            experimentFinished = false;
            currentTargetIndex = 0;
            currentRepetitionIndex = 0;
            successfulSelections = 0;
            missedSelections = 0;
            missesThisTrial = 0;
            results.Clear();

            currentAmplitude = amplitudes.Length > 0 ? amplitudes[0] : 260;
            currentWidth = widths.Length > 0 ? widths[0] : 42;

            BuildTargets();
            previousTargetCenter = Vector2.zero;
            trialStartTime = Time.time;
            resultText.enabled = false;
            UpdateTargetVisuals();
            UpdateHUD();
        }

        private void BuildTargets()
        {
            foreach (GameObject target in targetList)
            {
                Destroy(target);
            }

            targetList.Clear();
            targetCenters.Clear();

            int count = NumberOfTrials;
            float radius = currentAmplitude;

            for (int i = 0; i < count; i++)
            {
                float angle = (Mathf.PI * 2f * i) / count;
                Vector2 center = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

                GameObject target = new GameObject($"VR_Target_{i + 1:D2}");
                target.transform.SetParent(screenCanvas.transform, false);

                Image image = target.AddComponent<Image>();
                image.sprite = targetDiscSprite;
                image.color = foregroundColor;

                RectTransform rect = target.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(currentWidth, currentWidth);
                rect.anchoredPosition = center;
                targetList.Add(target);
                targetCenters.Add(center);
            }
        }

        private void HandleMouseClick()
        {
            Vector2 mouseLocal = GetMouseLocalPosition();

            if (currentTargetIndex >= targetList.Count)
            {
                FinishRepetitionOrExperiment();
                return;
            }

            currentTargetRect = targetList[currentTargetIndex].GetComponent<RectTransform>();
            bool hit = RectTransformUtility.RectangleContainsScreenPoint(currentTargetRect, Input.mousePosition, screenCanvas.worldCamera);

            if (!hit)
            {
                missedSelections++;
                missesThisTrial++;
                LogEvent("MISS", mouseLocal, currentTargetIndex + 1, 0, 0, 0);
                FlashTarget(buttonDownColor, 0.08f);
                return;
            }

            float movementTime = Time.time - trialStartTime;
            Vector2 targetCenter = targetCenters[currentTargetIndex];
            float distance = Vector2.Distance(previousTargetCenter, targetCenter);
            if (currentTargetIndex == 0) distance = currentAmplitude;

            float endpointError = Vector2.Distance(mouseLocal, targetCenter);
            float id = Mathf.Log((distance / Mathf.Max(1, currentWidth)) + 1f, 2f);
            float throughput = movementTime > 0.001f ? id / movementTime : 0f;

            successfulSelections++;

            TrialResult result = new TrialResult
            {
                trialNumber = results.Count + 1,
                repetition = currentRepetitionIndex + 1,
                targetIndex = currentTargetIndex + 1,
                targetWidth = currentWidth,
                targetDistance = distance,
                movementTime = movementTime,
                success = true,
                missesBeforeHit = missesThisTrial,
                endpointError = endpointError,
                indexOfDifficulty = id,
                throughput = throughput
            };

            results.Add(result);
            LogEvent("HIT", mouseLocal, currentTargetIndex + 1, movementTime, endpointError, throughput);
            LogEvaluation(result);

            if (audioFeedback && audioBeep != null) audioBeep.Play();

            missesThisTrial = 0;
            previousTargetCenter = targetCenter;
            currentTargetIndex++;

            if (currentTargetIndex >= targetList.Count)
            {
                FinishRepetitionOrExperiment();
            }
            else
            {
                trialStartTime = Time.time;
                UpdateTargetVisuals();
            }
        }

        private void FinishRepetitionOrExperiment()
        {
            currentRepetitionIndex++;

            if (currentRepetitionIndex >= numberOfRepetitions)
            {
                FinishExperiment();
                return;
            }

            currentTargetIndex = 0;
            missesThisTrial = 0;

            int widthIndex = currentRepetitionIndex % Mathf.Max(1, widths.Length);
            int amplitudeIndex = currentRepetitionIndex % Mathf.Max(1, amplitudes.Length);
            currentWidth = widths.Length > 0 ? widths[widthIndex] : currentWidth;
            currentAmplitude = amplitudes.Length > 0 ? amplitudes[amplitudeIndex] : currentAmplitude;

            BuildTargets();
            previousTargetCenter = Vector2.zero;
            trialStartTime = Time.time;
            UpdateTargetVisuals();
        }

        private void FinishExperiment()
        {
            experimentFinished = true;
            foreach (GameObject target in targetList) target.SetActive(false);

            float meanTime = results.Count > 0 ? results.Average(r => r.movementTime) : 0f;
            float meanError = results.Count > 0 ? results.Average(r => r.endpointError) : 0f;
            float meanID = results.Count > 0 ? results.Average(r => r.indexOfDifficulty) : 0f;
            float meanThroughput = results.Count > 0 ? results.Average(r => r.throughput) : 0f;
            float errorRate = (successfulSelections + missedSelections) > 0 ? (missedSelections * 100f) / (successfulSelections + missedSelections) : 0f;

            resultText.enabled = true;
            resultText.text =
                $"<b>EXPERIMENT COMPLETED</b>\n\n" +
                $"Prototype: {researchTitle}\n" +
                $"Participant: {SubjectID}    Condition: {Condition}\n\n" +
                $"Successful selections: {successfulSelections}\n" +
                $"Missed clicks: {missedSelections}\n" +
                $"Error rate: {errorRate:F1}%\n" +
                $"Mean movement time: {meanTime:F3} s\n" +
                $"Mean endpoint error: {meanError:F1} px\n" +
                $"Mean index of difficulty: {meanID:F2} bits\n" +
                $"Mean throughput: {meanThroughput:F2} bits/s\n\n" +
                $"CSV results saved to:\n<size=18>{lastExportFolder}</size>\n\n" +
                $"Press S to run the experiment again.";

            hudText.text = "Experiment complete. Use the final results for screenshots and lab-report tables.";
            CloseFileWriters();
        }

        private void UpdateTargetVisuals()
        {
            for (int i = 0; i < targetList.Count; i++)
            {
                Image image = targetList[i].GetComponent<Image>();
                RectTransform rect = targetList[i].GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(currentWidth, currentWidth);

                if (i == currentTargetIndex)
                {
                    image.color = targetColor;
                    targetList[i].transform.SetAsLastSibling();
                }
                else
                {
                    image.color = showAmplitudeTrials ? foregroundColor : new Color(0, 0, 0, 0);
                }

                targetList[i].SetActive(showAmplitudeTrials || i == currentTargetIndex);
            }
        }

        private void FlashTarget(Color color, float seconds)
        {
            if (currentTargetIndex >= targetList.Count) return;
            Image image = targetList[currentTargetIndex].GetComponent<Image>();
            image.color = color;
            CancelInvoke(nameof(UpdateTargetVisuals));
            Invoke(nameof(UpdateTargetVisuals), seconds);
        }

        private void UpdateCursor()
        {
            if (cursor == null || !renderCursorOnCanvas) return;

            RectTransform rect = cursor.GetComponent<RectTransform>();
            rect.anchoredPosition = GetMouseLocalPosition();
        }

        private Vector2 GetMouseLocalPosition()
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, Input.mousePosition, screenCanvas.worldCamera, out Vector2 localPoint);
            return localPoint;
        }

        private void UpdateHUD()
        {
            if (!showResearchHUD || hudText == null) return;

            int totalTrials = NumberOfTrials * numberOfRepetitions;
            int completedTrials = results.Count;
            float currentTime = experimentFinished ? 0f : Time.time - trialStartTime;
            float errorRate = (successfulSelections + missedSelections) > 0 ? (missedSelections * 100f) / (successfulSelections + missedSelections) : 0f;
            float meanTime = results.Count > 0 ? results.Average(r => r.movementTime) : 0f;
            float meanThroughput = results.Count > 0 ? results.Average(r => r.throughput) : 0f;

            hudText.text =
                $"<b>Live experiment data</b>\n" +
                $"Mode: desktop VR/AR simulation\n" +
                $"Participant: {SubjectID}\n" +
                $"Task: {Task}\n" +
                $"Progress: {completedTrials}/{totalTrials}\n" +
                $"Current target: {Mathf.Min(currentTargetIndex + 1, NumberOfTrials)}/{NumberOfTrials}\n" +
                $"Repetition: {Mathf.Min(currentRepetitionIndex + 1, numberOfRepetitions)}/{numberOfRepetitions}\n" +
                $"Target width: {currentWidth}px | Distance: {currentAmplitude}px\n" +
                $"Current trial: {currentTime:F2}s\n" +
                $"Hits: {successfulSelections} | Misses: {missedSelections} | Error: {errorRate:F1}%\n" +
                $"Mean time: {meanTime:F3}s | Throughput: {meanThroughput:F2} bits/s";
        }

        private void InitializeLogging()
        {
            string rootPath = Application.isEditor
                ? Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Results", "FittsLogging")
                : Path.Combine(Application.persistentDataPath, "Results", "FittsLogging");

            lastExportFolder = rootPath;
            Directory.CreateDirectory(rootPath);

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);

            if (saveEvents)
            {
                string path = Path.Combine(rootPath, $"EventLog_Subject{SubjectID}_{timestamp}.csv");
                eventLogWriter = new StreamWriter(path) { AutoFlush = true };
                eventLogWriter.WriteLine("Timestamp;Event;SubjectID;Condition;Task;Repetition;Target;MouseX;MouseY;MovementTime;EndpointError;Throughput");
            }

            if (saveEvaluation)
            {
                string path = Path.Combine(rootPath, $"Evaluation_Subject{SubjectID}_{timestamp}.csv");
                evaluationLogWriter = new StreamWriter(path) { AutoFlush = true };
                evaluationLogWriter.WriteLine("Trial;Repetition;TargetIndex;TargetWidth;TargetDistance;MovementTime;MissesBeforeHit;EndpointError;IndexOfDifficulty;Throughput");
            }
        }

        private void LogEvent(string eventName, Vector2 mouseLocal, int target, float movementTime, float endpointError, float throughput)
        {
            if (eventLogWriter == null) return;

            eventLogWriter.WriteLine(
                $"{Timestamp()};{eventName};{SubjectID};{Condition};{Task};{currentRepetitionIndex + 1};{target};" +
                $"{mouseLocal.x:F2};{mouseLocal.y:F2};{movementTime:F4};{endpointError:F2};{throughput:F4}");
        }

        private void LogEvaluation(TrialResult result)
        {
            if (evaluationLogWriter == null) return;

            evaluationLogWriter.WriteLine(
                $"{result.trialNumber};{result.repetition};{result.targetIndex};{result.targetWidth:F1};{result.targetDistance:F1};" +
                $"{result.movementTime:F4};{result.missesBeforeHit};{result.endpointError:F2};{result.indexOfDifficulty:F4};{result.throughput:F4}");
        }

        private string Timestamp()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        }

        private void CloseFileWriters()
        {
            if (eventLogWriter != null)
            {
                eventLogWriter.Flush();
                eventLogWriter.Close();
                eventLogWriter = null;
            }

            if (evaluationLogWriter != null)
            {
                evaluationLogWriter.Flush();
                evaluationLogWriter.Close();
                evaluationLogWriter = null;
            }
        }

        private void OnApplicationQuit()
        {
            CloseFileWriters();
        }
    }
}
