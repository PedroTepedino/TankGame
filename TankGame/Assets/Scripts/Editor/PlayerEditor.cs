using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Player))]
public class PlayerEditor : OdinEditor
{
    private void OnSceneGUI()
    {
        var player = target as Player;
        var transform = player.transform;
        
        EditorGUI.BeginChangeCheck();

        Handles.color = Color.green;
        var newRadius = Handles.RadiusHandle(transform.rotation, transform.position, player.Radius);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(player, "Changed Player");

            player.Radius = newRadius;
        }
    }
}