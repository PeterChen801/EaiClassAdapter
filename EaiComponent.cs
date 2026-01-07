using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace EaiClassAdapter
{
    class EaiComponent
    {

        #region //根據傳入SQL語法撈取資料(欄位全撈,得到DataTable)
        public DataTable SelectDB(string sql_cmd, string ConnectionName)
        {
            string ConnectionString = this.GetValueFromConfig("Connection", ConnectionName);
            SqlConnection con = new SqlConnection(ConnectionString);
            SqlCommand comm = new SqlCommand();
            comm.Connection = con;
            try
            {
                DataTable dt = new DataTable();
                comm.CommandText = sql_cmd;
                SqlDataAdapter da = new SqlDataAdapter();
                da.SelectCommand = comm;
                da.Fill(dt);
                return dt;
            }
            catch (Exception exp)
            {
                if (con.State != ConnectionState.Closed)
                {
                    con.Close();
                }
                WriteEventLog("EaiComponent Error", "SelectDB Error.\n SQL Syntax:\n" + sql_cmd + ".\n" + exp.ToString());
                throw (exp);
            }
        }

        //public DataTable SelectDB(string sql_cmd, string ConnectionString)
        //{

        //    SqlConnection con = new SqlConnection(ConnectionString);
        //    SqlCommand comm = new SqlCommand();
        //    comm.Connection = con;
        //    try
        //    {
        //        DataTable dt = new DataTable();
        //        comm.CommandText = sql_cmd;
        //        SqlDataAdapter da = new SqlDataAdapter();
        //        da.SelectCommand = comm;
        //        da.Fill(dt);

        //        return dt;
        //    }
        //    catch (Exception exp)
        //    {
        //        if (con.State != ConnectionState.Closed)
        //            con.Close();

        //        this.WriteEventLog("EaiComponent Error", "SelectDB Error.\n SQL Syntax:\n" + sql_cmd + ".\n" + exp.ToString());
        //        //Message = "SelectDB Error. SQL Syntax:" + sql_cmd + "." + exp.Message;
        //        throw (exp);
        //    }
        //}
        #endregion

        #region //根據傳入SQL語法撈取資料(直接取第幾筆,第幾個欄位)
        public bool SelectDB(string sql_cmd, string ConnectionString, int index, string FieldName, out string returnValue)
        {
            returnValue = "";
            SqlConnection con = new SqlConnection(ConnectionString);
            SqlCommand comm = new SqlCommand();
            comm.Connection = con;
            try
            {
                DataTable dt = new DataTable();
                comm.CommandText = sql_cmd;
                SqlDataAdapter da = new SqlDataAdapter();
                da.SelectCommand = comm;
                da.Fill(dt);

                if (dt.Rows.Count - 1 < index)//無法回傳所需資料
                    return false;
                else if (dt.Rows[index].IsNull(FieldName) == true)
                {
                    returnValue = ""; //該欄位為null,回傳空白
                    return true;
                }
                else
                {
                    returnValue = dt.Rows[index][FieldName].ToString();
                    return true;
                }
            }
            catch (Exception exp)
            {
                if (con.State != ConnectionState.Closed)
                    con.Close();

                WriteEventLog("EaiComponent Error", "SelectDB Error.\n SQL Syntax:\n" + sql_cmd + ".\n" + exp.ToString());
                //Message = "SelectDB Error. SQL Syntax:" + sql_cmd + "." + exp.Message;
                throw (exp);
            }
        }
        #endregion


        public string GetValueFromConfig(string SectionName, string ElementName)
        {
            DataSet ds_config = new DataSet();

            try
            { 
                    ds_config.ReadXml(AppDomain.CurrentDomain.BaseDirectory + "\\EaiJobScheduleList.xml");

                return ds_config.Tables[SectionName].Rows[0][ElementName].ToString();
            }
            catch (Exception exp)
            {
                WriteEventLog("EaiComponent Error" + " Error", "GetValueFromConfig()\n ds_config Error:\nSectionName:[" + SectionName + "] ElementName:[" + ElementName + "]\n\nConfig Content:" + ds_config.GetXml() + "\n\nError message:" + exp.ToString());
                throw (exp);

            }

        }


        #region // 寫入EventLog
        /// <param name="source">來源</param>
        /// <param name="message">訊息</param>
        public static void WriteEventLog(string source, string message)
        {
            System.Diagnostics.EventLog.WriteEntry(source, message, System.Diagnostics.EventLogEntryType.Error);
        }
        public static void WriteEventLog_Inf(string source, string message)
        {
            System.Diagnostics.EventLog.WriteEntry(source, message, System.Diagnostics.EventLogEntryType.Information);
        }
        #endregion


        #region //取出指定DataTable的欄位內容

        public bool GetValueFromDataTable(DataTable dt, string FieldName, out string ReturnValue)
        {
            try
            {
                ReturnValue = "Null";
                StringBuilder sb = new StringBuilder();
                if (dt.Rows.Count == 0) //是否有資料
                {
                    ReturnValue = "";
                    return false;
                }

                if (dt.Rows[0].IsNull(FieldName) == true)//查無此欄位
                {
                    ReturnValue = "";
                    return false;
                }
                else
                {
                    int count = dt.Rows.Count;
                    for (int index = 0; index < count; index++)
                    {
                        sb.Append(dt.Rows[index][FieldName].ToString() + ",");
                    }

                    ReturnValue = sb.ToString().Substring(0, sb.ToString().Length - 1);
                    return true;
                }

            }
            catch (Exception ex)
            {
                throw (new Exception("GetValueFromDataTable() Error!\nError Message" + ex.ToString()));
            }
        }

        public bool GetValueFromDataTable(DataTable dt, string FieldName, int index, out string ReturnValue)
        {
            try
            {
                ReturnValue = "Null";
                if (dt.Rows.Count == 0)
                {
                    ReturnValue = "";
                    return false;
                }
                if (dt.Rows.Count < index)//指定筆數超過 
                    return false;
                if (dt.Rows[index].IsNull(FieldName) == true)
                {
                    ReturnValue = "";
                    return false;
                }
                else
                {
                    ReturnValue = dt.Rows[index][FieldName].ToString();
                    return true;
                }
            }
            catch (Exception ex)
            {
                throw (new Exception("GetValueFromDataTable() Error!\nError Message" + ex.ToString()));
            }
        }

        public bool GetValueFromDataTable(DataTable dt, string FieldName, string RowFilter, out string ReturnValue)
        {
            try
            {
                ReturnValue = "Null";
                StringBuilder sb = new StringBuilder();
                if (dt.Rows.Count == 0)
                {
                    ReturnValue = "";
                    return false;
                }


                DataView dv = dt.DefaultView;
                dv.RowFilter = RowFilter;

                if (dv.Count == 0)
                {
                    ReturnValue = "";
                    return false;
                }

                if (dv[0].Row.IsNull(FieldName) == true)
                {
                    ReturnValue = "";
                    return true;
                }
                else
                {
                    int count = dv.Count;
                    for (int index = 0; index < count; index++)
                    {
                        sb.Append(dv[index][FieldName].ToString() + ",");
                    }

                    ReturnValue = sb.ToString().Substring(0, sb.ToString().Length - 1);
                    return true;
                }
            }
            catch (Exception ex)
            {
                throw (new Exception("GetValueFromDataTable() Error!\nError Message" + ex.ToString()));
            }
        }

        public bool GetValueFromDataTable(DataTable dt, string FieldName, int index, string RowFilter, out string ReturnValue)
        {
            try
            {
                ReturnValue = "Null";
                if (dt.Rows.Count == 0)
                {
                    ReturnValue = "";
                    return false;
                }
                if (dt.Rows.Count < index)//指定筆數超過            
                    return false;

                DataView dv = dt.DefaultView;
                dv.RowFilter = RowFilter;

                if (dv.Count == 0)
                {
                    ReturnValue = "";
                    return false;
                }

                if (dv[index].Row.IsNull(FieldName) == true)
                {
                    ReturnValue = "";
                    return true;
                }
                else
                {
                    ReturnValue = dv[index][FieldName].ToString();
                    return true;
                }
            }
            catch (Exception ex)
            {
                throw (new Exception("GetValueFromDataTable() Error!\nError Message" + ex.ToString()));
            }
        }
        #endregion

        public static void WriteFileLog(string JobFileName ,  string MessageContent)
        {

            string FOLDER = AppDomain.CurrentDomain.BaseDirectory + @"\Log\" + DateTime.Now.ToString("yyyyMMdd");
            if (!Directory.Exists(FOLDER))
            {
                Directory.CreateDirectory(FOLDER);
            }

            string FILE_NAME =  FOLDER + @"\" + JobFileName +".txt";

            if (File.Exists(FILE_NAME))
            {
                using (StreamWriter sw = File.AppendText(FILE_NAME))
                {                    
                    sw.WriteLine("處理時間:" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:sss"));
                    sw.WriteLine(MessageContent);
                    sw.WriteLine("==========================================================================");
                }
            }
            else
            {
                using (StreamWriter sw = File.CreateText(FILE_NAME))
                {                 
                    sw.WriteLine("處理時間:" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:sss"));
                    sw.WriteLine(MessageContent);
                    sw.WriteLine("==========================================================================");
                }
            }


        }

        public static void DBLogInsert(string Session_ID, string JobName, string MessageContent, string Status, string connString)
        {
            SqlCommand sql = new SqlCommand();
            sql.Parameters.AddWithValue("@ID", Session_ID);
            sql.Parameters.AddWithValue("@JobName", JobName);
            sql.Parameters.AddWithValue("@Message_Data", MessageContent);
            sql.Parameters.AddWithValue("@Status", Status);
            sql.Parameters.AddWithValue("@Start_DateTime", DateTime.Now);
            sql.CommandText = "insert into sysJobLog (ID,JobName,Start_DateTime,Message_Data,Status)  values (@ID , @JobName ,@Start_DateTime,@Message_Data , @Status)";

            int rtn = UpdateDB(sql,connString);

        }

        public static void DBLogUpdate(string Session_ID, string JobName, string MessageContent, string Status, string connString)
        {
            SqlCommand sql = new SqlCommand();
            sql.Parameters.AddWithValue("@ID", Session_ID);
            sql.Parameters.AddWithValue("@JobName", JobName);
            sql.Parameters.AddWithValue("@Message_Data", MessageContent);
            sql.Parameters.AddWithValue("@Status", Status);
            sql.Parameters.AddWithValue("@Start_DateTime", DateTime.Now);
            sql.CommandText = "update sysJobLog set Status = @Status , End_DateTime = @Start_DateTime , Message_Data = @Message_Data where ID = @ID";

            int rtn = UpdateDB(sql, connString);

        }

        public static int UpdateDB(SqlCommand sql, string connString)
        {
            SqlConnection con = new SqlConnection(connString);
            try
            {
                con.Open();
                SqlCommand comm = sql ;
                comm.Connection = con;                
                int result = comm.ExecuteNonQuery();
                con.Close();

                return result;
            }
            catch (Exception exp)
            {
                if (con.State != ConnectionState.Closed)
                    con.Close();

                throw (new Exception("EaiJobSchedule Update DB Error" + exp.Message.Replace("'", "''")));
            }
        }

        public static int UpdateDB(string sql_cmd)
        {
            string ConnectionString = "";

            SqlConnection con = new SqlConnection(ConnectionString);

            try
            {
                con.Open();
                SqlCommand comm = new SqlCommand();
                comm.Connection = con;
                comm.CommandText = sql_cmd;
                int result = comm.ExecuteNonQuery();
                con.Close();

                return result;
            }
            catch (Exception exp)
            {
                if (con.State != ConnectionState.Closed)
                    con.Close();

                throw (new Exception("EaiJobSchedule Update DB Error" + exp.Message.Replace("'", "''")));

            }


        }
    
    }
}
