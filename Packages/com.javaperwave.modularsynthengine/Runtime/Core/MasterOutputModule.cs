using UnityEngine;

public class MasterOutModule : Module
{
    private const float EURORACK_TO_UNITY = 1f / CVStandard.UNIPOLAR_MAX;

    protected override void Initialize()
    {
        moduleType = "MasterOut";
        AddPort("audio_in", "Audio In", PortType.AUDIO, PortDir.INPUT);
    }

    public override float[] execute(float[] data, CV cv)
    {
        var inputs = GetInputPorts();
        if (inputs.Count == 0 || inputs[0].connection == null)
            return data;

        inputs[0].connection.source.execute(data, inputs[0].connection);

        for (int i = 0; i < data.Length; i++)
            data[i] = Mathf.Clamp(data[i] * EURORACK_TO_UNITY, -1f, 1f);

        return data;
    }
}