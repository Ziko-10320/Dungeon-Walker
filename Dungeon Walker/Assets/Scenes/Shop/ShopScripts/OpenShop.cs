using UnityEngine;
using UnityEngine.SceneManagement;

public class OpenShopButton : MonoBehaviour
{
    [SerializeField] private string shopSceneName = "ShopScene"; // name of your shop scene

    public void OpenShop()
    {
        SceneManager.LoadScene(shopSceneName);
    }
}
