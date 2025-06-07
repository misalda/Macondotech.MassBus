using MassTransit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace CTM.EnterpriseBus.Conventions
{
    public class DefaultNamespaceResolver : INamespaceResolver
    {
        private const string CTM_ROOT_NAMESPACE = "CTM";
        private const string CTM_CONTRACT_NAMESPACE = "CTM.EnterpriseBus.Contracts";
        private readonly string _rootNamespace;
        private readonly string _contractNameSpace;
        private readonly string _defaultEndpointName;
        private readonly IEnumerable<Assembly> _assemblies;

        public DefaultNamespaceResolver(string rootNameSpace = null, string contractNameSpace = null, string defaultEndpointName = null)
        {
            _rootNamespace = rootNameSpace ?? CTM_ROOT_NAMESPACE;
            _contractNameSpace = contractNameSpace ?? CTM_CONTRACT_NAMESPACE;
            _defaultEndpointName = defaultEndpointName ?? GenerateDefaultEndpoint();
            LoadAssemblies();
            //get all the relevant assemblies
            _assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName.StartsWith(_rootNamespace, StringComparison.InvariantCultureIgnoreCase));

        }
        private string GenerateDefaultEndpoint()
        {
            try
            {
                return Assembly.GetEntryAssembly().DefinedTypes.Where(dt => dt.FullName.StartsWith(_rootNamespace, StringComparison.InvariantCultureIgnoreCase)).First().Namespace;
            }
            catch
            {
                return $"ctm.enterprisebus.{Guid.NewGuid()}";
            }

        }
        public string GetContractTopicPath(Type type)
        {
            return $"{type.Namespace}/{type.Name}".ToLower();
        }
        public string GetContractEndpointName(Type type)
        {
            return $"{type.FullName}".ToLower();
        }
        private void LoadAssemblies()
        {
            string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var files = Directory.GetFiles(directoryName ?? throw new InvalidOperationException(), $"{_rootNamespace}*").Where(f=> f.EndsWith(".dll"));
            foreach (string assemblyFile in files)
            {
                try
                {
                    Assembly.LoadFrom(assemblyFile);
                }
                catch {
                    continue;
                    //ignore assembly file
                }
            }
        }
        public TopologyMap BuildTopology()
        {
            //map each contract with its consumer             
            var topologyMap = new TopologyMap()
            {
                EventConsumers = GetConsumerEntries(DefaultContractTypes.Events),
                MessageConsumers = GetConsumerEntries(DefaultContractTypes.Messages),
                RequestConsumers = GetConsumerEntries(DefaultContractTypes.Requests),
                SendEndpoints = GetClientEndpoints(DefaultContractTypes.Messages),
                RequestClientEndpoints = GetClientEndpoints(DefaultContractTypes.Requests),
                DefaultEndPoint = _defaultEndpointName
            };
            return topologyMap;
        }
        private IList<ConsumerEntry> GetConsumerEntries(string contractType)
        {
            //get all the types that define contracts according to the DefaultContract Types
            IList<Type> messageTypes = GetContractTypes(contractType);

            var consumerEntries = new List<ConsumerEntry>(0);
            foreach (var messageType in messageTypes)
            {
                var entry = new ConsumerEntry() { ContractClassType = messageType };
                entry.ConsumerClassTypes = _assemblies.SelectMany((assembly) => GetLoadableTypes(assembly)).Where(dt => dt.GetInterfaces().Any(gi => gi.IsGenericType && gi.GetGenericTypeDefinition().Equals(typeof(IConsumer<>)) && gi.UnderlyingSystemType.GetGenericArguments()[0] == messageType));
                entry.ReceiveEndPoint = contractType.Equals(DefaultContractTypes.Events) ? GetContractTopicPath(messageType) : GetContractEndpointName(messageType);
                consumerEntries.Add(entry);
            }

            return consumerEntries;
        }
        private IReadOnlyDictionary<Type, string> GetClientEndpoints(string contractType)
        {
            //get all the types that define contracts according to the DefaultContract Types
            IList<Type> messageTypes = GetContractTypes(contractType);

            var endpointEntries = new Dictionary<Type,string>(0);
            foreach (var messageType in messageTypes)
            {
                endpointEntries.Add(messageType, GetContractEndpointName(messageType));
            }

            return endpointEntries;
        }
        private IList<Type> GetContractTypes(string contractType)
        {
            return (from type in _assemblies.SelectMany((assembly) => GetLoadableTypes(assembly))
                    where type.Namespace != null && type.Namespace.EndsWith(contractType) && type.Namespace.StartsWith(_contractNameSpace, StringComparison.InvariantCultureIgnoreCase)
                    select type).ToList();
        }
        public IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            // Algorithm from StackOverflow answer here:
            // https://stackoverflow.com/questions/7889228/how-to-prevent-reflectiontypeloadexception-when-calling-assembly-gettypes
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            try
            {
                return assembly.DefinedTypes.Select(t => t.AsType());
            }
            catch (ReflectionTypeLoadException ex)
            {
                var result = ex.Types.Where(type => type != null &&
                Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
                && type.IsGenericType && type.Name.Contains("AnonymousType")
                && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
                && type.Attributes.HasFlag(TypeAttributes.NotPublic));
                return result;
            }
        }
    }
}
