using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletBehavior : MonoBehaviour
{
    [SerializeField] private float BulletSpeed = 15f;
    [SerializeField] private float destroyTime = 3f;
    [SerializeField] private LayerMask collisionLayers; // الطبقات التي ستتفاعل معها الرصاصة
    [SerializeField] private GameObject waterExplosionParticleSystem; // نظام الجسيمات للانفجار

    private Rigidbody2D rb;
    private Vector2 moveDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        SetDestroyTime();
        SetStraightVelocity();
    }

    public void SetDirection(Vector2 direction)
    {
        moveDirection = direction.normalized;
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

    // دالة يتم استدعاؤها عند حدوث تصادم 2D
    private void OnTriggerEnter2D(Collider2D other)
    {
        // التحقق مما إذا كانت الطبقة التي حدث معها التصادم موجودة في collisionLayers
        if (((1 << other.gameObject.layer) & collisionLayers) != 0)
        {
            // تدمير الرصاصة.
            // عند تدميرها، سيتم استدعاء OnDestroy() تلقائيًا لتشغيل الجسيمات.
            Destroy(gameObject);
        }
    }

    // دالة يتم استدعاؤها عندما يتم تدمير كائن اللعبة
    // هذا سيتم استدعاؤه سواء تم تدمير الرصاصة بسبب التصادم أو بسبب انتهاء destroyTime
    private void OnDestroy()
    {
        // تشغيل نظام الجسيمات للانفجار
        if (waterExplosionParticleSystem != null)
        {
            // إنشاء نظام الجسيمات في موقع الرصاصة الحالي
            // تأكد من أن نظام الجسيمات لديه خاصية "Play On Awake" مفعلة
            Instantiate(waterExplosionParticleSystem, transform.position, Quaternion.identity);
        }
    }

    // إذا كنت تستخدم Colliders وليس Triggers، استخدم OnCollisionEnter2D بدلاً من OnTriggerEnter2D
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & collisionLayers) != 0)
        {
            // تدمير الرصاصة.
            // عند تدميرها، سيتم استدعاء OnDestroy() تلقائيًا لتشغيل الجسيمات.
            Destroy(gameObject);
        }
    }
    
}
