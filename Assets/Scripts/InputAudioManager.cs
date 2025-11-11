using UnityEngine;
using CSCore;
using CSCore.CoreAudioAPI;
using CSCore.Streams;
using System;
using CSCore.SoundIn;
using System.Collections.Generic;
using CSCore.DSP;
public class InputAudioManager : MonoBehaviour
{
    public double MinimumLoudnessDb = -90.0, MaximumLoudnessDb = 0.0;
    public static InputAudioManager Instance;
    private const int _maxFrequency = 8000, _minFrequency = 20;
    private int _maxFrequencyIndex, _minFrequencyIndex;
    private CSCore.SoundIn.WasapiLoopbackCapture audioLoopback;
    public int currentActiveAudioDevice = 1;
    public bool refreshAudioDevice = false;
    private MMDeviceCollection _deviceCollection;
    private AudioEndpointVolume _currentAudioEndPoint;
    private SoundInSource _soundInSource;
    private SingleBlockNotificationStream _singleBlockReader;
    private IWaveSource _realtimeWaveSource;
    private byte[] _waveBuffer;
    private FftProvider _fftProvider;
    private FftSize _fftSize = FftSize.Fft4096;
    private int _fftBandIndex, _maxFftIndex;
    public double SpectrumAmplitudeMultiplier;
    private bool _isListening = false, _hasBeenSetup = false;
    public int FrequencyBandCount = 16;
    // Variables for adaptive normalization
    private double[] _avgBandVolume;
    private double _adaptiveNormCoef = 0.9;
    public SpectrumNormalizationType SpectrumNormalization;
    public float GainAmplifier = 1.0f;
    

    private void Awake()
    {
        Instance = this;
        audioLoopback = new CSCore.SoundIn.WasapiLoopbackCapture();
        MMDeviceEnumerator devEnum = new MMDeviceEnumerator();
        _deviceCollection = devEnum.EnumAudioEndpoints(DataFlow.All, DeviceState.Active);
        _maxFftIndex = 4096 / 2 - 1;
        for (int i = 0; i < _deviceCollection.Count; i++)
        {
            MMDevice device = _deviceCollection.ItemAt(i);
            Debug.Log($"Found device {device.FriendlyName} at index {i}");
        }
        _minFrequencyIndex = 0;
        _maxFrequencyIndex = 4096;
    }
    private void Update()
    {
        if (refreshAudioDevice) 
        {
            if (currentActiveAudioDevice < _deviceCollection.Count) 
            {
                SetupLoopbackDevice(_deviceCollection.ItemAt(currentActiveAudioDevice));
            }
            else
            {
                currentActiveAudioDevice = 0;
            }
            refreshAudioDevice = false;
        }
        if (_isListening) 
        {
            //GetSpectrumData();
        }        
    }
    public void ChangeAudioDevice(int index)
    {
        currentActiveAudioDevice = index;
        if (_isListening) 
        {
            StopListening();
        }
        MMDevice audioDevice = _deviceCollection.ItemAt(index);
        _currentAudioEndPoint = AudioEndpointVolume.FromDevice(audioDevice);
        Debug.Log("Current device audio level: " + _currentAudioEndPoint.MasterVolumeLevelScalar);
        SetupLoopbackDevice(audioDevice);
        StartListening();
    }
    public MMDeviceCollection GetActiveAudioDevices()
    {
        return _deviceCollection;
    }
    private void SetupLoopbackDevice(MMDevice audioDevice)
    {
        if (_isListening || _hasBeenSetup)
        {
            DisposeOfLoopback();
            audioLoopback = new WasapiLoopbackCapture { Device = audioDevice };
        }
        if (_singleBlockReader != null) _singleBlockReader.Dispose();
        if (_realtimeWaveSource != null) _realtimeWaveSource.Dispose();

        //audioLoopback.Device = audioDevice;
        try
        {
            audioLoopback.Initialize();
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
            DisposeOfLoopback();
            return;
        }
        
        _soundInSource = new SoundInSource(audioLoopback);
        _hasBeenSetup = true;
    }
    public void StopListening()
    {
        audioLoopback.Stop();
    }
    private void OnApplicationQuit()
    {
        StopListening();
    }
    private void OnDisable()
    {
        StopListening();
    }
    public void StartListening()
    {
        if (audioLoopback == null)
        {
            DisposeOfLoopback();
            return;
        }
        audioLoopback.Start();

        _singleBlockReader = new SingleBlockNotificationStream(_soundInSource.ToSampleSource());
        _realtimeWaveSource = _singleBlockReader.ToWaveSource();
        _waveBuffer = new byte[_realtimeWaveSource.WaveFormat.BytesPerSecond / 2];
        _soundInSource.DataAvailable += SoundDataReceived;
        _fftProvider = new FftProvider(_realtimeWaveSource.WaveFormat.Channels, _fftSize);
        
        _isListening = true;
    }
    private void DisposeOfLoopback()
    {
        if (_soundInSource != null) 
            _soundInSource.Dispose();
        if (audioLoopback != null)
        {
            audioLoopback.Dispose();
            audioLoopback = null;
        }
    }
    private void SoundDataReceived(object sender, DataAvailableEventArgs eventArgs)
    {
        int read;
        while ((read = _realtimeWaveSource.Read(_waveBuffer, 0, _waveBuffer.Length)) > 0)
        {
            int bytesPerSample = _realtimeWaveSource.WaveFormat.BytesPerSample;
            for (int i = 0; i < read; i+= bytesPerSample)
            {
                float sample = BitConverter.ToSingle(_waveBuffer, i);
                _fftProvider.Add(sample, sample);
            }
        }
    }
    public SpectrumPointData[] GetSpectrumData()
    {
        if (_fftProvider == null) return null;
        float[] fftBuffer = new float[(int)_fftSize];
        if (_fftProvider.GetFftData(fftBuffer))
        {
            var points = GetSpectrumPoints(fftBuffer, 1.0d);
            return points;
        }
        return null;
    }
    private SpectrumPointData[] GetSpectrumPoints(float[] fftBuffer, double maxValue)
    {
        List<SpectrumPointData> points = new List<SpectrumPointData>();
        
        int currentSpectrumPointIndex = 0;
        int sampleRate = _realtimeWaveSource.WaveFormat.SampleRate;
        double frequencyStep = sampleRate / (double)_fftSize;
        double logFactor = Math.Log(_maxFrequency / (double)_minFrequency);
        float outputVolume = 1.0f;//_currentAudioEndPoint.MasterVolumeLevelScalar;
        //outputVolume = (float)Math.Log10(outputVolume);
        //Debug.Log("Current output volume: " + outputVolume);
        for (int bandIndex = 0; bandIndex < FrequencyBandCount; bandIndex++) 
        {
            double value = 0;
            int valuesCount = 0;
            //double currentBandMinFreq = _minFrequency * Math.Pow((_maxFrequency / (double)_minFrequency), (double)bandIndex / FrequencyBandCount); // Logarithmic to mimic human ear
            //double currentBandMaxFreq = _minFrequency * Math.Pow((_maxFrequency / (double)_minFrequency), (double)(bandIndex + 1) / FrequencyBandCount);
            double currentBandMinFreq = _minFrequency * Math.Exp(logFactor * bandIndex / FrequencyBandCount);
            double currentBandMaxFreq = _minFrequency * Math.Exp(logFactor * (bandIndex + 1) / FrequencyBandCount);
            int currentMinIndex = (int)(currentBandMinFreq / frequencyStep);
            int currentMaxIndex = (int)(currentBandMaxFreq / frequencyStep);
            if (currentMaxIndex <= currentMinIndex)
            {
                currentMaxIndex = currentMinIndex + 1;
            }
            for (int i = currentMinIndex; i < currentMaxIndex && i < fftBuffer.Length; i++)
            {
                double valueToAdd = fftBuffer[i];
                switch (SpectrumNormalization)
                {
                    case SpectrumNormalizationType.Gain:
                        {
                            double currentBandFrequency = (currentBandMaxFreq - currentBandMinFreq) * (i / (currentMaxIndex - currentMinIndex));
                            double gain = Math.Sqrt(currentBandFrequency / _minFrequency);
                            gain *= GainAmplifier;
                            gain /= (double)outputVolume;
                            valueToAdd = valueToAdd * gain;
                            break;
                        }
                    case SpectrumNormalizationType.Psychoacoustic:
                        {
                            double currentBandFrequency = (currentBandMaxFreq - currentBandMinFreq) * (i / (currentMaxIndex - currentMinIndex));
                            double gain = Math.Pow(currentBandFrequency / 1000.0, 0.3);
                            gain *= GainAmplifier;
                            gain /= (double)outputVolume;
                            valueToAdd = valueToAdd * gain;
                            break;
                        }
                    case SpectrumNormalizationType.Adaptive:
                        {
                            // Siia lisada adaptive normaliseerimine, nt on massiiv, kus iga FrequencyBandCount'i kohta salvestada keskmine helitugevus massiivi.
                            // Lõpptulemusel võiks saada nii, et valueToAdd saab mingi koefitsendiga korrutatud, et kõigel oleks enam-vähem sama dünaamika, mis teistel sagedustel
                            
                            // Lisasin normaliseerimisloogika allapoole kui juba individuaalsete baridega tegeleme (line 256) -Richard

                            // Initializing the _avgBandVolume array
                            if (_avgBandVolume == null)
                            {
                                _avgBandVolume = new double[FrequencyBandCount];
                                for (int j = 0; j < FrequencyBandCount; j++)
                                {
                                    _avgBandVolume[j] = 1.0;
                                }
                            }

                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
                valueToAdd /= Math.Clamp(outputVolume, 0.01, 1);
                value += valueToAdd * maxValue;
                valuesCount++;

                if (value > maxValue)
                {
                    value = maxValue;
                }
            }
            value = value / valuesCount;
            value *= SpectrumAmplitudeMultiplier;

            /// Spectrum normalization: Adaptive (CALCULATION)
            if (SpectrumNormalization == SpectrumNormalizationType.Adaptive)
            {
                // Update the running average for this freq. band
                double oldAvgWeighted = _avgBandVolume[bandIndex] * _adaptiveNormCoef;
                double newValWeighted = value * (1.0 - _adaptiveNormCoef);
                _avgBandVolume[bandIndex] = oldAvgWeighted + newValWeighted;

                // Normalize based on the tracked average
                if (_avgBandVolume[bandIndex] > 0.0001)
                {
                    // Target bar value of 2.5 (smaller nr = lower bar)
                    double normFactor = 2.5 / _avgBandVolume[bandIndex];

                    normFactor = Math.Clamp(normFactor, 0.1, 30);
                    value = value * normFactor;
                }
            }

            value = Double.IsNaN(value) ? 0 : value;
            points.Add(new SpectrumPointData { PointValue = value, PointIndex = currentSpectrumPointIndex });
            currentSpectrumPointIndex++;
        }
        return points.ToArray();
    }

    public struct SpectrumPointData
    {
        public int PointIndex;
        public double PointValue;
    }
    public enum SpectrumNormalizationType
    {
        None = 0,
        Gain = 1,
        Psychoacoustic = 2,
        Adaptive = 3,
        Logarithmic = 4,
    }
}
