using UnityEngine;
using UnityEngine.VFX;

public class VisualizerBarEffectDriver : EffectDriverBase
{
    public float visualEffectParticleAmount = 1.0f;
    private VisualEffect[] _effectVisuals;
    private Light[] _lights;
    public Vector2 MinMaxLightIntensity;
    public Vector2 MinMaxParticleSpeed;
    private void Start()
    {
        StartEffect();
    }
    public override void StartEffect()
    {
        _bandCount = InputAudioManager.Instance.FrequencyBandCount;

        _effectParentObjects = new GameObject[_bandCount];
        _effectObjects = new GameObject[_bandCount];
        _effectVisuals = new VisualEffect[_bandCount];
        _lights = new Light[_bandCount];
        for (int i = 0; i < _bandCount; i++) 
        {
            _effectParentObjects[i] = Instantiate(EffectObjectPrefab);
            _effectObjects[i] = _effectParentObjects[i].GetComponentInChildren<MeshRenderer>().gameObject;
            _effectVisuals[i] = _effectParentObjects[i].GetComponentInChildren<VisualEffect>();
            _lights[i] = _effectParentObjects[i].GetComponentInChildren<Light>();
        }
    }
    private void Update()
    {
        DriveEffect();
    }
    protected override void DriveEffect()
    {
        InputAudioManager.SpectrumPointData[] effectPowers = InputAudioManager.Instance.GetSpectrumData();
        if (effectPowers == null)
        {
            return;
        }
        for (int i = 0; i < _bandCount; i++) 
        {
            _effectParentObjects[i].transform.position = EffectCenter + ObjectOffset * i;
            EffectPerObject(i, (float) effectPowers[i].PointValue);
        }
    }
    protected override void EffectPerObject(int effectObjectIndex, float effectPower)
    {
        base.EffectPerObject(effectObjectIndex, effectPower);
        GameObject effectObject = _effectObjects[effectObjectIndex];
        Vector3 scale = effectObject.transform.localScale;
        float scaledEffectPower = effectPower * effectAmplitudeMultiplier;
        scale.y = Mathf.Clamp(scaledEffectPower, 0.00000001f, 9999f);
        effectObject.transform.localScale = scale;
        VisualEffect visualEffect = _effectVisuals[effectObjectIndex];
        if (visualEffect != null)
        {
            visualEffect.SetVector3("Scale", scale);
            int effectParticleAmount = (int)(effectPower * effectAmplitudeMultiplier * visualEffectParticleAmount);
            visualEffect.SetInt("EmissionIntensity", effectParticleAmount);
            float particleSpeed = Mathf.Lerp(MinMaxParticleSpeed.x, MinMaxParticleSpeed.y, Mathf.Clamp01(scaledEffectPower / MaxEffectStrength));
            visualEffect.SetFloat("SpeedMultiplier", particleSpeed);
        }
        Light light = _lights[effectObjectIndex];
        if (light != null)
        {
            Vector3 lightPos = light.transform.localPosition;
            lightPos.y = scaledEffectPower / 2;
            light.transform.localPosition = lightPos;
            float lightIntensity = Mathf.Lerp(MinMaxLightIntensity.x, MinMaxLightIntensity.y, Mathf.Clamp01(scaledEffectPower / MaxEffectStrength));
            light.intensity = lightIntensity;
        }
    }
    /*private void EffectPerObject(GameObject effectObject, float effectPower, VisualEffect visualEffect)
    {
        Vector3 scale = effectObject.transform.localScale;
        scale.y = Mathf.Clamp(effectPower * effectAmplitudeMultiplier, 0.00000001f, 9999f);
        effectObject.transform.localScale = scale;
        if (visualEffect != null) 
        {
            visualEffect.SetVector3("Scale", scale);
            int effectParticleAmount = (int)(effectPower * effectAmplitudeMultiplier * visualEffectParticleAmount);
            visualEffect.SetInt("EmissionIntensity", effectParticleAmount);
        }
    }*/
}
