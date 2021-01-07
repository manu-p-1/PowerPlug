using System.Management.Automation;

namespace PowerPlug.BaseCmdlets
{
    /// <summary>
    /// Represents the base structure of a Byname. All Byname cmdlet's stem from PSCmdlet.
    /// </summary>
    public abstract class BynameBase : PSCmdlet
    {
        /// <summary>
        /// The Name Property of the Byname. Every Byname contains a Name property, regardless of creating a new Byname,
        /// setting an existing Byname, or removing a Byname.
        /// </summary>
        public abstract string Name { get; set; }
    }
}