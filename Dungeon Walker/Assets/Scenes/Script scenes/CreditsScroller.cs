using UnityEngine;
using System.Collections;
using UnityEngine.UI; // Needed for the Button

// We can keep the name "CreditsScroller" even though it doesn't scroll anymore.
public class CreditsScroller : MonoBehaviour
{
    [Header("Credits Settings")]
    [Tooltip("The button that appears after the delay.")]
    [SerializeField] private Button closeButton;

    [Tooltip("How many seconds to wait before the close button appears.")]
    [SerializeField] private float delayBeforeButton = 10f;

    // This is the main function that gets called to start the credits.
    public void StartCredits()
    {
        // Make sure the close button is hidden at the start.
        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(false);
        }

        // Start the coroutine that will wait and then show the button.
        StartCoroutine(ShowCloseButtonAfterDelay());
    }

    private IEnumerator ShowCloseButtonAfterDelay()
    {
        // Wait for the specified amount of time.
        yield return new WaitForSeconds(delayBeforeButton);

        // After waiting, show the close button.
        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(true);
        }
    }

    // This function will be called by the close button itself.
    public void CloseCreditsPanel()
    {
        // Simply deactivate the entire panel this script is on.
        gameObject.SetActive(false);

        // Optional: If you still want to go back to the main menu after closing.
        // You can uncomment the line below.
        // UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
}
