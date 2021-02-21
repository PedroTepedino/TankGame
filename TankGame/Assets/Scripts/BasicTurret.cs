using System;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class BasicTurret : ATurret
{
    [SerializeField] private Vector3 _shootingPoint = Vector3.zero;

    public Vector3 ShootingPoint { get => _shootingPoint; set => _shootingPoint = value; }

    public override void Shoot()
    {
        var projectile = PoolingSystem.Instance.SpawnObject(_projectileType, this.transform.TransformPoint(_shootingPoint),
            this.transform.rotation);
        projectile.GetComponent<AProjectile>()?.Fire();
    }

    private void OnDrawGizmos()
    {
        if (UnityEditor.Selection.activeGameObject == this.gameObject) return;
        
        Handles.color = Color.green;
        Handles.zTest = CompareFunction.Less;

        Handles.ConeHandleCap(0,this.transform.TransformPoint(_shootingPoint), this.transform.rotation, 0.5f, EventType.Repaint);
    }
}


#if UNITY_EDITOR

[CustomEditor(typeof(BasicTurret))]
public class BasicTurretEditor : OdinEditor
{
    private void OnSceneGUI()
    {
        var turret = target as BasicTurret;
        
        EditorGUI.BeginChangeCheck();
        
        // var newShootPosition = Handles.PositionHandle(turret.transform.position + turret.ShootingPoint, turret.transform.rotation) - turret.transform.position ;
        var newShootPosition = turret.transform.InverseTransformPoint(Handles.PositionHandle(turret.transform.TransformPoint(turret.ShootingPoint), turret.transform.rotation)) ;

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(turret, "Changed Turret");

            turret.ShootingPoint = newShootPosition;
        }
        
    }

}
#endif