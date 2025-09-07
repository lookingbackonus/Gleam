using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class starlightReflection : MonoBehaviour
{
    [Header("별빛 반사 조건")]
    public int requiredFireflyCount = 3;

    [Header("별빛 반사 효과")]
    public float reflectionRadius = 8f;
    public float reflectionIntensity = 3f;
    public Color starlightColor = Color.cyan;
    public float reflectionDuration = 15f;

    [Header("시각 효과")]
    public GameObject starlightEffectPrefab;
    public ParticleSystem starlightParticles;

    // 상태 변수
    private List<firefly> encounteredFireflies = new List<firefly>();
    private firefly currentReflectedFirefly;                          
    private GameObject starlightEffect;                               
    private Light starlightLight;
    private bool isStarlightActive = false;
    private Coroutine starlightCoroutine;

    private fireflyManager fireflyManager;
    private Transform player;

    void Start()
    {
        fireflyManager = FindObjectOfType<fireflyManager>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        CreateStarlightSystem();
    }

    void Update()
    {
        CheckCurrentFirefly();
    }

    void CreateStarlightSystem()
    {
        GameObject lightObj = new GameObject("StarlightReflection");
        lightObj.transform.SetParent(transform);
        starlightLight = lightObj.AddComponent<Light>();

        starlightLight.type = LightType.Point;
        starlightLight.color = starlightColor;
        starlightLight.intensity = 0f;
        starlightLight.range = reflectionRadius;
        starlightLight.shadows = LightShadows.Soft;

        if (starlightParticles == null)
        {
            GameObject particleObj = new GameObject("StarlightParticles");
            particleObj.transform.SetParent(lightObj.transform);
            particleObj.transform.localPosition = Vector3.zero;

            starlightParticles = particleObj.AddComponent<ParticleSystem>();
            SetupStarlightParticles();
        }
    }

    void SetupStarlightParticles()
    {
        var main = starlightParticles.main;
        main.startLifetime = 3f;
        main.startSpeed = 0.5f;
        main.startSize = 0.3f;
        main.startColor = starlightColor;
        main.maxParticles = 50;

        var emission = starlightParticles.emission;
        emission.rateOverTime = 0f;

        var shape = starlightParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 1f;

        var velocityOverLifetime = starlightParticles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.radial = 0.2f;
    }

    void CheckCurrentFirefly()
    {
        if (fireflyManager == null) return;

        firefly currentFirefly = fireflyManager.GetCurrentActiveFirefly();

        if (currentFirefly != null && !encounteredFireflies.Contains(currentFirefly))
        {
            OnFireflyEncountered(currentFirefly);
        }
    }

    void OnFireflyEncountered(firefly firefly)
    {
        encounteredFireflies.Add(firefly);

        Debug.Log($"반딧불 만남: {encounteredFireflies.Count}/{requiredFireflyCount} " +
                  $"({firefly.GetFireflyType()})");

        if (encounteredFireflies.Count >= requiredFireflyCount)
        {
            TriggerStarlightReflection();
        }
    }

    void TriggerStarlightReflection()
    {
        if (isStarlightActive) return;

        firefly currentActiveFirefly = fireflyManager.GetCurrentActiveFirefly();

        if (currentActiveFirefly != null)
        {
            StartStarlightReflection(currentActiveFirefly);
        }
        else
        {
            Debug.LogWarning("별빛 반사 트리거되었지만 활성화된 반딧불이 없습니다!");
        }
        ResetFireflyCount();
    }


    void StartStarlightReflection(firefly targetFirefly)
    {
        currentReflectedFirefly = targetFirefly;
        isStarlightActive = true;

        starlightLight.transform.position = targetFirefly.transform.position;

        Debug.Log($"별빛 반사 시작! 현재 활성화된 {targetFirefly.GetFireflyType()} 반딧불이 밝게 빛납니다!");

        if (starlightCoroutine != null)
            StopCoroutine(starlightCoroutine);

        starlightCoroutine = StartCoroutine(StarlightEffectCoroutine());

        OnStarlightReflectionStart(targetFirefly);
    }

    IEnumerator StarlightEffectCoroutine()
    {
        yield return StartCoroutine(FadeStarlightIn());

        yield return new WaitForSeconds(reflectionDuration);

        yield return StartCoroutine(FadeStarlightOut());

        EndStarlightReflection();
    }
    IEnumerator FadeStarlightIn()
    {
        float time = 0f;
        float fadeInDuration = 2f;

        while (time < fadeInDuration)
        {
            time += Time.deltaTime;
            float t = time / fadeInDuration;

            starlightLight.intensity = Mathf.Lerp(0f, reflectionIntensity, t);

            var emission = starlightParticles.emission;
            emission.rateOverTime = Mathf.Lerp(0f, 20f, t);

            yield return null;
        }
    }

    IEnumerator FadeStarlightOut()
    {
        float time = 0f;
        float fadeOutDuration = 1f;
        float startIntensity = starlightLight.intensity;

        while (time < fadeOutDuration)
        {
            time += Time.deltaTime;
            float t = time / fadeOutDuration;

            starlightLight.intensity = Mathf.Lerp(startIntensity, 0f, t);

            var emission = starlightParticles.emission;
            emission.rateOverTime = Mathf.Lerp(20f, 0f, t);

            yield return null;
        }
    }

    void EndStarlightReflection()
    {
        isStarlightActive = false;
        currentReflectedFirefly = null;

        if (starlightEffect != null)
        {
            Destroy(starlightEffect);
        }

        Debug.Log("별빛 반사 종료");

        OnStarlightReflectionEnd();
    }

    void ResetFireflyCount()
    {
        if (encounteredFireflies.Count > 0)
        {
            Debug.Log($"반딧불 카운트 리셋 - 다음 별빛까지 {requiredFireflyCount}개 필요");
            encounteredFireflies.Clear();
        }
    }

    void OnStarlightReflectionStart(firefly targetFirefly)
    {
        if (starlightEffectPrefab != null)
        {
            starlightEffect = Instantiate(starlightEffectPrefab,
                                        targetFirefly.transform.position,
                                        Quaternion.identity);

            starlightEffect.transform.SetParent(targetFirefly.transform);
        }
    }

    void OnStarlightReflectionEnd()
    {
    }

    public void ManualTriggerStarlight()
    {
        TriggerStarlightReflection();
    }

    public int GetEncounteredFireflyCount() => encounteredFireflies.Count;
    public bool IsStarlightActive() => isStarlightActive;
    public firefly GetCurrentReflectedFirefly() => currentReflectedFirefly;

    public void SetRequiredFireflyCount(int count)
    {
        requiredFireflyCount = count;
    }

    public void SetReflectionDuration(float duration)
    {
        reflectionDuration = duration;
    }

    public void SetReflectionRadius(float radius)
    {
        reflectionRadius = radius;
        if (starlightLight != null)
            starlightLight.range = radius;
    }
}