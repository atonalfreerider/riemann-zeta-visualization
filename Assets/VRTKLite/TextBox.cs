using System.Linq;
using TMPro;
using UnityEngine;

namespace VRTKLite
{
    public class Font
    {
        public readonly TMP_FontAsset Typeface;
        public readonly float Size;
        public readonly Color Color;

        public Font(TMP_FontAsset typeface, float size, Color color)
        {
            Typeface = typeface;
            Size = size;

            // Bug: for TextMesh Pro, colors must be linearized before they are displayed on Windows.
            // See: http://digitalnativestudios.com/forum/index.php?topic=1773.0
            // While Android builds use Gamma color space, no modification is needed.
#if UNITY_ANDROID && !UNITY_EDITOR
            Color = QualitySettings.activeColorSpace == ColorSpace.Linear ? color.linear : color;
#else
            Color = color.linear;
#endif
        }
    }

    public class TextBox : MonoBehaviour
    {
        // Components - Attached in Unity Editor
        public TextMeshPro TextField;

        public TextAlignmentOptions Alignment
        {
            get => TextField.alignment;
            set
            {
                TextField.alignment = value;

                switch (value)
                {
                    case TextAlignmentOptions.Right:
                        TextField.rectTransform.pivot = new Vector2(1, 0.5f);
                        break;
                    case TextAlignmentOptions.Center:
                        TextField.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                        break;
                    case TextAlignmentOptions.TopLeft:
                        TextField.rectTransform.pivot = new Vector2(0, 1);
                        break;
                    case TextAlignmentOptions.Top:
                        TextField.rectTransform.pivot = new Vector2(0.5f, 1);
                        break;
                    default:
                        TextField.rectTransform.pivot = new Vector2(0, 0.5f);
                        break;
                }
            }
        }

        public string Text
        {
            set => TextField.SetText(value);
        }

        #region Font Setters

        public Font Font
        {
            set
            {
                TextField.font = value.Typeface;
                TextField.fontSize = value.Size;
                TextField.color = value.Color;
            }
        }

        public float Size
        {
            set => TextField.fontSize = value;
        }

        public Color Color
        {
            set => TextField.color = value;
        }

        #endregion

        public void SetFixedWithWrap(Vector2 size)
        {
            TextField.autoSizeTextContainer = false;
            TextField.enableWordWrapping = true;
            TextField.rectTransform.sizeDelta = size;
        }

        public static TextBox Create(
            string text,
            TextAlignmentOptions align)
        {
            GameObject textboxPrefab = Instantiate(Resources.Load("TextBox") as GameObject);
            TextBox textBox = textboxPrefab.GetComponent<TextBox>();
            textBox.name = $"TextBox: {text.Split('\n').First()}";

            textBox.Alignment = align;
            textBox.Text = text;

            return textBox;
        }
    }
}