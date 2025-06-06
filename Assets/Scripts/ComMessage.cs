using System.Collections.Generic;
using UnityEngine;

public class ComMessage : MonoBehaviour
{
    public EnemyController Sender { get; private set; }
    public EnemyController Receiver { get; private set; }  // null for broadcast
    public MessageType Type { get; private set; }
    public Dictionary<MessageInfoType, object> Payload { get; private set; }
    public float Timestamp { get; private set; }
    public int Priority { get; private set; }

    public ComMessage(EnemyController pSender, EnemyController pReceiver, MessageType pType, Dictionary<MessageInfoType, object> pPayload, float pTimestamp, int pPriority = 0) {
        Sender = pSender;
        Receiver = pReceiver;
        Type = pType;
        Payload = pPayload;
        Timestamp = pTimestamp;
        Priority = pPriority;
    }
}

public enum MessageType
{
    Communicate,
    EnemySpotted,
    Flank,
    GroupUp,
    RequestBackup,
    Retreat,
    SurroundTarget,
}

public enum MessageInfoType
{
    Ally,
    Allies,
    Behaviour,
    Direction,
    Distance,
    Enemy,
    Enemies,
    Object,
    Position,
}