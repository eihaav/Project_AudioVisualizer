using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
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
    private Volume _volume;
    private VolumeProfile _volumeProfile;
    //private bool hasRippleStarted = false;
    private void Awake()
    {
        HexDisplacementMaterial = HexDisplacementRenderer.sharedMaterial;
    }
    public override void StartEffect()
    {
        _volume = FindAnyObjectByType<Volume>();
        _volumeProfile = _volume.sharedProfile;
        _bandCount = InputAudioManager.Instance.FrequencyBandCount;

        _effectParentObjects = new GameObject[_bandCount];
        _effectObjects = new GameObject[_bandCount];

        for (int i = 0; i < _bandCount; i++)
        {
            _effectParentObjects[i] = Instantiate(EffectObjectPrefab);
            _effectObjects[i] = _effectParentObjects[i];
        }
        ColorPresetManager.Instance.SetActiveEffectDriver(this);
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
    public override void SetColorScheme(Color color1, Color color2)
    {
        HexDisplacementMaterial.SetColor("_ColorGradient1", color1);
        HexDisplacementMaterial.SetColor("_ColorGradient2", color2);
        if (_volumeProfile.TryGet<PhysicallyBasedSky>(out var sky))
        {
            sky.horizonTint.Override(color1);
            sky.zenithTint.Override(color2);
        }
        if (_volumeProfile.TryGet<Fog>(out var fog))
        {
            fog.albedo.overrideState = true;
            fog.albedo.Override(color1);
        }
    }
    private class RippleInstance
    {
        public GameObject RippleObj;
        public float ElapsedTime;
        public float Duration;
        public float MaxScale;
        public float StartAlpha;
    }
}
