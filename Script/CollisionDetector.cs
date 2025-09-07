using UnityEngine;
using System;

public class CollisionDetector : MonoBehaviour
{
    public event Action<string> OnCollisionWithTag;

    private void OnTriggerEnter(Collider other)
    {
        OnCollisionWithTag?.Invoke(other.tag);
    }

    private void OnCollisionEnter(Collision collision)
    {
        OnCollisionWithTag?.Invoke(collision.gameObject.tag);
    }
}