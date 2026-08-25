using UnityEngine;

/// <summary>
/// Add this script to the practice target prefab.
/// Connect XR Simple Interactable ¡ú Select Entered to SelectTarget().
/// </summary>
[DisallowMultipleComponent]
public class PracticeTarget : MonoBehaviour
{
    private PracticeManager practiceManager;
    private bool alreadySelected;

    public void Initialize(
        PracticeManager manager)
    {
        practiceManager = manager;
        alreadySelected = false;
    }

    public void SelectTarget()
    {
        if (alreadySelected)
        {
            return;
        }

        alreadySelected = true;

        Collider[] colliders =
            GetComponentsInChildren<Collider>();

        foreach (Collider item in colliders)
        {
            item.enabled = false;
        }

        if (practiceManager == null)
        {
            Debug.LogError(
                "PracticeTarget: PracticeManager is missing.",
                this);
            return;
        }

        practiceManager.RegisterTargetSelected(this);
    }
}