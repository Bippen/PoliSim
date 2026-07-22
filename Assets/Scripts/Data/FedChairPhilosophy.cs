namespace PoliSim.Data
{
    /// <summary>
    /// A FedChair's monetary-policy lean, applied as a bias on top of TaylorRule's suggested rate -
    /// see FedChair.RateBias. Hawkish effectively overweights the inflation gap (positive bias,
    /// tighter policy); Dovish effectively overweights the output/employment gap (negative bias,
    /// looser policy); Moderate tracks TaylorRule closely (bias near 0).
    /// </summary>
    public enum FedChairPhilosophy
    {
        Hawkish,
        Moderate,
        Dovish
    }
}
