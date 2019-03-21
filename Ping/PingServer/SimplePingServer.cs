using System;
using Trinity;

namespace PingServer 
{
  class SimplePingServer: PingServerBase {

    public override void SynPingHandler(PingMessageReader request) {
      Console.WriteLine("Received SynPing: {0}", request.Message);
    }

    public override void SynEchoPingHandler(PingMessageReader request, PingMessageWriter response) {
      Console.WriteLine("Received SynEchoPing: {0}", request.Message);
      response.Message = request.Message;
    }

    public override void AsynPingHandler(PingMessageReader request) {
      Console.WriteLine("Received AsynPing: {0}", request.Message);
    }

  }
}
