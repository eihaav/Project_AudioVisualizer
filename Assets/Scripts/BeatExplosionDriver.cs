using UnityEngine;
using UnityEngine.VFX;

public class BeatExplosionDriver : BeatDriverBase
{
    public float beatThreshold = 0.01f;
    public VisualEffect BeatExplosionEffect1, BeatExplosionEffect2;
    public Vector3 effectBoundsMin, effectBoundsMax;
    public int effectCount = 10;
    private VisualEffect[] _effectInstances;
    public Light BeatLight, AmbientLight;
    public float MinIntensity, MaxIntensity;
    private float _targetIntensity;
    private float rollingAverageBeat = 0.01f;
    public float BeatSmoothing = 0.1f;
    private void Start()
    {
        ColorPresetManager.Instance.SetActiveBeatDriver(this);
    }
    private void Awake()
    {
        _effectInstances = new VisualEffect[effectCount * 2];
        for (int i = 0; i < effectCount; i++)
        {
            Vector3 randomPosition = new Vector3(
                Random.Range(effectBoundsMin.x, effectBoundsMax.x),
                Random.Range(effectBoundsMin.y, effectBoundsMax.y),
                Random.Range(effectBoundsMin.z, effectBoundsMax.z)
            );
            VisualEffect effectInstance1 = Instantiate(BeatExplosionEffect1, randomPosition, Quaternion.identity);
            VisualEffect effectInstance2 = Instantiate(BeatExplosionEffect2, randomPosition, Quaternion.identity);
            _effectInstances[i * 2] = effectInstance1;
            _effectInstances[i * 2 + 1] = effectInstance2;
        }

    }
    void Update()
    {
        DriveEffect();
        // Smoothly interpolate light intensity
        BeatLight.intensity = Mathf.Lerp(BeatLight.intensity, _targetIntensity, Time.deltaTime * 10f);
    }
    private void DriveEffect()
    {
        float beatValue = InputAudioManager.Instance.GetBeatStrength();
        //Debug.Log("Beat Value: " + beatValue);
        _targetIntensity = Mathf.Lerp(MinIntensity, MaxIntensity, beatValue / (1 - beatThreshold) / rollingAverageBeat);
        if (beatValue >= beatThreshold)
        {
            rollingAverageBeat = Mathf.Lerp(rollingAverageBeat, beatValue, BeatSmoothing);
            EmitEffects();
        }
        rollingAverageBeat = Mathf.Lerp(rollingAverageBeat, 0.01f, BeatSmoothing * 0.5f * Time.deltaTime);
    }
    private void EmitEffects()
    {
        for (int i = 0; i < _effectInstances.Length; i++)
        {
            Vector3 randomPosition = new Vector3(
                Random.Range(effectBoundsMin.x, effectBoundsMax.x),
                Random.Range(effectBoundsMin.y, effectBoundsMax.y),
                Random.Range(effectBoundsMin.z, effectBoundsMax.z)
            );
            _effectInstances[i].transform.position = randomPosition;
            _effectInstances[i].SendEvent("OnPlay");
        }
    }
    public override void SetColorScheme(Color color1, Color color2)
    {
        foreach (var effect in _effectInstances) 
        {
            effect.SetVector4("BaseColor", color1);
            effect.SetVector4("EmissiveColor", color2);
        }
        AmbientLight.color = color1;
        BeatLight.color = color2;
    }
}
