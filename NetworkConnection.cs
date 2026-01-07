
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace EaiClassAdapter
{
    internal sealed class NetworkConnection : IDisposable
    {
        private readonly string _networkName;

        public NetworkConnection(string networkName, string user, string password)
        {
            _networkName = networkName;
            var nr = new NetResource
            {
                Scope = 2,
                ResourceType = 1,
                DisplayType = 3,
                RemoteName = networkName
            };
            int result = WNetAddConnection2(nr, password, user, 0);
            if (result != 0)
                throw new Win32Exception(result);
        }

        public void Dispose()
        {
            WNetCancelConnection2(_networkName, 0, true);
        }

        [DllImport("mpr.dll")]
        private static extern int WNetAddConnection2(NetResource netResource, string password, string username, int flags);
        [DllImport("mpr.dll")]
        private static extern int WNetCancelConnection2(string name, int flags, bool force);

        [StructLayout(LayoutKind.Sequential)]
        private class NetResource
        {
            public int Scope;
            public int ResourceType;
            public int DisplayType;
            public int Usage;
            public string LocalName;
            public string RemoteName;
            public string Comment;
            public string Provider;
        }
    }
}
