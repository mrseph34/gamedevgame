using UnityEngine;
using System.Collections;

[RequireComponent(typeof(HeartMonitor))]
public class BpmHandler : MonoBehaviour
{
    private HeartMonitor monitor;
    private Coroutine idleRoutine;
    private Coroutine rampRoutine;

    void Start()
    {
        monitor = GetComponent<HeartMonitor>();
        if (monitor == null)
        {
            Debug.LogError("BpmHandler requires a HeartMonitor.", this);
            enabled = false;
            return;
        }

        // Whenever damage is taken, restart the idle‐timer so auto‐increase
        // will kick in delayBeforeIncrease seconds later.
        monitor.OnHit += OnHit;

        RestartIdleTimer();
    }

    void OnDisable()
    {
        // Clean up
        if (monitor != null)
            monitor.OnHit -= OnHit;
        StopAllCoroutines();
    }

    // manual overrides testing?
    public void SetBPM(float newBpm)
    {
        monitor.BPM = newBpm;
        RestartIdleTimer();
    }

    public void AddBPM(float delta)
    {
        monitor.BPM = monitor.BPM + delta;
        RestartIdleTimer();
    }

    public void TakeDamage(int dmg)
    {
        monitor.TakeDamage(dmg);
        // note: HeartMonitor.TakeDamage fires OnHit, so RestartIdleTimer will be called there too
    }

    // Called whenever HeartMonitor.TakeDamage(...) fires OnHit
    private void OnHit(float flashDuration)
    {
        RestartIdleTimer();
    }

    private void RestartIdleTimer()
    {
        if (idleRoutine != null)
            StopCoroutine(idleRoutine);
        if (rampRoutine != null)
            StopCoroutine(rampRoutine);
        idleRoutine = StartCoroutine(IdleCoroutine());
    }

    private IEnumerator IdleCoroutine()
    {
        // wait the configured delay
        yield return new WaitForSeconds(monitor.delayBeforeIncrease);
        rampRoutine = StartCoroutine(RampUpCoroutine());
    }

    private IEnumerator RampUpCoroutine()
    {
        // smoothly ramp BPM toward max at autoIncreaseRate per second
        while (monitor.BPM < monitor.maxBPM)
        {
            monitor.BPM = Mathf.MoveTowards(
                monitor.BPM,
                monitor.maxBPM,
                monitor.autoIncreaseRate * Time.deltaTime
            );
            yield return null;
        }
        rampRoutine = null;
    }
}
