using UnityEngine;

// This attribute allows us to create instances of this class from the Unity Editor menu.
[CreateAssetMenu(fileName = "New PowerUp", menuName = "Game/PowerUp Data")]
public class PowerUpData : ScriptableObject
{
    [Header("Info")]
    public string powerUpName;
    [TextArea] public string description;
    public Sprite icon;
    public int price;

    [Header("Gameplay Effect")]
    // We can use an enum to define the type of power-up.
    // This makes it easy to add more types later.
    public PowerUpType type;
    public float effectValue; // e.g., for a speed boost, this could be 1.5 (for 50% extra speed)
}

// An enumeration to define the different kinds of power-ups we can have.
// We can easily add more here, like Shield, DoubleJump, Magnet, etc.
public enum PowerUpType
{
    SpeedBoost,
    AnotherBoost // Placeholder for your second boost type
}
