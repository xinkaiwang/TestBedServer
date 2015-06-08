using AustinHarris.JsonRpc;
using Catty.Bootstrap;
using Catty.Core;
using Catty.Core.Channel;
using Catty.Core.Handler.Codec;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace JsonRpcCatty
{
    class Program
    {
        static JsonRpcService service = new ExampleCalculatorService();

        static void Main(string[] args)
        {
            BasicConfigurator.Configure();

            Func<IChannelHandler[]> handlersFactory = () => new IChannelHandler[] { new LineBreakDecoder(), new JsonRpcHandler() };
            var server = new SimpleTcpService().SetHandlers(handlersFactory);
            server.Bind(new IPEndPoint(IPAddress.Any, 8002));
            Console.WriteLine("server started ...");
            new CtrlCListener().WaitForEvent();
            Console.WriteLine("server exiting ....");
        }
    }

    public class JsonRpcHandler : SimpleChannelUpstreamHandler
    {
        public override void MessageReceived(
                IChannelHandlerContext ctx, IMessageEvent e)
        {
            string line = e.GetMessage() as string;
            if (line != null)
            {
                IChannel channel = ctx.GetChannel();
                AsyncCallback handler = o =>
                {
                    channel.Write(((JsonRpcStateAsync)o).Result);
                };
                var async = new JsonRpcStateAsync(handler, null);
                async.JsonRpc = line;
                JsonRpcProcessor.Process(async);
            }
        }
    }

    public class ExampleCalculatorService : JsonRpcService
    {
        [JsonRpcMethod]
        private double add(double l, double r)
        {
            return l + r;
        }
    }
}
