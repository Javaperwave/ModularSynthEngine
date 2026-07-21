# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-07-21

### Added
- Initial public release of the Modular Synth Engine as a standalone Unity package.
- Directed, pull-based signal graph (`Synthesizer`, `Module`, `Port`, `CV`)
  connecting modules to an `AudioSource`.
- Real-time, sample-accurate audio processing via `OnAudioFilterRead`.
- Per-block frame caching to avoid redundant module execution when a module
  feeds multiple consumers.
- Modules: Oscillator (VCO), Amplifier (VCA), Filter (VCF), Distortion, Delay,
  Reverb, Ring Modulation, LFO, Envelope, Sample & Hold, Step Sequencer,
  Pitch Quantizer, Mixer, Attenuverter, Clock, Oscilloscope.
- Patch serialization and loading (`PatchSerializer`).
- Module registration/instantiation via `ModuleFactory`.
- Packaged for installation via Unity Package Manager (git URL), independent
  of any UI or demo project.

### Notes
- MIDI-to-CV input bridging is provided by the demo application (see the
  main repository), not by this standalone package — it depends on
  application-level input handling rather than being a core engine module.
