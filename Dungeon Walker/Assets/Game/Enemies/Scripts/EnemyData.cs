using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Enemy System/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public GameObject enemyPrefab;
    public string enemyName;
    public float health;
    public float speed;
    // Ajoutez d'autres propriétés spécifiques à l'ennemi ici (ex: damage, attack speed, etc.)
}

