using System;
using Unity.VisualScripting;
using UnityEngine;


[CreateAssetMenu(menuName = "AI/TreeValues")]
[Serializable]
public class TreeValuesSO : ScriptableObject
{
    public MessengerWeights Messenger = new();
    public HealthWeights Health = new();
    public HighLevelBranchWeights Decider = new();
    public CombatTacticWeights CombatTactic = new();
    public CombatAttackWeights CombatAttack = new();
    public DefenseWeights Defense = new();
    
    [Serializable]
    public class MessengerWeights {
        [Range(0, 100)] public int FlankWeight = 10;
        [Range(0, 100)] public int GroupUpWeight = 10;
        [Range(0, 100)] public int RetreatWeight = 10;
        [Range(0, 100)] public int BackUpWeight = 10;
        [Range(0, 100)] public int SurroundWeight = 10;
    }

    [Serializable]
    public class HealthWeights {
        [Range(0, 100)] public int LowHealthWeight = 20;
        [Range(0f, 1f)] public float LowHealthThreshold = 0.3f;
    }

    [Serializable]
    public class HighLevelBranchWeights {
        [Range(0, 100)] public int CombatWeight = 50;
        [Range(0, 100)] public int PatrolWeight = 25;
        [Range(0, 100)] public int AssembleWeight = 15;
        [Range(0, 100)] public int IdleWeight = 10;
    }

    [Serializable]
    public class CombatTacticWeights {
        [Range(0, 100)] public int SurroundWeight = 25;
        [Range(0, 100)] public int FlankWeight = 20;
        [Range(0, 100)] public int AttackTargetWeight = 30;
        [Range(0, 100)] public int DefendSelfWeight = 10;
        [Range(0, 100)] public int RetreatWeight = 15;
        [Range(0, 100)] public int RetreatSelfWeight = 8;
        [Range(0, 100)] public int RetreatGroupWeight = 7;
    }
    public class CombatAttackWeights {
        [Range(0, 100)] public int StabWeight = 25;
        [Range(0, 100)] public int SwingWeight = 20;
        [Range(0, 100)] public int StrongStabWeight = 10;
        [Range(0, 100)] public int WeakStabWeight = 30;
        [Range(0, 100)] public int StrongSwingWeight = 10;
        [Range(0, 100)] public int WeakSwingWeight = 30;
    }
    
    [Serializable]
    public class DefenseWeights {
        [Range(0, 100)] public int EvadeWeight = 10;
        [Range(0, 100)] public int ParryWeight = 10;
        [Range(0, 100)] public int BlockWeight = 10;
        [Range(0, 100)] public int RetreatWeight = 10;
    }
    
    public class TreeValuesRuntime
    {
        public MessengerWeights Message;
        public HealthWeights Health;
        public HighLevelBranchWeights Decider;
        public CombatTacticWeights CombatTactic;
        public CombatAttackWeights CombatAttack;
        public DefenseWeights Defense;

        public TreeValuesRuntime(TreeValuesSO source)
        {
            Message = JsonUtility.FromJson<MessengerWeights>(JsonUtility.ToJson(source.Messenger));
            Health = JsonUtility.FromJson<HealthWeights>(JsonUtility.ToJson(source.Health));
            Decider = JsonUtility.FromJson<HighLevelBranchWeights>(JsonUtility.ToJson(source.Decider));
            CombatTactic = JsonUtility.FromJson<CombatTacticWeights>(JsonUtility.ToJson(source.CombatTactic));
            CombatAttack = JsonUtility.FromJson<CombatAttackWeights>(JsonUtility.ToJson(source.CombatAttack));
            Defense = JsonUtility.FromJson<DefenseWeights>(JsonUtility.ToJson(source.Defense));
        }
    }
}
