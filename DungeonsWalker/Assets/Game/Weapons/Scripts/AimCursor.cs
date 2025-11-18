using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AimCursor : MonoBehaviour
{
    [Tooltip("حجم مؤشر التصويب. 1.0 هو الحجم الأصلي للصورة.")]
    [Range(0.1f, 5.0f)] // نطاق للتحكم في الحجم من 0.1 إلى 5.0
    public float cursorSize = 1.0f;

    [Tooltip("إزاحة المؤشر عن الماوس (إذا كنت تريد تعديل موضعه قليلاً).")]
    public Vector2 offset = Vector2.zero;

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("AimCursor: SpriteRenderer component not found on this GameObject!", this);
            enabled = false; // تعطيل السكربت إذا لم يتم العثور على SpriteRenderer
        }
    }

    void Start()
    {
        // إخفاء مؤشر الماوس الافتراضي للنظام
        Cursor.visible = false;
    }

    // *** التغيير هنا: استخدام LateUpdate بدلاً من Update ***
    void LateUpdate()
    {
        // الحصول على موضع الماوس في إحداثيات الشاشة
        Vector2 mouseScreenPosition = Input.mousePosition;

        // تحويل موضع الماوس من إحداثيات الشاشة إلى إحداثيات العالم
        // تأكد من أن الكاميرا الرئيسية (Camera.main) هي الكاميرا الصحيحة
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(mouseScreenPosition);

        // تعيين موضع المؤشر ليكون نفس موضع الماوس في العالم (مع إزالة Z)
        transform.position = new Vector3(mouseWorldPosition.x + offset.x, mouseWorldPosition.y + offset.y, transform.position.z);

        // تعيين حجم المؤشر
        transform.localScale = new Vector3(cursorSize, cursorSize, 1f);
    }

    void OnApplicationQuit()
    {
        // إعادة إظهار مؤشر الماوس الافتراضي عند إغلاق التطبيق
        Cursor.visible = true;
    }

    void OnDisable()
    {
        // إعادة إظهار مؤشر الماوس الافتراضي عند تعطيل الكائن أو السكربت
        Cursor.visible = true;
    }
}
