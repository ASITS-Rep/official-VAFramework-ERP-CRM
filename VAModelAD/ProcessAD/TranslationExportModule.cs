﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VAdvantage.Model;
using VAdvantage.Utility;
using VAdvantage.ProcessEngine;
using VAdvantage.Logging;
using System.Data;
using VAdvantage.DataBase;

namespace VAdvantage.Process
{
    public class TranslationExportModule : ProcessEngine.SvrProcess
    {
        /** The Language				*/
        private String _VAF_Language = null;

        private static String Mode_Import = "I";
        private static String Mode_Export = "E";
        /**	Export or Import Mode		*/
        private String _ImportExport = Mode_Export;

        public static String ExportScope_System = "S";
        /** System Data viewed by Tenants	*/
        public static String ExportScope_SystemUser = "U";
        public static String ExportScope_Tenant = "T";
        /** Export Scope		*/
        private String _ExportScope = ExportScope_System;
        /** Optional Specific Table		*/
        private int _VAF_TableView_ID = 0;

        public static String TranslationLevel_All = "A";
        public static String TranslationLevel_LabelOnly = "L";
        public static String TranslationLevel_LabelDescriptionOnly = "D";
        /** Translation Level			*/
        private String _TranslationLevel = TranslationLevel_All;

        /** Optional Directory			*/
        private String _Directory = null;

        private VAdvantage.Utility.Ctx _ctx = null;

        private string _prefix = "";

        private int _record_ID = 0;


        /// <summary>
        /// Prepare - e.g., get Parameters.	 
        /// </summary>
        protected override void Prepare()
        {
            ProcessInfoParameter[] para = GetParameter();
            foreach (ProcessInfoParameter element in para)
            {
                String name = element.GetParameterName();
                if (element.GetParameter() == null)
                {
                    ;
                }
                else if (name.Equals("VAF_Language"))
                    _VAF_Language = (String)element.GetParameter();
                else if (name.Equals("VAF_TableView_ID"))
                    _VAF_TableView_ID = element.GetParameterAsInt();
                else
                    log.Log(Level.SEVERE, "Unknown Parameter: " + name);
            }

            _record_ID = GetRecord_ID();

            IDataReader dr = DB.ExecuteReader(@" SELECT VAF_ModuleInfo_ID,Name,Prefix,VersionId,versionNo FROM VAF_ModuleInfo 
                                                WHERE VAF_ModuleInfo_ID = 
                                                (SELECT VAF_ModuleInfo_ID FROM VAF_Module_DB_Schema 
                                                  WHERE VAF_Module_DB_Schema_ID =" + _record_ID + ")");

            string Module_Name = "", versionId, versionNo = "";
            try
            {
                if (dr.Read())
                {
                    //VAF_ModuleInfo_ID = Util.GetValueOfInt(dr[0]);
                    Module_Name = dr[1].ToString().Trim();// .Replace(' ', '_');
                    _prefix = dr[2].ToString();
                    versionId = dr[3].ToString();
                    if (string.IsNullOrEmpty(versionId))
                    {
                        versionId = "1000";
                    }
                    versionNo = dr[4].ToString();
                    if (string.IsNullOrEmpty(versionNo))
                    {
                        versionNo = "1.0.0.0";
                    }
                }
                dr.Close();
                
            }
            catch
            {
                if (dr != null)
                {
                    dr.Close();
                    dr = null;
                }
            }
            _Directory = System.IO.Path.Combine(System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath, _prefix, Module_Name + "_" + versionNo + " \\Translations", _VAF_Language);
            _ctx = GetCtx();
        }	//	prepare

        /// <summary>
        /// messages
        /// </summary>
        /// <returns>string</returns>
        protected override String DoIt()
        {
            if (string.IsNullOrEmpty(_prefix) || string.IsNullOrEmpty(_Directory))
            {
                throw new Exception("@Error@ - prefix or folder path not found" );
            }

            log.Info("VAF_Language=" + _VAF_Language
                + ",Mode=" + _ImportExport
                + ",Scope=" + _ExportScope
                + ",VAF_TableView_ID=" + _VAF_TableView_ID
                + ",Level=" + _TranslationLevel
                + ",Directory=" + _Directory);

            if (!System.IO.Directory.Exists(_Directory))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(_Directory);
                }
                catch (Exception e)
                {
                    throw e;
                }
            }

            TranslationMgr t = new TranslationMgr(_ctx);
            t.SetPrefix(_prefix);
            t.SetByExportID(true);
           
            
            String msg = t.ValidateLanguage(_VAF_Language);
            if (msg.Length > 0)
                throw new Exception("@LanguageSetupError@ - " + msg);

            //	Mode
            bool imp = Mode_Import.Equals(_ImportExport);
            //	Client
            int VAF_Client_ID = 0;
            if (ExportScope_Tenant.Equals(_ExportScope))
                VAF_Client_ID = _ctx.GetVAF_Client_ID();
            t.SetExportScope(_ExportScope, VAF_Client_ID);

            //	Directory
            if (Util.IsEmpty(_Directory))
                _Directory = VAdvantage.DataBase.Ini.GetFrameworkHome();//

            int noWords = 0;
            //	All Tables
            if (_VAF_TableView_ID == 0)
            {
                String sql = "SELECT * FROM VAF_TableView WHERE IsActive='Y' AND IsView='N'"
                    + " AND TableName LIKE '%_Trl' AND TableName<>'VAF_Column_TL'";
                if (ExportScope_Tenant.Equals(_ExportScope))
                    sql += " AND AccessLevel<>'4'";	//	System Only
                else
                    sql += " AND AccessLevel NOT IN ('1','2','3')";	// Org/Client/Both
                sql += " ORDER BY TableName";
                List<MTable> tables = MTable.GetTablesByQuery(_ctx, sql);
                foreach (MTable table in tables)
                {
                    String tableName = table.GetTableName();
                    msg = null;
                    msg = imp
                        ? t.ImportTrl(_Directory, tableName)
                        : t.ExportTrl(_Directory, tableName, _TranslationLevel);
                    AddLog(msg);
                }
                noWords = t.GetWordCount();
            }
            else	//	single table
            {
                MTable table = MTable.Get(_ctx, _VAF_TableView_ID);
                msg = null;
                msg = imp
                    ? t.ImportTrl(_Directory, table.GetTableName())
                    : t.ExportTrl(_Directory, table.GetTableName(), _TranslationLevel);
                AddLog(msg);
                noWords = t.GetWordCount();
            }
            //
            sb.Append("Word Count = " + noWords);
            return sb.ToString();
        }

        StringBuilder sb = new StringBuilder("");
        private new void AddLog(string msg)
        {
            sb.Append(msg).Append("\n");
            //throw new NotImplementedException();
        }
    }
}
