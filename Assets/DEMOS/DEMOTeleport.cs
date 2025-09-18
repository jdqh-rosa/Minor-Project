using System.Collections;
using UnityEngine;

public class DEMOTeleport : MonoBehaviour
{
    [SerializeField] float teleportWait = 5f;
    [SerializeField] Vector2 teleportArea = new Vector2(10f, 10f);
    [SerializeField] private Rigidbody rb;
    void Start() {
        rb = GetComponent<Rigidbody>();
        StartCoroutine(TeleportAround());
    }

    IEnumerator TeleportAround() {
        while (true) {
            Vector2 newPos = new Vector2(Random.Range(-teleportArea.x, teleportArea.x), Random.Range(-teleportArea.y, teleportArea.y));

            if (!rb) {
                transform.position = new Vector3(newPos.x, transform.position.y, newPos.y);
            }
            else {
                rb.MovePosition(MiscHelper.Vec2ToVec3Pos(newPos));
            }

            yield return new WaitForSeconds(teleportWait);
        }
    }
}
