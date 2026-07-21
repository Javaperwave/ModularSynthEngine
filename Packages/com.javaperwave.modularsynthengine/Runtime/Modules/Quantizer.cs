using System.Collections;
using System.Collections.Generic;
//using UnityEditor;
using UnityEngine;

public class Quantizer : Module
{
    //ENTRADAS
    //CV In - Señal a cuantizar
    //(OP) Gate In - Activar la cuantización
    //(OP) Trigger In - Muestrear y mantetner CV en puntos específicos

    //SALIDAS
    //CV Out - Señal cuantizada

    public List<float> scale = new List<float>();
    public ScaleType scaleType;

    private ScaleType builtScale = (ScaleType)(-1);

    public override List<ModuleParameter> GetParameters() => new List<ModuleParameter>
    {
        ModuleParameter.Dropdown("scale", "Scale",new string[] { "MAJOR", "MINOR", "PENTA MAJ", "PENTA MIN", "CHROMATIC" }, (int)scaleType, i => scaleType = (ScaleType)i)
    };


    protected override void Initialize()
    {
        AddPort("cv_in",  "CV IN",  PortType.PITCHCV, PortDir.INPUT);
        AddPort("cv_out", "CV OUT", PortType.PITCHCV, PortDir.OUTPUT);
    }

    public override float[] execute(float[] data, CV cv)
    {
        if (TryGetFrameCache(data.Length, out float[] cache))
        {
            System.Array.Copy(cache, data, data.Length);
            return data;
        }

        /*
        setScale(scaleType);

        float[] quantizedData = new float[data.Length];

        if (inputsCV[0] != null)
        {
            float[] inputSignal = inputsCV[0].source.execute(data, inputsCV[0]);

            for (int i = 0; i < inputSignal.Length; i++)
            {
                quantizedData[i] = quantize(inputSignal[i]);
            }
        }

        return quantizedData;
        */

        float[] input = ReadInputPort("cv_in", data.Length);

        if (input == null)
        {
            System.Array.Clear(data, 0, data.Length);
            SaveToFrameCache(data);
            
            return data;
        }
        

        if (scaleType != builtScale)
        {
            setScale(scaleType);
            builtScale = scaleType;
        }
 
        for (int i = 0; i < input.Length; i++)
            data[i] = quantize(input[i]);
 
        SaveToFrameCache(data);

        return data;
    }

    public void setScale(ScaleType scaleType)
    {
        scale.Clear();

        //Each octave has 12th semitones
        switch (scaleType)
        {
            case ScaleType.Major:
                scale.AddRange(new float[] { 0f, 2f/12f, 4f/12f, 5f/12f, 7f/12f, 9f/12f, 11f/12f, 1f });
                break;

            case ScaleType.Minor:
                scale.AddRange(new float[] { 0f, 2f/12f, 3f/12f, 5f/12f, 7f/12f, 8f/12f, 10f/12f, 1f });
                break;

            case ScaleType.PentatonicMajor:
                scale.AddRange(new float[] { 0f, 2f/12f, 4f/12f, 7f/12f, 9f/12f, 1f });
                break;

            case ScaleType.PentatonicMinor:
                scale.AddRange(new float[] { 0f, 3f/12f, 5f/12f, 7f/12f, 10f/12f, 1f });
                break;

            case ScaleType.Chromatic:
                scale.AddRange(new float[] { 0f, 1f/12f, 2f/12f, 3f/12f, 4f/12f, 5f/12f, 6f/12f, 7f/12f, 8f/12f, 9f/12f, 10f/12f, 11f/12f, 1f });
                break;
        }
    }

    //1 volt per octave
    private float quantize(float input)
    {
        input = Mathf.Clamp(input, CVStandard.PITCH_CV_MIN, CVStandard.PITCH_CV_MAX);

        //Divide input between an int (octave) plus its fractional part (note)
        int octave = Mathf.FloorToInt(input);
        float noteValue = input - octave;

        float closestValue = scale[0];
        float minDif = Mathf.Abs(noteValue - scale[0]);

        //Search fot the closest value in the scale
        foreach (float note in scale)
        {
            float difference = Mathf.Abs(noteValue - note);
            if (difference < minDif)
            {
                minDif = difference;
                closestValue = note;
            }
        }

        return octave + closestValue;
    }

    public enum ScaleType
    { 
        Major,
        Minor,
        PentatonicMajor,
        PentatonicMinor,
        Chromatic
    }
}
