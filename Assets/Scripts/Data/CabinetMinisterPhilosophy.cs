namespace PoliSim.Data
{
    /// <summary>
    /// A cabinet minister's governing lean - unlike FedChairPhilosophy (where Hawkish/Dovish directly
    /// signs a single numeric RateBias), this determines which DECISION POOL a minister draws their
    /// interactive scenarios from (see CabinetSystem.DecisionPool) - "a Reformist vs. Hardline Interior
    /// Minister should generate genuinely different scenarios, not just a different number" (Master
    /// Roadmap, Part A). A minister's passive CabinetMinister.CompetenceBias is a SEPARATE axis (skill
    /// at the job, always beneficial in direction), intentionally not conflated with this one - a
    /// Traditionalist minister isn't "worse," just differently oriented.
    /// </summary>
    public enum CabinetMinisterPhilosophy
    {
        Reformist,
        Pragmatic,
        Traditionalist
    }
}
