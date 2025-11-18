using UnityEngine;

// This is an "abstract" class. It defines what methods a PowerUpManager MUST have,
// but doesn't fill them in. Both of your character managers will inherit from this.
public abstract class BasePowerUpManager : MonoBehaviour
{
    public abstract void ApplyPersistentEffect(PowerUpData data);
    public abstract void RemovePersistentEffect(PowerUpData data);
}
