using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class HexRippleEffectDriver : EffectDriverBase
{
    public GameObject RippleSpritePrefab;
    public float EffectThreshold = 0.1f, RippleDecayMultiplier, RippleFade;
    public float MaxScale = 1.0f, RippleDuration = 0.75f;
    public EffectTextureCreator EffectTextureCreator;
    public MeshRenderer HexDisplacementRenderer;
    private Material HexDisplacementMaterial;
    public string HexDisplacementTexturePropertyName = "_DisplacementTexture";
    private List<RippleInstance> _activeRipples = new List<RippleInstance>();
    public AnimationCurve AlphaCurve;
    //private bool hasRippleStarted = false;
    private void Awake()
    {
        HexDisplacementMaterial = HexDisplacementRenderer.sharedMaterial;
    }
    private void Update()
    {
        DriveEffect();
    }
    protected override void DriveEffect()
    {
        EffectTextureCreator.FadeAmount = RippleDecayMultiplier * Time.deltaTime;
        InputAudioManager.SpectrumPointData[] effectPowers = InputAudioManager.Instance.GetSpectrumData();
        if (effectPowers == null)
        {
            return;
        }
        for (int i = 0; i < _bandCount; i++)
        {
            _effectParentObjects[i].transform.position = EffectCenter + ObjectOffset * i;
            float pointValue = (float)effectPowers[i].PointValue;
            if (pointValue >= EffectThreshold)
            {
                GameObject rippleObj = Instantiate(RippleSpritePrefab, _effectParentObjects[i].transform);
                //Debug.Log("Creating ripple at band " + i + " with point value " + pointValue);
                RippleInstance rippleInstance = new RippleInstance
                {
                    RippleObj = rippleObj,
                    ElapsedTime = 0f,
                    Duration = RippleDuration * pointValue,
                    MaxScale = MaxScale * pointValue,
                    StartAlpha = 0.1f * pointValue
                };
                EffectTextureCreator.AddRipple(rippleObj);
                _activeRipples.Add(rippleInstance);
                //hasRippleStarted = true;
            }
            //EffectPerObject(i, (float)effectPowers[i].PointValue);
        }
        float deltaTime = Time.deltaTime;
        foreach (var ripple in _activeRipples.ToArray())
        {
            HandleRipple(ripple, deltaTime);
        }
    }
    private void HandleRipple(RippleInstance ripple, float deltaTime)
    {
        EffectTextureCreator.SetMaterialRT(HexDisplacementMaterial, HexDisplacementTexturePropertyName);
        //Debug.Log($"Updating ripple, elapsed time {ripple.ElapsedTime}, duration {ripple.Duration}, delta time {deltaTime}.");
        float elapsed = ripple.ElapsedTime;
        elapsed += deltaTime;
        ripple.ElapsedTime = elapsed;
        
        float progress = Mathf.Clamp01(ripple.ElapsedTime / ripple.Duration);
        if (progress >= 1.0f)
        {
            _activeRipples.Remove(ripple);
            EffectTextureCreator.RemoveRipple(ripple.RippleObj);
            Destroy(ripple.RippleObj);
            //Debug.Log("Destroying ripple");
        }
        float scale = Mathf.Lerp(0f, ripple.MaxScale, progress);
        float alpha = AlphaCurve.Evaluate(progress) * ripple.StartAlpha;
        //float alpha = Mathf.Lerp(ripple.StartAlpha, 0f, progress * RippleFade);
        ripple.RippleObj.transform.localScale = new Vector3(scale, scale, scale);
        SpriteRenderer spriteRenderer = ripple.RippleObj.GetComponent<SpriteRenderer>();
        Color color = spriteRenderer.color;
        color.a = alpha;
        spriteRenderer.color = color;
        
    }
    /*protected override void EffectPerObject(int effectObjectIndex, float effectPower)
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
    }*/
    private class RippleInstance
    {
        public GameObject RippleObj;
        public float ElapsedTime;
        public float Duration;
        public float MaxScale;
        public float StartAlpha;
    }
}
