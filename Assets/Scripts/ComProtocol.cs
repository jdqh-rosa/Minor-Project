using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComProtocol : MonoBehaviour
{
    public string AgentId;
    private List<ComMessage> inbox = new List<ComMessage>();
    private List<ComMessage> outbox = new List<ComMessage>();
    private float latency = 1f;
    private int bandwidth = 5;

    public void SendComMessage(EnemyController pRecipient, ComMessage message) {
        pRecipient.ReceiveComMessage(message);
    }

    public void ReceiveComMessage(ComMessage message) {
        inbox.Add(message);
    }
    
    

    private IEnumerator ProcessInbox() {
        while (true) { //sus line
            for (int i = 0; i < bandwidth; i++) {
                foreach (ComMessage message in inbox) {
                    ProcessMessage(message);
                }
            }
            yield return new WaitForSeconds(latency);
        }
    }
    
    private void ProcessMessage(ComMessage message) {
        switch (message.Type) {
            case MessageType.Communicate:
                
                break;
            case MessageType.EnemySpotted:
                ProcessEnemySpotted(message);
                break;
            case MessageType.Flank:
                ProcessFlank(message);
                break;
            case MessageType.GroupUp:
                ProcessGroupUp(message);
                break;
            case MessageType.Retreat:
                ProcessRetreat(message);
                break;
            case MessageType.RequestBackup:
                ProcessRequestBackup(message);
                break;
            case MessageType.SurroundTarget:
                ProcessSurroundTarget(message);
                break;
        }
    }

    private void ProcessEnemySpotted(ComMessage message) {
        GameObject _enemyTarget = (GameObject)message.Payload[MessageInfoType.Enemy];
        //todo: check if already aware of enemy, if not become aware
    }
    private void ProcessFlank(ComMessage message) {
        GameObject _enemyTarget = (GameObject)message.Payload[MessageInfoType.Enemy];
        GameObject _ally  = (GameObject)message.Payload[MessageInfoType.Ally];
        Vector2 _flankDirection = (Vector2)message.Payload[MessageInfoType.Direction];
        //todo: flank enemy opposite of ally
    }
    private void ProcessGroupUp(ComMessage message) {
        List<GameObject> _groupUp = (List<GameObject>)message.Payload[MessageInfoType.Allies];
        Vector3 _groupUpPosition = (Vector3)message.Payload[MessageInfoType.Position];
    }
    private void ProcessRetreat(ComMessage message) {
        Vector3 position = (Vector3)message.Payload[MessageInfoType.Position];
        //todo: set expected position (away or towards?)
    }
    private void ProcessRequestBackup(ComMessage message) {
        GameObject _enemyTarget = (GameObject)message.Payload[MessageInfoType.Enemy];
        //todo: target enemy and attack
    }
    private void ProcessSurroundTarget(ComMessage message) {
        GameObject _enemyTarget = (GameObject)message.Payload[MessageInfoType.Enemy];
        List<GameObject> _allies = (List<GameObject>)message.Payload[MessageInfoType.Allies];
        Vector2 _surroundAngle = (Vector2)message.Payload[MessageInfoType.Direction];
        //todo: set expected position relative to target enemy
    }
}
