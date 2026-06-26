using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Сервис-локатор (ТЗ §6 — убрать прямые зависимости через FindObjectOfType).
    /// Менеджеры/сервисы регистрируются здесь при старте (через Bootstrap или Singleton),
    /// а потребители запрашивают их по типу. Это разрывает жёсткую связанность:
    /// потребитель знает интерфейс/тип, но не способ его поиска на сцене.
    ///
    /// ВАЖНО: реестр живёт между сценами. Сервисы, привязанные к сцене (Player, Room*),
    /// обязаны Unregister в OnDestroy/OnDisable, иначе останутся «висячие» ссылки.
    /// </summary>
    public static class ServiceLocator
    {
        static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        // В Editor статика переживает выход из Play (если выключен Reload Domain).
        // Чистим реестр при старте игры, чтобы не остались ссылки на уничтоженные объекты.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay() => _services.Clear();

        public static void Register<T>(T service) where T : class
        {
            if (service == null) return;
            _services[typeof(T)] = service;
        }

        /// <summary>Снять регистрацию только если текущий сервис — это именно он.</summary>
        public static void Unregister<T>(T service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out var cur) && ReferenceEquals(cur, service))
                _services.Remove(typeof(T));
        }

        public static T Get<T>() where T : class
            => _services.TryGetValue(typeof(T), out var s) ? s as T : null;

        public static bool TryGet<T>(out T service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out var s) && s is T typed)
            {
                service = typed;
                return true;
            }
            service = null;
            return false;
        }

        public static void Clear() => _services.Clear();
    }
}
