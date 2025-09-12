using UnityEngine;
using System.Collections;

public class PowerUpManager : MonoBehaviour
{
    [Header("Component References")]
    [Tooltip("Faites glisser ici le script de mouvement de votre joueur (ex: L3antixMovement).")]
    [SerializeField] private KritinaMovement playerMovement;
    [SerializeField] private PlayerHealth playerHealth;
    [Header("Speed Boost Power-Up Settings")]
    [Tooltip("Le multiplicateur de vitesse à appliquer au joueur.")]
    public float speedMultiplier = 2f;
    [Tooltip("La durée du boost de vitesse en secondes.")]
    public float speedBoostDuration = 5f;
    [Tooltip("Le système de particules à jouer en boucle pendant le boost.")]
    [SerializeField] private ParticleSystem speedBoostParticles;
    [SerializeField] private ParticleSystem[] instantHealParticles;

    // Variables privées pour gérer l'état
    private float originalMoveSpeed;
    private bool isSpeedBoostActive = false;

    void Awake()
    {
        // S'assurer que la référence au script de mouvement est bien là
        if (playerMovement == null)
        {
            playerMovement = GetComponent<KritinaMovement>();
        }

        if (playerMovement == null)
        {
            Debug.LogError("PowerUpManager: Le script de mouvement du joueur est manquant !");
            enabled = false; // Désactive ce script s'il ne peut pas fonctionner
            return;
        }

        // S'assurer que le système de particules est configuré pour ne pas jouer au démarrage
        if (speedBoostParticles != null)
        {
            speedBoostParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }
    }

    // --- FONCTION PUBLIQUE À ACTIVER PAR LA CARTE (BOUTON UI) ---
    // C'est cette fonction que vous appellerez depuis votre bouton "Carte de Speed Boost".
    public void ActivateSpeedBoost()
    {
        // On ne peut pas activer le boost s'il est déjà actif
        if (isSpeedBoostActive)
        {
            Debug.Log("Speed Boost is already active!");
            return;
        }

        // On lance la coroutine qui va gérer le power-up
        StartCoroutine(SpeedBoostRoutine());
    }

    private IEnumerator SpeedBoostRoutine()
    {
        // --- PHASE D'ACTIVATION ---
        isSpeedBoostActive = true;
        Debug.Log("Speed Boost Activated!");

        // 1. Sauvegarder la vitesse originale du joueur
        originalMoveSpeed = playerMovement.moveSpeed;

        // 2. Appliquer le multiplicateur de vitesse
        playerMovement.moveSpeed *= speedMultiplier;

        // 3. Activer et faire boucler le système de particules
        if (speedBoostParticles != null)
        {
            var mainModule = speedBoostParticles.main;
            mainModule.loop = true; // On s'assure que l'effet boucle
            speedBoostParticles.Play();
        }

        // --- PHASE D'ATTENTE ---
        // On attend pendant la durée définie
        yield return new WaitForSeconds(speedBoostDuration);

        // --- PHASE DE DÉSACTIVATION ---
        Debug.Log("Speed Boost Finished.");

        // 1. Rétablir la vitesse originale du joueur
        playerMovement.moveSpeed = originalMoveSpeed;

        // 2. Arrêter le système de particules
        if (speedBoostParticles != null)
        {
            speedBoostParticles.Stop();
        }

        isSpeedBoostActive = false;
    }

    public void ActivateInstantHeal()
    {
        if (playerHealth == null)
        {
            Debug.LogError("PowerUpManager: PlayerHealth is missing!");
            return;
        }
        foreach (ParticleSystem ps in instantHealParticles)
        {
            if (ps != null)
            {
                ps.Play();
            }
        }
        playerHealth.FullHeal();
        Debug.Log("Instant Heal Activated! Player fully healed.");
    }


}
