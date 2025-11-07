using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem; // Needed for the new input system

public class CreditsScroller : MonoBehaviour
{
    public RectTransform creditsTextRect;
    public float scrollSpeed = 50f;
    public float fastScrollMultiplier = 3f; // How much faster it scrolls when holding
    public float endYPosition = 2500f;
    public float returnToMenuDelay = 5f; // Time to wait before returning to menu

    public void StartCredits()
    {
        // We now start the master routine that handles everything
        StartCoroutine(FullCreditsSequence());
    }

    private IEnumerator FullCreditsSequence()
    {
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 0f; // Start fully transparent
            float timer = 0f;
            while (timer < 1f) // Fade in over 1 second
            {
                timer += Time.deltaTime;
                cg.alpha = timer; // Fade from 0 to 1
                yield return null;
            }
            cg.alpha = 1f; // Ensure it's fully visible
        }
        // Start with the text at the bottom
        Vector2 startPos = new Vector2(creditsTextRect.anchoredPosition.x, -creditsTextRect.rect.height);
        creditsTextRect.anchoredPosition = startPos;

        Vector2 endPos = new Vector2(creditsTextRect.anchoredPosition.x, endYPosition);

        while (creditsTextRect.anchoredPosition.y < endPos.y)
        {
            // --- NEW: Check for input to speed up ---
            float currentSpeed = scrollSpeed;
            if (Mouse.current.leftButton.isPressed || (Touchscreen.current != null && Touchscreen.current.primaryTouch.isInProgress))
            {
                currentSpeed *= fastScrollMultiplier;
            }
            // ----------------------------------------

            creditsTextRect.anchoredPosition += Vector2.up * currentSpeed * Time.deltaTime;
            yield return null;
        }

        Debug.Log("Credits finished.");

        // --- NEW: Hide the panel and return to menu ---
        StartCoroutine(EndCreditsSequence());
    }

    private IEnumerator EndCreditsSequence()
    {
        // Wait a moment before starting the fade out
        yield return new WaitForSeconds(1f);

        // Optional: Fade out the credits panel
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg != null)
        {
            float timer = 0f;
            while (timer < 1f)
            {
                timer += Time.deltaTime;
                cg.alpha = 1f - timer;
                yield return null;
            }
        }

        // Deactivate the panel
        gameObject.SetActive(false);

        // Now, start the countdown to return to the main menu
        StartCoroutine(ReturnToMenu(returnToMenuDelay));
    }

    private IEnumerator ReturnToMenu(float delay)
    {
        yield return new WaitForSeconds(delay);
        // Load the scene at build index 0 (your main menu)
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
}
