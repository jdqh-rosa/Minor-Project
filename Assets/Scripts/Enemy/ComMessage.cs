using System.Collections.Generic;
using UnityEngine;

public class ComMessage
{
    public EnemyController Sender { get; private set; }
    public EnemyController Receiver { get; private set; }
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

        if (Sender == null || Type == default || Payload == null) {
            Debug.Log("Sender or Type or Payload can't be null");
        }
    }
}

public enum MessageType
{
    None = default,
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
    DirectionAngle,
    DirectionVector,
    Distance,
    Enemy,
    Enemies,
    Object,
    Position,
}