using UnityEngine;

public class RocketPersistence : MonoBehaviour
{
  private void Awake()
  {
    DontDestroyOnLoad(gameObject);
  }
}
