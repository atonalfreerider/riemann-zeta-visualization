using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Every glyph in the VR interface, drawn at start-up from a few signed-distance shapes (white with alpha, tinted by
/// the UI). Drawing them in code keeps the project free of binary assets and lets <see cref="DumpSheet"/> render a
/// contact sheet for review.
/// </summary>
public static class VRIcons
{
    public enum Id
    {
        Play, Pause, NextLine, PrevLine, NextChapter, PrevChapter, Replay, Menu, Close, Gear, Home, Target, Pin,
        Eye, Moon, ZoomIn, ZoomOut, RotateLeft, RotateRight, TiltUp, TiltDown, Closer, Farther, Up, Down, Grab, Move,
        Captions, Volume, Mute, List, Cube, Skeleton, Reset,
        Chain, Tail, Zeros, Trail, Waves, Stairs, Strip, Sacks, Saddle, Equations, Grid, Dial, Zeno, Stage, Machine, Key,
        Speed, Check,
        // the experiments tab
        Flask, Curves, Contours, Probe, Mirror, Lattice, Ideals, Chords, Comb, Pinch, Torus, Legendre, Fingerprint, Clock, Pair, Sieve, Twist
    }

    const int Size = 96;
    static readonly Dictionary<Id, Sprite> cache = new();
    static Sprite roundedBox;
    static Sprite circle;

    public static Sprite Get(Id id)
    {
        if (cache.TryGetValue(id, out Sprite sprite)) return sprite;
        Canvas2D c = new(Size);
        Draw(id, c);
        sprite = c.ToSprite(id.ToString());
        cache[id] = sprite;
        return sprite;
    }

    /// <summary>9-sliced rounded rectangle for button and panel backgrounds.</summary>
    public static Sprite RoundedBox()
    {
        if (roundedBox != null) return roundedBox;
        const int n = 64;
        Canvas2D c = new(n);
        c.Fill(Shape.Box(Vector2.zero, new Vector2(1f, 1f), 0.75f, 0f));
        Texture2D tex = c.ToTexture();
        roundedBox = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(24, 24, 24, 24));
        roundedBox.name = "VRRoundedBox";
        return roundedBox;
    }

    public static Sprite Circle()
    {
        if (circle != null) return circle;
        const int n = 64;
        Canvas2D c = new(n);
        c.Fill(Shape.Circle(Vector2.zero, 0.98f));
        circle = Sprite.Create(c.ToTexture(), new Rect(0, 0, n, n), new Vector2(.5f, .5f), 100f);
        circle.name = "VRCircle";
        return circle;
    }

    // ---------------------------------------------------------------------------------------------

    static Vector2 V(float x, float y) => new(x, y);

    static void Draw(Id id, Canvas2D c)
    {
        const float W = 0.17f; // heavy stroke
        switch (id)
        {
            case Id.Play:
                c.Fill(Shape.Tri(V(-.42f, -.7f), V(-.42f, .7f), V(.72f, 0f)));
                break;
            case Id.Pause:
                c.Fill(Shape.Box(V(-.33f, 0), V(.17f, .66f), .08f));
                c.Fill(Shape.Box(V(.33f, 0), V(.17f, .66f), .08f));
                break;
            case Id.NextLine:
                c.Fill(Shape.Tri(V(-.78f, -.62f), V(-.78f, .62f), V(.28f, 0f)));
                c.Fill(Shape.Box(V(.58f, 0), V(.13f, .64f), .06f));
                break;
            case Id.PrevLine:
                c.Fill(Shape.Tri(V(.78f, -.62f), V(.78f, .62f), V(-.28f, 0f)));
                c.Fill(Shape.Box(V(-.58f, 0), V(.13f, .64f), .06f));
                break;
            case Id.NextChapter:
                c.Fill(Shape.Tri(V(-.88f, -.55f), V(-.88f, .55f), V(-.1f, 0f)));
                c.Fill(Shape.Tri(V(-.12f, -.55f), V(-.12f, .55f), V(.66f, 0f)));
                c.Fill(Shape.Box(V(.82f, 0), V(.1f, .6f), .05f));
                break;
            case Id.PrevChapter:
                c.Fill(Shape.Tri(V(.88f, -.55f), V(.88f, .55f), V(.1f, 0f)));
                c.Fill(Shape.Tri(V(.12f, -.55f), V(.12f, .55f), V(-.66f, 0f)));
                c.Fill(Shape.Box(V(-.82f, 0), V(.1f, .6f), .05f));
                break;
            case Id.Replay:
                ArrowArc(c, V(0, 0), .58f, 110f, 400f, W, true);
                c.Fill(Shape.Tri(V(-.14f, -.2f), V(-.14f, .2f), V(.2f, 0f)));
                break;
            case Id.Menu:
                for (int i = -1; i <= 1; i++) c.Fill(Shape.Segment(V(-.62f, i * .5f), V(.62f, i * .5f), .2f));
                break;
            case Id.Close:
                c.Fill(Shape.Segment(V(-.58f, -.58f), V(.58f, .58f), .22f));
                c.Fill(Shape.Segment(V(-.58f, .58f), V(.58f, -.58f), .22f));
                break;
            case Id.Gear:
                c.Fill(Shape.Circle(V(0, 0), .6f));
                for (int i = 0; i < 8; i++) c.Fill(Shape.Box(V(0, 0) + Dir(i * 45f) * .72f, V(.14f, .17f), .04f, i * 45f));
                c.Erase(Shape.Circle(V(0, 0), .25f));
                break;
            case Id.Home:
                c.Fill(Shape.Tri(V(-.95f, .1f), V(0f, .9f), V(.95f, .1f)));
                c.Fill(Shape.Box(V(0, -.36f), V(.62f, .42f), .04f));
                c.Erase(Shape.Box(V(0, -.55f), V(.17f, .3f), .03f));
                break;
            case Id.Target:
                c.Fill(Shape.Ring(V(0, 0), .62f, .15f));
                c.Fill(Shape.Circle(V(0, 0), .2f));
                foreach (float a in new[] { 0f, 90f, 180f, 270f }) c.Fill(Shape.Segment(Dir(a) * .62f, Dir(a) * .95f, .15f));
                break;
            case Id.Pin:
                c.Fill(Shape.Circle(V(0, .22f), .55f));
                c.Fill(Shape.Tri(V(-.5f, .0f), V(.5f, .0f), V(0f, -.95f)));
                c.Erase(Shape.Circle(V(0, .22f), .22f));
                break;
            case Id.Eye:
                c.Fill(Shape.Lens(.95f, .56f, W * .85f, true));
                c.Fill(Shape.Circle(V(0, 0), .27f));
                break;
            case Id.Moon:
                c.Fill(Shape.Circle(V(0, 0), .78f));
                c.Erase(Shape.Circle(V(.42f, .3f), .66f));
                break;
            case Id.ZoomIn:
            case Id.ZoomOut:
                c.Fill(Shape.Ring(V(-.15f, .15f), .52f, W));
                c.Fill(Shape.Segment(V(.24f, -.24f), V(.78f, -.78f), .22f));
                c.Fill(Shape.Segment(V(-.38f, .15f), V(.08f, .15f), .13f));
                if (id == Id.ZoomIn) c.Fill(Shape.Segment(V(-.15f, -.08f), V(-.15f, .38f), .13f));
                break;
            case Id.RotateLeft:
                ArrowArc(c, V(0, 0), .62f, 40f, 330f, W, false);
                break;
            case Id.RotateRight:
                ArrowArc(c, V(0, 0), .62f, 40f, 330f, W, true);
                break;
            case Id.TiltUp: // swing the top of the model away from you
                c.Fill(Shape.Arc(V(-.5f, 0), .9f, W, -55f, 55f));
                Head(c, V(-.5f, 0) + Dir(55f) * .9f, 55f + 90f, .5f);
                break;
            case Id.TiltDown: // swing the top of the model toward you
                c.Fill(Shape.Arc(V(-.5f, 0), .9f, W, -55f, 55f));
                Head(c, V(-.5f, 0) + Dir(-55f) * .9f, -55f - 90f, .5f);
                break;
            case Id.Closer:
                c.Fill(Shape.Circle(V(0, 0), .16f));
                Arrow(c, V(-.95f, 0), V(-.3f, 0), W * .9f, .34f);
                Arrow(c, V(.95f, 0), V(.3f, 0), W * .9f, .34f);
                break;
            case Id.Farther:
                c.Fill(Shape.Circle(V(0, 0), .16f));
                Arrow(c, V(-.3f, 0), V(-.95f, 0), W * .9f, .34f);
                Arrow(c, V(.3f, 0), V(.95f, 0), W * .9f, .34f);
                break;
            case Id.Up:
                Arrow(c, V(0, -.8f), V(0, .8f), W, .42f);
                break;
            case Id.Down:
                Arrow(c, V(0, .8f), V(0, -.8f), W, .42f);
                break;
            case Id.Move:
                c.Fill(Shape.Circle(V(0, 0), .13f));
                foreach (float a in new[] { 0f, 90f, 180f, 270f }) Arrow(c, Dir(a) * .18f, Dir(a) * .88f, W * .8f, .36f);
                break;
            case Id.Grab:
                c.Fill(Shape.Box(V(0, -.3f), V(.46f, .4f), .2f));
                c.Fill(Shape.Segment(V(-.36f, -.05f), V(-.36f, .5f), .21f));
                c.Fill(Shape.Segment(V(-.12f, -.05f), V(-.12f, .68f), .21f));
                c.Fill(Shape.Segment(V(.12f, -.05f), V(.12f, .72f), .21f));
                c.Fill(Shape.Segment(V(.36f, -.05f), V(.36f, .56f), .21f));
                c.Fill(Shape.Segment(V(-.42f, -.38f), V(-.8f, -.02f), .22f));
                break;
            case Id.Skeleton:
                foreach (float x in new[] { -.4f, -.14f, .12f, .38f }) { c.Fill(Shape.Segment(V(x, -.1f), V(x, .62f), .07f)); c.Fill(Shape.Circle(V(x, .62f), .1f)); c.Fill(Shape.Circle(V(x, .25f), .08f)); }
                c.Fill(Shape.Segment(V(-.45f, -.5f), V(.5f, -.5f), .07f));
                c.Fill(Shape.Segment(V(-.4f, -.1f), V(-.45f, -.5f), .07f));
                c.Fill(Shape.Segment(V(.38f, -.1f), V(.5f, -.5f), .07f));
                c.Fill(Shape.Segment(V(-.45f, -.5f), V(-.8f, -.15f), .07f));
                c.Fill(Shape.Circle(V(-.8f, -.15f), .1f));
                break;
            case Id.Reset:
                ArrowArc(c, V(0, 0), .6f, 90f, 400f, W, true);
                c.Fill(Shape.Circle(V(0, 0), .16f));
                break;
            case Id.Captions:
                c.Fill(Shape.Box(V(0, 0), V(.92f, .66f), .22f));
                c.Erase(Shape.Box(V(0, 0), V(.92f - .15f, .66f - .15f), .12f));
                c.Fill(Shape.Arc(V(-.33f, 0), .22f, .12f, 40f, 320f));
                c.Fill(Shape.Arc(V(.3f, 0), .22f, .12f, 40f, 320f));
                break;
            case Id.Volume:
                c.Fill(Shape.Box(V(-.66f, 0), V(.2f, .26f), .03f));
                c.Fill(Shape.Tri(V(-.5f, .28f), V(-.5f, -.28f), V(.0f, -.7f)));
                c.Fill(Shape.Tri(V(-.5f, .28f), V(.0f, .7f), V(.0f, -.7f)));
                c.Fill(Shape.Arc(V(-.05f, 0), .45f, .13f, -45f, 45f));
                c.Fill(Shape.Arc(V(-.05f, 0), .78f, .13f, -45f, 45f));
                break;
            case Id.Mute:
                c.Fill(Shape.Box(V(-.66f, 0), V(.2f, .26f), .03f));
                c.Fill(Shape.Tri(V(-.5f, .28f), V(-.5f, -.28f), V(.0f, -.7f)));
                c.Fill(Shape.Tri(V(-.5f, .28f), V(.0f, .7f), V(.0f, -.7f)));
                c.Fill(Shape.Segment(V(.2f, -.3f), V(.8f, .3f), .15f));
                c.Fill(Shape.Segment(V(.2f, .3f), V(.8f, -.3f), .15f));
                break;
            case Id.List:
                for (int i = -1; i <= 1; i++)
                {
                    c.Fill(Shape.Circle(V(-.7f, i * .52f), .13f));
                    c.Fill(Shape.Segment(V(-.35f, i * .52f), V(.8f, i * .52f), .17f));
                }
                break;
            case Id.Cube:
            {
                Vector2[] hex = new Vector2[6];
                for (int i = 0; i < 6; i++) hex[i] = Dir(90f + i * 60f) * .9f;
                for (int i = 0; i < 6; i++) c.Fill(Shape.Segment(hex[i], hex[(i + 1) % 6], W * .85f));
                c.Fill(Shape.Segment(V(0, 0), hex[1], W * .8f));
                c.Fill(Shape.Segment(V(0, 0), hex[3], W * .8f));
                c.Fill(Shape.Segment(V(0, 0), hex[5], W * .8f));
                break;
            }

            // ---- the mathematics -------------------------------------------------------------------
            case Id.Chain: // the endless chain of arrows curling toward its centre
            {
                Vector2 prev = Spiral(0f);
                for (int i = 1; i <= 36; i++)
                {
                    Vector2 p = Spiral(i / 36f);
                    c.Fill(Shape.Segment(prev, p, .13f));
                    prev = p;
                }

                c.Fill(Shape.Circle(Spiral(1f), .17f));
                c.Fill(Shape.Circle(V(0, 0), .09f));
                break;
            }
            case Id.Tail: // the orange arrow that jumps straight to the centre
                Arrow(c, V(-.85f, -.7f), V(.55f, .45f), W * 1.05f, .5f);
                c.Fill(Shape.Circle(V(.62f, .52f), .13f));
                break;
            case Id.Zeros:
                c.Fill(Shape.Segment(V(0, -.9f), V(0, .9f), .1f));
                foreach (float y in new[] { -.62f, -.12f, .26f, .6f }) c.Fill(Shape.Circle(V(0, y), .2f));
                c.Fill(Shape.Segment(V(.2f, -.62f), V(.6f, -.62f), .08f));
                c.Fill(Shape.Segment(V(.2f, -.12f), V(.5f, -.12f), .08f));
                c.Fill(Shape.Segment(V(.2f, .26f), V(.56f, .26f), .08f));
                break;
            case Id.Trail: // the helix the tip draws
            {
                Vector2 prev = V(-.9f, -.5f);
                for (int i = 1; i <= 40; i++)
                {
                    float t = i / 40f;
                    Vector2 p = V(Mathf.Lerp(-.9f, .9f, t), -.62f + t * 1.2f + Mathf.Sin(t * Mathf.PI * 4f) * .22f);
                    c.Fill(Shape.Segment(prev, p, .13f));
                    prev = p;
                }

                c.Fill(Shape.Circle(prev, .17f));
                break;
            }
            case Id.Waves:
                Wave(c, 0f, .45f, 2f, .15f);
                Wave(c, -.28f, .22f, 3f, .1f);
                break;
            case Id.Stairs:
            {
                Vector2[] steps = { V(-.9f, -.7f), V(-.5f, -.7f), V(-.5f, -.3f), V(-.1f, -.3f), V(-.1f, .1f), V(.3f, .1f), V(.3f, .5f), V(.7f, .5f), V(.7f, .85f) };
                for (int i = 0; i + 1 < steps.Length; i++) c.Fill(Shape.Segment(steps[i], steps[i + 1], .15f));
                c.Fill(Shape.Segment(V(-.9f, -.85f), V(.9f, .75f), .06f));
                break;
            }
            case Id.Strip:
                c.Fill(Shape.Segment(V(-.62f, -.88f), V(-.62f, .88f), .12f));
                c.Fill(Shape.Segment(V(.62f, -.88f), V(.62f, .88f), .12f));
                for (int i = 0; i < 4; i++) c.Fill(Shape.Segment(V(0, -.8f + i * .46f), V(0, -.55f + i * .46f), .1f));
                c.Fill(Shape.Circle(V(0, .1f), .17f));
                c.Fill(Shape.Box(V(.8f, 0), V(.1f, .7f), 0f));
                break;
            case Id.Sacks: // dots along the Sacks spiral
                for (int n = 1; n <= 26; n++)
                {
                    float r = Mathf.Sqrt(n) * .17f;
                    float a = Mathf.Sqrt(n) * Mathf.PI * 2f;
                    bool prime = n == 2 || n == 3 || n == 5 || n == 7 || n == 11 || n == 13 || n == 17 || n == 19 || n == 23;
                    c.Fill(Shape.Circle(V(Mathf.Cos(a) * r, Mathf.Sin(a) * r), prime ? .15f : .075f));
                }

                break;
            case Id.Saddle:
            {
                Vector2 prev = V(-.9f, .45f);
                for (int i = 1; i <= 24; i++)
                {
                    float x = Mathf.Lerp(-.9f, .9f, i / 24f);
                    Vector2 p = V(x, .75f * x * x - .35f);
                    c.Fill(Shape.Segment(prev, p, .14f));
                    prev = p;
                }

                Vector2 q = V(-.45f, .15f);
                for (int i = 1; i <= 16; i++)
                {
                    float x = Mathf.Lerp(-.45f, .45f, i / 16f);
                    Vector2 p = V(x + 0.0f, -2.2f * x * x + .65f);
                    c.Fill(Shape.Segment(q, p, .14f));
                    q = p;
                }

                break;
            }
            case Id.Equations:
            {
                Vector2[] sigma = { V(.55f, .72f), V(-.5f, .72f), V(.12f, 0f), V(-.5f, -.72f), V(.55f, -.72f) };
                for (int i = 0; i + 1 < sigma.Length; i++) c.Fill(Shape.Segment(sigma[i], sigma[i + 1], .17f));
                c.Fill(Shape.Segment(V(.55f, .72f), V(.55f, .45f), .17f));
                c.Fill(Shape.Segment(V(.55f, -.72f), V(.55f, -.45f), .17f));
                break;
            }
            case Id.Grid:
                foreach (float v in new[] { -.36f, .36f })
                {
                    c.Fill(Shape.Segment(V(v, -.85f), V(v, .85f), .13f));
                    c.Fill(Shape.Segment(V(-.85f, v), V(.85f, v), .13f));
                }

                break;
            case Id.Dial:
                c.Fill(Shape.Ring(V(0, 0), .72f, .13f));
                c.Fill(Shape.Segment(V(0, 0), V(.34f, .44f), .14f));
                c.Fill(Shape.Circle(V(0, 0), .15f));
                for (int i = 0; i < 8; i++) c.Fill(Shape.Segment(Dir(i * 45f + 15f) * .55f, Dir(i * 45f + 15f) * .62f, .07f));
                break;
            case Id.Zeno: // halving steps
            {
                float x = -.9f;
                float w = .9f;
                for (int i = 0; i < 5; i++)
                {
                    c.Fill(Shape.Box(V(x + w * .5f, -.6f + i * .0f), V(w * .5f - .035f, .13f), .04f));
                    x += w;
                    w *= .5f;
                }

                c.Fill(Shape.Segment(V(.92f, -.9f), V(.92f, .9f), .09f));
                break;
            }
            case Id.Stage: // the floor and its pole
                c.Fill(Shape.Segment(V(-.85f, -.55f), V(-.2f, -.9f), .1f));
                c.Fill(Shape.Segment(V(-.2f, -.9f), V(.85f, -.55f), .1f));
                c.Fill(Shape.Segment(V(.85f, -.55f), V(.2f, -.25f), .1f));
                c.Fill(Shape.Segment(V(.2f, -.25f), V(-.85f, -.55f), .1f));
                c.Fill(Shape.Segment(V(0, -.55f), V(0, .85f), .13f));
                c.Fill(Shape.Circle(V(0, .62f), .2f));
                break;
            case Id.Machine:
                c.Fill(Shape.Circle(V(-.28f, .12f), .46f));
                for (int i = 0; i < 8; i++) c.Fill(Shape.Box(V(-.28f, .12f) + Dir(i * 45f) * .56f, V(.1f, .13f), .03f, i * 45f));
                c.Erase(Shape.Circle(V(-.28f, .12f), .17f));
                c.Fill(Shape.Circle(V(.52f, -.42f), .3f));
                for (int i = 0; i < 6; i++) c.Fill(Shape.Box(V(.52f, -.42f) + Dir(i * 60f + 15f) * .37f, V(.075f, .1f), .02f, i * 60f + 15f));
                c.Erase(Shape.Circle(V(.52f, -.42f), .1f));
                break;
            case Id.Key:
                c.Fill(Shape.Ring(V(-.5f, .0f), .36f, .2f));
                c.Fill(Shape.Segment(V(-.15f, 0f), V(.85f, 0f), .17f));
                c.Fill(Shape.Segment(V(.55f, 0f), V(.55f, -.38f), .17f));
                c.Fill(Shape.Segment(V(.82f, 0f), V(.82f, -.3f), .17f));
                break;
            case Id.Speed:
                c.Fill(Shape.Arc(V(0, -.2f), .75f, W, 20f, 160f));
                c.Fill(Shape.Segment(V(0, -.2f), V(.38f, .38f), .15f));
                c.Fill(Shape.Circle(V(0, -.2f), .15f));
                break;
            case Id.Check:
                c.Fill(Shape.Segment(V(-.7f, -.02f), V(-.25f, -.5f), .24f));
                c.Fill(Shape.Segment(V(-.25f, -.5f), V(.72f, .55f), .24f));
                break;

            // ---- the experiments ------------------------------------------------------------------
            case Id.Flask:
                c.Fill(Shape.Box(V(0, .52f), V(.2f, .36f), .06f));
                c.Fill(Shape.Box(V(0, .88f), V(.36f, .07f), .05f));
                c.Fill(Shape.Tri(V(-.2f, .3f), V(.2f, .3f), V(.86f, -.8f)));
                c.Fill(Shape.Tri(V(-.2f, .3f), V(.86f, -.8f), V(-.86f, -.8f)));
                c.Erase(Shape.Circle(V(-.18f, -.45f), .14f));
                c.Erase(Shape.Circle(V(.2f, -.6f), .1f));
                c.Erase(Shape.Circle(V(.02f, -.22f), .08f));
                break;
            case Id.Curves: // a family of cancellation curves
                foreach (float shift in new[] { -.55f, 0f, .55f })
                {
                    bool have = false;
                    Vector2 prev = V(0, 0);
                    for (int i = 0; i <= 40; i++)
                    {
                        float x = Mathf.Lerp(-.95f, .95f, i / 40f);
                        float y = 1.25f * (x - shift) * (x - shift) - .75f;
                        if (y > .95f) { have = false; continue; }
                        Vector2 p = V(x, y);
                        if (have) c.Fill(Shape.Segment(prev, p, .12f));
                        prev = p;
                        have = true;
                    }
                }

                break;
            case Id.Contours: // nested level lines
                c.Fill(Shape.Ring(V(0, 0), .9f, .1f));
                c.Fill(Shape.Ring(V(.05f, .04f), .6f, .1f));
                c.Fill(Shape.Ring(V(.1f, .08f), .3f, .1f));
                c.Fill(Shape.Circle(V(.12f, .1f), .07f));
                break;
            case Id.Probe: // a needle touching a bent surface
                c.Fill(Shape.Arc(V(0, -1.25f), 1.2f, .14f, 52f, 128f));
                c.Fill(Shape.Segment(V(0, .88f), V(0, .1f), .12f));
                c.Fill(Shape.Circle(V(0, -.05f), .17f));
                c.Fill(Shape.Segment(V(-.5f, .88f), V(.5f, .88f), .11f));
                break;
            case Id.Mirror: // a shape and its reflection
                c.Fill(Shape.Segment(V(0, -.92f), V(0, .92f), .1f));
                c.Fill(Shape.Tri(V(-.88f, -.62f), V(-.28f, -.62f), V(-.88f, .62f)));
                c.Fill(Shape.Tri(V(.88f, -.62f), V(.28f, -.62f), V(.88f, .62f)));
                break;
            case Id.Lattice: // whole-number points under a circle
                foreach (float x in new[] { -.65f, -.22f, .22f, .65f })
                foreach (float y in new[] { -.65f, -.22f, .22f, .65f })
                {
                    c.Fill(Shape.Circle(V(x, y), .095f));
                }

                c.Fill(Shape.Arc(V(-.85f, -.85f), 1.55f, .08f, 0f, 90f));
                break;
            case Id.Ideals: // the hexagonal lattice of a ring of integers
                c.Fill(Shape.Circle(V(0, 0), .19f));
                for (int i = 0; i < 6; i++)
                {
                    Vector2 corner = Dir(i * 60f) * .66f;
                    c.Fill(Shape.Segment(V(0, 0), corner, .07f));
                    c.Fill(Shape.Segment(corner, Dir((i + 1) * 60f) * .66f, .07f));
                    c.Fill(Shape.Circle(corner, .15f));
                }

                break;
            case Id.Chords: // chords at right angles to a mirror line
                c.Fill(Shape.Segment(V(-.88f, -.88f), V(.88f, .88f), .08f));
                foreach (float t in new[] { -.5f, 0f, .5f })
                {
                    float half = .5f - Mathf.Abs(t) * .35f;
                    Vector2 mid = V(t, t);
                    Vector2 d = V(1f, -1f).normalized * half;
                    c.Fill(Shape.Segment(mid - d, mid + d, .1f));
                    c.Fill(Shape.Circle(mid - d, .12f));
                    c.Fill(Shape.Circle(mid + d, .12f));
                }

                break;
            case Id.Comb:
            {
                float[] teeth = { .35f, .72f, .52f, .88f, .42f, .66f, .3f };
                c.Fill(Shape.Segment(V(-.88f, -.5f), V(.88f, -.5f), .17f));
                for (int i = 0; i < teeth.Length; i++)
                {
                    float x = -.75f + i * .25f;
                    c.Fill(Shape.Segment(V(x, -.5f), V(x, -.5f + teeth[i] * 1.3f), .1f));
                }

                break;
            }
            case Id.Pinch: // two curves squeezing a point
                c.Fill(Shape.Arc(V(-1.05f, 0), .82f, .14f, -52f, 52f));
                c.Fill(Shape.Arc(V(1.05f, 0), .82f, .14f, 128f, 232f));
                c.Fill(Shape.Circle(V(0, 0), .17f));
                break;
            case Id.Torus:
                Ellipse(c, V(0, 0), .92f, .6f, .12f);
                Ellipse(c, V(0, .02f), .4f, .2f, .1f);
                break;
            case Id.Legendre: // ( a over p )
                c.Fill(Shape.Arc(V(.34f, 0), 1.1f, .13f, 150f, 210f));
                c.Fill(Shape.Arc(V(-.34f, 0), 1.1f, .13f, -30f, 30f));
                c.Fill(Shape.Circle(V(0, .4f), .17f));
                c.Fill(Shape.Segment(V(-.34f, 0), V(.34f, 0), .1f));
                c.Fill(Shape.Circle(V(0, -.4f), .17f));
                break;
            case Id.Fingerprint:
                foreach (float r in new[] { .22f, .42f, .62f, .82f })
                {
                    c.Fill(Shape.Arc(V(0, -.18f), r, .08f, 18f, 162f));
                }

                foreach (float r in new[] { .32f, .52f, .72f })
                {
                    c.Fill(Shape.Arc(V(0, -.18f), r, .08f, 205f, 335f));
                }

                c.Fill(Shape.Segment(V(0, -.18f), V(0, -.55f), .08f));
                break;
            case Id.Clock:
                c.Fill(Shape.Ring(V(0, 0), .8f, .12f));
                for (int i = 0; i < 12; i++)
                {
                    float inner = i % 3 == 0 ? .5f : .58f;
                    c.Fill(Shape.Segment(Dir(i * 30f) * inner, Dir(i * 30f) * .64f, i % 3 == 0 ? .1f : .06f));
                }

                c.Fill(Shape.Segment(V(0, 0), V(0, .46f), .12f));
                c.Fill(Shape.Segment(V(0, 0), Dir(-35f) * .32f, .12f));
                c.Fill(Shape.Circle(V(0, 0), .1f));
                break;
            case Id.Pair: // two numbers equally far either side of a mirror
                for (int i = -1; i <= 1; i++) c.Fill(Shape.Segment(V(0, i * .5f - .14f), V(0, i * .5f + .14f), .08f));
                c.Fill(Shape.Circle(V(-.6f, -.12f), .22f));
                c.Fill(Shape.Circle(V(.6f, -.12f), .22f));
                c.Fill(Shape.Arc(V(0, -.12f), .6f, .09f, 38f, 142f));
                break;
            case Id.Sieve: // a perforated disc
                c.Fill(Shape.Circle(V(0, 0), .9f));
                foreach (float x in new[] { -.52f, 0f, .52f })
                foreach (float y in new[] { -.52f, 0f, .52f })
                {
                    if (x * x + y * y < .55f) c.Erase(Shape.Circle(V(x, y), .15f));
                }

                break;
            case Id.Twist: // a trefoil knot
            {
                Vector2 prev = Trefoil(0f);
                for (int i = 1; i <= 90; i++)
                {
                    Vector2 p = Trefoil(i / 90f);
                    c.Fill(Shape.Segment(prev, p, .13f));
                    prev = p;
                }

                break;
            }
        }
    }

    // ---- helpers -------------------------------------------------------------------------------

    static Vector2 Dir(float degrees) => new(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad));

    static Vector2 Spiral(float t)
    {
        float a = t * Mathf.PI * 5.2f + 0.6f;
        float r = Mathf.Lerp(.95f, .12f, t);
        return V(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
    }

    static Vector2 Trefoil(float t)
    {
        float a = t * Mathf.PI * 2f;
        return V((Mathf.Sin(a) + 2f * Mathf.Sin(2f * a)) * .3f, (Mathf.Cos(a) - 2f * Mathf.Cos(2f * a)) * .3f);
    }

    static void Ellipse(Canvas2D c, Vector2 centre, float rx, float ry, float width)
    {
        Vector2 prev = centre + V(rx, 0f);
        for (int i = 1; i <= 56; i++)
        {
            float a = i / 56f * Mathf.PI * 2f;
            Vector2 p = centre + V(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry);
            c.Fill(Shape.Segment(prev, p, width));
            prev = p;
        }
    }

    static void Wave(Canvas2D c, float y, float amplitude, float cycles, float width)
    {
        Vector2 prev = V(-.92f, y);
        for (int i = 1; i <= 48; i++)
        {
            float t = i / 48f;
            Vector2 p = V(Mathf.Lerp(-.92f, .92f, t), y + Mathf.Sin(t * Mathf.PI * 2f * cycles) * amplitude);
            c.Fill(Shape.Segment(prev, p, width));
            prev = p;
        }
    }

    /// <summary>Filled arrowhead with its tip at <paramref name="tip"/>, pointing along <paramref name="degrees"/>.</summary>
    static void Head(Canvas2D c, Vector2 tip, float degrees, float size)
    {
        Vector2 d = Dir(degrees);
        Vector2 n = new(-d.y, d.x);
        c.Fill(Shape.Tri(tip + d * size * .6f, tip - d * size * .5f + n * size * .62f, tip - d * size * .5f - n * size * .62f));
    }

    static void Arrow(Canvas2D c, Vector2 from, Vector2 to, float width, float head)
    {
        Vector2 d = (to - from).normalized;
        c.Fill(Shape.Segment(from, to - d * head * .45f, width));
        Head(c, to - d * head * .4f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, head);
    }

    /// <summary>Open circular arrow from angle a0 to a1; clockwise puts the head at the a0 end.</summary>
    static void ArrowArc(Canvas2D c, Vector2 centre, float radius, float a0, float a1, float width, bool clockwise)
    {
        c.Fill(Shape.Arc(centre, radius, width, a0, a1));
        float tipAngle = clockwise ? a0 : a1;
        Vector2 tip = centre + Dir(tipAngle) * radius;
        float tangent = tipAngle + (clockwise ? -90f : 90f);
        Head(c, tip, tangent, .5f);
    }

    // ---- contact sheet for review --------------------------------------------------------------

    public static Texture2D DumpSheet(int columns = 9)
    {
        Id[] ids = (Id[])Enum.GetValues(typeof(Id));
        int rows = (ids.Length + columns - 1) / columns;
        const int cell = 120;
        Texture2D sheet = new(columns * cell, rows * cell, TextureFormat.RGBA32, false);
        Color32[] fill = new Color32[sheet.width * sheet.height];
        for (int i = 0; i < fill.Length; i++) fill[i] = new Color32(24, 26, 38, 255);
        sheet.SetPixels32(fill);
        for (int n = 0; n < ids.Length; n++)
        {
            Canvas2D icon = new(Size);
            Draw(ids[n], icon);
            float[] coverage = icon.Coverage;
            int ox = (n % columns) * cell + (cell - Size) / 2;
            int oy = (rows - 1 - n / columns) * cell + (cell - Size) / 2;
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float a = coverage[y * Size + x];
                Color32 bg = fill[(oy + y) * sheet.width + ox + x];
                sheet.SetPixel(ox + x, oy + y, new Color32((byte)(bg.r + (235 - bg.r) * a), (byte)(bg.g + (240 - bg.g) * a), (byte)(bg.b + (255 - bg.b) * a), 255));
            }
        }

        sheet.Apply();
        return sheet;
    }

    // ---- software rasteriser ---------------------------------------------------------------------

    struct Shape
    {
        enum Kind { Circle, Ring, Segment, Box, Tri, Arc, Lens }

        Kind kind;
        Vector2 a, b, c;
        float r, w, angle;

        public static Shape Circle(Vector2 centre, float radius) => new() { kind = Kind.Circle, a = centre, r = radius };
        public static Shape Ring(Vector2 centre, float radius, float width) => new() { kind = Kind.Ring, a = centre, r = radius, w = width };
        public static Shape Segment(Vector2 from, Vector2 to, float width) => new() { kind = Kind.Segment, a = from, b = to, w = width };
        public static Shape Box(Vector2 centre, Vector2 half, float rounding, float angleDegrees = 0f) => new() { kind = Kind.Box, a = centre, b = half, r = rounding, angle = angleDegrees * Mathf.Deg2Rad };
        public static Shape Tri(Vector2 p0, Vector2 p1, Vector2 p2) => new() { kind = Kind.Tri, a = p0, b = p1, c = p2 };
        /// <summary>Arc from degrees a0 to a1 (counter-clockwise), round caps.</summary>
        public static Shape Arc(Vector2 centre, float radius, float width, float a0, float a1) => new() { kind = Kind.Arc, a = centre, r = radius, w = width, b = new Vector2(a0 * Mathf.Deg2Rad, a1 * Mathf.Deg2Rad) };
        /// <summary>Lens (eye) outline of the given half extents; <paramref name="outline"/> draws it as a stroke.</summary>
        public static Shape Lens(float halfWidth, float halfHeight, float strokeWidth, bool outline) => new() { kind = Kind.Lens, a = new Vector2(halfWidth, halfHeight), w = outline ? strokeWidth : -1f };

        public float Distance(Vector2 p)
        {
            switch (kind)
            {
                case Kind.Circle:
                    return (p - a).magnitude - r;
                case Kind.Ring:
                    return Mathf.Abs((p - a).magnitude - r) - w * .5f;
                case Kind.Segment:
                {
                    Vector2 pa = p - a, ba = b - a;
                    float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Mathf.Max(ba.sqrMagnitude, 1e-9f));
                    return (pa - ba * h).magnitude - w * .5f;
                }
                case Kind.Box:
                {
                    Vector2 q = p - a;
                    float cs = Mathf.Cos(-angle), sn = Mathf.Sin(-angle);
                    q = new Vector2(q.x * cs - q.y * sn, q.x * sn + q.y * cs);
                    Vector2 d = new Vector2(Mathf.Abs(q.x), Mathf.Abs(q.y)) - (b - Vector2.one * r);
                    return new Vector2(Mathf.Max(d.x, 0f), Mathf.Max(d.y, 0f)).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0f) - r;
                }
                case Kind.Tri:
                    return TriangleDistance(p, a, b, c);
                case Kind.Arc:
                {
                    Vector2 q = p - a;
                    float ang = Mathf.Atan2(q.y, q.x);
                    float a0 = b.x, a1 = b.y;
                    float span = a1 - a0;
                    float rel = Mathf.Repeat(ang - a0, Mathf.PI * 2f);
                    if (rel <= span) return Mathf.Abs(q.magnitude - r) - w * .5f;
                    Vector2 e0 = a + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * r;
                    Vector2 e1 = a + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * r;
                    return Mathf.Min((p - e0).magnitude, (p - e1).magnitude) - w * .5f;
                }
                default: // Lens
                {
                    float hw = a.x, hh = a.y;
                    float radius = (hw * hw + hh * hh) / (2f * hh);
                    float offset = radius - hh;
                    float d1 = (p - new Vector2(0f, -offset)).magnitude - radius;
                    float d2 = (p - new Vector2(0f, offset)).magnitude - radius;
                    float d = Mathf.Max(d1, d2);
                    return w < 0f ? d : Mathf.Abs(d) - w * .5f;
                }
            }
        }

        static float TriangleDistance(Vector2 p, Vector2 p0, Vector2 p1, Vector2 p2)
        {
            Vector2 e0 = p1 - p0, e1 = p2 - p1, e2 = p0 - p2;
            Vector2 v0 = p - p0, v1 = p - p1, v2 = p - p2;
            Vector2 pq0 = v0 - e0 * Mathf.Clamp01(Vector2.Dot(v0, e0) / e0.sqrMagnitude);
            Vector2 pq1 = v1 - e1 * Mathf.Clamp01(Vector2.Dot(v1, e1) / e1.sqrMagnitude);
            Vector2 pq2 = v2 - e2 * Mathf.Clamp01(Vector2.Dot(v2, e2) / e2.sqrMagnitude);
            float s = Mathf.Sign(e0.x * e2.y - e0.y * e2.x);
            Vector2 d0 = new(Vector2.Dot(pq0, pq0), s * (v0.x * e0.y - v0.y * e0.x));
            Vector2 d1 = new(Vector2.Dot(pq1, pq1), s * (v1.x * e1.y - v1.y * e1.x));
            Vector2 d2 = new(Vector2.Dot(pq2, pq2), s * (v2.x * e2.y - v2.y * e2.x));
            float dx = Mathf.Min(d0.x, Mathf.Min(d1.x, d2.x));
            float dy = Mathf.Min(d0.y, Mathf.Min(d1.y, d2.y));
            return -Mathf.Sqrt(dx) * Mathf.Sign(dy);
        }
    }

    sealed class Canvas2D
    {
        readonly int n;
        readonly float[] coverage;
        public float[] Coverage => coverage;
        readonly float scale;

        public Canvas2D(int size)
        {
            n = size;
            coverage = new float[size * size];
            scale = size * .5f; // pixels per unit of distance
        }

        public void Fill(Shape s)
        {
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = s.Distance(Pixel(x, y));
                float cov = Mathf.Clamp01(.5f - d * scale);
                int i = y * n + x;
                if (cov > coverage[i]) coverage[i] = cov;
            }
        }

        public void Erase(Shape s)
        {
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = s.Distance(Pixel(x, y));
                coverage[y * n + x] *= 1f - Mathf.Clamp01(.5f - d * scale);
            }
        }

        Vector2 Pixel(int x, int y) => new(((x + .5f) / n) * 2f - 1f, ((y + .5f) / n) * 2f - 1f);

        public Texture2D ToTexture()
        {
            Texture2D tex = new(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            Color32[] px = new Color32[n * n];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(coverage[i] * 255f));
            tex.SetPixels32(px);
            tex.Apply(true, true);
            return tex;
        }

        public Sprite ToSprite(string name)
        {
            Texture2D tex = ToTexture();
            tex.name = "VRIcon_" + name;
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(.5f, .5f), 100f);
            sprite.name = tex.name;
            return sprite;
        }
    }
}
