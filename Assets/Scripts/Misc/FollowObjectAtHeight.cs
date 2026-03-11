using UnityEngine;

public class FollowObjectAtHeight : MonoBehaviour
{
    [SerializeField] private GameObject target;
    [SerializeField] private float height;
    void Update()
    {
        if(!target || !target.activeSelf) return;
        transform.position = target.transform.position + Vector3.up * height;
    }
}
