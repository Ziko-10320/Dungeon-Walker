using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            instance = this;
        }

        DontDestroyOnLoad(gameObject);
    }

    // This function will be called every time a new scene is finished loading.
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // --- This is the modified part ---

        // Get the index of the newly loaded scene.
        int sceneIndex = scene.buildIndex;

        // Check if the scene's index is NOT 0 (Lobby) AND NOT 1 (CharacterSelection).
        if (sceneIndex != 0 && sceneIndex != 1)
        {
            // If we are in any other scene (e.g., index 2, 3, etc.), destroy the music player.
            instance = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
