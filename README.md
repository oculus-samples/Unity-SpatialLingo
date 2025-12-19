![SpatialLingo.png](Documentation/Images/SpatialLingo.png)

# Spatial Lingo

Spatial Lingo is a spatialized language practice experience, guiding users to identify and describe objects around their environment in a target language. This was made possible using Meta libraries, such as [Llama](https://www.llama.com/),  [Mixed Reality Utility Kit (MRUK)](https://developers.meta.com/horizon/documentation/unity/unity-mr-utility-kit-overview/), and the [Voice SDK](https://developers.meta.com/horizon/documentation/unity/voice-sdk-overview). This experience supports both hand tracking and controllers.

Follow Golly Gosh (the polyglot!) as they lead you through your own, real-world space, allowing you to practice your vocabulary using familiar objects. Grow the language tree by completing lessons from Golly Gosh, learning nouns, verbs, and adjectives along the way!

# Project Overview

The **Spatial Lingo** project helps Unity developers understand and develop for multiple Meta features: Passthrough Camera API (PCA), Voice SDK, Interaction SDK, Mixed Reality Utility Kit (MRUK), Llama API, and Unity Sentis. The main scene as well as multiple sample scenes demonstrate the implementation and usefulness of each feature.

| Gym Scene | Word Cloud Scene | Character Scene | Camera Image Scene |
|:-------------:|:--------------------:|:---------------------:|:------------:|
| ![GIF 1](./Documentation/Images/Overview/gif_gym.gif) | ![GIF 2](./Documentation/Images/Overview/gif_wc.gif) | ![GIF 3](./Documentation/Images/Overview/gif_char.gif) | ![GIF 4](./Documentation/Images/Overview/gif_pass.gif) |

# Getting Started

## Getting The Code

First, ensure you have Git LFS installed by running this command:

```sh
git lfs install
```

Then, clone this repo using the "Code" button above, or this command:

```sh
git clone https://github.com/oculus-samples/Unity-SpatialLingo.git
```

### Application Settings

For development, configure your Llama API key in [Assets/SpatialLingo/Resources/ScriptableSettings/SpatialLingoSettings.asset](./Assets/SpatialLingo/Resources/ScriptableSettings/SpatialLingoSettings.asset).

**Important:** Do not ship Quest apps with embedded API keys, as they can be extracted from the app binary. For production, use `LlamaRestApi.GetApiKeyAsync` to implement server-side authentication. See the [Llama API documentation](Packages/com.meta.utilities.llamaapi/README.md#configuration) for details.

## How to run the project in Unity

1. Make sure you're using Unity 6000.0.51f1 or newer
2. Load the [Assets/SpatialLingo/Scenes/MainScene.unity](Assets/SpatialLingo/Scenes/MainScene.unity) scene
3. Open the Meta XR Simulator
4. Start Play Mode

# Showcase Features

Each of these features have been built to be accessible and scalable for other developers to take and build upon in their own projects.

## Object Identification

*Spatial Lingo* is able to identify objects around the user's environment, allowing for spatial placement and dynamic generation of language lessons.

## Lesson Generation

Dynamic vocabulary lessons are generated as the user progresses in growing the langauge tree. After objects are identified in the user's environment, relevant verbs and adjectives for those objects are generated to allow for more lesson variety.

## Voice Synthesis

Golly Gosh is able to speak in several different languages. Voice is dynamically synthesized from text, so they can teach users proper pronounciation during language lessons.

## Voice Transcription

Users' speech is transcribed when presented with a word cloud, which is also supported in several languages.

## Lesson Evaluation

A user's response is sent to Llama to determine if the user has responded well enough to complete a given lesson's word cloud.

# Dependencies

This project makes use of the following plugins and software:

- [Unity](https://unity.com/download) 6000.0.51f1 or newer
- [YOLO](https://github.com/MultimediaTechLab/YOLO) (with [COCO dataset](https://cocodataset.org/#home))
- See [MetaSdk.md](Documentation/MetaSdk.md) for all Meta libraries used

# Project Documentation

More information about the services and systems of this project can be found in the [Documentation](Documentation) section.

## Voice Services

- [Voice Transcription](Packages/com.meta.utilities.speechandtext/Documentation~/VoiceTranscription.md)
- [Voice Synthesis](Packages/com.meta.utilities.speechandtext/Documentation~/VoiceSynthesis.md)

## LLM

- [Llama](Packages/com.meta.utilities.llamaapi/README.md)

## Object Recognition and Tracking

- [Image Object Recognition](Packages/com.meta.utilities.objectclassifier/Documentation~/ImageObjectRecognition.md)
- [Face Blurring](Packages/com.meta.utilities.objectclassifier/Documentation~/FaceBlurring.md)

## Systems

- [Visual Scripting](Documentation/StateMachine.md)

# Sample Scenes

Sample scenes can be found at [Assets/SpatialLingo/Scenes](Assets/SpatialLingo/Scenes).

## Voice Transcription Scene

<img src="./Documentation/Images/SampleScenes/VoiceTranscription.gif" width="300" />

To run, open [WordCloudSample.unity](Assets/SpatialLingo/Scenes/Samples/WordCloudSample.unity) and enter play mode with the simulator. Click the "Activate Microphone" button to start transcription thorugh your microphone.

# License

See [LICENSE.md](LICENSE).

# Contributing

See [CONTRIBUTING.md](./CONTRIBUTING.md).
