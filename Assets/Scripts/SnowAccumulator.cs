using UnityEngine;

public class SnowAccumulator : MonoBehaviour
{
    // Snow accumulation parameters.
    public float accumulationSpeed = 0.01f;      // Snow increase per second.
    public float targetSnowAmount = 0.6f;          // Maximum snow value during event.
    public float initialSnowAmount = 0.3f;         // Base snow value.
    public float clearanceSpeed = 0.005f;          // Snow clearance speed when not snowing.
    public float shakeClearAmount = 0.05f;         // Amount of snow removed per second during shake.

    private Material instanceMat;

    void Start()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            // Create an instance so each object updates its own material.
            instanceMat = rend.material;
            instanceMat.SetFloat("_SnowAmount", initialSnowAmount);
        }
    }

    void Update()
    {
        if (instanceMat == null || !instanceMat.HasProperty("_SnowAmount"))
            return;

        float currentSnow = instanceMat.GetFloat("_SnowAmount");

        if (SnowEventManager.clearSnowNow)
        {
            // Reduce snow by shakeClearAmount but do not go below initialSnowAmount.
            currentSnow = Mathf.Max(currentSnow - shakeClearAmount * Time.deltaTime, initialSnowAmount);
        }
        else if (SnowEventManager.isSnowing)
        {
            // Increase snow gradually toward the target.
            currentSnow = Mathf.MoveTowards(currentSnow, targetSnowAmount, accumulationSpeed * Time.deltaTime);
        }
        else
        {
            // Slowly clear snow when not snowing.
            currentSnow = Mathf.MoveTowards(currentSnow, initialSnowAmount, clearanceSpeed * Time.deltaTime);
        }

        instanceMat.SetFloat("_SnowAmount", currentSnow);
    }
}
