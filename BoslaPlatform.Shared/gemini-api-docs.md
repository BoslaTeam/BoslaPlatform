# Gemini API Documentation

> **Note:** The **Interactions API** is now generally available (GA) and is the recommended interface for all new projects. It is optimized for agentic workflows, state management, and the latest models.
>
> Base URL: `https://generativelanguage.googleapis.com/v1beta`
> All requests require the header: `x-goog-api-key: $GEMINI_API_KEY`

---

## Quick Start

Get an API key from [Google AI Studio](https://aistudio.google.com/apikey), then make your first call:

**Python**
```python
from google import genai

client = genai.Client()

interaction = client.interactions.create(
    model="gemini-3.5-flash",
    input="Explain how AI works in a few words"
)

print(interaction.output_text)
```

**JavaScript**
```javascript
import { GoogleGenAI } from "@google/genai";

const ai = new GoogleGenAI({});

const interaction = await ai.interactions.create({
  model: "gemini-3.5-flash",
  input: "Explain how AI works in a few words",
});

console.log(interaction.output_text);
```

**REST**
```bash
curl -X POST "https://generativelanguage.googleapis.com/v1beta/interactions" \
  -H "x-goog-api-key: $GEMINI_API_KEY" \
  -H 'Content-Type: application/json' \
  -d '{
    "model": "gemini-3.5-flash",
    "input": "Explain how AI works in a few words"
  }'
```

---

## Models

### Gemini 3 Series (Latest)

| Model | Description | Status |
|-------|-------------|--------|
| `gemini-3.1-pro` | Most intelligent model; best-in-class multimodal understanding with advanced reasoning | Preview |
| `gemini-3.5-flash` | Most intelligent Flash model; frontier-class performance for agentic & coding tasks | Stable |
| `gemini-3-flash` | Frontier-class performance at a fraction of the cost | Preview |
| `gemini-3.1-flash-lite` | High-volume, cost-sensitive model with Gemini 3 series quality | Stable |
| `gemini-3.5-live-translate` | Low-latency, real-time speech-to-speech translation; 70+ languages | Preview |
| `gemini-3.1-flash-live` | High-quality, low-latency Live API model for real-time voice AI | Preview |
| `gemini-3.1-flash-tts` | Powerful low-latency speech generation with expressive audio tags | Preview |
| Nano Banana 2 | High-efficiency image generation & editing, optimized for speed | Stable |
| Nano Banana Pro | State-of-the-art native image generation for highly contextual creation | Stable |

### Gemini 2.5 Series

| Model | Description | Status |
|-------|-------------|--------|
| `gemini-2.5-pro` | Most advanced model; deep reasoning and coding capabilities | Stable |
| `gemini-2.5-flash` | Best price-performance model for low-latency, high-volume tasks | Stable |
| `gemini-2.5-flash-lite` | Fastest and most budget-friendly in the 2.5 family | Stable |
| `gemini-2.5-flash` (image) | Native image generation & editing for fast creative workflows | Stable |
| `gemini-2.5-flash-live` | Flagship Live API model with bidirectional voice/video & native audio reasoning | Preview |
| `gemini-2.5-flash-tts` | Fast, controllable TTS for real-time assistants | Preview |
| `gemini-2.5-pro-tts` | High-fidelity speech synthesis for podcasts and audiobooks | Preview |

### Model Version Aliases

- **Stable** (`gemini-3.5-flash`) — points to a specific stable release; best for production
- **Preview** (`gemini-3.1-pro-preview`) — production-usable but may have stricter rate limits; deprecated with ≥2 weeks notice
- **Latest** — hot-swapped with each new release of a model variation
- **Experimental** — cutting-edge, may change rapidly

---

## Core APIs

### Interactions API (Recommended)

The unified, stateful interface for Gemini models and agents. GA since mid-2026.

- Optimized for agentic workflows and long-running tasks
- Built-in state management
- Supports all latest models and frontier features
- Migration guide available for legacy `generateContent` users

Endpoint: `POST /v1beta/interactions`

### generateContent (Legacy)

The original unary/streaming content generation API. Fully supported and will continue receiving new mainline Gemini models.

```bash
curl "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash:generateContent" \
  -H "x-goog-api-key: $GEMINI_API_KEY" \
  -H 'Content-Type: application/json' \
  -X POST \
  -d '{"contents": [{"parts": [{"text": "Your prompt here"}]}]}'
```

---

## Core Capabilities

### Text Generation
Generate text responses from text, image, video, audio, or document inputs.
- Streaming supported via `streamGenerateContent`
- Structured output (JSON mode) available

### Multimodal Input
- **Images** — understanding, analysis, generation (Nano Banana models)
- **Video** — understanding (upload or YouTube URL); generation via Veo 3.1
- **Audio** — understanding (transcription, analysis); generation via TTS models
- **Documents** — process up to 1,000 pages of PDFs with full multimodal understanding
- **Long context** — input millions of tokens for unstructured data

### Structured Outputs
Constrain the model to respond with JSON for automated processing.
```python
response = client.interactions.create(
    model="gemini-3.5-flash",
    input="List 3 planets",
    config={"response_mime_type": "application/json"}
)
```

### Function Calling
Connect Gemini to external APIs and tools to build agentic workflows. Supports combining built-in tools with custom function calls in a single API call.

### Thinking / Reasoning
Thinking capabilities improve performance on complex tasks and multi-step agents. Exposes "thought signatures" for interpretability.

### Embeddings
Generate text embeddings for semantic search, clustering, and retrieval.

---

## Tools (Built-in)

| Tool | Description |
|------|-------------|
| **Google Search** | Ground responses with live web search results |
| **Google Maps** | Location-aware grounding (GA) |
| **Code Execution** | Generate and run Python code; output graphs |
| **URL Context** | Fetch and use web page content as context (GA) |
| **Computer Use** | Control a computer interface |
| **File Search** | Search across uploaded files |

All built-in tools can be combined with custom function calling in a single API call.

---

## Live API

Low-latency, real-time bidirectional streaming for voice and video agents.

**Key features:**
- Sub-second audio response latency
- Audio + video + text input streams
- Barge-in (interrupt the model mid-response)
- Affective dialog (adapts tone to user's expression)
- Tool use (function calling, Google Search)
- Audio transcriptions for both input and output
- 24 supported languages
- Proactive audio mode (preview)

**Getting started:**
- Via GenAI SDK (Python): `genai.Client().live.connect(...)`
- Via raw WebSockets (JavaScript)

**Live Translate:** Real-time speech-to-speech translation across 70+ languages.

---

## Agents

Build autonomous agents powered by Gemini.

- **Managed Agents** — hosted agents with built-in state and tool orchestration
- **Antigravity Agent** — Google's reference agentic coding assistant
- **Deep Research Agent** — multi-step research and synthesis
- **Custom Agents** — define your own agent logic, environments, and tools
- Supports LangChain/LangGraph, CrewAI, LlamaIndex, Vercel AI SDK, Temporal

---

## Generative Media

### Image Generation (Nano Banana / Imagen)
- **Nano Banana 2** (`gemini-3.1-flash-image`) — fast, high-efficiency image generation & editing
- **Nano Banana Pro** (`gemini-3-pro-image`) — highest quality native image creation
- **Imagen 4** — standalone image generation model (Ultra, Standard, Fast variants)

### Video Generation (Veo)
- **Veo 3.1** — state-of-the-art cinematic video with native audio; supports 4/6/8s clips
- **Veo 3.1 Fast** — developer-focused, lower cost video generation
- Text-to-video and image-to-video; first/last frame control

### Music Generation (Lyria)
- **Lyria 3 Clip** — 30-second music clips from text/image prompts (48kHz stereo)
- **Lyria 3 Pro** — full-length song generation
- **Lyria RealTime** — real-time music streaming

### Speech / TTS
- **Gemini 3.1 Flash TTS** — expressive, steerable narration control
- **Gemini 2.5 Flash TTS** — fast, cost-efficient TTS
- **Gemini 2.5 Pro TTS** — high-fidelity for podcasts and audiobooks

---

## Optimization & Infrastructure

| Feature | Description |
|---------|-------------|
| **Batch API** | Send large volumes of non-time-sensitive requests; up to 90% cost discount |
| **Context Caching** | Cache repeated prompt prefixes to reduce cost and latency |
| **Webhooks** | Real-time notifications for async operations (batch jobs, video generation) |
| **Flex Inference** | Lower-cost tier; trades latency for price |
| **Priority Inference** | Higher-throughput tier for latency-sensitive workloads |
| **Streaming** | Server-sent events for incremental response delivery |

---

## File Input

| Method | Details |
|--------|---------|
| **Inline data** | Base64-encode small files (<20MB) directly in requests |
| **Files API** | Upload files up to 2GB; store for 48 hours; reference by URI |
| **URL / YouTube** | Pass public URLs directly as context |

Supported file types: images (JPEG, PNG, WebP, HEIC, HEIF), video (MP4, MPEG, MOV, AVI, etc.), audio (WAV, MP3, FLAC, AAC, etc.), documents (PDF, plain text, HTML, CSV, Markdown).

---

## API Reference

### Authentication
All requests require an API key:
```
x-goog-api-key: YOUR_API_KEY
```
OAuth 2.0 also supported for user-context access.

### API Versions
- **v1** — stable, production-recommended
- **v1beta** — access to preview features and latest models

### Key Endpoints

| Endpoint | Description |
|----------|-------------|
| `POST /v1beta/interactions` | Interactions API (recommended) |
| `POST /v1beta/models/{model}:generateContent` | Single-turn generation |
| `POST /v1beta/models/{model}:streamGenerateContent` | Streaming generation |
| `POST /v1beta/models/{model}:embedContent` | Generate embeddings |
| `POST /v1beta/models/{model}:countTokens` | Count tokens |
| `POST /v1beta/files` | Upload a file (Files API) |
| `GET /v1beta/files/{name}` | Get file metadata |
| `DELETE /v1beta/files/{name}` | Delete an uploaded file |
| `GET /v1beta/models` | List available models |
| `GET /v1beta/models/{model}` | Get model details |
| `POST /v1beta/models/{model}:generateImages` | Generate images (Imagen) |
| `POST /v1beta/videos:generate` | Generate video (Veo) |

---

## Safety

- **Safety settings** — configure harm category thresholds (harassment, hate speech, sexually explicit, dangerous content)
- **Safety guidance** — best practices for responsible deployment
- Abuse monitoring is active on all API traffic

```python
from google.genai import types

config = types.GenerateContentConfig(
    safety_settings=[
        types.SafetySetting(
            category="HARM_CATEGORY_DANGEROUS_CONTENT",
            threshold="BLOCK_LOW_AND_ABOVE"
        )
    ]
)
```

---

## SDKs & Compatibility

| SDK / Library | Install |
|---------------|---------|
| Python (Google GenAI) | `pip install google-genai` |
| JavaScript/TypeScript | `npm install @google/genai` |
| Go | `go get google.golang.org/genai` |
| Java | Maven: `com.google.genai:google-genai` |
| OpenAI-compatible | Use base URL `https://generativelanguage.googleapis.com/v1beta/openai/` |

### Framework Integrations
LangChain, LangGraph, CrewAI, LlamaIndex, Vercel AI SDK, Temporal

---

## Pricing & Limits

- **Free tier** available via Google AI Studio (rate-limited)
- **Pay-as-you-go** billing: charged per input/output token; media billed per second/image
- **Batch API** offers up to 90% discount for async workloads
- Rate limits vary by model and tier; see [Rate limits docs](https://ai.google.dev/gemini-api/docs/rate-limits)
- Prepay and Postpay billing plans available

---

## Resources

| Resource | Link |
|----------|-------|
| Official Docs | https://ai.google.dev/gemini-api/docs |
| API Reference | https://ai.google.dev/api |
| Google AI Studio | https://aistudio.google.com |
| Cookbook (examples) | https://github.com/google-gemini/cookbook |
| Developer Community | https://discuss.ai.google.dev/c/gemini-api/ |
| Status Page | https://aistudio.google.com/status |
| Release Notes | https://ai.google.dev/gemini-api/docs/changelog |
| Deprecations | https://ai.google.dev/gemini-api/docs/deprecations |
| Migrate to Interactions API | https://ai.google.dev/gemini-api/docs/migrate-to-interactions |

---

*Last updated from official docs: June 24, 2026*
