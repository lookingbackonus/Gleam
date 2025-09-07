using UnityEngine;
using System.Collections;

public class MazeTeleport : MonoBehaviour
{
    [Header("텔레포트 설정")]
    public Vector3 teleportPosition = new Vector3(300, 100, 300);
    public bool enableTeleport = true;

    public static event System.Action OnMazeTeleportStart;

    private static bool _isTeleporting = false;
    public static bool IsTeleporting
    {
        get => _isTeleporting;
        private set => _isTeleporting = value;
    }

    private bool hasExecuted = false;
    private Player player;
    private DandelionSpawner spawner;
    private Coroutine teleportCoroutine;

    void Start()
    {
        IsTeleporting = false;

        FindPlayer();
        FindSpawner();

        if (enableTeleport && spawner != null)
        {
            StartCoroutine(CheckRainbowAndTeleport());
        }
        StartCoroutine(SafetyCheck());
    }

    void OnDestroy()
    {
        IsTeleporting = false;
        OnMazeTeleportStart = null;

        if (teleportCoroutine != null)
        {
            StopCoroutine(teleportCoroutine);
        }
    }
    IEnumerator SafetyCheck()
    {
        while (true)
        {
            yield return new WaitForSeconds(10f);
            if (IsTeleporting && teleportCoroutine == null)
            {
                ForceEndTeleport();
            }
        }
    }

    void FindPlayer()
    {
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            PlayerCol playerCol = playerObject.GetComponent<PlayerCol>();
            if (playerCol != null)
            {
                player = playerCol.player;
            }
        }
    }

    void FindSpawner()
    {
        spawner = FindFirstObjectByType<DandelionSpawner>();
    }

    IEnumerator CheckRainbowAndTeleport()
    {
        while (!hasExecuted)
        {
            yield return new WaitForSeconds(0.1f);

            if (spawner != null && spawner.IsRainbowActivated())
            {
                hasExecuted = true;
                float totalWaitTime = 5f + FadeManager.Instance.fadeDuration;
                yield return new WaitForSeconds(totalWaitTime);
                teleportCoroutine = StartCoroutine(ExecuteCinematicTeleport());
            }
        }
    }

    IEnumerator ExecuteCinematicTeleport()
    {
        if (player == null)
        {
            ForceEndTeleport();
            yield break;
        }
        IsTeleporting = true;

        player.SetInputCan(false);
        if (player.rb != null)
            player.rb.isKinematic = true;

        PerformTeleport();

        FadeManager.Instance.FadeIn(() =>
        {
            RestorePlayerAndComplete();
        });

        teleportCoroutine = null;
    }

    void PerformTeleport()
    {
        if (player == null) return;

        player.playerObj.transform.position = teleportPosition;
        if (player.stateMachine != null && player.idleState != null)
            player.stateMachine.ChangeState(player.idleState);
    }

    void RestorePlayerAndComplete()
    {
        RestorePlayer();
        IsTeleporting = false;
    }

    void RestorePlayer()
    {
        if (player == null)
        {
            return;
        }
        if (player.rb != null)
        {
            player.rb.isKinematic = false;
            player.rb.linearVelocity = Vector3.zero;
            player.rb.angularVelocity = Vector3.zero;
        }

        player.SetInputCan(true);
        if (player.stateMachine != null && player.idleState != null)
        {
            player.stateMachine.ChangeState(player.idleState);
        }
    }
    void ForceEndTeleport()
    {
        if (teleportCoroutine != null)
        {
            StopCoroutine(teleportCoroutine);
            teleportCoroutine = null;
        }

        RestorePlayer();
        IsTeleporting = false;
    }
}