using UnityEngine;

/// <summary>
/// Ensures that the GameObject this script is attached to persists when loading new scenes.
/// </summary>
public class DontDestroyOnLoad : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }
}
