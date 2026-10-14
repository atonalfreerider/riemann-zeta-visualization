using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Bakes the full master wave (−Σ cos(γ·log n) over every zero in Resources/zetazeros.txt) into Resources/MasterWave, so
/// the home view, the outro and explore mode show it at once instead of computing 10,000 zeros × 160,000 samples at run
/// time. Re-run after changing the zeros table or the sampling in SacksSpiral.FullWaveLogN:
///   unity run . -- -executeMethod MasterWaveBake.Bake        (or Zeta ▸ Bake Master Wave)
/// </summary>
public static class MasterWaveBake
{
    const float NMax = 1300f;
    static readonly int[] SampleCounts = { 160000, 60000 }; // desktop, and the Quest's budget

    [MenuItem("Zeta/Bake Master Wave")]
    public static void Bake()
    {
        double[] zeros = File.ReadAllLines("Assets/Resources/zetazeros.txt")
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => Convert.ToDouble(line.Split(' ')[1], CultureInfo.InvariantCulture))
            .ToArray();
        Directory.CreateDirectory("Assets/Resources/MasterWave");
        foreach (int samples in SampleCounts)
        {
            DateTime started = DateTime.Now;
            float[] sums = new float[samples];
            Parallel.For(0, samples, i =>
            {
                double logN = SacksSpiral.FullWaveLogN(i, samples, NMax), s = 0;
                for (int z = 0; z < zeros.Length; z++) s -= Math.Cos(zeros[z] * logN);
                sums[i] = (float)s;
            });

            byte[] bytes = new byte[samples * 4];
            Buffer.BlockCopy(sums, 0, bytes, 0, bytes.Length);
            string path = "Assets/Resources/" + SacksSpiral.BakedFullWaveName(zeros.Length, samples, NMax) + ".bytes";
            File.WriteAllBytes(path, bytes);
            Debug.Log($"MasterWaveBake: {zeros.Length} zeros × {samples} samples → {path} in {(DateTime.Now - started).TotalSeconds:0.0} s");
        }

        AssetDatabase.Refresh();
        if (Application.isBatchMode) EditorApplication.Exit(0);
    }
}
