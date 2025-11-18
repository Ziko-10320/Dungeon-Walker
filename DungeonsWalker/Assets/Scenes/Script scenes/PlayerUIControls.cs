using UnityEngine;
using UnityEngine.UI; // --- AJOUT IMPORTANT : Pour accéder au Graphic Raycaster ---
using Photon.Pun;

public class PlayerUIController : MonoBehaviour
{
    [Tooltip("Faites glisser ici le GameObject du Canvas qui contient les contrôles mobiles.")]
    public GameObject mobileControlsCanvas;

    private PhotonView view;

    void Awake()
    {
        view = GetComponent<PhotonView>();

        if (view == null)
        {
            Debug.LogError("PlayerUIController a besoin d'un composant PhotonView sur le même objet ou un parent.");
            return;
        }

        // --- LOGIQUE AMÉLIORÉE ---
        // Si ce n'est pas mon personnage...
        if (!view.IsMine)
        {
            // ...je désactive son Canvas pour ne pas le voir.
            if (mobileControlsCanvas != null)
            {
                mobileControlsCanvas.SetActive(false);
                Debug.Log("Canvas de l'autre joueur désactivé sur ma machine.");
            }

            // --- AJOUT DE SÉCURITÉ CRUCIAL ---
            // Je cherche aussi le composant 'Graphic Raycaster' sur son Canvas
            // et je le désactive manuellement.
            // Cela garantit à 100% qu'il ne pourra pas intercepter mes clics.
            GraphicRaycaster raycaster = mobileControlsCanvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                raycaster.enabled = false;
                Debug.Log("Graphic Raycaster de l'autre joueur désactivé sur ma machine.");
            }
        }
        else // Si c'est MON personnage...
        {
            // ...je m'assure que mon propre Canvas et mon Raycaster sont bien actifs,
            // au cas où ils auraient été désactivés dans l'éditeur.
            if (mobileControlsCanvas != null)
            {
                mobileControlsCanvas.SetActive(true);
                GraphicRaycaster raycaster = mobileControlsCanvas.GetComponent<GraphicRaycaster>();
                if (raycaster != null)
                {
                    raycaster.enabled = true;
                }
            }
        }
    }
}
