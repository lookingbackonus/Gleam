using UnityEngine;
using System.Collections;

public enum FireflyType
{
    CorrectPath,WrongPath
}

public enum BlinkPattern
{
    CorrectPath, WrongPath
}

public class firefly : MonoBehaviour
{
    [Header("반딧불 타입")]
    public FireflyType fireflyType = FireflyType.CorrectPath;
    public BlinkPattern blinkPattern = BlinkPattern.CorrectPath;

    [Header("반딧불 설정")]
    public float lightRadius = 3f;
    public float lightIntensity = 1f;
    public Color fireflyColor = Color.white;

    [Header("모스부호 패턴 설정 (TRUE/FALSE)")]
    public float blinkInterval = 6f;
    public float dotDuration = 0.2f;
    public float dashDuration = 0.6f;
    public float gapBetweenBlinks = 0.15f;
    public float gapBetweenLetters = 0.4f;

    [Header("길 안내 설정")]
    public GameObject[] pathsToReveal;
    public float revealDuration = 5f;

    // 컴포넌트 참조
    private Light fireflyLight;
    private ParticleSystem glowEffect;
    private Transform player;

    // 상태 변수
    private bool isActive = false;
    private bool isNearestToPlayer = false;
    private Color currentColor;
    private Coroutine blinkCoroutine;
    private bool isPathRevealed = false;

    void Start()
    {
        SetupLight();
        SetupParticleEffect();

        // 플레이어 찾기
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        // 처음엔 꺼진 상태
        SetLightActive(false);
    }

    void SetupLight()
    {
        fireflyLight = GetComponent<Light>();
        if (fireflyLight == null)
        {
            fireflyLight = gameObject.AddComponent<Light>();
        }

        fireflyLight.type = LightType.Point;
        fireflyLight.range = lightRadius;
        fireflyLight.intensity = 0f; // 처음엔 꺼진 상태
        fireflyLight.color = fireflyColor;
        fireflyLight.shadows = LightShadows.Soft;
    }

    void SetupParticleEffect()
    {
        GameObject glowObj = new GameObject("FireflyGlow");
        glowObj.transform.SetParent(transform);
        glowObj.transform.localPosition = Vector3.zero;

        glowEffect = glowObj.AddComponent<ParticleSystem>();
        var main = glowEffect.main;
        main.startLifetime = 1f;
        main.startSpeed = 0.05f;
        main.startSize = 0.2f;
        main.maxParticles = 5;

        var emission = glowEffect.emission;
        emission.rateOverTime = 0f; // 처음엔 꺼진 상태

        var shape = glowEffect.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;
    }

    void SetColorByType()
    {
        // 모든 반딧불이 동일한 색상 사용
        fireflyLight.color = fireflyColor;
    }

    // 외부에서 호출: 가장 가까운 반딧불로 설정
    public void SetAsNearestFirefly(bool isNearest)
    {
        isNearestToPlayer = isNearest;

        if (isNearest)
        {
            StartBlinking();
        }
        else
        {
            StopBlinking();
        }
    }

    // 깜빡임 시작
    void StartBlinking()
    {
        if (blinkCoroutine != null)
            StopCoroutine(blinkCoroutine);

        blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    // 깜빡임 중지
    void StopBlinking()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        SetLightActive(false);
    }

    // 깜빡임 루틴
    IEnumerator BlinkRoutine()
    {
        while (isNearestToPlayer)
        {
            // 패턴에 따른 깜빡임 실행
            yield return StartCoroutine(ExecuteBlinkPattern());

            // 다음 깜빡임까지 대기
            yield return new WaitForSeconds(blinkInterval);
        }
    }

    // 모스부호 패턴 실행
    IEnumerator ExecuteBlinkPattern()
    {
        switch (blinkPattern)
        {
            case BlinkPattern.CorrectPath:
                // TRUE: T(−) R(·−·) U(··−) E(·)
                yield return StartCoroutine(MorseWord("− ·−· ··− ·"));
                break;

            case BlinkPattern.WrongPath:
                // FALSE: F(··−·) A(·−) L(·−··) S(···) E(·)
                yield return StartCoroutine(MorseWord("··−· ·− ·−·· ··· ·"));
                break;
        }

        // 패턴 완료 후 길 밝히기 (올바른 길인 경우)
        if (fireflyType == FireflyType.CorrectPath && !isPathRevealed)
        {
            RevealPath();
        }
    }

    // 모스부호 단어 실행 (공백으로 글자 구분)
    IEnumerator MorseWord(string word)
    {
        string[] letters = word.Split(' ');

        for (int i = 0; i < letters.Length; i++)
        {
            // 각 글자의 모스부호 실행
            yield return StartCoroutine(MorseLetter(letters[i]));

            // 글자 사이 간격 (마지막 글자가 아닐 때)
            if (i < letters.Length - 1)
            {
                yield return new WaitForSeconds(gapBetweenLetters);
            }
        }
    }

    // 모스부호 글자 실행 ("."는 dot, "−"는 dash)
    IEnumerator MorseLetter(string letter)
    {
        for (int i = 0; i < letter.Length; i++)
        {
            char morse = letter[i];

            SetLightActive(true);

            if (morse == '.')
            {
                // 짧은 깜빡임 (dot)
                yield return new WaitForSeconds(dotDuration);
            }
            else if (morse == '−')
            {
                // 긴 깜빡임 (dash)
                yield return new WaitForSeconds(dashDuration);
            }

            SetLightActive(false);

            // 같은 글자 안에서 dot/dash 사이 간격
            if (i < letter.Length - 1)
            {
                yield return new WaitForSeconds(gapBetweenBlinks);
            }
        }
    }

    // 빛 활성화/비활성화
    void SetLightActive(bool active)
    {
        isActive = active;

        if (active)
        {
            fireflyLight.intensity = lightIntensity;

            var emission = glowEffect.emission;
            emission.rateOverTime = 10f;

            var main = glowEffect.main;
            main.startColor = fireflyColor;
        }
        else
        {
            fireflyLight.intensity = 0f;

            var emission = glowEffect.emission;
            emission.rateOverTime = 0f;
        }
    }

    // 길 밝히기
    void RevealPath()
    {
        if (pathsToReveal.Length == 0) return;

        isPathRevealed = true;
        StartCoroutine(RevealPathCoroutine());
    }

    IEnumerator RevealPathCoroutine()
    {
        Debug.Log($"{fireflyType} 반딧불이 길을 밝힙니다!");

        // 길 오브젝트들 활성화
        foreach (GameObject path in pathsToReveal)
        {
            if (path != null)
            {
                path.SetActive(true);

                // 페이드 인 효과
                Renderer renderer = path.GetComponent<Renderer>();
                if (renderer != null)
                {
                    StartCoroutine(FadeInPath(renderer));
                }
            }
        }

        // 지속 시간 대기
        yield return new WaitForSeconds(revealDuration);

        // 길 다시 숨기기 (잘못된 길인 경우만)
        if (fireflyType == FireflyType.WrongPath)
        {
            foreach (GameObject path in pathsToReveal)
            {
                if (path != null)
                {
                    Renderer renderer = path.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        StartCoroutine(FadeOutPath(renderer, path));
                    }
                    else
                    {
                        path.SetActive(false);
                    }
                }
            }
        }

        isPathRevealed = false;
    }

    // 페이드 인
    IEnumerator FadeInPath(Renderer renderer)
    {
        Material mat = renderer.material;
        Color originalColor = mat.color;
        originalColor.a = 0f;
        mat.color = originalColor;

        float time = 0f;
        while (time < 1f)
        {
            time += Time.deltaTime;
            Color currentColor = originalColor;
            currentColor.a = time;
            mat.color = currentColor;
            yield return null;
        }

        originalColor.a = 1f;
        mat.color = originalColor;
    }

    // 페이드 아웃
    IEnumerator FadeOutPath(Renderer renderer, GameObject pathObject)
    {
        Material mat = renderer.material;
        Color originalColor = mat.color;

        float time = 0f;
        while (time < 1f)
        {
            time += Time.deltaTime;
            Color currentColor = originalColor;
            currentColor.a = 1f - time;
            mat.color = currentColor;
            yield return null;
        }

        pathObject.SetActive(false);

        // 색상 원복
        originalColor.a = 1f;
        mat.color = originalColor;
    }

    // 플레이어와의 거리 반환
    public float GetDistanceToPlayer()
    {
        if (player == null) return float.MaxValue;
        return Vector3.Distance(transform.position, player.position);
    }

    public bool IsActive() => isActive;

    public FireflyType GetFireflyType() => fireflyType;

    public void SetFireflyType(FireflyType type)
    {
        fireflyType = type;
        SetColorByType();
    }

    public void SetBlinkPattern(BlinkPattern pattern)
    {
        blinkPattern = pattern;
    }
}