namespace TruckManagement.Enums
{
    /// <summary>
    /// Represents the lifecycle status of a contract version.
    /// </summary>
    public enum ContractVersionStatus
    {
        /// <summary>
        /// Contract generated but not yet finalized.
        /// </summary>
        Draft = 0,

        /// <summary>
        /// PDF created and ready for review/signing.
        /// </summary>
        Generated = 1,

        /// <summary>
        /// Contract has been signed by the driver.
        /// </summary>
        Signed = 2,

        /// <summary>
        /// A newer version exists, this one is no longer current.
        /// </summary>
        Superseded = 3,

        /// <summary>
        /// Old version, kept for historical records only.
        /// </summary>
        Archived = 4
    }
}

