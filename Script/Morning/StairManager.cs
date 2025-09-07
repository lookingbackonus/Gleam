using UnityEngine;

public class StairManager : MonoBehaviour
{
    [Header("Stair Prefabs")]
    public GameObject stair1Prefab;
    public GameObject stair2Prefab;
    public GameObject trapPrefab;

    [Header("Gimmick Prefabs")]
    public GameObject petalPrefab;
    public GameObject plantPrefab;
    public GameObject puddlePrefab;

    [Header("Gimmick Y Offsets")]
    public float petalYOffset = 0.1f;
    public float plantYOffset = 0.2f;
    public float puddleYOffset = 0.05f;

    public int stairsToSpawn = 77;
    [Range(0f, 0.5f)]
    public float gimmickXOffsetRate = 0.3f;

    public RainController rainController;

    [Header("Stair Spacing")]
    public float stairSpacingZ = 1.3f;
    public float stairHeightY = 0.3f;

    private Vector3 localSpawnPos = new Vector3(0, 0, 2);

    private int lastGimmickPosition = -1;
    private int currentStairIndex = 0; // 현재 생성 중인 계단의 인덱스

    void Start()
    {
        currentStairIndex = 0; // 인덱스 초기화
        for (int i = 0; i < stairsToSpawn; i++)
        {
            SpawnStairWithGimmick();
            currentStairIndex++;
        }
    }

    void SpawnStairWithGimmick()
    {
        // 마지막 계단인지 확인
        bool isLastStair = (currentStairIndex == stairsToSpawn - 1);

        GameObject selectedStairPrefab = SelectStairPrefab(isLastStair);

        if (selectedStairPrefab == null)
        {
            Debug.LogError("선택된 계단 프리팹이 null입니다!");
            return;
        }

        Vector3 worldSpawnPos = transform.TransformPoint(localSpawnPos);
        GameObject stair = Instantiate(selectedStairPrefab, worldSpawnPos, transform.rotation);
        stair.transform.parent = this.transform;

        bool isTrapStair = (selectedStairPrefab == trapPrefab);

        MeshRenderer mesh = stair.GetComponentInChildren<MeshRenderer>();
        if (mesh == null)
        {
            goto SkipGimmick;
        }

        // 마지막 계단이거나 트랩 계단이면 기믹 생성 안 함
        if (isLastStair || isTrapStair)
        {
            goto SkipGimmick;
        }

        Bounds bounds = mesh.bounds;
        float width = bounds.size.x;

        float rand = Random.value;
        if (rand > 0.4f)
        {
            int gimmickType = Random.Range(0, 3);
            GameObject gimmickPrefab = null;
            float gimmickYOffsetFinal = 0.1f;

            switch (gimmickType)
            {
                case 0:
                    if (petalPrefab != null)
                    {
                        gimmickPrefab = petalPrefab;
                        gimmickYOffsetFinal = petalYOffset;
                    }
                    break;
                case 1:
                    if (plantPrefab != null)
                    {
                        gimmickPrefab = plantPrefab;
                        gimmickYOffsetFinal = plantYOffset;
                    }
                    break;
                case 2:
                    if (rainController != null && !rainController.HasRained)
                    {
                        goto SkipGimmick;
                    }

                    if (puddlePrefab != null)
                    {
                        gimmickPrefab = puddlePrefab;
                        gimmickYOffsetFinal = puddleYOffset;
                    }
                    break;
            }

            if (gimmickPrefab != null)
            {
                AddGimmickToStair(stair, gimmickPrefab, bounds, width, gimmickYOffsetFinal);
            }
        }

    SkipGimmick:
        localSpawnPos.y += stairHeightY;
        localSpawnPos.z += stairSpacingZ;
    }

    GameObject SelectStairPrefab(bool isLastStair = false)
    {
        // 마지막 계단이면 무조건 일반 계단 중에서만 선택 (trap 제외)
        if (isLastStair)
        {
            float rand = Random.value;
            if (rand <= 0.5f)
            {
                return stair1Prefab;
            }
            else
            {
                return stair2Prefab;
            }
        }

        // 일반적인 계단 선택 로직
        float randNormal = Random.value;
        if (randNormal <= 0.45f)
        {
            return stair1Prefab;
        }
        else if (randNormal <= 0.9f)
        {
            return stair2Prefab;
        }
        else
        {
            return trapPrefab;
        }
    }

    public void SpawnPuddlesOnExistingStairs()
    {
        if (puddlePrefab == null)
        {
            Debug.LogError("puddlePrefab이 null입니다!");
            return;
        }

        int puddleCount = 0;
        int eligibleStairs = 0;
        int stairIndex = 0;

        foreach (Transform stair in transform)
        {
            // 마지막 계단인지 확인 (가장 위쪽 계단)
            bool isLastStair = (stairIndex == transform.childCount - 1);

            MeshRenderer mesh = stair.GetComponentInChildren<MeshRenderer>();
            if (mesh == null)
            {
                stairIndex++;
                continue;
            }

            if (stair.Find("GimmickAnchor") != null || stair.Find("PuddleAnchor") != null)
            {
                stairIndex++;
                continue;
            }

            // 마지막 계단이면 물웅덩이 생성 안 함
            if (isLastStair)
            {
                stairIndex++;
                continue;
            }

            eligibleStairs++;

            float rand = Random.value;
            if (rand > 0.2f)
            {
                stairIndex++;
                continue;
            }

            Bounds bounds = mesh.bounds;
            float width = bounds.size.x;

            int posIndex = GetRandomPositionExcludingLast(-1);
            float xOffset = width * gimmickXOffsetRate;
            float localX = CalculateLocalX(posIndex, xOffset);

            float y = bounds.max.y + puddleYOffset;
            float z = bounds.center.z;
            Vector3 baseWorld = new Vector3(bounds.center.x, y, z);
            Vector3 anchorLocal = stair.InverseTransformPoint(baseWorld);
            anchorLocal.x += localX;

            GameObject anchor = new GameObject("PuddleAnchor");
            anchor.transform.parent = stair;
            anchor.transform.localPosition = anchorLocal;
            anchor.transform.localRotation = Quaternion.identity;
            anchor.transform.localScale = Vector3.one;

            GameObject puddle = Instantiate(puddlePrefab, anchor.transform);
            puddle.transform.localPosition = Vector3.zero;
            puddle.transform.localRotation = Quaternion.identity;
            puddle.transform.localScale = Vector3.one;

            puddleCount++;
            stairIndex++;
        }
    }

    void AddGimmickToStair(GameObject stair, GameObject prefab, Bounds bounds, float width, float yOffset)
    {
        int posIndex = GetRandomPositionExcludingLast(lastGimmickPosition);
        lastGimmickPosition = posIndex;

        float xOffset = width * gimmickXOffsetRate;
        float localX = CalculateLocalX(posIndex, xOffset);

        float y = bounds.max.y + yOffset;
        float z = bounds.center.z;

        Vector3 baseWorld = new Vector3(bounds.center.x, y, z);
        Vector3 anchorLocal = stair.transform.InverseTransformPoint(baseWorld);
        anchorLocal.x += localX;

        GameObject anchor = new GameObject("GimmickAnchor");
        anchor.transform.parent = stair.transform;
        anchor.transform.localPosition = anchorLocal;
        anchor.transform.localRotation = Quaternion.identity;
        anchor.transform.localScale = Vector3.one;

        GameObject gimmick = Instantiate(prefab, anchor.transform);
        gimmick.transform.localPosition = Vector3.zero;
        gimmick.transform.localRotation = Quaternion.identity;
        gimmick.transform.localScale = Vector3.one;
    }

    int GetRandomPositionExcludingLast(int excludePosition)
    {
        int posIndex;
        int attempts = 0;

        do
        {
            posIndex = Random.Range(0, 5);
            attempts++;
        }
        while (posIndex == excludePosition && attempts < 10);

        return posIndex;
    }

    float CalculateLocalX(int posIndex, float xOffset)
    {
        switch (posIndex)
        {
            case 1: return -xOffset;
            case 2: return xOffset;
            case 3: return -xOffset * 1.5f;
            case 4: return xOffset * 1.5f;
            default: return 0f;
        }
    }
}