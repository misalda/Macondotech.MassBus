using CTM.EnterpriseBus.Common.Configuration;
using System;
using System.Reflection;
using Unity;
using MassTransit;
using Unity.Lifetime;
using CTM.EnterpriseBus.Conventions;
using System.Collections.Generic;

namespace CTM.EnterpriseBus.Unity.Extensions
{
    public static class CTMUnityRegistrationExtensions
    {
        public static void ReqisterRequestClients(this UnityContainer unityContainer, IReadOnlyDictionary<Type, string> clientEntries)
        {
            var registerRequestClientMethodInfo = typeof(CTMUnityRegistrationExtensions).GetMethod("RegisterRequestClient", new[] { typeof(IUnityContainer), typeof(string) });
            foreach (var entry in clientEntries)
            {
                MethodInfo genericMethod = registerRequestClientMethodInfo.MakeGenericMethod(entry.Key);
                genericMethod.Invoke(null, new object[] { unityContainer, entry.Value });
            }
        }
        /// <summary>
        /// Registers a Request client for a specific contract T using Unity's register factory 
        /// it uses a generic method that neets to be called at run tyme using reflection since the TYpe of T is not know until runtime.
        /// that it is what there are no References to this method directly 
        /// DO NOT DELETE!!
        /// more info here:
        /// https://stackoverflow.com/questions/232535/how-do-i-use-reflection-to-call-a-generic-method
        /// https://docs.microsoft.com/en-us/dotnet/api/system.reflection.methodinfo.makegenericmethod?redirectedfrom=MSDN&view=netcore-2.2#System_Reflection_MethodInfo_MakeGenericMethod_System_Type___
        /// </summary>
        /// <typeparam name="T">Request Client Type</typeparam>
        /// <param name="container"> Unity Container </param>
        /// <param name="endpointValue"></param>
        public static void RegisterRequestClient<T>(this IUnityContainer container,string endpointValue) where T : class
        {
            container.RegisterFactory<IRequestClient<T>>(c =>
            {
                var busControl = c.Resolve<IBusControl>();
                var busConfiguration = c.Resolve<BusConfiguration>();
                return busControl.CreateRequestClient<IRequestClient<T>>(new Uri($"{busConfiguration.AzureServiceBus.Uri}/{endpointValue}"), RequestTimeout.After(m: 10));
            });
        }
        public static void RegisterConsumer(this IReceiveEndpointConfigurator configurator, IUnityContainer unityContainer, Type[] entries)
        {
            var methodInfo = typeof(UnityExtensions).GetMethod("Consumer");
            foreach (Type cs in entries)
            {
                unityContainer.RegisterType(cs, new ContainerControlledLifetimeManager());
                MethodInfo genericMethod = methodInfo.MakeGenericMethod(cs);
                genericMethod.Invoke(configurator, new object[] { configurator, unityContainer, null });
            }
        }
    }
}
