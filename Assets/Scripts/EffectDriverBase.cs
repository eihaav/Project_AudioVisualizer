using UnityEngine;
using UnityEngine.VFX;

public class EffectDriverBase : MonoBehaviour
{
    public GameObject EffectObjectPrefab;
    public Vector3 ObjectOffset, EffectCenter;
    public float effectAmplitudeMultiplier = 1.0f;
    protected GameObject[] _effectParentObjects;
    protected GameObject[] _effectObjects;
    protected int _bandCount;
    public float MaxEffectStrength = 40f;
    private void Start()
    {
        StartEffect();
    }
    public virtual void StartEffect()
    {
        _bandCount = InputAudioManager.Instance.FrequencyBandCount;

        _effectParentObjects = new GameObject[_bandCount];
        _effectObjects = new GameObject[_bandCount];
        for (int i = 0; i < _bandCount; i++)
        {
            _effectParentObjects[i] = Instantiate(EffectObjectPrefab);
            _effectObjects[i] = _effectParentObjects[i].GetComponentInChildren<MeshRenderer>().gameObject;
        }
    }
    private void Update()
    {
        DriveEffect();
    }
    protected virtual void DriveEffect()
    {
        InputAudioManager.SpectrumPointData[] effectPowers = InputAudioManager.Instance.GetSpectrumData();
        if (effectPowers == null)
        {
            return;
        }
        for (int i = 0; i < _bandCount; i++)
        {
            EffectPerObject(i, (float)effectPowers[i].PointValue);
        }
    }
    protected virtual void EffectPerObject(int effectObjectIndex, float effectPower)
    {
        _effectParentObjects[effectObjectIndex].transform.position = EffectCenter + ObjectOffset * effectObjectIndex;
    }
}
