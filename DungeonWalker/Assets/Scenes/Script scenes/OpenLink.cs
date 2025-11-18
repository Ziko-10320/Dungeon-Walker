using UnityEngine;

public class OpenLink : MonoBehaviour
{
    public void OpenYouTubeChannel(string url)
    {
        Application.OpenURL(url);
    }
}
