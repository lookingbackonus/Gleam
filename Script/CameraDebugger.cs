using UnityEngine;
using System.Collections;

public class CameraDebugger : MonoBehaviour
{
    [Header("디버깅 설정")]
    public bool enableDebug = true;
    public float debugInterval = 1f;

    private void Start()
    {
        if (enableDebug)
        {
            InvokeRepeating(nameof(DebugAllCameras), 0f, debugInterval);

            // MazeTeleport 이벤트 감지
            StartCoroutine(MonitorMazeTeleport());
        }
    }

    private IEnumerator MonitorMazeTeleport()
    {
        while (true)
        {
            if (MazeTeleport.IsTeleporting)
            {
                Debug.Log("[CameraDebugger] ⚠️ MazeTeleport.IsTeleporting = TRUE 감지!");
                DebugDetailedCameraState();
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    [ContextMenu("모든 카메라 상태 출력")]
    public void DebugAllCameras()
    {
        Camera[] allCameras = FindObjectsOfType<Camera>();

        Debug.Log($"[CameraDebugger] ========== 카메라 상태 ({allCameras.Length}개) ==========");

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Debug.Log($"[CameraDebugger] 🎯 Camera.main: {mainCamera.name} (Active: {mainCamera.gameObject.activeInHierarchy})");
        }
        else
        {
            Debug.Log($"[CameraDebugger] ❌ Camera.main이 null입니다!");
        }

        for (int i = 0; i < allCameras.Length; i++)
        {
            Camera cam = allCameras[i];
            string status = cam.gameObject.activeInHierarchy ? "🟢 ACTIVE" : "🔴 INACTIVE";
            string tag = cam.tag;
            string mainCamMark = (cam == mainCamera) ? " [MAIN]" : "";

            Debug.Log($"[CameraDebugger] {i + 1}. {cam.name} - {status} - Tag: {tag}{mainCamMark}");
        }

        // Player 정보
        DebugPlayerCamera();

        // DandelionCameraManager 정보
        DebugDandelionCameraManager();

        // MazeTeleport 정보
        DebugMazeTeleport();

        Debug.Log($"[CameraDebugger] ========================================");
    }

    private void DebugPlayerCamera()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            PlayerCol playerCol = playerObj.GetComponent<PlayerCol>();
            if (playerCol != null && playerCol.player != null)
            {
                Player player = playerCol.player;
                if (player.mainCamera != null)
                {
                    bool isActive = player.mainCamera.gameObject.activeInHierarchy;
                    Debug.Log($"[CameraDebugger] 👤 Player.mainCamera: {player.mainCamera.name} (Active: {isActive})");
                }
                else
                {
                    Debug.Log($"[CameraDebugger] ❌ Player.mainCamera가 null입니다!");
                }
            }
        }
    }

    private void DebugDandelionCameraManager()
    {
        DandelionCameraManager dcm = FindFirstObjectByType<DandelionCameraManager>();
        if (dcm != null)
        {
            Debug.Log($"[CameraDebugger] 🌼 DandelionCameraManager.IsStageActive: {dcm.IsStageActive}");
        }
        else
        {
            Debug.Log($"[CameraDebugger] ❌ DandelionCameraManager를 찾을 수 없습니다!");
        }
    }

    private void DebugMazeTeleport()
    {
        MazeTeleport mt = FindFirstObjectByType<MazeTeleport>();
        if (mt != null)
        {
            Debug.Log($"[CameraDebugger] 🌀 MazeTeleport.IsTeleporting: {MazeTeleport.IsTeleporting}");
            Debug.Log($"[CameraDebugger] 🌀 MazeTeleport.enableTeleport: {mt.enableTeleport}");
        }
        else
        {
            Debug.Log($"[CameraDebugger] ❌ MazeTeleport를 찾을 수 없습니다!");
        }
    }

    [ContextMenu("상세 카메라 상태 출력")]
    public void DebugDetailedCameraState()
    {
        Debug.Log("[CameraDebugger] ========== 상세 카메라 디버깅 ==========");

        DebugAllCameras();

        // 추가 상세 정보
        Camera[] allCameras = FindObjectsOfType<Camera>();
        foreach (Camera cam in allCameras)
        {
            if (cam.name.Contains("Dandelion") || cam.name.Contains("dandelion"))
            {
                Debug.Log($"[CameraDebugger] 🌼 민들레 카메라 발견: {cam.name}");
                Debug.Log($"    - GameObject Active: {cam.gameObject.activeSelf}");
                Debug.Log($"    - In Hierarchy: {cam.gameObject.activeInHierarchy}");
                Debug.Log($"    - Component Enabled: {cam.enabled}");
                Debug.Log($"    - Tag: {cam.tag}");
                Debug.Log($"    - Position: {cam.transform.position}");
            }
        }
    }

    [ContextMenu("플레이어 카메라 강제 활성화")]
    public void ForceActivatePlayerCamera()
    {
        Debug.Log("[CameraDebugger] 플레이어 카메라 강제 활성화 시도...");

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            PlayerCol playerCol = playerObj.GetComponent<PlayerCol>();
            if (playerCol != null && playerCol.player != null)
            {
                Player player = playerCol.player;
                if (player.mainCamera != null)
                {
                    // 모든 카메라 비활성화
                    Camera[] allCameras = FindObjectsOfType<Camera>();
                    foreach (Camera cam in allCameras)
                    {
                        cam.gameObject.SetActive(false);
                        cam.tag = "Untagged";
                    }

                    // 플레이어 카메라만 활성화
                    player.mainCamera.gameObject.SetActive(true);
                    player.mainCamera.tag = "MainCamera";

                    Debug.Log($"[CameraDebugger] ✅ 플레이어 카메라 강제 활성화 완료: {player.mainCamera.name}");
                }
                else
                {
                    Debug.Log("[CameraDebugger] ❌ Player.mainCamera가 null입니다!");
                }
            }
        }

        // 결과 확인
        StartCoroutine(DelayedDebug());
    }

    private IEnumerator DelayedDebug()
    {
        yield return new WaitForSeconds(0.1f);
        DebugAllCameras();
    }

    [ContextMenu("모든 민들레 카메라 강제 비활성화")]
    public void ForceDeactivateDandelionCameras()
    {
        Debug.Log("[CameraDebugger] 모든 민들레 카메라 강제 비활성화...");

        Camera[] allCameras = FindObjectsOfType<Camera>();
        int deactivatedCount = 0;

        foreach (Camera cam in allCameras)
        {
            if (cam.name.Contains("Dandelion") || cam.name.Contains("dandelion"))
            {
                cam.gameObject.SetActive(false);
                cam.tag = "Untagged";
                deactivatedCount++;
                Debug.Log($"[CameraDebugger] 🔴 비활성화: {cam.name}");
            }
        }

        Debug.Log($"[CameraDebugger] ✅ {deactivatedCount}개의 민들레 카메라를 비활성화했습니다.");

        // 결과 확인
        StartCoroutine(DelayedDebug());
    }

    private void OnGUI()
    {
        if (!enableDebug) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("카메라 디버거", new GUIStyle() { fontSize = 16, normal = new GUIStyleState() { textColor = Color.white } });

        if (GUILayout.Button("카메라 상태 출력"))
        {
            DebugAllCameras();
        }

        if (GUILayout.Button("플레이어 카메라 강제 활성화"))
        {
            ForceActivatePlayerCamera();
        }

        if (GUILayout.Button("민들레 카메라 강제 비활성화"))
        {
            ForceDeactivateDandelionCameras();
        }

        GUILayout.Label($"IsTeleporting: {MazeTeleport.IsTeleporting}");

        GUILayout.EndArea();
    }
}