using UnityEngine;
using UnityEngine.Events;
public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Death Settings")]
    public float destroyDelay = 2f; // Délai avant destruction après la mort
    public GameObject deathEffect; // Effet visuel de mort (optionnel)

    [Header("Events")]
    public UnityEvent<GameObject> OnDeath; // Événement déclenché à la mort
    public UnityEvent<float> OnHealthChanged; // Événement déclenché quand la santé change

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth);
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float healAmount)
    {
        if (isDead) return;

        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth);
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log($"{gameObject.name} est mort!");

        // Déclencher l'événement de mort
        OnDeath?.Invoke(gameObject);

        // Jouer l'effet de mort si défini
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, transform.rotation);
        }

        // Désactiver les composants de mouvement/IA si présents
        DisableEnemyComponents();

        // Détruire l'objet après le délai
        Destroy(gameObject, destroyDelay);
    }

    void DisableEnemyComponents()
    {
        // Désactiver les composants communs d'ennemi
        var rigidbody = GetComponent<Rigidbody>();
        if (rigidbody != null) rigidbody.isKinematic = true;

        var collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = false;

        // Désactiver les scripts d'IA ou de mouvement
        var aiScripts = GetComponents<MonoBehaviour>();
        foreach (var script in aiScripts)
        {
            if (script != this && script.enabled) // Ne pas désactiver ce script
            {
                script.enabled = false;
            }
        }
    }

    // Méthode pour tuer instantanément l'ennemi
    public void KillInstantly()
    {
        currentHealth = 0;
        Die();
    }

    // Méthode pour vérifier si l'ennemi est mort
    public bool IsDead()
    {
        return isDead;
    }

    // Méthode pour obtenir le pourcentage de santé
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }
}

