using System;
using UnityEngine;

[Serializable]
public class CharacterInfo
{
    public string Name { get; private set; }
    public float Health { get; private set; }
    public float MaxHealth { get; private set; }
    public CharacterTeam Team { get; private set; }

    public event Action HealthChanged;
    
    public CharacterInfo(UnitData pData) {
        Name = pData.CharacterName;
        Health = pData.MaxHealth;
        MaxHealth = pData.MaxHealth;
        Team = pData.CharacterTeam;
        HealthChanged = null;
    }

    public void TakeDamage(float damage) {
        Health -= damage;
        HealthChanged?.Invoke();
    }

}
