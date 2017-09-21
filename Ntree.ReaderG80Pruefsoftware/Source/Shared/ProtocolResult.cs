namespace Shared
{
    public enum ProtocolResult
    {
        None = 0,
        Ack,
        AckAck,
        FileEnd,
        CrcError,
        EventError,
        IncorrectLength,
        InvalidParameter,
        UnknownCommand,
        FileNotFound,
        Timeout,
        UnknownError,
        ReaderNotConfigured,
        PortRedirectTimeout,
        I2CError
    }
}