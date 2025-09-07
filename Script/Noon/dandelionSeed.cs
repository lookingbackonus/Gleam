using UnityEngine;
using System.Collections;

public class dandelionSeed : MonoBehaviour
{
    [Header("홀씨 설정")]
    public float fallSpeed = 0.02f;
    public bool isBeingRidden = false;

    [Header("바람 효과")]
    public float windResistance = 1f;
    public float windEffectDuration = 2f;

    [Header("충돌 감지")]
    public LayerMask groundLayer = 1;
    public ParticleSystem disappearEffect;

    private float baseGravity = 0.3f;
    private Vector3 windVelocity = Vector3.zero;
    private bool isAffectedByWind = false;
    private Coroutine windEffectCoroutine;
    private Vector3 floatOffset;

    private Rigidbody rb;
    private Collider col;

    void Awake()
    {
        InitializeComponents();
        SetupPhysics();
    }

    void Start()
    {
        GenerateFloatOffset();
    }

    void Update()
    {
        if (!isBeingRidden)
        {
            ApplyNaturalMovement();
        }

        ApplyCustomGravity();

        if (isAffectedByWind)
        {
            ApplyWindEffect();
        }
    }

    void InitializeComponents()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        if (rb == null)
        {
            Debug.LogError($"{gameObject.name}: Rigidbody가 필요합니다!");
            return;
        }

        if (col == null)
        {
            Debug.LogError($"{gameObject.name}: Collider가 필요합니다!");
            return;
        }
    }

    void SetupPhysics()
    {
        if (rb != null)
        {
            rb.useGravity = false;
            rb.linearDamping = 2f;
        }

        if (col != null)
        {
            if (col is MeshCollider meshCol)
            {
                if (meshCol.sharedMesh != null && !meshCol.convex)
                {
                    meshCol.convex = true;
                }
            }
            col.isTrigger = true;
        }
    }

    void GenerateFloatOffset()
    {
        floatOffset = new Vector3(
            Random.Range(0f, Mathf.PI * 2),
            Random.Range(0f, Mathf.PI * 2),
            Random.Range(0f, Mathf.PI * 2)
        );
    }

    void ApplyNaturalMovement()
    {
        if (rb == null) return;

        float time = Time.time * 1.5f;
        Vector3 floatMovement = new Vector3(
            Mathf.Sin(time + floatOffset.x) * 0.3f,
            Mathf.Sin(time * 0.7f + floatOffset.y) * 0.1f,
            Mathf.Cos(time * 0.8f + floatOffset.z) * 0.2f
        );

        rb.AddForce(floatMovement * Time.deltaTime, ForceMode.VelocityChange);
    }

    void ApplyCustomGravity()
    {
        if (rb == null) return;

        if (isBeingRidden)
        {
            rb.AddForce(Vector3.down * fallSpeed, ForceMode.Acceleration);
            rb.AddForce(Vector3.up * (fallSpeed * 0.3f), ForceMode.Acceleration);
        }
        else
        {
            rb.AddForce(Vector3.down * baseGravity, ForceMode.Acceleration);
        }
    }

    void ApplyWindEffect()
    {
        if (rb == null) return;

        rb.AddForce(windVelocity * Time.deltaTime, ForceMode.VelocityChange);
        windVelocity = Vector3.Lerp(windVelocity, Vector3.zero, Time.deltaTime * 0.5f);

        if (windVelocity.magnitude < 0.1f)
        {
            StopWind();
        }
    }

    public void ApplyWind(Vector3 windDirection, float windStrength)
    {
        Vector3 effectiveWind = windDirection * windStrength / windResistance;
        windVelocity += effectiveWind;
        isAffectedByWind = true;

        if (windEffectCoroutine != null)
            StopCoroutine(windEffectCoroutine);
        windEffectCoroutine = StartCoroutine(WindEffectTimer());
    }

    IEnumerator WindEffectTimer()
    {
        yield return new WaitForSeconds(windEffectDuration);

        while (windVelocity.magnitude > 0.1f)
        {
            windVelocity = Vector3.Lerp(windVelocity, Vector3.zero, Time.deltaTime * 2f);
            yield return null;
        }

        StopWind();
    }

    void OnTriggerEnter(Collider other)
    {
        Player playerComponent = other.GetComponentInParent<Player>();
        if (playerComponent != null && CanAttachToPlayer(playerComponent))
        {
            // 홀씨와 플레이어 충돌 효과음 재생
            SoundManager.Instance.PlaySFX(SFXCategory.CH1_Spring, SFXSubCategory.Noon, "DandelionSeed");

            AttachToPlayer(playerComponent);
            return;
        }

        if (IsInLayerMask(other.gameObject, groundLayer))
        {
            OnHitGround();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (IsInLayerMask(collision.gameObject, groundLayer))
        {
            OnHitGround();
        }
    }

    bool CanAttachToPlayer(Player playerComponent)
    {
        if (playerComponent.dan == gameObject) return false;

        GameObject attachPoint = GameObject.Find("AttachPoint");
        if (attachPoint != null && attachPoint.transform.childCount > 0)
        {
            return false;
        }

        return playerComponent.stateMachine != null && playerComponent.dandelionState != null;
    }

    void AttachToPlayer(Player playerComponent)
    {
        playerComponent.dan = gameObject;
        playerComponent.stateMachine.ChangeState(playerComponent.dandelionState);
        isBeingRidden = true;
    }

    bool IsInLayerMask(GameObject obj, LayerMask layerMask)
    {
        return (layerMask.value & (1 << obj.layer)) > 0;
    }

    void OnHitGround()
    {
        DestroySeed();
    }

    void DestroySeed()
    {
        if (disappearEffect != null)
        {
            ParticleSystem effect = Instantiate(disappearEffect, transform.position, Quaternion.identity);
            Destroy(effect.gameObject, 2f);
        }

        Destroy(gameObject);
    }

    public void StopWind()
    {
        isAffectedByWind = false;
        windVelocity = Vector3.zero;
        if (windEffectCoroutine != null)
        {
            StopCoroutine(windEffectCoroutine);
            windEffectCoroutine = null;
        }
    }
    public bool IsBeingRidden() => isBeingRidden;
    public Vector3 GetWindVelocity() => windVelocity;
    public bool IsAffectedByWind() => isAffectedByWind;
    public void ForceDestroy() => DestroySeed();
    public void SetFallSpeed(float speed) => fallSpeed = speed;
    public void SetWindResistance(float resistance) => windResistance = resistance;
}