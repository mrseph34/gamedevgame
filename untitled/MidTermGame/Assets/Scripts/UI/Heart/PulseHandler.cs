using UnityEngine;

[RequireComponent(typeof(HeartMonitor))]
public class PulseHandler : MonoBehaviour
{
    private HeartMonitor monitor;
    private RectTransform heartRect;
    private Vector3 baseScale;
    private float phase;

    void Awake()
    {
        monitor = GetComponent<HeartMonitor>();
    }

    void Start()
    {
        heartRect = monitor.GetHeartRect();
        baseScale = heartRect.localScale;
        phase = 0f;
    }

    void Update()
    {
        // advance at BPM/60 cycles per sec → rad/sec
        phase += (monitor.BPM / 60f) * 2f * Mathf.PI * Time.deltaTime;
        float sine = Mathf.Sin(phase); // –1..+1
        float scaleF = 1f + sine * monitor.pulseAmplitude; // [1–amp .. 1+amp]
        heartRect.localScale = baseScale * scaleF;
    }

    void OnDisable()
    {
        if (heartRect != null)
            heartRect.localScale = baseScale;
    }
}