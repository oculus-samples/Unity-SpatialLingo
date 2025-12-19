// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using Meta.XR.Samples;
using SpatialLingo.Audio;
using SpatialLingo.SpeechAndText;
using SpatialLingo.UI;
using TMPro;
using UnityEngine;

namespace SpatialLingo.Samples
{
    /// <summary>
    /// Test Out Synthesizing Audio
    /// </summary>
    [MetaCodeSample("SpatialLingo")]
    public class VoiceSynthesizeSample : MonoBehaviour
    {
        [Header("Assets")]
        [SerializeReference] private GameObject m_buttonPrefab;
        [SerializeReference] private VoiceSynthesizer m_synthesizer;

        private void Start()
        {
            var buttonNames = new List<(string, Action)>()
            {
                ("English", HandleButtonActionEnglish),
                ("Spanish", HandleButtonActionSpanish),
                ("Vietnamese", HandleButtonActionVietnamese),
                ("French", HandleButtonActionFrench),
                ("German", HandleButtonActionGerman),
                ("Arabic", HandleButtonActionArabic),
                ("Italian", HandleButtonActionItalian),
                ("Thai", HandleButtonActionThai),
                ("Hindi", HandleButtonActionHindi),
                ("Portuguese", HandleButtonActionPortuguese),
                ("Indonesian", HandleButtonActionIndonesian),
                ("Tagalog", HandleButtonActionTagalog),
            };
            var index = 0;
            foreach (var tuple in buttonNames)
            {
                var buttonName = tuple.Item1;
                var buttonAction = tuple.Item2;
                var button = Instantiate(m_buttonPrefab);
                var text = button.GetComponentInChildren<TextMeshPro>();
                text.text = buttonName;
                var canvasButton = button.GetComponentInChildren<CanvasXRButton>();
                canvasButton.ButtonWasSelected += CanvasWasSelectedEvent;
                m_canvasButtons.Add((canvasButton, buttonAction));
                var position = new Vector3(-0.66f + index * 0.14f, 1.0f, 0.5f);
                button.transform.position = position;
                index += 1;
            }
            // start off with something:
            HandleButtonActionEnglish();
            // HandleButtonActionSpanish();
            // HandleButtonActionVietnamese();
            // HandleButtonActionFrench();
            // HandleButtonActionGerman();
            // HandleButtonActionArabic();
            // HandleButtonActionItalian();
            // HandleButtonActionThai();
            // HandleButtonActionHindi();
            // HandleButtonActionTagalog();
        }

        private Dictionary<string, AudioClip> m_clipCache = new();

        private List<(CanvasXRButton, Action)> m_canvasButtons = new();
        private void CanvasWasSelectedEvent(CanvasXRButton button)
        {
            for (var i = 0; i < m_canvasButtons.Count; i++)
            {
                if (m_canvasButtons[i].Item1 == button)
                {
                    m_canvasButtons[i].Item2.Invoke();
                    break;
                }
            }
        }

        private async void SynthesizeAudio(string text)
        {
            if (m_clipCache.TryGetValue(text, out var existing))
            {
                _ = AudioManager.Instance.PlayOneShot2D(existing);
                return;
            }
            var clip = await m_synthesizer.SythesizeAudioForText(text);
            if (clip != null)
            {
                m_clipCache[text] = clip;
                _ = AudioManager.Instance.PlayOneShot2D(clip);
            }
        }

        private void HandleButtonActionEnglish()
        {
            SynthesizeAudio("This is an English phrase to play.");
        }

        private void HandleButtonActionSpanish()
        {
            SynthesizeAudio("Esta es una prueba de texto a voz.");
        }

        private void HandleButtonActionVietnamese()
        {
            SynthesizeAudio("điện thoại không dây ở đâu?");
        }

        private void HandleButtonActionFrench()
        {
            SynthesizeAudio("Combien de chaussures devons-nous acheter?");
        }

        private void HandleButtonActionGerman()
        {
            SynthesizeAudio("Draußen bellen alle Hunde die Katzen an.");
        }

        private void HandleButtonActionArabic()
        {
            //SynthesizeAudio(AssistantAI.SupportedLanguage.Arabic, "يواجه هاتفي مشكلة في الاتصال بالإنترنت.");
            SynthesizeAudio("yuajih hatifi mushkilatan fi aliatisal bial'iintirnti");
        }

        private void HandleButtonActionItalian()
        {
            SynthesizeAudio("Molte persone hanno provato ad accendere la televisione.");
        }

        private void HandleButtonActionPortuguese()
        {
            SynthesizeAudio("Ontem deixei meu carregador no carro.");
        }

        private void HandleButtonActionThai()
        {
            // SynthesizeAudio(AssistantAI.SupportedLanguage.Thai, "ฉันไม่แน่ใจว่าเส้นทางที่ดีที่สุดไปสนามบินคืออะไร");
            SynthesizeAudio("C̄hạn mị̀ næ̀cı ẁā s̄ên thāng thī̀ dī thī̀s̄ud pị s̄nām bin khụ̄x xarị");
        }

        private void HandleButtonActionHindi()
        {
            // SynthesizeAudio(AssistantAI.SupportedLanguage.Hindi, "पिछली बार जब मैं यहां आया था तो हमने परिवार के साथ समय बिताया था।");
            SynthesizeAudio("pichhalee baar jab main yahaan aaya tha to hamane parivaar ke saath samay bitaaya tha.");
        }

        private void HandleButtonActionIndonesian()
        {
            SynthesizeAudio("Saya ingin membeli beberapa buku lagi dari toko itu.");
        }

        private void HandleButtonActionTagalog() // Filipino
        {
            SynthesizeAudio("Handa na akong matulog, pwede ba tayong umalis agad?");
        }
    }
}