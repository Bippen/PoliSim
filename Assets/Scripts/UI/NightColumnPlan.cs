using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;
using ColumnCase = PoliSim.UI.ElectionNightScreen.ColumnCase;
using ColumnPlan = PoliSim.UI.ElectionNightScreen.ColumnPlan;

namespace PoliSim.UI
{
    /// <summary>
    /// D-NIGHT (the review's H1): the election night's WHO GOVERNS column, planned to its rect before it is drawn - the arithmetic and the shrink order the
    /// comment on <see cref="ElectionNightScreen.ColumnPx"/> sets out. Its own file so the night (`ElectionNightScreen.BuildGoverns`) and the check
    /// (`FormationProjectionDiagnostic`) call one planner.
    /// </summary>
    public static class NightColumnPlan
    {
        private const int ColumnPx = ElectionNightScreen.ColumnPx;

        /// <summary>The column's height in board px for these sizes - the arithmetic above, block by block.</summary>
        public static int ColumnHeight(ColumnCase k, int bridgePx, int movers, bool key, bool k119, int effectRows)
        {
            int h = 28;
            if (k.Round) { h += 6 + 24; }
            h += 8 + 1 + 30;
            if (!k.Counting && !k.NoFormation)
            {
                if (k.NoneExpected) { h += 20 * k.ReasonRows; }
                else if (k.SupportRow) { h += 26; }
            }
            if (k.Counting || k.Lanes > 0)
            {
                int lanes = k.Counting ? 1 : k.Lanes;
                h += 8 + 26 + lanes * (4 + 20);
                if (key && !k.Counting) { h += 6 + 18; }
            }
            if (!k.Counting && !k.NoFormation && k.You) { h += 8 + 1 + 30 + (k119 ? 20 : 0); }
            if (k.Ledger) { h += 12 + 26 + 4 + bridgePx + 18 + (movers > 0 ? 4 + 20 * movers : 0); }
            else { h += k.LedgerFault ? 12 + 26 : 12 + 26 + 4 + 24; }
            if (k.Budget) { h += k.Effects == 0 ? 12 + 26 + 20 : 12 + 26 + 4 + 22 * effectRows; }
            return h;
        }

        /// <summary>The column's plan: the natural height, then the shrink order (<see cref="ColumnPx"/>) until it fits. <paramref name="shrink"/> false: the
        /// natural sizes, as a check's plant asks.</summary>
        public static ColumnPlan PlanColumn(ColumnCase k, bool shrink = true)
        {
            var p = new ColumnPlan { BridgePx = Mathf.Clamp(k.BridgePx, 24, 40), Movers = Mathf.Clamp(k.Movers, 0, 2), Key = k.Key, K119 = k.K119, EffectRows = (k.Effects + 2) / 3 };
            int H() => ColumnHeight(k, p.BridgePx, p.Movers, p.Key, p.K119, p.EffectRows);
            p.Natural = H();
            if (shrink)
            {
                if (H() > ColumnPx && p.BridgePx > 24) { p.BridgePx = 24; p.Shrunk.Add("the bridge lane to its floor (24 px)"); }
                if (H() > ColumnPx && p.Movers > 1) { p.Movers = 1; p.Shrunk.Add("the fall and rise to one row"); }
                if (H() > ColumnPx && p.Movers > 0) { p.Movers = 0; p.Shrunk.Add("the fall and rise to none"); }
                if (H() > ColumnPx && p.Key) { p.Key = false; p.Shrunk.Add("the key line into the vote's slip"); }
                if (H() > ColumnPx && p.EffectRows > 2) { p.EffectRows = 2; p.Shrunk.Add("the outcomes to two rows"); }
                if (H() > ColumnPx && p.K119) { p.K119 = false; p.Shrunk.Add("the K119 line into the You slip"); }
            }
            p.Final = H();
            return p;
        }

        /// <summary>The worst case a night's view can draw, for a check: every optional row its view allows, the tallest bridge, two movers, seven outcomes.</summary>
        public static ColumnCase WorstCaseOf(CountryId country, GovernmentFormation.View g)
        {
            var k = new ColumnCase { Country = country, Budget = country != CountryId.Germany, Effects = 7, Key = true, BridgePx = 40, Movers = 2 };
            if (g == null) { k.NoFormation = true; k.Lanes = 0; return k; }
            k.Round = g.Round.Count > 0;
            k.NoneExpected = !g.HasGovernment;
            k.SupportRow = g.HasGovernment && g.Support.Count > 0;
            k.ReasonRows = g.HasGovernment ? 0 : 2;
            k.Lanes = g.Votes.Count > 0 ? g.Votes.Count : g.HasGovernment ? 1 : 0;
            k.K119 = g.ReadsPlayersAnswer && g.PlayerParty != null;
            return k;
        }

    }
}
