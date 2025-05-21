using UnityEngine;
using UnityEngine.UI;

public class BoostUI : MonoBehaviour
{
    [SerializeField] Slider boostSlider;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        boostSlider.value = 1;

    }

    // Update is called once per frame
    void Update()
    {
        //boostSlider.value = Mathf.Clamp01(boostSlider.value - Time.deltaTime * 0.1f);
        //if (boostSlider.value <= 0)
        //{
        //    boostSlider.value = 1;
        //}
    }

    public void SetBoost(float value)
    {
        boostSlider.value = Mathf.Clamp01(value);
    }
}
