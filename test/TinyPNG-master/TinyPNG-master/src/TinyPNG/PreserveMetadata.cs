using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyPng
{
    [Flags]
    public enum PreserveMetadata
    {
        None = 1 << 0,
        Copyright = 1 << 1,
        Creation = 1 << 2,
        Location = 1 << 3
    }
}
