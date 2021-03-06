using System.Collections.Generic;
using System.Runtime.InteropServices;
using PureMidi.CoreMmSystem.MidiIO.Definitions;
using PureMidi.CoreMmSystem.MidiIO.Exceptions;

namespace PureMidi.CoreMmSystem.MidiIO.DeviceInfo
{
    public sealed class MidiOutInfo : MidiDeviceInfo
    {
        internal MidiOutInfo(
            ushort deviceIndex,
            string productName,
            ushort manufacturerId,
            ushort productId,
            ushort driverVerssion,
            EMidiDeviceTechnology technology,
            ushort voices,
            ushort notes,
            ushort channelMask,
            uint support)
        {
            DeviceIndex = deviceIndex;
            ProductName = productName;
            ManufacturerId = manufacturerId;
            ProductId = productId;
            DriverVerssion = driverVerssion;
            Technology = technology;
            Voices = voices;
            Notes = notes;
            ChannelMask = channelMask;
            Support = support;
        }

        public static IEnumerable<MidiOutInfo> Informations
        {
            get
            {
                var retVal = new List<MidiOutInfo>();
                for (ushort i = 0; i < WindowsMultimediaDevice.midiOutGetNumDevs(); i++)
                {
                    var caps = new MidiOutCaps();
                    int error = WindowsMultimediaDevice.midiOutGetDevCaps(i, ref caps, Marshal.SizeOf(caps));
                    if (error != (int)EDeviceException.MmsyserrNoerror) throw new MidiDeviceException(error);
                    retVal.Add(
                        new MidiOutInfo(
                            i,
                            caps.name,
                            (ushort) caps.mid,
                            (ushort) caps.pid,
                            (ushort) caps.driverVersion,
                            (EMidiDeviceTechnology) caps.support,
                            (ushort) caps.voices,
                            (ushort) caps.notes,
                            (ushort) caps.channelMask,
                            (uint) caps.support)
                        );
                }
                return retVal;
            }
        }

        public EMidiDeviceTechnology Technology { get; private set; }

        public ushort Voices { get; private set; }

        public ushort Notes { get; private set; }

        public ushort ChannelMask { get; private set; }

    }
}