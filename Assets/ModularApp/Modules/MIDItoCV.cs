using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

public class MIDIToCV : Module
{
    //SALIDAS
    //Pitch CV  - 1V/Oct, last-note priority, con glide y pitch bend aplicados
    //Gate Out  - HIGH mientras alguna tecla este pulsada
    //Trig Out  - Pulso corto al inicio de cada nueva nota (retrigger de envelopes)

    //CONTROLES DE TECLADO
    //Notas: AWSEDFTGYHUJ -> C, C#, D, D#, E, F, F#, G, G#, A, A#, B

    private static readonly KeyCode[] noteKeys =
    {
        KeyCode.A, KeyCode.W, KeyCode.S, KeyCode.E, KeyCode.D, KeyCode.F,
        KeyCode.T, KeyCode.G, KeyCode.Y, KeyCode.H, KeyCode.U, KeyCode.J
    };

    private const float BEND_TRAVEL_TIME = 0.08f;


    [Range(-3, 7)]
    public int octave = 4;

    [Range(-12, 12)]
    public int transpose = 0;

    [Range(0f, 2f)]
    public float glide = 0f;

    //Rango del pitch bend en semitonos. +/-2 es el estandar MIDI.
    [Range(1, 12)]
    public int bendRange = 2;

    private enum EventType { NoteOn, NoteOff, BendTarget }

    private struct InputEvent
    {
        public EventType type;
        public int intValue;
        public float floatValue;
    }

    private readonly ConcurrentQueue<InputEvent> events = new ConcurrentQueue<InputEvent>();

    private float lastBendTargetMain = 0f;

    private readonly List<int> heldNotes = new List<int>();
    private float targetPitchCV = 0f;
    private float currentPitchCV = 0f;
    private float bendTarget = 0f;
    private float bendCurrent = 0f;
    private bool gateHigh = false;

    private int triggerSamplesRemaining = 0;
    private int triggerDurationSamples;

    private float[] pitchBuffer;
    private float[] gateBuffer;
    private float[] trigBuffer;

    private double sampleRate;
    private double lastDspTime = -1.0;


    public override List<ModuleParameter> GetParameters()
    {
        return new List<ModuleParameter>
        {
            ModuleParameter.Knob("octave", "Octave", octave, -3f, 7f, v => octave = Mathf.RoundToInt(v), isInt: true),
            ModuleParameter.Knob("transpose", "Trans",  transpose, -12f, 12f, v => transpose = Mathf.RoundToInt(v), isInt: true),
            ModuleParameter.Knob("glide", "Glide",  glide, 0f, 2f, v => glide = v),
            ModuleParameter.Knob("bend_range", "Bend", bendRange, 1f, 12f, v => bendRange = Mathf.RoundToInt(v), isInt: true),
        };
    }

    protected override void Initialize()
    {
        AddPort("pitch_cv", "PITCH CV", PortType.PITCHCV, PortDir.OUTPUT);
        AddPort("gate_out", "GATE OUT", PortType.GATE,    PortDir.OUTPUT);
        AddPort("trig_out", "TRIG OUT", PortType.TRIGGER, PortDir.OUTPUT);

        sampleRate = AudioSettings.outputSampleRate;
        triggerDurationSamples = (int)((CVStandard.TRIGGER_TYPICAL_DURATION_MS / 1000.0) * sampleRate);
    }

    private void Update()
    {
        for (int i = 0; i < noteKeys.Length; i++)
        {
            if (Input.GetKeyDown(noteKeys[i]))
            {
                int absoluteSemitone = i + octave * 12 + transpose;
                events.Enqueue(new InputEvent { type = EventType.NoteOn, intValue = absoluteSemitone });
            }
            else if (Input.GetKeyUp(noteKeys[i]))
            {
                int absoluteSemitone = i + octave * 12 + transpose;
                events.Enqueue(new InputEvent { type = EventType.NoteOff, intValue = absoluteSemitone });
            }
        }

        bool leftHeld  = Input.GetKey(KeyCode.LeftArrow);
        bool rightHeld = Input.GetKey(KeyCode.RightArrow);

        float newTarget = 0f;
        if (leftHeld && !rightHeld) newTarget = -1f;
        else if (rightHeld && !leftHeld) newTarget = 1f;

        if (newTarget != lastBendTargetMain)
        {
            events.Enqueue(new InputEvent { type = EventType.BendTarget, floatValue = newTarget });
            lastBendTargetMain = newTarget;
        }
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
            case "pitch_cv":
                System.Array.Copy(pitchBuffer, data, data.Length);
                return data;

            case "gate_out":
                System.Array.Copy(gateBuffer, data, data.Length);
                return data;

            case "trig_out":
                System.Array.Copy(trigBuffer, data, data.Length);
                return data;

            default:
                System.Array.Clear(data, 0, data.Length);
                return data;
        }
    }

    private void PrecomputeBlock(int bufferSize)
    {
        while (events.TryDequeue(out InputEvent ev))
            ProcessEvent(ev);

        if (pitchBuffer == null || pitchBuffer.Length != bufferSize) pitchBuffer = new float[bufferSize];
        if (gateBuffer  == null || gateBuffer.Length  != bufferSize) gateBuffer  = new float[bufferSize];
        if (trigBuffer  == null || trigBuffer.Length  != bufferSize) trigBuffer  = new float[bufferSize];

        float glideRate = (glide > 0.0001f) ? (1f / (glide * (float)sampleRate)) : float.MaxValue;

        float bendRate = 1f / (BEND_TRAVEL_TIME * (float)sampleRate);

        float bendRangeCV = bendRange / 12f;
        float gateValue = gateHigh ? CVStandard.GATE_HIGH : CVStandard.GATE_LOW;

        for (int i = 0; i < bufferSize; i++)
        {
            if (currentPitchCV < targetPitchCV)
                currentPitchCV = Mathf.Min(currentPitchCV + glideRate, targetPitchCV);
            else if (currentPitchCV > targetPitchCV)
                currentPitchCV = Mathf.Max(currentPitchCV - glideRate, targetPitchCV);

            if (bendCurrent < bendTarget)
                bendCurrent = Mathf.Min(bendCurrent + bendRate, bendTarget);
            else if (bendCurrent > bendTarget)
                bendCurrent = Mathf.Max(bendCurrent - bendRate, bendTarget);

            pitchBuffer[i] = currentPitchCV + bendCurrent * bendRangeCV;

            gateBuffer[i] = gateValue;

            if (triggerSamplesRemaining > 0)
            {
                trigBuffer[i] = CVStandard.GATE_HIGH;
                triggerSamplesRemaining--;
            }
            else
            {
                trigBuffer[i] = CVStandard.GATE_LOW;
            }
        }
    }

    private void ProcessEvent(InputEvent ev)
    {
        switch (ev.type)
        {
            case EventType.NoteOn:
            {
                bool wasEmpty = (heldNotes.Count == 0);

                heldNotes.Remove(ev.intValue);
                heldNotes.Add(ev.intValue);

                targetPitchCV = ev.intValue / 12f;

                if (wasEmpty)
                    currentPitchCV = targetPitchCV;

                gateHigh = true;

                triggerSamplesRemaining = triggerDurationSamples;
                break;
            }

            case EventType.NoteOff:
            {
                heldNotes.Remove(ev.intValue);

                if (heldNotes.Count > 0)
                {
                    int last = heldNotes[heldNotes.Count - 1];
                    targetPitchCV = last / 12f;
                }
                else
                {
                    gateHigh = false;
                }
                break;
            }

            case EventType.BendTarget:
                bendTarget = ev.floatValue;
                break;
        }
    }
}