using UnityEngine;
using Photon.Pun;
using Photon.Realtime; // Très important pour les Callbacks

// 2. Héritez de MonoBehaviourPunCallbacks pour accéder aux événements de Photon.
public class SpawnPlayers : MonoBehaviourPunCallbacks
{
    [Header("Player Prefabs")]
    [Tooltip("Faites glisser ici le PRÉFABRIQUÉ de votre joueur EN LIGNE. Il doit être dans un dossier 'Resources'.")]
    public GameObject onlinePlayerPrefab;
    public GameObject onlinePlayerPrefab2;

    [Header("Spawn Settings")]
    [Tooltip("Le point de spawn pour le premier joueur (l'hôte de la salle).")]
    public Transform spawnPoint1;
    [Tooltip("Le point de spawn pour le deuxième joueur (et les suivants).")]
    public Transform spawnPoint2;

    // Cette fonction est appelée par Unity dès que l'objet est créé.
    void Start()
    {
        // On vérifie si on est bien connecté à une salle Photon.
        if (PhotonNetwork.IsConnectedAndReady)
        {
            // Si c'est le cas, on fait apparaître notre propre joueur.
            SpawnMyPlayer();
        }
        else
        {
            Debug.LogError("SpawnPlayers: Non connecté à une salle Photon. Impossible de faire apparaître le joueur.");
        }
    }

    // 3. On s'abonne et se désabonne aux événements de Photon pour une gestion propre.
    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    /// <summary>
    /// Cette fonction est appelée automatiquement par Photon chaque fois qu'un NOUVEAU joueur
    /// rejoint la salle APRES nous. C'est utile pour afficher des messages ou mettre à jour une liste de joueurs.
    /// </summary>
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log(newPlayer.NickName + " a rejoint la salle !");
    }

    /// <summary>
    /// La fonction qui gère l'instanciation de NOTRE propre joueur sur le réseau.
    /// </summary>
    private void SpawnMyPlayer()
    {
        // On utilise le numéro d'acteur de Photon pour savoir qui nous sommes.
        // L'hôte est toujours le numéro 1. L'invité est le numéro 2.
        int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        Debug.Log("Mon numéro d'acteur est : " + actorNumber);

        // --- MODIFICATION : On choisit le préfabriqué ET le point de spawn ---
        GameObject prefabToSpawn = (actorNumber == 1) ? onlinePlayerPrefab : onlinePlayerPrefab2;
        Transform spawnPointToUse = (actorNumber == 1) ? spawnPoint1 : spawnPoint2;

        // Sécurité : on vérifie que le préfabriqué à utiliser a bien été assigné.
        if (prefabToSpawn == null)
        {
            Debug.LogError($"SpawnPlayers: Le préfabriqué pour le joueur {actorNumber} n'est pas assigné dans l'inspecteur !");
            return;
        }

        // Sécurité : on vérifie que le point de spawn a bien été assigné.
        if (spawnPointToUse == null)
        {
            Debug.LogWarning($"Le point de spawn pour le joueur {actorNumber} n'est pas défini. Utilisation de la position par défaut.");
            spawnPointToUse = this.transform;
        }

        // On instancie le bon préfabriqué au bon endroit.
        PhotonNetwork.Instantiate(prefabToSpawn.name, spawnPointToUse.position, spawnPointToUse.rotation);

        Debug.Log($"Le joueur {actorNumber} a été instancié avec le préfabriqué '{prefabToSpawn.name}'.");
    }
}
