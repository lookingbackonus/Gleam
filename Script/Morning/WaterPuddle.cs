using UnityEngine;
using System.Collections;

public class WaterPuddle : MonoBehaviour
{
    [Header("미끄러짐 설정")]
    public float slipDuration = 3f;
    public float slipForce = 20f;

    [Header("기울어짐 효과")]
    public float maxTiltAngle = 15f;
    public float tiltSpeed = 8f;

    [Header("미끄러짐 물리 설정")]
    public float inputMultiplier = 6f;
    public float frictionReduction = 0.3f;
    public float maxSlipSpeed = 12f;
    public float afterEffectDuration = 4f;

    private bool isPlayerOnPuddle = false;
    private Player currentPlayer;
    private bool hasSlipEffect = false;
    private Coroutine afterEffectCoroutine;
    private float originalLinearDamping = 0f;

    private float slipVelocityX = 0f;

    void Start()
    {
        ParticleSystem ps = GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.startLifetime = Mathf.Infinity;
            main.loop = true;
        }

        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            BoxCollider boxCol = gameObject.AddComponent<BoxCollider>();
            boxCol.size = new Vector3(2f, 0.2f, 2f);
            boxCol.center = new Vector3(0, 0.1f, 0);
            boxCol.isTrigger = true;
        }
        else
        {
            col.isTrigger = true;
        }
    }

    void Update()
    {
        if (hasSlipEffect && currentPlayer != null)
        {
            HandleSlipperyInput();
            ApplySlipVelocity();
        }
    }

    void HandleSlipperyInput()
    {
        float horizontalInput = 0f;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            horizontalInput = -1f;
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            horizontalInput = 1f;

        float axisInput = Input.GetAxis("Horizontal");
        if (Mathf.Abs(axisInput) > 0.1f)
            horizontalInput = axisInput;

        if (Mathf.Abs(horizontalInput) > 0.01f)
        {
            float targetVelocity = horizontalInput * maxSlipSpeed;
            slipVelocityX = Mathf.Lerp(slipVelocityX, targetVelocity, Time.deltaTime * 3f);
        }
        else
        {
            slipVelocityX *= Mathf.Clamp01(1f - frictionReduction * 2f * Time.deltaTime);

            if (Mathf.Abs(slipVelocityX) < 0.1f)
                slipVelocityX = 0f;
        }
    }

    void ApplySlipVelocity()
    {
        if (Mathf.Abs(slipVelocityX) > 0.001f && currentPlayer != null)
        {
            if (!currentPlayer.rb.isKinematic)
            {
                Vector3 velocity = currentPlayer.rb.linearVelocity;
                velocity.x = slipVelocityX;
                currentPlayer.rb.linearVelocity = velocity;
            }
            else
            {
                Vector3 slipMovement = new Vector3(slipVelocityX * Time.deltaTime, 0, 0);
                currentPlayer.rb.MovePosition(currentPlayer.rb.position + slipMovement);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isPlayerOnPuddle)
        {
            if (other.TryGetComponent(out PlayerCol playerCol))
            {
                SoundManager.Instance.PlaySFX(SFXCategory.CH1_Spring, SFXSubCategory.Morning, "Puddle");

                isPlayerOnPuddle = true;
                currentPlayer = playerCol.player;
                hasSlipEffect = true;

                originalLinearDamping = currentPlayer.rb.linearDamping;
                currentPlayer.rb.linearDamping = frictionReduction;

                slipVelocityX = 0f;

                if (afterEffectCoroutine != null)
                    StopCoroutine(afterEffectCoroutine);

                afterEffectCoroutine = StartCoroutine(TotalSlipEffect());
                StartCoroutine(SlipEffect(currentPlayer));
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && isPlayerOnPuddle)
            isPlayerOnPuddle = false;
    }

    IEnumerator SlipEffect(Player player)
    {
        float elapsed = 0f;
        Vector3 originalRotation = player.playerObj.transform.eulerAngles;

        while (elapsed < slipDuration && isPlayerOnPuddle)
        {
            float tiltAngle = Mathf.Sin(elapsed * tiltSpeed) * maxTiltAngle;
            Vector3 tiltRotation = new Vector3(originalRotation.x, originalRotation.y, tiltAngle);
            player.playerObj.transform.eulerAngles = tiltRotation;

            elapsed += Time.deltaTime;
            yield return null;
        }

        player.playerObj.transform.eulerAngles = originalRotation;

        if (isPlayerOnPuddle)
            hasSlipEffect = false;
    }

    IEnumerator TotalSlipEffect()
    {
        float elapsed = 0f;

        while (elapsed < afterEffectDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / afterEffectDuration;
            float currentFrictionMultiplier = Mathf.Lerp(frictionReduction, 1f, progress * progress);

            slipVelocityX *= (1f - currentFrictionMultiplier * 1.5f * Time.deltaTime);

            if (Mathf.Abs(slipVelocityX) < 0.1f)
                slipVelocityX = 0f;

            yield return null;
        }

        hasSlipEffect = false;
        slipVelocityX = 0f;

        if (currentPlayer != null)
        {
            currentPlayer.rb.linearDamping = originalLinearDamping;
            StartCoroutine(GradualStop(currentPlayer));
            currentPlayer = null;
        }

        afterEffectCoroutine = null;
    }

    IEnumerator GradualStop(Player player)
    {
        float stopTime = 0.5f;

        if (player.rb.isKinematic)
            yield break;

        Vector3 initialVelocity = player.rb.linearVelocity;

        for (float t = 0; t < stopTime; t += Time.deltaTime)
        {
            if (player.rb.isKinematic)
                yield break;

            float progress = t / stopTime;
            Vector3 currentVelocity = Vector3.Lerp(initialVelocity, new Vector3(0, initialVelocity.y, 0), progress);
            player.rb.linearVelocity = currentVelocity;
            yield return null;
        }

        if (!player.rb.isKinematic)
            player.rb.linearVelocity = new Vector3(0, player.rb.linearVelocity.y, 0);
    }
}