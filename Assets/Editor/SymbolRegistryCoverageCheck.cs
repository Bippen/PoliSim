using System;
using PoliSim.Data;
using PoliSim.Simulation;
using PoliSim.UI;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §662 (UI v3.3 §4.2): **EVERY SYMBOL IN THE REGISTRY RESOLVES, AND EVERY LAW'S PAIR IS DRAWABLE.** Through the game's own `Resources.Load`
    /// path, each <see cref="Symbol"/> loads at Design's 256×256 convention; a symbol that does not is a FAILURE, because Design has delivered
    /// all sixteen (`send/d24_vocab`, installed from its zip) and the registry would draw its word where a glyph was delivered. The symbols drawn
    /// as a fallback this session (<see cref="SymbolRegistry.Fallbacks"/>) are named. And the pair: every law's verb reads (<see cref="LawVerbs"/>),
    /// a both-ways law shows no verb until Design's reading names it, and every law category's emblem resolves - the area's icon, or the interest
    /// rate's stat icon for the monetary regime (Design's call, `#d24-note` item 1).
    /// </summary>
    public static class SymbolRegistryCoverageCheck
    {
        public static void Run()
        {
            // the registry's loader and stems, and the law row's emblem, are private to the game: read by reflection, as D24Inventory reads the category's area
            var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
            System.Reflection.MethodInfo texture = typeof(SymbolRegistry).GetMethod("Texture", flags), stem = typeof(SymbolRegistry).GetMethod("Stem", flags), lawEmblem = typeof(GameController).GetMethod("LawEmblem", flags);
            if (texture == null || stem == null || lawEmblem == null) { Debug.LogError("  the registry's Texture/Stem or GameController.LawEmblem is not reachable - VERIFIED NOTHING"); CheckExit.Finish(1); return; }
            Texture2D Tex(Symbol s) => (Texture2D)texture.Invoke(null, new object[] { s });
            string Stem(Symbol s) => (string)stem.Invoke(null, new object[] { s });
            int total = 0, missing = 0;
            foreach (Symbol s in (Symbol[])Enum.GetValues(typeof(Symbol)))
            {
                total++;
                Texture2D t = Tex(s);
                if (t == null) { Debug.LogError($"  MISSING {s} -> {Stem(s)} (the registry would draw '{SymbolRegistry.Word(s)}')"); missing++; }
                else if (t.width != 256 || t.height != 256) { Debug.LogError($"  ODD SIZE {s} -> {t.width}x{t.height}, Design's convention is 256x256"); missing++; }
                else { Debug.Log($"  ok   {s} -> {Stem(s)} ({SymbolRegistry.Word(s)})"); }
            }
            if (total == 0) { Debug.LogError("  EMPTY ENUMERATION - no Symbol values. VERIFIED NOTHING."); CheckExit.Finish(1); return; }

            int raise = 0, lower = 0, both = 0, none = 0, read = 0;
            foreach (LawDefinition law in LawCatalog.All)
            {
                LawVerb verb = LawVerbs.Of(law);
                if (LawVerbs.IsBothWays(law)) { both++; if (verb != LawVerb.None) { read++; } }
                else if (verb == LawVerb.Raise) { raise++; }
                else if (verb == LawVerb.Lower) { lower++; }
                else { none++; }
            }
            int emblems = 0, emblemMissing = 0;
            foreach (LawCategory category in (LawCategory[])Enum.GetValues(typeof(LawCategory)))
            {
                emblems++;
                if (lawEmblem.Invoke(null, new object[] { category }) == null) { Debug.LogError($"  MISSING the {category} emblem"); emblemMissing++; }
            }
            string fallbacks = SymbolRegistry.Fallbacks.Count == 0 ? "none" : string.Join(", ", SymbolRegistry.Fallbacks);
            Debug.Log($"=== symbol registry: {total - missing} of {total} symbols resolve; drawn as their word this session: {fallbacks}. Laws: {raise} RAISE, {lower} LOWER, "
                      + $"{both} both ways ({read} read by Design, the rest no verb), {none} moving nothing. Emblems: {emblems - emblemMissing} of {emblems} categories ===");
            CheckExit.Finish(missing == 0 && emblemMissing == 0 ? 0 : 1);
        }
    }
}
