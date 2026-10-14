using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// Data model for Assets/Resources/Narration/script.json (loaded with JsonUtility).

[Serializable]
public class TourScriptData
{
    public string title;
    public TourChapter[] chapters;
}

[Serializable]
public class TourChapter
{
    public string id;
    public string title;
    public string subtitle;
    /// <summary>Actions applied when the chapter starts, after the stage is cleared: what is on screen when it opens.</summary>
    public string[] setup;
    public TourLine[] lines;
}

[Serializable]
public class TourLine
{
    public string id;
    public string text;
    /// <summary>Camera cue name (see <see cref="CameraRig"/>).</summary>
    public string camera;
    /// <summary>Actions applied when the line starts, e.g. "play", "speed:3", "show:tail" (see <see cref="TourDirector"/>).</summary>
    public string[] actions;
    /// <summary>Extra silence after the narration, in seconds.</summary>
    public float pause;
    /// <summary>Small on-screen source/caveat line.</summary>
    public string footnote;
}

[Serializable]
public class WordTiming
{
    public string w;
    public float s;
    public float e;
}

[Serializable]
public class LineTiming
{
    public WordTiming[] words;
    public float duration;
}

public static class TourScriptLoader
{
    public const string ScriptResource = "Narration/script";
    public const string AudioFolder = "Narration/Audio/";
    public const string TimingFolder = "Narration/Timing/";

    public static TourScriptData Load()
    {
        TextAsset asset = Resources.Load<TextAsset>(ScriptResource);
        if (asset == null)
        {
            Debug.LogError($"Tour script not found at Resources/{ScriptResource}.json");
            return new TourScriptData { chapters = Array.Empty<TourChapter>() };
        }

        return JsonUtility.FromJson<TourScriptData>(asset.text);
    }

    /// <summary>Word timings produced by Tools/generate_narration.py (ElevenLabs alignment), or null.</summary>
    public static LineTiming LoadTiming(string lineId)
    {
        TextAsset asset = Resources.Load<TextAsset>(TimingFolder + lineId);
        if (asset == null) return null;
        LineTiming timing = JsonUtility.FromJson<LineTiming>(asset.text);
        if (timing?.words == null || timing.words.Length == 0) return null;
        if (timing.duration <= 0f) timing.duration = timing.words[^1].e;
        return timing;
    }

    public static AudioClip LoadAudio(string lineId)
    {
        return Resources.Load<AudioClip>(AudioFolder + lineId);
    }

    /// <summary>
    /// Fallback timing used before narration audio exists: ~2.7 words per second,
    /// longer words take longer, punctuation adds a beat.
    /// </summary>
    public static LineTiming Estimate(string text, float speed = 1f)
    {
        List<WordTiming> words = new();
        float t = 0.15f;
        foreach (string raw in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            float length = (0.17f + 0.032f * raw.Length) / speed;
            WordTiming word = new() { w = raw, s = t, e = t + length };
            words.Add(word);
            t += length;

            char last = raw[^1];
            if (last is '.' or '!' or '?') t += 0.5f / speed;
            else if (last is ',' or ';' or ':') t += 0.22f / speed;
        }

        return new LineTiming { words = words.ToArray(), duration = t + 0.3f };
    }

    /// <summary>Plain caption text with the word at <paramref name="index"/> wrapped for highlighting.</summary>
    public static string Highlight(WordTiming[] words, int pageStart, int pageEnd, int index, string highlightHex, string dimHex)
    {
        StringBuilder sb = new();
        for (int i = pageStart; i < pageEnd; i++)
        {
            if (i > pageStart) sb.Append(' ');
            if (i == index)
            {
                sb.Append("<color=#").Append(highlightHex).Append("><b>").Append(words[i].w).Append("</b></color>");
            }
            else if (i < index)
            {
                sb.Append(words[i].w);
            }
            else
            {
                sb.Append("<color=#").Append(dimHex).Append('>').Append(words[i].w).Append("</color>");
            }
        }

        return sb.ToString();
    }
}
