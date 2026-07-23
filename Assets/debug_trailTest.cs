using TheBlackCat.TrailEffect2D;
using UnityEngine;

public class debug_trailTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TrailManager.Instance.StartTrail(gameObject);
    }
}
