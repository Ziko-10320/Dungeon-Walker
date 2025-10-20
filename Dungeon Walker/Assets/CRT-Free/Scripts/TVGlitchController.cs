using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TVGlitchController : MonoBehaviour
{
    public Image tvEffectImage;

    private Material tvMaterial;
    private CanvasGroup canvasGroup;

    // --- Tweak these for the REALISTIC glitch ---
    public float minWaitTime = 3.0f;
    public float maxWaitTime = 8.0f;

    // --- Glitch Stutter Settings ---
    public int minGlitches = 3;
    public int maxGlitches = 6;
    public float stutterGap = 0.06f;
    public float glitchDuration = 0.1f;

    // --- Glitch INTENSITY Settings ---
    public float glitchAlpha = 0.9f;
    public Color glitchColor = new Color(0.3f, 0.3f, 0.3f); // NEW: Dark grey color for the glitch
    public float verticalJumpAmount = 0.1f;
    public float desaturationAmount = 0.7f;
    public float staticAmount = 0.9f;

    // Store original values
    private float originalAlpha;
    private Color originalColor; // NEW: Store the original color
    private float originalDesaturation;
    private float originalStatic;

    void Start()
    {
        tvMaterial = tvEffectImage.material;
        canvasGroup = tvEffectImage.GetComponent<CanvasGroup>();

        // Store the normal, calm state of the TV
        originalAlpha = canvasGroup.alpha;
        originalColor = tvEffectImage.color; // NEW: Store the image's starting color
        originalDesaturation = tvMaterial.GetFloat("_Desaturation");
        originalStatic = tvMaterial.GetFloat("_Spread");

        StartCoroutine(GlitchEventLoop());
    }

    IEnumerator GlitchEventLoop()
    {
        while (true)
        {
            float waitTime = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(waitTime);
            StartCoroutine(TriggerStutterGlitch());
        }
    }

    IEnumerator TriggerStutterGlitch()
    {
        int stutterCount = Random.Range(minGlitches, maxGlitches);

        for (int i = 0; i < stutterCount; i++)
        {
            // --- GLITCH STARTS HERE ---
            canvasGroup.alpha = glitchAlpha;
            tvEffectImage.color = glitchColor; // NEW: Set the image color to dark grey

            float yJump = Random.Range(-verticalJumpAmount, verticalJumpAmount);
            tvMaterial.SetTextureOffset("_MainTex", new Vector2(0, yJump));

            tvMaterial.SetFloat("_Desaturation", desaturationAmount);
            tvMaterial.SetFloat("_Spread", staticAmount);

            yield return new WaitForSeconds(glitchDuration);

            // --- GLITCH ENDS HERE ---
            canvasGroup.alpha = originalAlpha;
            tvEffectImage.color = originalColor; // NEW: Return to the original color (white)

            tvMaterial.SetTextureOffset("_MainTex", Vector2.zero);
            tvMaterial.SetFloat("_Desaturation", originalDesaturation);
            tvMaterial.SetFloat("_Spread", originalStatic);

            yield return new WaitForSeconds(stutterGap);
        }
    }
}
