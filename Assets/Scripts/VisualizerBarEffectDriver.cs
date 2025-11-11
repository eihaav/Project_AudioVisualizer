using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.VFX;

public class VisualizerBarEffectDriver : EffectDriverBase
{
    public float visualEffectParticleAmount = 1.0f;
    private VisualEffect[] _effectVisuals;
    private Light[] _lights;
    public Vector2 MinMaxLightIntensity;
    public Vector2 MinMaxParticleSpeed;
    private Material _barMaterial;
    private Volume _volume;
    private VolumeProfile _volumeProfile;
    private void Start()
    {
        StartEffect();
    }
    public override void StartEffect()
    {
        _volume = FindAnyObjectByType<Volume>();
        _volumeProfile = _volume.sharedProfile;
        _bandCount = InputAudioManager.Instance.FrequencyBandCount;

        _effectParentObjects = new GameObject[_bandCount];
        _effectObjects = new GameObject[_bandCount];
        _effectVisuals = new VisualEffect[_bandCount];
        _lights = new Light[_bandCount];
        for (int i = 0; i < _bandCount; i++) 
        {
            _effectParentObjects[i] = Instantiate(EffectObjectPrefab);
            MeshRenderer barRenderer = _effectParentObjects[i].GetComponentInChildren<MeshRenderer>();
            if (_barMaterial == null) 
            {
                _barMaterial = barRenderer.material;
            }
            barRenderer.sharedMaterial = _barMaterial;
            _effectObjects[i] = barRenderer.gameObject;
            _effectVisuals[i] = _effectParentObjects[i].GetComponentInChildren<VisualEffect>();
            _lights[i] = _effectParentObjects[i].GetComponentInChildren<Light>();
        }
        ColorPresetManager.Instance.SetActiveEffectDriver(this);
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
    public override void SetColorScheme(Color color1, Color color2)
    {
        _barMaterial.SetColor("_ColorGradient1", color1);
        _barMaterial.SetColor("_ColorGradient2", color2);
        foreach (Light light in _lights) 
        {
            light.color = color2;
        }
        Gradient gradient = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[2];
        colorKeys[0].time = 0.33f;
        colorKeys[0].color = color2 * 16f;
        colorKeys[1].time = 0.66f;
        colorKeys[1].color = color1 * 16f;
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[3];
        alphaKeys[0].time = 0f;
        alphaKeys[0].alpha = 1.0f;
        alphaKeys[1].time = 0.8f;
        alphaKeys[1].alpha = 1.0f;
        alphaKeys[2].time = 1.00f;
        alphaKeys[2].alpha = 0f;
        gradient.SetKeys(colorKeys, alphaKeys);
        foreach (VisualEffect effect in _effectVisuals)
        {
            effect.SetGradient("ColorGradient", gradient);
        }
        if (_volumeProfile.TryGet<PhysicallyBasedSky>(out var sky))
        {
            sky.horizonTint.Override(color1);
            sky.zenithTint.Override(color2);
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
