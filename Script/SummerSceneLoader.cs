using UnityEngine;

public class SummerSceneLoader : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerCol playerCol = other.GetComponent<PlayerCol>();
            if (playerCol != null)
            {
                CustomSceneManager.Instance.LoadCH2();
            }
        }
    }
}