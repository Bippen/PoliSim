using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §496 (2026-09-14): THE NO-POLICY TRAJECTORY'S SENTINEL - the first <see cref="Turns"/> turns of the trajectory dump's own text
    /// (<see cref="TrajectoryBaselineDump.Build"/>, the same builder the dump writes from), at both seeds, hashed and held against the
    /// digests of the declared baseline. §490 moved the no-policy trajectory from its SECOND turn - Germany's revenue left § 32a's own
    /// arithmetic for the ramps derived from it - and no bar saw it: the pass was filmed, not dumped, and `ArtifactIdentityCheck` verifies
    /// the trajectory files already on disk, never a rerun. A change that moves this text is either a family - dump it, explain it per
    /// country, and set <see cref="BaselineLabel"/> and the digests here in the same commit - or a numeric-inertness break (§402).
    /// First in the simulation group, so no other check's static state can reach it before it runs.
    /// </summary>
    public static class TrajectorySentinelCheck
    {
        /// <summary>The baseline these digests are the first turns of: `traj_t3a` - T-3 form A, the identity's G at the sourced share (§507) (was `traj_f4s2`).</summary>
        public const string BaselineLabel = "t3a";

        /// <summary>CONVENTION: twenty turns - §490's divergence showed on turn 2, and the pair of runs costs seconds.</summary>
        public const int Turns = 20;

        /// <summary>SHA-256 of the dump's text through turn 20 (the header and every row of turns 1-20), read off `traj_t3a_s{seed}_t100.csv`.</summary>
        private static readonly (int Seed, string Sha256)[] Expected =
        {
            (777, "f9f1a59f26212b7d0ba43d16c85bfb2c4400a56fd0453a72911ea44f37278ff3"),
            (424242, "f84ce6898313519671f4e229f02c22195a52e5e6725bc1b40c8fc0b535352d31"),
        };

        public static void Run()
        {
            CheckExit.ArmLogFold();
            FieldInfo[] fields = TrajectoryBaselineDump.StateFields();
            bool ok = true;
            var sb = new StringBuilder();
            sb.Append($"=== TRAJECTORY SENTINEL: the first {Turns} turns of the no-policy dump against baseline '{BaselineLabel}' ===\n");
            foreach ((int seed, string expected) in Expected)
            {
                string got = Sha256(TrajectoryBaselineDump.Build(seed, Turns, fields));
                bool same = got == expected;
                sb.Append($"    seed {seed}: {got} {(same ? "- the baseline's" : "- NOT the baseline's " + expected)}\n");
                if (!same)
                {
                    ok = false;
                    Debug.LogError($"TRAJECTORY SENTINEL: seed {seed}'s first {Turns} no-policy turns moved off baseline '{BaselineLabel}' - a family (dump it, explain it per country, set the label and the digests here in the same commit) or a numeric-inertness break.");
                }
            }
            Debug.Log(sb.ToString());
            Debug.Log(ok ? "=== TrajectorySentinelCheck: ALL ASSERTIONS PASS ===" : "=== TrajectorySentinelCheck: FAILED ===");
            CheckExit.Finish(ok ? 0 : 1);
        }

        private static string Sha256(string text)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(new UTF8Encoding(false).GetBytes(text));
                var hex = new StringBuilder(64);
                foreach (byte b in hash) { hex.Append(b.ToString("x2")); }
                return hex.ToString();
            }
        }
    }
}
