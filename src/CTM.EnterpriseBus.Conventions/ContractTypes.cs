namespace CTM.EnterpriseBus.Conventions
{
    internal class DefaultContractTypes
    {
        internal const string Events = "Events";

        internal const string Messages = "Messages";

        internal const string Requests = "Requests";

        internal static string[] Namespaces = new string[3]
        {
            Requests,
            Events,
            Messages
        };
    }

}
