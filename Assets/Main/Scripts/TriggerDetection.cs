using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class PlayerRelicDetector : MonoBehaviour
{
    [Header("UI Settings")]
    public Canvas uiCanvas;
    public GameObject uiPrefab;
    public GameObject exitBtn;
    public Vector3 uiOffset = new Vector3(0, 50f, 0);
    public GameObject sidePanelPrefab;
    public Vector3 leftOffset = new Vector3(-150f, 0, 0);
    public Vector3 rightOffset = new Vector3(150f, 0, 0);

    [Header("Panel Texts")]
    [TextArea] public string leftPanelTitle = "Left Panel Title";
    [TextArea] public string leftPanelContent = "Left panel content goes here.";
    [TextArea] public string rightPanelTitle = "Right Panel Title";
    [TextArea] public string rightPanelContent = "Right panel content goes here.";

    [Header("References")]
    public FloatingJoystick joystick;
    public TextMeshProUGUI showroomLabel;

    private Camera mainCamera;
    private GameObject currentRelic;
    private Transform relicVisual;
    private GameObject uiInstance;
    private GameObject leftPanel;
    private GameObject rightPanel;

    private Vector3 relicOriginalPos;
    private Quaternion relicOriginalRot;
    private Quaternion relicVisualOriginalRot;

    private Coroutine resetRoutine;

    public bool IsInspecting { get; private set; }

    void Start()
    {
        mainCamera = Camera.main;

        if (showroomLabel != null)
            showroomLabel.text = "Showroom";
    }

    void Update()
    {
        if (currentRelic != null && !IsInspecting)
        {
            if (uiInstance != null)
            {
                Vector3 dirToRelic = (relicVisual.position - mainCamera.transform.position).normalized;
                float dot = Vector3.Dot(mainCamera.transform.forward, dirToRelic);

                if (dot > 0f)
                {
                    Vector3 screenPos = mainCamera.WorldToScreenPoint(relicVisual.position);

                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        uiCanvas.transform as RectTransform,
                        screenPos,
                        uiCanvas.worldCamera,
                        out Vector2 localPoint
                    );

                    uiInstance.GetComponent<RectTransform>().localPosition = localPoint + (Vector2)uiOffset;
                    uiInstance.SetActive(true);

                    var textComponent = uiInstance.GetComponentInChildren<TextMeshProUGUI>();
                    if (textComponent != null)
                        textComponent.text = "Inspect";
                }
                else
                {
                    uiInstance.SetActive(false);
                }
            }
        }

        if (IsInspecting && currentRelic != null && relicVisual != null)
        {
            Vector3 dirToRelic = (relicVisual.position - mainCamera.transform.position).normalized;
            float dot = Vector3.Dot(mainCamera.transform.forward, dirToRelic);

            if (dot > 0f)
            {
                Vector3 screenPos = mainCamera.WorldToScreenPoint(relicVisual.position);

                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    uiCanvas.transform as RectTransform,
                    screenPos,
                    uiCanvas.worldCamera,
                    out Vector2 relicLocalPoint
                );

                if (leftPanel != null)
                {
                    leftPanel.GetComponent<RectTransform>().localPosition = relicLocalPoint + (Vector2)leftOffset;
                    leftPanel.SetActive(true);
                }

                if (rightPanel != null)
                {
                    rightPanel.GetComponent<RectTransform>().localPosition = relicLocalPoint + (Vector2)rightOffset;
                    rightPanel.SetActive(true);
                }
            }
            else
            {
                if (leftPanel != null) leftPanel.SetActive(false);
                if (rightPanel != null) rightPanel.SetActive(false);
            }

            currentRelic.transform.position = relicOriginalPos;
            currentRelic.transform.rotation = relicOriginalRot;

#if UNITY_EDITOR
            if (Mouse.current.leftButton.isPressed && !EventSystem.current.IsPointerOverGameObject())
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                RotateRelic(delta);
            }
#else
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                // For mobile, check if touch is over UI
                if (!EventSystem.current.IsPointerOverGameObject(Touchscreen.current.primaryTouch.touchId.ReadValue()))
                {
                    Vector2 delta = Touchscreen.current.primaryTouch.delta.ReadValue();
                    RotateRelic(delta);
                }
            }
#endif
        }
    }

    private void RotateRelic(Vector2 delta)
    {
        float rotationSpeed = 0.2f;
        relicVisual.Rotate(Vector3.up, -delta.x * rotationSpeed, Space.World);
        relicVisual.Rotate(Vector3.right, delta.y * rotationSpeed, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Relic") && !IsInspecting)
        {
            currentRelic = other.gameObject;

            relicVisual = currentRelic.transform.childCount > 0 ? currentRelic.transform.GetChild(0) : currentRelic.transform;

            if (uiPrefab != null && uiCanvas != null && uiInstance == null)
            {
                uiInstance = Instantiate(uiPrefab, uiCanvas.transform);

                var textComponent = uiInstance.GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = "Inspect\n" + currentRelic.name;
                }
            }

            if (showroomLabel != null)
                showroomLabel.text = currentRelic.name;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsInspecting)
        {
            IsInspecting = false;
        }
        if (!IsInspecting && other.CompareTag("Relic") && other.gameObject == currentRelic)
        {
            if (uiInstance != null) Destroy(uiInstance);
            currentRelic = null;
            relicVisual = null;

            if (showroomLabel != null)
                showroomLabel.text = "Showroom";
        }
    }

    public void StartInspecting()
    {
        if (currentRelic == null) return;

        IsInspecting = true;

        relicOriginalPos = currentRelic.transform.position;
        relicOriginalRot = currentRelic.transform.rotation;

        if (relicVisual != null)
            relicVisualOriginalRot = relicVisual.rotation;

        if (uiInstance != null) uiInstance.SetActive(false);

        if (joystick != null) joystick.gameObject.SetActive(false);

        if (!exitBtn.activeSelf) exitBtn.SetActive(true);

        if (sidePanelPrefab != null && uiCanvas != null)
        {
            if (leftPanel == null)
            {
                leftPanel = Instantiate(sidePanelPrefab, uiCanvas.transform);
                AssignPanelTexts(leftPanel, leftPanelTitle, leftPanelContent);
            }

            if (rightPanel == null)
            {
                rightPanel = Instantiate(sidePanelPrefab, uiCanvas.transform);
                AssignPanelTexts(rightPanel, rightPanelTitle, rightPanelContent);
            }
        }
    }

    public void ExitInspectMode()
    {
        if (currentRelic != null)
        {
            if (resetRoutine != null) StopCoroutine(resetRoutine);
            resetRoutine = StartCoroutine(ResetRelicTransform(currentRelic, relicVisual));
        }

        IsInspecting = false;

        if (joystick != null) joystick.gameObject.SetActive(true);
        if (exitBtn.activeSelf) exitBtn.SetActive(false);
        if (uiInstance != null) uiInstance.SetActive(true);

        if (leftPanel != null) Destroy(leftPanel);
        if (rightPanel != null) Destroy(rightPanel);
    }

    private void AssignPanelTexts(GameObject panel, string title, string content)
    {
        TextMeshProUGUI[] texts = panel.GetComponentsInChildren<TextMeshProUGUI>();
        if (texts.Length >= 2)
        {
            texts[0].text = title;
            texts[1].text = content;
        }
    }

    private IEnumerator ResetRelicTransform(GameObject relic, Transform visual)
    {
        float duration = 0.5f;
        float elapsed = 0f;

        Vector3 startPos = relic.transform.position;
        Quaternion startRot = relic.transform.rotation;
        Quaternion startVisualRot = visual.rotation;

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            relic.transform.position = Vector3.Lerp(startPos, relicOriginalPos, t);
            relic.transform.rotation = Quaternion.Slerp(startRot, relicOriginalRot, t);
            if (visual != null)
                visual.rotation = Quaternion.Slerp(startVisualRot, relicVisualOriginalRot, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        relic.transform.position = relicOriginalPos;
        relic.transform.rotation = relicOriginalRot;
        if (visual != null)
            visual.rotation = relicVisualOriginalRot;

        resetRoutine = null;
    }
}
