﻿//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2019 Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Windows
// 
//  Dapplo.Windows is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Windows is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Windows. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using Dapplo.Windows.Messages.Enums;
using Microsoft.Win32;

namespace Dapplo.Windows.Messages.Structs
{
    /// <summary>
    /// Contains information about a class of devices.
    /// See <a href="https://www.pinvoke.net/default.aspx/Structures.DEV_BROADCAST_DEVICEINTERFACE">DEV_BROADCAST_DEVICEINTERFACE</a>
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DevBroadcastDeviceInterface
    {
        private int _size;
        // The device type, which determines the event-specific information that follows the first three members. 
        private DeviceBroadcastDeviceType _deviceType;
        private readonly int _reserved;
        private Guid _classGuid;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
        private readonly string _name;

        /// <summary>
        /// The GUID for the interface device class.
        /// </summary>
        public Guid DeviceClassGuid
        {
            get => _classGuid;
            set => _classGuid = value;
        }

        /// <summary>
        /// The name of the device.
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// Factory for an empty, but initialized, DevBroadcastDeviceInterface
        /// </summary>
        /// <returns>DevBroadcastDeviceInterface</returns>
        public static DevBroadcastDeviceInterface Create()
        {
            return new DevBroadcastDeviceInterface
            {
                _deviceType = DeviceBroadcastDeviceType.DeviceInterface,
                _size = Marshal.SizeOf(typeof(DevBroadcastDeviceInterface))
            };
        }

        /// <summary>
        /// Returns a more friendly name for the device
        /// </summary>
        public string FriendlyDeviceName
        {
            get
            {
                string[] parts = _name.Split('#');
                if (parts.Length < 3)
                {
                    return _name;
                }

                string devType = parts[0].Substring(parts[0].IndexOf(@"?\", StringComparison.Ordinal) + 2);
                string deviceInstanceId = parts[1];
                string deviceUniqueId = parts[2];
                string regPath = @"SYSTEM\CurrentControlSet\Enum\" + devType + "\\" + deviceInstanceId + "\\" + deviceUniqueId;
                using (var key = Registry.LocalMachine.OpenSubKey(regPath))
                {
                    if (key == null)
                    {
                        return _name;
                    }

                    if (key.GetValue("FriendlyName") is string result)
                    {
                        return result;
                    }
                    result = key.GetValue("DeviceDesc") as string;
                    if (result != null)
                    {
                        // Example: @msclmd.inf,%scmspivcarddevicename%;Identifizierungsgerät (NIST SP 800-73 [PIV])
                        var semiColonIndex = result.LastIndexOf(';');
                        if (semiColonIndex >= 0)
                        {
                            return result.Substring(semiColonIndex + 1);
                        }
                        return result;
                    }
                }
                return _name;

            }
        }

        /// <summary>
        /// Use an enum for handling the device class, instead of guid
        /// </summary>
        public DeviceInterfaceClass DeviceClass
        {
            get
            {
                var guidToFind = _classGuid.ToString();
                foreach (var deviceClass in Enum.GetValues(typeof(DeviceInterfaceClass)).Cast<DeviceInterfaceClass>())
                {
                    var descriptionAttribute = GetAttributeOfType<DescriptionAttribute>(deviceClass);
                    if (descriptionAttribute == null || !string.Equals(guidToFind, descriptionAttribute.Description, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    return deviceClass;
                }

                return DeviceInterfaceClass.Unknown;
            }

            set
            {
                var descriptionAttribute = GetAttributeOfType<DescriptionAttribute>(value);
                if (descriptionAttribute != null)
                {
                    _classGuid = Guid.Parse(descriptionAttribute.Description);
                }
            }

        }

        private static T GetAttributeOfType<T>(Enum enumVal) where T : Attribute
        {
            var type = enumVal.GetType();
            var memInfo = type.GetMember(enumVal.ToString());
            var attributes = memInfo[0].GetCustomAttributes(typeof(T), false);
            return attributes.Length > 0 ? (T)attributes[0] : null;
        }
    }
}
