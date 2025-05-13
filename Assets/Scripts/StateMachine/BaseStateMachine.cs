using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

public abstract class BaseStateMachine<TStateMachine> : MonoBehaviour
    where TStateMachine : BaseStateMachine<TStateMachine>
{
    public string customName;
    public BaseState<TStateMachine> InitialState;
    protected BaseState<TStateMachine> currentState;
    protected BaseState<TStateMachine> nextState;
    protected Dictionary<string, BaseState<TStateMachine>> states = new Dictionary<string, BaseState<TStateMachine>>();

    protected virtual void Start()
    {
        foreach (string s in states.Keys) {
            states[s].StateMachine = (TStateMachine)this;
            states[s].Ready();
            states[s].Exit();
        }
        
        currentState = states[InitialState.Name];
        
        if (currentState != null) {
            currentState.Enter((TStateMachine)this);
        }
    }

    protected virtual void Update()
    {
        if (currentState != null) {
            currentState.UpdateLogic(Time.deltaTime);
        }
    }

    protected virtual void FixedUpdate()
    {
        if (currentState != null) {
            currentState.UpdatePhysics(Time.deltaTime);
        }
    }

    public virtual void TransitionToState(BaseState<TStateMachine> pNewState) {
        TransitionToState(pNewState.Name);
    }
    
    public virtual void TransitionToState(string pNewState) {
        Debug.Log($"Switching States from current state {currentState.Name} to {pNewState}", this);
        if (!states.ContainsKey(pNewState)) return;
        currentState?.Exit();
        
        currentState = states[pNewState];
        currentState?.Enter((TStateMachine)this);
    }

    public virtual void EndCurrentState() {
        currentState.Exit();
        currentState = null;
    }

    public virtual BaseState<TStateMachine> GetState(string pStateName) {
        if (!states.TryGetValue(pStateName, out BaseState<TStateMachine> state)) return null;
        return state;
    }

    public virtual void AddState(BaseState<TStateMachine> pNewState) {
        if (GetState(pNewState.Name) != null) {
            Debug.LogWarning("Duplicate state: " + pNewState.Name + ". Previous state overwritten.");
        }
        
        states[pNewState.Name] = pNewState;
    }

    public virtual BaseState<TStateMachine> GetCurrentState() {
        return currentState;
    }

    private void OnGUI()
    {
        string content = currentState != null ? currentState.Name : "(no current state)";
        GUILayout.Label($"<color='black'><size=40>{content}</size></color>");
    }
}
