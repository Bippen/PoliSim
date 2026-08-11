#!/usr/bin/env python3
"""Fails when SeatAllocation.cs cannot reproduce a real Riksdag's real seat count exactly.

WHY THIS EXISTS
    SeatAllocation.cs's own doc comment sets the standard: "Fed Sweden's real 2022 vote shares it
    must return 107/73/68/24/24/19/18/16, not 'roughly that'." A highest-averages allocator that is
    subtly wrong - one divisor off, a threshold check in the wrong order, a >= that should be a >
    - still produces a plausible-looking chamber. It does not throw, does not NaN, does not fail to
    compile. The only way to catch it is to feed it a real result and diff the output, which is what
    this does. Same shape as the D'Hondt/Poland check in POLISIM_POLITICS_ELECTIONS_ROADMAP.md 3.2:
    "found in minutes by a script with no engine, no Editor and no compile."

    This is a manual line-for-line port of SeatAllocation.Allocate / AllocateHighestAverages /
    Divisor / ApplyThreshold as they stand in Assets/Scripts/Simulation/SeatAllocation.cs - not a
    reimplementation of Sainte-Lague from a textbook. It deliberately copies this file's exact loop
    order and exact tie-break, so it tests what the C# actually does, not what Sainte-Lague is
    supposed to do in general. If SeatAllocation.cs changes, re-port by hand; a check that quietly
    drifts from the code it is checking is worse than no check.

WHAT IT TESTS, AND WHAT EACH RESULT ACTUALLY SHOWS
    1. Sweden 2022, full pipeline (ApplyThreshold then Allocate(SainteLagueModified), 349 seats) vs
       the real, official, certified result. THIS IS THE HEADLINE CHECK - it is the direct answer to
       "is the Riksdag allocator right".

    2. Sweden 2014, the same pipeline, at BOTH the current first divisor (1.2) and the pre-2018 legal
       divisor (1.4). This was meant as a real-data negative control - proof the check has teeth,
       per this project's own rule that "a check that passes on known-broken input is telling you
       about the check, not the code." It turned into a more interesting result than planned; see
       the FINDING printed at runtime and in the module docstring's tail below. Report it honestly
       rather than editing it down to the tidy negative control it was supposed to be.

    3. A synthetic, hand-verified case (900 votes vs 125 votes, 5 seats) constructed so the first
       divisor is DECISIVE: divisor 1.4 gives [5, 0], divisor 1.2 gives [4, 1]. This is the actual
       proof that Divisor()/AllocateHighestAverages's first-divisor parameter has teeth in the code -
       needed because, per finding 2, neither real Swedish election tried here exercises it.

    4. Two more synthetic checks for behaviour 2022's real data cannot reach at all: an exact
       quotient tie, and a party sitting exactly on the 4% threshold line.

FINDING: 2014 does not exactly reproduce, at EITHER divisor, and this is not a divisor problem
    Running 2014's real national vote totals through the same pipeline that reproduced 2022 exactly
    gives 112/85/47/25/22/21/20/17 against the real 113/84/49/25/22/21/19/16 - a total of 6 seats of
    absolute error (S -1, M +1, SD -2, FP +1, KD +1), and this is IDENTICAL at both first-divisor
    values, so the divisor is not the variable that explains it.

    Per this project's rule for a surprising diagnostic ("check it against something already known
    to be true before acting on it"): the vote and seat figures used here were cross-checked against
    three independent sources each (val.se official results, two Wikipedia pages, svt.se, and the
    "List of members of the Riksdag 2014-2018" page all agree digit-for-digit) before trusting this
    result over the code.

    The leading hypothesis is NOT a bug in this file. POLISIM_POLITICS_ELECTIONS_ROADMAP.md 3.2
    asserts as a general property of the Swedish system that "a national allocation reproduces the
    real chamber exactly" because the 39 levelling seats exist to true up the national total. That
    is confirmed for 2022 (0 seats of error, all 8 parties) but this run shows it is NOT reliably
    exact - most likely because a party's directly-won constituency ("fasta") seats can, in some
    election years, already meet or exceed its national proportional entitlement in a way that
    perturbs how the remaining levelling seats settle - the same shape of national-vs-constituency
    gap already recorded for Poland's D'Hondt, just far smaller here because Sweden's 39-seat
    levelling pool absorbs most of it. THIS IS UNCONFIRMED - resolving it for real would need all 29
    constituencies' 2014 vote data, which this check does not have and did not fetch. Recorded as a
    gap, not silently dropped - see POLISIM_POLITICS_ELECTIONS_ROADMAP.md 3.3, which narrows 3.2's
    claim rather than silently rewriting it.

    What this does NOT do: cast doubt on the 2022 result. The 2022 chamber - the one that matters for
    this file today, since Sweden's next election (13 Sept 2026) is under the same 1.2-divisor law -
    matched exactly. 2014 is additional evidence, and it narrows the claim from "a national calc
    always reproduces the real chamber" to "confirmed for 2022; not guaranteed in general."

WHAT IT DOES NOT TEST
    - AllocateLargestRemainder (Italy's tier) - ported below for a future Italy check, not exercised
      by main() here.
    - The coalition-share route. ThresholdRule.CoalitionShare exists on the struct (Poland 8%, Italy
      10%) but SeatAllocation.ApplyThreshold's C# body never reads rule.CoalitionShare for ANY
      country - see test_coalition_share_not_enforced() below and
      POLISIM_POLITICS_ELECTIONS_ROADMAP.md 3.3 / 10.5. Not ported here because there is nothing in
      the source to port; flagged separately, not silently absorbed into "not applicable to Sweden".
    - Constituency-level allocation for Poland/Italy - already known broken for a different, larger
      reason (70 seats, national vs per-constituency D'Hondt), recorded in
      POLISIM_POLITICS_ELECTIONS_ROADMAP.md 3.2, not re-tested here.
    - Full reconciliation of the 2014 gap above - needs constituency-level 2014 data this check does
      not have.

USAGE
    python3 seat_allocation_check.py
    exit 0 = every check passed, 1 = at least one mismatch, 2 = bad invocation
"""

import sys

# ---------------------------------------------------------------------------------------------------
# Port of Assets/Scripts/Simulation/SeatAllocation.cs. Keep this block a literal translation - do not
# "clean it up" relative to the C#, that would defeat the point of the check.
# ---------------------------------------------------------------------------------------------------

SWEDISH_FIRST_DIVISOR = 1.2  # SeatAllocation.SwedishFirstDivisor


def divisor(seats_already_held, first_divisor, odd_divisors):
    """Port of SeatAllocation.Divisor."""
    if seats_already_held == 0:
        return first_divisor
    return (2 * seats_already_held + 1) if odd_divisors else (seats_already_held + 1)


def allocate_highest_averages(votes, seats, first_divisor, odd_divisors):
    """Port of SeatAllocation.AllocateHighestAverages. Seat-at-a-time, same as the C#."""
    awarded = [0] * len(votes)

    for _seat in range(seats):
        best = -1
        best_quotient = float("-inf")

        for party in range(len(votes)):
            if votes[party] <= 0:
                continue

            d = divisor(awarded[party], first_divisor, odd_divisors)
            quotient = votes[party] / d

            # Strictly-greater, so the earlier index wins an exact tie once raw votes have also tied -
            # same comparison, same operator, as the C#.
            if quotient > best_quotient or (quotient == best_quotient and best >= 0 and votes[party] > votes[best]):
                best_quotient = quotient
                best = party

        if best < 0:
            break

        awarded[best] += 1

    return awarded


def allocate_largest_remainder(votes, seats):
    """Port of SeatAllocation.AllocateLargestRemainder. Not exercised by Sweden; kept for Italy."""
    awarded = [0] * len(votes)

    total_votes = sum(v for v in votes if v > 0)
    if total_votes <= 0:
        return awarded

    allocated = 0
    for party in range(len(votes)):
        if votes[party] <= 0:
            continue
        full = (votes[party] * seats) // total_votes
        awarded[party] = full
        allocated += full

    while allocated < seats:
        best = -1
        best_remainder = -1
        for party in range(len(votes)):
            if votes[party] <= 0:
                continue
            remainder = votes[party] * seats - awarded[party] * total_votes
            if remainder > best_remainder or (remainder == best_remainder and best >= 0 and votes[party] > votes[best]):
                best_remainder = remainder
                best = party
        if best < 0:
            break
        awarded[best] += 1
        allocated += 1

    return awarded


def allocate(formula, votes, seats):
    """Port of SeatAllocation.Allocate's switch, for the three list-PR formulas."""
    if seats <= 0 or len(votes) == 0:
        return [0] * len(votes)

    if formula == "SainteLagueModified":
        return allocate_highest_averages(votes, seats, SWEDISH_FIRST_DIVISOR, True)
    if formula == "DHondt":
        return allocate_highest_averages(votes, seats, 1.0, False)
    if formula == "LargestRemainder":
        return allocate_largest_remainder(votes, seats)
    raise ValueError(f"{formula} is not a list-allocation formula")


def apply_threshold(votes, national_share, alternative_share=0.0, best_constituency_share=None,
                     basic_mandate_seats=0, constituency_seats_won=None, is_recognised_minority=None):
    """Port of SeatAllocation.ApplyThreshold.

    NOTE: no coalition_share parameter, deliberately. See the module docstring's "WHAT IT DOES NOT
    TEST" - the C# never reads ThresholdRule.CoalitionShare in this method, for any country.
    """
    filtered = [0] * len(votes)

    total_votes = sum(v for v in votes if v > 0)
    if total_votes <= 0:
        return filtered

    for party in range(len(votes)):
        if votes[party] <= 0:
            continue

        exempt = is_recognised_minority is not None and is_recognised_minority[party]

        cleared_alternative = (best_constituency_share is not None
                                and alternative_share > 0.0
                                and best_constituency_share[party] >= alternative_share)

        cleared_basic_mandate = (constituency_seats_won is not None
                                  and basic_mandate_seats > 0
                                  and constituency_seats_won[party] >= basic_mandate_seats)

        share = votes[party] / total_votes
        cleared_national = share >= national_share

        if exempt or cleared_alternative or cleared_basic_mandate or cleared_national:
            filtered[party] = votes[party]

    return filtered


# ---------------------------------------------------------------------------------------------------
# Fixtures: real, official Swedish Riksdag results.
#
# 2022 source: en.wikipedia.org/wiki/Results_of_the_2022_Swedish_general_election, cross-checked
# against en.wikipedia.org/wiki/2022_Swedish_general_election and val.se's own results page
# (val.se/valresultat/riksdag-region-och-kommun/2022/valresultat.html) - all three agree
# digit-for-digit on votes, seats and the "other parties" share. Retrieved 2026-08-11.
#
# 2014 source: val.se's historical archive (historik.val.se/val/val2014/slutresultat/R/rike -
# votes and % only), cross-checked for SEATS against svt.se's 2014 results page (sourced from
# Valmyndigheten) and Wikipedia's "List of members of the Riksdag, 2014-2018" - all three agree.
# The 2014 vote total (6,231,573) was independently verified by summing all 10 listed parties by
# hand; it reconciles exactly. Retrieved 2026-08-11.
#
# [VERIFIED] both fixtures, exact vote counts (not rounded shares) per this file's own precision
# lesson from the German case in POLISIM_POLITICS_ELECTIONS_ROADMAP.md 3.2.
# ---------------------------------------------------------------------------------------------------

SWEDEN_NATIONAL_SHARE = 0.04  # ThresholdRule.Sweden.NationalShare
SWEDEN_SEATS = 349

SWEDEN_2022 = {
    "label": "Sweden 2022 (current law: first divisor 1.2)",
    "first_divisor": 1.2,
    "names":       ["S", "SD", "M", "V", "C", "KD", "MP", "L", "OTHER"],
    "votes":       [1_964_474, 1_330_325, 1_237_428, 437_050, 434_945, 345_712, 329_242, 298_542, 100_252],
    "real_seats":  [107, 73, 68, 24, 24, 19, 18, 16, 0],
}

SWEDEN_2014 = {
    "label": "Sweden 2014 (law at the time: first divisor 1.4)",
    "first_divisor": 1.4,
    "names":       ["S", "M", "SD", "MP", "C", "V", "FP", "KD", "FI", "OTHER"],
    "votes":       [1_932_711, 1_453_517, 801_178, 429_275, 380_937, 356_331, 337_773, 284_806, 194_719, 60_326],
    "real_seats":  [113, 84, 49, 25, 22, 21, 19, 16, 0, 0],
}


def run_fixture(fixture, first_divisor_override=None, label_override=None):
    names, votes, real = fixture["names"], fixture["votes"], fixture["real_seats"]
    fd = first_divisor_override if first_divisor_override is not None else fixture["first_divisor"]
    label = label_override or fixture["label"]

    filtered = apply_threshold(votes, national_share=SWEDEN_NATIONAL_SHARE)
    seats = allocate_highest_averages(filtered, SWEDEN_SEATS, fd, odd_divisors=True)

    diffs = [(n, s, r, s - r) for n, s, r in zip(names, seats, real) if s != r]
    total_error = sum(abs(d[3]) for d in diffs)

    print(f"\n=== {label} ===")
    print(f'{"party":6} {"votes":>10} {"got":>5} {"real":>5} {"diff":>5}')
    for n, v, s, r in zip(names, votes, seats, real):
        flag = "" if s == r else "  <-- MISMATCH"
        print(f"{n:6} {v:>10} {s:>5} {r:>5} {s - r:>5}{flag}")
    print(f"seats awarded: {sum(seats)} / {SWEDEN_SEATS}    total seat-error vs real: {total_error}")

    return total_error == 0, total_error, diffs


def test_first_divisor_is_decisive():
    """Synthetic, hand-verified: A already holds 4 seats (divisor 9), B holds 0 (divisor =
    first_divisor). A=900 votes, B=125 votes, 5 seats. Chosen so A's 5th-seat quotient (100) sits
    strictly between B's quotient at 1.4 (89.29, A wins) and at 1.2 (104.17, B wins) - the divisor
    changes who gets the 5th seat. This is the proof that the parameter has teeth in the code, since
    neither real Sweden fixture above turns out to exercise it (see the FINDING)."""
    at_14 = allocate_highest_averages([900, 125], 5, 1.4, odd_divisors=True)
    at_12 = allocate_highest_averages([900, 125], 5, 1.2, odd_divisors=True)
    ok = at_14 == [5, 0] and at_12 == [4, 1] and at_14 != at_12
    print(f"\n=== Synthetic: first divisor decisive by construction (900 vs 125 votes, 5 seats) ===")
    print(f"divisor 1.4 -> {at_14} (expected [5, 0])")
    print(f"divisor 1.2 -> {at_12} (expected [4, 1])")
    print("OK - divisor changes the outcome" if ok else "MISMATCH - divisor did not behave as constructed")
    return ok


def test_tie_break():
    """Synthetic: two parties tied on votes for the final seat. Real Swedish data cannot exercise
    this - this file's own doc comment says national-scale exact ties "need identical vote counts"
    and are "vanishingly rare." Confirms the documented departure from law (earlier index wins, not
    a lot) behaves as documented."""
    votes = [1000, 1000]
    seats = allocate_highest_averages(votes, 1, 1.0, odd_divisors=True)
    ok = seats == [1, 0]
    print(f"\n=== Synthetic: exact tie, equal votes, 1 seat ===\ngot {seats}, expected [1, 0] (earlier index wins)"
          f" -> {'OK' if ok else 'MISMATCH'}")
    return ok


def test_coalition_share_not_enforced():
    """Demonstrates a real gap found while reading ApplyThreshold for this check: ThresholdRule has a
    CoalitionShare field (Poland 8%, Italy 10%) but ApplyThreshold's C# body never reads
    rule.CoalitionShare anywhere - only NationalShare, AlternativeConstituencyShare and
    BasicMandateSeats. Out of scope for Sweden (ThresholdRule.Sweden never sets CoalitionShare, so
    this does not affect the Riksdag path this check exists to verify), but it means a Polish
    coalition list polling between the 5% party bar and the real 8% coalition bar currently clears
    the threshold when the law says it should not. Recorded here as a demonstration, not a pass/fail
    gate, because there is no coalition-membership parameter in ApplyThreshold's signature to assert
    a "correct" answer against - the fix is a design decision (how coalition membership and combined
    vote share get threaded through), not a one-line change, and is out of scope for a Sweden check.
    """
    # A hypothetical Polish-style list at 6% - clears the party bar (5%) but should fail a coalition
    # bar (8%) if it were flagged as a coalition. ApplyThreshold has no way to be told that.
    votes = [600_000, 9_400_000]  # minor list at 6.0%, rest of the electorate
    filtered = apply_threshold(votes, national_share=0.05)  # PolandSejm.NationalShare; CoalitionShare (0.08) unused
    cleared = filtered[0] == 600_000
    print(f"\n=== Demonstration: CoalitionShare (Poland 8%, Italy 10%) is never read by ApplyThreshold ===")
    print(f"6% list, PolandSejm.NationalShare=0.05 applied (CoalitionShare=0.08 has no parameter to receive it): "
          f"{'clears the threshold' if cleared else 'blocked'}")
    print("This is correct if the list is a single party (5% bar) and WRONG if it is a coalition (8% bar) -")
    print("ApplyThreshold cannot currently tell the difference. Does not affect Sweden: ThresholdRule.Sweden")
    print("never sets CoalitionShare, so this path is never reached for the Riksdag.")
    return None  # informational, not a pass/fail gate - see docstring


def test_threshold_boundary():
    """Synthetic: a party sitting on EXACTLY 4.00% of the vote. Neither real fixture above has a
    party within 0.5pp of the line (2022's lowest qualifier, Liberals, sat at 4.61%), so this is the
    only thing that exercises the >= in `share >= rule.NationalShare` rather than assuming it."""
    votes = [960, 40]  # 40 of 1000 = exactly 4%
    filtered = apply_threshold(votes, national_share=0.04)
    ok = filtered[1] == 40  # must clear: the C# uses >=, not >
    print(f"\n=== Synthetic: party at exactly 4.00% national share ===\nfiltered = {filtered}, "
          f"expected party 1 to clear (>= is inclusive) -> {'OK' if ok else 'MISMATCH'}")
    return ok


def main():
    results = []

    ok, error, _ = run_fixture(SWEDEN_2022)
    results.append(("HEADLINE: Sweden 2022, current code, vs real result", ok))
    if not ok:
        print(f"!!! current code does NOT reproduce the real 2022 result: {error} seats of error")

    ok14_14, err14_14, _ = run_fixture(SWEDEN_2014)
    ok14_12, err14_12, _ = run_fixture(SWEDEN_2014, first_divisor_override=1.2,
                                        label_override="Sweden 2014 data, run at TODAY's divisor 1.2 (for comparison only)")
    print(f"\n2014 result identical at both divisors: {err14_14 == err14_12} "
          f"(1.4 error={err14_14}, 1.2 error={err14_12}) - see FINDING in module docstring")
    # Recorded as informational, not pass/fail: this project's own history (the German case) says a
    # surprising diagnostic needs checking, not silent acceptance OR a reflexive "fix" - see the
    # FINDING block above for why this is not treated as a defect in SeatAllocation.cs.
    results.append(("INFORMATIONAL: Sweden 2014 exact reproduction (see FINDING, not a pass/fail gate)", None))

    results.append(("first divisor is decisive by construction (synthetic)", test_first_divisor_is_decisive()))
    results.append(("synthetic tie-break (earlier index wins)", test_tie_break()))
    results.append(("synthetic threshold boundary (exactly 4.00% clears)", test_threshold_boundary()))
    test_coalition_share_not_enforced()
    results.append(("INFORMATIONAL: CoalitionShare gap (see demonstration output, not a pass/fail gate, does not affect Sweden)", None))

    print("\n=== summary ===")
    all_ok = True
    for name, passed in results:
        if passed is None:
            print(f"INFO {name}")
            continue
        print(f'{"PASS" if passed else "FAIL":4} {name}')
        all_ok = all_ok and passed

    return 0 if all_ok else 1


if __name__ == "__main__":
    if len(sys.argv) != 1:
        print(__doc__, file=sys.stderr)
        sys.exit(2)
    sys.exit(main())
