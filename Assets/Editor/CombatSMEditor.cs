using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CombatSM))]
public class CombatSMEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

#if UNITY_EDITOR
        CombatSM combatSM = (CombatSM)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Editor States", EditorStyles.boldLabel);

        if (GUILayout.Button("Apply to Dictionary"))
        {
            foreach (var stateObj in combatSM.EditorStates)
            {
                if (stateObj == null) continue;

                if (stateObj.StateMachine == null)
                {
                    stateObj.Enter(combatSM);
                }

                combatSM.AddState(stateObj);
            }

            EditorUtility.SetDirty(combatSM);
            Debug.Log("States added to CombatSM.");
        }

        serializedObject.ApplyModifiedProperties();
#endif
    }
}
