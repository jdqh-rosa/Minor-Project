using System;
using Unity.VisualScripting;
using UnityEngine;

public class TreeValues : ScriptableObject
{
    [InspectorLabel("Messages")]
    [SerializeField] private int FlankMessageWeight;
    [SerializeField] private int FlankMessageModifier;
    [SerializeField] private int GroupUpMessageWeight;
    [SerializeField] private int GroupUpMessageModifier;
    [SerializeField] private int RetreatMessageWeight;
    [SerializeField] private int RetreatMessageModifier;
    [SerializeField] private int BackUpMessageWeight;
    [SerializeField] private int BackUpMessageModifier;
    [SerializeField] private int SurroundMessageWeight;
    [SerializeField] private int SurroundMessageModifier;

    [InspectorLabel("Health")]
    [SerializeField] private int LowHealthWeight;
    [SerializeField] private float lowHealthBarrier;
    
    [InspectorLabel("Decider")]
    [SerializeField] private int CombatWeight;
    [SerializeField] private int PatrolWeight;
    [SerializeField] private int AssembleWeight;
    [SerializeField] private int IdleWeight;
    
    [InspectorLabel("Combat")]
    [SerializeField] private int SurroundWeight;
    [SerializeField] private int FlankWeight;
    [SerializeField] private int AttackTargetWeight;
    [SerializeField] private int DefendSelfWeight;
    [SerializeField] private int RetreatWeight;
    [SerializeField] private int RetreatSelfWeight;
    [SerializeField] private int RetreatGroupWeight;

    [InspectorLabel("AttackTarget")] 
    [SerializeField] private int WeakAttackPreference;
    [SerializeField] private int StrongAttackPreference;
    [SerializeField] private int JabPreference;
    [SerializeField] private int SwingPreference;
    
    [Serializable]
    public class MessageWeights {
        public int FlankWeight;
        public int FlankModifier;
        public int GroupUpWeight;
        public int GroupUpModifier; 
        public int RetreatWeight;    
        public int RetreatModifier; 
        public int BackUpWeight;     
        public int BackUpModifier;   
        public int SurroundWeight;   
        public int SurroundModifier;
    }

    [Serializable]
    public class HealthWeights {
        public int LowHealthWeight;
        public float LowHealthThreshold;
    }

    [Serializable]
    public class HighLevelBranchWeights {
        public int CombatWeight;
        public int PatrolWeight;
        public int AssembleWeight;
        public int IdleWeight;
    }
    
    [Serializable]
    public class CombatWeights {
        public int SurroundWeight;
        public int FlankWeight;
        public int AttackTargetWeight;
        public int DefendSelfWeight;
        public int RetreatWeight;
        public int RetreatSelfWeight;
        public int RetreatGroupWeight;
    }

    [CreateAssetMenu(menuName = "AI/TreeValues")]
    public class TreeValuesSO : ScriptableObject
    {
        public MessageWeights message;
        public HealthWeights health;
        public HighLevelBranchWeights decider;
        public CombatWeights combat;
    }

}
