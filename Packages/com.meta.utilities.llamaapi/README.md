# Meta Llama API

Llama is a Large Language Model AI, developed by Meta.

## Overview

While Llama can be used to accomplish many goals, the ones outlined here are targeted toward common use cases:

- Ask questions, e.g.: 'What is a synonym for chair?'
- Make queries about images, e.g.: 'What is in this image: {image}'
- Translate data, e.g.: 'Translate "Hello" into Vietnamese.'
- Format data or answers into JSON

Getting the desired output from an AI relies mostly on constructing "prompts" (questions with context) worded in particular ways to generate suitable responses.

There is a Meta provided Llama API service available to interface with various models. Multiple models are available, e.g.: Llama-3.3, and Llama-4, with different variations & targeted purposes.

For more information: [`Llama API Docs`](https://llama.developer.meta.com/docs/overview/)

### Getting Started

Create an Account at: [`Llama API`](https://llama.developer.meta.com/).

Create an API Key to pass in requests: [`API Keys`](https://llama.developer.meta.com/api-keys/).

## Installation

You can integrate this package into your own project by using the Package Manager to [add the following Git URL](https://docs.unity3d.com/Manual/upm-ui-giturl.html):

```txt
https://github.com/oculus-samples/Unity-SpatialLingo.git?path=Packages/com.meta.utilities.llamaapi
```

## API Overview

Requests should start with a base URL: `https://api.llama.com/v1`, and end with an endpoint path. Here only the `/chat/completions` endpoint is described.

### Text Chat Completion

A chat consists of: an (optional) system prompt, and a list of (at least one) user messages. The system role is used to define how the AI should behave. The user role is used to pass messages or similar information to the AI on behalf of the user. Other parameters, such as temperature (how free responses can range) can be provided. An example curl command:

```bash
export LLAMA_API_KEY='YOUR_UNIQUE_LLAMA_API_KEY_GOES_HERE'

curl -X POST https://api.llama.com/v1/chat/completions \
-H "Content-Type: application/json" \
-H "Authorization: Bearer $LLAMA_API_KEY" \
-d '{
    "model": "Llama-4-Maverick-17B-128E-Instruct-FP8",
    "messages": [
        {
            "role": "user",
            "content": "Hello, how are you?"
        }
    ],
    "max_completion_tokens": 1024,
    "temperature": 0.7
}'
```

Example response:

```json
{
    "id": "AgxYOFsu1QLBrt2fWbs7pDy",
    "completion_message": {
        "role": "assistant",
        "stop_reason": "stop",
        "content": {
            "type": "text",
            "text": "I'm just a language model, so I don't have feelings or emotions like humans do, but I'm functioning properly and ready to help with any questions or tasks you have! How can I assist you today?"
        }
    },
    "metrics": [
        {
            "metric": "num_completion_tokens",
            "value": 41,
            "unit": "tokens"
        },
        {
            "metric": "num_prompt_tokens",
            "value": 16,
            "unit": "tokens"
        },
        {
            "metric": "num_total_tokens",
            "value": 57,
            "unit": "tokens"
        }
    ]
}
```

### Image Understanding

An image can be passed, using a URL or a base64 encoded string, as shown in the following:

```bash
curl -X POST https://api.llama.com/v1/chat/completions \
-H "Content-Type: application/json" \
-H "Authorization: Bearer $LLAMA_API_KEY" \
-d '{
 "model": "Llama-4-Maverick-17B-128E-Instruct-FP8",
    "messages": [
    {
   "role": "system",
   "content": [
       {
            "type": "text",
        "text": "You are a helpful assistant that provides concise answers."
       }
   ]
 },
 {
      "role": "user",
      "content": [
        {
          "type": "text",
          "text": "What word best describes the object in this image? Only respond with the minimal amount of words. Do not include any punctuation."
        },
        {
          "type": "image_url",
          "image_url": {
            "url": "https://www.wikipedia.org/portal/wikipedia.org/assets/img/Wikipedia-logo-v2@2x.png"
          }
        }
      ]
    }
  ]
}'
```

Example response:

```json
{
    "id": "AlDuGfFHvGjeJpi17WXfPgK",
    "completion_message": {
        "role": "assistant",
        "stop_reason": "stop",
        "content": {
            "type": "text",
            "text": "Wikipedia logo"
        }
    },
    "metrics": [
        {
            "metric": "num_completion_tokens",
            "value": 3,
            "unit": "tokens"
        },
        {
            "metric": "num_prompt_tokens",
            "value": 777,
            "unit": "tokens"
        },
        {
            "metric": "num_total_tokens",
            "value": 780,
            "unit": "tokens"
        }
    ]
}
```

### JSON Formatting

The AI can be prompted to return JSON objects rather than freeform statements:

```bash
export LLAMA_API_KEY='YOUR_UNIQUE_LLAMA_API_KEY_GOES_HERE'

curl -X POST https://api.llama.com/v1/chat/completions \
-H "Content-Type: application/json" \
-H "Authorization: Bearer $LLAMA_API_KEY" \
-d '{
    "model": "Llama-4-Maverick-17B-128E-Instruct-FP8",
    "messages": [
        {
            "role": "user",
            "content": "Dissect the following sentence into nouns, verbs, and adjectives. Return a json formatted object with keys: \"nouns\", \"verbs\", and \"adjectives\". Do not include any extra information, only the json object: \"Where is the beef?\""
        }
    ],
    "max_completion_tokens": 1024
}'
```

Example response:

```json
{
    "id": "Aqkxl58rcHtTD35kyCMJDtM",
    "completion_message": {
        "role": "assistant",
        "stop_reason": "stop",
        "content": {
            "type": "text",
            "text": "{\"nouns\": [\"beef\"], \"verbs\": [\"is\"], \"adjectives\": []}"
        }
    },
    "metrics": [
        {
            "metric": "num_completion_tokens",
            "value": 21,
            "unit": "tokens"
        },
        {
            "metric": "num_prompt_tokens",
            "value": 61,
            "unit": "tokens"
        },
        {
            "metric": "num_total_tokens",
            "value": 82,
            "unit": "tokens"
        }
    ]
}
```

## Configuration

Because querying the Llama service requires a unique API key, the value needs to be configurable. The key should be stored securely and passed when initializing the API client.

### Security Considerations for Quest Apps

**Important:** Quest apps that are shipped to end users should **not** directly embed Llama API keys in the application. Doing so exposes your API key to anyone who inspects the app binary, potentially leading to unauthorized usage and unexpected charges.

**Recommended Approach:** Instead of embedding API keys directly, Quest apps should:

1. Authenticate the client/app with your own backend server
2. Have your server validate the client's identity and entitlements
3. Make Llama API calls from your server (or proxy to another service provider, or use your own hosted model with something like Ollama)

This approach keeps your API keys secure on the server side and gives you control over rate limiting, user authentication, and cost management.

### Custom API Key Provider

You can configure custom authentication logic using `LlamaRestApi.GetApiKeyAsync`. This static delegate allows you to provide dynamic API keys and base URLs at runtime:

```csharp
using Meta.Utilities.LlamaAPI;

[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
public static void ConfigureCustomAuth()
{
    LlamaRestApi.GetApiKeyAsync = async () =>
    {
        // Example: Authenticate with your backend server
        string userToken = await AuthenticateWithYourServer();
        string serverProxyUrl = "https://your-server.com/llama-proxy/v1/";

        // Return tuple: (apiKey/token, baseUrl)
        // baseUrl can be null to use the default Llama API endpoint
        return (userToken, serverProxyUrl);
    };
}
```

The function should return a tuple containing:

- `string apiKey`: The API key or authentication token to use
- `string baseUrl`: The base URL for the API endpoint (or `null` to use the default `https://api.llama.com/v1`)

### Development Configuration

For development and testing purposes only, you can configure your Llama API key by modifying [Assets/SpatialLingo/Resources/ScriptableSettings/SpatialLingoSettings.asset](../../Assets/SpatialLingo/Resources/ScriptableSettings/SpatialLingoSettings.asset). This approach is suitable for local development but should not be used in production builds shipped to users.

## C# REST API

[LlamaRestApi.cs](../com.meta.utilities.llamaapi/Runtime/Scripts/LlamaRestApi.cs) encapsulates the Llama endpoint requests, converting elements to/from C# objects to URL requests.

The API is initialized passing the API Key:

```csharp
var llama = new LlamaRestApi(api_key);
```

You can start a new chat sequence using `llama.StartNewChat(SystemPrompt);` and continue the chat with additional messages. Giving the API more history allows for more conversational based interactions:

```csharp
var chat = llama.StartNewChat("You are a language assistant.");
var result = await llama.ContinueChat(chat, "Translate \"Cat\" from English into Vietnamese.");
// eg result: The translation of "Cat" from English into Vietnamese is "Mèo".
```

## AssistantAI Wrapper

[AssistantAI.cs](../../Assets/SpatialLingo/Scripts/AI/AssistantAI.cs) encapsulates the prompts, chats, and data passing, JSON parsing necessary to communicate with Llama and receive C# objects.

The class is initialized by passing an instance of the LlamaRestApi:

```csharp
var assistantAI = new AssistantAI(llamaAPI);
```

By abstracting the details away, the Assistant AI interface allows for more targeted usage. As an example, word clouds are generated from a single classification & cropped camera image (from an object recognition pass). The result is a rich description of the scene with nouns, verbs, and adjectives.

```csharp
// Definition ([AssistantAI.cs](../../Assets/SpatialLingo/Scripts/AI/AssistantAI.cs)):
public async Task<WordCloudResult> GenerateWordCloudData(SupportedLanguage userLanguage, SupportedLanguage targetLanguage, string classification, string imageString, string context = null)

// Call:
var wordCloudResult = await m_assistantAI.GenerateWordCloudData(userLanguage, targetLanguage, classification, imageString);

// wordCloudResult contains parts of speech for presentation to the user during gameplay
```

The class includes helper functions, such as converting a texture into a base64 encoded string that is needed for Llama to perform image sensing:

```csharp
// Definition ([AssistantAI.cs](../../Assets/SpatialLingo/Scripts/AI/AssistantAI.cs)):
private string GetSourceImageAsString(Texture2D textureImage, EncodeImageType encodeType = EncodeImageType.JPG)

// Call:
var imageString = GetSourceImageAsString(imageTexture);
```

Other helper functions exist, such as automatically scaling images down to make more optimal requests to help minimize network traffic:

```csharp
// Definition ([AssistantAI.cs](../../Assets/SpatialLingo/Scripts/AI/AssistantAI.cs)):
public string GetNetworkSafeImageString(Texture2D imageSource)

// Call:
var imageString = assistantAI.GetNetworkSafeImageString(croppedImage);
```

## Example Use

### Basic Chat Example

```csharp
using Meta.Utilities.LlamaAPI;

// ...

var api_key = "<Your Key Here>";
var llama = new LlamaRestApi(api_key);

var chat = llama.StartNewChat($"You are a translation assistant. You translate sentences from \"English\" to \"Spanish\".");
var task = llama.ContinueChat(chat, $"Translate this sentence from \"English\" to \"Spanish\": \"Where is the nearest coffee shop?\" ");
var response = await task;

var translation = "";
if (response != null)
{
    translation = response.message.text;
}

Debug.Log($"Translation: {translation}");
```

## Example Scenes

### LlamaAPISample Scene

The example Llama API scene demonstrates how to make basic calls using the LlamaRestApi class:

Example Image:

![640_atrium.png](Documentation~/Images/LLAMA/640_atrium.png)

Example Result:

![ExampleLlama.png](Documentation~/Images/LLAMA/ExampleLlama.png)

### AssistantAISample Scene

The example Assistant AI scene demonstrates how to make basic calls using the AssistantAI class:

Example AssistantAI generating sentences with varying degrees of complexity:

![ExampleAssistantAI2.png](Documentation~/Images/LLAMA/ExampleAssistantAI2.png)

Example AssistantAI evaluating a user transcription, responding with reasoning:

![ExampleAssistantAI1.png](Documentation~/Images/LLAMA/ExampleAssistantAI1.png)

## Common Use Cases

### Accurate Object Description

The classification from object detection is a coarse (and sometimes incorrect) umbrella term for detected objects. Passing a cropped image of the subject item to Llama, the scene description is able to provide a more accurate and descriptive summary of the item.

### Generating Lesson Data (Word Clouds)

Passing a descriptive noun along with an image of the corresponding object to the Llama scene description service, it is able to return a rich set of nouns, adjectives, and verbs.

### Translation

Llama is used to translate the scene description results into both the user's native language and the target language, to facilitate translation and language learning.

### Speech Evaluation

Using a transcription of the user's voice, Llama is used to evaluate a user's response using multiple criteria, including checking for the presence of specific words and parts of speech (nouns, verbs, and adjectives).
