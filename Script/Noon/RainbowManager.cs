using UnityEngine;
public class RainbowManager : MonoBehaviour
{
    [Header("무지개 설정")]
    public GameObject rainbowObject;
    public bool showDebugMessages = true;
    [Header("사운드 설정 (선택사항)")]
    public AudioSource audioSource;
    public AudioClip activationSound;
    public AudioClip deactivationSound;
    // 상태 변수
    private bool isRainbowActive = false;
    // 이벤트
    public System.Action OnRainbowActivated;
    public System.Action OnRainbowDeactivated;
    void Start()
    {
        if (rainbowObject != null)
        {
            rainbowObject.SetActive(false);
            if (showDebugMessages)
                Debug.Log("무지개 오브젝트 초기화: " + rainbowObject.name);
        }
        else
        {
            Debug.LogWarning("무지개 오브젝트가 할당되지 않았습니다!");
        }
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    public void ActivateRainbow()
    {
        if (rainbowObject == null)
        {
            Debug.LogWarning("무지개 오브젝트가 설정되지 않았습니다!");
            return;
        }
        if (isRainbowActive)
            return; // 이미 켜져 있으면 무시
        rainbowObject.SetActive(true);
        isRainbowActive = true;
        if (showDebugMessages)
            Debug.Log("무지개 활성화!");
        PlaySound(activationSound);
        OnRainbowActivated?.Invoke();
    }
    public void DeactivateRainbow()
    {
        if (!isRainbowActive)
            return;
        if (rainbowObject != null)
            rainbowObject.SetActive(false);
        isRainbowActive = false;
        if (showDebugMessages)
            Debug.Log("무지개 비활성화");
        PlaySound(deactivationSound);
        OnRainbowDeactivated?.Invoke();
    }
    public void ToggleRainbow()
    {
        if (isRainbowActive)
            DeactivateRainbow();
        else
            ActivateRainbow();
    }
    public void ForceDeactivateRainbow()
    {
        DeactivateRainbow();
    }
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
    public bool IsRainbowActive() => isRainbowActive;
    void OnDrawGizmosSelected()
    {
        if (rainbowObject != null)
        {
            Gizmos.color = isRainbowActive ? Color.magenta : Color.gray;
            Gizmos.DrawWireCube(rainbowObject.transform.position, Vector3.one * 2f);
            if (isRainbowActive)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(rainbowObject.transform.position, 3f);
            }
        }
    }
    void OnDestroy()
    {
        if (rainbowObject != null)
            rainbowObject.SetActive(false);
    }
}