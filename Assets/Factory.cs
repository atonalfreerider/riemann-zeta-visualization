using UnityEngine;

public class Factory : MonoBehaviour
{
    public Shader mainShader;
    public static Material mainMat;

    void Awake()
    {
        mainMat = new Material(mainShader);
    }

    public static LineRenderer NewLine(string lineName, float W, Color color)
    {
        LineRenderer newLine = new GameObject(lineName).AddComponent<LineRenderer>();
        newLine.material = mainMat;
        newLine.startColor = color;
        newLine.endColor = color;
        newLine.startWidth = W;
        newLine.endWidth = W;
        return newLine;
    }

    public static class Viridis
    {
        // https://cran.r-project.org/web/packages/viridis/vignettes/intro-to-viridis.html
        static readonly Color32[] viridis = {
            new(253, 231, 37, 255),
            new(220, 227, 25, 255),
            new(184, 222, 41, 255),
            new(149, 216, 64, 255),
            new(115, 208, 85, 255),
            new(85, 198, 103, 255),
            new(60, 187, 117, 255),
            new(41, 175, 127, 255),
            new(32, 163, 135, 255),
            new(31, 150, 139, 255),
            new(35, 138, 141, 255),
            new(40, 125, 142, 255),
            new(45, 112, 142, 255),
            new(51, 99, 141, 255),
            new(57, 86, 140, 255),
            new(64, 71, 136, 255),
            new(69, 55, 129, 255),
            new(72, 38, 119, 255),
            new(72, 21, 103, 255),
            new(68, 1, 84, 255)
        };
        
        public static Color ViridisColor(float prct)
        {
            return viridis[Mathf.RoundToInt(Mathf.Min(prct, 1) * (viridis.Length - 1))];
        }
    }
}