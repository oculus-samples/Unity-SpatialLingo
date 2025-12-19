# Voice Synthesis - wit.ai

## Introduction

Voice synthesis and speech for GollyGosh is achieved through a package provided by Meta and [wit.ai](https://wit.ai/).

## Setup

To avoid rate limits, it's advised to create personal instances of wit.ai for use.

- Register an app on wit.ai for each supported language within Spatial Lingo.
- Copy the server access token (Management > Settings).
![WitaiSettings.png](Images/VoiceSynthesis/WitaiSettings.png)
- In Unity, go to Meta > Voice SDK > Voice Hub
![VoiceHubMenu.png](Images/VoiceSynthesis/VoiceHubMenu.png)

- In the Voice Hub window, select the Wit Configurations tab, then click the New button.
![VoiceHubWindow.png](Images/VoiceSynthesis/VoiceHubWindow.png)
- Paste your Server Access Token in the wizard, then click Create.
![CreateWitai.png](Images/VoiceSynthesis/CreateWitai.png)
- Save the configuration asset to [Assets/SpatialLingo/Data/Witai/](../../../Assets/SpatialLingo/Data/Witai/)
- If this is a language outside of English or Spanish, go to [Assets/SpatialLingo/Scripts/AppSystems/AppSessionData.cs](../../../Assets/SpatialLingo/Scripts/AppSystems/AppSessionData.cs) and add it to the `Language` enum.
- Add the configuration asset to [Assets/SpatialLingo/Data/Witai/WitaiSettingsHolder.asset](../../../Assets/SpatialLingo/Data/Witai/WitaiSettingsHolder.asset), and select the appropriate language.
![WitaiSettingsHolder.png](Images/VoiceSynthesis/WitaiSettingsHolder.png)

## Scripts

[VoiceSpeaker.cs](../Runtime/Scripts/VoiceSpeaker.cs)

- This wraps the wit.ai libraries, adding configuration for GollyGosh's voice and pitch, as well as handling any edge cases before querying the wit.ai service.

[Tutorial.cs](../../../Assets/SpatialLingo/Scripts/Lessons/Tutorial.cs)

- This contains the full script for GollyGosh, to be referenced when triggering any speech.

[GollyGoshInteractionManager.cs](../../../Assets/SpatialLingo/Scripts/Characters/GollyGoshInteractionManager.cs)

- Contains the `Speak()` function, referencing the lines in [Tutorial.cs](../../../Assets/SpatialLingo/Scripts/Lessons/Tutorial.cs) and processing them through VoiceSpeaker.

## Additional Tips

- wit.ai uses [SSML](https://wit.ai/docs/ssml) to add many more options in how text is synthesized into speech audio.
- Voice synthesis [officially supports only English](https://wit.ai/docs/http/20240304/#post__synthesize_link), meaning it will attempt to synthesize foreign words with incorrect pronounciation. Unofficially, the pronounciation can be corrected if given enough context. A single word, like "cuchara", will have an English pronounciation. However, "La palabra es: cuchara" will generate a Spanish pronounciation. Using SSML to silence the contextual phrasing allows GollyGosh to pronounce non-English words more accurately. This is implemented with LanguageTextHint in VoiceSpeaker.
