using UnityEngine;
using UnityEngine.EventSystems;

// This script's only job is to send a signal when a touch starts or ends.
public class JoystickBroadcaster : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    // This is a static "event" that any other script can listen to. It's a radio signal.
    public static event System.Action<bool> OnJoystickTouchStateChanged;

    public void OnPointerDown(PointerEventData eventData)
    {
        // When a finger touches this UI element, broadcast "true" (finger is down).
        OnJoystickTouchStateChanged?.Invoke(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // When a finger is lifted from this UI element, broadcast "false" (finger is up).
        OnJoystickTouchStateChanged?.Invoke(false);
    }
}
