using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField]Slider slider;

    public void ChangeHealth(float value)
    {
        slider.value = value;
    }
}
