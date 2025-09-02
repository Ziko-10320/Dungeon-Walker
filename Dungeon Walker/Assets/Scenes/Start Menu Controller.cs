using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenuController : MonoBehaviour
{
    // This is your existing function for the single-player "Start" button
    public void OnStartClick()
    {
        // This probably loads your single-player game scene
        SceneManager.LoadScene("SampleScene");
    }

    // --- ADD THIS NEW FUNCTION ---
    // This function will be called by your new "Online" button
    public void OnOnlineClick()
    {
        // This will load our new scene dedicated to online multiplayer
        SceneManager.LoadScene("OnlineLobbyScene");
    }
    // -----------------------------

    // This is your existing function for the "Exit" button
    public void OnExitClick()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }
}
