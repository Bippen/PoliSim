using System.Collections.Generic;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>The D24 vocabulary (board 19a): five families - verb, trend, verdict, honesty, state - and the slip's pin.</summary>
    public enum Symbol
    {
        Raise, Lower, Ban, Allow,
        TrendUp, TrendDown, TrendFlat,
        Good, Bad,
        Absent, Billed, Dated, Provisional,
        Locked, Draft,
        Pin,
    }

    /// <summary>
    /// §662 (UI v3.3 §4.2, `docs/specs/UI_V33_SPEC.md`): **THE ICON REGISTRY, WITH HONEST FALLBACKS.** Every symbol has a file stem (Design's
    /// `send/d24_vocab`, installed from its zip under `Resources/Art/UI/Vocab/`) and a WORD - the glyph's own word, which is also the slip's
    /// first line and the dense view's label. A symbol the registry cannot load draws its WORD in its place, never a blank and never a stand-in
    /// glyph, and is recorded in <see cref="Fallbacks"/>, which `SymbolRegistryCoverageCheck` names. Tinted at draw (19a: white on alpha).
    /// </summary>
    public static class SymbolRegistry
    {
        private const string VocabResourcesPath = "Art/UI/Vocab/";

        /// <summary>The symbols drawn as their word this session because the file did not load - the coverage check's list.</summary>
        public static readonly HashSet<Symbol> Fallbacks = new HashSet<Symbol>();

        private static readonly Dictionary<Symbol, Texture2D> Cache = new Dictionary<Symbol, Texture2D>();

        private static string Stem(Symbol s) => s switch
        {
            Symbol.Raise => "vocab_verb_raise", Symbol.Lower => "vocab_verb_lower", Symbol.Ban => "vocab_verb_ban", Symbol.Allow => "vocab_verb_allow",
            Symbol.TrendUp => "vocab_trend_up", Symbol.TrendDown => "vocab_trend_down", Symbol.TrendFlat => "vocab_trend_flat",
            Symbol.Good => "vocab_verdict_good", Symbol.Bad => "vocab_verdict_bad",
            Symbol.Absent => "vocab_honesty_absent", Symbol.Billed => "vocab_honesty_billed", Symbol.Dated => "vocab_honesty_dated", Symbol.Provisional => "vocab_honesty_provisional",
            Symbol.Locked => "vocab_state_locked", Symbol.Draft => "vocab_state_draft",
            _ => "vocab_ui_pin",
        };

        /// <summary>The glyph's own word (19a's legend; 19b's slip first line).</summary>
        public static string Word(Symbol s) => s switch
        {
            Symbol.Raise => "RAISE", Symbol.Lower => "LOWER", Symbol.Ban => "BAN", Symbol.Allow => "ALLOW",
            Symbol.TrendUp => "RISING", Symbol.TrendDown => "FALLING", Symbol.TrendFlat => "FLAT",
            Symbol.Good => "GOOD", Symbol.Bad => "BAD",
            Symbol.Absent => "ABSENT", Symbol.Billed => "BILLED", Symbol.Dated => "DATED", Symbol.Provisional => "PROVISIONAL",
            Symbol.Locked => "LOCKED", Symbol.Draft => "DRAFT",
            _ => "PIN",
        };

        /// <summary>The symbol's texture, or null when its file is not held.</summary>
        private static Texture2D Texture(Symbol s)
        {
            if (Cache.TryGetValue(s, out Texture2D t)) { return t; }
            t = Resources.Load<Texture2D>(VocabResourcesPath + Stem(s));
            Cache[s] = t;
            return t;
        }

        /// <summary>§685 (board 21d): the symbol's texture for a Canvas surface (the election night's DATED glyph), or null when its file is not held -
        /// the caller draws <see cref="Word"/> in its place, the registry's honest fallback, and the symbol is recorded in <see cref="Fallbacks"/>.</summary>
        public static Texture2D TextureOf(Symbol s)
        {
            Texture2D t = Texture(s);
            if (t == null) { Fallbacks.Add(s); }
            return t;
        }

        /// <summary>A verb symbol for a law's verb, or null for none.</summary>
        public static Symbol? Of(LawVerb verb) => verb switch
        {
            LawVerb.Raise => Symbol.Raise, LawVerb.Lower => Symbol.Lower, LawVerb.Ban => Symbol.Ban, LawVerb.Allow => Symbol.Allow, _ => (Symbol?)null,
        };

        /// <summary>Draws the symbol square in <paramref name="r"/>, tinted; its word in <paramref name="wordStyle"/> where the file is not held.</summary>
        public static void Draw(Rect r, Symbol s, Color ink, GUIStyle wordStyle)
        {
            if (Event.current.type != EventType.Repaint) { return; }
            Texture2D t = Texture(s);
            if (t == null)
            {
                Fallbacks.Add(s);
                PoliSimWidgets.MeasuredLabel(r, Word(s), wordStyle);
                return;
            }
            float side = Mathf.Min(r.width, r.height);
            Color previous = GUI.color;
            GUI.color = ink;
            GUI.DrawTexture(new Rect(r.x, r.y + (r.height - side) * 0.5f, side, side), t, ScaleMode.ScaleToFit, true);
            GUI.color = previous;
        }
    }
}
