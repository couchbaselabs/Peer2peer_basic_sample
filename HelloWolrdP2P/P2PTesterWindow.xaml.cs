using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Couchbase.Lite.P2P;
using Couchbase.Lite;
using Couchbase.Lite.Sync;
using Couchbase.Lite.Query;
using Couchbase.Lite.Logging;

using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.IO;

namespace HelloWolrdP2P
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Constants
        // used by listener
        const string ListenerCommonName = "com.example.list-sync-server";

        const string ListenerCertLabel = "list-sync-server-cert-label";
        //const string ListenerCertKeyP12File = "listener-cert-pkey";  // valid for pinned cert. test
        const string ListenerCertKeyP12File = "peerlistener-fleray-test-fr"; // valid for well known CA test.

        const string ListenerCertKeyExportPassword = "couchbase";

        // used by replicator
        const string ListenerPinnedCertFile = "listener-pinned-cert";

        const string PeerEndpointString = "peerlistener.fleray.test.fr";
        #endregion


        private Database _db;
        private URLEndpointListener _thisListener;
        private string portListener;

        private string validUser = "user";
        private string validPassword = "password";

        private X509Store _store;

        public MainWindow()
        {
            InitializeComponent();

            // Set true to get CBL Console Logs (Verbose) and additional app debug messages.
            CoreApp.IsDebugging = true;
            CoreApp.LoadAndInitDB();
            _db = CoreApp.DB;

            using (_store = new X509Store(StoreName.My))
            {
                TLSIdentity.DeleteIdentity(_store, ListenerCertLabel, null);
            }

            if (CoreApp.IsDebugging)
            {
                Database.Log.Console.Level = LogLevel.Verbose;
            }

            CoreApp.RequiresUserAuth = true;

            // Couchbase.Lite
           //  thisDB = new Database("myBase");
        }



        private void Button_Click_Listener(object sender, RoutedEventArgs e)
        {
            var listenerConfig = new URLEndpointListenerConfiguration(_db);

            // string DBPath2 = System.IO.Path.Combine(System.IO.Path.GetTempPath().Replace("cache", "files"), "myTestBase");
            // MessageBox.Show(DBPath2);

            listenerConfig.Authenticator =
              new ListenerPasswordAuthenticator(
                (sender, username, password) =>
                {

                    string pass = Utils.SecureStringToString(password);
                    /*
                    MessageBox.Show(username.ToString());
                    MessageBox.Show(pass);

                    MessageBox.Show("Authentifcation succeed? " + (username.Equals(validUser) && pass.Equals(validPassword)));
                    */
                    return username.ToString().Equals(validUser) && (pass.Equals(validPassword));
                }
              );


            switch (CoreApp.ListenerTLSMode)
            { // <2>
                //tag::TLSDisabled[]
                case LISTENER_TLS_MODE.DISABLED:
                    listenerConfig.DisableTLS = true;
                    listenerConfig.TlsIdentity = null;
                    //end::TLSDisabled[]
                    break;
                //tag::TLSWithAnonymousAuth[]
                case LISTENER_TLS_MODE.WITH_ANONYMOUS_AUTH:
                    listenerConfig.DisableTLS = false; // Use with anonymous self signed cert if TlsIdentity is null
                    listenerConfig.TlsIdentity = null;
                    //end::TLSWithAnonymousAuth[]
                    break;
                //tag::TLSWithBundledCert[]
                case LISTENER_TLS_MODE.WITH_BUNDLED_CERT:
                    listenerConfig.DisableTLS = false;
                    listenerConfig.TlsIdentity = ImportTLSIdentityFromPkc12(ListenerCertLabel);
                    //end::TLSWithBundledCert[]
                    break;
                //tag::TLSWithGeneratedSelfSignedCert[]
                case LISTENER_TLS_MODE.WITH_GENERATED_SELF_SIGNED_CERT:
                    listenerConfig.DisableTLS = false;
                    listenerConfig.TlsIdentity = CreateIdentityWithCertLabel(ListenerCertLabel);
                    //end::TLSWithGeneratedSelfSignedCert[]
                    break;
            }

            listenerConfig.EnableDeltaSync = true;

            _thisListener = new URLEndpointListener(listenerConfig);

            _thisListener.Start();

            listenerPort.Content = _thisListener.Port.ToString();
            listenerPort.FontStyle = FontStyles.Normal;

            /*MessageBox.Show(_thisListener.Port.ToString());
            foreach (Uri uri in _thisListener.Urls)
            {
                MessageBox.Show(uri.ToString());
                break;
            }
            */

        }

        private void Button_Click_Replicator(object sender, RoutedEventArgs e)
        {
            string uriString = CoreApp.PeerEndpointStringWithProcotol(PeerEndpointString) + ":" + portListener;

            Uri hostListener = new Uri(uriString);
            var dbUrl = new Uri(hostListener, _db.Name);
            
            MessageBox.Show(dbUrl.ToString());

            var theListenerEndpoint = new URLEndpoint(dbUrl);

            var replicatorConfig = new ReplicatorConfiguration(_db, theListenerEndpoint);

            replicatorConfig.AcceptOnlySelfSignedServerCertificate = true;
            replicatorConfig.ReplicatorType = ReplicatorType.PushAndPull;
            replicatorConfig.Continuous = true;

            replicatorConfig.Authenticator =
              new BasicAuthenticator(validUser, validPassword);

            // add TLS part for replicator

            //if (CoreApp.ListenerTLSMode > 0)
            if(CoreApp.UseTLSMode)
            {

                // Explicitly allows self signed certificates. By default, only
                // CA signed cert is allowed
                switch (CoreApp.ListenerCertValidationMode)
                { // <2>
                    case LISTENER_CERT_VALIDATION_MODE.SKIP_VALIDATION:
                        // Use acceptOnlySelfSignedServerCertificate set to true to only accept self signed certs.
                        // There is no cert validation
                        replicatorConfig.AcceptOnlySelfSignedServerCertificate = true;
                        break;

                    case LISTENER_CERT_VALIDATION_MODE.ENABLE_VALIDATION_WITH_CERT_PINNING:
                        // Use acceptOnlySelfSignedServerCertificate set to false to only accept CA signed certs
                        // Self signed certs will fail validation

                        replicatorConfig.AcceptOnlySelfSignedServerCertificate = false;

                        // Enable cert pinning to only allow certs that match pinned cert

                        try
                        {
                            var pinnedCert = LoadSelfSignedCertForListenerFromBundle();
                            replicatorConfig.PinnedServerCertificate = pinnedCert;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Failed to load server cert to pin. Will proceed without pinning. {ex}");
                        }

                        break;

                    case LISTENER_CERT_VALIDATION_MODE.ENABLE_VALIDATION:
                        // Use acceptOnlySelfSignedServerCertificate set to false to only accept CA signed certs
                        // Self signed certs will fail validation. There is no cert pinning
                        replicatorConfig.AcceptOnlySelfSignedServerCertificate = false;
                        break;
                }
            }



            var thisReplicator = new Replicator(replicatorConfig);

            thisReplicator.Start();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Console.WriteLine(e.ToString());
            TextBox tb = (TextBox)e.Source;

            portListener = tb.Text;
        }

        private void Button_Click_Add_One_Doc(object sender, RoutedEventArgs e)
        {
            string uuid = "my_doc_" + System.Guid.NewGuid().ToString();
            MutableDocument mutableDoc = new MutableDocument( uuid);
            mutableDoc.SetString("id_field", uuid);
            mutableDoc.SetString("titi", "toto");
            mutableDoc.SetInt("titiInt", 4);

            _db.Save(mutableDoc);
        }

        private void Button_Click_Get_All_docs(object sender, RoutedEventArgs e)
        {

            var query = QueryBuilder.Select(SelectResult.All()).From(DataSource.Database(_db));

            var results = query.Execute().AllResults();

            var resultString = "";
            foreach (var val in results.Select(r => r.GetDictionary(_db.Name)).ToArray())
            {
                foreach (var i in val)
                {
                    resultString += i.Key + "\t" + i.Value + "\n";
                }
                resultString += " ----\n";
            }

            MessageBox.Show(resultString);

        }



        #region Server TLSIdentity
        // Used by listener
        internal TLSIdentity ImportTLSIdentityFromPkc12(string label)
        {
            using (_store = new X509Store(StoreName.My))
            {
                // Check if identity exists, use the id if it is.
                var id = TLSIdentity.GetIdentity(_store, label, null);
                if (id != null)
                {
                    return id;
                }

                try
                {
                    byte[] data = null;
                    using (var stream = ResourceLoader.GetEmbeddedResourceStream(typeof(CoreApp).Assembly, $"{ListenerCertKeyP12File}.p12"))
                    {
                        using (var reader = new BinaryReader(stream))
                        {
                            data = reader.ReadBytes((int)stream.Length);
                        }
                    }

                    id = TLSIdentity.ImportIdentity(_store, data, ListenerCertKeyExportPassword, label, null);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error while loading self signed cert : {ex}");
                }

                return id;
            }
        }

        // Used by listener too
        internal TLSIdentity CreateIdentityWithCertLabel(string label)
        {
            using (_store = new X509Store(StoreName.My))
            {
                // Check if identity exists, use the id if it is.
                var id = TLSIdentity.GetIdentity(_store, label, null);
                if (id != null)
                {
                    return id;
                }

                try
                {
                    id = TLSIdentity.CreateIdentity(true,
                    new Dictionary<string, string>() { { Certificate.CommonNameAttribute, ListenerCommonName } },
                    null,
                    _store,
                    label,
                    null);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error while creating self signed cert : {ex}");
                }

                return id;
            }
        }

        // Used by replicator
        private X509Certificate2 LoadSelfSignedCertForListenerFromBundle()
        {
            using (var cert = ResourceLoader.GetEmbeddedResourceStream(typeof(CoreApp).Assembly, $"{ListenerPinnedCertFile}.cer"))
            {
                using (var ms = new MemoryStream())
                {
                    cert.CopyTo(ms);
                    return new X509Certificate2(ms.ToArray());
                }
            }
        }
        #endregion

        private void TLSMode_Changed(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = (ComboBox) e.Source;
            CoreApp.ListenerTLSMode = (LISTENER_TLS_MODE) cb.SelectedItem;

            if(null != ListenerTLSDesc)
            {
                ListenerTLSDesc.Text = TLSSettingsData.ListenerTLSMode.ElementAt(cb.SelectedIndex).Description;
                ListenerTLSDesc.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            }
            // MessageBox.Show(CoreApp.ListenerTLSMode.ToString());
        }

        private void CertValidationMode_Changed(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = (ComboBox) e.Source;
            CoreApp.ListenerCertValidationMode = (LISTENER_CERT_VALIDATION_MODE) cb.SelectedItem;

            if (null != ListenerValidationDesc)
            {
                ListenerValidationDesc.Text = TLSSettingsData.ListenerCertValidationMode.ElementAt(cb.SelectedIndex).Description;
                ListenerValidationDesc.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            }

            // MessageBox.Show(CoreApp.ListenerTLSMode.ToString());
        }

        private void radioUseTLSFalseChecked(object sender, RoutedEventArgs e)
        {
            RadioButton rb = (RadioButton) e.Source;
            CoreApp.UseTLSMode = (bool) !(rb.IsChecked);

            radioUseTLSFalse.IsChecked = true;
            if (null != radioUseTLSTrue)
            {
                radioUseTLSTrue.IsChecked = (bool)!(radioUseTLSFalse.IsChecked);
            }

            if (null != ListenerValidationDesc)
            {
                ListenerValidationDesc.IsEnabled = false;
            }

            if (null != combo_Cert_Validation_mode)
            {
                combo_Cert_Validation_mode.IsEnabled = false;
            }

            // MessageBox.Show(CoreApp.ListenerTLSMode.ToString());
        }


        private void radioUseTLSTrueChecked(object sender, RoutedEventArgs e)
        {
            RadioButton rb = (RadioButton)e.Source;
            CoreApp.UseTLSMode = (bool) (rb.IsChecked);


            radioUseTLSTrue.IsChecked = true;

            if (null != radioUseTLSFalse)
            {
                radioUseTLSFalse.IsChecked = (bool)!(radioUseTLSTrue.IsChecked);
            }

            if (null != ListenerValidationDesc)
            {
                ListenerValidationDesc.IsEnabled = true;
            }

            if (null != combo_Cert_Validation_mode)
            {
                combo_Cert_Validation_mode.IsEnabled = true;
            }

            // MessageBox.Show(CoreApp.ListenerTLSMode.ToString());
        }


    }

}
