using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightingManager : MonoBehaviour
{
    [SerializeField] private Light DirectionalLight = null;
    [SerializeField] private LightingPreset Preset = null;
    
    [SerializeField] private float TimeOfDay = 0;
    [SerializeField] private float TimePerDay = 24000;

    public Transform viewer;

    private void Start(){
        RenderSettings.fogDensity = Preset.FogIntensity;
    }

    private void Update(){
        TimeOfDay += Time.deltaTime;
        TimeOfDay %= TimePerDay;
        UpdateLighting(TimeOfDay/TimePerDay);
    }

    private void UpdateLighting(float timePercent){
        RenderSettings.ambientLight = Preset.AmbientColor.Evaluate(timePercent);
        RenderSettings.fogColor = Preset.FogColor.Evaluate(timePercent);
        DirectionalLight.color = Preset.DirectionalColor.Evaluate(timePercent);
        DirectionalLight.transform.localRotation = Quaternion.Euler(new Vector3((timePercent*360f) - 90f, -170, 0));
        DirectionalLight.transform.position = viewer.position;
    }

}
