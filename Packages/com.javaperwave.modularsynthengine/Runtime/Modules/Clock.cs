using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clock : Module
{
    // ENTRADAS
    // Reset In  - Reinicia el clock al inicio del pulso
    // Run In    - Gate que activa/detiene el clock externamente

    // SALIDAS
    // Clock Out  - Pulso principal al BPM configurado
    // Div/2 Out  - Mitad de velocidad  (cada 2 pulsos)
    // Div/4 Out  - Un cuarto            (cada 4 pulsos)
    // Div/8 Out  - Un octavo            (cada 8 pulsos)
    // Run Out    - Gate HIGH mientras el clock esta corriendo


    public float bpm = 120f;
    public bool autoRun = true;
    public int pulsesPerBeat = 4;

    private double sampleRate;
    private double samplesPerPulse;

    private double samplePosition = 0.0;

    private int pulseCount = 0;
    private int div2Counter = 0;
    private int div4Counter = 0;
    private int div8Counter = 0;

    private bool clockGateHigh = false;
    private bool div2GateHigh = false;
    private bool div4GateHigh = false;
    private bool div8GateHigh = false;

    private double pulseDurationSamples;

    private double clockPulsePos = 0.0;
    private double div2PulsePos = 0.0;
    private double div4PulsePos = 0.0;
    private double div8PulsePos = 0.0;

    private bool isRunning = false;
    //private bool wasRunHigh = false;
    private bool wasResetHigh = false;

    // Precompute por bloque
    private double lastDspTime = -1.0;

    private bool[] clockAtSample;
    private bool[] div2AtSample;
    private bool[] div4AtSample;
    private bool[] div8AtSample;
    private bool[] runAtSample;

    protected override void Initialize()
    {
        sampleRate = AudioSettings.outputSampleRate;

        AddPort("reset_in", "RESET IN", PortType.TRIGGER, PortDir.INPUT);
        AddPort("run_in", "RUN IN", PortType.GATE, PortDir.INPUT);

        AddPort("clock_out", "CLOCK OUT", PortType.TRIGGER, PortDir.OUTPUT);
        AddPort("div2_out", "DIV2 OUT", PortType.TRIGGER, PortDir.OUTPUT);
        AddPort("div4_out", "DIV4 OUT", PortType.TRIGGER, PortDir.OUTPUT);
        AddPort("div8_out", "DIV8 OUT", PortType.TRIGGER, PortDir.OUTPUT);
        AddPort("run_out", "RUN OUT", PortType.GATE, PortDir.OUTPUT);

        isRunning = autoRun;
        UpdateTimingParameters();
    }

    public override List<ModuleParameter> GetParameters()
    {
        return new List<ModuleParameter>
        {
            ModuleParameter.Knob("bpm", "BPM", bpm, 10f, 300f, v => {bpm = v; UpdateTimingParameters();}),
            ModuleParameter.Knob("ppb", "Pulsos/Beat", pulsesPerBeat, 1f, 8f, v => {pulsesPerBeat = Mathf.RoundToInt(v); UpdateTimingParameters();}, isInt: true),
            ModuleParameter.Toggle("auto_run", "Auto Run", autoRun, v => {autoRun  = v; if (autoRun) isRunning = true;}),
        };
    }

    private void UpdateTimingParameters()
    {
        double beatsPerSecond = bpm / 60.0;
        double pulsesPerSecond = beatsPerSecond * pulsesPerBeat;
        samplesPerPulse = sampleRate / pulsesPerSecond;

        // 50 % duty cycle para los gates de salida
        pulseDurationSamples = samplesPerPulse * 0.5;
    }

    public override float[] execute(float[] data, CV cv)
    {
        double currentDspTime = AudioSettings.dspTime;
        if (lastDspTime != currentDspTime)
        {
            PrecomputeBlock(data.Length);
            lastDspTime = currentDspTime;
        }

        switch (cv.portId)
        {
            case "clock_out": return BoolArrayToGate(clockAtSample, data);
            case "div2_out": return BoolArrayToGate(div2AtSample, data);
            case "div4_out": return BoolArrayToGate(div4AtSample, data);
            case "div8_out": return BoolArrayToGate(div8AtSample, data);
            case "run_out": return BoolArrayToGate(runAtSample, data);
            default: return data;
        }
    }

    private void PrecomputeBlock(int bufferSize)
    {
        EnsureArrays(bufferSize);

        float[] resetSignal = ReadInputPort("reset_in", bufferSize);
        float[] runSignal = ReadInputPort("run_in", bufferSize);

        double localPos = samplePosition;
        double localClockPos = clockPulsePos;
        double localDiv2Pos = div2PulsePos;
        double localDiv4Pos = div4PulsePos;
        double localDiv8Pos = div8PulsePos;

        int localPulse = pulseCount;
        int localDiv2 = div2Counter;
        int localDiv4 = div4Counter;
        int localDiv8 = div8Counter;

        //bool localRunning = isRunning;
        //bool localWasRun = wasRunHigh;
        bool localWasReset = wasResetHigh;

        bool localClockHigh = clockGateHigh;
        bool localDiv2High = div2GateHigh;
        bool localDiv4High = div4GateHigh;
        bool localDiv8High = div8GateHigh;

        for (int i = 0; i < bufferSize; i++)
        {
            bool localRunning;

            if (runSignal != null)
            {
                localRunning = CVStandard.IsGateActive(runSignal[i]) || autoRun;
            }
            else
            {
                localRunning = autoRun ? true : isRunning;
            }

            if (resetSignal != null && CVStandard.IsGateActive(resetSignal[i]) && !localWasReset)
            {
                localPos = 0.0;
                localClockPos = 0.0;
                localDiv2Pos = 0.0;
                localDiv4Pos = 0.0;
                localDiv8Pos = 0.0;
                localPulse = 0;
                localDiv2 = 0;
                localDiv4 = 0;
                localDiv8 = 0;
                localClockHigh = false;
                localDiv2High = false;
                localDiv4High = false;
                localDiv8High = false;
            }

            localWasReset = (resetSignal != null && CVStandard.IsGateActive(resetSignal[i]));

            if (localRunning)
            {
                localPos++;

                if (localClockHigh) localClockPos++;
                if (localDiv2High) localDiv2Pos++;
                if (localDiv4High) localDiv4Pos++;
                if (localDiv8High) localDiv8Pos++;

                //Clock pulse
                if (localPos >= samplesPerPulse)
                {
                    localPos -= samplesPerPulse;

                    //Main clock
                    localClockHigh = true;
                    localClockPos = 0.0;
                    localPulse++;

                    localDiv2++;
                    if (localDiv2 >= 2)
                    {
                        localDiv2 = 0;
                        localDiv2High = true;
                        localDiv2Pos = 0.0;
                    }

                    localDiv4++;
                    if (localDiv4 >= 4)
                    {
                        localDiv4 = 0;
                        localDiv4High = true;
                        localDiv4Pos = 0.0;
                    }

                    localDiv8++;
                    if (localDiv8 >= 8)
                    {
                        localDiv8 = 0;
                        localDiv8High = true;
                        localDiv8Pos = 0.0;
                    }
                }

                if (localClockHigh && localClockPos >= pulseDurationSamples)
                    localClockHigh = false;

                if (localDiv2High && localDiv2Pos >= pulseDurationSamples * 2)
                    localDiv2High = false;

                if (localDiv4High && localDiv4Pos >= pulseDurationSamples * 4)
                    localDiv4High = false;

                if (localDiv8High && localDiv8Pos >= pulseDurationSamples * 8)
                    localDiv8High = false;
            }

            clockAtSample[i] = localRunning && localClockHigh;
            div2AtSample[i] = localRunning && localDiv2High;
            div4AtSample[i] = localRunning && localDiv4High;
            div8AtSample[i] = localRunning && localDiv8High;
            runAtSample[i] = localRunning;

            isRunning = localRunning;
        }

        samplePosition = localPos;
        clockPulsePos = localClockPos;
        div2PulsePos = localDiv2Pos;
        div4PulsePos = localDiv4Pos;
        div8PulsePos = localDiv8Pos;
        pulseCount = localPulse;
        div2Counter = localDiv2;
        div4Counter = localDiv4;
        div8Counter = localDiv8;
        //isRunning = localRunning;
        //wasRunHigh = localWasRun;
        wasResetHigh = localWasReset;
        clockGateHigh = localClockHigh;
        div2GateHigh = localDiv2High;
        div4GateHigh = localDiv4High;
        div8GateHigh = localDiv8High;
    }

    private void EnsureArrays(int size)
    {
        if (clockAtSample == null || clockAtSample.Length != size)
        {
            clockAtSample = new bool[size];
            div2AtSample = new bool[size];
            div4AtSample = new bool[size];
            div8AtSample = new bool[size];
            runAtSample = new bool[size];
        }
    }

    private float[] BoolArrayToGate(bool[] source, float[] data)
    {
        for (int i = 0; i < data.Length; i++)
            data[i] = source[i] ? CVStandard.GATE_HIGH : CVStandard.GATE_LOW;
        return data;
    }

    public void Start() => isRunning = true;
    public void Stop() => isRunning = false;

    public bool ClockGateHigh => clockGateHigh;
    public bool Div2GateHigh => div2GateHigh;
    public bool Div4GateHigh => div4GateHigh;
    public bool Div8GateHigh => div8GateHigh;

    public void Reset()
    {
        samplePosition = 0.0;
        clockPulsePos = div2PulsePos = div4PulsePos = div8PulsePos = 0.0;
        pulseCount = div2Counter = div4Counter = div8Counter = 0;
        clockGateHigh = div2GateHigh = div4GateHigh = div8GateHigh = false;
        wasResetHigh = false;
    }

    public bool IsRunning => isRunning;
    public float BPM => bpm;
}