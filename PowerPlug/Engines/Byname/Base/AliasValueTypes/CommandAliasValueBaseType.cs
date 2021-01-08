using PowerPlug.BaseCmdlets;

namespace PowerPlug.Engines.Byname.Base.AliasValueTypes
{
    /// <summary>
    /// The base representation of the Value of a cmdlet command. The value types are used by
    /// the <see cref="WritableBynameCreatorBaseOperation"/> to detect the type of the value of the command string.
    /// Ideally, this representation should be changed to the respective type supported by the PowerShell Standard
    /// Library in the future. For more information on how to contribute to PowerPlug, visit the GitHub link.
    /// </summary>
    internal abstract record CommandAliasValueBaseType
    {
        /// <summary>
        /// The WritableByname instance.
        /// </summary>
        protected WritableByname AliasCmdlet { get; }

        /// <summary>
        /// Sets the variable for the WritableByname instance.
        /// </summary>
        /// <param name="cmdlet"></param>
        protected CommandAliasValueBaseType(WritableByname cmdlet)
        {
            AliasCmdlet = cmdlet;
        }
    }
}