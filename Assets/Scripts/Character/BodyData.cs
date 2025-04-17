using UnityEngine;

[CreateAssetMenu(fileName = "New Body", menuName = "Character/Body")]
public class BodyData : ScriptableObject
{
    public float StepLength = 1f;
    public float StepDuration = 0.3f;
    public float StepFraction = 0.5f;
}
