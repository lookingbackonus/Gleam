using UnityEngine;

public class BGMStarter : MonoBehaviour
{
    void Start()
    {
        SoundManager.Instance.PlayBGM(BGMType.CH1_Spring);
    }
}