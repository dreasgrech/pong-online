using UnityEngine;
using System.Collections;

public class ColliderEventBubbler2D : MonoBehaviour
{
    public string methodName = "HitPaddleCollider";

    void OnCollisionEnter2D(Collision2D other)
    {
        Debug.Log("COLLISION");
        SendMessageUpwards(methodName, other, SendMessageOptions.DontRequireReceiver);
    }
}
