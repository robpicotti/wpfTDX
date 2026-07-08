using System;
using System.Collections.Generic;
using System.Linq;

namespace wpfTDX
{
    /// <summary>
    /// Client-side mirror of the server's scale precedence (hoover/scale.py:
    /// specificity_sort_cols / _wildcard_rank). Given a proposed manual scale and
    /// the existing scaled_positions rows, works out whether the new scale will
    /// actually take effect for its fund, or be superseded by an existing scale.
    ///
    /// Precedence (higher wins): rank = 4*(ticker != '*') + 2*(fundname != '*')
    /// + 1*(fundgroupname != '*'); on a tie the LOWER scaled_target wins
    /// (most conservative). Only one scale applies per ticker for a given fund —
    /// scales never compound.
    /// </summary>
    public static class ScaleResolver
    {
        public class Effect
        {
            /// <summary>True if the proposed manual scale is the one that will apply for this fund.</summary>
            public bool NewApplies { get; set; }
            /// <summary>The existing scale that competes (either the one overridden, or the one that wins). Null if none.</summary>
            public ScaledPositionsDataModel Rival { get; set; }
            /// <summary>Human-readable description of the outcome, or null when there is no rival to warn about.</summary>
            public string Message { get; set; }
        }

        private static string Norm(string s)
        {
            return string.IsNullOrWhiteSpace(s) ? "*" : s.Trim();
        }

        // Wildcard-aware compatibility: equal, or either side is the '*' wildcard.
        private static bool Compatible(string a, string b)
        {
            return a == b || a == "*" || b == "*";
        }

        private static int Rank(string ticker, string fundGroup, string fundName)
        {
            return 4 * (ticker != "*" ? 1 : 0)
                 + 2 * (fundName != "*" ? 1 : 0)
                 + 1 * (fundGroup != "*" ? 1 : 0);
        }

        private static bool IsOne(double? v)
        {
            return v.HasValue && Math.Abs(v.Value - 1.0) < 1e-9;
        }

        private static string Pct(double? v)
        {
            return v.HasValue ? (v.Value * 100.0).ToString("0.#") + "%" : "?";
        }

        /// <summary>
        /// Resolve the effect of inserting a manual scale (fundGroup, fundName, ticker, target)
        /// against the current scaled_positions rows.
        /// </summary>
        public static Effect Resolve(string fundGroup, string fundName, string ticker,
                                     double newTarget, IEnumerable<ScaledPositionsDataModel> existing,
                                     string newType = "manual")
        {
            string typeLabel = string.IsNullOrWhiteSpace(newType) ? "manual" : newType.Trim().ToLower();
            var result = new Effect { NewApplies = true, Rival = null, Message = null };
            if (existing == null) return result;

            string fg = Norm(fundGroup);
            string fn = Norm(fundName);
            string tk = (ticker ?? "").Trim();
            int newRank = Rank(tk, fg, fn);

            // Rows that compete for this fund's scale, wildcard-aware.
            // Exclude the exact same (group, fund, ticker) key — a new insert there just
            // replaces it (newest runtime wins via select_latest), it is not a rival.
            // Exclude trivial 1/1 no-ops (they scale nothing).
            var rivals = existing.Where(r =>
                r != null &&
                Compatible(Norm(r.FundGroupName), fg) &&
                Compatible(Norm(r.FundName), fn) &&
                Compatible((r.TickerName ?? "").Trim(), tk) &&
                !(Norm(r.FundGroupName) == fg && Norm(r.FundName) == fn && (r.TickerName ?? "").Trim() == tk) &&
                !(IsOne(r.ScaledPercent) && IsOne(r.ScaledTarget))
            ).ToList();

            if (rivals.Count == 0) return result;   // nothing to conflict with

            // Strongest rival: highest rank, then lowest target.
            ScaledPositionsDataModel best = null;
            int bestRank = int.MinValue;
            double bestTarget = double.MaxValue;
            foreach (var r in rivals)
            {
                int rank = Rank((r.TickerName ?? "").Trim(), Norm(r.FundGroupName), Norm(r.FundName));
                double tgt = r.ScaledTarget ?? 1.0;
                if (rank > bestRank || (rank == bestRank && tgt < bestTarget))
                {
                    bestRank = rank;
                    bestTarget = tgt;
                    best = r;
                }
            }

            result.Rival = best;

            // New wins if it is more specific, or (on a specificity tie) not less conservative.
            bool newWins = newRank > bestRank || (newRank == bestRank && newTarget <= bestTarget);
            result.NewApplies = newWins;

            if (newWins)
            {
                result.Message =
                    "This " + typeLabel + " scale WILL take effect and take precedence over the existing " +
                    best.ScaleType + " scale (target " + Pct(best.ScaledTarget) + ") for this fund.";
            }
            else
            {
                string why = bestRank > newRank
                    ? "is more fund-specific"
                    : "is more conservative (lower target)";
                result.Message =
                    "This " + typeLabel + " scale will be stored but WILL NOT take effect: the existing " +
                    best.ScaleType + " scale (target " + Pct(best.ScaledTarget) + ") " + why +
                    " and takes precedence for this fund.";
            }

            return result;
        }
    }
}
