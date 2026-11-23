using TMPro;
using UnityEngine;

public class GameDurationProgress : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private UnityEngine.UI.Image progressBar;
    [SerializeField] private RectTransform movingImage;

    [Header("Extra Time")]
    [SerializeField] private float extraTime = 0f;

    [Header("Cinematic Path")]
    [SerializeField] private int pathSegments = 24;
    [SerializeField] private int checkpointCount = 7;
    [SerializeField] private Vector2 pathStart = new Vector2(-340f, -15f);
    [SerializeField] private Vector2 pathControl = new Vector2(0f, 55f);
    [SerializeField] private Vector2 pathEnd = new Vector2(340f, 0f);
    [SerializeField] private Color completedColor = new Color(0.05f, 0.78f, 1f, 1f);
    [SerializeField] private Color remainingColor = new Color(1f, 1f, 1f, 0.62f);

    private float timer;
    private float totalDuration = 65f;
    private RectTransform visualRoot;
    private UnityEngine.UI.Image[] pathImages;
    private UnityEngine.UI.Image[] checkpointImages;
    private int lastCompletedSegment = -1;
    private int lastCompletedCheckpoint = -1;

    private void Start()
    {
        if (gameManager == null || progressBar == null)
        {
            enabled = false;
            return;
        }

        BindOrBuildCinematicPath();
        ResetProgress();
    }

    private void OnEnable()
    {
        GameEvents.GameStarted += HandleGameStarted;
        GameEvents.Victory += HandleVictory;
        GameEvents.GameOver += HandleGameOver;
    }

    private void OnDisable()
    {
        GameEvents.GameStarted -= HandleGameStarted;
        GameEvents.Victory -= HandleVictory;
        GameEvents.GameOver -= HandleGameOver;
    }

    private void HandleGameStarted()
    {
        enabled = true;
        ResetProgress();
    }

    private void HandleVictory()
    {
        SetVisualProgress(1f);
    }

    private void HandleGameOver()
    {
        // Keep visual progress frozen at current point
    }

    public void ResetProgress()
    {
        totalDuration = (gameManager != null && gameManager.GameDuration > 0f)
            ? gameManager.GameDuration
            : (gameManager != null && gameManager.RemainingTime > 0f ? gameManager.RemainingTime : 65f);

        timer = 0f;
        lastCompletedSegment = -2;
        lastCompletedCheckpoint = -2;
        lastVisualProgress = -1f;
        SetVisualProgress(0f);
    }

    private float lastVisualProgress = -1f;

    private void Update()
    {
        if (gameManager == null || gameManager.State != GameState.Playing)
            return;

        float duration = totalDuration > 0f ? totalDuration : 65f;
        float remaining = Mathf.Clamp(gameManager.RemainingTime, 0f, duration);

        // Perfectly synchronized with flight duration and runway touchdown
        float progress = Mathf.Clamp01((duration - remaining) / duration);

        if (Mathf.Abs(progress - lastVisualProgress) >= 0.001f || progress >= 1f || progress <= 0f)
        {
            lastVisualProgress = progress;
            SetVisualProgress(progress);
        }
    }

    public void BuildPersistentHierarchy()
    {
        BindOrBuildCinematicPath();
    }

    private void BindOrBuildCinematicPath()
    {
        RectTransform parent =
            progressBar.rectTransform.parent as RectTransform;

        if (parent == null)
            return;

        parent.gameObject.SetActive(true);
        progressBar.gameObject.SetActive(false);

        Transform existingVisual = parent.Find("Cinematic Progress");

        if (existingVisual != null)
        {
            BindExistingHierarchy(existingVisual as RectTransform);
            return;
        }

        visualRoot = CreateRect("Cinematic Progress", parent);
        visualRoot.anchorMin = new Vector2(0.5f, 1f);
        visualRoot.anchorMax = new Vector2(0.5f, 1f);
        visualRoot.pivot = new Vector2(0.5f, 1f);
        visualRoot.anchoredPosition = new Vector2(0f, -48f);
        visualRoot.sizeDelta = new Vector2(820f, 130f);
        visualRoot.localScale = Vector3.one * 1.2f;

        pathSegments = Mathf.Max(8, pathSegments);
        checkpointCount = Mathf.Max(2, checkpointCount);
        pathImages = new UnityEngine.UI.Image[pathSegments];

        for (int i = 0; i < pathSegments; i++)
        {
            float startT = i / (float)pathSegments;
            float endT = (i + 1f) / pathSegments;

            pathImages[i] = CreateLineSegment(
                $"Path Segment {i:00}",
                visualRoot,
                GetPathPosition(startT),
                GetPathPosition(endT)
            );
        }

        checkpointImages =
            new UnityEngine.UI.Image[checkpointCount];
        Sprite circleSprite = CreateCircleSprite();

        for (int i = 0; i < checkpointCount; i++)
        {
            float t = i / (float)(checkpointCount - 1);
            UnityEngine.UI.Image checkpoint =
                CreateImage($"Checkpoint {i + 1}", visualRoot);

            checkpoint.sprite = circleSprite;
            checkpoint.rectTransform.sizeDelta =
                i == checkpointCount - 1
                    ? new Vector2(18f, 18f)
                    : new Vector2(8f, 8f);
            checkpoint.rectTransform.anchoredPosition = GetPathPosition(t);
            checkpointImages[i] = checkpoint;
        }

        CreateDirectionLabel("W", 0.22f, 28f);
        CreateDirectionLabel("N", 0.50f, 43f);
        CreateDirectionLabel("E", 0.78f, 28f);

        TextMeshProUGUI destination =
            CreateLabel("Destination", visualRoot);
        destination.text = "DESTINATION";
        destination.fontSize = 17f;
        destination.alignment = TextAlignmentOptions.Left;
        destination.color = Color.white;
        destination.rectTransform.sizeDelta = new Vector2(170f, 30f);
        destination.rectTransform.anchoredPosition =
            pathEnd + new Vector2(28f, 22f);

        if (movingImage != null)
        {
            movingImage.SetParent(visualRoot, false);
            movingImage.anchorMin = new Vector2(0.5f, 0.5f);
            movingImage.anchorMax = new Vector2(0.5f, 0.5f);
            movingImage.pivot = new Vector2(0.5f, 0.5f);
            movingImage.sizeDelta = new Vector2(54f, 30f);
            movingImage.SetAsLastSibling();

            UnityEngine.UI.Image icon =
                movingImage.GetComponent<UnityEngine.UI.Image>();

            if (icon != null)
            {
                icon.preserveAspect = true;
                icon.raycastTarget = false;
            }
        }

        EnsureSubCanvas(visualRoot);
    }

    private void EnsureSubCanvas(RectTransform root)
    {
        if (root != null && root.GetComponent<Canvas>() == null)
        {
            var subCanvas = root.gameObject.AddComponent<Canvas>();
            subCanvas.overrideSorting = false;
        }
    }

    private void BindExistingHierarchy(RectTransform existingRoot)
    {
        visualRoot = existingRoot;
        pathSegments = Mathf.Max(8, pathSegments);
        checkpointCount = Mathf.Max(2, checkpointCount);
        pathImages = new UnityEngine.UI.Image[pathSegments];
        checkpointImages = new UnityEngine.UI.Image[checkpointCount];

        for (int i = 0; i < pathSegments; i++)
        {
            Transform segment =
                visualRoot.Find($"Path Segment {i:00}");

            if (segment != null)
                pathImages[i] = segment.GetComponent<UnityEngine.UI.Image>();
        }

        for (int i = 0; i < checkpointCount; i++)
        {
            Transform checkpoint =
                visualRoot.Find($"Checkpoint {i + 1}");

            if (checkpoint != null)
                checkpointImages[i] = checkpoint.GetComponent<UnityEngine.UI.Image>();
        }

        Transform planeIcon = visualRoot.Find("Image");

        if (planeIcon != null)
            movingImage = planeIcon as RectTransform;

        EnsureSubCanvas(visualRoot);
    }

    private void SetVisualProgress(float progress)
    {
        if (visualRoot == null)
            return;

        int totalSegments = pathImages != null ? pathImages.Length : 24;
        int activeSegment = progress <= 0f ? -1 : Mathf.Min(totalSegments - 1, Mathf.FloorToInt(progress * totalSegments));
        if (activeSegment != lastCompletedSegment && pathImages != null)
        {
            lastCompletedSegment = activeSegment;
            for (int i = 0; i < pathImages.Length; i++)
            {
                if (pathImages[i] != null)
                {
                    pathImages[i].color = (activeSegment >= 0 && i <= activeSegment) ? completedColor : remainingColor;
                }
            }
        }

        int totalCheckpoints = checkpointImages != null ? checkpointImages.Length : 7;
        int activeCheckpoint = progress <= 0f ? 0 : Mathf.Min(totalCheckpoints - 1, Mathf.FloorToInt(progress * (totalCheckpoints - 1)));
        if (activeCheckpoint != lastCompletedCheckpoint && checkpointImages != null)
        {
            lastCompletedCheckpoint = activeCheckpoint;
            for (int i = 0; i < checkpointImages.Length; i++)
            {
                if (checkpointImages[i] != null)
                {
                    checkpointImages[i].color = i <= activeCheckpoint ? completedColor : remainingColor;
                }
            }
        }

        if (movingImage == null)
            return;

        movingImage.anchoredPosition = GetPathPosition(progress);

        float lookAhead = Mathf.Min(1f, progress + 0.02f);
        Vector2 direction =
            GetPathPosition(lookAhead) - GetPathPosition(progress);

        if (direction.sqrMagnitude > 0.001f)
        {
            float angle =
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            movingImage.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    private Vector2 GetPathPosition(float t)
    {
        float inverse = 1f - t;
        return inverse * inverse * pathStart +
               2f * inverse * t * pathControl +
               t * t * pathEnd;
    }

    private UnityEngine.UI.Image CreateLineSegment(
        string objectName,
        RectTransform parent,
        Vector2 start,
        Vector2 end)
    {
        UnityEngine.UI.Image image = CreateImage(objectName, parent);
        Vector2 difference = end - start;
        RectTransform rect = image.rectTransform;
        rect.anchoredPosition = (start + end) * 0.5f;
        rect.sizeDelta = new Vector2(difference.magnitude + 1f, 2.5f);
        rect.localRotation = Quaternion.Euler(
            0f,
            0f,
            Mathf.Atan2(difference.y, difference.x) * Mathf.Rad2Deg
        );
        return image;
    }

    private void CreateDirectionLabel(
        string text,
        float pathPosition,
        float yOffset)
    {
        TextMeshProUGUI label =
            CreateLabel($"Direction {text}", visualRoot);
        label.text = text;
        label.fontSize = text == "N" ? 20f : 15f;
        label.fontStyle =
            text == "N" ? FontStyles.Bold : FontStyles.Normal;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.rectTransform.sizeDelta = new Vector2(42f, 28f);
        label.rectTransform.anchoredPosition =
            GetPathPosition(pathPosition) + Vector2.up * yOffset;
    }

    private RectTransform CreateRect(
        string objectName,
        RectTransform parent)
    {
        GameObject child =
            new GameObject(objectName, typeof(RectTransform));
        child.layer = parent.gameObject.layer;
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        return rect;
    }

    private UnityEngine.UI.Image CreateImage(
        string objectName,
        RectTransform parent)
    {
        RectTransform rect = CreateRect(objectName, parent);
        UnityEngine.UI.Image image =
            rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color = remainingColor;
        image.raycastTarget = false;
        return image;
    }

    private TextMeshProUGUI CreateLabel(
        string objectName,
        RectTransform parent)
    {
        RectTransform rect = CreateRect(objectName, parent);
        TextMeshProUGUI label =
            rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.raycastTarget = false;
        return label;
    }

    private Sprite CreateCircleSprite()
    {
        const int size = 32;
        Texture2D texture =
            new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "Runtime Progress Circle";
        texture.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[size * size];
        Vector2 center =
            new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.46f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance =
                    Vector2.Distance(new Vector2(x, y), center);
                float alpha =
                    Mathf.Clamp01(radius - distance + 0.75f);
                pixels[y * size + x] =
                    new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f
        );
    }
}
