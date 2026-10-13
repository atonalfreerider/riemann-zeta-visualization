using TMPro;
using UnityEngine;

public static class RenderedEquations
{
    public static TextBox zetaZeroCounter;

    public static GameObject DrawPrimeEquation()
    {
        GameObject equation = new GameObject("prime equation");
        float dropD = 1.5f;
        float shiftD = 2.6f;

        TextBox numerator = TextBox.Create("cos(γ · log x) + 2γ · sin(γ · log x)", 3f);
        numerator.transform.SetParent(equation.transform, false);
        numerator.Alignment = TextAlignmentOptions.Center;
        numerator.transform.Translate(Vector3.up * .2f);
        numerator.Color = Color.gray;

        LineRenderer divLine = Factory.NewLine("div", .01f, Color.gray);
        divLine.transform.SetParent(equation.transform);
        divLine.positionCount = 2;
        divLine.SetPositions(new[]
        {
            new Vector3(-2.1f + shiftD, -dropD, 0),
            new Vector3(2.1f + shiftD, -dropD, 0)
        });

        TextBox denomenator = TextBox.Create("1 + 4γ²", 3f);
        denomenator.transform.SetParent(equation.transform, false);
        denomenator.Alignment = TextAlignmentOptions.Center;
        denomenator.transform.Translate(Vector3.down * .2f);
        denomenator.Color = Color.gray;

        TextBox sigma = TextBox.Create("Σ", 10f);
        sigma.transform.SetParent(equation.transform, false);
        sigma.Alignment = TextAlignmentOptions.Center;
        sigma.transform.Translate(Vector3.left * 2.7f);
        sigma.Color = Color.gray;

        TextBox gamma = TextBox.Create("γ", 3f);
        gamma.transform.SetParent(equation.transform, false);
        gamma.Alignment = TextAlignmentOptions.Center;
        gamma.transform.Translate(Vector3.left * 2.7f);
        gamma.transform.Translate(Vector3.down * .45f);
        gamma.Color = Color.gray;

        TextBox fourRoot = TextBox.Create("ψ(x) ≈ x − ln 2π − 4√x", 3f);
        fourRoot.transform.SetParent(equation.transform, false);
        fourRoot.Alignment = TextAlignmentOptions.Right;
        fourRoot.transform.Translate(Vector3.left * 3.1f);
        fourRoot.Color = Color.gray;

        zetaZeroCounter = TextBox.Create("", 3f);
        zetaZeroCounter.transform.SetParent(equation.transform, false);
        zetaZeroCounter.Alignment = TextAlignmentOptions.Center;
        zetaZeroCounter.transform.Translate(Vector3.left * 2.7f);
        zetaZeroCounter.transform.Translate(Vector3.up * .5f);
        zetaZeroCounter.Color = Factory.Palette.Zero;

        equation.transform.Translate(Vector3.down * dropD);
        equation.transform.Translate(Vector3.right * shiftD);
        return equation;
    }

    public static GameObject DrawZetaEquation()
    {
        GameObject equation = new GameObject("zeta equation");
        float dropD = 1.5f;
        float shiftD = 3f;

        TextBox numerator = TextBox.Create("ζ(s) = ", 3f);
        numerator.transform.SetParent(equation.transform, false);
        numerator.Alignment = TextAlignmentOptions.Center;
        numerator.Color = Color.white;

        TextBox sigma = TextBox.Create("Σ", 10f);
        sigma.transform.SetParent(equation.transform, false);
        sigma.Alignment = TextAlignmentOptions.Center;
        sigma.transform.Translate(Vector3.right * .7f);
        sigma.Color = Color.white;

        TextBox n = TextBox.Create("n", 3f);
        n.transform.SetParent(equation.transform, false);
        n.Alignment = TextAlignmentOptions.Center;
        n.transform.Translate(Vector3.right * .7f);
        n.transform.Translate(Vector3.down * .45f);
        n.Color = Color.white;

        TextBox term = TextBox.Create("1 / n^s   +  tail", 3f);
        term.transform.SetParent(equation.transform, false);
        term.Alignment = TextAlignmentOptions.Left;
        term.transform.Translate(Vector3.right * 1.1f);
        term.Color = Color.white;

        TextBox s = TextBox.Create("s = ½ + i·t", 2.2f);
        s.transform.SetParent(equation.transform, false);
        s.Alignment = TextAlignmentOptions.Center;
        s.transform.Translate(Vector3.down * .9f + Vector3.right * 1.2f);
        s.Color = Color.gray;

        equation.transform.Translate(Vector3.up * dropD);
        equation.transform.Translate(Vector3.left * shiftD);
        return equation;
    }
}
