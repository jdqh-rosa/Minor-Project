using System;
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
    public MovementWeights Movement = new();
    public MiscellaneousValues Miscellaneous = new();
    
    [Serializable]
    public class MessengerWeights {
        [Header("Flank Message")]
        [Range(0, 100)] public int FlankWeight = 10;
        [Range(0, 100)] public int FlankMod = 10;
        public bool IsFlankModified  = false;
        [Header("Group Up Message")]
        [Range(0, 100)] public int GroupUpWeight = 10;
        [Range(0, 100)] public int GroupUpMod = 10;
        public bool IsGroupUpModified  = false;
        [Header("Retreat Message")]
        [Range(0, 100)] public int RetreatWeight = 10;
        [Range(0, 100)] public int RetreatMod = 10;
        public bool IsRetreatModified  = false;
        [Header("Backup Message")]
        [Range(0, 100)] public int BackUpWeight = 10;
        [Range(0, 100)] public int BackUpMod = 10;
        public bool IsBackUpModified  = false;
        [Header("Surround Message")]
        [Range(0, 100)] public int SurroundWeight = 10;
        [Range(0, 100)] public int SurroundMod = 10;
        public bool IsSurroundModified  = false;
    }

    [Serializable]
    public class HealthWeights {
        [Range(0, 100)] public int LowHealthWeight = 20;
        [Range(0f, 1f)] public float LowHealthThreshold = 0.3f;
        public bool IsLowHealth  = false;
    }

    [Serializable]
    public class HighLevelBranchWeights {
        [Range(0, 100)] public int CombatWeight = 50;
        [Range(0, 100)] public int PatrolWeight = 25;
        [Range(0, 100)] public int AssembleWeight = 30;
        [Range(0, 100)] public int AssembleMod = 20;
        public bool IsAssembleModified = false;
        [Range(0, 100)] public int IdleWeight = 10;
    }

    [Serializable]
    public class CombatTacticWeights {
        [Header("Surround")]
        [Range(0, 100)] public int SurroundWeight = 25;
        [Range(0, 100)] public int SurroundMod = 10;
        public bool IsSurroundModified  = false;
        [Header("Flank")]
        [Range(0, 100)] public int FlankWeight = 20;
        [Range(0, 100)] public int FlankMod = 20;
        public bool IsFlankModified  = false;
        [Header("Attack")]
        [Range(0, 100)] public int AttackTargetWeight = 30;
        [Range(0, 100)] public int AttackTargetMod = 30;
        public bool IsAttackTargetModified  = false;
        [Header("Defend")]
        [Range(0, 100)] public int DefendSelfWeight = 10;
        [Range(0, 100)] public int DefendSelfMod = 10;
        public bool IsDefendSelfModified  = false;
        [Header("Retreat")]
        [Range(0, 100)] public int RetreatWeight = 15;
        [Range(0, 100)] public int RetreatMod = 15;
        public bool IsRetreatModified  = false;
        [Range(0, 100)] public int RetreatSelfWeight = 8;
        [Range(0, 100)] public int RetreatGroupWeight = 7;
    }
    
    [Serializable]
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

    [Serializable]
    public class MovementWeights
    {
        [Range(-10, 10)] public float AlignAttackForce = 1f;
        [Range(-10, 10)] public float EnterRangeForce = 1f;
        [Range(-10, 10)] public float ChooseObjectForce = 1f;
        [Range(-10, 10)] public float DistanceSelfForce = -3f;
        [Range(-10, 10)] public float AvoidObjectForce = -2f;
        [Range(-10, 10)] public float FlankForce = 3f;
        [Range(-10, 10)] public float GroupUpForce = 3f;
        [Range(-10, 10)] public float TargetAllyForce = 1f;
        [Range(-10, 10)] public float RetreatForce = -3f;
        [Range(-10, 10)] public float SurroundForce = 3f;
    }

    [Serializable]
    public class MiscellaneousValues
    {
        [Range(0, 100)] public float FindRange = 10;
    }
    
    public class TreeValuesRuntime
    {
        public MessengerWeights Messenger;
        public HealthWeights Health;
        public HighLevelBranchWeights Decider;
        public CombatTacticWeights CombatTactic;
        public CombatAttackWeights CombatAttack;
        public DefenseWeights Defense;
        public MovementWeights Movement;
        public MiscellaneousValues Miscellaneous;

        public TreeValuesRuntime(TreeValuesSO source)
        {
            Messenger = JsonUtility.FromJson<MessengerWeights>(JsonUtility.ToJson(source.Messenger));
            Health = JsonUtility.FromJson<HealthWeights>(JsonUtility.ToJson(source.Health));
            Decider = JsonUtility.FromJson<HighLevelBranchWeights>(JsonUtility.ToJson(source.Decider));
            CombatTactic = JsonUtility.FromJson<CombatTacticWeights>(JsonUtility.ToJson(source.CombatTactic));
            CombatAttack = JsonUtility.FromJson<CombatAttackWeights>(JsonUtility.ToJson(source.CombatAttack));
            Defense = JsonUtility.FromJson<DefenseWeights>(JsonUtility.ToJson(source.Defense));
            Movement = JsonUtility.FromJson<MovementWeights>(JsonUtility.ToJson(source.Movement));
            Miscellaneous = JsonUtility.FromJson<MiscellaneousValues>(JsonUtility.ToJson(source.Miscellaneous));
        }
    }
}
