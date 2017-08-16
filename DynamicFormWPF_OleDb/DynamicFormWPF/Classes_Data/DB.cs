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
                csb.ConnectionString = ConfigurationManager.ConnectionStrings["DynamicFormWPF.Properties.Settings.SEISConnectionString"].ConnectionString;
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

        public static bool isFromOriginalDatabase(DataTable dt)
        {
            bool isFrom = false;

            DataTable currentDT = dt;

            if (currentDT.Rows.Count > 0)
            {
                DataView dv = currentDT.DefaultView;
                dv.Sort = "TargetName ASC";
                currentDT = dv.ToTable();
            }

            DataTable currentTargetDT = getTargetList(-1);

            if (currentTargetDT.Rows.Count > 0)
            {
                DataView dv = currentTargetDT.DefaultView;
                dv.Sort = "TargetName ASC";
                currentTargetDT = dv.ToTable();
            }

            // compare new target list with current target list, if new TL contains all current TL then true
            for (int i = 0; i < currentTargetDT.Rows.Count; i++)
            {
                //if (currentTargetDT.Rows[i]["TargetID"].ToString() == dt.Rows[i]["TargetID"].ToString() &&
                //    currentTargetDT.Rows[i]["ParentID"].ToString() == dt.Rows[i]["ParentID"].ToString() &&
                //    currentTargetDT.Rows[i]["TargetName"].ToString() == dt.Rows[i]["TargetName"].ToString())
                if (currentTargetDT.Rows[i]["TargetName"].ToString() == currentDT.Rows[i]["TargetName"].ToString())
                {
                    isFrom = true;
                }
                else 
                {
                    return false;
                }
            }
            return isFrom;
        }

        public static void refreshCSBConnectionString()
        {
            csb.ConnectionString = ConfigurationManager.ConnectionStrings["DynamicFormWPF.Properties.Settings.SEISConnectionString"].ConnectionString;
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
                string connectionString = ConfigurationManager.ConnectionStrings["DynamicFormWPF.Properties.Settings.SEISConnectionString"].ConnectionString;
                // get the config file for this applications
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                // set the new values
                config.ConnectionStrings.ConnectionStrings["DynamicFormWPF.Properties.Settings.SEISConnectionString"].ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + databasePath;

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

            // tips
            // varchar(MAX) does not work
            // cannot create more than 1 table in 1 sql statement
            // cannot contain "," at the end of the statement

            //cmd2.CommandText = "CREATE TABLE tblTargetLV3(TargetLevel3_ID int Primary Key,TargetName varchar(50) Not Null,UnitCount varchar(10) Not Null,Quantity int,ClientID int,RecordDate datetime,Constraint FK01_Client Foreign Key (ClientID) References tblClient(ClientID))";
            //cmd3.CommandText = "CREATE TABLE tblTargetLV2(TargetLevel2_ID int Primary Key,TargetLevel3_ID int,TargetName varchar(50) Not Null,UnitCount varchar(10) Not Null,Quantity int,ClientID int,RecordDate datetime,Constraint FK01_Target Foreign Key (TargetLevel3_ID) References tblTargetLV3(TargetLevel3_ID),Constraint FK02_Client Foreign Key (ClientID) References tblClient(ClientID))";
            //cmd4.CommandText = "CREATE TABLE tblTargetLV1(TargetLevel1_ID int Primary Key,TargetLevel2_ID int,TargetName varchar(50) Not Null,Constraint FK02_Target Foreign Key (TargetLevel2_ID) References tblTargetLV2(TargetLevel2_ID))";
            
            cmd.CommandText = " CREATE TABLE tblClient    (ClientID AUTOINCREMENT, ClientName varchar(255), TelephoneNumber int NOT NULL, Primary Key(ClientID))";
            cmd2.CommandText = "CREATE TABLE tblTarget    (TargetID AUTOINCREMENT, ParentID int NOT NULL, TargetName varchar(255), Primary Key(TargetID))";
            cmd3.CommandText = "CREATE TABLE tblRecord    (RecordID AUTOINCREMENT, TargetID int, QuantityValue int, ClientID int, RecordTime datetime, Primary Key(RecordID), Constraint FK_Target Foreign Key (TargetID) References tblTarget(TargetID), Constraint FK_Client Foreign Key (ClientID) References tblClient(ClientID))";
            cmd4.CommandText = "CREATE TABLE tblFile      (FileID AUTOINCREMENT, ClientID int, FileName varchar(255), RecordTime datetime, Primary Key(FileID), Constraint FK_File Foreign Key (ClientID) References tblClient(ClientID))";

            cmd.Connection = conn;
            cmd2.Connection = conn;
            cmd3.Connection = conn;
            cmd4.Connection = conn;

            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
                cmd2.ExecuteNonQuery();
                cmd3.ExecuteNonQuery();
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

        public static string updateTargetFromXMLtoDB(int targetID, int parentID, string targetName, ref int ID)
        {
            if (isExistedTarget(parentID, targetName))
            {
                ID = 0;
                return "OK";
            }

            string info = string.Empty;

            OleDbConnection conn = new OleDbConnection();
            conn.ConnectionString = csb.ConnectionString;

            OleDbCommand cmd = new OleDbCommand();
            OleDbCommand cmd2 = new OleDbCommand();
            cmd.CommandText = "INSERT INTO tblTarget (TargetID, ParentID, TargetName) VALUES (@pTargetID, @pParentID, @pTargetName)";
            cmd2.CommandText = "SELECT @@Identity";
            cmd.Connection = conn;
            cmd2.Connection = conn;

            cmd.Parameters.AddWithValue("pTargetID", targetID);
            cmd.Parameters.AddWithValue("pParentID", parentID);
            cmd.Parameters.AddWithValue("pTargetName", targetName);

            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
                ID = Convert.ToInt32(cmd2.ExecuteScalar());
                info = "OK";
            }
            catch (OleDbException e)
            {
                info = e.Message;
                ID = 0;
            }
            finally
            {
                conn.Close();
            }
            return info;
        }

        public static string updateContent(string content, string controlName)
        {
            string info = string.Empty;
            int targetID = 0;
            int quantityValue = 0;
            DateTime recordTime = new DateTime();

            try
            {
                targetID = Convert.ToInt32(controlName.Substring(4));
                quantityValue = Convert.ToInt32(content);
                // Access does not accept DateTime.Now
                recordTime = Convert.ToDateTime(String.Format("{0:yyyy-MM-dd}", DateTime.Now));
            }
            catch (Exception) { }

            OleDbConnection conn = new OleDbConnection();
            conn.ConnectionString = csb.ConnectionString;

            OleDbCommand cmd = new OleDbCommand();
            //cmd.CommandText = "INSERT INTO tblRecord(TargetID, QuantityValue, RecordTime, ClientID) VALUES(@pTargetID, @pQuantityValue, @pRecordTime, @pClientID)";
            cmd.CommandText = "INSERT INTO tblRecord(TargetID, QuantityValue, RecordTime) VALUES(@pTargetID, @pQuantityValue, @pRecordTime)";
            cmd.Connection = conn;

            cmd.Parameters.AddWithValue("pTargetID", targetID);
            cmd.Parameters.AddWithValue("pQuantityValue", quantityValue);
            cmd.Parameters.AddWithValue("pRecordTime", recordTime);
            //cmd.Parameters.AddWithValue("pClientID", 1);

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

        public static string updateContentWithDate(int targetID, int quantityValue, DateTime date, ref int number)
        {
            DateTime recordTime = new DateTime();
            string info = string.Empty;
            try
            {
                // Access does not accept DateTime.Now
                if (date == Convert.ToDateTime("1/01/0001"))
                {
                    recordTime = Convert.ToDateTime(String.Format("{0:yyyy-MM-dd}", DateTime.Today));
                }
                else
                {
                    recordTime = Convert.ToDateTime(String.Format("{0:yyyy-MM-dd}", date));
                }
            }
            catch (Exception) { }

            OleDbConnection conn = new OleDbConnection();
            conn.ConnectionString = csb.ConnectionString;

            OleDbCommand cmd = new OleDbCommand();
            //cmd.CommandText = "INSERT INTO tblRecord(TargetID, QuantityValue, RecordTime, ClientID) VALUES(@pTargetID, @pQuantityValue, @pRecordTime, @pClientID)";
            cmd.CommandText = "INSERT INTO tblRecord(TargetID, QuantityValue, RecordTime) VALUES(@pTargetID, @pQuantityValue, @pRecordTime)";
            cmd.Connection = conn;

            cmd.Parameters.AddWithValue("pTargetID", targetID);
            cmd.Parameters.AddWithValue("pQuantityValue", quantityValue);
            cmd.Parameters.AddWithValue("pRecordTime", recordTime);
            //cmd.Parameters.AddWithValue("pClientID", 1);

            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
                info = "OK";
                number++;
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
            try
            {
                adapter.Fill(result);
            }
            catch (Exception)
            {
                result = null;
            }
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

        public static string getTargetNameByID(int targetID)
        {
            string targetName = string.Empty;

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
                    targetName = odr["TargetName"].ToString();
                }
            }
            catch (OleDbException e)
            {
                targetName = e.Message;
            }
            finally
            {
                conn.Close();
            }
            return targetName;
        }

        public static int getParentID(int targetID)
        {
            int parentID = 0;

            OleDbConnection conn = new OleDbConnection();
            conn.ConnectionString = csb.ConnectionString;

            OleDbCommand cmd = new OleDbCommand();

            cmd.Connection = conn;
            cmd.CommandText = "SELECT ParentID FROM tblTarget WHERE TargetID = " + targetID;

            try
            {
                conn.Open();
                OleDbDataReader odr = cmd.ExecuteReader();
                if (odr.Read())
                {
                    parentID = Convert.ToInt32(odr["ParentID"]);
                }
            }
            catch (OleDbException)
            {
                parentID = 0;
            }
            finally
            {
                conn.Close();
            }
            return parentID;
        }

        public static string editTargetName(int targetID, string newTargetName)
        {
            string info = "NotOK";
            OleDbConnection conn = new OleDbConnection();
            conn.ConnectionString = csb.ConnectionString;

            OleDbCommand cmd = new OleDbCommand();
            cmd.CommandText = "UPDATE tblTarget SET TargetName = '" + newTargetName + "' WHERE TargetID = " + targetID;
            cmd.Connection = conn;


            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
                info = "Đã thay đổi tên chỉ tiêu";
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
            targetID = getTargetIDorParentIDByName(parentName, 0);
            if (parentName != "null" && targetID == 0) // does not exist
            {
                return true;
            }

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

        public static bool isExistedTarget(int parentID, string targetName)
        {
            bool isExisted = false;

            OleDbConnection conn = new OleDbConnection();
            conn.ConnectionString = csb.ConnectionString;

            OleDbCommand cmd = new OleDbCommand();

            cmd.Connection = conn;
            cmd.CommandText = "SELECT * FROM tblTarget WHERE ParentID = " + parentID + " AND TargetName ='" + targetName + "'";

            try
            {
                conn.Open();
                OleDbDataReader odr = cmd.ExecuteReader();
                if (odr.Read())
                {
                    isExisted = true;
                }
                else
                {
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

        public static bool isExistedRecord(int targetID, DateTime recordTime)
        {
            bool isExisted = false;

            try
            {
                recordTime = Convert.ToDateTime(String.Format("{0:yyyy-MM-dd}", recordTime));
            }
            catch (Exception) { }

            OleDbConnection conn = new OleDbConnection();
            conn.ConnectionString = csb.ConnectionString;

            OleDbCommand cmd = new OleDbCommand();

            cmd.Connection = conn;
            cmd.CommandText = "SELECT * FROM tblRecord WHERE TargetID = " + targetID + " AND RecordTime = " + recordTime;

            try
            {
                conn.Open();
                OleDbDataReader odr = cmd.ExecuteReader();
                if (odr.Read())
                {
                    isExisted = true;
                }
                else
                {
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
        public static DataTable getTargetList(int ID)
        {
            OleDbConnection conn = new OleDbConnection();
            conn.ConnectionString = csb.ConnectionString;

            OleDbCommand cmd = new OleDbCommand();
            if (ID == -1)
            {
                cmd.CommandText = "SELECT * FROM tblTarget";
            }
            else
            {
                cmd.CommandText = "SELECT * FROM tblTarget Where ParentID = " + ID;
            }
            cmd.Connection = conn;

            OleDbDataAdapter adapter = new OleDbDataAdapter();
            adapter.SelectCommand = cmd;

            DataTable result = new DataTable();
            try
            {
                adapter.Fill(result);
            }
            catch (Exception)
            {
                result = null;
            }
            return result;
        }

        // pass in child's parentID and get parent's targetID
        public static void getIDofParent(int parentID, List<string> list)
        {
            OleDbConnection conn = new OleDbConnection();
            conn.ConnectionString = csb.ConnectionString;

            OleDbCommand cmd = new OleDbCommand();

            cmd.Connection = conn;
            cmd.CommandText = "SELECT TargetID FROM tblTarget WHERE TargetID = " + parentID;

            try
            {
                conn.Open();
                OleDbDataReader odr = cmd.ExecuteReader();
                if (odr.Read())
                {
                    list.Add(getTargetNameByID(Convert.ToInt32(odr["TargetID"])));
                    getIDofParent(getParentID(parentID), list); // loop backward to get all parents
                }
            }
            catch (OleDbException)
            {

            }
            finally
            {
                conn.Close();
            }
        }

        // get a list of selected parent's children
        public static List<int> getTargetIDList(int parentID)
        {
            List<int> targetIDs = new List<int>();

            targetIDs.Add(parentID); // get first parent

            getIDofChildren(parentID, targetIDs); // get children

            return targetIDs;
        }

        // pass in child's parentID and get parent's targetID
        public static void getIDofChildren(int targetID, List<int> list)
        {
            int childID = 0;
            OleDbConnection conn = new OleDbConnection();
            conn.ConnectionString = csb.ConnectionString;

            OleDbCommand cmd = new OleDbCommand();

            cmd.Connection = conn;
            cmd.CommandText = "SELECT TargetID FROM tblTarget WHERE ParentID = " + targetID;

            try
            {
                conn.Open();
                OleDbDataReader odr = cmd.ExecuteReader();
                if (odr.HasRows)
                {
                    while (odr.Read())
                    {
                        childID = Convert.ToInt32(odr["TargetID"]);
                        list.Add(childID);
                        getIDofChildren(childID, list); // loop forward to get all children
                    }
                }
            }
            catch (OleDbException)
            {

            }
            finally
            {
                conn.Close();
            }
        }

        // get a list of selected child's parents
        public static List<string> getTargetNameList(int childID)
        {
            List<string> targetNames = new List<string>();

            targetNames.Add(getTargetNameByID(childID)); // get first child

            getIDofParent(getParentID(childID), targetNames); // get parents

            targetNames.Reverse(); // LV1 -> LV2 -> LV3 and so on

            return targetNames;
        }

        public static bool isChildContained(int id) // check if current target contains children
        {
            bool isChildContained = false;
            OleDbConnection conn = new OleDbConnection();
            conn.ConnectionString = csb.ConnectionString;

            OleDbCommand cmd = new OleDbCommand();

            cmd.Connection = conn;
            cmd.CommandText = "SELECT * FROM tblTarget WHERE ParentID = " + id;

            try
            {
                conn.Open();
                OleDbDataReader odr = cmd.ExecuteReader();
                if (odr.Read())
                {
                    isChildContained = true;
                }
                else
                {
                    isChildContained = false;
                }
            }
            catch (OleDbException)
            {
                isChildContained = false;
            }
            finally
            {
                conn.Close();
            }
            return isChildContained;
        }

        public static bool isRecordContained(int id) // check if current target has any records
        {
            bool isRecordContained = false;
            OleDbConnection conn = new OleDbConnection();
            conn.ConnectionString = csb.ConnectionString;

            OleDbCommand cmd = new OleDbCommand();

            cmd.Connection = conn;
            cmd.CommandText = "SELECT * FROM tblRecord WHERE TargetID = " + id;

            try
            {
                conn.Open();
                OleDbDataReader odr = cmd.ExecuteReader();
                if (odr.Read())
                {
                    isRecordContained = true;
                }
                else
                {
                    isRecordContained = false;
                }
            }
            catch (OleDbException)
            {
                isRecordContained = false;
            }
            finally
            {
                conn.Close();
            }
            return isRecordContained;
        }

        public static string deleteTarget(int id)
        {
            string info = string.Empty;
            OleDbConnection conn = new OleDbConnection();
            conn.ConnectionString = csb.ConnectionString;

            OleDbCommand cmd = new OleDbCommand();
            cmd.CommandText = "DELETE FROM tblTarget WHERE TargetID = " + id;
            cmd.Connection = conn;

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

        public static DataTable getReportDataTable(DateTime fromDate, DateTime toDate)
        {
            string whereDateString = string.Empty; // WHERE RecordTime BETWEEN #" + fromDate + "# AND #" + toDate + "#
            string fromDateString = string.Empty;
            string toDateString = string.Empty;

            // input empty toDate
            if (toDate == Convert.ToDateTime("1/01/0001"))
            {
                fromDateString = String.Format("{0:yyyy-MM-dd}", fromDate);
                toDateString = String.Format("{0:yyyy-MM-dd}", DateTime.Today);
            }

            else
            {
                fromDateString = String.Format("{0:yyyy-MM-dd}", fromDate);
                toDateString = String.Format("{0:yyyy-MM-dd}", toDate);
            }

            whereDateString = " WHERE RecordTime BETWEEN #" + fromDateString + "# AND #" + toDateString + "#";

            OleDbConnection conn = new OleDbConnection();
            conn.ConnectionString = csb.ConnectionString;

            OleDbCommand cmd = new OleDbCommand();
            //cmd.CommandText = "SELECT T.TargetName, R.QuantityValue, R.RecordTime FROM tblRecord AS R INNER JOIN tblTarget AS T ON R.TargetID = T.TargetID"
            //    + " WHERE R.TargetID in ({0})";
            //cmd.CommandText = String.Format(cmd.CommandText, String.Join(",", IDs.ToArray()));
            cmd.CommandText = "SELECT T.TargetID, ParentID, TargetName AS [Tên chỉ tiêu], Quantity AS [Số lượng] FROM tblTarget AS T INNER JOIN"
                + " (SELECT T.TargetID, SUM(R.QuantityValue) AS Quantity"
                + " FROM tblTarget AS T LEFT OUTER JOIN"
                + " (SELECT * FROM tblRecord" + whereDateString + ") AS R ON T.TargetID = R.TargetID"
                + " GROUP BY T.TargetID) AS R ON T.TargetID = R.TargetID";

            cmd.Connection = conn;

            OleDbDataAdapter adapter = new OleDbDataAdapter();
            adapter.SelectCommand = cmd;

            DataTable result = new DataTable();
            try
            {
                adapter.Fill(result);
            }
            catch (Exception)
            {
                result = null;
            }
            return result;
        }

        public static DataTable getReportDataTable2(DateTime fromDate, DateTime toDate)
        {
            string whereDateString = string.Empty; // WHERE RecordTime BETWEEN #" + fromDate + "# AND #" + toDate + "#
            string fromDateString = string.Empty;
            string toDateString = string.Empty;

            // input empty toDate
            if (toDate == Convert.ToDateTime("1/01/0001"))
            {
                fromDateString = String.Format("{0:yyyy-MM-dd}", fromDate);
                toDateString = String.Format("{0:yyyy-MM-dd}", DateTime.Today); 
            }

            else
            {
                fromDateString = String.Format("{0:yyyy-MM-dd}", fromDate);
                toDateString = String.Format("{0:yyyy-MM-dd}", toDate);
            }

            whereDateString = " WHERE RecordTime BETWEEN #" + fromDateString + "# AND #" + toDateString + "#";

            OleDbConnection conn = new OleDbConnection();
            conn.ConnectionString = csb.ConnectionString;

            OleDbCommand cmd = new OleDbCommand();
            //cmd.CommandText = "SELECT T.TargetName, R.QuantityValue, R.RecordTime FROM tblRecord AS R INNER JOIN tblTarget AS T ON R.TargetID = T.TargetID"
            //    + " WHERE R.TargetID in ({0})";
            //cmd.CommandText = String.Format(cmd.CommandText, String.Join(",", IDs.ToArray()));
            cmd.CommandText = "SELECT T.TargetID, ParentID, TargetName, Quantity FROM tblTarget AS T INNER JOIN"
                + " (SELECT T.TargetID, SUM(R.QuantityValue) AS Quantity"
                + " FROM tblTarget AS T LEFT OUTER JOIN"
                + " (SELECT * FROM tblRecord" + whereDateString + ") AS R ON T.TargetID = R.TargetID"
                + " GROUP BY T.TargetID) AS R ON T.TargetID = R.TargetID";

            cmd.Connection = conn;

            OleDbDataAdapter adapter = new OleDbDataAdapter();
            adapter.SelectCommand = cmd;

            DataTable result = new DataTable();
            try
            {
                adapter.Fill(result);
            }
            catch (Exception)
            {
                result = null;
            }
            return result;
        }

        // get the minimum date that was recorded in tblRecord
        public static DateTime getMinDateTime() 
        {
            DateTime date = new DateTime();

            OleDbConnection conn = new OleDbConnection();
            conn.ConnectionString = csb.ConnectionString;

            OleDbCommand cmd = new OleDbCommand();

            cmd.Connection = conn;
            cmd.CommandText = "SELECT * FROM tblRecord WHERE RecordTime = (SELECT MIN(RecordTime) FROM tblRecord)";

            try
            {
                conn.Open();
                OleDbDataReader odr = cmd.ExecuteReader();
                if (odr.Read())
                {
                    date = Convert.ToDateTime(odr["RecordTime"]);
                }
            }
            catch (OleDbException)
            {
                date = Convert.ToDateTime("1/01/0001");
            }
            finally
            {
                conn.Close();
            }
            return date;
        }

        // get the maximum date that recorded in tblRecord
        public static DateTime getMaxDateTime()
        {
            DateTime date = new DateTime();

            OleDbConnection conn = new OleDbConnection();
            conn.ConnectionString = csb.ConnectionString;

            OleDbCommand cmd = new OleDbCommand();

            cmd.Connection = conn;
            cmd.CommandText = "SELECT * FROM tblRecord WHERE RecordTime = (SELECT MAX(RecordTime) FROM tblRecord)";

            try
            {
                conn.Open();
                OleDbDataReader odr = cmd.ExecuteReader();
                if (odr.Read())
                {
                    date = Convert.ToDateTime(odr["RecordTime"]);
                }
            }
            catch (OleDbException)
            {
                date = Convert.ToDateTime(DateTime.Today);
            }
            finally
            {
                conn.Close();
            }
            return date;
        }

        public static DataTable getDifferentRecords(DataTable FirstDataTable, DataTable SecondDataTable)
        {
            //Create Empty Table     
            DataTable ResultDataTable = new DataTable("ResultDataTable");

            //use a Dataset to make use of a DataRelation object     
            using (DataSet ds = new DataSet())
            {
                //Add tables     
                ds.Tables.AddRange(new DataTable[] { FirstDataTable.Copy(), SecondDataTable.Copy() });

                //Get Columns for DataRelation     
                DataColumn[] firstColumns = new DataColumn[ds.Tables[0].Columns.Count];
                for (int i = 0; i < firstColumns.Length; i++)
                {
                    firstColumns[i] = ds.Tables[0].Columns[i];
                }

                DataColumn[] secondColumns = new DataColumn[ds.Tables[1].Columns.Count];
                for (int i = 0; i < secondColumns.Length; i++)
                {
                    secondColumns[i] = ds.Tables[1].Columns[i];
                }

                //Create DataRelation     
                DataRelation r1 = new DataRelation(string.Empty, firstColumns, secondColumns, false);
                ds.Relations.Add(r1);

                DataRelation r2 = new DataRelation(string.Empty, secondColumns, firstColumns, false);
                ds.Relations.Add(r2);

                //Create columns for return table     
                for (int i = 0; i < FirstDataTable.Columns.Count; i++)
                {
                    ResultDataTable.Columns.Add(FirstDataTable.Columns[i].ColumnName, FirstDataTable.Columns[i].DataType);
                }

                //If FirstDataTable Row not in SecondDataTable, Add to ResultDataTable.     
                ResultDataTable.BeginLoadData();
                foreach (DataRow parentrow in ds.Tables[0].Rows)
                {
                    DataRow[] childrows = parentrow.GetChildRows(r1);
                    if (childrows == null || childrows.Length == 0)
                        ResultDataTable.LoadDataRow(parentrow.ItemArray, true);
                }

                //If SecondDataTable Row not in FirstDataTable, Add to ResultDataTable.     
                foreach (DataRow parentrow in ds.Tables[1].Rows)
                {
                    DataRow[] childrows = parentrow.GetChildRows(r2);
                    if (childrows == null || childrows.Length == 0)
                        ResultDataTable.LoadDataRow(parentrow.ItemArray, true);
                }
                ResultDataTable.EndLoadData();
            }

            return ResultDataTable;
        }

        public static DataTable getYearList() 
        {
            OleDbConnection conn = new OleDbConnection();
            conn.ConnectionString = csb.ConnectionString;

            OleDbCommand cmd = new OleDbCommand();
            cmd.CommandText = "SELECT * FROM (SELECT DISTINCT YEAR(RecordTime) AS YearList FROM tblRecord) ORDER BY YearList DESC";
            cmd.Connection = conn;

            OleDbDataAdapter adapter = new OleDbDataAdapter();
            adapter.SelectCommand = cmd;

            DataTable result = new DataTable();
            try
            {
                adapter.Fill(result);
            }
            catch (Exception)
            {
                result = null;
            }
            return result;
        }
    }
}
