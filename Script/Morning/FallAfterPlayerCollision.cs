using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class FallAfterPlayerCollision : MonoBehaviour
{
    [Header("낙하 설정")]
    public float delayBeforeFall = 3f;
    public float fallDistanceBeforeDestroy = 10f;
    public float rotationSpeedMin = -2f;
    public float rotationSpeedMax = 2f;
    public float fallSpeed = 5f;

    private Rigidbody rb;
    private bool isFalling = false;
    private float initialY;
    private Vector3 rotationSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        initialY = transform.position.y;

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            MeshCollider meshCol = col as MeshCollider;
            if (meshCol != null)
            {
                meshCol.convex = true;
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        PlayerCol playerCol = collision.collider.GetComponent<PlayerCol>();
        if (playerCol != null && !isFalling)
        {
            StartCoroutine(FallRoutine());
        }
    }

    private IEnumerator FallRoutine()
    {
        SoundManager.Instance.PlaySFX(SFXCategory.CH1_Spring, SFXSubCategory.Morning, "Trap");

        yield return new WaitForSeconds(delayBeforeFall);

        rotationSpeed = new Vector3(
            Random.Range(rotationSpeedMin, rotationSpeedMax),
            Random.Range(rotationSpeedMin, rotationSpeedMax),
            Random.Range(rotationSpeedMin, rotationSpeedMax)
        );

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        isFalling = true;
    }

    private void Update()
    {
        if (isFalling)
        {
            transform.position += Vector3.down * fallSpeed * Time.deltaTime;
            transform.Rotate(rotationSpeed * Time.deltaTime);
            if (transform.position.y < initialY - fallDistanceBeforeDestroy)
            {
                Destroy(gameObject);
            }
        }
    }
}