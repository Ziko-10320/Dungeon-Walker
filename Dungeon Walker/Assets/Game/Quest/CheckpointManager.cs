using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
  
public class CheckpointManager : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    public List<Checkpoint> checkpoints = new List<Checkpoint>();
    public Transform player;
    public bool loopCheckpoints = true;

    [Header("Coin Settings")]
    public int coinsPerCheckpoint = 10;
    public Button interactButton;

    [Header("Score Settings")]
    public int scorePerCheckpoint = 100;
    public int scorePerLoop = 1;

    [Header("Arrow Settings")]
    public ArrowPointer arrowPointer;

    [Header("Events")]
    public UnityEvent<int> OnCheckpointReached;
    public UnityEvent<int> OnLoopCompleted;
    public UnityEvent<int> OnScoreChanged;
    public UnityEvent<float> OnCheckpointTimerUpdate;
    public UnityEvent<bool> OnShowEButton; // Show/hide E button
    public UnityEvent<string> OnEButtonTextUpdate; // Update E button text

    private int currentCheckpointIndex = 0;
    private int totalScore = 0;
    private float currentCheckpointTimer = 0f;
    private bool isPlayerAtCheckpoint = false;
    private bool isTimerRunning = false;
    private Coroutine checkpointTimerCoroutine;

    public int CurrentCheckpointIndex => currentCheckpointIndex;
    public int TotalScore => totalScore;
    public Vector3 CurrentTargetPosition => currentCheckpointIndex < checkpoints.Count ? checkpoints[currentCheckpointIndex].transform.position : Vector3.zero;
    public bool IsTimerRunning => isTimerRunning;

    [Header("Player References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private L3antixHealth l3antixHealth;

    void Start()
    {
        if (interactButton != null)
        {
            if (interactButton.interactable)
            {
                interactButton.gameObject.SetActive(false);
                interactButton.interactable = false;
            }

            // 👇 Hook UI button directly
            interactButton.onClick.AddListener(OnInteractButtonPressed);
        }

        InitializeCheckpoints();
        UpdateArrowTarget();
        OnShowEButton?.Invoke(false);
        OnCheckpointTimerUpdate?.Invoke(0f);

        // 👇 New: auto-assign the active player
        if (player == null || (playerHealth == null && l3antixHealth == null))
        {
            GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");

            if (allPlayers.Length == 0)
            {
                Debug.LogError("⚠️ No GameObject with tag 'Player' found in the scene!");
            }
            else
            {
                foreach (GameObject p in allPlayers)
                {
                    if (p.activeInHierarchy)
                    {
                        player = p.transform;

                        // Try first type of health
                        playerHealth = p.GetComponent<PlayerHealth>();

                        // If not found, try the second one
                        if (playerHealth == null)
                        {
                            l3antixHealth = p.GetComponent<L3antixHealth>();
                        }

                        break;
                    }
                }
            }
        }
    }

    void Update()
    {
        if (player == null || checkpoints.Count == 0) return;

        if (currentCheckpointIndex >= checkpoints.Count)
        {
            return;
        }

        Checkpoint currentCheckpoint = checkpoints[currentCheckpointIndex];
        if (currentCheckpoint == null) return;

        float distance = Vector3.Distance(player.position, currentCheckpoint.transform.position);
        bool playerIsNowInRadius = (distance <= currentCheckpoint.Radius);

        if (playerIsNowInRadius && !isPlayerAtCheckpoint)
        {
            isPlayerAtCheckpoint = true;
            currentCheckpoint.SetPlayerInRadius(true);
            currentCheckpoint.PlayRadiusEnterSound();
            UpdateEButtonVisibility();
            Debug.Log($"Player entered checkpoint {currentCheckpointIndex + 1} radius.");
        }
        else if (!playerIsNowInRadius && isPlayerAtCheckpoint)
        {
            isPlayerAtCheckpoint = false;
            currentCheckpoint.SetPlayerInRadius(false);
            OnShowEButton?.Invoke(false);

            if (!currentCheckpoint.QuestStarted && !isTimerRunning)
            {
                currentCheckpoint.PlayRadiusExitPrematureSound();
            }
            Debug.Log($"Player left checkpoint {currentCheckpointIndex + 1} radius.");
        }

        // 👇 Optional: pressing E simulates button click
        if (Input.GetKeyDown(KeyCode.E) && interactButton != null && interactButton.gameObject.activeSelf)
        {
            interactButton.onClick.Invoke();
        }
    }

    void InitializeCheckpoints()
    {
        for (int i = 0; i < checkpoints.Count; i++)
        {
            if (checkpoints[i] != null)
            {
                checkpoints[i].SetCheckpointIndex(i);
                checkpoints[i].SetActive(i == 0);
            }
        }

        Debug.Log($"Initialized {checkpoints.Count} checkpoints. First target: {(checkpoints.Count > 0 ? checkpoints[0].name : "None")}");
    }

    void UpdateEButtonVisibility()
    {
        if (currentCheckpointIndex >= checkpoints.Count || interactButton == null) return;

        Checkpoint currentCheckpoint = checkpoints[currentCheckpointIndex];
        bool shouldBeVisible = false;

        if (isPlayerAtCheckpoint)
        {
            if (!currentCheckpoint.QuestStarted)
            {
                shouldBeVisible = true;
            }
            else if (currentCheckpoint.TimerEnded)
            {
                shouldBeVisible = true;
            }
        }

        interactButton.gameObject.SetActive(shouldBeVisible);
        interactButton.interactable = shouldBeVisible;
    }

    // --- Public: called by UI or E key ---
    public void OnInteractButtonPressed()
    {
        if (currentCheckpointIndex >= checkpoints.Count) return;
        Checkpoint currentCheckpoint = checkpoints[currentCheckpointIndex];

        if (!isPlayerAtCheckpoint) return;

        if (!currentCheckpoint.QuestStarted)
        {
            currentCheckpoint.StartQuest();
            currentCheckpointTimer = currentCheckpoint.TimeToStay;
            isTimerRunning = true;

            if (interactButton != null)
            {
                if (interactButton.interactable)
                {
                    interactButton.gameObject.SetActive(false);
                    interactButton.interactable = false;
                }
            }

            if (checkpointTimerCoroutine != null) StopCoroutine(checkpointTimerCoroutine);
            checkpointTimerCoroutine = StartCoroutine(CheckpointTimer());
        }
        else if (currentCheckpoint.TimerEnded)
        {
            ReachCheckpoint();
        }
    }

    IEnumerator CheckpointTimer()
    {
        while (currentCheckpointTimer > 0)
        {
            yield return null;
            currentCheckpointTimer -= Time.deltaTime;
            OnCheckpointTimerUpdate?.Invoke(currentCheckpointTimer);
        }

        if (currentCheckpointIndex < checkpoints.Count)
        {
            Checkpoint currentCheckpoint = checkpoints[currentCheckpointIndex];
            currentCheckpoint.EndTimer();
            isTimerRunning = false;

            foreach (CheckpointManager mgr in FindObjectsOfType<CheckpointManager>())
            {
                if (mgr != null)
                {
                    mgr.UpdateEButtonVisibility();
                }
            }

            Debug.Log($"Timer ended for checkpoint {currentCheckpointIndex + 1}");
        }
    }

    void ReachCheckpoint()
    {
        if (player != null)
        {
            InGamePowerUpManager tempPowerUpManager = player.GetComponent<InGamePowerUpManager>();
            if (tempPowerUpManager != null)
            {
                tempPowerUpManager.RemoveAllTemporaryPowerUps();
            }
        }
        if (playerHealth != null)
        {
            playerHealth.RestoreShieldAtCheckpoint();
        }
        else if (l3antixHealth != null)
        {
            l3antixHealth.RestoreShieldAtCheckpoint();
        }
        if (interactButton != null)
        {
            if (interactButton.interactable)
            {
                interactButton.gameObject.SetActive(false);
                interactButton.interactable = false;
            }
        }
        if (checkpointTimerCoroutine != null) StopCoroutine(checkpointTimerCoroutine);
        isTimerRunning = false;

        if (WalletManager.Instance != null)
        {
            WalletManager.Instance.AddCoins(coinsPerCheckpoint);
        }
        else
        {
            Debug.LogWarning("WalletManager.Instance not found. Cannot add coins.");
        }

        if (PlayerStatsManager.Instance != null)
        {
            PlayerStatsManager.Instance.AddCoins(coinsPerCheckpoint);
        }

        PowerUpManager powerUpManager = player.GetComponent<PowerUpManager>();
        if (powerUpManager != null && powerUpManager.HasPowerUp(PowerUpType.Invisibility))
        {
            PlayerInvisibility invis = player.GetComponent<PlayerInvisibility>();
            if (invis != null)
            {
                float duration = -1f;
                var inv = InventoryManager.Instance;
                if (inv != null)
                {
                    foreach (PowerUpData pd in inv.equippedPowerUps)
                    {
                        if (pd != null && pd.type == PowerUpType.Invisibility)
                        {
                            duration = pd.effectValue;
                            break;
                        }
                    }
                }

                if (duration > 0f) invis.ActivateInvisibility(duration);
                else invis.ActivateInvisibility();
            }
            else
            {
                Debug.LogWarning("CheckpointManager: PlayerInvisibility not found on player.");
            }
        }
        PowerUpManagerL3antix PowerUpManagerL3antix = player.GetComponent<PowerUpManagerL3antix>();
        if (PowerUpManagerL3antix != null && PowerUpManagerL3antix.HasPowerUp(PowerUpType.Invisibility))
        {
            PlayerInvisibility3antix invis3antix = player.GetComponent<PlayerInvisibility3antix>();
            if (invis3antix != null)
            {
                float duration = -1f;
                var inv = InventoryManager.Instance;
                if (inv != null)
                {
                    foreach (PowerUpData pd in inv.equippedPowerUps)
                    {
                        if (pd != null && pd.type == PowerUpType.Invisibility)
                        {
                            duration = pd.effectValue;
                            break;
                        }
                    }
                }

                if (duration > 0f) invis3antix.ActivateInvisibility(duration);
                else invis3antix.ActivateInvisibility();
            }
            else
            {
                Debug.LogWarning("CheckpointManager: PlayerInvisibility not found on player.");
            }
        }

        OnShowEButton?.Invoke(false);

        checkpoints[currentCheckpointIndex].SetReached();

        AddScore(scorePerCheckpoint);

        OnCheckpointReached?.Invoke(currentCheckpointIndex);

        Debug.Log($"Checkpoint {currentCheckpointIndex + 1} reached! Score: +{scorePerCheckpoint}");

        currentCheckpointIndex++;

        if (currentCheckpointIndex >= checkpoints.Count)
        {
            AddScore(scorePerLoop);
            OnLoopCompleted?.Invoke(totalScore);
            Debug.Log($"Loop completed! Score: +{scorePerLoop}. Total Score: {totalScore}");

            currentCheckpointIndex = 0;
            foreach (Checkpoint cp in checkpoints)
            {
                cp.ResetCheckpoint();
            }
            checkpoints[currentCheckpointIndex].SetActive(true);
        }
        else
        {
            checkpoints[currentCheckpointIndex].SetActive(true);
            Debug.Log($"Next target: {checkpoints[currentCheckpointIndex].name}");
        }

        UpdateArrowTarget();
    }

    void AddScore(int points)
    {
        totalScore += points;
        OnScoreChanged?.Invoke(totalScore);
    }

    void UpdateArrowTarget()
    {
        if (arrowPointer != null && currentCheckpointIndex < checkpoints.Count)
        {
            arrowPointer.SetTarget(checkpoints[currentCheckpointIndex].transform);
        }
        else if (arrowPointer != null)
        {
            arrowPointer.SetTarget(null);
        }
    }

    public void ResetCheckpoints()
    {
        currentCheckpointIndex = 0;
        totalScore = 0;
        isPlayerAtCheckpoint = false;
        isTimerRunning = false;
        if (checkpointTimerCoroutine != null) StopCoroutine(checkpointTimerCoroutine);
        currentCheckpointTimer = 0f;
        OnCheckpointTimerUpdate?.Invoke(0f);
        OnShowEButton?.Invoke(false);

        for (int i = 0; i < checkpoints.Count; i++)
        {
            if (checkpoints[i] != null)
            {
                checkpoints[i].ResetCheckpoint();
                checkpoints[i].SetActive(i == 0);
            }
        }

        OnScoreChanged?.Invoke(totalScore);
        UpdateArrowTarget();
        Debug.Log("Checkpoints reset!");
    }

    public void AddCheckpoint(Checkpoint checkpoint)
    {
        if (!checkpoints.Contains(checkpoint))
        {
            checkpoints.Add(checkpoint);
            checkpoint.SetCheckpointIndex(checkpoints.Count - 1);
        }
    }

    public Vector3 GetNextCheckpointPosition()
    {
        if (currentCheckpointIndex < checkpoints.Count)
        {
            return checkpoints[currentCheckpointIndex].transform.position;
        }
        return Vector3.zero;
    }

    public bool HasNextCheckpoint()
    {
        return currentCheckpointIndex < checkpoints.Count;
    }

    public float GetDistanceToCurrentCheckpoint()
    {
        if (player != null && currentCheckpointIndex < checkpoints.Count)
        {
            return Vector3.Distance(player.position, checkpoints[currentCheckpointIndex].transform.position);
        }
        return float.MaxValue;
    }
}
