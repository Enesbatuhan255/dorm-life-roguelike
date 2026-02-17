using System;
using System.Collections.Generic;

namespace DormLifeRoguelike
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, IService> Services = new Dictionary<Type, IService>();

        public static void Register<T>(T service) where T : class, IService
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            Services[typeof(T)] = service;
        }

        public static T Get<T>() where T : class, IService
        {
            if (!TryGet<T>(out var service))
            {
                throw new InvalidOperationException($"Service not registered: {typeof(T).Name}");
            }

            return service;
        }

        public static bool TryGet<T>(out T service) where T : class, IService
        {
            if (Services.TryGetValue(typeof(T), out var rawService) && rawService is T typedService)
            {
                service = typedService;
                return true;
            }

            service = null;
            return false;
        }

        public static void Unregister<T>() where T : class, IService
        {
            Services.Remove(typeof(T));
        }

        public static void Clear()
        {
            Services.Clear();
        }
    }
}
