using UnityEngine;

public class ProjectileMover : MonoBehaviour
{
    private Vector2 _velocity;

    // This function is called by the BatAttackSystem to set the projectile's speed and direction.
    public void Initialize(Vector2 direction, float speed)
    {
        _velocity = direction.normalized * speed;
    }

    // Update is used for frame-by-frame movement.
    void Update()
    {
        // Move the projectile based on its velocity and the time passed.
        // Using Time.deltaTime makes the movement smooth and frame-rate independent.
        transform.position += (Vector3)_velocity * Time.deltaTime;
    }
}