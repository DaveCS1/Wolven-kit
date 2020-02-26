﻿using RED.FNV1A;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace WolvenKit.CR2W
{
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct CR2WName
    {
        [FieldOffset(0)]
        public uint value;

        [FieldOffset(4)]
        public uint hash;
    }

    public class CR2WNameWrapper
    {
        private CR2WName _name;
        private string str1;

        public CR2WName Name
        {
            get
            {
                _name.hash = FNV1A32HashAlgorithm.HashString(str, Encoding.ASCII, true);
                return _name;
            }
            set => _name = value;
        }

        public string str
        {
            get
            {
                return str1;
            }
            set
            {
                str1 = value;
                _name.hash = FNV1A32HashAlgorithm.HashString(str1, Encoding.ASCII, true);
            }
        }

        public CR2WNameWrapper()
        {
            _name = new CR2WName();
        }

        public CR2WNameWrapper(CR2WName name)
        {
            _name = name;
        }

        public void SetOffset(uint offset) => _name.value = offset;
    }
}