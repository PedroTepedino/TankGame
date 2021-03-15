using System;
using UnityEngine;
using UnityEngine.UI;

public class HeatManagerUi : MonoBehaviour
{
    [SerializeField] private Image _barFill;
    private HeatManager _heatManager;

    private void OnEnable()
    {
        _heatManager = FindObjectOfType<Player>().HeatManager;

        if (_heatManager == null)
        {
            this.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        _barFill.fillAmount = _heatManager.HeatPercentage;
    }
}
