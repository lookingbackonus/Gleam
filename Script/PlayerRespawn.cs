using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    [SerializeField][Header("스테이지 넘버 (봄:1, 여름:2, 가을:3, 겨울:4)")] int stageNum;
    [Space(8)]
    [SerializeField][Header("시간대 (아침 : 1, 점심 : 2, 저녁 : 3)")] int timeNum;
    [Space(8)]
    [SerializeField][Header("시간대별 기믹 몇 번째 인지 (ex.점심 첫 번째 기믹 : 1)")] int level;

    private float respawnCooldown = 2.0f;

    private static bool globalRespawning = false;
    private static float globalLastRespawnTime = -10f;

    void OnTriggerEnter(Collider col)
    {
        if (col.CompareTag("Player"))
        {
            // 텔레포트 체크 부분 삭제하고 바로 기존 코드
            if (globalRespawning || Time.time - globalLastRespawnTime < respawnCooldown)
            {
                return;
            }

            if (col.TryGetComponent(out PlayerCol _playerCol))
            {
                RespawnPlayer(_playerCol.player);
            }
        }
    }

    void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.CompareTag("Player"))
        {
            if (globalRespawning || Time.time - globalLastRespawnTime < respawnCooldown)
            {
                return;
            }

            if (col.gameObject.TryGetComponent(out PlayerCol _playerCol))
            {
                RespawnPlayer(_playerCol.player);
            }
        }
    }

    void RespawnPlayer(Player player)
    {
        if (globalRespawning || Time.time - globalLastRespawnTime < respawnCooldown)
        {
            return;
        }

        globalRespawning = true;
        globalLastRespawnTime = Time.time;

        Vector3 targetPosition = GetTargetRespawnPosition();

        player.SetInputCan(false);
        if (player.rb != null)
        {
            player.rb.isKinematic = true;
        }

        FadeManager.Instance.FadeOut(() =>
        {
            // 페이드아웃 완료 후 즉시 게임 시스템들 재시작 (위치 변경 전에)
            RestartGameSystems();

            if (targetPosition != Vector3.zero)
            {
                player.playerObj.transform.position = targetPosition;

                if (player.rb != null)
                {
                    player.rb.isKinematic = true;
                    player.rb.linearVelocity = Vector3.zero;
                    player.rb.angularVelocity = Vector3.zero;
                }

                if (player.stateMachine != null && player.idleState != null)
                {
                    player.stateMachine.ChangeState(player.idleState);
                }

                StartCoroutine(EnsureCorrectPosition(player, targetPosition));
            }

            FadeManager.Instance.FadeIn(() =>
            {
                if (targetPosition != Vector3.zero)
                {
                    player.playerObj.transform.position = targetPosition;

                    if (player.rb != null)
                    {
                        player.rb.isKinematic = false;
                        player.rb.linearVelocity = Vector3.zero;
                        player.rb.angularVelocity = Vector3.zero;
                    }
                }

                player.SetInputCan(true);
                globalRespawning = false;
            });
        });
    }

    Vector3 GetTargetRespawnPosition()
    {
        Vector3 respawnPos = GetSavedPosition();

        if (respawnPos != Vector3.zero)
        {
            return respawnPos;
        }
        else
        {
            Vector3 fallbackPos = FindAnyValidSavePosition();
            if (fallbackPos != Vector3.zero)
            {
                return fallbackPos;
            }
        }

        return Vector3.zero;
    }

    Vector3 FindAnyValidSavePosition()
    {
        for (int time = 1; time <= 3; time++)
        {
            for (int lv = 1; lv <= 10; lv++)
            {
                string saveKey = string.Format("{0}, {1}, {2}", stageNum, time, lv);
                if (PlayerPrefs.GetInt(saveKey, 0) == 1)
                {
                    string posKeyX = string.Format("{0}, {1}, SavePos_X", stageNum, lv);
                    string posKeyY = string.Format("{0}, {1}, SavePos_Y", stageNum, lv);
                    string posKeyZ = string.Format("{0}, {1}, SavePos_Z", stageNum, lv);

                    float x = PlayerPrefs.GetFloat(posKeyX, 0f);
                    float y = PlayerPrefs.GetFloat(posKeyY, 0f);
                    float z = PlayerPrefs.GetFloat(posKeyZ, 0f);

                    Vector3 pos = new Vector3(x, y, z);
                    if (pos != Vector3.zero)
                    {
                        return pos;
                    }
                }
            }
        }
        return Vector3.zero;
    }

    Vector3 GetSavedPosition()
    {
        string saveKey = string.Format("{0}, {1}, {2}", stageNum, timeNum, level);

        if (PlayerPrefs.GetInt(saveKey, 0) == 1)
        {
            string posKeyX = string.Format("{0}, {1}, SavePos_X", stageNum, level);
            string posKeyY = string.Format("{0}, {1}, SavePos_Y", stageNum, level);
            string posKeyZ = string.Format("{0}, {1}, SavePos_Z", stageNum, level);

            float x = PlayerPrefs.GetFloat(posKeyX, 0f);
            float y = PlayerPrefs.GetFloat(posKeyY, 0f);
            float z = PlayerPrefs.GetFloat(posKeyZ, 0f);

            return new Vector3(x, y, z);
        }

        return Vector3.zero;
    }

    System.Collections.IEnumerator EnsureCorrectPosition(Player player, Vector3 targetPosition)
    {
        yield return null;

        player.playerObj.transform.position = targetPosition;

        if (player.rb != null)
        {
            player.rb.isKinematic = true;
            player.rb.linearVelocity = Vector3.zero;
            player.rb.angularVelocity = Vector3.zero;
        }
    }

    void RestartGameSystems()
    {
        RestartStairManager();
        RestartRainController();
        RestartDandelionSpawner();
        RestartRainbowManager();
    }

    void RestartStairManager()
    {
        StairManager stairManager = FindFirstObjectByType<StairManager>();
        if (stairManager != null)
        {
            for (int i = stairManager.transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(stairManager.transform.GetChild(i).gameObject);
            }

            var stairType = stairManager.GetType();
            var localSpawnPosField = stairType.GetField("localSpawnPos", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (localSpawnPosField != null)
            {
                localSpawnPosField.SetValue(stairManager, new Vector3(0, 0, 2));
            }

            for (int i = 0; i < stairManager.stairsToSpawn; i++)
            {
                var spawnMethod = stairType.GetMethod("SpawnStairWithGimmick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (spawnMethod != null)
                {
                    spawnMethod.Invoke(stairManager, null);
                }
            }
        }
    }

    void RestartRainController()
    {
        RainController rainController = FindFirstObjectByType<RainController>();
        if (rainController != null)
        {
            rainController.StopAllCoroutines();
            rainController.enabled = false;
            rainController.enabled = true;
            StartCoroutine(RestartRainControllerCoroutine(rainController));
        }
    }

    System.Collections.IEnumerator RestartRainControllerCoroutine(RainController rainController)
    {
        yield return null;

        var rainType = rainController.GetType();

        var hasRainedField = rainType.GetField("hasRained", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (hasRainedField != null)
        {
            hasRainedField.SetValue(rainController, false);
        }

        var isRainingField = rainType.GetField("isRaining", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (isRainingField != null)
        {
            isRainingField.SetValue(rainController, false);
        }

        var puddlesCreatedField = rainType.GetField("puddlesCreated", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (puddlesCreatedField != null)
        {
            puddlesCreatedField.SetValue(rainController, false);
        }

        var timerField = rainType.GetField("timer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (timerField != null)
        {
            timerField.SetValue(rainController, 0f);
        }

        var activationCountField = rainType.GetField("activationCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (activationCountField != null)
        {
            activationCountField.SetValue(rainController, 0);
        }

        var activationTimesField = rainType.GetField("activationTimes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (activationTimesField != null)
        {
            var activationTimes = activationTimesField.GetValue(rainController) as System.Collections.Generic.List<float>;
            if (activationTimes != null)
            {
                activationTimes.Clear();
            }
        }

        var startMethod = rainType.GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (startMethod != null)
        {
            startMethod.Invoke(rainController, null);
        }
    }

    void RestartDandelionSpawner()
    {
        DandelionSpawner dandelionSpawner = FindFirstObjectByType<DandelionSpawner>();
        if (dandelionSpawner != null)
        {
            dandelionSpawner.ClearAllSeeds();
            dandelionSpawner.StopAutoSpawn();
            dandelionSpawner.StopWindSystem();
            StartCoroutine(RestartDandelionSpawnerCoroutine(dandelionSpawner));
        }
    }

    System.Collections.IEnumerator RestartDandelionSpawnerCoroutine(DandelionSpawner dandelionSpawner)
    {
        yield return null;

        var spawnerType = dandelionSpawner.GetType();

        var hasStartedSpawningField = spawnerType.GetField("hasStartedSpawning", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (hasStartedSpawningField != null)
        {
            hasStartedSpawningField.SetValue(dandelionSpawner, false);
        }

        var windSystemStartedField = spawnerType.GetField("windSystemStarted", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (windSystemStartedField != null)
        {
            windSystemStartedField.SetValue(dandelionSpawner, false);
        }

        var collisionCountField = spawnerType.GetField("collisionCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (collisionCountField != null)
        {
            collisionCountField.SetValue(dandelionSpawner, 0);
        }

        dandelionSpawner.useAutoSpawn = true;

        var startMethod = spawnerType.GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (startMethod != null)
        {
            startMethod.Invoke(dandelionSpawner, null);
        }
    }

    void RestartRainbowManager()
    {
        RainbowManager rainbowManager = FindFirstObjectByType<RainbowManager>();
        if (rainbowManager != null)
        {
            rainbowManager.DeactivateRainbow();
            StartCoroutine(RestartRainbowManagerCoroutine(rainbowManager));
        }
    }

    System.Collections.IEnumerator RestartRainbowManagerCoroutine(RainbowManager rainbowManager)
    {
        yield return null;

        var rainbowType = rainbowManager.GetType();

        var isRainbowActiveField = rainbowType.GetField("isRainbowActive", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (isRainbowActiveField != null)
        {
            isRainbowActiveField.SetValue(rainbowManager, false);
        }

        var startMethod = rainbowType.GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (startMethod != null)
        {
            startMethod.Invoke(rainbowManager, null);
        }
    }
}