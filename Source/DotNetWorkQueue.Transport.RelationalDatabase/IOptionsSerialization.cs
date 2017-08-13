using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetWorkQueue.Transport.RelationalDatabase
{
    public interface IOptionsSerialization
    {
        byte[] ConvertToBytes();
    }
}
