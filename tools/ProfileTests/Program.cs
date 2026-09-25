using HeroRadialMenusMod;
using System;
using System.IO;
using System.Linq;

/// <summary>
/// Exercises the shipped radial profile reader end to end: a JSON file on disk
/// is parsed, cached, re-read on change, and turned into the exact values the
/// mod would write onto its objects.
///
/// Unity components cannot be constructed outside the engine (UnityEngine.Object's
/// static initializer throws), so the assertions run against
/// <see cref="LayerProfile.Resolve"/>, which shares the parse, the key lookup
/// and the game-owned-text guard with Apply — only the final assignment to a
/// component differs.
///
/// These cover the defects the 2026-09-16 review found:
///   P1  the radial mod had no profile reader at all;
///   P1  apply-without-restore left removed overrides in place, so reset was a
///       no-op (checked here as "the profile stops naming the key");
///   P2  text styling was looked up on UI.Text only.
/// </summary>
internal static class Program
{
    private static int _failures;
    private static string _dir = "";

    private static void Main()
    {
        var sandbox = Path.Combine(Path.GetTempPath(), "vh-prof-" + Guid.NewGuid().ToString("N").Substring(0, 8));
        _dir = Path.Combine(sandbox, "HeroModUiProfiles");
        Directory.CreateDirectory(_dir);
        // BepInEx.Paths is read-only outside the game, so point the reader at the sandbox.
        LayerProfile.ProfileDirectoryOverride = _dir;

        try { RunAll(); }
        finally { try { Directory.Delete(sandbox, true); } catch { } }

        Console.WriteLine();
        Console.WriteLine(_failures == 0
            ? "PASS: the radial reader loads and re-reads profiles, resolves every supported property, keeps the "
              + "two wheels independent, drops removed keys so reset restores, refuses broken/newer files, and "
              + "never replaces game-owned text."
            : $"FAILED: {_failures} check(s)");
        Environment.Exit(_failures == 0 ? 0 : 1);
    }

    private static void RunAll()
    {
        Section("mouse selection and inactive empty sectors");
        Assert("centre cancels even with zero dead zone", RadialSelection.HoverIndex(0, 0, 0, 3, 1) == -1);
        Assert("inside dead zone cancels", RadialSelection.HoverIndex(0, 20, 34, 3, 1) == -1);
        Assert("occupied top sector selects", RadialSelection.HoverIndex(0, 320, 34, 3, 1) == 0);
        Assert("empty right sector cannot hover", RadialSelection.HoverIndex(277, -160, 34, 3, 1) == -1);
        Assert("empty left sector cannot hover", RadialSelection.HoverIndex(-277, -160, 34, 3, 2) == -1);
        Assert("second occupied sector selects", RadialSelection.HoverIndex(277, -160, 34, 3, 2) == 1);
        Assert("empty inventory cannot hover", RadialSelection.HoverIndex(0, 320, 34, 3, 0) == -1);
        Assert("wrap across north selects first sector", RadialSelection.HoverIndex(-1, 320, 34, 16, 16) == 0);
        // --------------------------------------- the reader exists and works
        Section("radial profile reader (P1: there was no reader)");

        var heal = Write("heal-wheel", @"{
          ""schemaVersion"": 1,
          ""overrides"": {
            ""heal-wheel.root"": { ""pos"": [40, -25], ""scale"": [1.5, 1.5] },
            ""heal-wheel.dim"": { ""tint"": [0.1, 0.2, 0.3, 0.9], ""alpha"": 0.5 },
            ""heal-wheel.slot[2]"": { ""enabled"": false }
          }
        }");
        Assert("profile file is read", heal.Poll(Log()), "Poll returned false");
        Assert("overrides parsed", heal.HasOverrides, "none parsed");

        var root = heal.Resolve("heal-wheel.root");
        Assert("root override found", root.Found, "not found");
        Assert("pos resolved", Near(root.Pos, 40, -25), Show(root.Pos));
        Assert("scale resolved", Near(root.Scale, 1.5f, 1.5f), Show(root.Scale));

        var dim = heal.Resolve("heal-wheel.dim");
        Assert("tint resolved", Near(dim.Tint, 0.1f, 0.2f, 0.3f, 0.9f), Show(dim.Tint));
        Assert("alpha resolved", dim.Alpha.HasValue && Math.Abs(dim.Alpha.Value - 0.5f) < 1e-4,
            dim.Alpha?.ToString() ?? "null");

        var slot = heal.Resolve("heal-wheel.slot[2]");
        Assert("per-sector enabled resolved", slot.Enabled == false, slot.Enabled?.ToString() ?? "null");

        Assert("a node the profile does not name resolves to nothing",
            !heal.Resolve("heal-wheel.notThere").Found, "found something");

        // ------------------------------------------ the two wheels are separate
        Section("heal and arrow wheels never share overrides");

        var arrow = Write("arrow-wheel", @"{
          ""schemaVersion"": 1,
          ""overrides"": { ""arrow-wheel.root"": { ""pos"": [-11, 3] } }
        }");
        arrow.Poll(Log());

        Assert("arrow wheel has its own value",
            Near(arrow.Resolve("arrow-wheel.root").Pos, -11, 3),
            Show(arrow.Resolve("arrow-wheel.root").Pos));
        Assert("heal wheel keeps its own value",
            Near(heal.Resolve("heal-wheel.root").Pos, 40, -25),
            Show(heal.Resolve("heal-wheel.root").Pos));
        Assert("heal profile does not answer arrow ids",
            !heal.Resolve("arrow-wheel.root").Found, "answered");
        Assert("arrow profile does not answer heal ids",
            !arrow.Resolve("heal-wheel.root").Found, "answered");
        Assert("the two use different files",
            heal.ProfilePath != arrow.ProfilePath, heal.ProfilePath);

        // --------------------------------------- removed keys must stop applying
        Section("reset (P1: removed overrides used to stay applied)");

        // A -> B, where B keeps only `size`. Everything else must stop being
        // named, which is what lets restore-then-apply put the base back.
        Write("heal-wheel", @"{
          ""schemaVersion"": 1,
          ""overrides"": { ""heal-wheel.slot[0]"": {
            ""pos"": [99, -99], ""size"": [80, 80], ""tint"": [1, 0, 0, 1] } }
        }");
        var pA = new LayerProfile("heal-wheel");
        pA.Poll(Log());
        var a = pA.Resolve("heal-wheel.slot[0]");
        Assert("A names pos, size and tint",
            a.Pos != null && a.Size != null && a.Tint != null, Show(a.Pos));

        Write("heal-wheel", @"{
          ""schemaVersion"": 1,
          ""overrides"": { ""heal-wheel.slot[0]"": { ""size"": [70, 70] } }
        }");
        var pB = new LayerProfile("heal-wheel");
        pB.Poll(Log());
        var b = pB.Resolve("heal-wheel.slot[0]");
        Assert("B still names size", Near(b.Size, 70, 70), Show(b.Size));
        Assert("B no longer names pos", b.Pos == null, Show(b.Pos));
        Assert("B no longer names tint", b.Tint == null, Show(b.Tint));

        // An emptied profile names nothing at all.
        Write("heal-wheel", @"{ ""schemaVersion"": 1, ""overrides"": {} }");
        var pEmpty = new LayerProfile("heal-wheel");
        pEmpty.Poll(Log());
        Assert("emptied profile has no overrides", !pEmpty.HasOverrides, "still has some");
        Assert("emptied profile names nothing", !pEmpty.Resolve("heal-wheel.slot[0]").Found, "found");

        // Deleting the file behaves the same, and Poll reports the change so the
        // caller knows to restore. Load a profile that actually HAS overrides
        // first — otherwise these assertions pass trivially and would not catch
        // a reader that keeps its cache after the file goes away.
        var path = Path.Combine(_dir, "com.heromod.valheim.radialmenus.heal-wheel.json");
        Write("heal-wheel", @"{
          ""schemaVersion"": 1,
          ""overrides"": { ""heal-wheel.slot[0]"": { ""pos"": [88, -88] } }
        }");
        var pDel = new LayerProfile("heal-wheel");
        pDel.Poll(Log());
        Assert("the profile really is loaded before deletion",
            pDel.HasOverrides && pDel.Resolve("heal-wheel.slot[0]").Found, "nothing loaded");

        File.Delete(path);
        Assert("deleting the profile is reported", pDel.Poll(Log()), "Poll returned false");
        Assert("deleted profile drops its overrides", !pDel.HasOverrides, "cache survived deletion");
        Assert("deleted profile names nothing", !pDel.Resolve("heal-wheel.slot[0]").Found, "found");

        // --------------------------------------------------- reload on change
        Section("the profile is re-read when the file changes");

        Write("heal-wheel", @"{ ""schemaVersion"": 1,
          ""overrides"": { ""heal-wheel.root"": { ""pos"": [1, 1] } } }");
        var live = new LayerProfile("heal-wheel");
        Assert("first poll loads", live.Poll(Log()), "no load");
        Assert("value v1", Near(live.Resolve("heal-wheel.root").Pos, 1, 1),
            Show(live.Resolve("heal-wheel.root").Pos));
        Assert("an unchanged file does not reload", !live.Poll(Log()), "reloaded anyway");

        System.Threading.Thread.Sleep(20);
        Write("heal-wheel", @"{ ""schemaVersion"": 1,
          ""overrides"": { ""heal-wheel.root"": { ""pos"": [2, 2] } } }");
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
        Assert("a changed file reloads", live.Poll(Log()), "did not reload");
        Assert("value v2", Near(live.Resolve("heal-wheel.root").Pos, 2, 2),
            Show(live.Resolve("heal-wheel.root").Pos));

        // ------------------------------------------------------ bad input
        Section("broken and future profiles are refused");

        File.WriteAllText(path, "{ this is not json");
        var pBad = new LayerProfile("heal-wheel");
        pBad.Poll(Log());
        Assert("broken JSON yields no overrides", !pBad.HasOverrides, "parsed anyway");
        Assert("broken JSON names nothing", !pBad.Resolve("heal-wheel.root").Found, "found");

        File.WriteAllText(path, @"{ ""schemaVersion"": 99, ""overrides"": {
            ""heal-wheel.root"": { ""pos"": [5, 5] } } }");
        var pFuture = new LayerProfile("heal-wheel");
        pFuture.Poll(Log());
        Assert("a newer schema is refused", !pFuture.HasOverrides, "applied anyway");

        // -------------------------------------------------------- text rules
        Section("text styling and the game-owned value guard (P2)");

        Write("heal-wheel", @"{
          ""schemaVersion"": 1,
          ""overrides"": { ""heal-wheel.centerLabel"": {
            ""text"": { ""size"": 34, ""color"": [1, 0, 0, 1], ""value"": ""replaced"" } } }
        }");
        var pText = new LayerProfile("heal-wheel");
        pText.Poll(Log());

        // The wheel passes allowTextValue:false for the centre label.
        var guarded = pText.Resolve("heal-wheel.centerLabel", allowTextValue: false);
        Assert("font size is resolved", guarded.TextSize.HasValue
            && Math.Abs(guarded.TextSize.Value - 34f) < 1e-4, guarded.TextSize?.ToString() ?? "null");
        Assert("font colour is resolved", Near(guarded.TextColor, 1, 0, 0, 1), Show(guarded.TextColor));
        Assert("game-owned text value is NOT taken", guarded.TextValue == null,
            guarded.TextValue ?? "null");

        var open = pText.Resolve("heal-wheel.centerLabel", allowTextValue: true);
        Assert("text value is taken when allowed", open.TextValue == "replaced", open.TextValue ?? "null");
        Assert("styling is unaffected by the guard",
            open.TextSize.HasValue && Math.Abs(open.TextSize.Value - 34f) < 1e-4, "changed");

        DynamicNodeRules();
    }

    /// <summary>
    /// The highlighter, the chevron and the slots are rewritten by the update
    /// loop every frame, so their overrides are merged with the live state
    /// instead of being written once. The 2026-09-16 follow-up review found the
    /// merge missing entirely: UpdateHover re-enabled a hidden ornament and put
    /// the .cfg colour back over the profile's tint, and Bind() re-enabled a
    /// hidden slot as soon as it held an item.
    ///
    /// These run the real merge (DynamicOverride is the same code the wheel
    /// calls), not a restatement of it.
    /// </summary>
    private static void DynamicNodeRules()
    {
        Section("dynamic nodes merge profile with live state (P1)");

        // --- visibility: the profile may hide, but never force-show.
        Assert("shown when the game shows it and the profile allows it",
            DynamicOverride.Visible(true, true), "hidden");
        Assert("a profile-disabled node stays hidden while the game shows it",
            !DynamicOverride.Visible(true, false), "the override was ignored");
        Assert("a node the game hides stays hidden",
            !DynamicOverride.Visible(false, true), "shown anyway");
        Assert("a profile cannot reveal what the game hides",
            !DynamicOverride.Visible(false, false), "shown anyway");

        var cfg = new UnityEngine.Color(1f, 0.72f, 0.36f, 1f);   // Plugin.RadialFrameColor
        var tint = new UnityEngine.Color(0f, 0.5f, 1f, 1f);

        // --- colour: the profile's tint wins over .cfg, or .cfg stands.
        var noTint = DynamicOverride.Tint(cfg, null, 1f, 1f);
        Assert("with no tint the .cfg colour is used",
            Same(noTint, cfg.r, cfg.g, cfg.b, 1f), Show(noTint));

        var tinted = DynamicOverride.Tint(cfg, tint, 1f, 1f);
        Assert("the profile tint replaces the .cfg colour",
            Same(tinted, 0f, 0.5f, 1f, 1f), Show(tinted));

        // --- alpha: the hover fade and the profile alpha multiply, so a dimmed
        // ornament still animates instead of being pinned to a constant.
        var faded = DynamicOverride.Tint(cfg, null, 0.5f, 1f);
        Assert("the animated fade alone halves the alpha",
            Math.Abs(faded.a - 0.5f) < 1e-4, faded.a.ToString());

        var dimmed = DynamicOverride.Tint(cfg, null, 1f, 0.5f);
        Assert("the profile alpha alone halves the alpha",
            Math.Abs(dimmed.a - 0.5f) < 1e-4, dimmed.a.ToString());

        var both = DynamicOverride.Tint(cfg, null, 0.5f, 0.5f);
        Assert("the two alphas multiply rather than one winning",
            Math.Abs(both.a - 0.25f) < 1e-4, both.a.ToString());

        var mid = DynamicOverride.Tint(cfg, null, 0.5f, 0.4f);
        Assert("a dimmed ornament still fades (alpha is not pinned)",
            mid.a > 0f && mid.a < 0.4f, mid.a.ToString());

        // The tint's own alpha is part of the product too.
        var halfTint = new UnityEngine.Color(0f, 0.5f, 1f, 0.5f);
        var prod = DynamicOverride.Tint(cfg, halfTint, 0.5f, 0.5f);
        Assert("the tint's own alpha is included in the product",
            Math.Abs(prod.a - 0.125f) < 1e-4, prod.a.ToString());

        // --- the wheel really routes through these rules.
        var wheelSrc = ReadModFile("RadialMenus.cs");
        Assert("the highlighter's colour goes through the merge",
            wheelSrc.Contains("_highlighterImage!.color = DynamicOverride.Tint("), "not merged");
        Assert("the highlighter's visibility goes through the merge",
            wheelSrc.Contains("DynamicOverride.Visible(true, _ovHighlighterEnabled)"), "not merged");
        Assert("the cursor's visibility goes through the merge",
            wheelSrc.Contains("_ovCursorEnabled)"), "not merged");
        Assert("the cursor's colour goes through the merge",
            wheelSrc.Contains("_cursorImage!.color = DynamicOverride.Tint("), "not merged");
        Assert("Bind honours a profile-hidden slot",
            wheelSrc.Contains("DynamicOverride.Visible(true, !HiddenByProfile)"), "Bind re-enables it");
        Assert("SetActive honours a profile-hidden slot",
            wheelSrc.Contains("DynamicOverride.Visible(active, !HiddenByProfile)"), "not honoured");

        // --- dynamic nodes must be kept out of the baseline, or Restore would
        // pin their angle-driven transform and freeze the animation, and a slot
        // hidden by a smaller Rebuild could be switched back on.
        Assert("dynamic nodes are excluded from capture/restore",
            wheelSrc.Contains("if (IsDynamicNode(pair.Key)) continue;"), "still captured");
        foreach (var frag in new[] { ".highlighter\"", ".cursor\"", ".cursor.tip\"", ".slot[\"" })
            Assert($"IsDynamicNode covers {frag.Trim('"')}",
                wheelSrc.Contains($"$\"{{_surfaceId}}{frag}"), "not covered");
    }

    private static bool Same(UnityEngine.Color c, float r, float g, float b, float a) =>
        Math.Abs(c.r - r) < 1e-4 && Math.Abs(c.g - g) < 1e-4
        && Math.Abs(c.b - b) < 1e-4 && Math.Abs(c.a - a) < 1e-4;

    private static string Show(UnityEngine.Color c) => $"({c.r}, {c.g}, {c.b}, {c.a})";

    /// <summary>Read a file from the mod's own source tree (path injected by MSBuild).</summary>
    private static string ReadModFile(string name)
    {
        var dir = typeof(Program).Assembly
            .GetCustomAttributes(typeof(System.Reflection.AssemblyMetadataAttribute), false)
            .Cast<System.Reflection.AssemblyMetadataAttribute>()
            .First(a => a.Key == "ModSourceDir").Value;
        return File.ReadAllText(Path.Combine(dir, name));
    }

    // ------------------------------------------------------------- helpers

    private static LayerProfile Write(string surfaceId, string json)
    {
        File.WriteAllText(
            Path.Combine(_dir, $"com.heromod.valheim.radialmenus.{surfaceId}.json"), json);
        return new LayerProfile(surfaceId);
    }

    private static bool Near(float[]? actual, params float[] want)
    {
        if (actual == null || actual.Length != want.Length) return false;
        for (int i = 0; i < want.Length; i++)
            if (Math.Abs(actual[i] - want[i]) > 1e-3f) return false;
        return true;
    }

    private static string Show(float[]? v) => v == null ? "null" : "[" + string.Join(", ", v) + "]";

    private static BepInEx.Logging.ManualLogSource Log() =>
        BepInEx.Logging.Logger.CreateLogSource("ProfileTests");

    private static void Section(string name) => Console.WriteLine(name);

    private static void Assert(string name, bool ok, string detail = "")
    {
        if (ok) Console.WriteLine($"  ok   {name}");
        else { Console.WriteLine($"  FAIL {name}  {detail}"); _failures++; }
    }
}
