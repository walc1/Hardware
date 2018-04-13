namespace Shared
{
    public enum CommandType : byte
    {
        Relays = 0x01,
        RelayTime = 0x02,
        Beeper = 0x03,
        Inputs = 0x04,
        RequestInputs = 0x05,

        ReadI2C = 0x10,
        WriteI2C = 0x11,
        WriteReadI2C = 0x12,
        I2CData = 0x13,
        WriteReadSpi = 0x14,
        SpiData = 0x15,

        ReadFile = 0x20,
        WriteFile = 0x21,
        FileData = 0x22,
        FileEnd = 0x23,
        FileList = 0x24,
        RequestFileList = 0x25,
        DeleteFile = 0x26,

        DisplayText = DisplayCommand.Text,
        DisplayButton = DisplayCommand.Button,
        DisplayRectangle = DisplayCommand.Rectangle,
        DisplayLine = DisplayCommand.Line,
        DisplayImage = DisplayCommand.Image,
        TemplateMsg = 0x45,

        DisplayDeleteText = DisplayCommand.Text + 0x10,
        DisplayDeleteButton = DisplayCommand.Button + 0x10,
        DisplayDeleteRectangle = DisplayCommand.Rectangle + 0x10,
        DisplayDeleteLine = DisplayCommand.Line + 0x10,
        DisplayDeleteImage = DisplayCommand.Image + 0x10,

        DisplayInvalidate = 0x60,
        DisplayClearAll = 0x61,
        Backlight = 0x62,
        TouchButton = 0x63,
        RequestTouchButton = 0x64,

        ReadMedia = 0x70,
        RequestMedia = 0x71,
        PortRedirect = 0x72,
        PortRedirectCRT310 = 0x73,
        PortRedirectCRT310Answer = 0x74,
        DeviceState = 0x75,

        SystemInfo = 0x80,
        RequestSystemInfo = 0x81,
        Date = 0x82,
        RequestDate = 0x83,

        Reboot = 0xE0,

        Ack = 0xFE,
        Nack = 0xFF
    }
}