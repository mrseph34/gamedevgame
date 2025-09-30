using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEditor.Animations;

[RequireComponent(typeof(PulseHandler))]
public class HeartMonitor : MonoBehaviour
{
    [Header("UI Config")]
    public UIPositionConfig positionConfig;
    public Sprite heartSprite;
    public Vector2 initialSize = new Vector2(100, 100);

    [Header("BPM Settings")]
    [Min(0)]
    public float minBPM = 0f;
    [Min(0)]
    public float maxBPM = 200f;
    [SerializeField]
    private float bpm = 60f;
    public float BPM
    {
        get => bpm;
        set => SetBPM(value);
    }

    [Header("Auto‐Increase Settings")]
    [Tooltip("Seconds after last manual BPM change before auto‐increasing")]
    public float delayBeforeIncrease = 10f;

    [Tooltip("How many BPM per second to add once auto‐increase starts")]
    public float autoIncreaseRate = 5f;

    [Header("Pulse Settings")]
    [Tooltip("± scale around 1.0 for the continuous pulse (e.g. 0.2 = 20%)")]
    [Min(0)]
    public float pulseAmplitude = 0.2f;

    // Events
    public event Action OnBeat;
    public event Action<float> OnHit; // flash duration

    // Animator refs
    public Animator playerAnimator;

    // UI refs
    private RectTransform heartRect;
    private Image heartImage;
    private Text heartText;

    // Internal coroutines
    private Coroutine beatRoutine;
    private Coroutine adjustRoutine;

    void Awake()
    {
        if (GetComponent<BpmHandler>() == null)
            gameObject.AddComponent<BpmHandler>();
        if (GetComponent<PulseHandler>() == null)
            gameObject.AddComponent<PulseHandler>();
        if (GetComponent<HitFlashHandler>() == null)
            gameObject.AddComponent<HitFlashHandler>();

        CreateUI();
        bpm = Mathf.Clamp(bpm, minBPM, maxBPM);
        heartText.text = Mathf.RoundToInt(bpm).ToString();
    }

    void OnEnable()
    {
        StartBeating();
    }

    void OnDisable()
    {
        StopBeating();
        if (adjustRoutine != null)
            StopCoroutine(adjustRoutine);
    }

    void CreateUI()
    {
        var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var cgo = new GameObject("Canvas", typeof(Canvas));
            canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cgo.AddComponent<CanvasScaler>();
            cgo.AddComponent<GraphicRaycaster>();
        }

        var hg = new GameObject("HeartUI", typeof(RectTransform));
        heartRect = hg.GetComponent<RectTransform>();
        heartRect.SetParent(canvas.transform, false);
        heartRect.sizeDelta = initialSize;
        heartRect.pivot = Vector2.one * 0.5f;
        heartRect.anchorMin = positionConfig.anchorMin;
        heartRect.anchorMax = positionConfig.anchorMax;
        heartRect.anchoredPosition = positionConfig.anchoredPosition;

        heartImage = hg.AddComponent<Image>();
        heartImage.sprite = heartSprite;
        heartImage.preserveAspect = true;
        heartImage.color = Color.white;

        var tg = new GameObject("HeartText", typeof(RectTransform));
        var tr = tg.GetComponent<RectTransform>();
        tr.SetParent(heartRect, false);
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = tr.offsetMax = Vector2.zero;

        heartText = tg.AddComponent<Text>();
        heartText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        heartText.alignment = TextAnchor.MiddleCenter;
        heartText.color = Color.black;
        heartText.fontSize = 24;
    }

    private void SetBPM(float newBpm)
    {
        bpm = Mathf.Clamp(newBpm, minBPM, maxBPM);
        if (heartText != null)
            heartText.text = Mathf.RoundToInt(bpm).ToString();
        // OnBeat coroutine picks up bpm changes automatically

        //restart when bpm = 0
        if (bpm <= 0f)
        {
            Debug.Log("BPM reached 0! Restarting scene...");
            playerAnimator.SetTrigger("playerDeath");
            StartCoroutine(RestartSceneAfterDelay()); // optional delay
        }
    }

    private IEnumerator RestartSceneAfterDelay()
    {
        AnimatorStateInfo playerStateInfo = playerAnimator.GetCurrentAnimatorStateInfo(0);
        while (!playerStateInfo.IsName("P1_Death_Clip"))
        {
            yield return null;
            playerStateInfo = playerAnimator.GetCurrentAnimatorStateInfo(0);
        }
        yield return new WaitForSeconds(playerStateInfo.length);
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }


    public void StartBeating()
    {
        if (beatRoutine == null)
            beatRoutine = StartCoroutine(BeatTick());
    }

    public void StopBeating()
    {
        if (beatRoutine != null)
        {
            StopCoroutine(beatRoutine);
            beatRoutine = null;
        }
    }

    private IEnumerator BeatTick()
    {
        while (true)
        {
            if (bpm <= 0f)
                yield break;

            float interval = 60f / bpm;
            yield return new WaitForSeconds(interval);
            OnBeat?.Invoke();
        }
    }

    public void AdjustBPM(float delta, float rate)
    {
        if (adjustRoutine != null)
            StopCoroutine(adjustRoutine);
        float target = Mathf.Clamp(bpm + delta, minBPM, maxBPM);
        adjustRoutine = StartCoroutine(AdjustRoutine(target, rate));
    }

    private IEnumerator AdjustRoutine(float target, float rate)
    {
        while (!Mathf.Approximately(bpm, target))
        {
            float tempBPM = Mathf.MoveTowards(bpm, target, rate * Time.deltaTime);
            SetBPM(tempBPM);
            heartText.text = Mathf.RoundToInt(bpm).ToString();
            yield return null;
        }
        adjustRoutine = null;
    }

    public void TakeDamage(int dmg, float adjustRate = 30f, float flashDuration = 1f)
    {
        AdjustBPM(-dmg, adjustRate);
        OnHit?.Invoke(flashDuration);
    }

    // Expose for handlers
    public RectTransform GetHeartRect() => heartRect;
    public Image GetHeartImage() => heartImage;
}
