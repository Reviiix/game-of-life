using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameOfLife.Core
{
    /// <summary>Global registry that holds exactly one instance of each game service so systems can find each other without scene searches.</summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> Services = new();

        /// <summary>Registers the single instance of a service type; logs an error if that type is already registered.</summary>
        public static void Register<TService>(TService service) where TService : class
        {
            var serviceType = typeof(TService);
            if (Services.ContainsKey(serviceType))
            {
                Debug.LogError($"{serviceType.Name} is already registered. Only one instance of each service is allowed.");
                return;
            }

            Services.Add(serviceType, service);
        }

        /// <summary>Returns the registered instance of a service type; throws if it has not been registered yet.</summary>
        public static TService Get<TService>() where TService : class
        {
            if (Services.TryGetValue(typeof(TService), out var service))
            {
                return (TService)service;
            }

            throw new InvalidOperationException($"{typeof(TService).Name} has not been registered. Register it in {nameof(GameInitialiser)} before it is used.");
        }

        /// <summary>Removes every registered service so a reloaded scene starts from a clean registry.</summary>
        public static void Clear()
        {
            Services.Clear();
        }

        /// <summary>Clears stale services when entering Play mode with domain reload disabled.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearOnPlayModeStart()
        {
            Clear();
        }
    }
}
