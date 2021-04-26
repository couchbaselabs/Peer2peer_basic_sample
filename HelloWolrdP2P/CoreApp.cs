//
// CoreApp.cs
//
// Author:
// 	Sandy Chuang
//
//  Copyright © 2020 Couchbase Inc. All rights reserved.
//

using Couchbase.Lite;
using System;
using System.IO;

namespace HelloWolrdP2P
{
    /// <summary>
    /// Switch between these various modes to try various auth types
    /// </summary>
    public enum LISTENER_TLS_MODE 
    {
        DISABLED,
        WITH_ANONYMOUS_AUTH,
        WITH_BUNDLED_CERT, // Bring your own cert (self-signed or CA)
        WITH_GENERATED_SELF_SIGNED_CERT // Use convenience API to generate cert
    }

    /// <summary>
    /// Switch between these various modes to try various auth types
    /// </summary>
    public enum LISTENER_CERT_VALIDATION_MODE
    {
        SKIP_VALIDATION,
        ENABLE_VALIDATION, // Used for CA cert validation
        ENABLE_VALIDATION_WITH_CERT_PINNING // User for self signed cert
    }

    public sealed class CoreApp
    {
        public static string DbName = "my_local_db";
        
        #region Properties

        /// <summary>
        /// Flag to determine if we need to login to the app and set user password auth for listener 
        /// when LISTENER_TLS_MODE is DISABLED 
        /// </summary>
        public static bool RequiresUserAuth { get; set; }

        public static bool IsDebugging { get; set; }

        public static Database DB { get; private set; }
        internal static string DBPath => Path.Combine(Path.GetTempPath().Replace("cache", "files"), "myTestBase");

        //       public static List<User> AllowedUsers { get; private set; }
        //       public static User CurrentUser { get; set; }

        /// <summary>
        /// Switch between listener auth modes.
        /// </summary>
        //tag::ListenerTLSTestMode[]
        public static LISTENER_TLS_MODE ListenerTLSMode = LISTENER_TLS_MODE.DISABLED;
        //public static LISTENER_TLS_MODE ListenerTLSMode = LISTENER_TLS_MODE.WITH_ANONYMOUS_AUTH;
        // public static LISTENER_TLS_MODE ListenerTLSMode = LISTENER_TLS_MODE.WITH_BUNDLED_CERT;
        //end::ListenerTLSTestMode[]

        /// <summary>
        /// Skip validation for self signed certs
        /// </summary>
        //tag::ListenerValidationTestMode[]
        public static LISTENER_CERT_VALIDATION_MODE ListenerCertValidationMode = LISTENER_CERT_VALIDATION_MODE.SKIP_VALIDATION;
        //end::ListenerValidationTestMode[]

        public static Boolean UseTLSMode = false;
        #endregion

        #region Public Methods
        public static void LoadAndInitDB()
        {
            // Enable this or uninstall app to reset db
            //Database.Delete(DbName, DBPath); 
            //tag::OpenOrCreateDatabase[]
            /*
            if (!Database.Exists(DbName, DBPath)) {
                using (var dbZip = new ZipArchive(ResourceLoader.GetEmbeddedResourceStream(typeof(CoreApp).GetTypeInfo().Assembly, $"{DbName}.cblite2.zip"))) {
                    dbZip.ExtractToDirectory(DBPath);
                }
            }
            */

            Console.WriteLine("DBPath = " + DBPath);
            DB = new Database(DbName, new DatabaseConfiguration() { Directory = DBPath });
            //end::OpenOrCreateDatabase[]
        }

        public static string PeerEndpointStringWithProcotol(string PeerEndpointString)
        {
            return !CoreApp.UseTLSMode ? $"ws://{PeerEndpointString}" : $"wss://{PeerEndpointString}";
        }
        #endregion
    }
}
