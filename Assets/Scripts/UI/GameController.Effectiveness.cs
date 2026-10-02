using System.Globalization;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// P5-C7 (2026-09-05, late) - THE CABINET'S EFFECTIVENESS READOUT, Design's board 9d (D15 item 4): on the minister's card a readout block under
    /// the attribute rows, separated by a rule and directly beneath EFFICIENCY (its multiplier) - the ratio at mono 15 bold (×0.91), 5c's arrow from a
    /// baseline that is UNITY (length ∝ |r − 1| against the largest departure in the Cabinet, pointing left below 1 and right above), the decomposition
    /// beneath as two ratios and a word (0.965 ALLOC ÷ REQ × 0.94 EFFICIENCY), and the scope line. No money on the card - the amounts live on the
    /// Budget row. Below unity the ratio takes Bad; at or above it inkText (met is not a verdict, overfunding is not Good). The state line under the
    /// name says the consequence in words the coupling table owns. A ratio, never a score: ×, two decimals, no 0–100 scale, no bar-to-full. The Health
    /// minister's card also quotes the family's three KEYS (9c: "the card quotes, the page reads") - figure + unit + source, no band, no arrows.
    ///
    /// <para>§745 (UI v3.5, Politics › Cabinet): the card is the composition's now - the readout's figures, its decomposition and the level, and the family's
    /// keys, are the card's slip (`CabinetEffectivenessLines`, `CabinetFamilyKeyLines`); below unity the ratio stays on the card as a chip in Bad. The
    /// state line is what remains here.</para>
    /// </summary>
    public partial class GameController
    {
        /// <summary>The state line under the minister's name - the consequence in words the coupling table owns; text, not colour (9d).</summary>
        private string EffectivenessStateLine(CabinetPortfolio portfolio)
        {
            float ratio = Effectiveness.RatioOf(_playerCountry, portfolio);
            if (ratio < 0.995f)
            {
                return portfolio == CabinetPortfolio.HealthSocialAffairs ? "UNDERFUNDED — WAITS RISE, QUALITY DRIFTS" : "UNDERFUNDED — THE PORTFOLIO'S OUTCOMES DRIFT WHEN A FAMILY READS IT";
            }
            return ratio > 1.005f ? "ABOVE UNITY — MET IS NOT A VERDICT" : "MET — THE REQUEST IS FUNDED";
        }
    }
}
