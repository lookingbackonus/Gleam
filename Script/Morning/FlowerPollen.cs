using UnityEngine;
using System.Collections;
using System.Reflection;

public class FlowerPollen : MonoBehaviour
{
    [Header("꽃가루 효과 설정")]
    public float slowEffectDuration = 5f;
    public float speedReductionPercent = 50f;

    private bool isPlayerInPollen = false;
    private Player currentPlayer;
    private bool hasSlowEffect = false;
    private Coroutine slowEffectCoroutine;

    private static float originalMoveSpeed = 0f;
    private static float originalRunSpeed = 0f;
    private static bool isSpeedStored = false;

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
            SphereCollider sphereCol = gameObject.AddComponent<SphereCollider>();
            sphereCol.radius = 1.5f;
            sphereCol.isTrigger = true;
        }
        else
        {
            col.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isPlayerInPollen)
        {
            if (other.TryGetComponent(out PlayerCol playerCol))
            {
                SoundManager.Instance.PlaySFX(SFXCategory.CH1_Spring, SFXSubCategory.Morning, "Pollen");

                isPlayerInPollen = true;
                currentPlayer = playerCol.player;
                hasSlowEffect = true;

                if (!isSpeedStored)
                {
                    originalMoveSpeed = currentPlayer.moveSpeed;
                    originalRunSpeed = currentPlayer.runSpeed;
                    isSpeedStored = true;
                }

                if (slowEffectCoroutine != null)
                    StopCoroutine(slowEffectCoroutine);

                slowEffectCoroutine = StartCoroutine(SlowEffect(currentPlayer));
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && isPlayerInPollen)
            isPlayerInPollen = false;
    }

    IEnumerator SlowEffect(Player player)
    {
        float reducedMoveSpeed = originalMoveSpeed * (1f - speedReductionPercent / 100f);
        float reducedRunSpeed = originalRunSpeed * (1f - speedReductionPercent / 100f);

        player.SetMoveSpeed(reducedMoveSpeed);

        var runSpeedProperty = typeof(Player).GetProperty("runSpeed");
        if (runSpeedProperty != null)
        {
            runSpeedProperty.SetValue(player, reducedRunSpeed);
        }

        float elapsed = 0f;

        while (elapsed < slowEffectDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        RestoreOriginalSpeed(player);

        slowEffectCoroutine = null;
    }

    void RestoreOriginalSpeed(Player player)
    {
        if (player != null)
        {
            player.SetMoveSpeed(originalMoveSpeed);

            var runSpeedProperty = typeof(Player).GetProperty("runSpeed");
            if (runSpeedProperty != null)
            {
                runSpeedProperty.SetValue(player, originalRunSpeed);
            }

            hasSlowEffect = false;
            currentPlayer = null;
        }
    }

    void OnDestroy()
    {
        if (slowEffectCoroutine != null)
        {
            StopCoroutine(slowEffectCoroutine);
            if (currentPlayer != null)
            {
                RestoreOriginalSpeed(currentPlayer);
            }
        }
    }

    public bool IsEffectActive()
    {
        return hasSlowEffect;
    }

    public static void ResetOriginalSpeed()
    {
        isSpeedStored = false;
        originalMoveSpeed = 0f;
        originalRunSpeed = 0f;
    }
}