using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostEffect : MonoBehaviour
{
    [Header("Ghost Effect Settings")]
    [SerializeField] private float ghostSpawnInterval = 0.05f; // Time between spawning each ghost
    [SerializeField] private float ghostLifetime = 0.5f; // How long a ghost stays on screen
    [SerializeField] private Color ghostColor = new Color(1f, 1f, 1f, 0.5f); // The color and transparency of the ghost

    private bool isGhosting = false;
    private float timer;

    // We get all sprite renderers from the player's children at the start
    private SpriteRenderer[] playerSpriteRenderers;

    void Awake()
    {
        // Find all SpriteRenderer components in this GameObject and its children
        playerSpriteRenderers = GetComponentsInChildren<SpriteRenderer>();
    }

    void Update()
    {
        if (isGhosting)
        {
            if (timer <= 0)
            {
                SpawnGhost();
                timer = ghostSpawnInterval;
            }
            else
            {
                timer -= Time.deltaTime;
            }
        }
    }

    public void StartGhostEffect()
    {
        isGhosting = true;
        timer = 0; // Spawn the first ghost immediately
    }

    public void StopGhostEffect()
    {
        isGhosting = false;
    }

    private void SpawnGhost()
    {
        // Create an empty parent object for the entire ghost snapshot
        GameObject ghostParent = new GameObject("Ghost Parent");
        ghostParent.transform.position = transform.position;
        ghostParent.transform.rotation = transform.rotation;
        ghostParent.transform.localScale = transform.localScale;

        // Iterate through each of the player's sprite renderers
        foreach (SpriteRenderer playerPartSprite in playerSpriteRenderers)
        {
            // Skip any disabled parts
            if (!playerPartSprite.enabled) continue;

            // Create a new child GameObject for this specific part
            GameObject ghostPart = new GameObject("Ghost Part");
            ghostPart.transform.SetParent(ghostParent.transform);

            // Copy the transform properties from the player part to the ghost part
            ghostPart.transform.position = playerPartSprite.transform.position;
            ghostPart.transform.rotation = playerPartSprite.transform.rotation;
            ghostPart.transform.localScale = playerPartSprite.transform.localScale;

            // Add a SpriteRenderer to the ghost part and copy the sprite properties
            SpriteRenderer ghostPartSprite = ghostPart.AddComponent<SpriteRenderer>();
            ghostPartSprite.sprite = playerPartSprite.sprite;
            ghostPartSprite.color = ghostColor;
            ghostPartSprite.sortingLayerID = playerPartSprite.sortingLayerID;
            ghostPartSprite.sortingOrder = playerPartSprite.sortingOrder - 1; // Render ghost behind the player
        }

        // Destroy the entire ghost parent object after its lifetime
        Destroy(ghostParent, ghostLifetime);

        // Optional: Add a fade-out effect
        // You can add a script to the ghostParent that fades the alpha of all its children's sprites over time.
        StartCoroutine(FadeGhost(ghostParent));
    }

    // Coroutine to handle the fading effect
    private IEnumerator FadeGhost(GameObject ghostParent)
    {
        SpriteRenderer[] ghostSprites = ghostParent.GetComponentsInChildren<SpriteRenderer>();
        float elapsedTime = 0f;

        while (elapsedTime < ghostLifetime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / ghostLifetime); // Calculate new alpha

            foreach (SpriteRenderer sr in ghostSprites)
            {
                if (sr != null)
                {
                    Color newColor = ghostColor;
                    newColor.a = alpha * ghostColor.a; // Apply the fading alpha to the original ghost alpha
                    sr.color = newColor;
                }
            }
            yield return null; // Wait for the next frame
        }
    }
}
