using UnityEngine;
using System.Collections;

public enum PowerUpType
{
    SpeedBoost,
    InstantHeal,
    InstantSuper,
    Shield,
    SoapTrail,
    AcidTrail,
    Revive,
    ReviveUpgraded,
    Invisibility
}

[CreateAssetMenu(fileName = "NewPowerUp", menuName = "PowerUps/PowerUpData")]
public class PowerUpData : ScriptableObject
{
    public PowerUpType type;
    public bool enabledByDefault = false;

    [Header("Speed Boost Settings")]
    public float speedMultiplier = 2f;

    [Header("Other Settings")]
    public float duration = 5f;
    [Header("Info")]
    public string powerUpName;
    [TextArea] public string description;
    public Sprite icon;
    public int price;

    [Header("Gameplay Effect")]
    public float effectValue;
}