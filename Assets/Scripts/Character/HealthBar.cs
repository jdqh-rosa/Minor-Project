using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Slider slider;

    public void ChangeHealth(float pValue)
    {
        slider.value = pValue;
    }
}
