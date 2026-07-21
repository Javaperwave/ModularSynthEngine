using System;
using System.Collections.Generic;
using UnityEngine;

public class ModuleParameter
{
    public string id;
    public string label;
    public ParameterType type;

    //KNOB
    public float value;
    public float min;
    public float max;
    public bool isInt;
    public Action<float> onKnobChanged;
    public ParameterCurve curve = ParameterCurve.LINEAR; //Logaritmic response to the control

    //TOGGLE
    public bool boolValue;
    public Action<bool> onToggleChanged;

    //DROPDOWN
    public string[] options;
    public int selectedIndex;
    public Action<int> onDropdownChanged;

    //GROUP
    public List<ModuleParameter> children;

    //DISPLAY
    public Action<RectTransform> onDisplaySpawn;
    public float displayHeight;

    //HIDDEN - Serialization without UI
    public bool hidden = false;


    public static ModuleParameter Knob(string id, string label, float value, float min, float max, Action<float> onChange, bool isInt = false, ParameterCurve curve = ParameterCurve.LINEAR)
    {
        return new ModuleParameter
        {
            id = id,
            label = label,
            type = ParameterType.KNOB,
            value = value,
            min = min,
            max = max,
            isInt = isInt,
            onKnobChanged = onChange,
            curve = curve
        };
    }

    public static ModuleParameter Toggle(string id, string label, bool value, Action<bool> onChange, bool hidden = false)
    {
        return new ModuleParameter
        {
            id = id,
            label = label,
            type = ParameterType.TOGGLE,
            boolValue = value,
            onToggleChanged = onChange,
            hidden = hidden
        };
    }

    public static ModuleParameter Dropdown(string id, string label, string[] options, int selectedIndex, Action<int> onChange)
    {
        return new ModuleParameter
        {
            id = id,
            label = label,
            type = ParameterType.DROPDOWN,
            options = options,
            selectedIndex = selectedIndex,
            onDropdownChanged = onChange
        };
    }

    public static ModuleParameter Slider(string id, string label, float value, float min, float max, Action<float> onChange, bool isInt = false, ParameterCurve curve = ParameterCurve.LINEAR)
    {
        return new ModuleParameter
        {
            id = id,
            label = label,
            type = ParameterType.SLIDER,
            value = value,
            min = min,
            max = max,
            isInt = isInt,
            onKnobChanged = onChange,
            curve = curve
        };
    }

    public static ModuleParameter Group(string id, params ModuleParameter[] items)
    {
        return new ModuleParameter
        {
            id = id,
            type = ParameterType.GROUP,
            children = new List<ModuleParameter>(items)
        };
    }

    public static ModuleParameter Display(string id, float height, Action<RectTransform> onSpawn)
    {
        return new ModuleParameter
        {
            id = id,
            type = ParameterType.DISPLAY,
            displayHeight = height,
            onDisplaySpawn = onSpawn
        };
    }

    public float ValueToNormalized(float v)
    {
        if (max <= min) return 0f;

        if (curve == ParameterCurve.LOGARITHMIC && min > 0f && max > 0f)
        {
            float vClamped = Mathf.Clamp(v, min, max);  
            return Mathf.Log(vClamped / min) / Mathf.Log(max / min);
        }

        return Mathf.InverseLerp(min, max, v);
    }

    public float NormalizedToValue(float t)
    {
        t = Mathf.Clamp01(t);

        if (curve == ParameterCurve.LOGARITHMIC && min > 0f && max > 0f)
        {
            //value = min * (max/min)^t
            return min * Mathf.Pow(max / min, t);
        }

        return Mathf.Lerp(min, max, t);
    }
}

public enum ParameterCurve
{
    LINEAR,
    LOGARITHMIC
}

public enum ParameterType
{
    KNOB,
    TOGGLE,
    DROPDOWN,
    SLIDER,
    GROUP,
    DISPLAY
}