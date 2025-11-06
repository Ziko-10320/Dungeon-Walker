using UnityEngine;
using System.Collections;

public class CreditsScroller : MonoBehaviour
{
    public RectTransform creditsTextRect; // The text object to scroll
    public float scrollSpeed = 50f;       // How fast it scrolls (pixels per second)
    public float endYPosition = 2500f;    // When to stop scrolling

    public void StartCredits()
    {
        StartCoroutine(ScrollCreditsRoutine());
    }

    private IEnumerator ScrollCreditsRoutine()
    {
        // Start with the text at the bottom
        Vector2 startPos = new Vector2(creditsTextRect.anchoredPosition.x, -creditsTextRect.rect.height);
        creditsTextRect.anchoredPosition = startPos;

        // The target position is high above the screen
        Vector2 endPos = new Vector2(creditsTextRect.anchoredPosition.x, endYPosition);

        // Keep scrolling until the text reaches the end position
        while (creditsTextRect.anchoredPosition.y < endPos.y)
        {
            creditsTextRect.anchoredPosition += Vector2.up * scrollSpeed * Time.deltaTime;
            yield return null; // Wait for the next frame
        }

        Debug.Log("Credits finished.");
        // Here you could load the main menu after a delay
        // StartCoroutine(ReturnToMenu(5f));
    }

    // Optional: A function to return to the main menu after credits
    private IEnumerator ReturnToMenu(float delay)
    {
        yield return new WaitForSeconds(delay);
        // Replace "MainMenuScene" with the actual name of your main menu scene
        // UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
    }
}
