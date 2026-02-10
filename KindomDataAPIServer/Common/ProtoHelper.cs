using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KindomDataAPIServer.Common
{
    public class ProtoHelper
    {

        public static T FromStream<T>(Stream s) where T : IMessage, IMessage<T>, new()
        {
            var parser = new MessageParser<T>(() => new T());
            return parser.ParseFrom(s);
        }

        public static Stream ToMemoryStream<T>(T obj) where T : IMessage, IMessage<T>
        {
            var data = new MemoryStream(obj.CalculateSize());
            using (var output = new CodedOutputStream(data, true))
            {
                obj.WriteTo(output);
                output.Flush();
                data.Seek(0, SeekOrigin.Begin);
            }
            return data;

        }

    }
}
