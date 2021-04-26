using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Diagnostics;

using Couchbase.Lite.P2P;

namespace HelloWolrdP2P
{
    public class Utils
    {
        public static String SecureStringToString(System.Security.SecureString value)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }


        //public static string GetLocalIPv4(NetworkInterfaceType type)
        //{
        //    string output = null;
        //    foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
        //    {
        //        if (item.NetworkInterfaceType == type && item.OperationalStatus == OperationalStatus.Up)
        //        {
        //            IPInterfaceProperties adapterProperties = item.GetIPProperties();
        //            if (adapterProperties.GatewayAddresses.FirstOrDefault() != null)
        //            {
        //                foreach (UnicastIPAddressInformation ip in adapterProperties.UnicastAddresses)
        //                {
        //                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
        //                    {
        //                        output = ip.Address.ToString();
        //                        break;
        //                    }
        //                }
        //            }
        //        }

        //        if (output != null) { break; }
        //    }

        //    return output;
        //}

        public static void PrintListener(URLEndpointListener listener)
        {
            Debug.WriteLine("Listener Config TLSIdentity info: ");
            if (listener?.Config.TlsIdentity == null)
            {
                Debug.WriteLine("No TLSIdentity to print.");
            }
            else
            {
                PrintTLSIdentity(listener.Config.TlsIdentity);
            }

            Debug.WriteLine("Listener TLSIdentity info: ");
            if (listener?.TlsIdentity == null)
            {
                Debug.WriteLine("No TLSIdentity to print.");
            }
            else
            {
                PrintTLSIdentity(listener.TlsIdentity);
            }
        }

        public static void PrintTLSIdentity(TLSIdentity id)
        {
            var certs = id.Certs;
            if (certs == null)
            {
                Debug.WriteLine("No certs to print.");
                return;
            }

            foreach (var x509 in certs)
            {
                //Print to console information contained in the certificate.
                Debug.WriteLine("{0}Subject: {1}{0}", Environment.NewLine, x509.Subject);
                Debug.WriteLine("{0}Issuer: {1}{0}", Environment.NewLine, x509.Issuer);
                Debug.WriteLine("{0}Version: {1}{0}", Environment.NewLine, x509.Version);
                Debug.WriteLine("{0}Valid Date: {1}{0}", Environment.NewLine, x509.NotBefore);
                Debug.WriteLine("{0}Expiry Date: {1}{0}", Environment.NewLine, x509.NotAfter);
                Debug.WriteLine("{0}Thumbprint: {1}{0}", Environment.NewLine, x509.Thumbprint);
                Debug.WriteLine("{0}Serial Number: {1}{0}", Environment.NewLine, x509.SerialNumber);
                Debug.WriteLine("{0}Friendly Name: {1}{0}", Environment.NewLine, x509.PublicKey.Oid.FriendlyName);
                Debug.WriteLine("{0}Public Key Format: {1}{0}", Environment.NewLine, x509.PublicKey.EncodedKeyValue.Format(true));
                Debug.WriteLine("{0}Raw Data Length: {1}{0}", Environment.NewLine, x509.RawData.Length);
                Debug.WriteLine("{0}Certificate to string: {1}{0}", Environment.NewLine, x509.ToString(true));
                //Debug.WriteLine("{0}Certificate to XML String: {1}{0}", Environment.NewLine, x509.PublicKey.Key.ToXmlString(false));
            }
        }

    }
}
