using UnityEngine;

public class TacticIndicator : MonoBehaviour {
    [SerializeField] private Renderer indicator;
    private static readonly int ColorProp = Shader.PropertyToID("_Color");
    
    public void SetTactic(TacticType tactic) {
        indicator.material.SetColor(ColorProp, tacticToColor(tactic));
        indicator.material.color = tacticToColor(tactic);
    }
    
    private Color tacticToColor(TacticType tactic) => tactic switch {
        TacticType.Attack => Color.red,
        TacticType.Surround => Color.yellow,
        TacticType.Flank => Color.magenta,
        TacticType.Heal => Color.green,
        TacticType.Defend => Color.blue,
        TacticType.Flee => Color.black,
        TacticType.Group => Color.cyan,
        _ => Color.grey
    };
}

public enum TacticType
{
    None= 0,
    Attack,
    Surround,
    Flank,
    Heal,
    Defend,
    Flee,
    Group,
}