using System;
using System.Text;

// ReSharper disable All

namespace Shared
{
    public class Protocol
    {
        private readonly ILogger _logger;
        private readonly ushort _displayWidth;
        private readonly ushort _displayHeight;
        public const int FILE_BUFFER_SIZE = 256;
        public const byte END = 0x03;
        public const byte BACKLIGHT_ON = 0x7F;
        public const byte BACKLIGHT_OUTOFORDER = 0x0A;

        public delegate void DelegateSenderOnly(object sender);
        public delegate void DelegateByte(object sender, byte id);
        public delegate void DelegateMediaRead(object sender, byte readerId, byte[] cardData);
        public delegate void DelegateRelay(object sender, Relay[] relais);
        public delegate void DelegateInput(object sender, bool[] inputStates);
        public delegate void DelegateBoolean(object sender, bool state);
        public delegate void DelegateDisplayText(object sender, DisplayTextInfo displayTextInfo);
        public delegate void DelegateButton(object sender, ButtonInfo buttonInfo);
        public delegate void DelegateRectange(object sender, RectangleInfo rectangleInfo);
        public delegate void DelegateLine(object sender, LineInfo lineInfo);
        public delegate void DelegateColor(object sender, ColorInfo color);
        public delegate void DelegateDisplayImage(object sender, ImageInfo imageInfo);
        public delegate void DelegateTemplateMessage(object sender, string text1, string text2, string text3, string imageFilename, bool lockReading);
        public delegate void DelegateDeleteDisplayItem(object sender, DisplayCommand type, byte index);
        public delegate ProtocolResult DelegateFile(object sender, string filename);
        public delegate void DelegateDateTime(object sender, DateTime dateTime);
        public delegate void DelegateBeeper(object sender, double time);
        public delegate void DelegateSystemInfo(object sender, Version version, byte[] macAddress);
        public delegate void DelegateFileList(object sender, string[] files);
        public delegate void DelegatePortRedirect(object sender, byte readerId, ushort timeout, ushort readLenght, byte[] writeData);
        public delegate void DelegatePortRedirectCRT310(object sender, byte readerId, ushort timeout, CRT310ResultLengthType readLenghtType, byte[] writeData);
        public delegate void DelegatePortRedirectCRT310Answer(object sender, byte readerId, byte[] answer);
        public delegate void DelegateDeviceStateChanged(object sender, byte readerId, DeviceState state);
        public delegate void DelegateCommandChanged(object sender, byte id, Command command);
        public delegate ProtocolResult DelegateI2CWrite(object sender, byte address, byte[] data);
        public delegate void DelegateI2CRead(object sender, byte address, int length);
        public delegate void DelegateI2CWriteRead(object sender, byte address, byte[] data, int readLength);
        public delegate void DelegateSpiWriteRead(object sender, SpiChipSelect cs, SpiSpeed speed, bool csActiveState, bool clockIdle, bool clockEdge, byte[] data);

        public event DelegateMediaRead MediaRead;
        public event DelegateByte RequestMediaRead;
        public event DelegateRelay SwitchRelais;
        public event DelegateBeeper ActivateBeeper;
        public event DelegateInput InputChanged;
        public event DelegateSenderOnly RequestInputs;
        public event DelegateByte TouchButton;
        public event DelegateSenderOnly RequestTouchButton;
        public event DelegateDisplayText DisplayTextChange;
        public event DelegateButton DisplayButtonChange;
        public event DelegateRectange DisplayRectangleChange;
        public event DelegateLine DisplayLineChange;
        public event DelegateColor DisplayClearAll;
        public event DelegateSenderOnly DisplayInvalidate;
        public event DelegateDisplayImage DisplayImageChange;
        public event DelegateTemplateMessage TemplateMessageChange;
        public event DelegateDeleteDisplayItem DeleteDisplayItem;
        public event DelegateFile ReadFile;
        public event DelegateFile WriteFile;
        public event DelegateFile DeleteFile;
        public event DelegateDateTime DateTimeChange;
        public event DelegateSenderOnly Reboot;
        public event DelegateSenderOnly RequestDateTime;
        public event DelegateSenderOnly RequestFileList;
        public event DelegateFileList FileListChanged;
        public event DelegateSenderOnly RequestSystemInfo;
        public event DelegateSystemInfo SystemInfoChanged;
        public event DelegatePortRedirect PortRedirect;
        public event DelegatePortRedirectCRT310 PortRedirectCRT310;
        public event DelegatePortRedirectCRT310Answer PortRedirectCRT310Answer;
        public event DelegateDeviceStateChanged DeviceStateChanged;
        public event DelegateCommandChanged CommandChanged;
        public event DelegateByte BacklightChange;
        public event DelegateI2CWrite I2CWriteData;
        public event DelegateI2CRead I2CReadData;
        public event DelegateI2CWriteRead I2CWriteReadData;
        public event DelegateSpiWriteRead SpiWriteRead;

        public Protocol(ILogger logger)
            : this(logger, 0, 0)
        {
        }

        public Protocol(ILogger logger, ushort displayWidth, ushort displayHeight)
        {
            _logger = logger;
            _displayWidth = displayWidth;
            _displayHeight = displayHeight;
        }

        public ProtocolResult Parse(byte[] data, out byte index, out byte[] resultData)
        {
            resultData = null;
            index = 0;
            if (data.Length > 1)
            {
                var commandType = (CommandType)data[0];

                if (!CheckLength(commandType, data))
                {
                    _logger.LogError("Incorrect length @command: " + commandType.ToString());
                    return ProtocolResult.IncorrectLength;
                }

                switch (commandType)
                {
                    case CommandType.Relays:
                        return HandleRelays(data);
                    case CommandType.RelayTime:
                        {
                            return HandleRelaisTime(data);
                        }
                    case CommandType.Beeper:
                        {
                            return HandleBeeper(data);
                        }
                    case CommandType.Inputs:
                        {
                            return HandleInputs(data);
                        }
                    case CommandType.RequestInputs:
                        {
                            return HandleRequestInput();
                        }
                    case CommandType.WriteI2C:
                        {
                            return HandleI2CWrite(data);
                        }
                    case CommandType.ReadI2C:
                        {
                            return HandleI2CRead(data);
                        }
                    case CommandType.WriteReadI2C:
                        {
                            return HandleI2CWriteRead(data);
                        }
                    case CommandType.I2CData:
                        {
                            return HandleI2CData(data, out resultData);
                        }
                    case CommandType.WriteReadSpi:
                        {
                            return HandleSpiWriteRead(data);
                        }
                    case CommandType.SpiData:
                        {
                            return HandleSpiData(data, out resultData);
                        }
                    case CommandType.ReadFile:
                        {
                            return HandleReadFile(data);
                        }
                    case CommandType.WriteFile:
                        {
                            return HandleWriteFile(data);
                        }
                    case CommandType.FileData:
                        {
                            return HandleFileData(data, out index, out resultData);
                        }
                    case CommandType.FileEnd:
                        return ProtocolResult.FileEnd;
                    case CommandType.FileList:
                        {
                            return HandleFileList(data);
                        }
                    case CommandType.RequestFileList:
                        {
                            return HandleRequestFileList();
                        }
                    case CommandType.DeleteFile:
                        {
                            return HandleDeleteFile(data);
                        }
                    case CommandType.DisplayText:
                        {
                            return HandleDisplayText(data);
                        }
                    case CommandType.DisplayButton:
                        {
                            return HandleDisplayButton(data);
                        }
                    case CommandType.DisplayRectangle:
                        {
                            return HandleDisplayRectangle(data);
                        }
                    case CommandType.DisplayLine:
                        {
                            return HanldeDisplayLine(data);
                        }
                    case CommandType.DisplayImage:
                        {
                            return HandleDisplayImage(data);
                        }
                    case CommandType.TemplateMsg:
                        {
                            return HandleTemplateMessage(data);
                        }
                    case CommandType.DisplayDeleteText:
                    case CommandType.DisplayDeleteButton:
                    case CommandType.DisplayDeleteRectangle:
                    case CommandType.DisplayDeleteLine:
                    case CommandType.DisplayDeleteImage:
                        {
                            return HandleDisplayDelete(data, commandType);
                        }
                    case CommandType.DisplayInvalidate:
                        {
                            return HandleDisplayInvalidate();
                        }
                    case CommandType.DisplayClearAll:
                        {
                            return HandleDisplayClearAll(data);
                        }
                    case CommandType.TouchButton:
                        {
                            return HandleTouchButton(data);
                        }
                    case CommandType.RequestTouchButton:
                        {
                            return HandleRequestTouchButton();
                        }
                    case CommandType.Backlight:
                        {
                            return HandleBacklight(data);
                        }
                    case CommandType.PortRedirect:
                        {
                            return HandlePortRedirect(data);
                        }
                    case CommandType.PortRedirectCRT310:
                        {
                            return HandlePortRedirectCRT310(data);
                        }
                    case CommandType.PortRedirectCRT310Answer:
                        {
                            return HandlePortRedirectCRT310Answer(data);
                        }
                    case CommandType.ReadMedia:
                        {
                            return HandleReadMedia(data);
                        }
                    case CommandType.RequestMedia:
                        {
                            return HandleRequestMedia(data);
                        }
                    case CommandType.DeviceState:
                        {
                            return HandleDeviceState(data);
                        }
                    case CommandType.Command:
                        {
                            return HandleCommand(data);
                        }
                    case CommandType.SystemInfo:
                        {
                            return HandleSystemInfo(data);
                        }
                    case CommandType.RequestSystemInfo:
                        {
                            return HandleRequestVersion();
                        }
                    case CommandType.Date:
                        {
                            return HandleDate(data);
                        }
                    case CommandType.RequestDate:
                        {
                            return HandleDateRequest();
                        }
                    case CommandType.Reboot:
                        {
                            return HandleReboot();
                        }
                    case CommandType.Ack:
                        return ProtocolResult.AckAck;
                    case CommandType.Nack:
                        return (ProtocolResult)data[1];
                }
            }
            return ProtocolResult.UnknownCommand;
        }

        private ProtocolResult HandleSpiData(byte[] data, out byte[] resultData)
        {
            var length = data[1] + (data[2] << 8);
            resultData = new byte[length];
            Array.Copy(data, 3, resultData, 0, length);
            return ProtocolResult.Ack;
        }

        private ProtocolResult HandleSpiWriteRead(byte[] data)
        {
            var cs = (SpiChipSelect)data[1];
            var config = data[2];
            var csActiveState = (config & 0x01) > 0;
            var clockIdleState = (config & 0x02) > 0;
            var clockEdge = (config & 0x04) > 0;
            var freq = (SpiSpeed)((config & 0x18) >> 3);

            var length = data[3] + (data[4] << 8);
            var writeBuffer = new byte[length];
            Array.Copy(data, 5, writeBuffer, 0, length);
            if (SpiWriteRead != null)
            {
                SpiWriteRead(this, cs, freq, csActiveState, clockIdleState, clockEdge, writeBuffer);
                return ProtocolResult.None;
            }
            return ProtocolResult.EventError;
        }

        private ProtocolResult HandleI2CWrite(byte[] data)
        {
            var addr = data[1];
            var length = data[2] + (data[3] << 8);
            var writeBuffer = new byte[length];
            Array.Copy(data, 4, writeBuffer, 0, length);
            if (I2CWriteData != null)
            {
                return I2CWriteData(this, addr, writeBuffer);
            }
            return ProtocolResult.EventError;
        }

        private ProtocolResult HandleI2CRead(byte[] data)
        {
            var addr = data[1];
            var length = data[2] + (data[3] << 8);
            if (I2CReadData != null)
            {
                I2CReadData(this, addr, length);
                return ProtocolResult.None;
            }
            return ProtocolResult.EventError;
        }

        private ProtocolResult HandleI2CWriteRead(byte[] data)
        {
            var addr = data[1];
            var readLength = data[2] + (data[3] << 8);
            var length = data[4] + (data[5] << 8);
            var writeBuffer = new byte[length];
            Array.Copy(data, 6, writeBuffer, 0, length);
            if (I2CWriteReadData != null)
            {
                I2CWriteReadData(this, addr, writeBuffer, readLength);
                return ProtocolResult.None;
            }
            return ProtocolResult.EventError;
        }

        private ProtocolResult HandleI2CData(byte[] data, out byte[] resultData)
        {
            var length = data[1] + (data[2] << 8);
            resultData = new byte[length];
            Array.Copy(data, 3, resultData, 0, length);
            return ProtocolResult.Ack;
        }

        private ProtocolResult HandleBacklight(byte[] data)
        {
            if (BacklightChange != null)
            {
                BacklightChange(this, data[1]);
                return ProtocolResult.Ack;
            }
            return ProtocolResult.EventError;
        }

        private ProtocolResult HandleRequestInput()
        {
            if (RequestInputs != null)
            {
                RequestInputs(this);
                return ProtocolResult.None;
            }
            return ProtocolResult.EventError;
        }

        private ProtocolResult HandleRequestFileList()
        {
            if (RequestFileList != null)
            {
                RequestFileList(this);
                return ProtocolResult.None;
            }
            return ProtocolResult.EventError;
        }

        private ProtocolResult HandleDeleteFile(byte[] data)
        {
            var filename = ByteArrayToString(data, 2, data[1]);
            if (DeleteFile != null)
            {
                return DeleteFile(this, filename);
            }
            return ProtocolResult.EventError;
        }

        private ProtocolResult HandleReboot()
        {
            if (Reboot != null)
            {
                Reboot(this);
            }
            return ProtocolResult.None;
        }

        private ProtocolResult HandleDateRequest()
        {
            if (RequestDateTime != null)
            {
                RequestDateTime(this);
                return ProtocolResult.None;
            }
            return ProtocolResult.EventError;
        }

        private ProtocolResult HandleDate(byte[] data)
        {
            var day = data[1];
            var month = data[2];
            var year = (ushort)(data[3] + (data[4] << 8));
            var hour = data[5];
            var min = data[6];
            var sec = data[7];

            if (!CheckDisplayParamter(day, 1, 31, "Day -> Date"))
            {
                return ProtocolResult.InvalidParameter;
            }
            if (!CheckDisplayParamter(month, 1, 12, "Month -> Date"))
            {
                return ProtocolResult.InvalidParameter;
            }
            if (!CheckDisplayParamter(year, 2000, 2500, "Year -> Date"))
            {
                return ProtocolResult.InvalidParameter;
            }
            if (!CheckDisplayParamter(hour, 0, 23, "Hour -> Date"))
            {
                return ProtocolResult.InvalidParameter;
            }
            if (!CheckDisplayParamter(min, 0, 59, "Minute -> Date"))
            {
                return ProtocolResult.InvalidParameter;
            }
            if (!CheckDisplayParamter(sec, 0, 59, "Second -> Date"))
            {
                return ProtocolResult.InvalidParameter;
            }

            var dateTime = new DateTime(year, month, day, hour, min, sec);
            if (DateTimeChange != null)
            {
                DateTimeChange(this, dateTime);
                return ProtocolResult.Ack;
            }
            return ProtocolResult.EventError;
        }

        private ProtocolResult HandleRequestVersion()
        {
            if (RequestSystemInfo != null)
            {
                RequestSystemInfo(this);
                return ProtocolResult.None;
            }
            return ProtocolResult.EventError;
        }

        private ProtocolResult HandleSystemInfo(byte[] data)
        {
            var version = new Version(data[1], data[2], data[3], data[4]);
            var mac = new byte[6];
            Array.Copy(data, 5, mac, 0, 6);

            if (SystemInfoChanged != null)
            {
                SystemInfoChanged(this, version, mac);
                return ProtocolResult.AckAck;
            }
            return ProtocolResult.EventError;
        }

        private ProtocolResult HandleReadMedia(byte[] data)
        {
            var readerId = data[1];
            if (!CheckDisplayParamter(readerId, 1, 3, "ReaderId -> ReadMedia"))
            {
                return ProtocolResult.InvalidParameter;
            }

            var length = data[2] + (data[3] << 8);
            var cardData = new byte[length];
            Array.Copy(data, 4, cardData, 0, length);
            if (MediaRead != null)
            {
                MediaRead(this, readerId, cardData);
                return ProtocolResult.AckAck;
            }
            return ProtocolResult.EventError;
        }

        private ProtocolResult HandlePortRedirect(byte[] data)
        {
            var readerId = data[1];

            if (!CheckDisplayParamter(readerId, 1, 3, "ReaderId -> PortRedirect"))
            {
                return ProtocolResult.InvalidParameter;
            }

            var timeout = (ushort)(data[2] + (data[3] << 8));
            var writeLength = data[4] + (data[5] << 8);
            var readLength = (ushort)(data[6] + (data[7] << 8));
            var writeBuffer = new byte[writeLength];
            Array.Copy(data, 8, writeBuffer, 0, writeLength);
            if (PortRedirect != null)
            {
                PortRedirect(this, readerId, timeout, readLength, writeBuffer);
                return ProtocolResult.None;
            }
            return ProtocolResult.EventError;
        }

        private ProtocolResult HandlePortRedirectCRT310(byte[] data)
        {
            var readerId = data[1];

            if (!CheckDisplayParamter(readerId, 1, 3, "ReaderId -> PortRedirect"))
            {
                return ProtocolResult.InvalidParameter;
            }

            var timeout = (ushort)(data[2] + (data[3] << 8));
            var writeLength = data[4] + (data[5] << 8);
            CRT310ResultLengthType readLengthType = (CRT310ResultLengthType)data[6];
            var writeBuffer = new byte[writeLength];
            Array.Copy(data, 7, writeBuffer, 0, writeLength);
            if (PortRedirectCRT310 != null)
            {
                PortRedirectCRT310(this, readerId, timeout, readLengthType, writeBuffer);
                return ProtocolResult.None;
            }
            return ProtocolResult.EventError;
        }

        private ProtocolResult HandlePortRedirectCRT310Answer(byte[] data)
        {
            var readerId = data[1];
            if (!CheckDisplayParamter(readerId, 1, 3, "ReaderId -> PortRedirectCRT310Answer"))
            {
                return ProtocolResult.InvalidParameter;
            }

            var length = data[2] + (data[3] << 8);
            var answer = new byte[length];
            Array.Copy(data, 4, answer, 0, length);
            if (PortRedirectCRT310Answer != null)
            {
                PortRedirectCRT310Answer(this, readerId, answer);
                return ProtocolResult.AckAck;
            }
            return ProtocolResult.EventError;
        }

        private ProtocolResult HandleDeviceState(byte[] data)
        {
            var readerId = data[1];
            if (!CheckDisplayParamter(readerId, 1, 3, "ReaderId -> DeviceState"))
            {
                return ProtocolResult.InvalidParameter;
            }

            var state = (DeviceState)data[2];
            if (DeviceStateChanged != null)
            {
                DeviceStateChanged(this, readerId, state);
                return ProtocolResult.AckAck;
            }
            return ProtocolResult.EventError;
        }

        private ProtocolResult HandleCommand(byte[] data)
        {
            var id = data[1];
            if (!CheckDisplayParamter(id, 0, 3, "Id -> Command"))
            {
                return ProtocolResult.InvalidParameter;
            }

            var command = (Command)data[2];
            if (CommandChanged != null)
            {
                CommandChanged(this, id, command);
                return ProtocolResult.Ack;
            }
            return ProtocolResult.EventError;
        }

        private ProtocolResult HandleTouchButton(byte[] data)
        {
            if (TouchButton != null)
            {
                TouchButton(this, data[1]);
                return ProtocolResult.Ack;
            }
            return ProtocolResult.EventError;
        }

        private ProtocolResult HandleRequestTouchButton()
        {
            if (RequestTouchButton != null)
            {
                RequestTouchButton(this);
                return ProtocolResult.None;
            }
            return ProtocolResult.EventError;
        }

        private ProtocolResult HandleDisplayClearAll(byte[] data)
        {
            var color = new ColorInfo(data[1], data[2], data[3]);
            if (DisplayClearAll != null)
            {
                DisplayClearAll(this, color);
                return ProtocolResult.Ack;
            }
            return ProtocolResult.EventError;
        }

        private ProtocolResult HandleDisplayInvalidate()
        {
            if (DisplayInvalidate != null)
            {
                DisplayInvalidate(this);
                return ProtocolResult.Ack;
            }
            return ProtocolResult.EventError;
        }

        private ProtocolResult HandleDisplayDelete(byte[] data, CommandType commandType)
        {
            var type = (DisplayCommand)commandType - 0x10;
            var idx = data[1];

            if (!CheckDisplayParamter((byte)commandType, 0x50, 0x54, "Index -> DisplayDelete"))
            {
                return ProtocolResult.InvalidParameter;
            }
            if (!CheckDisplayParamter(idx, 1, 10, "Index -> DisplayDelete"))
            {
                return ProtocolResult.InvalidParameter;
            }

            if (DeleteDisplayItem != null)
            {
                DeleteDisplayItem(this, type, idx);
                return ProtocolResult.Ack;
            }
            return ProtocolResult.EventError;
        }

        private ProtocolResult HandleDisplayImage(byte[] data)
        {
            var idx = data[1];
            var x = (ushort)(data[2] + (data[3] << 8));
            var y = (ushort)(data[4] + (data[5] << 8));

            if (!CheckDisplayParamter(idx, 1, 10, "Index -> DisplayImage"))
            {
                return ProtocolResult.InvalidParameter;
            }
            if (!CheckDisplayParamter(x, 0, _displayWidth, "X -> DisplayImage"))
            {
                return ProtocolResult.InvalidParameter;
            }
            if (!CheckDisplayParamter(y, 0, _displayHeight, "Y -> DisplayImage"))
            {
                return ProtocolResult.InvalidParameter;
            }

            var len = data[6];
            string file = string.Empty;
            for (int i = 0; i < len; i++)
            {
                file += (char)data[7 + i];
            }

            if (DisplayImageChange != null)
            {
                DisplayImageChange(this, new ImageInfo(idx, file, x, y));
                return ProtocolResult.Ack;
            }
            return ProtocolResult.EventError;
        }

        private ProtocolResult HanldeDisplayLine(byte[] data)
        {
            var idx = data[1];
            var x1 = (ushort)(data[2] + (data[3] << 8));
            var y1 = (ushort)(data[4] + (data[5] << 8));
            var x2 = (ushort)(data[6] + (data[7] << 8));
            var y2 = (ushort)(data[8] + (data[9] << 8));

            if (!CheckDisplayParamter(idx, 1, 10, "Index -> DisplayLine"))
            {
                return ProtocolResult.InvalidParameter;
            }
            if (!CheckDisplayParamter(x1, 0, _displayWidth, "X1 -> DisplayLine"))
            {
                return ProtocolResult.InvalidParameter;
            }
            if (!CheckDisplayParamter(y1, 0, _displayHeight, "Y1 -> DisplayLine"))
            {
                return ProtocolResult.InvalidParameter;
            }

            if (!CheckDisplayParamter(x2, 0, _displayWidth, "X2 -> DisplayLine"))
            {
                return ProtocolResult.InvalidParameter;
            }
            if (!CheckDisplayParamter(y2, 0, _displayHeight, "Y2 -> DisplayLine"))
            {
                return ProtocolResult.InvalidParameter;
            }

            var color = new ColorInfo(data[10], data[11], data[12]);
            var width = data[13];
            if (DisplayLineChange != null)
            {
                DisplayLineChange(this, new LineInfo(idx, color, x1, y1, x2, y2, width));
                return ProtocolResult.Ack;
            }
            return ProtocolResult.EventError;
        }

        private ProtocolResult HandleDisplayRectangle(byte[] data)
        {
            var idx = data[1];
            var x = (ushort)(data[2] + (data[3] << 8));
            var y = (ushort)(data[4] + (data[5] << 8));
            var width = (ushort)(data[6] + (data[7] << 8));
            var height = (ushort)(data[8] + (data[9] << 8));

            if (!CheckDisplayParamter(idx, 1, 10, "Index -> DisplayRectangle"))
            {
                return ProtocolResult.InvalidParameter;
            }
            if (!CheckDisplayParamter(x, 0, _displayWidth, "X -> DisplayRectangle"))
            {
                return ProtocolResult.InvalidParameter;
            }
            if (!CheckDisplayParamter(y, 0, _displayHeight, "Y -> DisplayRectangle"))
            {
                return ProtocolResult.InvalidParameter;
            }
            if (!CheckDisplayParamter(width, 1, _displayWidth, "X -> DisplayRectangle"))
            {
                return ProtocolResult.InvalidParameter;
            }
            if (!CheckDisplayParamter(height, 1, _displayHeight, "Y -> DisplayRectangle"))
            {
                return ProtocolResult.InvalidParameter;
            }

            var color = new ColorInfo(data[10], data[11], data[12]);
            if (DisplayRectangleChange != null)
            {
                DisplayRectangleChange(this, new RectangleInfo(idx, color, x, y, width, height));
                return ProtocolResult.Ack;
            }
            return ProtocolResult.EventError;
        }

        private ProtocolResult HandleDisplayButton(byte[] data)
        {
            var idx = data[1];
            var x = (ushort)(data[2] + (data[3] << 8));
            var y = (ushort)(data[4] + (data[5] << 8));
            var width = (ushort)(data[6] + (data[7] << 8));
            var height = (ushort)(data[8] + (data[9] << 8));

            if (!CheckDisplayParamter(idx, 1, 10, "Index -> DisplayButton"))
            {
                return ProtocolResult.InvalidParameter;
            }
            if (!CheckDisplayParamter(x, 0, _displayWidth, "X -> DisplayButton"))
            {
                return ProtocolResult.InvalidParameter;
            }
            if (!CheckDisplayParamter(y, 0, _displayHeight, "Y -> DisplayButton"))
            {
                return ProtocolResult.InvalidParameter;
            }
            if (!CheckDisplayParamter(width, 1, _displayWidth, "X -> DisplayButton"))
            {
                return ProtocolResult.InvalidParameter;
            }
            if (!CheckDisplayParamter(height, 1, _displayHeight, "Y -> DisplayButton"))
            {
                return ProtocolResult.InvalidParameter;
            }

            var fcolor = new ColorInfo(data[10], data[11], data[12]);
            var bcolor = new ColorInfo(data[13], data[14], data[15]);
            var size = data[16];
            var len = data[17];
            string text = string.Empty;
            for (int i = 0; i < len; i++)
            {
                text += (char)data[18 + i];
            }
            if (DisplayButtonChange != null)
            {
                DisplayButtonChange(this, new ButtonInfo(idx, fcolor, bcolor, size, x, y, width, height, text));
                return ProtocolResult.Ack;
            }
            return ProtocolResult.EventError;
        }

        private ProtocolResult HandleDisplayText(byte[] data)
        {
            var idx = data[1];
            var x = (ushort)(data[2] + (data[3] << 8));
            var y = (ushort)(data[4] + (data[5] << 8));

            if (!CheckDisplayParamter(idx, 1, 20, "Index -> DisplayText"))
            {
                return ProtocolResult.InvalidParameter;
            }
            if (!CheckDisplayParamter(x, 0, _displayWidth, "X -> DisplayText"))
            {
                return ProtocolResult.InvalidParameter;
            }
            if (!CheckDisplayParamter(y, 0, _displayHeight, "Y -> DisplayText"))
            {
                return ProtocolResult.InvalidParameter;
            }
            var color = new ColorInfo(data[6], data[7], data[8]);
            var size = data[9];
            var len = data[10];
            string text = string.Empty;
            for (int i = 0; i < len; i++)
            {
                text += (char)data[11 + i];
            }
            if (DisplayTextChange != null)
            {
                DisplayTextChange(this, new DisplayTextInfo(idx, size, color, x, y, text));
                return ProtocolResult.Ack;
            }
            return ProtocolResult.EventError;
        }

        private ProtocolResult HandleTemplateMessage(byte[] data)
        {
            string text1 = string.Empty;
            string text2 = string.Empty;
            string text3 = string.Empty;
            string imgFilename = string.Empty;

            var idx = 1;
            // text 1
            var lenText1 = data[idx];
            idx++;
            for (int i = 0; i < lenText1; i++)
            {
                text1 += (char)data[idx];
                idx++;
            }
            // text 2
            var lenText2 = data[idx];
            idx++;
            for (int i = 0; i < lenText2; i++)
            {
                text2 += (char)data[idx];
                idx++;
            }
            // text 3
            var lenText3 = data[idx];
            idx++;
            for (int i = 0; i < lenText3; i++)
            {
                text3 += (char)data[idx];
                idx++;
            }
            // image
            var lenImage = data[idx];
            idx++;
            for (int i = 0; i < lenImage; i++)
            {
                imgFilename += (char)data[idx];
                idx++;
            }
            // beeper
            var beeperDuration = (data[idx] + (data[idx + 1] << 8)) / 10.0;
            idx += 2;
            // output relais
            var relais = new Relay[7];
            for (int i = 0; i < relais.Length; i++)
            {
                relais[i] = new Relay(i, (data[idx] & Pow(2, i)) > 0);
            }
            idx++;
            // lock reading serial port 1-3
            bool lockReading = data[idx] == 0x01;

            if (TemplateMessageChange != null && SwitchRelais != null && ActivateBeeper != null)
            {
                SwitchRelais(this, relais);
                ActivateBeeper(this, beeperDuration);
                TemplateMessageChange(this, text1, text2, text3, imgFilename, lockReading);

                return ProtocolResult.Ack;
            }

            return ProtocolResult.EventError;
        }

        private ProtocolResult HandleFileList(byte[] data)
        {
            var len = data[1] + (data[2] << 8);
            string text = string.Empty;
            for (int i = 0; i < len; i++)
            {
                text += (char)data[3 + i];
            }

            var files = text.Split(',');
            if (FileListChanged != null)
            {
                FileListChanged(this, files);
                return ProtocolResult.Ack;
            }
            return ProtocolResult.EventError;
        }

        private static ProtocolResult HandleFileData(byte[] data, out byte index, out byte[] resultData)
        {
            index = data[1];
            var length = data[2] + (data[3] << 8);
            resultData = new byte[length];
            Array.Copy(data, 4, resultData, 0, length);
            return ProtocolResult.Ack;
        }

        private ProtocolResult HandleWriteFile(byte[] data)
        {
            var filname = ByteArrayToString(data, 2, data[1]);
            if (WriteFile != null)
            {
                return WriteFile(this, filname);
            }
            return ProtocolResult.EventError;
        }

        private ProtocolResult HandleReadFile(byte[] data)
        {
            var filname = ByteArrayToString(data, 2, data[1]);
            if (ReadFile != null)
            {
                return ReadFile(this, filname);
            }
            return ProtocolResult.EventError;
        }

        private ProtocolResult HandleInputs(byte[] data)
        {
            var mask = data[1];
            var result = new bool[4];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = (mask & Pow(2, i)) > 0;
            }
            if (InputChanged != null)
            {
                InputChanged(this, result);
                return ProtocolResult.Ack;
            }
            return ProtocolResult.EventError;
        }

        private ProtocolResult HandleRelaisTime(byte[] data)
        {
            var time = (data[2] + (data[3] << 8)) / 10.0;
            byte bit = 0;
            for (byte i = 0; i < 7; i++)
            {
                if (data[1] == Pow(2, i))
                {
                    bit = i;
                    break;
                }
            }
            var relais = new Relay(bit, time);

            if (SwitchRelais != null)
            {
                SwitchRelais(this, new[] { relais });
                return ProtocolResult.Ack;
            }
            return ProtocolResult.EventError;
        }

        private ProtocolResult HandleBeeper(byte[] data)
        {
            var time = (data[1] + (data[2] << 8)) / 10.0;

            if (ActivateBeeper != null)
            {
                ActivateBeeper(this, time);
                return ProtocolResult.Ack;
            }
            return ProtocolResult.EventError;
        }

        private ProtocolResult HandleRequestMedia(byte[] data)
        {
            var id = data[1];
            if (id > 3 || id == 0)
            {
                _logger.LogError("Invild params (RequestMedia) -> readerId must be between 1 .. 3");
                return ProtocolResult.InvalidParameter;
            }

            if (RequestMediaRead != null)
            {
                RequestMediaRead(this, id);
                return ProtocolResult.None;
            }
            return ProtocolResult.EventError;
        }

        private ProtocolResult HandleRelays(byte[] data)
        {
            var relais = new Relay[7];
            for (int i = 0; i < relais.Length; i++)
            {
                relais[i] = new Relay(i, (data[1] & Pow(2, i)) > 0);
            }
            if (SwitchRelais != null)
            {
                SwitchRelais(this, relais);
                return ProtocolResult.Ack;
            }
            return ProtocolResult.EventError;
        }

        private bool CheckLength(CommandType command, byte[] data)
        {
            // skip commandtype byte
            var len = data.Length - 1;
            switch (command)
            {
                case CommandType.Relays:
                    return len >= 1;
                case CommandType.RelayTime:
                    return len >= 3;
                case CommandType.Beeper:
                    return len >= 2;
                case CommandType.Inputs:
                    return len >= 1;
                case CommandType.RequestInputs:
                    return len >= 0;
                case CommandType.WriteI2C:
                    return len > 3 && len >= 3 + data[2] + (data[3] << 8);
                case CommandType.ReadI2C:
                    return len >= 3;
                case CommandType.WriteReadI2C:
                    return len > 5 && len >= 5 + data[4] + (data[5] << 8);
                case CommandType.I2CData:
                    return len > 2 && len >= 2 + data[1] + (data[2] << 8);
                case CommandType.WriteReadSpi:
                    return len > 4 && len >= 4 + data[3] + (data[4] << 8);
                case CommandType.SpiData:
                    return len > 2 && len >= 2 + data[1] + (data[2] << 8);
                case CommandType.ReadFile:
                    return len > 1 && len >= 1 + data[1];
                case CommandType.WriteFile:
                    return len > 1 && len >= 1 + data[1];
                case CommandType.FileData:
                    return len > 3 && len >= 3 + data[2] + (data[3] << 8);
                case CommandType.FileEnd:
                    return len >= 0;
                case CommandType.FileList:
                    return len >= 2 && len >= 2 + data[1] + (data[2] << 8);
                case CommandType.RequestFileList:
                    return len >= 0;
                case CommandType.DeleteFile:
                    return len >= 1 && len >= 1 + data[1];
                case CommandType.DisplayText:
                    return len > 10 && len >= 10 + data[10];
                case CommandType.DisplayButton:
                    return len > 17 && len >= 17 + data[17];
                case CommandType.DisplayRectangle:
                    return len > 14 && len >= 14 + data[14];
                case CommandType.DisplayLine:
                    return len >= 12;
                case CommandType.DisplayImage:
                    return len > 6 && len >= 6 + data[6];
                case CommandType.TemplateMsg:
                    return len >= 8;
                case CommandType.DisplayDeleteText:
                    return len >= 1;
                case CommandType.DisplayDeleteButton:
                    return len >= 1;
                case CommandType.DisplayDeleteRectangle:
                    return len >= 1;
                case CommandType.DisplayDeleteLine:
                    return len >= 1;
                case CommandType.DisplayDeleteImage:
                    return len >= 1;
                case CommandType.DisplayInvalidate:
                    return len >= 0;
                case CommandType.DisplayClearAll:
                    return len >= 3;
                case CommandType.TouchButton:
                    return len >= 1;
                case CommandType.RequestTouchButton:
                    return len >= 1;
                case CommandType.Backlight:
                    return len >= 1;
                case CommandType.PortRedirect:
                    return len > 7 && len >= 7 + data[4] + (data[5] << 8);
                case CommandType.PortRedirectCRT310:
                    return len > 6 && len >= 6 + data[4] + (data[5] << 8);
                case CommandType.PortRedirectCRT310Answer:
                    return len > 3 && len >= 3 + data[2] + (data[3] << 8);
                case CommandType.ReadMedia:
                    return len > 3 && len >= 3 + data[2] + (data[3] << 8);
                case CommandType.RequestMedia:
                    return len >= 1;
                case CommandType.DeviceState:
                    return len >= 3;
                case CommandType.Command:
                    return len >= 3;
                case CommandType.SystemInfo:
                    return len >= 4;
                case CommandType.RequestSystemInfo:
                    return len >= 0;
                case CommandType.Date:
                    return len >= 7;
                case CommandType.RequestDate:
                    return len >= 0;
                case CommandType.Reboot:
                    return len >= 0;
                case CommandType.Ack:
                    return len >= 1;
                case CommandType.Nack:
                    return len >= 1;
            }
            return false;
        }

        private bool CheckDisplayParamter(ushort value, ushort min, ushort max, string errorText)
        {
            if (value < min && value > max)
            {
                _logger.LogError("Invalid parameter: " + errorText);
                return false;
            }
            return true;
        }

        public byte[] CreateRelaisCommand(byte bitmask)
        {
            var data = new byte[2];
            data[0] = (byte)CommandType.Relays;
            data[1] = bitmask;
            return data;
        }

        public byte[] CreateSingleRelaisCommand(byte bitmask, double duration)
        {
            var data = new byte[4];
            data[0] = (byte)CommandType.RelayTime;
            data[1] = bitmask;
            var time = (short)(duration * 10);
            data[2] = (byte)time;
            data[3] = (byte)(time >> 8);
            return data;
        }

        public byte[] CreateBeeperCommand(double duration)
        {
            var data = new byte[3];
            data[0] = (byte)CommandType.Beeper;
            var time = (short)(duration * 10);
            data[1] = (byte)time;
            data[2] = (byte)(time >> 8);
            return data;
        }

        public byte[] CreateInputCommand(bool[] inputStates)
        {
            var data = new byte[2];
            data[0] = (byte)CommandType.Inputs;
            for (int i = 0; i < inputStates.Length; i++)
            {
                if (inputStates[i])
                {
                    data[1] |= (byte)Pow(2, i);
                }
            }
            return data;
        }

        public byte[] CreateTouchButtonCommand(byte buttonId)
        {
            var data = new byte[2];
            data[0] = (byte)CommandType.TouchButton;
            data[1] = buttonId;
            return data;
        }

        public byte[] CreateDisplayTextCommand(byte index, ColorInfo color, short x, short y, byte size, string text)
        {
            //var textBytes = Encoding.UTF8.GetBytes(text);
            var textBytes = text.ToCharArray();

            if (textBytes.Length > 255)
            {
                throw new ArgumentException("Text length max 255");
            }

            var data = new byte[11 + textBytes.Length];
            data[0] = (byte)CommandType.DisplayText;
            data[1] = index;
            data[2] = (byte)x;
            data[3] = (byte)(x >> 8);
            data[4] = (byte)y;
            data[5] = (byte)(y >> 8);
            data[6] = color.R;
            data[7] = color.G;
            data[8] = color.B;
            data[9] = size;
            data[10] = (byte)textBytes.Length;
            for (int i = 0; i < textBytes.Length; i++)
            {
                data[11 + i] = (byte)textBytes[i];
            }
            return data;
        }

        public byte[] CreateTemplateMessageCommand(string text1, string text2, string text3, string imgFilename, double beepDuration, byte outputBitmask, bool lockReading)
        {
            var textBytes1 = Encoding.UTF8.GetBytes(text1);
            var textBytes2 = Encoding.UTF8.GetBytes(text2);
            var textBytes3 = Encoding.UTF8.GetBytes(text3);

            if (textBytes1.Length > 255 || textBytes2.Length > 255 || textBytes3.Length > 255)
            {
                throw new ArgumentException("Text length max 255");
            }

            var imgText = Encoding.UTF8.GetBytes(imgFilename);
            if (imgText.Length > 255)
            {
                throw new ArgumentException("Filename length max 255");
            }

            var data = new byte[9 + textBytes1.Length + textBytes2.Length + textBytes3.Length + imgText.Length];
            var idx = 0;

            data[idx] = (byte)CommandType.TemplateMsg;
            idx++;
            // text 1
            data[idx] = (byte)textBytes1.Length;
            idx++;
            for (int i = 0; i < textBytes1.Length; i++)
            {
                data[idx] = textBytes1[i];
                idx++;
            }
            // text 2
            data[idx] = (byte)textBytes2.Length;
            idx++;
            for (int i = 0; i < textBytes2.Length; i++)
            {
                data[idx] = textBytes2[i];
                idx++;
            }
            // text 3
            data[idx] = (byte)textBytes3.Length;
            idx++;
            for (int i = 0; i < textBytes3.Length; i++)
            {
                data[idx] = textBytes3[i];
                idx++;
            }
            // image
            data[idx] = (byte)imgText.Length;
            idx++;
            for (int i = 0; i < imgText.Length; i++)
            {
                data[idx] = imgText[i];
                idx++;
            }
            // beeper
            var time = (short)(beepDuration * 10);
            data[idx] = (byte)time;
            idx++;
            data[idx] = (byte)(time >> 8);
            idx++;
            // output mask
            data[idx] = outputBitmask;
            idx++;
            // lock reading serial port 1-3
            if (lockReading)
            {
                data[idx] = 0x01;
            }
            else
            {
                data[idx] = 0x00;
            }

            return data;
        }

        public byte[] CreateDisplayButtonCommand(byte index, ColorInfo fontcolor, ColorInfo buttoncolor, ushort x, ushort y, ushort width, ushort height, byte fontSize, string text)
        {
            var utfText = Encoding.UTF8.GetBytes(text);

            if (utfText.Length > 255)
            {
                throw new ArgumentException("Text length max 255");
            }

            var data = new byte[18 + utfText.Length];
            data[0] = (byte)CommandType.DisplayButton;
            data[1] = index;
            data[2] = (byte)x;
            data[3] = (byte)(x >> 8);
            data[4] = (byte)y;
            data[5] = (byte)(y >> 8);
            data[6] = (byte)width;
            data[7] = (byte)(width >> 8);
            data[8] = (byte)height;
            data[9] = (byte)(height >> 8);
            data[10] = fontcolor.R;
            data[11] = fontcolor.G;
            data[12] = fontcolor.B;
            data[13] = buttoncolor.R;
            data[14] = buttoncolor.G;
            data[15] = buttoncolor.B;
            data[16] = fontSize;
            data[17] = (byte)utfText.Length;

            for (int i = 0; i < utfText.Length; i++)
            {
                data[18 + i] = utfText[i];
            }
            return data;
        }

        public byte[] CreateDisplayRectangleCommand(byte index, ColorInfo color, ushort x, ushort y, ushort width, ushort height)
        {
            var data = new byte[13];
            data[0] = (byte)CommandType.DisplayRectangle;
            data[1] = index;
            data[2] = (byte)x;
            data[3] = (byte)(x >> 8);
            data[4] = (byte)y;
            data[5] = (byte)(y >> 8);
            data[6] = (byte)width;
            data[7] = (byte)(width >> 8);
            data[8] = (byte)height;
            data[9] = (byte)(height >> 8);
            data[10] = color.R;
            data[11] = color.G;
            data[12] = color.B;
            return data;
        }

        public byte[] CreateDisplayLineCommand(byte index, ColorInfo color, ushort x1, ushort y1, ushort x2, ushort y2, byte size)
        {
            var data = new byte[14];
            data[0] = (byte)CommandType.DisplayLine;
            data[1] = index;
            data[2] = (byte)x1;
            data[3] = (byte)(x1 >> 8);
            data[4] = (byte)y1;
            data[5] = (byte)(y1 >> 8);
            data[6] = (byte)x2;
            data[7] = (byte)(x2 >> 8);
            data[8] = (byte)y2;
            data[9] = (byte)(y2 >> 8);
            data[10] = color.R;
            data[11] = color.G;
            data[12] = color.B;
            data[13] = size;
            return data;
        }

        public byte[] CreateDisplayImageCommand(byte index, ushort x, ushort y, string filename)
        {
            var utfText = Encoding.UTF8.GetBytes(filename);

            if (utfText.Length > 255)
            {
                throw new ArgumentException("Filename length max 255");
            }
            var data = new byte[7 + utfText.Length];
            data[0] = (byte)CommandType.DisplayImage;
            data[1] = index;
            data[2] = (byte)x;
            data[3] = (byte)(x >> 8);
            data[4] = (byte)y;
            data[5] = (byte)(y >> 8);
            data[6] = (byte)utfText.Length;

            for (int i = 0; i < utfText.Length; i++)
            {
                data[7 + i] = utfText[i];
            }
            return data;
        }

        public byte[] CreateDisplayDeleteCommand(byte index, DisplayCommand type)
        {
            var data = new byte[2];
            data[0] = (byte)(type + 0x10);
            data[1] = index;
            return data;
        }

        public byte[] CreateDisplayClearAllCommand(ColorInfo backgroundColor)
        {
            var data = new byte[4];
            data[0] = (byte)CommandType.DisplayClearAll;
            data[1] = backgroundColor.R;
            data[2] = backgroundColor.G;
            data[3] = backgroundColor.B;
            return data;
        }

        public byte[] CreateDisplayInvalidateCommand()
        {
            var data = new byte[1];
            data[0] = (byte)CommandType.DisplayInvalidate;
            return data;
        }

        public byte[] CreateSetDateCommand(DateTime date)
        {
            var data = new byte[8];
            data[0] = (byte)CommandType.Date;
            data[1] = (byte)date.Day;
            data[2] = (byte)date.Month;
            data[3] = (byte)date.Year;
            data[4] = (byte)(date.Year >> 8);
            data[5] = (byte)date.Hour;
            data[6] = (byte)date.Minute;
            data[7] = (byte)date.Second;
            return data;
        }

        public byte[] CreateGetDateCommand()
        {
            var data = new byte[1];
            data[0] = (byte)CommandType.RequestDate;
            return data;
        }

        public byte[] CreateAck(byte status = 0)
        {
            var data = new byte[2];
            data[0] = (byte)CommandType.Ack;
            data[1] = status;
            return data;
        }


        public byte[] CreateNack(ProtocolResult error)
        {
            var data = new byte[2];
            data[0] = (byte)CommandType.Nack;
            data[1] = (byte)error;
            return data;
        }

        private int Pow(int basis, int exponent)
        {
            var result = basis;
            if (exponent == 0)
            {
                return 1;
            }
            for (int i = 1; i < exponent; i++)
            {
                result *= basis;
            }
            return result;
        }

        private string ByteArrayToString(byte[] data, int start, int length)
        {
            string text = string.Empty;
            for (int i = start; i < length + start; i++)
            {
                text += (char)data[i];
            }
            return text;
        }

        public byte[] CreateFileTransferCommand(byte index, byte[] buffer)
        {
            var data = new byte[4 + buffer.Length];
            data[0] = (byte)CommandType.FileData;
            data[1] = index;
            data[2] = (byte)buffer.Length;
            data[3] = (byte)(buffer.Length >> 8);
            Array.Copy(buffer, 0, data, 4, buffer.Length);
            return data;
        }

        public byte[] CreateFileTransferEndCommand()
        {
            var data = new byte[1];
            data[0] = (byte)CommandType.FileEnd;
            return data;
        }

        public byte[] CreateReadFile(string filename)
        {
            var data = new byte[2 + filename.Length];
            data[0] = (byte)CommandType.ReadFile;
            data[1] = (byte)filename.Length;
            for (int i = 0; i < filename.Length; i++)
            {
                data[2 + i] = (byte)filename[i];
            }
            return data;
        }

        public byte[] CreateWriteFile(string filename)
        {
            var data = new byte[2 + filename.Length];
            data[0] = (byte)CommandType.WriteFile;
            data[1] = (byte)filename.Length;
            for (int i = 0; i < filename.Length; i++)
            {
                data[2 + i] = (byte)filename[i];
            }
            return data;
        }

        public byte[] CreateTimeCommand()
        {
            var now = DateTime.Now;
            var data = new byte[8];
            data[0] = (byte)CommandType.Date;
            data[1] = (byte)now.Day;
            data[2] = (byte)now.Month;
            data[3] = (byte)now.Year;
            data[4] = (byte)(now.Year >> 8);
            data[5] = (byte)now.Hour;
            data[6] = (byte)now.Minute;
            data[7] = (byte)now.Second;
            return data;
        }

        public byte[] CreateTimeRequestCommand()
        {
            var data = new byte[1];
            data[0] = (byte)CommandType.RequestDate;
            return data;
        }

        public byte[] CreateRebootCommand()
        {
            var data = new byte[1];
            data[0] = (byte)CommandType.Reboot;
            return data;
        }

        public byte[] CreateInputRequestCommand()
        {
            var data = new byte[1];
            data[0] = (byte)CommandType.RequestInputs;
            return data;
        }

        public byte[] CreateTouchButtonRequestCommand()
        {
            var data = new byte[1];
            data[0] = (byte)CommandType.RequestTouchButton;
            return data;
        }

        public byte[] CreateFileListCommand(string files)
        {
            var data = new byte[3 + files.Length];
            data[0] = (byte)CommandType.FileList;
            data[1] = (byte)files.Length;
            data[2] = (byte)(files.Length >> 8);

            for (int i = 0; i < files.Length; i++)
            {
                data[3 + i] = (byte)files[i];
            }
            return data;
        }

        public byte[] CreateRequestFileListCommand()
        {
            var data = new byte[1];
            data[0] = (byte)CommandType.RequestFileList;
            return data;
        }

        public byte[] CreateDeleteFileCommand(string filename)
        {
            var data = new byte[2 + filename.Length];
            data[0] = (byte)CommandType.DeleteFile;
            data[1] = (byte)filename.Length;
            for (int i = 0; i < filename.Length; i++)
            {
                data[2 + i] = (byte)filename[i];
            }
            return data;
        }

        public byte[] CreateSystemInfoCommand(Version version, byte[] macAddress)
        {
            var data = new byte[5 + macAddress.Length];
            data[0] = (byte)CommandType.SystemInfo;
            data[1] = (byte)version.Major;
            data[2] = (byte)version.Minor;
            data[3] = (byte)version.Build;
            data[4] = (byte)version.Revision;
            Array.Copy(macAddress, 0, data, 5, macAddress.Length);
            return data;
        }

        public byte[] CreateVersionRequestCommand()
        {
            var data = new byte[1];
            data[0] = (byte)CommandType.RequestSystemInfo;
            return data;
        }

        public byte[] CreateReadMediaCommand(byte readerId, byte[] cardData)
        {
            var data = new byte[4 + cardData.Length];
            data[0] = (byte)CommandType.ReadMedia;
            data[1] = readerId;
            data[2] = (byte)cardData.Length;
            data[3] = (byte)(cardData.Length >> 8);
            Array.Copy(cardData, 0, data, 4, cardData.Length);
            return data;
        }

        public byte[] CreateRequestMediaReadCommand(byte readerId)
        {
            var data = new byte[2];
            data[0] = (byte)CommandType.RequestMedia;
            data[1] = readerId;
            return data;
        }

        public byte[] CreatePortRedirectCommand(byte readerId, ushort timeout, ushort answerLength, byte[] dataToWrite)
        {
            var data = new byte[8 + dataToWrite.Length];
            data[0] = (byte)CommandType.PortRedirect;
            data[1] = readerId;
            data[2] = (byte)timeout;
            data[3] = (byte)(timeout >> 8);
            data[4] = (byte)dataToWrite.Length;
            data[5] = (byte)(dataToWrite.Length >> 8);
            data[6] = (byte)answerLength;
            data[7] = (byte)(answerLength >> 8);
            Array.Copy(dataToWrite, 0, data, 8, dataToWrite.Length);
            return data;
        }

        public byte[] CreatePortRedirectCRT310Command(byte readerId, ushort timeout, CRT310ResultLengthType answerLengthType, byte[] dataToWrite)
        {
            var data = new byte[8 + dataToWrite.Length];
            data[0] = (byte)CommandType.PortRedirectCRT310;
            data[1] = readerId;
            data[2] = (byte)timeout;
            data[3] = (byte)(timeout >> 8);
            data[4] = (byte)dataToWrite.Length;
            data[5] = (byte)(dataToWrite.Length >> 8);
            data[6] = (byte)answerLengthType;
            Array.Copy(dataToWrite, 0, data, 7, dataToWrite.Length);
            return data;
        }

        public byte[] CreatePortRedirectCRT310AnswerCommand(byte readerId, byte[] answer)
        {
            var data = new byte[4 + answer.Length];
            data[0] = (byte)CommandType.PortRedirectCRT310Answer;
            data[1] = readerId;
            data[2] = (byte)answer.Length;
            data[3] = (byte)(answer.Length >> 8);
            Array.Copy(answer, 0, data, 4, answer.Length);
            return data;
        }

        public byte[] CreateDeviceStateCommand(byte readerId, byte state)
        {
            var data = new byte[3];
            data[0] = (byte)CommandType.DeviceState;
            data[1] = readerId;
            data[2] = state;
            return data;
        }

        public byte[] CreateSetCommand(byte readerId, byte command)
        {
            var data = new byte[3];
            data[0] = (byte)CommandType.Command;
            data[1] = readerId;
            data[2] = command;
            return data;
        }

        public byte[] CreateDisplayBacklightCommand(byte value)
        {
            var data = new byte[2];
            data[0] = (byte)CommandType.Backlight;
            data[1] = value;
            return data;
        }

        public byte[] CreateI2CDataCommand(byte[] value)
        {
            var data = new byte[3 + value.Length];
            data[0] = (byte)CommandType.I2CData;
            data[1] = (byte)value.Length;
            data[2] = (byte)(value.Length >> 8);
            Array.Copy(value, 0, data, 3, value.Length);
            return data;
        }

        public byte[] CreateI2CWriteDataCommand(byte addr, byte[] value)
        {
            var data = new byte[4 + value.Length];
            data[0] = (byte)CommandType.WriteI2C;
            data[1] = addr;
            data[2] = (byte)value.Length;
            data[3] = (byte)(value.Length >> 8);
            Array.Copy(value, 0, data, 4, value.Length);
            return data;
        }

        public byte[] CreateI2CReadDataCommand(byte addr, int length)
        {
            var data = new byte[4];
            data[0] = (byte)CommandType.ReadI2C;
            data[1] = addr;
            data[2] = (byte)length;
            data[3] = (byte)(length >> 8);
            return data;
        }

        public byte[] CreateI2CWriteReadDataCommand(byte addr, byte[] value, int readLength)
        {
            var data = new byte[6 + value.Length];
            data[0] = (byte)CommandType.WriteReadI2C;
            data[1] = addr;
            data[2] = (byte)readLength;
            data[3] = (byte)(readLength >> 8);
            data[4] = (byte)value.Length;
            data[5] = (byte)(value.Length >> 8);
            Array.Copy(value, 0, data, 6, value.Length);
            return data;
        }

        public byte[] CreateSpiWriteReadCommand(SpiChipSelect cs, bool csActiveState, bool clockEdge, bool clockIdle, SpiSpeed spiSpeed, byte[] value)
        {
            var data = new byte[5 + value.Length];
            data[0] = (byte)CommandType.WriteReadSpi;
            data[1] = (byte)cs;
            byte config = csActiveState ? (byte)0x01 : (byte)0x00;
            config |= clockIdle ? (byte)0x02 : (byte)0x00;
            config |= clockEdge ? (byte)0x04 : (byte)0x00;
            config |= (byte)((byte)spiSpeed << 3);
            data[2] = config;
            data[3] = (byte)value.Length;
            data[4] = (byte)(value.Length >> 8);
            Array.Copy(value, 0, data, 5, value.Length);
            return data;
        }

        public byte[] CreateSpiDataCommand(byte[] value)
        {
            var data = new byte[3 + value.Length];
            data[0] = (byte)CommandType.SpiData;
            data[1] = (byte)value.Length;
            data[2] = (byte)(value.Length >> 8);
            Array.Copy(value, 0, data, 3, value.Length);
            return data;
        }
    }
}