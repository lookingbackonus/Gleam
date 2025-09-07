using UnityEngine;
using System;
using System.Collections;

public class DandelionCameraManager : MonoBehaviour
{
    [Header("카메라 설정")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Camera dandelionStageCamera;

    [Header("태그 설정")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string stageStartTag = "DandelionStage";

    [Header("무지개 연출 설정")]
    [SerializeField] private Vector3 rainbowCameraPosition = new Vector3(136.7f, 79.1f, 167.1f);
    [SerializeField] private Vector3 rainbowCameraStartRotation = new Vector3(6f, -34.97f, 0f);
    [SerializeField] private Vector3 rainbowCameraEndRotation = new Vector3(33f, -34.97f, 0f);
    [SerializeField] private float rainbowCinematicDuration = 5f;

    public static event Action OnStageStarted;
    public static event Action OnStageEnded;

    public bool IsStageActive { get; private set; }

    private Transform playerTransform;
    private CollisionDetector collisionDetector;

    private bool rainbowHandled = false;

    private Vector3 originalDandelionPosition;
    private Quaternion originalDandelionRotation;
    private bool originalPositionSaved = false;

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeCameras();
    }

    private void Start()
    {
        SetupPlayer();

        RainbowManager rainbowManager = GameObject.FindFirstObjectByType<RainbowManager>();
        if (rainbowManager != null)
        {
            rainbowManager.OnRainbowActivated += OnRainbowActivated;
        }
    }

    private void OnDestroy()
    {
        CleanupEventSubscriptions();

        RainbowManager rainbowManager = GameObject.FindFirstObjectByType<RainbowManager>();
        if (rainbowManager != null)
        {
            rainbowManager.OnRainbowActivated -= OnRainbowActivated;
        }
    }
    #endregion

    #region Initialization
    private void InitializeCameras()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerCamera == null)
        {
            Debug.LogError("[DandelionCameraManager] 플레이어 카메라를 찾을 수 없습니다.");
            return;
        }

        if (dandelionStageCamera == null)
        {
            Debug.LogError("[DandelionCameraManager] 민들레 스테이지 카메라가 설정되지 않았습니다.");
            return;
        }

        SaveOriginalDandelionCameraTransform();
        SetCameraState(playerCamera, true);
        SetCameraState(dandelionStageCamera, false);
    }

    private void SaveOriginalDandelionCameraTransform()
    {
        if (dandelionStageCamera != null && !originalPositionSaved)
        {
            originalDandelionPosition = dandelionStageCamera.transform.position;
            originalDandelionRotation = dandelionStageCamera.transform.rotation;
            originalPositionSaved = true;
        }
    }

    private void SetupPlayer()
    {
        var playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject == null)
        {
            Debug.LogError($"[DandelionCameraManager] {playerTag} 태그를 가진 오브젝트를 찾을 수 없습니다.");
            return;
        }

        playerTransform = playerObject.transform;
        SetupCollisionDetection(playerObject);
    }

    private void SetupCollisionDetection(GameObject playerObject)
    {
        collisionDetector = playerObject.GetComponent<CollisionDetector>();
        if (collisionDetector == null)
        {
            collisionDetector = playerObject.AddComponent<CollisionDetector>();
        }

        collisionDetector.OnCollisionWithTag += HandlePlayerCollision;
    }
    #endregion

    #region Event Handlers
    private void HandlePlayerCollision(string tag)
    {
        if (rainbowHandled) return;

        if (tag == stageStartTag && !IsStageActive)
        {
            StartStage();
        }
    }

    private void OnRainbowActivated()
    {
        if (!rainbowHandled)
        {
            rainbowHandled = true;
            StartCoroutine(RainbowCinematic());
        }
    }
    #endregion

    #region Rainbow Cinematic
    private IEnumerator RainbowCinematic()
    {
        SoundManager.Instance.PlaySFX(SFXCategory.CH1_Spring, SFXSubCategory.Noon, "Rainbow");

        if (!IsStageActive)
        {
            SetCameraState(playerCamera, false);
            SetCameraState(dandelionStageCamera, true);
            IsStageActive = true;
        }

        dandelionStageCamera.transform.position = rainbowCameraPosition;
        dandelionStageCamera.transform.rotation = Quaternion.Euler(rainbowCameraStartRotation);

        float elapsedTime = 0f;
        Vector3 startRotation = rainbowCameraStartRotation;
        Vector3 endRotation = rainbowCameraEndRotation;

        while (elapsedTime < rainbowCinematicDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / rainbowCinematicDuration;

            Vector3 currentRotation = Vector3.Lerp(startRotation, endRotation, t);
            dandelionStageCamera.transform.rotation = Quaternion.Euler(currentRotation);

            yield return null;
        }

        dandelionStageCamera.transform.rotation = Quaternion.Euler(endRotation);

        FadeManager.Instance.FadeOut(() =>
        {
            EndStage();
            RestoreOriginalDandelionCameraTransform();
        });
    }

    private void RestoreOriginalDandelionCameraTransform()
    {
        if (dandelionStageCamera != null && originalPositionSaved)
        {
            dandelionStageCamera.transform.position = originalDandelionPosition;
            dandelionStageCamera.transform.rotation = originalDandelionRotation;
        }
    }
    #endregion

    #region Public Methods
    public void StartStage()
    {
        if (IsStageActive) return;

        if (!ValidateCameras()) return;

        if (rainbowHandled)
        {
            return;
        }

        IsStageActive = true;

        SwitchCamera(playerCamera, dandelionStageCamera);

        OnStageStarted?.Invoke();
    }

    public void EndStage()
    {
        if (!IsStageActive) return;

        if (!ValidateCameras()) return;

        IsStageActive = false;

        SwitchCamera(dandelionStageCamera, playerCamera);

        OnStageEnded?.Invoke();
    }
    #endregion

    #region Private Helper Methods
    private void SwitchCamera(Camera fromCamera, Camera toCamera)
    {
        SetCameraState(fromCamera, false);
        SetCameraState(toCamera, true);
    }

    private void SetCameraState(Camera camera, bool isActive)
    {
        if (camera == null) return;

        camera.gameObject.SetActive(isActive);
        camera.tag = isActive ? "MainCamera" : "Untagged";
    }

    private bool ValidateCameras()
    {
        if (playerCamera == null)
        {
            Debug.LogError("[DandelionCameraManager] 플레이어 카메라가 null입니다.");
            return false;
        }

        if (dandelionStageCamera == null)
        {
            Debug.LogError("[DandelionCameraManager] 민들레 스테이지 카메라가 null입니다.");
            return false;
        }

        return true;
    }

    private void CleanupEventSubscriptions()
    {
        if (collisionDetector != null)
        {
            collisionDetector.OnCollisionWithTag -= HandlePlayerCollision;
        }
    }
    #endregion
}