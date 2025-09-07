using UnityEngine;
using System.Collections;

public class PlantTrap : MonoBehaviour
{
    [Header("식물 함정 설정")]
    public float stunDuration = 3f;

    private bool hasTriggered = false;

    void Start()
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            BoxCollider boxCol = gameObject.AddComponent<BoxCollider>();
            boxCol.size = new Vector3(1.5f, 2f, 1.5f);
            boxCol.isTrigger = true;
        }
        else
        {
            col.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasTriggered)
        {
            if (other.TryGetComponent(out PlayerCol playerCol))
            {
                SoundManager.Instance.PlaySFX(SFXCategory.CH1_Spring, SFXSubCategory.Morning, "Grass");

                hasTriggered = true;
                StartCoroutine(TrapPlayer(playerCol.player));
            }
        }
    }

    IEnumerator TrapPlayer(Player player)
    {
        if (player == null) yield break;

        player.SetInputCan(false);
        player.SetDie(true);

        Animator anim = player.playerObj.GetComponentInChildren<Animator>();
        if (anim != null)
            anim.speed = 0f;

        Rigidbody rb = player.rb;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        StartPlayerTrappedEffect(player);

        float elapsed = 0f;
        while (elapsed < stunDuration)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            elapsed += Time.deltaTime;
            yield return null;
        }

        player.SetDie(false);
        player.SetInputCan(true);

        StopPlayerTrappedEffect(player);

        if (anim != null)
            anim.speed = 1f;

        yield return new WaitForSeconds(2f);
        hasTriggered = false;
    }

    void StartPlayerTrappedEffect(Player player)
    {
        MeshRenderer renderer = player.playerObj.GetComponentInChildren<MeshRenderer>();
        if (renderer != null)
            renderer.material.color = Color.red;
    }

    void StopPlayerTrappedEffect(Player player)
    {
        MeshRenderer renderer = player.playerObj.GetComponentInChildren<MeshRenderer>();
        if (renderer != null)
            renderer.material.color = Color.white;
    }
}