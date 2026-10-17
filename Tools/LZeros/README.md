# Zeros of L(s, (·/163))

`LZeros.cs` computes the zeros on the critical line of the Dirichlet L-function of the character
`χ(n) = (n/163)`, the Kronecker symbol of discriminant −163 (Euler's ring). The results are in
`Assets/Resources/lzeros163.txt` (one zero per line, `index height`), which section Q of the experiments
and the "Primes of Euler's ring" module read.

* `L(½ + it)` is summed directly up to `n = Kq` (`q = 163`, `K ≈ 4|t|/2π`) with an Euler–Maclaurin tail for each
  residue class, in double precision.
* `Z(t) = Re(e^{iθ(t)} L(½ + it))` with `θ(t) = (t/2) log(q/π) + arg Γ(¾ + it/2)` is real because `χ` is odd with root
  number 1. Zeros are its sign changes on a grid of step 0.05, refined by regula falsi. Places where `|Z|` dips
  toward zero without crossing are rescanned finely, which recovers close pairs hidden inside one grid step.
* Checks: `|L|` at the computed zeros is below 1e-12 by mpmath's independent Hurwitz-zeta evaluation, and the
  count below `T` matches `(T/2π) log(qT/2πe)` to within half a zero at T = 100, 500, 1000, 2000 and 6000
  (10 462 zeros below 6000).

Build and run with the .NET SDK bundled with Unity (`E` is the editor's `Data/DotNetSdk`):

```bash
REF=$E/packs/Microsoft.NETCore.App.Ref/8.0.21/ref/net8.0
$E/dotnet $E/sdk/8.0.318/Roslyn/bincore/csc.dll -nologo -optimize+ -target:exe -out:LZeros.dll $(for f in $REF/*.dll; do echo -n "-r:$f "; done) LZeros.cs
$E/dotnet LZeros.dll 6000 lzeros163.txt 0.05
```

It takes about five minutes on eight cores.
