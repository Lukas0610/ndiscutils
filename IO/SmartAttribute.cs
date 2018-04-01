/*
 * nDiscUtils - Advanced utilities for disc management
 * Copyright (C) 2018  Lukas Berger
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

namespace nDiscUtils.IO
{

    public sealed class SmartAttribute
    {

        private static readonly SmartAttribute[] KnownAttributes = new SmartAttribute[]
        {
            new SmartAttribute(0x01, "Read error rate", SmartValueIdeality.Low, false),
            new SmartAttribute(0x02, "Throughput Performance", SmartValueIdeality.High, false),
            new SmartAttribute(0x03, "Spin-Up Time", SmartValueIdeality.Low, false),
            new SmartAttribute(0x04, "Start/Stop Count", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0x05, "Reallocated Sectors Count", SmartValueIdeality.Low, true),
            new SmartAttribute(0x06, "Read Channel Margin", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0x07, "Seek Error Rate", SmartValueIdeality.Varies, false),
            new SmartAttribute(0x08, "Seek Time Performance", SmartValueIdeality.High, false),
            new SmartAttribute(0x09, "Power-On Hours", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0x0A, "Spin Retry Count", SmartValueIdeality.Low, true),
            new SmartAttribute(0x0B, "Calibration Retry Count", SmartValueIdeality.Low, false),
            new SmartAttribute(0x0C, "Power Cycle Count", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0x0D, "Soft Read Error Rate", SmartValueIdeality.Low, false),
            new SmartAttribute(0x16, "Current Helium Level", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0xAA, "Available Reserved Space", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0xAB, "SSD Program Fail Count", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0xAC, "SSD Erase Fail Count", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0xAD, "SSD Wear Leveling Count", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0xAE, "Unexpected power loss count", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0xAF, "Power Loss Protection Failure", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0xB0, "Erase Fail Count", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0xB1, "Wear Range Delta", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0xB3, "Used Reserved Block Count Total", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0xB4, "Unused Reserved Block Count Total", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0xB5, "Non-4K Aligned Access Count", SmartValueIdeality.Low, false),
            new SmartAttribute(0xB6, "Erase Fail Count", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0xB7, "SATA Downshift Error Count", SmartValueIdeality.Low, false),
            new SmartAttribute(0xB8, "End-to-End Error Count", SmartValueIdeality.Low, true),
            new SmartAttribute(0xB9, "Head Stability", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0xBA, "Induced Op-Vibration Detection", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0xBB, "Reported Uncorrectable Errors", SmartValueIdeality.Low, true),
            new SmartAttribute(0xBC, "Command Timeout", SmartValueIdeality.Low, true),
            new SmartAttribute(0xBD, "High Fly Writes", SmartValueIdeality.Low, false),
            new SmartAttribute(0xBE, "Airflow Temperature", SmartValueIdeality.Varies, false),
            new SmartAttribute(0xBF, "G-sense Error Rate", SmartValueIdeality.Low, false),
            new SmartAttribute(0xC0, "Power-off Retract Count", SmartValueIdeality.Low, false),
            new SmartAttribute(0xC1, "Load Cycle Count", SmartValueIdeality.Low, false),
            new SmartAttribute(0xC2, "Temperature Celsius", SmartValueIdeality.Low, false),
            new SmartAttribute(0xC3, "Hardware ECC Recovered", SmartValueIdeality.Varies, false),
            new SmartAttribute(0xC4, "Reallocation Event Count", SmartValueIdeality.Low, true),
            new SmartAttribute(0xC5, "Current Pending Sector Count", SmartValueIdeality.Low, true),
            new SmartAttribute(0xC6, "Offline/Uncorrectable Sector Count", SmartValueIdeality.Low, true),
            new SmartAttribute(0xC7, "UltraDMA CRC Error Count", SmartValueIdeality.Low, false),
            new SmartAttribute(0xC8, "Multi-Zone Error Rate", SmartValueIdeality.Low, false),
            new SmartAttribute(0xC9, "Soft Read Error Rate", SmartValueIdeality.Low, true),
            new SmartAttribute(0xCA, "Data Address Mark errors", SmartValueIdeality.Low, false),
            new SmartAttribute(0xCB, "Run Out Cancel", SmartValueIdeality.Low, false),
            new SmartAttribute(0xCC, "Soft ECC Correction", SmartValueIdeality.Low, false),
            new SmartAttribute(0xCD, "Thermal Asperity Rate", SmartValueIdeality.Low, false),
            new SmartAttribute(0xCE, "Flying Height", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0xCF, "Spin High Current", SmartValueIdeality.Low, false),
            new SmartAttribute(0xD0, "Spin Buzz", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0xD1, "Offline Seek Performance", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0xD2, "Vibration During Write", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0xD3, "Vibration During Write", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0xD4, "Shock During Write", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0xDC, "Disk Shift", SmartValueIdeality.Low, false),
            new SmartAttribute(0xDD, "G-Sense Error Rate", SmartValueIdeality.Low, false),
            new SmartAttribute(0xDE, "Loaded Hours", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0xDF, "Load/Unload Retry Count", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0xE0, "Load Friction", SmartValueIdeality.Low, false),
            new SmartAttribute(0xE1, "Load/Unload Cycle Count", SmartValueIdeality.Low, false),
            new SmartAttribute(0xE2, "Load 'In'-time", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0xE3, "Torque Amplification Count", SmartValueIdeality.Low, false),
            new SmartAttribute(0xE4, "Power-Off Retract Cycle", SmartValueIdeality.Low, false),
            new SmartAttribute(0xE6, "GMR Head Amplitude", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0xE6, "Drive Life Protection Status", SmartValueIdeality.Unknown, false, HardDiskType.SSD),
            new SmartAttribute(0xE7, "Temperature", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0xE7, "Life Left", SmartValueIdeality.Unknown, false, HardDiskType.SSD),
            new SmartAttribute(0xE8, "Available Reserved Space", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0xE9, "Power-On Hours", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0xE9, "Media Wearout Indicator", SmartValueIdeality.Unknown, false, HardDiskType.SSD),
            new SmartAttribute(0xEA, "Erase count", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0xEB, "Block Count", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0xF0, "Head Flying Hours", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0xF1, "Total LBAs Written", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0xF2, "Total LBAs Read", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0xF3, "Total LBAs Written Expanded", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0xF4, "Total LBAs Read Expanded", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0xF9, "NAND Writes (1GiB)", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0xFA, "Read Error Retry Rate", SmartValueIdeality.Low, false),
            new SmartAttribute(0xFB, "Minimum Spares Remaining", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0xFC, "Newly Added Bad Flash Block", SmartValueIdeality.Unknown, false),
            new SmartAttribute(0xFE, "Free Fall Protection", SmartValueIdeality.Low, false),
        };

        public int ID { get; }

        public string Name { get; }

        public SmartValueIdeality Ideal { get; }

        public bool Critical { get; }

        public HardDiskType DiskType { get; }

        private SmartAttribute(int id, string name, SmartValueIdeality ideal, bool critical)
            : this(id, name, ideal, critical, HardDiskType.HDD | HardDiskType.SSD)
        { }

        private SmartAttribute(int id, string name, SmartValueIdeality ideal, bool critical, HardDiskType diskType)
        {
            this.ID = id;
            this.Name = name;
            this.Ideal = ideal;
            this.Critical = critical;
            this.DiskType = diskType;
        }

        public static SmartAttribute GetAttribute(int attributeId, HardDiskType diskType)
        {
            foreach (var attribute in KnownAttributes)
            {
                if (attribute.ID == attributeId && (attribute.DiskType & diskType) != 0)
                    return attribute;
            }
            return null;
        }

    }

}
