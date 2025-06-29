using UnityEngine;

[CreateAssetMenu(fileName = "New UnitData", menuName = "Character/Unit")]
public class UnitData : ScriptableObject
{
    public string CharacterName;
    public CharacterTeam CharacterTeam;
    public float MaxHealth;
}
