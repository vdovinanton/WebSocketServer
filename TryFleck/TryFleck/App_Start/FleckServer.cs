using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Fleck;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TryFleck
{
    public class SomeClass
    {
        public string Method { get; set; }
        public string Data { get; set; }
    }

    /// <summary>
    /// Represent data provider between <see cref="FleckServer"/> and <see cref="Hub"/> heirs
    /// </summary>
    public class Hub
    {
        /// <summary>
        /// Constructor withoud parameters, he signs derived types
        /// </summary>
        public Hub()
        {
            var res = FindAllDerivedTypes<Hub>();
            res.ForEach(q => FleckServer.Instance().Subscribe(q));
        }

        private static List<Type> FindAllDerivedTypes<T>()
        {
            return FindAllDerivedTypes<T>(Assembly.GetAssembly(typeof(T)));
        }
        private static List<Type> FindAllDerivedTypes<T>(Assembly assembly)
        {
            var derivedType = typeof(T);
            return assembly
                .GetTypes()
                .Where(t => t != derivedType && derivedType.IsAssignableFrom(t)).ToList();
        }
    }

    /// <summary>
    /// Fleck source https://github.com/statianzo/Fleck
    /// Troubles with running http://stackoverflow.com/questions/9177089/running-fleck-or-any-websocket-server-on-windows-azure?rq=1
    /// </summary>
    public class FleckServer: IDisposable
    {
        #region Singleton
        private static readonly Lazy<FleckServer> _instance = new Lazy<FleckServer>(() => new FleckServer());
        public static FleckServer Instance() => _instance.Value;
        #endregion

        protected MethodInfo Methood { get; set; }

        private readonly WebSocketServer _server;
        private readonly ICollection<IWebSocketConnection> _allSockets;
        private readonly IList<Type> _subscribes = new List<Type>();
        public void Subscribe(Type obj)
        {
            _subscribes.Add(obj);
        }

        protected FleckServer()
        {
            FleckLog.Level = LogLevel.Debug;
            _allSockets = new List<IWebSocketConnection>();
            _server = new WebSocketServer("ws://0.0.0.0:8181") { RestartAfterListenError = true };
        }
        
        public void Start()
        {
            _server.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    Debug.WriteLine($"Connect: {socket.ConnectionInfo.Id}");
                    _allSockets.Add(socket);
                };
                socket.OnClose = () =>
                {
                    Debug.WriteLine($"Disconnect: {socket.ConnectionInfo.Id}");
                    _allSockets.Remove(socket);
                };
                socket.OnMessage = OnMessage;
            });
        }

        /// <summary>
        /// Send to all subscribes
        /// </summary>
        /// <param name="json">Serializable object</param>
        public void OnMessage(string json)
        {
            if (!string.IsNullOrEmpty(json))
            {
                var data = (JObject)JsonConvert.DeserializeObject(json);
                var methodName = data["Method"].Value<string>();
                var jsonData = data["Data"].Value<string>();

                // get desired method
                var currentType = _subscribes.FirstOrDefault(q => q.GetMethods().Any(x => x.Name == methodName));
                var method = currentType?.GetMethod(methodName);

                // get parameter method type
                var methodParameter = method?.GetParameters().FirstOrDefault();
                var methodParameterType = methodParameter?.ParameterType;

                if (methodParameter == null) return;

                var desObj = JsonConvert.DeserializeObject(jsonData, methodParameterType);

                // call the method with parameter
                var instance = Activator.CreateInstance(currentType);
                method.Invoke(instance, new[] { desObj });
            }
        }

        public void Dispose()
        {
            _server.Dispose();
            _allSockets.Clear();
        }
    }
}