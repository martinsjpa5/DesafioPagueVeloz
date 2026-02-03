
using System.Security.Cryptography;
using System.Text;

namespace Infraestrutura.Messaging.RabbitMq
{
    public static class ShardRouter
    {
        public static int CalculateShard(string key, int shardCount)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
            var value = BitConverter.ToInt32(bytes, 0) & int.MaxValue;
            return value % shardCount;
        }
    }
}
