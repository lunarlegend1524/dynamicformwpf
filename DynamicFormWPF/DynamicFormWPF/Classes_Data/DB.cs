// -----------------------------------------------------------------------
// <copyright file="DB.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace DynamicFormWPF.Classes_Data
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data;
    using System.Data.SqlClient;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class DB
    {
        public static readonly SqlConnectionStringBuilder csb;

        static DB()
        {
            // register the DB file path into registry
            // string filePath
            try
            {
                csb = new SqlConnectionStringBuilder();

                // get connectionString in app.config
                // add System.configuration to references beforehand
                csb.ConnectionString = ConfigurationManager.ConnectionStrings["SEISConnectionString"].ConnectionString;
            }
            catch (SqlException)
            {
            }
        }

        public static bool isConnectedToDB()
        {
            bool isConnected = false;

            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            try
            {
                conn.Open();
                isConnected = true;
            }
            catch (SqlException)
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

            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();

            cmd.Connection = conn;
            cmd.CommandText = "SELECT * FROM tblTarget";

            try
            {
                conn.Open();
                SqlDataReader odr = cmd.ExecuteReader();
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
            catch (SqlException)
            {
                isEmpty = false;
            }
            finally
            {
                conn.Close();
            }
            return isEmpty;
        }

        public static void takeDBOffline(string DBName)
        {
            string info = string.Empty;

            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();

            cmd.CommandText = "ALTER DATABASE " + DBName + " SET OFFLINE WITH ROLLBACK IMMEDIATE";
            cmd.Connection = conn;

            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (SqlException) { }
            finally
            {
                conn.Close();
            }
        }

        public static void takeDBOnline(string DBName)
        {
            string info = string.Empty;

            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();

            cmd.CommandText = "ALTER DATABASE " + DBName + " SET ONLINE";
            cmd.Connection = conn;

            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (SqlException) { }
            finally
            {
                conn.Close();
            }
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
            csb.ConnectionString = ConfigurationManager.ConnectionStrings["SEISConnectionString"].ConnectionString;
        }

        public static string getDBNameFromConnectionString()
        {
            return csb.InitialCatalog;
        }

        // create new physically database
        //public static string createNewPhysicalDB(string filePath)
        //{
        //    string info = string.Empty;

        //    CatalogClass cat = new CatalogClass();
        //    string tmpStr;

        //    //tmpStr = "Provider=Microsoft.Jet.Sql.4.0;";
        //    //tmpStr += "Data Source=" + filePath + ";Jet Sql:Engine Type=5";
        //    tmpStr = "Provider=Microsoft.ACE.Sql.12.0;";
        //    tmpStr += "Data Source=" + filePath + ";Jet Sql:Engine Type=5";
        //    try
        //    {
        //        // check exist
        //        cat.Create(tmpStr);

        //        //Table nTable = new Table();
        //        //nTable.Name = "PersonData";
        //        //nTable.Columns.Append("LastName", DataTypeEnum.adVarWChar,);
        //        //nTable.Columns.Append("FirstName", DataTypeEnum.adVarWChar, 25);
        //        //nTable.Columns.Append("Address 1", DataTypeEnum.adVarWChar, 45);
        //        //nTable.Columns.Append("Address 2", DataTypeEnum.adVarWChar, 45);
        //        //nTable.Columns.Append("City", DataTypeEnum.adVarWChar, 25);
        //        //nTable.Columns.Append("State", DataTypeEnum.adVarWChar, 2);
        //        //nTable.Columns.Append("Zip", DataTypeEnum.adVarWChar, 9);
        //        //cat.Tables.Append(nTable);

        //        //System.Runtime.InteropServices.Marshal.FinalReleaseComObject(nTable);
        //        //System.Runtime.InteropServices.Marshal.FinalReleaseComObject(cat.Tables);
        //        //System.Runtime.InteropServices.Marshal.FinalReleaseComObject(cat.ActiveConnection);
        //        //System.Runtime.InteropServices.Marshal.FinalReleaseComObject(cat);

        //        // Add Constraints & Primary Keys
        //        // AddConstraintsToDB(filePath);

        //        //RegistryEditor.setConnectionStringToRegistry(filePath);
        //        //RegistryEditor.changeAppConfigSetting();
        //        refreshCSBConnectionString();

        //        // add tables to DB after getting new connectionstring
        //        info = addTablesToDB();
        //    }
        //    catch (Exception e)
        //    {
        //        info = e.Message;
        //    }
        //    return info;
        //}

        // create new SQL database
        public static string createNewDB(string fileName)
        {
            string info = string.Empty;

            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();

            cmd.CommandText = "CREATE DATABASE " + fileName;

            cmd.Connection = conn;

            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();

                // update connectionstring with the new selected DB and write to registry
                RegistryEditor.setNewConfigSettingWithDBName(fileName);

                info = addTablesToDB();
            }
            catch (SqlException)
            {
                info = "NotOK";
            }
            finally
            {
                conn.Close();
            }
            return info;
        }

        // add tables to newly created DB
        public static string addTablesToDB()
        {
            string info = string.Empty;

            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();
            SqlCommand cmd2 = new SqlCommand();
            SqlCommand cmd3 = new SqlCommand();
            SqlCommand cmd4 = new SqlCommand();

            // tips
            // nvarchar(MAX) does not work
            // cannot create more than 1 table in 1 sql statement
            // cannot contain "," at the end of the statement

            //cmd2.CommandText = "CREATE TABLE tblTargetLV3(TargetLevel3_ID int Primary Key,TargetName nvarchar(50) Not Null,UnitCount nvarchar(10) Not Null,Quantity int,ClientID int,RecordDate datetime,Constraint FK01_Client Foreign Key (ClientID) References tblClient(ClientID))";
            //cmd3.CommandText = "CREATE TABLE tblTargetLV2(TargetLevel2_ID int Primary Key,TargetLevel3_ID int,TargetName nvarchar(50) Not Null,UnitCount nvarchar(10) Not Null,Quantity int,ClientID int,RecordDate datetime,Constraint FK01_Target Foreign Key (TargetLevel3_ID) References tblTargetLV3(TargetLevel3_ID),Constraint FK02_Client Foreign Key (ClientID) References tblClient(ClientID))";
            //cmd4.CommandText = "CREATE TABLE tblTargetLV1(TargetLevel1_ID int Primary Key,TargetLevel2_ID int,TargetName nvarchar(50) Not Null,Constraint FK02_Target Foreign Key (TargetLevel2_ID) References tblTargetLV2(TargetLevel2_ID))";

            cmd.CommandText = " CREATE TABLE tblClient    (ClientID int PRIMARY KEY IDENTITY, ParentID int NOT NULL, ClientName nvarchar(255), TelephoneNumber nvarchar(10) NOT NULL)"; // TelephoneNumber must be string to stored '0'
            cmd2.CommandText = "CREATE TABLE tblTarget    (TargetID int PRIMARY KEY IDENTITY, ParentID int NOT NULL, TargetName nvarchar(255), ClientID int, Constraint FK_Client Foreign Key (ClientID) References tblClient(ClientID))";
            cmd3.CommandText = "CREATE TABLE tblRecord    (RecordID int PRIMARY KEY IDENTITY, TargetID int, QuantityValue int, ClientID int, RecordTime datetime, Constraint FK_Target Foreign Key (TargetID) References tblTarget(TargetID), Constraint FK_Client2 Foreign Key (ClientID) References tblClient(ClientID))";
            cmd4.CommandText = "CREATE TABLE tblFile      (FileID int PRIMARY KEY IDENTITY, ClientID int, FileName nvarchar(255), RecordTime datetime, IsSaved bit)";

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
            catch (SqlException e)
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

            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();
            SqlCommand cmd2 = new SqlCommand();

            // must SET IDENTITY_INSERT ON before insert an IDENTITY value into tbl such as Primary Key
            cmd.CommandText = "SET IDENTITY_INSERT tblTarget ON INSERT INTO tblTarget (TargetID, ParentID, TargetName) VALUES (@pTargetID, @pParentID, @pTargetName) SET IDENTITY_INSERT tblTarget OFF";
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
            catch (SqlException e)
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

            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();

            if (isExistedRecord(targetID, recordTime))
            {
                cmd.CommandText = "UPDATE tblRecord SET QuantityValue = @pQuantityValue WHERE TargetID = " + targetID + " AND RecordTime = @pRecordTime";
                cmd.Connection = conn;

                cmd.Parameters.AddWithValue("pQuantityValue", quantityValue);
                cmd.Parameters.AddWithValue("pRecordTime", recordTime);
            }

            else
            {
                //cmd.CommandText = "INSERT INTO tblRecord(TargetID, QuantityValue, RecordTime, ClientID) VALUES(@pTargetID, @pQuantityValue, @pRecordTime, @pClientID)";
                cmd.CommandText = "INSERT INTO tblRecord(TargetID, QuantityValue, RecordTime) VALUES(@pTargetID, @pQuantityValue, @pRecordTime)";

                cmd.Connection = conn;

                cmd.Parameters.AddWithValue("pTargetID", targetID);
                cmd.Parameters.AddWithValue("pQuantityValue", quantityValue);
                cmd.Parameters.AddWithValue("pRecordTime", recordTime);

                //cmd.Parameters.AddWithValue("pClientID", 1);
            }

            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
                info = "OK";
            }
            catch (SqlException)
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

            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();

            if (isExistedRecord(targetID, recordTime))
            {
                cmd.CommandText = "UPDATE tblRecord SET QuantityValue = @pQuantityValue WHERE TargetID = " + targetID + " AND RecordTime = @pRecordTime";
                cmd.Connection = conn;

                cmd.Parameters.AddWithValue("pQuantityValue", quantityValue);
                cmd.Parameters.AddWithValue("pRecordTime", recordTime);
            }

            else
            {
                //cmd.CommandText = "INSERT INTO tblRecord(TargetID, QuantityValue, RecordTime, ClientID) VALUES(@pTargetID, @pQuantityValue, @pRecordTime, @pClientID)";
                cmd.CommandText = "INSERT INTO tblRecord(TargetID, QuantityValue, RecordTime) VALUES(@pTargetID, @pQuantityValue, @pRecordTime)";
                cmd.Connection = conn;

                cmd.Parameters.AddWithValue("pTargetID", targetID);
                cmd.Parameters.AddWithValue("pQuantityValue", quantityValue);
                cmd.Parameters.AddWithValue("pRecordTime", recordTime);

                //cmd.Parameters.AddWithValue("pClientID", 1);
            }

            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
                info = "OK";
                number++;
            }
            catch (SqlException)
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
            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "SELECT * FROM tblRecord AS R INNER JOIN tblTarget AS T ON R.TargetID = T.TargetID";
            cmd.Connection = conn;

            SqlDataAdapter adapter = new SqlDataAdapter();
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
            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "DELETE FROM " + tableName;
            cmd.Connection = conn;

            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (SqlException)
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

            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();

            cmd.Connection = conn;
            cmd.CommandText = "SELECT " + target + " FROM tblTarget WHERE TargetName = @pName";
            cmd.Parameters.AddWithValue("pName", name);

            try
            {
                conn.Open();
                SqlDataReader odr = cmd.ExecuteReader();
                if (odr.Read())
                {
                    id = Convert.ToInt32(odr[target]);
                }
            }
            catch (SqlException)
            {
                id = 0;
            }
            finally
            {
                conn.Close();
            }
            return id;
        }

        public static int getTargetIDByTargetNameAndParentID(string targetName, int parentID)
        {
            int id = 0;

            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();

            cmd.Connection = conn;
            cmd.CommandText = "SELECT TargetID FROM tblTarget WHERE TargetName = @pName AND ParentID = @pParentID";
            cmd.Parameters.AddWithValue("pName", targetName);
            cmd.Parameters.AddWithValue("pParentID", parentID);

            try
            {
                conn.Open();
                SqlDataReader odr = cmd.ExecuteReader();
                if (odr.Read())
                {
                    id = Convert.ToInt32(odr["TargetID"]);
                }
            }
            catch (SqlException)
            {
                id = 0;
            }
            finally
            {
                conn.Close();
            }
            return id;
        }

        public static string getNameByID(int ID, string mode) // mode "Client" and "Target"
        {
            string name = string.Empty;

            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();

            cmd.Connection = conn;

            if (mode == "Target")
            {
                cmd.CommandText = "SELECT TargetName FROM tblTarget WHERE TargetID = " + ID;
            }
            else if (mode == "Client")
            {
                cmd.CommandText = "SELECT ClientName FROM tblClient WHERE ClientID = " + ID;
            }

            try
            {
                conn.Open();

                if (mode == "Target")
                {
                    SqlDataReader odr = cmd.ExecuteReader();
                    if (odr.Read())
                    {
                        name = odr["TargetName"].ToString();
                    }
                }
                else if (mode == "Client")
                {
                    SqlDataReader odr = cmd.ExecuteReader();
                    if (odr.Read())
                    {
                        name = odr["ClientName"].ToString();
                    }
                }
            }
            catch (SqlException e)
            {
                name = e.Message;
            }
            finally
            {
                conn.Close();
            }
            return name;
        }

        public static int getParentID(int ID, string mode) // mode "Client" or "Target"
        {
            int parentID = 0;

            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();

            cmd.Connection = conn;

            if (mode == "Client")
            {
                cmd.CommandText = "SELECT ParentID FROM tblClient WHERE ClientID = " + ID;
            }
            if (mode == "Target")
            {
                cmd.CommandText = "SELECT ParentID FROM tblTarget WHERE TargetID = " + ID;
            }

            try
            {
                conn.Open();
                SqlDataReader odr = cmd.ExecuteReader();
                if (odr.Read())
                {
                    parentID = Convert.ToInt32(odr["ParentID"]);
                }
            }
            catch (SqlException)
            {
                parentID = 0;
            }
            finally
            {
                conn.Close();
            }
            return parentID;
        }

        public static List<int> getParentIDsByName(string name, string mode) // mode "Client" or "Target"
        {
            List<int> parentIDs = new List<int>();

            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();

            cmd.Connection = conn;

            // must add N before 'stringvalue' to reconize unicode characters

            if (mode == "Client")
            {
                cmd.CommandText = "SELECT ParentID FROM tblClient WHERE ClientName = N'" + name + "'";
            }
            else if (mode == "Target")
            {
                cmd.CommandText = "SELECT ParentID FROM tblTarget WHERE TargetName = N'" + name + "'";
            }

            try
            {
                conn.Open();
                SqlDataReader odr = cmd.ExecuteReader();
                while (odr.Read())
                {
                    parentIDs.Add(Convert.ToInt32(odr["ParentID"]));
                }
            }
            catch (SqlException)
            {
                parentIDs = null;
            }
            finally
            {
                conn.Close();
            }
            return parentIDs;
        }

        public static string editTargetName(int targetID, string newTargetName)
        {
            string info = "NotOK";
            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();

            // must add N before 'stringvalue' to reconize unicode characters
            cmd.CommandText = "UPDATE tblTarget SET TargetName = N'" + newTargetName + "' WHERE TargetID = " + targetID;
            cmd.Connection = conn;

            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
                info = "Đã thay đổi tên chỉ tiêu";
            }
            catch (SqlException)
            {
                info = "NotOK";
            }
            finally
            {
                conn.Close();
            }
            return info;
        }

        public static string editClientInfo(int clientID, string newClientName, string newPhoneNumber)
        {
            string info = "NotOK";
            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();

            // must add N before 'stringvalue' to reconize unicode characters
            cmd.CommandText = "UPDATE tblClient SET ClientName = N'" + newClientName + "', TelephoneNumber = N'" + newPhoneNumber + "' WHERE ClientID = " + clientID;
            cmd.Connection = conn;

            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
                info = "Đã thay đổi thông tin đơn vị";
            }
            catch (SqlException)
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

            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();

            cmd.Connection = conn;

            // must add N before 'stringvalue' to reconize unicode characters
            cmd.CommandText = "SELECT ClientID FROM tblClient WHERE ClientName = N'" + name + "'";

            try
            {
                conn.Open();
                SqlDataReader odr = cmd.ExecuteReader();
                if (odr.Read())
                {
                    id = Convert.ToInt32(odr["ClientID"]);
                }
            }
            catch (SqlException)
            {
                id = 0;
            }
            finally
            {
                conn.Close();
            }
            return id;
        }

        public static string getClientNameByTargetID(int ID)
        {
            string name = string.Empty;

            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();

            cmd.Connection = conn;

            // must add N before 'stringvalue' to reconize unicode characters
            cmd.CommandText = "SELECT ClientName FROM tblClient AS C INNER JOIN tblTarget AS T ON C.ClientID = T.ClientID WHERE TargetID = " + ID;

            try
            {
                conn.Open();
                SqlDataReader odr = cmd.ExecuteReader();
                if (odr.Read())
                {
                    name = odr["ClientName"].ToString();
                }
            }
            catch (SqlException)
            {
                name = string.Empty;
            }
            finally
            {
                conn.Close();
            }
            return name;
        }

        public static int getClientIDByTargetID(int ID)
        {
            int id = 0;

            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();

            cmd.Connection = conn;
            cmd.CommandText = "SELECT ClientID FROM tblTarget WHERE TargetID = " + ID;

            try
            {
                conn.Open();
                SqlDataReader odr = cmd.ExecuteReader();
                if (odr.Read())
                {
                    id = Convert.ToInt32(odr["ClientID"]);
                }
            }
            catch (SqlException)
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

            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();

            cmd.Connection = conn;
            cmd.CommandText = "SELECT TargetName FROM tblTarget WHERE TargetID = " + targetID;

            try
            {
                conn.Open();
                SqlDataReader odr = cmd.ExecuteReader();
                if (odr.Read())
                {
                    parentName = odr["TargetName"].ToString();
                }
            }
            catch (SqlException)
            {
            }
            finally
            {
                conn.Close();
            }
            return parentName;
        }

        // get parent ID of newTargetName, then check if a targetID with the same value has existed
        public static bool isExistedParentandChild(int targetIDofParent, string newTargetName)
        {
            // for LV1 targets only
            if (targetIDofParent == 0)
            {
                if (isExistedTarget(0, newTargetName))
                {
                    return true;
                }

                else
                {
                    return false;
                }
            }

            else if (targetIDofParent != 0)
            {
                List<int> parentIDs = getParentIDsByName(newTargetName, "Target");

                foreach (int parentID in parentIDs)
                {
                    if (targetIDofParent == parentID) // relation exists
                    {
                        return true;
                    }
                }

                // if no parentID is found in list, then relation does not exists
                // can be wrong if add LV3 target while LV2 target textbox is empty, program will mistake for LV1 target
                return false;
            }

            else
            {
                return true;
            }
        }

        public static bool isExistedTarget(int parentID, string targetName)
        {
            bool isExisted = false;

            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();

            cmd.Connection = conn;

            // must add N before 'stringvalue' to reconize unicode characters
            cmd.CommandText = "SELECT * FROM tblTarget WHERE ParentID = " + parentID + " AND TargetName =N'" + targetName + "'";

            try
            {
                conn.Open();
                SqlDataReader odr = cmd.ExecuteReader();
                if (odr.Read())
                {
                    isExisted = true;
                }
                else
                {
                    isExisted = false;
                }
            }
            catch (SqlException)
            {
                isExisted = false;
            }
            finally
            {
                conn.Close();
            }
            return isExisted;
        }

        // get parent ID of newTargetName, then check if a targetID with the same value has existed
        public static bool isExistedParentandChildClientMode(int clientIDofParent, string newClientName)
        {
            // for LV1 targets only
            if (clientIDofParent == 0)
            {
                if (isExistedClient(0, newClientName))
                {
                    return true;
                }

                else
                {
                    return false;
                }
            }

            else if (clientIDofParent != 0)
            {
                List<int> parentIDs = getParentIDsByName(newClientName, "Client");

                foreach (int parentID in parentIDs)
                {
                    if (clientIDofParent == parentID) // relation exists
                    {
                        return true;
                    }
                }

                // if no parentID is found in list, then relation does not exists
                // can be wrong if add LV3 target while LV2 target textbox is empty, program will mistake for LV1 target
                return false;
            }

            else
            {
                return true;
            }
        }

        public static bool isExistedClient(int parentID, string clientName)
        {
            bool isExisted = false;

            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();

            cmd.Connection = conn;

            // must add N before 'stringvalue' to reconize unicode characters
            cmd.CommandText = "SELECT * FROM tblClient WHERE ParentID = " + parentID + " AND ClientName =N'" + clientName + "'";

            try
            {
                conn.Open();
                SqlDataReader odr = cmd.ExecuteReader();
                if (odr.Read())
                {
                    isExisted = true;
                }
                else
                {
                    isExisted = false;
                }
            }
            catch (SqlException)
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
            string recordTimeS = string.Empty;

            try
            {
                recordTimeS = String.Format("{0:yyyy-MM-dd}", recordTime);
            }
            catch (Exception) { }

            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();

            cmd.Connection = conn;
            cmd.CommandText = "SELECT * FROM tblRecord WHERE TargetID = " + targetID + " AND RecordTime = '" + recordTimeS + "'";

            try
            {
                conn.Open();
                SqlDataReader odr = cmd.ExecuteReader();
                if (odr.Read())
                {
                    isExisted = true;
                }
                else
                {
                    isExisted = false;
                }
            }
            catch (SqlException)
            {
                isExisted = false;
            }
            finally
            {
                conn.Close();
            }
            return isExisted;
        }

        public static bool isExistedFile(string fileName, DateTime creationTime)
        {
            bool isExisted = false;
            string recordTimeS = string.Empty;

            try
            {
                recordTimeS = String.Format("{0:yyyy-MM-dd}", creationTime);
            }

            catch (Exception) { }

            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();

            cmd.Connection = conn;
            cmd.CommandText = "SELECT * FROM tblFile WHERE FileName = N'" + fileName + "' AND RecordTime = '" + recordTimeS + "'";

            try
            {
                conn.Open();
                SqlDataReader odr = cmd.ExecuteReader();
                if (odr.Read())
                {
                    isExisted = true;
                }
                else
                {
                    isExisted = false;
                }
            }
            catch (SqlException)
            {
                isExisted = false;
            }
            finally
            {
                conn.Close();
            }
            return isExisted;
        }

        public static bool isFileSavedToDB(string fileName, DateTime creationTime)
        {
            bool isSaved = false;
            string creationTimeS = string.Empty;

            try
            {
                creationTimeS = String.Format("{0:yyyy-MM-dd}", creationTime);
            }
            catch (Exception) { }

            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();

            cmd.Connection = conn;
            cmd.CommandText = "SELECT IsSaved FROM tblFile WHERE FileName = N'" + fileName + "' AND RecordTime = '" + creationTimeS + "'";

            try
            {
                conn.Open();
                SqlDataReader odr = cmd.ExecuteReader();
                if (odr.Read())
                {
                    if (Convert.ToBoolean(odr["IsSaved"]) == true)
                    {
                        isSaved = true;
                    }
                    else
                    {
                        isSaved = false;
                    }
                }
                else
                {
                    isSaved = false;
                }
            }
            catch (SqlException)
            {
                isSaved = false;
            }
            finally
            {
                conn.Close();
            }
            return isSaved;
        }

        public static void setFileIsSaved(string fileName, DateTime creationTime)
        {
            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            string creationTimeS = string.Empty;

            try
            {
                creationTimeS = String.Format("{0:yyyy-MM-dd}", creationTime);
            }
            catch (Exception) { }

            SqlCommand cmd = new SqlCommand();

            // parameters won't work with update statement ??
            cmd.CommandText = "UPDATE tblFile SET IsSaved = 1 WHERE FileName = N'" + fileName + "' AND RecordTime = '" + creationTimeS + "'";
            cmd.Connection = conn;

            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (SqlException)
            {
            }
            finally
            {
                conn.Close();
            }
        }

        public static string addNewFile(string fileName, DateTime creationTime)
        {
            string info = "NotOK";
            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            creationTime = Convert.ToDateTime(String.Format("{0:yyyy-MM-dd}", creationTime));

            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "INSERT INTO tblFile(FileName, RecordTime, IsSaved) VALUES(@pFileName, @pRecordTime, @pIsSaved)";
            cmd.Connection = conn;

            cmd.Parameters.AddWithValue("pFileName", fileName);
            cmd.Parameters.AddWithValue("pRecordTime", creationTime);
            cmd.Parameters.AddWithValue("pIsSaved", false);

            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
                info = "OK";
            }
            catch (SqlException)
            {
                info = "NotOK";
            }
            finally
            {
                conn.Close();
            }
            return info;
        }

        public static string addNewTarget(string targetName, int targetIDofParent, int clientID)
        {
            string info = "NotOK";
            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "INSERT INTO tblTarget(ParentID, TargetName, ClientID) VALUES(@pParentID, @pTargetName, @pClientID)";
            cmd.Connection = conn;

            cmd.Parameters.AddWithValue("pParentID", targetIDofParent);
            cmd.Parameters.AddWithValue("pTargetName", targetName);
            cmd.Parameters.AddWithValue("pClientID", clientID);

            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
                info = "OK";
            }
            catch (SqlException)
            {
                info = "NotOK";
            }
            finally
            {
                conn.Close();
            }
            return info;
        }

        public static string addNewClient(string clientName, int clientIDofParent, string phoneNumber)
        {
            string info = "NotOK";
            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "INSERT INTO tblClient(ParentID, ClientName, TelephoneNumber) VALUES(@pParentID, @pClientName, @pTelephoneNumber)";
            cmd.Connection = conn;

            cmd.Parameters.AddWithValue("pParentID", clientIDofParent);
            cmd.Parameters.AddWithValue("pClientName", clientName);
            cmd.Parameters.AddWithValue("pTelephoneNumber", phoneNumber);

            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
                info = "OK";
            }
            catch (SqlException)
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
            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();

            if (ID == -1)
            {
                cmd.CommandText = "SELECT * FROM tblTarget";
            }
            else
            {
                cmd.CommandText = "SELECT * FROM tblTarget Where ParentID = " + ID;
            }
            cmd.Connection = conn;

            SqlDataAdapter adapter = new SqlDataAdapter();
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

        public static DataTable getAllChildrenOfTargetID(int ID)
        {
            DataTable dt = new DataTable("Children");
            dt.Columns.Add("TargetID", typeof(int));
            dt.Columns.Add("ParentID", typeof(int));
            dt.Columns.Add("TargetName", typeof(string));

            // get a list contains children's targetID
            List<int> targetIDs = new List<int>();
            targetIDs = getTargetIDList(ID);

            try
            {
                foreach (int id in targetIDs)
                {
                    // Rows.Add(row) does not work - the row is belong to another table
                    dt.ImportRow(getRowByID(id, "Target"));
                }
            }
            catch (Exception) { }

            return dt;
        }

        public static DataRow getRowByID(int ID, string mode)
        {
            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();

            if (mode == "Client")
            {
                cmd.CommandText = "SELECT ClientID, ParentID, ClientName FROM tblClient Where ClientID = " + ID;
            }
            else if (mode == "Target")
            {
                cmd.CommandText = "SELECT TargetID, ParentID, TargetName FROM tblTarget Where TargetID = " + ID;
            }

            cmd.Connection = conn;

            SqlDataAdapter adapter = new SqlDataAdapter();
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

            return result.Rows[0];
        }

        public static DataTable getTargetListWithClientName()
        {
            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "SELECT TargetID, T.ParentID, TargetName, ClientName FROM tblTarget AS T INNER JOIN tblClient AS C ON T.ClientID = C.ClientID";
            cmd.Connection = conn;

            SqlDataAdapter adapter = new SqlDataAdapter();
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
            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();

            cmd.Connection = conn;
            cmd.CommandText = "SELECT TargetID FROM tblTarget WHERE TargetID = " + parentID;

            try
            {
                conn.Open();
                SqlDataReader odr = cmd.ExecuteReader();
                if (odr.Read())
                {
                    list.Add(getNameByID(Convert.ToInt32(odr["TargetID"]), "Target"));
                    getIDofParent(getParentID(parentID, "Target"), list); // loop backward to get all parents
                }
            }
            catch (SqlException)
            {
            }
            finally
            {
                conn.Close();
            }
        }

        // get a list of selected targetName's children
        public static List<int> getTargetIDList(int parentID)
        {
            List<int> targetIDs = new List<int>();

            targetIDs.Add(parentID); // get first parent

            getIDofChildren(parentID, targetIDs, "Target"); // get children

            return targetIDs;
        }

        // pass in child's parentID and get parent's targetID
        public static void getIDofChildren(int ID, List<int> list, string mode)
        {
            int childID = 0;
            string modeID = string.Empty;
            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();

            cmd.Connection = conn;

            if (mode == "Client")
            {
                cmd.CommandText = "SELECT ClientID FROM tblClient WHERE ParentID = " + ID;
                modeID = "ClientID";
            }
            else if (mode == "Target")
            {
                cmd.CommandText = "SELECT TargetID FROM tblTarget WHERE ParentID = " + ID;
                modeID = "TargetID";
            }

            try
            {
                conn.Open();
                SqlDataReader odr = cmd.ExecuteReader();
                if (odr.HasRows)
                {
                    while (odr.Read())
                    {
                        childID = Convert.ToInt32(odr[modeID]);
                        list.Add(childID);
                        getIDofChildren(childID, list, mode); // loop forward to get all children
                    }
                }
            }
            catch (SqlException)
            {
            }
            finally
            {
                conn.Close();
            }
        }

        // get a list of selected parent's children
        public static List<int> getClientIDList(int parentID)
        {
            List<int> clientIDs = new List<int>();

            clientIDs.Add(parentID); // get first parent

            getIDofChildren(parentID, clientIDs, "Client"); // get children

            return clientIDs;
        }

        // get a list of selected child's parents
        public static List<string> getTargetNameList(int childID)
        {
            List<string> targetNames = new List<string>();

            targetNames.Add(getNameByID(childID, "Target")); // get first child

            getIDofParent(getParentID(childID, "Target"), targetNames); // get parents

            targetNames.Reverse(); // LV1 -> LV2 -> LV3 and so on

            return targetNames;
        }

        public static bool isChildContained(int id, string mode) // check if current target contains children
        {
            bool isChildContained = false;
            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();

            cmd.Connection = conn;

            // mode "Client" and "Target"
            if (mode == "Client")
            {
                cmd.CommandText = "SELECT * FROM tblClient WHERE ParentID = " + id;
            }
            else if (mode == "Target")
            {
                cmd.CommandText = "SELECT * FROM tblTarget WHERE ParentID = " + id;
            }

            try
            {
                conn.Open();
                SqlDataReader odr = cmd.ExecuteReader();
                if (odr.Read())
                {
                    isChildContained = true;
                }
                else
                {
                    isChildContained = false;
                }
            }
            catch (SqlException)
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
            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();

            cmd.Connection = conn;
            cmd.CommandText = "SELECT * FROM tblRecord WHERE TargetID = " + id;

            try
            {
                conn.Open();
                SqlDataReader odr = cmd.ExecuteReader();
                if (odr.Read())
                {
                    isRecordContained = true;
                }
                else
                {
                    isRecordContained = false;
                }
            }
            catch (SqlException)
            {
                isRecordContained = false;
            }
            finally
            {
                conn.Close();
            }
            return isRecordContained;
        }

        public static string deleteTargetOrClient(int id, string mode) // mode "Client" and "Target"
        {
            string info = string.Empty;
            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();

            if (mode == "Client")
            {
                cmd.CommandText = "DELETE FROM tblClient WHERE ClientID = " + id;
            }
            else if (mode == "Target")
            {
                cmd.CommandText = "DELETE FROM tblTarget WHERE TargetID = " + id;
            }
            cmd.Connection = conn;

            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
                info = "OK";
            }
            catch (SqlException)
            {
                info = "NotOK";
            }
            finally
            {
                conn.Close();
            }
            return info;
        }

        public static DataTable getReportDataTable(DateTime fromDate, DateTime toDate, List<int> IDs, bool hasHeader)
        {
            string whereDateString = string.Empty; // WHERE RecordTime BETWEEN #" + fromDate + "# AND #" + toDate + "#
            string fromDateString = string.Empty;
            string toDateString = string.Empty;

            string listIDString = string.Empty;

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

            // access - #datetime#
            // whereDateString = " WHERE RecordTime BETWEEN #" + fromDateString + "# AND #" + toDateString + "#";

            // mssql
            whereDateString = " WHERE RecordTime BETWEEN '" + fromDateString + "' AND '" + toDateString + "'";

            // get ClientName condition
            if (IDs.Count > 0)
            {
                listIDString = " WHERE ClientID = ";

                if (IDs.Count == 1)
                {
                    listIDString += IDs[0];
                }

                else
                {
                    listIDString += IDs[0];
                    foreach (int ID in IDs)
                    {
                        listIDString += " OR ClientID = " + ID;
                    }
                }
            }

            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();

            //cmd.CommandText = "SELECT T.TargetName, R.QuantityValue, R.RecordTime FROM tblRecord AS R INNER JOIN tblTarget AS T ON R.TargetID = T.TargetID"
            //    + " WHERE R.TargetID in ({0})";
            //cmd.CommandText = String.Format(cmd.CommandText, String.Join(",", IDs.ToArray()));

            if (hasHeader == true)
            {
                cmd.CommandText = "SELECT T.TargetID, ParentID, TargetName AS [Tên chỉ tiêu], Quantity AS [Số lượng], ClientName AS [Tên đơn vị], ClientID"
                    + " FROM (SELECT TargetID, A.ParentID, TargetName, ClientName, A.ClientID"
                    + " FROM tblTarget AS A INNER JOIN tblClient AS B ON A.ClientID = B.ClientID) AS T INNER JOIN"
                    + " (SELECT T.TargetID, SUM(R.QuantityValue) AS Quantity FROM tblTarget AS T LEFT OUTER JOIN"
                    + " (SELECT * FROM tblRecord" + whereDateString + ") AS R ON T.TargetID = R.TargetID"
                    + " GROUP BY T.TargetID) AS R ON T.TargetID = R.TargetID" + listIDString;
            }

            else if (hasHeader == false)
            {
                cmd.CommandText = "SELECT T.TargetID, ParentID, TargetName, Quantity, ClientName, ClientID"
                    + " FROM (SELECT TargetID, A.ParentID, TargetName, ClientName, A.ClientID"
                    + " FROM tblTarget AS A INNER JOIN tblClient AS B ON A.ClientID = B.ClientID) AS T INNER JOIN"
                    + " (SELECT T.TargetID, SUM(R.QuantityValue) AS Quantity FROM tblTarget AS T LEFT OUTER JOIN"
                    + " (SELECT * FROM tblRecord" + whereDateString + ") AS R ON T.TargetID = R.TargetID"
                    + " GROUP BY T.TargetID) AS R ON T.TargetID = R.TargetID" + listIDString;
            }
            cmd.Connection = conn;

            SqlDataAdapter adapter = new SqlDataAdapter();
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

            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();

            cmd.Connection = conn;

            // access
            //cmd.CommandText = "SELECT * FROM tblRecord WHERE RecordTime = (SELECT MIN(RecordTime) FROM tblRecord)";

            //mssql
            cmd.CommandText = "SELECT MIN(RecordTime) AS RecordTime FROM tblRecord";
            cmd.Connection = conn;

            try
            {
                conn.Open();
                SqlDataReader odr = cmd.ExecuteReader();
                if (odr.Read())
                {
                    date = Convert.ToDateTime(odr["RecordTime"]);
                }
            }
            catch (Exception)
            {
                date = Convert.ToDateTime(DateTime.Today);

                //date = Convert.ToDateTime("1/01/0001");
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

            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();

            // access
            //cmd.CommandText = "SELECT * FROM tblRecord WHERE RecordTime = (SELECT MAX(RecordTime) FROM tblRecord)";

            //mssql
            cmd.CommandText = "SELECT MAX(RecordTime) AS RecordTime FROM tblRecord";
            cmd.Connection = conn;

            try
            {
                conn.Open();
                SqlDataReader odr = cmd.ExecuteReader();
                if (odr.Read())
                {
                    date = Convert.ToDateTime(odr["RecordTime"]);
                }
            }
            catch (Exception)
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
            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();

            // access
            //cmd.CommandText = "SELECT * FROM (SELECT DISTINCT YEAR(RecordTime) AS YearList FROM tblRecord) ORDER BY YearList DESC";

            //mssql
            cmd.CommandText = "SELECT DISTINCT DATEPART(yyyy, RecordTime) AS YearList FROM tblRecord ORDER BY YearList";
            cmd.Connection = conn;

            SqlDataAdapter adapter = new SqlDataAdapter();
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

        public static ArrayList getDBNameList()
        {
            ArrayList al = new ArrayList();

            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            try
            {
                conn.Open();
                DataTable tblDatabases = conn.GetSchema("Databases");
                tblDatabases = tblDatabases.Select("dbid>4").CopyToDataTable();
                foreach (DataRow row in tblDatabases.Rows)
                {
                    al.Add(row["database_name"].ToString());
                }
            }
            catch (SqlException)
            {
                al = null;
            }
            finally
            {
                conn.Close();
            }
            return al;
        }

        public static string deleteDBFromServer(string name)
        {
            string info = string.Empty;

            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();

            cmd.CommandText = "DROP DATABASE " + name;

            cmd.Connection = conn;

            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
                info = "Đã xóa thành công CSDL '" + name + "'";
            }
            catch (SqlException)
            {
                info = "NotOK";
            }
            finally
            {
                conn.Close();
            }
            return info;
        }

        public static DataTable getClientList()
        {
            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "SELECT * FROM tblClient";
            cmd.Connection = conn;

            SqlDataAdapter adapter = new SqlDataAdapter();
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

        public static DataTable getClientListByParentID(int ID)
        {
            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "SELECT * FROM tblClient WHERE ParentID = " + ID;
            cmd.Connection = conn;

            SqlDataAdapter adapter = new SqlDataAdapter();
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

        public static string getTelephoneNumberByID(int ID)
        {
            string phoneNo = string.Empty;

            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = csb.ConnectionString;

            SqlCommand cmd = new SqlCommand();

            cmd.Connection = conn;
            cmd.CommandText = "SELECT TelephoneNumber FROM tblClient WHERE ClientID = " + ID;

            try
            {
                conn.Open();
                SqlDataReader odr = cmd.ExecuteReader();
                if (odr.Read())
                {
                    phoneNo = odr["TelephoneNumber"].ToString();
                }
            }
            catch (SqlException e)
            {
                phoneNo = e.Message;
            }
            finally
            {
                conn.Close();
            }
            return phoneNo;
        }
    }
}