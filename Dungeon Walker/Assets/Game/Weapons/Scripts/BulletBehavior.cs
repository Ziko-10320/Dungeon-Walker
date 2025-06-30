using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletBehavior : MonoBehaviour
{
    [SerializeField] private float BulletSpeed = 15f;
    [SerializeField] private float destroyTime = 3f;
    private Rigidbody2D rb;
    private Vector2 moveDirection; // New variable to store the direction

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        SetDestroyTime();

        // Set velocity using the stored moveDirection
        SetStraightVelocity();
    }

    public void SetDirection(Vector2 direction)
    {
        moveDirection = direction.normalized; // Normalize to ensure consistent speed
        // Rotate the bullet to face the direction it's moving
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void SetStraightVelocity()
    {
        rb.velocity = moveDirection * BulletSpeed;
    }

    private void SetDestroyTime()
    {
        Destroy(gameObject, destroyTime);
    }
}
