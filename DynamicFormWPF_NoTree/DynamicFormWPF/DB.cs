// -----------------------------------------------------------------------
// <copyright file="DB.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace DynamicFormWPF
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Data;
    using System.Data.OleDb;
    using System.Configuration;
    using System.Xml.Linq;
    using ADOX;
    using Microsoft.Win32;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class DB
    {
        public static readonly OleDbConnectionStringBuilder csb;
        private static string databasePath;

        static DB()
        {
            // register the DB file path into registry
            // string filePath
            try
            {
                csb = new OleDbConnectionStringBuilder();
                // get connectionString in app.config
                // add System.configuration to references beforehand
                csb.ConnectionString = ConfigurationManager.ConnectionStrings["DynamicFormWPF.Properties.Settings.KTXHCTConnectionString"].ConnectionString;
            }
            catch (OleDbException)
            {

            }
        }

        public static bool isConnectedToDB()
        {
            bool isConnected = false;

            OleDbConnection conn = new OleDbConnection();
            conn.ConnectionString = csb.ConnectionString;

            try
            {
                conn.Open();
                isConnected = true;
            }
            catch (OleDbException)
            {
                isConnected = false;
            }
            finally
            {
                conn.Close();
            }

            return isConnected;
        }

        public static bool isDataEmpty()
        {
            bool isEmpty = false;

            OleDbConnection conn = new OleDbConnection();
            conn.ConnectionString = csb.ConnectionString;

            OleDbCommand cmd = new OleDbCommand();

            cmd.Connection = conn;
            cmd.CommandText = "SELECT * FROM tblTarget";

            try
            {
                conn.Open();
                OleDbDataReader odr = cmd.ExecuteReader();
                if (odr.Read())
                {
                    // data exists
                    isEmpty = false;
                }
                else
                {
                    // no data
                    isEmpty = true;
                }
            }
            catch (OleDbException)
            {
                isEmpty = false;
            }
            finally
            {
                conn.Close();
            }
            return isEmpty;
        }

        public static void refreshCSBConnectionString()
        {
            csb.ConnectionString = ConfigurationManager.ConnectionStrings["DynamicFormWPF.Properties.Settings.KTXHCTConnectionString"].ConnectionString;
        }

        // get file path value stored in registry
        public static string getDBPathFromRegistry()
        {
            string info = string.Empty;
            RegistryKey appKey = Registry.CurrentUser.OpenSubKey(@"Software\" + Registry.CurrentUser.Name + @"\SEIS");
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

        // create new physically database
        public static string createNewDB(string filePath)
        {
            string info = string.Empty;

            CatalogClass cat = new CatalogClass();
            string tmpStr;
            //tmpStr = "Provider=Microsoft.Jet.OLEDB.4.0;";
            //tmpStr += "Data Source=" + filePath + ";Jet OLEDB:Engine Type=5";  
            tmpStr = "Provider=Microsoft.ACE.OLEDB.12.0;";
            tmpStr += "Data Source=" + filePath + ";Jet OLEDB:Engine Type=5";
            try
            {
                // check exist
                cat.Create(tmpStr);

                //Table nTable = new Table();
                //nTable.Name = "PersonData";
                //nTable.Columns.Append("LastName", DataTypeEnum.adVarWChar,);
                //nTable.Columns.Append("FirstName", DataTypeEnum.adVarWChar, 25);
                //nTable.Columns.Append("Address 1", DataTypeEnum.adVarWChar, 45);
                //nTable.Columns.Append("Address 2", DataTypeEnum.adVarWChar, 45);
                //nTable.Columns.Append("City", DataTypeEnum.adVarWChar, 25);
                //nTable.Columns.Append("State", DataTypeEnum.adVarWChar, 2);
                //nTable.Columns.Append("Zip", DataTypeEnum.adVarWChar, 9);
                //cat.Tables.Append(nTable);

                //System.Runtime.InteropServices.Marshal.FinalReleaseComObject(nTable);
                //System.Runtime.InteropServices.Marshal.FinalReleaseComObject(cat.Tables);
                //System.Runtime.InteropServices.Marshal.FinalReleaseComObject(cat.ActiveConnection);
                //System.Runtime.InteropServices.Marshal.FinalReleaseComObject(cat);

                // Add Constraints & Primary Keys
                // AddConstraintsToDB(filePath);

                RegistryKey appKey = Registry.CurrentUser.CreateSubKey(@"Software\" + Registry.CurrentUser.Name + @"\SEIS");
                appKey.SetValue("DatabasePath", filePath);
                changeAppConfigSetting();
                refreshCSBConnectionString();

                // add tables to DB after getting new connectionstring
                info = addTablesToDB();
            }
            catch (Exception e)
            {
                info = e.Message;
            }
            return info;
        }

        // change app.config setting after created database
        public static void changeAppConfigSetting()
        {
            try
            {
                databasePath = DB.getDBPathFromRegistry();
                string connectionString = ConfigurationManager.ConnectionStrings["DynamicFormWPF.Properties.Settings.KTXHCTConnectionString"].ConnectionString;
                // get the config file for this applications
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                // set the new values
                config.ConnectionStrings.ConnectionStrings["DynamicFormWPF.Properties.Settings.KTXHCTConnectionString"].ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + databasePath;

                // save and refresh the config file
                config.Save(ConfigurationSaveMode.Minimal);
                ConfigurationManager.RefreshSection("connectionStrings");
            }
            catch (Exception)
            {
            }
        }

        // add tables to newly created DB
        public static string addTablesToDB()
        {
            string info = string.Empty;

            OleDbConnection conn = new OleDbConnection();
            conn.ConnectionString = csb.ConnectionString;

            OleDbCommand cmd = new OleDbCommand();
            OleDbCommand cmd2 = new OleDbCommand();
            OleDbCommand cmd3 = new OleDbCommand();
            OleDbCommand cmd4 = new OleDbCommand();
            // varchar(MAX) does not work
            // cannot create more than 1 table in 1 sql statement
            // cannot contain "," at the end of the statement

            //cmd2.CommandText = "CREATE TABLE tblTargetLV3(TargetLevel3_ID int Primary Key,TargetName varchar(50) Not Null,UnitCount varchar(10) Not Null,Quantity int,ClientID int,RecordDate datetime,Constraint FK01_Client Foreign Key (ClientID) References tblClient(ClientID))";
            //cmd3.CommandText = "CREATE TABLE tblTargetLV2(TargetLevel2_ID int Primary Key,TargetLevel3_ID int,TargetName varchar(50) Not Null,UnitCount varchar(10) Not Null,Quantity int,ClientID int,RecordDate datetime,Constraint FK01_Target Foreign Key (TargetLevel3_ID) References tblTargetLV3(TargetLevel3_ID),Constraint FK02_Client Foreign Key (ClientID) References tblClient(ClientID))";
            //cmd4.CommandText = "CREATE TABLE tblTargetLV1(TargetLevel1_ID int Primary Key,TargetLevel2_ID int,TargetName varchar(50) Not Null,Constraint FK02_Target Foreign Key (TargetLevel2_ID) References tblTargetLV2(TargetLevel2_ID))";
            cmd.CommandText = " CREATE TABLE tblClient    (ClientID AUTOINCREMENT,ClientName varchar(255), TelephoneNumber int NOT NULL, Primary Key(ClientID))";
            cmd2.CommandText = "CREATE TABLE tblTarget    (TargetID AUTOINCREMENT,ParentID int NOT NULL,TargetName varchar(50),Primary Key(TargetID))";
            //cmd3.CommandText = "CREATE TABLE tblUnitCount (UnitCountID AUTOINCREMENT,UnitCountName varchar(50),Primary Key(UnitCountID))";
            cmd4.CommandText = "CREATE TABLE tblRecord    (RecordID AUTOINCREMENT,TargetID int,QuantityValue int,ClientID int,RecordTime datetime,Primary Key(RecordID),Constraint FK_Target Foreign Key (TargetID) References tblTarget(TargetID),Constraint FK_Client Foreign Key (ClientID) References tblClient(ClientID))";

            cmd.Connection = conn;
            cmd2.Connection = conn;
            //cmd3.Connection = conn;
            cmd4.Connection = conn;

            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
                cmd2.ExecuteNonQuery();
                //cmd3.ExecuteNonQuery();
                cmd4.ExecuteNonQuery();
                info = "Đã tạo thành công CSDL mới";
            }
            catch (OleDbException e)
            {
                info = e.Message;
            }
            finally
            {
                conn.Close();
            }

            return info;
        }

        public static string updateFromXMLtoDB(string targetName, int quantityValue, string clientName, DateTime recordTime)
        {
            string info = string.Empty;

            OleDbConnection conn = new OleDbConnection();
            conn.ConnectionString = csb.ConnectionString;

            OleDbCommand cmd = new OleDbCommand();
            cmd.CommandText = "INSERT INTO tblRecord (TargetID, QuantityValue, ClientID, RecordTime) VALUES (@pTargetID, @pQuantityValue, @pClientID, @pRecordTime)";
            cmd.Connection = conn;

            cmd.Parameters.AddWithValue("pTargetID", getTargetIDorParentIDByName(targetName, 0));
            cmd.Parameters.AddWithValue("pQuantityValue", quantityValue);
            cmd.Parameters.AddWithValue("pClientID", getClientIDByName(clientName));
            cmd.Parameters.AddWithValue("pRecordTime", recordTime);

            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
                info = "OK";
            }
            catch (OleDbException e)
            {
                info = e.Message;
            }
            finally
            {
                conn.Close();
            }
            return info;
        }

        public static void updateContent(string content, string controlName) 
        {
            int targetId = 0;
            int quantityValue = 0;
            DateTime recordTime = new DateTime();

            try
            {
                targetId = Convert.ToInt32(controlName.Substring(4));
                quantityValue = Convert.ToInt32(content);
                // Access does not accept DateTime.Now
                recordTime = DateTime.Parse(String.Format("{0:yyyy-MM-dd}", DateTime.Now));
            }
            catch (Exception) { }

            OleDbConnection conn = new OleDbConnection();
            conn.ConnectionString = csb.ConnectionString;

            OleDbCommand cmd = new OleDbCommand();
            cmd.CommandText = "INSERT INTO tblRecord(TargetID, QuantityValue, RecordTime) VALUES(@pTargetID, @pQuantityValue, @pRecordTime)";
            //cmd.CommandText = "INSERT INTO tblRecord(TargetID, QuantityValue) VALUES(@pTargetID, @pQuantityValue)";
            cmd.Connection = conn;

            cmd.Parameters.AddWithValue("pTargetID", targetId);
            cmd.Parameters.AddWithValue("pQuantityValue", quantityValue);
            cmd.Parameters.AddWithValue("pRecordTime", recordTime);

            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (OleDbException)
            {
            }
            finally
            {
                conn.Close();
            }
        }

        // get data to fill in record datatable
        public static DataTable getRecordList()
        {
            OleDbConnection conn = new OleDbConnection();
            conn.ConnectionString = csb.ConnectionString;

            OleDbCommand cmd = new OleDbCommand();
            cmd.CommandText = "SELECT * FROM tblRecord AS R INNER JOIN tblTarget AS T ON R.TargetID = T.TargetID";
            cmd.Connection = conn;

            OleDbDataAdapter adapter = new OleDbDataAdapter();
            adapter.SelectCommand = cmd;

            DataTable result = new DataTable();
            adapter.Fill(result);

            return result;
        }

        public static void ClearTable(string tableName)
        {
            OleDbConnection conn = new OleDbConnection();
            conn.ConnectionString = csb.ConnectionString;

            OleDbCommand cmd = new OleDbCommand();
            cmd.CommandText = "DELETE FROM " + tableName;
            cmd.Connection = conn;

            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (OleDbException)
            {
            }
            finally
            {
                conn.Close();
            }
        }

        // 0 for TargetID, 1 for ParentID
        public static int getTargetIDorParentIDByName(string name, int checkID)
        {
            int id = 0;
            string target = string.Empty;

            if (name.Equals(string.Empty))
            {
                return id;
            }

            if (checkID == 0)
            {
                target = "TargetID";
            }
            else
            {
                target = "ParentID";
            }

            OleDbConnection conn = new OleDbConnection();
            conn.ConnectionString = csb.ConnectionString;

            OleDbCommand cmd = new OleDbCommand();

            cmd.Connection = conn;
            cmd.CommandText = "SELECT " + target + " FROM tblTarget WHERE TargetName = @pName";
            cmd.Parameters.AddWithValue("pName", name);

            try
            {
                conn.Open();
                OleDbDataReader odr = cmd.ExecuteReader();
                if (odr.Read())
                {
                    id = Convert.ToInt32(odr[target]);
                }
            }
            catch (OleDbException)
            {
                id = 0;
            }
            finally
            {
                conn.Close();
            }
            return id;
        }

        public static int getClientIDByName(string name)
        {
            int id = 0;

            OleDbConnection conn = new OleDbConnection();
            conn.ConnectionString = csb.ConnectionString;

            OleDbCommand cmd = new OleDbCommand();

            cmd.Connection = conn;
            cmd.CommandText = "SELECT ClientID FROM tblClient WHERE ClientName = '" + name + "'";

            try
            {
                conn.Open();
                OleDbDataReader odr = cmd.ExecuteReader();
                if (odr.Read())
                {
                    id = Convert.ToInt32(odr["ClientID"]);
                }
            }
            catch (OleDbException)
            {
                id = 0;
            }
            finally
            {
                conn.Close();
            }
            return id;
        }

        public static string findParentName(string targetName)
        {
            string parentName = string.Empty;
            int targetID = 0;

            targetID = getTargetIDorParentIDByName(parentName, 1);

            OleDbConnection conn = new OleDbConnection();
            conn.ConnectionString = csb.ConnectionString;

            OleDbCommand cmd = new OleDbCommand();

            cmd.Connection = conn;
            cmd.CommandText = "SELECT TargetName FROM tblTarget WHERE TargetID = " + targetID;

            try
            {
                conn.Open();
                OleDbDataReader odr = cmd.ExecuteReader();
                if (odr.Read())
                {
                    parentName = odr["TargetName"].ToString();  
                }
            }
            catch (OleDbException)
            {
            }
            finally
            {
                conn.Close();
            }
            return parentName;
        }

        public static bool isExistedParentandChild(string targetName, string parentName)
        {
            bool isExisted = false;
            int targetID = 0;

            // check if parent does exist 
            if (parentName != "null" && getTargetIDorParentIDByName(parentName, 0) == 0) // does not exist
            {
                return true;
            }

            targetID = getTargetIDorParentIDByName(parentName, 0);

            OleDbConnection conn = new OleDbConnection();
            conn.ConnectionString = csb.ConnectionString;

            OleDbCommand cmd = new OleDbCommand();

            cmd.Connection = conn;
            cmd.CommandText = "SELECT TargetID FROM tblTarget WHERE TargetName = '" + targetName + "' AND ParentID = " + targetID;

            try
            {
                conn.Open();
                OleDbDataReader odr = cmd.ExecuteReader();
                if (odr.Read())
                {
                    // relation exists
                    isExisted = true;
                    //isExisted = isExistedParentandChild(parentName, findParentName(parentName));
                }
                else
                {
                    // no relation
                    isExisted = false;
                }
            }
            catch (OleDbException)
            {
                isExisted = false;
            }
            finally
            {
                conn.Close();
            }
            return isExisted;
        }

        public static string addNewTarget(string targetName, string parentName)
        {
            string info = "NotOK";
            OleDbConnection conn = new OleDbConnection();
            conn.ConnectionString = csb.ConnectionString;

            OleDbCommand cmd = new OleDbCommand();
            cmd.CommandText = "INSERT INTO tblTarget(ParentID, TargetName) VALUES(@pParentID, @pTargetName)";
            cmd.Connection = conn;

            cmd.Parameters.AddWithValue("pParentID", getTargetIDorParentIDByName(parentName, 0));
            cmd.Parameters.AddWithValue("pTargetName", targetName);

            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
                info = "OK";
            }
            catch (OleDbException)
            {
                info = "NotOK";
            }
            finally
            {
                conn.Close();
            }
            return info;
        }

        // parentID = 0 for Level 1 Targets, -1 for other child targets
        // pass in targetID to retrieve the child targets
        public static DataTable getTargetList(int parentID)
        {
            OleDbConnection conn = new OleDbConnection();
            conn.ConnectionString = csb.ConnectionString;

            OleDbCommand cmd = new OleDbCommand();
            if (parentID == -1)
            {
                cmd.CommandText = "SELECT * FROM tblTarget";
            }
            else 
            {
                cmd.CommandText = "SELECT * FROM tblTarget Where ParentID = " + parentID;
            }
            cmd.Connection = conn;

            OleDbDataAdapter adapter = new OleDbDataAdapter();
            adapter.SelectCommand = cmd;

            DataTable result = new DataTable();
            adapter.Fill(result);

            return result;
        }
    }
}
