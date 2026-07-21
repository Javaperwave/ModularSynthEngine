using UnityEngine;

public static class CVStandard 
{
    //Pitch CV - 1V/Oct
    public const float VOLTS_PER_OCTAVE = 1f;
    public const float PITCH_CV_MIN = -3f;  //-3 Oct
    public const float PITCH_CV_MAX = 7f;   //+7 Oct

    //C0 - 16.35 Hz
    //public const float C0_FREQ = 16.35f;
    public const float C3_FREQ = 130.81f;
    
    //Bipolar CV
    public const float MOD_CV_MIN = -5f;
    public const float MOD_CV_MAX = 5f;
    
    //Unipolar CV
    public const float UNIPOLAR_MIN = 0f;
    public const float UNIPOLAR_MAX = 5f;
    
    //Gate
    public const float GATE_LOW = 0f;
    public const float GATE_HIGH = 5f;
    public const float GATE_THRESHOLD = 2.5f;
    
    //Trigger
    public const float TRIGGER_MIN_DURATION_MS = 1f;
    public const float TRIGGER_TYPICAL_DURATION_MS = 5f;
    

    //CV pitch to Freq
    public static float CVToFrequency(float cv)
    {
        return Mathf.Pow(2f, cv);
    }
    
    //Freq to CV pitch
    public static float FrequencyRatioToCV(float ratio)
    {
        return Mathf.Log(ratio, 2f);
    }

    //CV bipolar [-5, 5] to Unipolar [0, 1]
    public static float BipolarToUnipolar(float cv)
    {
        return Mathf.Clamp01((cv - MOD_CV_MIN) / (MOD_CV_MAX - MOD_CV_MIN));
    }

    //Unipolar [0, 1] to CV bipolar [-5, 5]
    public static float UnipolarToBipolar(float normalized)
    {
        return MOD_CV_MIN + (normalized * (MOD_CV_MAX - MOD_CV_MIN));
    }
    
    //Active gate
    public static bool IsGateActive(float voltage)
    {
        return voltage >= GATE_THRESHOLD;
    }
}