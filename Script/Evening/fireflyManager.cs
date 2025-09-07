using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class fireflyManager : MonoBehaviour
{
    [Header("반딧불 스팟 설정")]
    public List<Transform> fireflySpots = new List<Transform>();
    public GameObject fireflyPrefab;

    [Header("가장 가까운 반딧불 설정")]
    public float updateInterval = 0.5f;
    public float maxActiveDistance = 10f;

    [Header("반딧불 타입 분배")]
    [Range(0f, 1f)]
    public float correctPathRatio = 0.5f;


    [Header("깜빡임 패턴 설정")]
    public BlinkPattern correctPathPattern = BlinkPattern.CorrectPath;
    public BlinkPattern wrongPathPattern = BlinkPattern.WrongPath;

    // 상태 변수
    private List<firefly> allFireflies = new List<firefly>();
    private firefly currentNearestFirefly;
    private Transform player;

    void Start()
    {
        // 플레이어 찾기
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        // 스팟들에 반딧불 생성
        SpawnFirefliesAtSpots();

        // 주기적으로 가장 가까운 반딧불 업데이트
        InvokeRepeating(nameof(UpdateNearestFirefly), 0f, updateInterval);
    }

    // 스팟들에 반딧불 생성
    void SpawnFirefliesAtSpots()
    {
        if (fireflySpots.Count == 0)
        {
            Debug.LogWarning("반딧불 스팟이 설정되지 않았습니다!");
            return;
        }

        // 타입별 개수 계산
        int totalSpots = fireflySpots.Count;
        int correctCount = Mathf.RoundToInt(totalSpots * correctPathRatio);
        int wrongCount = totalSpots - correctCount; // 나머지는 모두 잘못된 길

        Debug.Log($"반딧불 생성: TRUE({correctCount}) FALSE({wrongCount})");

        // 타입 리스트 생성
        List<FireflyType> typeList = new List<FireflyType>();

        // 올바른 길 반딧불 추가 (TRUE)
        for (int i = 0; i < correctCount; i++)
            typeList.Add(FireflyType.CorrectPath);

        // 잘못된 길 반딧불 추가 (FALSE)
        for (int i = 0; i < wrongCount; i++)
            typeList.Add(FireflyType.WrongPath);

        // 타입 리스트 섞기 (랜덤 분배)
        for (int i = 0; i < typeList.Count; i++)
        {
            FireflyType temp = typeList[i];
            int randomIndex = Random.Range(i, typeList.Count);
            typeList[i] = typeList[randomIndex];
            typeList[randomIndex] = temp;
        }

        // 각 스팟에 반딧불 생성
        for (int i = 0; i < fireflySpots.Count; i++)
        {
            if (fireflySpots[i] == null) continue;

            GameObject newFirefly = Instantiate(fireflyPrefab, fireflySpots[i].position, fireflySpots[i].rotation);
            firefly fireflyComponent = newFirefly.GetComponent<firefly>();

            if (fireflyComponent != null)
            {
                // 타입 설정
                FireflyType assignedType = typeList[i];
                fireflyComponent.SetFireflyType(assignedType);

                // 패턴 설정
                BlinkPattern pattern = GetPatternForType(assignedType);
                fireflyComponent.SetBlinkPattern(pattern);

                allFireflies.Add(fireflyComponent);

                Debug.Log($"스팟 {i}: {assignedType} 타입, {pattern} 패턴");
            }
        }
    }

    // 타입에 따른 패턴 반환
    BlinkPattern GetPatternForType(FireflyType type)
    {
        switch (type)
        {
            case FireflyType.CorrectPath:
                return correctPathPattern;
            case FireflyType.WrongPath:
                return wrongPathPattern;
            default:
                return correctPathPattern;
        }
    }

    // 가장 가까운 반딧불 업데이트
    void UpdateNearestFirefly()
    {
        if (player == null || allFireflies.Count == 0) return;

        // 활성 거리 내의 반딧불들만 필터링
        var nearbyFireflies = allFireflies.Where(f =>
            f != null &&
            f.GetDistanceToPlayer() <= maxActiveDistance
        ).ToList();

        if (nearbyFireflies.Count == 0)
        {
            // 근처에 반딧불이 없으면 현재 활성화된 것 끄기
            if (currentNearestFirefly != null)
            {
                currentNearestFirefly.SetAsNearestFirefly(false);
                currentNearestFirefly = null;
            }
            return;
        }

        // 가장 가까운 반딧불 찾기
        firefly nearestFirefly = nearbyFireflies
            .OrderBy(f => f.GetDistanceToPlayer())
            .FirstOrDefault();

        // 가장 가까운 반딧불이 바뀌었으면 업데이트
        if (nearestFirefly != currentNearestFirefly)
        {
            // 이전 반딧불 비활성화
            if (currentNearestFirefly != null)
            {
                currentNearestFirefly.SetAsNearestFirefly(false);
            }

            // 새로운 반딧불 활성화
            currentNearestFirefly = nearestFirefly;
            if (currentNearestFirefly != null)
            {
                currentNearestFirefly.SetAsNearestFirefly(true);
                Debug.Log($"가장 가까운 반딧불: {currentNearestFirefly.GetFireflyType()} 타입");
            }
        }
    }

    // 스팟 추가
    public void AddFireflySpot(Transform spot)
    {
        if (spot != null && !fireflySpots.Contains(spot))
        {
            fireflySpots.Add(spot);
        }
    }

    // 스팟 제거
    public void RemoveFireflySpot(Transform spot)
    {
        if (fireflySpots.Contains(spot))
        {
            fireflySpots.Remove(spot);
        }
    }

    // 모든 스팟 클리어
    public void ClearAllSpots()
    {
        fireflySpots.Clear();
    }

    // 반딧불 재생성
    public void RegenerateFireflies()
    {
        // 기존 반딧불들 제거
        foreach (firefly f in allFireflies)
        {
            if (f != null)
                DestroyImmediate(f.gameObject);
        }

        allFireflies.Clear();
        currentNearestFirefly = null;

        // 새로 생성
        SpawnFirefliesAtSpots();
    }

    // 특정 타입의 반딧불들 가져오기
    public List<firefly> GetFirefliesByType(FireflyType type)
    {
        return allFireflies.Where(f => f != null && f.GetFireflyType() == type).ToList();
    }

    // 현재 활성화된 반딧불 가져오기
    public firefly GetCurrentActiveFirefly()
    {
        return currentNearestFirefly;
    }

    // 전체 반딧불 개수
    public int GetTotalFireflyCount()
    {
        return allFireflies.Count(f => f != null);
    }

    // 타입별 개수 정보
    public void PrintFireflyInfo()
    {
        int correctCount = GetFirefliesByType(FireflyType.CorrectPath).Count;
        int wrongCount = GetFirefliesByType(FireflyType.WrongPath).Count;

        Debug.Log($"반딧불 현황 - TRUE: {correctCount}, FALSE: {wrongCount}");
    }

    // 최대 활성화 거리 설정
    public void SetMaxActiveDistance(float distance)
    {
        maxActiveDistance = distance;
    }

    // 업데이트 주기 설정
    public void SetUpdateInterval(float interval)
    {
        updateInterval = interval;
        CancelInvoke(nameof(UpdateNearestFirefly));
        InvokeRepeating(nameof(UpdateNearestFirefly), 0f, updateInterval);
    }
}