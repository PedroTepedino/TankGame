using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;

[CustomEditor(typeof(BasicTurret))]
public class BasicTurretEditor : OdinEditor
{
    private void OnSceneGUI()
    {
        var turret = target as BasicTurret;
        
        EditorGUI.BeginChangeCheck();
        
        // var newShootPosition = Handles.PositionHandle(turret.transform.position + turret.ShootingPoint, turret.transform.rotation) - turret.transform.position ;
        Handles.color = Color.green;
        var newShootPosition = turret.transform.InverseTransformPoint(Handles.PositionHandle(turret.transform.TransformPoint(turret.ShootingPoint), turret.transform.rotation)) ;

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(turret, "Changed Turret");

            turret.ShootingPoint = newShootPosition;
        }
        
    }
}