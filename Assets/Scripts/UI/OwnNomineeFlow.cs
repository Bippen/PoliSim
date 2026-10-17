using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;

namespace PoliSim.UI
{
    /// <summary>
    /// PS-6 US-33 (`docs/specs/USA_STAGE_PLAN.md`, "Your own nominee (SP-6a)"; R-US4 (a), RULED - F8 §781): **THE CREATION FLOW'S US FORM - A LEADER AT THE HEAD
    /// OF THE DEM OR REP TICKET, NOT A PARTY** - its logic apart from its drawing. Four steps: the ticket (the two parties' own marks), the placement (the
    /// creation flow's compass, the nominee clamped inside the party's range, `OwnNominees.TryRangeOf`), the nominee's name (the creation flow's leader field) and
    /// the review; the origin, the mark and the ink are dropped, because the real party's exist (the plan's §5, "Not asked"). The commit registers the nominee
    /// (`OwnNominees.TryRegister`) and seats the player as its party. `GameController.DrawOwnNomineeScreen` draws it; `PartyCreationCheck` reads the same members,
    /// so the check cannot pass on a choice the screen does not offer. Opened from the party panel's YOUR OWN NOMINEE through the creation flow's own seam.
    /// </summary>
    public static class OwnNomineeFlow
    {
        public const int StepCount = 4;

        /// <summary>The four steps' plate heads, in order.</summary>
        public static readonly string[] StepHeads = { "TICKET", "PLACEMENT", "NOMINEE", "REVIEW" };

        /// <summary>Where the flow is offered: where a nominee of the player's own may stand (`OwnNominees.Offered` - the USA).</summary>
        public static bool Offered(CountryId country) => OwnNominees.Offered(country);

        /// <summary>A new draft: the first ticket offered (the roster's order), the nominee on that party's own point - inside its range, so the default is a
        /// choice the flow offers, none invented. The name is empty: the player names the nominee.</summary>
        public static OwnNominee NewDraft(CountryId country)
        {
            var d = new OwnNominee { Country = country, Name = string.Empty };
            List<string> tickets = OwnNominees.Tickets(country);
            if (tickets.Count > 0) { ChooseTicket(d, tickets[0]); }
            return d;
        }

        /// <summary>A ticket chosen: the nominee moves onto that party's own point (the roster's position, inside its range); a party the flow does not offer changes nothing.</summary>
        public static void ChooseTicket(OwnNominee d, string party)
        {
            if (d == null || !OwnNominees.TryRangeOf(d.Country, party, out OwnNominees.Range _)) { return; }
            foreach (PoliticalParty p in PartySystems.RealRoster(d.Country))
            {
                if (p.Abbrev != party) { continue; }
                d.Party = party;
                d.LrEcon = p.LrEcon;
                d.Galtan = p.Galtan;
                OwnNominees.Clamp(d);
                return;
            }
        }

        /// <summary>The compass's placement: the point to a tenth on each axis, then brought inside the party's range.</summary>
        public static void Place(OwnNominee d, float econ, float soc)
        {
            if (d == null) { return; }
            d.LrEcon = (float)(Math.Round(econ * 10.0, MidpointRounding.AwayFromZero) / 10.0);
            d.Galtan = (float)(Math.Round(soc * 10.0, MidpointRounding.AwayFromZero) / 10.0);
            OwnNominees.Clamp(d);
        }

        /// <summary>What the flow still asks of the player (one word), or null; the registry's own refusal is the last word.</summary>
        public static string Missing(OwnNominee d) => d == null || string.IsNullOrWhiteSpace(d.Name) ? "name" : null;

        /// <summary>The commit: the draft registered as the country's own nominee (the registry's refusal returned as it words it). The flow then seats the player as its party.</summary>
        public static bool TryCommit(OwnNominee d, out string refused)
        {
            refused = Missing(d);
            if (refused != null) { refused = "the " + refused + " is not set"; return false; }
            return OwnNominees.TryRegister(d, out refused);
        }

        /// <summary>The party's share of the model's idle prediction (a percentage) with the nominee where the draft stands - the moved point - or at the party's own
        /// point (<paramref name="atPartyPoint"/>); NaN where no prediction can be made. Read inside a preview of the draft, then the prediction re-run without it, so
        /// the regional derivation the live game reads is the real one again (the creation flow's own discipline).</summary>
        public static double PredictedShare(OwnNominee d, bool atPartyPoint = false)
        {
            if (d == null || string.IsNullOrEmpty(d.Party)) { return double.NaN; }
            OwnNominee probe = d;
            if (atPartyPoint)
            {
                probe = new OwnNominee { Country = d.Country, Party = d.Party, Name = d.Name };
                ChooseTicket(probe, d.Party);
            }
            double share = double.NaN;
            try
            {
                using (OwnNominees.Preview(probe))
                {
                    if (NationalElection.TryPredictShares(d.Country, out Dictionary<string, double> shares) && shares.TryGetValue(d.Party, out double s)) { share = s * 100.0; }
                }
            }
            finally { NationalElection.TryPredictShares(d.Country, out _); }
            return share;
        }

        /// <summary>The ticket in the player's words: the party's short name and name.</summary>
        public static string TicketLine(OwnNominee d)
        {
            if (d == null || string.IsNullOrEmpty(d.Party)) { return "-"; }
            foreach (PoliticalParty p in PartySystems.RealRoster(d.Country)) { if (p.Abbrev == d.Party) { return p.ShortName + " · " + p.Name; } }
            return d.Party;
        }

        /// <summary>A range in the player's words: economic {lo} to {hi} · GAL-TAN {lo} to {hi}, each to one decimal.</summary>
        public static string RangeLine(OwnNominees.Range r) =>
            string.Format(CultureInfo.InvariantCulture, "economic {0:0.0} to {1:0.0} · GAL-TAN {2:0.0} to {3:0.0}", r.EconLo, r.EconHi, r.SocLo, r.SocHi);

        /// <summary>The flow's slip book, built from the draft and the two predictions: an anchor per control the page draws without words.</summary>
        public static PeopleSlips.Book Slips(OwnNominee d, double predicted, double atParty)
        {
            var book = new PeopleSlips.Book();
            for (int s = 0; s < StepCount; s++) { book.Anchors["step/" + s] = new SlipContent(StepHeads[s]).Add(StepLine(s)); }
            foreach (PoliticalParty p in PartySystems.RealRoster(d.Country))
            {
                book.Anchors["party/" + p.Abbrev] = new SlipContent(p.ShortName).Add(p.Name).Add(string.Format(CultureInfo.InvariantCulture, "Economic {0:0.0} · GAL-TAN {1:0.0}", p.LrEcon, p.Galtan));
                if (!OwnNominees.TryRangeOf(d.Country, p.Abbrev, out OwnNominees.Range r)) { continue; }
                book.Anchors["ticket/" + p.Abbrev] = new SlipContent(p.ShortName).Add(p.Name)
                    .Add("Your nominee heads its ticket in place of the nominee of record; the party's mark, ink and seats stay its own.")
                    .Add("Its range: " + RangeLine(r) + " - the middle half of its experts' placements (Global Party Survey 2019).");
            }
            string range = OwnNominees.TryRangeOf(d.Country, d.Party, out OwnNominees.Range own) ? RangeLine(own) : "-";
            book.Anchors["compass"] = new SlipContent("PLACEMENT").Add("Economic left-right across, GAL-TAN up (CHES, 0-10).")
                .Add("The frame is the party's range, " + range + "; the cross is the electorate's centre; the marks are the parties.")
                .Add(string.Format(CultureInfo.InvariantCulture, "Your nominee: economic {0:0.0} · GAL-TAN {1:0.0}.", d.LrEcon, d.Galtan));
            book.Anchors["predicted"] = new SlipContent("THE PARTY'S POLL")
                .Add("The House vote the model gives the party with your nominee where it stands, before any campaign: the party's point is your nominee's.")
                .Add(double.IsNaN(predicted) || double.IsNaN(atParty) ? "No prediction can be made."
                    : string.Format(CultureInfo.InvariantCulture, "At the party's own point {0:0.00} %; with your nominee here {1:0.00} % ({2:+0.00;-0.00;0.00} points).", atParty, predicted, predicted - atParty))
                .Add(string.Format(CultureInfo.InvariantCulture, "On polling day your nominee stands at the party's poll, at a factor of {0:0.0}.", PresidentialElection.FutureFactor));
            book.Anchors["name"] = new SlipContent("NOMINEE").Add("Your nominee's name, as the count and the screens print it.")
                .Add("A new person: no name the record holds for the office - the term limit counts only the elections the game holds.");
            return book;
        }

        private static string StepLine(int s) => s switch
        {
            0 => "The party whose ticket your nominee heads - its mark, ink and seats stay its own.",
            1 => "Where your nominee stands on the two axes the vote model reads, inside the party's range.",
            2 => "Your nominee's name.",
            _ => "The ticket as it enters the run-up. START seats you as the party, your nominee at its head.",
        };
    }
}
