using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infraestrutura.Messaging.RabbitMq
{
    public sealed class RabbitConnection : IRabbitConnection
    {
        private readonly RabbitMqOptions _opt;
        private readonly ConnectionFactory _factory;

        private readonly object _sync = new();
        private IConnection? _connection;

        public RabbitConnection(RabbitMqOptions opt)
        {
            _opt = opt;

            _factory = new ConnectionFactory
            {
                HostName = opt.HostName,
                Port = opt.Port,
                UserName = opt.UserName,
                Password = opt.Password,
                VirtualHost = opt.VirtualHost,

                DispatchConsumersAsync = true,

                // Recovery
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
            };

            if (opt.UseSsl)
            {
                _factory.Ssl = new SslOption
                {
                    Enabled = true,
                    ServerName = opt.HostName
                };
            }
        }

        public IConnection GetConnection()
        {
            lock (_sync)
            {
                if (_connection is { IsOpen: true })
                    return _connection;

                _connection?.Dispose();
                _connection = _factory.CreateConnection();
                return _connection;
            }
        }

        public IModel CreateChannel()
        {
            var conn = GetConnection();
            return conn.CreateModel();
        }

        public void Dispose()
        {
            lock (_sync)
            {
                _connection?.Dispose();
                _connection = null;
            }
        }
    }
}