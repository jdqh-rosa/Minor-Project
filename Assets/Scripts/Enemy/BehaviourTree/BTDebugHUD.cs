using UnityEngine;

public class BTDebugHUD : MonoBehaviour
{
    public BehaviourTree tree;
    Vector2 scroll;

    void OnGUI()
    {
        if (tree == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, Screen.height - 20), GUI.skin.box);
        scroll = GUILayout.BeginScrollView(scroll);

        DrawNodeGUI(tree, 0);

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    void DrawNodeGUI(Node node, int indent)
    {
        // Indent
        GUILayout.BeginHorizontal();
        GUILayout.Space(indent * 15);

        // Label with color
        GUI.color = GetStatusColor(node);
        GUILayout.Label($"{node.Name} [{node.GetStatus()}]");
        GUI.color = Color.white;

        GUILayout.EndHorizontal();

        // Only expand the active branch
        if (node.IsActive && node.GetCurrentChildIndex() < node.children.Count)
        {
            DrawNodeGUI(node.children[node.GetCurrentChildIndex()], indent + 1);
        }
    }

    Color GetStatusColor(Node n)
    {
        return n.GetStatus() switch
        {
            Node.NodeStatus.Running => Color.yellow,
            Node.NodeStatus.Success => Color.green,
            Node.NodeStatus.Failure => Color.red,
            _                   => Color.white
        };
    }
}
