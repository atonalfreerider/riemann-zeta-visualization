using UnityEngine;

/// <summary>Hides its GameObject a set time after it is shown: a label that names a step while it happens and then gets out of the way.</summary>
public class ShowFor : MonoBehaviour
{
    public float Seconds = 2f;
    float shownAt;

    void OnEnable() => shownAt = Time.time;

    void Update()
    {
        if (Time.time - shownAt > Seconds) gameObject.SetActive(false);
    }
}
