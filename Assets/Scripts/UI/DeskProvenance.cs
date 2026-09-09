using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// D16 §2, and §9.1 RULED by Elias (2026-09-09): the desk's PROVENANCE mode. Two states, READING (default) and PROVENANCE, and
    /// <b>one value for the whole desk</b> - the same `†` tab in the same corner on the People page, the Laws pages and the Riksbank
    /// page, persisted so a player who turns provenance on is in a mode and does not turn it on again per page.
    ///
    /// <para><b>The state rule, load-bearing (§2):</b> PROVENANCE adds LINES, never COLUMNS. Column boundaries, row order and every
    /// figure's position are identical in both states; the page gets taller and nothing moves sideways. Reserving the lanes at rest was
    /// costed at ~270 px of dead paper on the page whose complaint is density, and refused.</para>
    ///
    /// <para><b>The at-rest exception (§2), a rule and not a taste:</b> a token prints at rest only if it qualifies something currently
    /// drawn. BILLED, ABSENT, ◇ DATED, ‡ TWO-DEFINITION and the report count (drawn into the pips) stay at rest; SOURCED and DERIVED go
    /// behind the tab because they say nothing about a figure you can already see; COUPLING DRAFT prints exactly when 5c's arrow prints,
    /// following its subject onto the page and off it.</para>
    ///
    /// Persisted through PlayerPrefs on the AudioDirector's idiom - read on first touch, so a harness that never asks never touches the
    /// prefs, and a check that sets it restores what it found.
    /// </summary>
    public static class DeskProvenance
    {
        private const string ProvenancePref = "polisim.desk.provenance";
        private static bool _read;
        private static bool _on;

        /// <summary>The desk's state: false = READING, true = PROVENANCE. One value for every page.</summary>
        public static bool On
        {
            get { ReadPrefs(); return _on; }
            set { ReadPrefs(); _on = value; PlayerPrefs.SetInt(ProvenancePref, value ? 1 : 0); }
        }

        /// <summary>The tab's glyph - the printer's footnote mark, one cell instead of two words (§3.6).</summary>
        public const string Glyph = "†";

        /// <summary>◇ DATED: this row's vintage is older than its family's (§3.6).</summary>
        public const string DatedGlyph = "◇";

        /// <summary>‡ TWO DEFINITIONS: the sources disagree on what is counted (§3.6).</summary>
        public const string TwoDefinitionGlyph = "‡";

        private static void ReadPrefs()
        {
            if (_read) { return; }
            _read = true;
            _on = PlayerPrefs.GetInt(ProvenancePref, 0) == 1;
        }
    }
}
