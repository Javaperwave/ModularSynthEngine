using UnityEngine;

[DefaultExecutionOrder(100)]
public class AppModules : MonoBehaviour
{
    void Awake()
    {
        ModuleFactory.Instance.Register("MIDI to CV", typeof(MIDIToCV));
    }
}