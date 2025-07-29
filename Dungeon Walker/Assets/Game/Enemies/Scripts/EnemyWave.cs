using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyWaveData
{
    public EnemyData enemyType;
    public int minCount = 1;
    public int maxCount = 5;
    public float spawnDelay = 0.5f; // Délai entre chaque spawn d'ennemi de ce type

    public int GetRandomCount()
    {
        return Random.Range(minCount, maxCount + 1);
    }
}

[System.Serializable]
public class EnemyWave
{
    [Header("Wave Settings")]
    public string waveName = "Wave 1";
    public List<EnemyWaveData> enemyTypes = new List<EnemyWaveData>();

    [Header("Spawn Settings")]
    public float delayBeforeWave = 0f;
    public float delayBetweenEnemyTypes = 1f;

    [Header("Conditions")]
    public int requiredScoreToTrigger = 0; // Score requis pour déclencher cette vague
    public bool canRepeat = true; // Si cette vague peut se répéter

    // Calculer le nombre total d'ennemis qui seront spawnés dans cette vague
    public int GetTotalEnemyCount()
    {
        int total = 0;
        foreach (var enemyType in enemyTypes)
        {
            total += enemyType.GetRandomCount();
        }
        return total;
    }

    // Vérifier si cette vague peut être déclenchée avec le score actuel
    public bool CanTrigger(int currentScore)
    {
        return currentScore >= requiredScoreToTrigger;
    }
}
