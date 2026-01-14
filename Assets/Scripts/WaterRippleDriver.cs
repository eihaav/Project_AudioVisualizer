using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.VFX;

public class WaterRippleDriver : BeatDriverBase
{
    public float beatThreshold = 0.01f;
    public int MinEmitCount, MaxEmitCount;
    public float MinVelocity, MaxVelocity;
    public float MinSize, MaxSize;
    public VisualEffect BeatParticleExplosion;
    public GameObject RipplePrefab;
    private List<RippleInstance> _rippleInstances = new List<RippleInstance>();
    public Vector3 EffectCenter;
    public int effectCount = 10;
    public float RippleDuration = 2.0f, RippleMaxScale = 5.0f, RippleStartFade = 1.0f;
    private float rollingAverageBeat = 0f;
    public float BeatSmoothing = 0.1f;

    void Update()
    {
        DriveEffect();
        // Smoothly interpolate light intensity
        //BeatLight.intensity = Mathf.Lerp(BeatLight.intensity, _targetIntensity, Time.deltaTime * 10f);
    }
    private void DriveEffect()
    {
        float beatValue = InputAudioManager.Instance.GetBeatStrength();
        //Debug.Log("Beat Value: " + beatValue);
        //_targetIntensity = Mathf.Lerp(MinIntensity, MaxIntensity, beatValue / (1 - beatThreshold));
        
        if (beatValue >= beatThreshold)
        {
            rollingAverageBeat = Mathf.Lerp(rollingAverageBeat, beatValue, BeatSmoothing);
            EmitEffects(beatValue);
        }
        HandleRipples();
        rollingAverageBeat = Mathf.Lerp(rollingAverageBeat, 0, BeatSmoothing * 0.5f * Time.deltaTime);
    }
    private void HandleRipples()
    {
        foreach (var ripple in _rippleInstances.ToArray())
        {
            ripple.ElapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(ripple.ElapsedTime / ripple.Duration);
            float scale = Mathf.Lerp(0f, ripple.MaxScale, progress);
            ripple.RippleObj.transform.localScale = new Vector3(scale, scale, scale);
            ripple.RippleDecal.fadeFactor = Mathf.Clamp01((1f - progress) * ripple.StartFade);
            if (progress >= 1f)
            {
                _rippleInstances.Remove(ripple);
                Destroy(ripple.RippleObj);
            }
        }
    }
    private void EmitEffects(float beatStrength)
    {
        GameObject rippleObj = Instantiate(RipplePrefab);
        rippleObj.transform.position = EffectCenter;

        beatStrength = (beatStrength - beatThreshold) / (1 - beatThreshold);
        RippleInstance rippleInstance = new RippleInstance
        {
            RippleObj = rippleObj,
            RippleDecal = rippleObj.GetComponent<DecalProjector>(),
            ElapsedTime = 0f,
            Duration = RippleDuration * beatStrength / rollingAverageBeat,
            MaxScale = RippleMaxScale * beatStrength / rollingAverageBeat,
            StartFade = RippleStartFade * beatStrength / rollingAverageBeat
        };
        rippleObj.transform.localScale = Vector3.zero;
        _rippleInstances.Add(rippleInstance);
        int emitCount = Mathf.RoundToInt(Mathf.Lerp(MinEmitCount, MaxEmitCount, beatStrength / rollingAverageBeat));
        float velocity = Mathf.Lerp(MinVelocity, MaxVelocity, beatStrength / rollingAverageBeat);
        //float size = Mathf.Lerp(MinSize, MaxSize, beatStrength / rollingAverageBeat);
        BeatParticleExplosion.SetFloat("Speed", velocity);
        BeatParticleExplosion.SetInt("Count", emitCount);
        BeatParticleExplosion.SendEvent("EmitExplosion");
    }
    public override void SetColorScheme(Color color1, Color color2)
    {
        // TO-DO: Implement color scheme
    }
    private class RippleInstance
    {
        public GameObject RippleObj;
        public DecalProjector RippleDecal;
        public float ElapsedTime;
        public float Duration;
        public float MaxScale;
        public float StartFade;
    }
}
