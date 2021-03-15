using System;
using DG.Tweening;
using UnityEngine;

public class VisualEffectsManager : MonoBehaviour
{
    private Material _emissiveMaterial;

    [SerializeField] private MeshRenderer _meshRenderer;

    private readonly int _emissiveProperty = Shader.PropertyToID("_Intensity");

    private Tween _turnIntensityOffTween;

    private void Awake()
    {
        foreach (var mat in _meshRenderer.sharedMaterials)
        {
            if (mat.HasProperty(_emissiveProperty))
                _emissiveMaterial = mat;
        }

        _turnIntensityOffTween = DOTween.To(() => _emissiveMaterial.GetFloat(_emissiveProperty),
            x => _emissiveMaterial.SetFloat(_emissiveProperty, x), 0, 0.5f)
            .From(1f)
            .SetAutoKill(false)
            .SetEase(Ease.OutExpo);
        
        _turnIntensityOffTween.Rewind();
    }

    public void TurnOfLights() => _turnIntensityOffTween.Restart();
    public void TurnOnLights() => _turnIntensityOffTween.SmoothRewind();
}