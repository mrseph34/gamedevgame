using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(HeartMonitor))]
public class HitFlashHandler : MonoBehaviour
{
    [Tooltip("Seconds between color toggles")]
    public float flashInterval = 0.2f;

    private HeartMonitor monitor;
    private Image heartImg;

    void Awake()
    {
        monitor = GetComponent<HeartMonitor>();
    }

    void OnEnable()
    {
        monitor.OnHit += HandleHit;
    }

    void OnDisable()
    {
        monitor.OnHit -= HandleHit;
        // ensure we end on right colourr
        if (heartImg != null)
            heartImg.color = Color.white;
    }

    void Start()
    {
        heartImg = monitor.GetHeartImage();
        if (heartImg == null)
            Debug.LogError("HitFlashHandler: no Image found on HeartMonitor!", this);
    }

    private void HandleHit(float duration)
    {
        StopAllCoroutines();
        StartCoroutine(FlashRoutine(duration));
    }

    private IEnumerator FlashRoutine(float duration)
    {
        float elapsed = 0f;
        bool isRed = false;

        while (elapsed < duration)
        {
            isRed = !isRed;
            heartImg.color = isRed ? Color.red : Color.white;
            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval;
        }

        heartImg.color = Color.white;
    }
}