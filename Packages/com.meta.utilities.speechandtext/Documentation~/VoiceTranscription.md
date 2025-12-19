# Voice Transcription - wit.ai

## Introduction

Voice transcription is achieved through a package provided by Meta and [wit.ai](https://wit.ai/). It's used to listen for the user's speech, and compare it to lesson objectives.

## Setup

See [VoiceSynthesis.md](VoiceSynthesis.md) setup for installation steps.

## Scripts

[VoiceTranscriber.cs](../Runtime/Scripts/VoiceTranscriber.cs)

- This wraps wit.ai's voice transcription. It triggers `VoiceTranscriptionUpdateIncomplete` word by word as the user speaks, and `VoiceTranscriptionUpdateComplete` when a pause has been detected in the user's speech. This is primarily used in [ExerciseManager.cs](../../../Assets/SpatialLingo/Scripts/Lessons/ExerciseManager.cs) to compare spoken words with the given lesson.

[STTLanguageSwitch.cs](../Runtime/Scripts/STTLanguageSwitch.cs)

- This swaps the active wit.ai configuration, given a target language. This allows Spatial Lingo to listen for any language supported by wit.ai, as long as a configuration is provided. As soon as the user selects a language, this script activates the corresponding wit.ai configuration.
