# Modular Synth Engine

A real-time modular synthesizer engine for Unity, written entirely in C# with
no external dependencies, that emulates Eurorack-standard analog modular
synthesizers. Audio is generated and processed sample-by-sample in real time
through `OnAudioFilterRead`, driven by a directed signal graph that connects
modules to an `AudioSource`.

This package contains **only the audio engine**. It has no dependency on any
UI framework and can be embedded standalone in your own projects.

> Looking for the optional control UI and the full demo project? See the
> [main repository](https://github.com/Javaperwave/ModularSynthEngine).

## Modules included

- Oscillator (VCO)
- Amplifier (VCA)
- Filter (VCF)
- Distortion
- Delay
- Reverb
- Ring Modulation
- LFO
- Envelope
- Sample & Hold
- Step Sequencer
- Pitch Quantizer
- Mixer
- Attenuverter
- Clock
- Oscilloscope

> **Note:** the full demo project additionally includes a MIDI-to-CV module
> (a keyboard-to-pitch/gate/trigger bridge) as part of the application layer,
> not the standalone package — it's an example of connecting external input
> to the module graph rather than a core engine module. See the
> [main repository](https://github.com/Javaperwave/ModularSynthEngine) if you
> need it as a reference for building your own input bridge.

## Requirements

Developed and tested with **Unity 6.3.6f1**. Other Unity 6.x versions are
likely compatible but haven't been verified.

Installing via git URL requires **Git** to be installed on your system and
available on your PATH (Unity uses it internally to fetch the package).

## Installation

### Option A — Engine only (recommended for embedding in your own project)

In your Unity project: **Window > Package Manager > + > Add package from git URL**

```
https://github.com/Javaperwave/ModularSynthEngine.git?path=Packages/com.javaperwave.modularsynthengine#v1.0.0
```

### Option B — Full demo project

```bash
git clone https://github.com/Javaperwave/ModularSynthEngine.git
```

Open it with Unity Hub using Unity 6.3.6f1 (or close to it), then load the
`MainMenu` scene to reach the interactive UI and try the included patches.

## Architecture

Core components:

- **`Synthesizer`** — singleton on the scene's root `GameObject`. Bridges
  Unity's `OnAudioFilterRead` with the module graph.
- **`Module`** — abstract base class for every module. Implements
  `execute(data, cv)`, reading and writing the audio buffer.
- **`Port`** — a module's input/output connector. Carries a signal type
  (`AUDIO`, `PITCHCV`, `MODCV`, `GATE`, `TRIGGER`) and a direction
  (`INPUT` / `OUTPUT`).
- **`CV`** — represents a patch cable between two ports; holds the reference
  to the source module.

Signal is **pulled, not pushed**: each module calls `ReadInputPort()` on its
inputs, which recursively triggers `execute()` on the connected source
module. To avoid redundant work when a module feeds several consumers, each
module caches its last computed buffer per audio block, keyed by
`AudioSettings.dspTime` (`TryGetFrameCache` / `SaveToFrameCache`).

Signal levels follow Eurorack-style conventions (`CVStandard`):

| Signal | Level |
|---|---|
| Audio | ±5V |
| CV (bipolar) | ±5V |
| Gate | 0/5V |
| Pitch | 1V/octave |

Each module processes audio inside `OnAudioFilterRead`, which Unity calls on
the **audio thread** — avoid allocating memory or touching Unity's
main-thread APIs (UI, GameObjects) from within module processing code.

## Using the Engine as a Standalone Library

To embed the engine in your own Unity project, add these components to a
`GameObject` with an `AudioSource`:

```
Synthesizer, PatchManager, ModuleFactory, PatchSerializer
```

Any code that creates or connects modules must run after these components
have started.

Minimal example — create an Oscillator and connect it to the master output:

```csharp
var master = Synthesizer.Instance.GetMasterOut();
var osc = (Oscilator)ModuleFactory.Instance.CreateModule("Oscillator");
osc.waveform = Oscilator.WaveformType.SIN;
osc.coarse = 12;
PatchManager.Instance.Connect(
    osc.moduleId, "audio_out", master.moduleId, "audio_in");
```

Saving / loading patches:

```csharp
PatchSerializer.Instance.SavePatchToPath(path);
PatchSerializer.Instance.LoadPatchFromPath(path);
```

Creating a new module type, at minimum:

- Inherit from `Module`.
- Implement `Initialize()`, declaring ports via
  `AddPort(id, label, portType, portDir)`.
- Implement `execute(float[] data, CV cv)`, filling and returning `data[]`.
- Register it with `ModuleFactory.Register()` so it can be instantiated and
  loaded from saved patches.

## License

Licensed under the [PolyForm Noncommercial License 1.0.0](./LICENSE.md) —
free for noncommercial use; commercial use requires separate permission
from the author.

## Author

**Javier Balenzategui Garcia**
[github.com/Javaperwave](https://github.com/Javaperwave)
