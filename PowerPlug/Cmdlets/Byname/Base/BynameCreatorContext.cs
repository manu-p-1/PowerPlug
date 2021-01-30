namespace PowerPlug.Cmdlets.Byname.Base
{
    /// <summary>
    /// A BynameCreator context which invokes a <see cref="BynameCreatorStrategy"/> instance. The context
    /// is useful for invoking instances of a strategy design pattern.
    /// </summary>
    internal class BynameCreatorContext
    {
        /// <summary>
        /// The BynameCreatorStrategy instance to invoke.
        /// </summary>
        private BynameCreatorStrategy BynameCreatorStrategy { get; }

        /// <summary>
        /// Creates a new BynameCreatorContext given a BynameCreator strategy.
        /// </summary>
        /// <param name="bynameCreatorStrategy"></param>
        internal BynameCreatorContext(BynameCreatorStrategy bynameCreatorStrategy)
        {
            this.BynameCreatorStrategy = bynameCreatorStrategy;
        }

        /// <summary>
        /// Executes a BynameCreatorStrategy instance assigned to this instance.
        /// </summary>
        internal void ExecuteStrategy() => BynameCreatorStrategy.ExecuteCommand();
    }
}
