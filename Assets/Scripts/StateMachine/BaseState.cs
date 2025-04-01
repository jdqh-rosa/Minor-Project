using UnityEngine;

public abstract class BaseState<TStateMachine>
    where TStateMachine : BaseStateMachine<TStateMachine>
{
    public string Name;
    public TStateMachine StateMachine;
    
    public BaseState(string pName) {
        Name = pName;
    }

    public virtual void Enter(TStateMachine pStateMachine)
    {
        StateMachine = pStateMachine;
    }
    public virtual void Exit(){}

    public virtual void Ready() { }
    public virtual void UpdateLogic(float delta){}
    public virtual void UpdatePhysics(float delta){}
    //public virtual void HandleInput(InputEvent @event) {}
    
}
