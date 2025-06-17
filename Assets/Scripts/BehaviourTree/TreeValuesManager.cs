using System;
using UnityEngine;

[Serializable]
public class TreeValuesManager : MonoBehaviour
{
    [SerializeField] private TreeValuesSO baseValues;  // assign in Inspector
    public TreeValuesSO runtimeValues { get; private set; }

    private EnemyBlackboard blackboard;
    private EnemyController  self;

    public TreeValuesManager(EnemyBlackboard pBlackboard, EnemyController pSelf) {
        blackboard = pBlackboard;
        self = pSelf;
    }
    
    void Awake()
    {
        runtimeValues = Instantiate(baseValues);
        
        // 3) Immediately push “AgentSelf” into blackboard so strategies can read it
        blackboard.SetKeyValue(CommonKeys.AgentSelf, self);
    }
}
