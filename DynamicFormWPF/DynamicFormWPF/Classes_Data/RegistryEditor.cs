namespace DynamicFormWPF.Classes_Data
{
    using System;
    using System.Configuration;
    using System.Data.SqlClient;
    using System.IO;
    using Microsoft.Win32;

    public class RegistryEditor
    {
        public static void setNewConfigSettingWithConnectionString(string connectionString, bool isClient)
        {
            if (isClient)
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                config.ConnectionStrings.ConnectionStrings["SEISConnectionString"].ConnectionString = connectionString;

                // save and refresh the config file
                config.Save(ConfigurationSaveMode.Minimal);
                ConfigurationManager.RefreshSection("connectionStrings");

                setConnectionStringToRegistry(connectionString);
            }

            else
            {
                // get current connectionstring
                SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder(connectionString);

                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                // set the new values
                config.ConnectionStrings.ConnectionStrings["SEISConnectionString"].ConnectionString = csb.ConnectionString;

                // save and refresh the config file
                config.Save(ConfigurationSaveMode.Minimal);
                ConfigurationManager.RefreshSection("connectionStrings");

                DB.refreshCSBConnectionString();

                setConnectionStringToRegistry(csb.ConnectionString);
            }
        }

        public static void setConnectionStringToRegistry(string value)
        {
            RegistryKey appKey = Registry.CurrentUser.CreateSubKey(@"Software\SEIS");
            appKey.SetValue("DatabasePath", value);
        }

        // get file path value stored in registry
        public static string getConnectionStringFromRegistry()
        {
            string info = string.Empty;
            RegistryKey appKey = Registry.CurrentUser.OpenSubKey(@"Software\SEIS");
            try
            {
                info = appKey.GetValue("DatabasePath", "").ToString();
            }
            catch (Exception)
            {
                info = "";
            }
            return info;
        }

        // update connectionstring with the new selected DB and write to registry
        public static void setNewConfigSettingWithDBName(string dbName)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["SEISConnectionString"].ConnectionString;

            // get current connectionstring
            SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder(connectionString) { ConnectTimeout = 5, InitialCatalog = dbName };

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            // set the new values
            config.ConnectionStrings.ConnectionStrings["SEISConnectionString"].ConnectionString = csb.ConnectionString;

            // save and refresh the config file
            config.Save(ConfigurationSaveMode.Minimal);
            ConfigurationManager.RefreshSection("connectionStrings");

            DB.refreshCSBConnectionString();

            setConnectionStringToRegistry(csb.ConnectionString);
        }

        // define client or server
        public static bool isServer(ref bool isClient)
        {
            bool isServer = false;

            string content = string.Empty;

            // read content from server.seis file and decrypt to match the key
            content = XMLProcessor.readStringFromText(AppDomain.CurrentDomain.BaseDirectory + "server.seis");

            //
            if (DataEncryption.Decrypt(content, true) == "SEIS-Server")
            {
                isClient = false;
                isServer = true;
            }

            else
            {
                // no need to show notification
                //MessageBox.Show("Không xác định được file nhận diện server" + Environment.NewLine + "Chương trình khởi tạo dưới chế độ máy trạm", "Thông báo");
                isClient = true;
                isServer = false;
            }

            return isServer;
        }

        public static void checkDataFolder()
        {
            if (!Directory.Exists(@"C:\data"))
            {
                Directory.CreateDirectory(@"C:\data");
            }

            if (!Directory.Exists(@"C:\data\temp"))
            {
                Directory.CreateDirectory(@"C:\data\temp");
            }
        }
    }
}