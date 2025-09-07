using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RainController : MonoBehaviour
{
    public ParticleSystem rainParticle;
    public StairManager stairSpawner;
    [Header("플레이어 추적 설정")]
    public Transform player;
    public Vector3 rainOffset = new Vector3(0, 10f, 0);
    public bool followPlayer = true;

    private List<float> activationTimes = new List<float>();
    private float timer = 0f;
    private int activationCount = 0;
    private bool isRaining = false;
    private bool hasRained = false;
    private bool puddlesCreated = false;
    private bool rainingStopped = false;

    private string rainSoundName = "RainSound";

    public bool HasRained => hasRained;
    public bool IsRaining => isRaining;

    void Start()
    {
        if (player == null)
        {
            PlayerCol playerCol = Object.FindFirstObjectByType<PlayerCol>();
            if (playerCol != null)
            {
                player = playerCol.player.playerObj.transform;
            }
            else
            {
                Debug.LogWarning("PlayerCol 컴포넌트를 찾을 수 없습니다!");
            }
        }
        if (rainParticle != null)
        {
            rainParticle.Stop();
        }
        else
        {
            Debug.LogError("rainParticle이 연결되지 않았습니다!");
        }

        DandelionCameraManager.OnStageStarted += StopRainPermanently;

        GenerateRandomTimes();
        StartCoroutine(RainRoutine());
    }

    void OnDestroy()
    {
        DandelionCameraManager.OnStageStarted -= StopRainPermanently;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopNamedSFX(rainSoundName);
        }
    }

    void Update()
    {
        if (isRaining && followPlayer && player != null && rainParticle != null)
        {
            Vector3 targetPosition = player.position + rainOffset;
            rainParticle.transform.position = targetPosition;
        }
    }

    void StopRainPermanently()
    {
        rainingStopped = true;
        if (isRaining && rainParticle != null)
        {
            rainParticle.Stop();
            isRaining = false;

            SoundManager.Instance.StopNamedSFX(rainSoundName, 0.3f);
        }

        StopAllCoroutines();
    }

    void GenerateRandomTimes()
    {
        while (activationTimes.Count < 3)
        {
            float randomTime = Random.Range(0f, 30f);
            bool tooClose = false;
            foreach (float time in activationTimes)
            {
                if (Mathf.Abs(time - randomTime) < 3f)
                {
                    tooClose = true;
                    break;
                }
            }
            if (!tooClose)
            {
                activationTimes.Add(randomTime);
            }
        }
        activationTimes.Sort();
    }

    IEnumerator RainRoutine()
    {
        while (timer < 30f && !rainingStopped)
        {
            if (activationCount < activationTimes.Count && timer >= activationTimes[activationCount])
            {
                StartCoroutine(PlayRainForSeconds(5f));
                activationCount++;
            }
            timer += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator PlayRainForSeconds(float duration)
    {
        if (!isRaining && !rainingStopped)
        {
            isRaining = true;
            hasRained = true;

            if (SoundManager.Instance.IsNamedSFXPlaying(rainSoundName))
            {
                SoundManager.Instance.StopNamedSFX(rainSoundName, 0.1f);
                yield return new WaitForSeconds(0.2f);
            }

            SoundManager.Instance.PlayNamedSFX(SFXCategory.CH1_Spring, SFXSubCategory.Morning, "Rain", rainSoundName, 1f, true);

            if (player != null && rainParticle != null)
            {
                Vector3 startPosition = player.position + rainOffset;
                rainParticle.transform.position = startPosition;
            }
            if (rainParticle != null)
            {
                rainParticle.Play();
            }
            else
            {
                Debug.LogError("rainParticle이 null입니다!");
            }

            if (!puddlesCreated && stairSpawner != null)
            {
                stairSpawner.SpawnPuddlesOnExistingStairs();
                puddlesCreated = true;
            }

            float elapsed = 0f;
            while (elapsed < duration && !rainingStopped)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (rainParticle != null)
            {
                rainParticle.Stop();
            }
            SoundManager.Instance.StopNamedSFX(rainSoundName, 0.3f);

            isRaining = false;
        }
    }
}