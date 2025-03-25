using UnityEngine;
using System.Collections;

public class SnowEventManager : MonoBehaviour
{
    // Timing parameters.
    public float cooldown = 30f;          // Seconds between snow events.
    public float snowDuration = 60f;      // Duration of each snow event in seconds.
    public float eventStartSnowAmount = 0.3f;
    public float eventTargetSnowAmount = 0.6f;

    // Global flags for snow events.
    public static bool isSnowing = false;
    public static bool clearSnowNow = false;

    // Optional particle system for snow.
    public ParticleSystem snowParticles;

    // UI image for snow event gameplay tip.
    public RectTransform snowEventUIImage;
    private Vector3 originalUIPosition;

    void Start()
    {
        // Ensure particle system and UI image are initially disabled.
        if (snowParticles != null)
            snowParticles.Stop();
        if (snowEventUIImage != null)
        {
            originalUIPosition = snowEventUIImage.anchoredPosition;
            snowEventUIImage.gameObject.SetActive(false);
        }
        StartCoroutine(SnowEventCycle());
    }

    IEnumerator SnowEventCycle()
    {
        while (true)
        {
            // Wait for cooldown before starting the event.
            yield return new WaitForSeconds(cooldown);

            // Start snow event.
            isSnowing = true;
            float eventTime = 0f;
            bool uiTriggered = false;

            if (snowEventUIImage != null)
            {
                snowEventUIImage.gameObject.SetActive(true);
                originalUIPosition = snowEventUIImage.anchoredPosition;
            }
            if (snowParticles != null)
                snowParticles.Play();

            while (eventTime < snowDuration)
            {
                // At 30 seconds, trigger UI animation if not done yet.
                if (!uiTriggered && eventTime >= 30f && snowEventUIImage != null)
                {
                    
                    yield return StartCoroutine(LerpUIPosition(snowEventUIImage, originalUIPosition, new Vector3(-261, -77, 0), 1f));
                    yield return new WaitForSeconds(8f);
                    
                    yield return StartCoroutine(LerpUIPosition(snowEventUIImage, snowEventUIImage.anchoredPosition, originalUIPosition, 1f));
                    snowEventUIImage.gameObject.SetActive(false);
                    uiTriggered = true;
                }
                eventTime += 1f;
                yield return new WaitForSeconds(1f);
            }
            isSnowing = false;
            if (snowParticles != null)
                snowParticles.Stop();
            if (snowEventUIImage != null)
                snowEventUIImage.gameObject.SetActive(false);
        }
    }

    // Lerp the gameplay tip UI image's position.
    IEnumerator LerpUIPosition(RectTransform rectTransform, Vector3 startPos, Vector3 endPos, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            rectTransform.anchoredPosition = Vector3.Lerp(startPos, endPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rectTransform.anchoredPosition = endPos;
    }
}
