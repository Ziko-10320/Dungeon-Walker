using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenuController : MonoBehaviour
{
    // This is your existing function for the single-player "Start" button
    public void OnStartClick()
    {
        SceneManager.LoadScene(1);
    }

    // This is your existing function for the "Online" button
    public void OnOnlineClick()
    {
        SceneManager.LoadScene(3);
    }

    // --- THE OnShopClick() FUNCTION IS NO LONGER NEEDED HERE ---

    // This is your existing function for the "Exit" button
    public void OnExitClick()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }
}
