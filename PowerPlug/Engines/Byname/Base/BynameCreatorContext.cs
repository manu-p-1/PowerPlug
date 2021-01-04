namespace PowerPlug.Engines.Byname.Base
{
    public class BynameCreatorContext
    {
        private BynameCreatorStrategy BynameCreatorStrategy { get; }

        public BynameCreatorContext(BynameCreatorStrategy bynameCreatorStrategy)
        {
            this.BynameCreatorStrategy = bynameCreatorStrategy;
        }

        public void ExecuteStrategy() => BynameCreatorStrategy.ExecuteCommand();
    }
}
