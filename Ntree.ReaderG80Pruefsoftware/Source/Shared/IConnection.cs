namespace Shared
{
    public delegate void MessageReceived(object sender, byte[] data);

    public interface IConnection
    {
        event MessageReceived MessageReceived;
        bool IsConnected { get; }
        string Name { get; }
        bool Send(byte[] data);
        byte[] SendReceive(byte[] data, int timoutMs = 1000);
        bool Connect();
        bool Disconnect();
    }
}