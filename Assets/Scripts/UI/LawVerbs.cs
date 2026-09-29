using System.Collections.Generic;
using PoliSim.Data;

namespace PoliSim.UI
{
    /// <summary>The verb half of a law's pair (D24, board 19a): RAISE · LOWER · BAN · ALLOW, or none.</summary>
    public enum LawVerb { None, Raise, Lower, Ban, Allow }

    /// <summary>
    /// §662 (D24, board 19a and Design's `#d24-note` item 2, answered 2026-09-29): **A LAW'S VERB, READ FROM THE SIGNS OF WHAT IT MOVES.**
    /// Every dial delta (<see cref="LawDefinition.DialDeltas"/>) and every structural delta (<see cref="LawDefinition.Structural"/>, on its own
    /// printed scale - a higher number is RAISE, never "stricter", because strict and loose are verdicts) is read by sign: all up is RAISE, all
    /// down is LOWER. A law whose moves go BOTH WAYS takes Design's rule - BAN if its net move is a prohibition, ALLOW if it permits what was not,
    /// no verb if it does both or neither - applied BY NAME in <see cref="BothWaysReading"/>; a both-ways law Design has not yet read shows no
    /// verb, which is the rule's own honest answer. A law that moves nothing shows no verb.
    /// </summary>
    public static class LawVerbs
    {
        /// <summary>Design's reading of each both-ways law by id, as Design sends it (the 13 names went to Design on 2026-09-29, §662). Empty until
        /// it answers: every both-ways law is then no verb.</summary>
        public static readonly Dictionary<string, LawVerb> BothWaysReading = new Dictionary<string, LawVerb>();

        /// <summary>Whether the law's moves go both ways - the census's BOTH WAYS column, the one definition.</summary>
        public static bool IsBothWays(LawDefinition law)
        {
            Signs(law, out bool up, out bool down);
            return up && down;
        }

        public static LawVerb Of(LawDefinition law)
        {
            if (law == null) { return LawVerb.None; }
            Signs(law, out bool up, out bool down);
            if (up && down) { return law.Id != null && BothWaysReading.TryGetValue(law.Id, out LawVerb read) ? read : LawVerb.None; }
            return up ? LawVerb.Raise : down ? LawVerb.Lower : LawVerb.None;
        }

        private static void Signs(LawDefinition law, out bool up, out bool down)
        {
            up = false; down = false;
            foreach (float d in law.DialDeltas) { if (d > 0f) { up = true; } else if (d < 0f) { down = true; } }
            if (law.Structural == null) { return; }
            foreach (StructuralDelta s in law.Structural) { if (s.Delta > 0f) { up = true; } else if (s.Delta < 0f) { down = true; } }
        }
    }
}
