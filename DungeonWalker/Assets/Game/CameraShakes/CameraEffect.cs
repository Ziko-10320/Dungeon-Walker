using System.Collections;
using UnityEngine;

public class CameraEffects : MonoBehaviour
{
    [Header("Camera Hold Effect Settings")]
    [SerializeField] private float holdIntensity = 0.1f; // How much the camera "holds" (zoom in slightly)
    [SerializeField] private float holdDuration = 1.0f; // Duration of the hold effect
    [SerializeField] private AnimationCurve holdCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Curve for smooth transitions

    [Header("Camera Shake Settings")]
    [SerializeField] private float shakeIntensity = 0.2f; // Intensity of the camera shake
    [SerializeField] private float shakeDuration = 0.3f; // Duration of the shake effect

    private Camera cam;
    private Vector3 currentShakeOffset = Vector3.zero; // The current offset applied by shake
    private float originalSize;
    private bool isEffectActive = false;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = Camera.main;
        }
        originalSize = cam.orthographicSize;
    }

    void LateUpdate()
    {
        // Apply the shake offset in LateUpdate to ensure it's applied after CameraFollowMouseHorizontal
        transform.position += currentShakeOffset;
    }

    public void StartHoldEffect()
    {
        if (!isEffectActive)
        {
            StartCoroutine(HoldEffectRoutine());
        }
    }

    public void StartShakeEffect()
    {
        if (!isEffectActive)
        {
            StartCoroutine(ShakeEffectRoutine());
        }
    }

    public void StartHoldAndReleaseEffect()
    {
        if (!isEffectActive)
        {
            StartCoroutine(HoldAndReleaseRoutine());
        }
    }

    IEnumerator HoldEffectRoutine()
    {
        isEffectActive = true;
        float timer = 0f;
        float startSize = cam.orthographicSize;
        float targetSize = originalSize * (1f - holdIntensity);

        // Hold phase
        while (timer < holdDuration)
        {
            timer += Time.unscaledDeltaTime; // Use unscaled time to work with Time.timeScale changes
            float progress = holdCurve.Evaluate(timer / holdDuration);

            cam.orthographicSize = Mathf.Lerp(startSize, targetSize, progress);
            yield return null;
        }

        isEffectActive = false;
    }

    IEnumerator ShakeEffectRoutine()
    {
        isEffectActive = true;
        float timer = 0f;

        while (timer < shakeDuration)
        {
            timer += Time.unscaledDeltaTime;

            Vector3 randomOffset = Random.insideUnitSphere * shakeIntensity;
            randomOffset.z = 0; // Keep the camera on the same Z plane

            currentShakeOffset = randomOffset; // Update the offset
            yield return null;
        }

        currentShakeOffset = Vector3.zero; // Reset offset after shake
        isEffectActive = false;
    }

    IEnumerator HoldAndReleaseRoutine()
    {
        isEffectActive = true;

        // Hold phase
        float timer = 0f;
        float startSize = cam.orthographicSize;
        float targetSize = originalSize * (1f - holdIntensity);

        while (timer < holdDuration * 0.8f) // Hold for 80% of the duration
        {
            timer += Time.unscaledDeltaTime;
            float progress = holdCurve.Evaluate(timer / (holdDuration * 0.8f));

            cam.orthographicSize = Mathf.Lerp(startSize, targetSize, progress);
            yield return null;
        }

        // Release phase with shake
        timer = 0f;
        float releaseDuration = holdDuration * 0.2f; // Release for 20% of the duration

        while (timer < releaseDuration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = timer / releaseDuration;

            // Return to original size
            cam.orthographicSize = Mathf.Lerp(targetSize, originalSize, progress);

            // Add shake effect during release
            Vector3 randomOffset = Random.insideUnitSphere * shakeIntensity * (1f - progress);
            randomOffset.z = 0;
            currentShakeOffset = randomOffset; // Update the offset

            yield return null;
        }

        // Ensure we're back to original state
        cam.orthographicSize = originalSize;
        currentShakeOffset = Vector3.zero; // Reset offset after shake

        isEffectActive = false;
    }

    void OnDestroy()
    {
        // Reset camera to original state when destroyed
        if (cam != null)
        {
            cam.orthographicSize = originalSize;
        }
        currentShakeOffset = Vector3.zero; // Ensure offset is reset
    }
}
