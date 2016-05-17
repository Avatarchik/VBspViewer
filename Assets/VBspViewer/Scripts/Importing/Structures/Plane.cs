﻿using System.Runtime.InteropServices;

namespace VBspViewer.Importing.Structures
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Plane
    {
        public Vector Normal;
        public float Distance;
        public int Type;
    }
}
