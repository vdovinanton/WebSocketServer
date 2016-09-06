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
        public string Type { get; set; }
        public string Data { get; set; }
    }

    public class Customer
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public int Id { get; set; }
        public decimal Price { get; set; }
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

        private readonly WebSocketServer _server;
        private readonly ICollection<IWebSocketConnection> _allSockets;

        private FleckServer()
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
                socket.OnMessage = Send;
            });
        }

        /// <summary>
        /// Send to all subscribes
        /// </summary>
        /// <param name="json">Serializable object</param>
        public void Send(string json)
        {
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var data = (JObject)JsonConvert.DeserializeObject(json);
                    var typeName = data["type"].Value<string>();
                    var jsonData = data["data"].Value<string>();


                    var m = this.GetType().GetMethods().FirstOrDefault(x => x.Name == typeName);
                    var p = m.GetParameters().FirstOrDefault();
                    var type = p.ParameterType;

                    //var type = GetTypeByName(typeName);

                    var desObj = JsonConvert.DeserializeObject(jsonData, type);

                    m.Invoke(this, new[] {desObj});

                    _allSockets.ToList().ForEach(s => s.Send(json));
                }
                catch (Exception e)
                {
                    // ignored
                }
            }
        }

        public void Customer(Customer cust)
        {
            
        }

        /// <summary>
        /// Send by subscribe id
        /// </summary>
        /// <param name="id">Subscribe <see cref="Guid"/> id</param>
        /// <param name="json">Serializable object</param>
        public void Send(Guid id, string json)
        {
            Debug.WriteLine($"{json} to {id}");
            var socket = _allSockets.ToList().FirstOrDefault(q => q.ConnectionInfo.Id == id);
            socket?.Send(json);
        }

        public void Dispose()
        {
            _server.Dispose();
            _allSockets.Clear();
        }

        private static Type[] GetTypeByName(string className)
        {
            var returnVal = new List<Type>();

            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                var assemblyTypes = a.GetTypes();
                returnVal.AddRange(assemblyTypes.Where(t => t.Name == className));
            }

            return returnVal.ToArray();
        }
    }
}