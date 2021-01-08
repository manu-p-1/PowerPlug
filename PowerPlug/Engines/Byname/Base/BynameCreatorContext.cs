namespace PowerPlug.Engines.Byname.Base
{
    /// <summary>
    /// A BynameCreator context which invokes a <see cref="BynameCreatorStrategy"/> instance. The context
    /// is useful for invoking instances of a strategy design pattern.
    /// </summary>
    public class BynameCreatorContext
    {
        /// <summary>
        /// The BynameCreatorStrategy instance to invoke.
        /// </summary>
        private BynameCreatorStrategy BynameCreatorStrategy { get; }

        /// <summary>
        /// Creates a new BynameCreatorContext given a BynameCreator strategy.
        /// </summary>
        /// <param name="bynameCreatorStrategy"></param>
        public BynameCreatorContext(BynameCreatorStrategy bynameCreatorStrategy)
        {
            this.BynameCreatorStrategy = bynameCreatorStrategy;
        }

        /// <summary>
        /// Executes a BynameCreatorStrategy instance assigned to this instance.
        /// </summary>
        public void ExecuteStrategy() => BynameCreatorStrategy.ExecuteCommand();
    }
}
