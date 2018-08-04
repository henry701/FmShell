namespace FmShell
{
    /// <summary>
    /// Used as arguments for the methods which the shell invokes.
    /// </summary>
    /// <threadsafety static="true" instance="true"/>
    public sealed class FmShellArguments
    {
        public Shell Shell { get; private set; }
        public object[] Args { get; private set; }

        public FmShellArguments(Shell shell, object[] args)
        {
            Shell = shell;
            Args = args;
        }
    }
}
