﻿/********************************************************
 * Module Name    :     Report
 * Purpose        :     Generate Reports
 * Author         :     Jagmohan Bhatt
 * Date           :     13-July-2009
  ******************************************************/
using System;
using System.Linq;
using System.Text;

using VAdvantage.Classes;
using System.Data;
using System.Data.SqlClient;
using VAdvantage.DataBase;
using VAdvantage.Utility;
using System.IO;
using VAdvantage.Login;
using VAdvantage.Logging;
using System.Drawing;

using System.Drawing.Printing;
using VAdvantage.ProcessEngine;
using VAdvantage.Model;

namespace VAdvantage.Print
{

    public class ReportEngine_N:IReportEngine,IReportView
    {
        public ReportEngine_N(Ctx ctx, MVAFPrintRptLayout pf, Query query, PrintInfo info)
        {
            string tableName = "";

            if (info != null)
            {
                try
                {
                    string sqlTable = "select tablename from vaf_tableview where vaf_tableview_id = " + info.GetVAF_TableView_ID();
                    tableName = Util.GetValueOfString(DB.ExecuteScalar(sqlTable, null, null));
                    if (tableName != null || tableName != "")
                    {
                        string orgSql = "select vaf_org_id from " + tableName + " where  " + tableName + "_ID = " + info.GetRecord_ID();
                        _vaf_org_id = Util.GetValueOfInt(DB.ExecuteScalar(orgSql, null, null));
                    }
                }
                catch
                {
                    _vaf_org_id = -1;
                }
            }


            if (pf == null)
                throw new ArgumentException("ReportEngine - no PrintFormat");
            log.Info(pf + " -- " + query);
            m_ctx = ctx;
            m_printerName = m_ctx.GetPrinterName();
            //
            m_printFormat = pf;
            m_info = info;



            SetQuery(query);		//	loads Data
        }	//	ReportEngine

        /**	Static Logger	*/
        private static VLogger log = VLogger.GetVLogger(typeof(ReportEngine_N).FullName);

        /**	Context					*/
        private Ctx m_ctx;

        /**	Print Format			*/
        private MVAFPrintRptLayout m_printFormat;
        /** Print Info				*/
        private PrintInfo m_info;
        /**	Query					*/
        private Query m_query;
        /**	Query Data				*/
        private PrintData m_printData;
        /** Layout					*/
        private LayoutEngine m_layout = null;
        /**	Printer					*/
        private String m_printerName = null;
        /**	View					*/
        private View m_view = null;

        //******************//
        private int _vaf_org_id = -1;

        public void SetPrintFormat(MVAFPrintRptLayout pf)
        {
            m_printFormat = pf;
            if (m_layout != null)
            {
                SetPrintData();
                m_layout.SetPrintFormat(pf, false);
                m_layout.SetPrintData(m_printData, m_query, true);	//	format changes data
            }
            if (m_view != null)
                m_view.Invalidate();
        }	//	setPrintFormat

        public void SetQuery(Query query)
        {
            m_query = query;
            if (query == null)
                return;
            //
            SetPrintData();
            if (m_layout != null)
                m_layout.SetPrintData(m_printData, m_query, true);
            if (m_view != null)
                m_view.Refresh();
        }	//	setQuery

        public Query GetQuery()
        {
            return m_query;
        }	//	getQuery

        private void SetPrintData()
        {
            if (m_query == null)
                return;
            DataEngine de = new DataEngine(m_printFormat.GetLanguage());
            de.SetPInfo(m_info);
            SetPrintData(de.GetPrintData(m_ctx, m_printFormat, m_query));
            //	m_printData.dump();
        }	//	setPrintData

        private void SetPrintData(Query query)
        {
            if (query == null)
                return;
            DataEngine de = new DataEngine(m_printFormat.GetLanguage());
            de.SetPInfo(m_info);
            SetPrintData(de.GetPrintData(m_ctx, m_printFormat, query));
            //	m_printData.dump();
        }	//	setPrintData

        public PrintData GetPrintData()
        {
            return m_printData;
        }	//	getPrintData

        public void SetPrintData(PrintData printData)
        {
            if (printData == null)
                return;
            m_printData = printData;
        }	//	setPrintData


        StringBuilder html = null;

        private void Layout()
        {
            if (m_printFormat == null)
                throw new Exception("No print format");
            if (m_printData == null)
                throw new Exception("No print data (Delete Print Format and restart)");

            //actaull calling for the reports happens here
           // m_layout = new LayoutEngine(m_printFormat, m_printData, m_query);
            m_layout = new LayoutEngine(m_printFormat, m_printData, m_query,_vaf_org_id);
            html = m_layout.GetRptHtml();
            //	Printer
            String printerName = m_printFormat.GetPrinterName();
            if (printerName == null && m_info != null)
                printerName = m_info.GetPrinterName();
            //setPrinterName(printerName);
        }	//	layout

        public StringBuilder GetRptHtml()
        {
            return html;
        }

        protected LayoutEngine GetLayout()
        {
            if (m_layout == null)
                Layout();
            return m_layout;
        }	//	getLayout

        public String GetName()
        {
            return m_printFormat.GetName();
        }	//	getName

        public MVAFPrintRptLayout GetPrintFormat()
        {
            return m_printFormat;
        }	//	getPrintFormat

        public PrintInfo GetPrintInfo()
        {
            return m_info;
        }	//	getPrintInfo

        public Ctx GetCtx()
        {
            return m_layout.GetCtx();
        }	//	getCtx

        public int GetRowCount()
        {
            return m_printData.GetRowCount();
        }	//	getRowCount

        public int GetColumnCount()
        {
            if (m_layout != null)
                return m_layout.GetColumnCount();
            return 0;
        }	//	getColumnCount

        public View GetView()
        {
            if (m_layout == null)
                Layout();
            if (m_view == null)
                m_view = new View(m_layout);
            return m_view;
        }	//	getView


        public void Print()
        {
            log.Info(m_info.ToString());
            if (m_layout == null)
                Layout();

            PrintDocument pd = new PrintDocument();
            pd.DefaultPageSettings.Landscape = m_layout.GetPaper().IsLandscape();

        }

        #region Archive

        public byte[] CreateCSV(Ctx ctx)
        {
            //added by jagmohan
            FILE_PATH = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath + "TempDownload";

            if (!Directory.Exists(FILE_PATH))
                Directory.CreateDirectory(FILE_PATH);

            string filePath = FILE_PATH + "\\temp_" + CommonFunctions.CurrentTimeMillis() + ".csv";
            System.IO.FileStream fs = new System.IO.FileStream(filePath, System.IO.FileMode.CreateNew, System.IO.FileAccess.Write);
            CreateCSV(fs, ',', Env.GetLanguage(ctx));

            var ms = new MemoryStream();
            var buff = new byte[64000];
            using (var sr = new FileStream(filePath, FileMode.Open))
            {
                for (; ; )
                {
                    int read = sr.Read(buff, 0, buff.Length);
                    ms.Write(buff, 0, read);
                    if (read == 0) break;
                }
            }

            return ms.ToArray();
        }



        public string GetCSVPath(Ctx ctx)
        {
            //added by jagmohan
            FILE_PATH = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath + "TempDownload";

            if (!Directory.Exists(FILE_PATH))
                Directory.CreateDirectory(FILE_PATH);

            string filePath = FILE_PATH + "\\temp_" + CommonFunctions.CurrentTimeMillis() + Guid.NewGuid().ToString("N").Substring(0, 5) + ".csv";
            System.IO.FileStream fs = new System.IO.FileStream(filePath, System.IO.FileMode.CreateNew, System.IO.FileAccess.Write);
            CreateCSV(fs, ',', Env.GetLanguage(ctx));

            return filePath.Substring(filePath.IndexOf("TempDownload"));
        }


        public string FILE_PATH = "D:\\TempDownload";

        public byte[] GetReportBytes()
        {
            return CreatePDF();
        }

        public string GetReportString()
        {
            return null;
        }

        public String GetReportFilePath(bool fetchBytes, out byte[] bArry)
        {
            bArry = null;
            return CreatePDF(fetchBytes, bArry);
        }

        public bool StartReport(Ctx ctx, ProcessInfo pi, Trx trx)
        {
            return true;
        }

        public byte[] CreatePDF()
        {
            //added by jagmohan
            FILE_PATH = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath + "TempDownload";

            if (!Directory.Exists(FILE_PATH))
                Directory.CreateDirectory(FILE_PATH);

            string filePath = FILE_PATH + "\\temp_" + CommonFunctions.CurrentTimeMillis() + ".pdf";

            var ms = new MemoryStream();
           
            try
            {
                //string fileName = "C:\\" + GetName().Replace(" ", "_") + "_" + VAdvantage.Classes.CommonFunctions.CurrentTimeMillis() + ".pdf";
                PdfSharp.Pdf.PdfDocument document = new PdfSharp.Pdf.PdfDocument();


                
                PdfSharp.Drawing.XGraphics xg;
                for (int page = 0; page < m_layout.GetPages().Count(); page++)
                {
                    Rectangle pageRectangle = GetRectangleOfPage(page + 1);
                    PdfSharp.Pdf.PdfPage pdfpage = document.AddPage();
                    pdfpage.Height = pageRectangle.Height;
                    pdfpage.Width = pageRectangle.Width;
                    xg = PdfSharp.Drawing.XGraphics.FromPdfPage(pdfpage);
                    Page p = (Page)m_layout.GetPages()[page];
                    p.PaintPdf(xg, pageRectangle, true, false);		//	sets context
                    m_layout.GetHeaderFooter().PaintPdf(xg, pageRectangle, true);
                }
                //xg.Dispose();


                document.Save(filePath);
                document.Close();

                // load file into stream

                var buff = new byte[64000];
                using (var sr = new FileStream(filePath, FileMode.Open))
                {
                    for (; ; )
                    {
                        int read = sr.Read(buff, 0, buff.Length);
                        ms.Write(buff, 0, read);
                        if (read == 0) break;
                    }
                }

                //bytes = StreamFile(Application.StartupPath + "\\temp.pdf");

                //File.Delete(filePath);
            }
            catch
            {
                return null;
            }

            return ms.ToArray();
        }

        public string CreatePDF(bool fetchBytes, byte[] bytes)
        {
            //added by jagmohan
            FILE_PATH = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath + "TempDownload";
            if (!Directory.Exists(FILE_PATH))
                Directory.CreateDirectory(FILE_PATH);

            string filePath = FILE_PATH + "\\temp_" + CommonFunctions.CurrentTimeMillis() + Guid.NewGuid().ToString("N").Substring(0, 5) + ".pdf";

            var ms = new MemoryStream();
            //byte[] bytes = null;
            try
            {
                //string fileName = "C:\\" + GetName().Replace(" ", "_") + "_" + VAdvantage.Classes.CommonFunctions.CurrentTimeMillis() + ".pdf";
                PdfSharp.Pdf.PdfDocument document = new PdfSharp.Pdf.PdfDocument();



                PdfSharp.Drawing.XGraphics xg;
                for (int page = 0; page < m_layout.GetPages().Count(); page++)
                {
                    Rectangle pageRectangle = GetRectangleOfPage(page + 1);
                    PdfSharp.Pdf.PdfPage pdfpage = document.AddPage();
                    pdfpage.Height = pageRectangle.Height;
                    pdfpage.Width = pageRectangle.Width;
                    xg = PdfSharp.Drawing.XGraphics.FromPdfPage(pdfpage);
                    Page p = (Page)m_layout.GetPages()[page];
                    p.PaintPdf(xg, pageRectangle, true, false);		//	sets context
                    m_layout.GetHeaderFooter().PaintPdf(xg, pageRectangle, true);
                }
                //xg.Dispose();


                document.Save(filePath);
                document.Close();

                // load file into stream
                if (fetchBytes)
                {
                    var buff = new byte[64000];
                    using (var sr = new FileStream(filePath, FileMode.Open))
                    {
                        for (; ; )
                        {
                            int read = sr.Read(buff, 0, buff.Length);
                            ms.Write(buff, 0, read);
                            if (read == 0) break;
                        }
                    }
                    bytes = ms.ToArray();
                }
            }
            catch(Exception e)
            {
                log.Severe("ReportEngine_N_CreatePDF_" + e.ToString());
                return null;
            }

            return filePath.Substring(filePath.IndexOf("TempDownload"));
        }

        private byte[] StreamFile(string filename)
        {
            FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read);

            // Create a byte array of file stream length
            byte[] ImageData = new byte[fs.Length];

            //Read block of bytes from stream into the byte array
            fs.Read(ImageData, 0, System.Convert.ToInt32(fs.Length));

            //Close the File Stream
            fs.Close();
            return ImageData; //return the byte data
        }
        #endregion

        public bool CreatePDF(string fileName, bool isPrint)
        {
            if (File.Exists(fileName))
                File.Delete(fileName);

            bool b = CreatePDF(fileName);
            if (isPrint && b)
            {
                try
                {
                    System.Diagnostics.Process myProcess = new System.Diagnostics.Process();
                    myProcess.StartInfo.FileName = fileName;
                    myProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    myProcess.StartInfo.CreateNoWindow = true;
                    myProcess.StartInfo.Verb = "print";
                    //myProcess.StartInfo.Arguments = cboTSPrinter.SelectedItem;
                    myProcess.StartInfo.UseShellExecute = true;

                    myProcess.Start();
                    myProcess.WaitForExit(2000);

                    myProcess.Close();
                }
                catch (Exception ex)
                {
                    log.Info(ex.Message);
                }
            }

            return true;
        }

        public bool CreatePDF(string fileName)
        {
            try
            {
                //First check for diretory if not exits then Craete
                if (!Directory.Exists(Path.Combine(System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath, "TempDownload")))
                {
                    Directory.CreateDirectory(Path.Combine(System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath, "TempDownload"));
                }


                //string fileName = "C:\\" + GetName().Replace(" ", "_") + "_" + VAdvantage.Classes.CommonFunctions.CurrentTimeMillis() + ".pdf";
                PdfSharp.Pdf.PdfDocument document = new PdfSharp.Pdf.PdfDocument();
                PdfSharp.Drawing.XGraphics xg;
                for (int page = 0; page < m_layout.GetPages().Count(); page++)
                {
                    Rectangle pageRectangle = GetRectangleOfPage(page + 1);
                    PdfSharp.Pdf.PdfPage pdfpage = document.AddPage();
                    pdfpage.Height = pageRectangle.Height;
                    pdfpage.Width = pageRectangle.Width;
                    xg = PdfSharp.Drawing.XGraphics.FromPdfPage(pdfpage);
                    Page p = (Page)m_layout.GetPages()[page];
                    p.PaintPdf(xg, pageRectangle, true, false);		//	sets context
                    m_layout.GetHeaderFooter().PaintPdf(xg, pageRectangle, true);
                }
                //FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                //StreamWriter sw = new StreamWriter(fs);

                //xg.Dispose();
                document.Save(fileName);
                document.Close();
            }
            catch
            {
                return false;
            }
            //System.Diagnostics.Process.Start(fileName);
            return true;
        }

        public byte[] CreatePDF(string table_ID, string Record_ID)
        {
            string path = table_ID + "_" + Record_ID;
            FILE_PATH = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath + "TempDownload";

            if (!Directory.Exists(FILE_PATH))
                Directory.CreateDirectory(FILE_PATH);

            string filePath = FILE_PATH + "\\Inv_" + path + ".pdf";

            var ms = new MemoryStream();
            
            try
            {
                //string fileName = "C:\\" + GetName().Replace(" ", "_") + "_" + VAdvantage.Classes.CommonFunctions.CurrentTimeMillis() + ".pdf";
                PdfSharp.Pdf.PdfDocument document = new PdfSharp.Pdf.PdfDocument();
                PdfSharp.Drawing.XGraphics xg;
                for (int page = 0; page < m_layout.GetPages().Count(); page++)
                {
                    Rectangle pageRectangle = GetRectangleOfPage(page + 1);
                    PdfSharp.Pdf.PdfPage pdfpage = document.AddPage();
                    pdfpage.Height = pageRectangle.Height;
                    pdfpage.Width = pageRectangle.Width;
                    xg = PdfSharp.Drawing.XGraphics.FromPdfPage(pdfpage);
                    Page p = (Page)m_layout.GetPages()[page];
                    p.PaintPdf(xg, pageRectangle, true, false);		//	sets context
                    m_layout.GetHeaderFooter().PaintPdf(xg, pageRectangle, true);
                }
                //xg.Dispose();


                document.Save(filePath);
                document.Close();

                // load file into stream

                var buff = new byte[64000];
                using (var sr = new FileStream(filePath, FileMode.Open))
                {
                    for (; ; )
                    {
                        int read = sr.Read(buff, 0, buff.Length);
                        ms.Write(buff, 0, read);
                        if (read == 0) break;
                    }
                }

                //bytes = StreamFile(Application.StartupPath + "\\temp.pdf");

                //File.Delete(filePath);
            }
            catch
            {
                return null;
            }

            return ms.ToArray();
        }

        /**	Margin around paper				*/
        public static int MARGIN = 5;

        public int GetPaperHeight()
        {
            //return 0;
            return (int)m_layout.GetPaper().GetHeight(true);
        }	//	getPaperHeight

        public int GetPaperWidth()
        {
            //return 0;
            return (int)m_layout.GetPaper().GetWidth(true);
        }	//	getPaperHeight

        public Rectangle GetRectangleOfPage(int pageNo)
        {
            int y = MARGIN + ((pageNo - 1) * (GetPaperHeight() + MARGIN));
            return new Rectangle(MARGIN, 5, GetPaperWidth(), GetPaperHeight());
        }

        /** Order = 0				*/
        public static int ORDER = 0;
        /** Shipment = 1				*/
        public static int SHIPMENT = 1;
        /** Invoice = 2				*/
        public static int INVOICE = 2;
        /** Project = 3				*/
        public static int PROJECT = 3;
        /** RfQ = 4					*/
        public static int RFQ = 4;
        /** Remittance = 5			*/
        public static int REMITTANCE = 5;
        /** Check = 6				*/
        public static int CHECK = 6;
        /** Dunning = 7				*/
        public static int DUNNING = 7;
        /** Movement = 8            */
        public static int MOVEMENT = 8;
        /** Inventory = 9            */
        public static int INVENTORY = 9;
        /******************Manufacturing**************/
        /** WorkOrder = 10            */
        public static int WORKORDER = 10;
        /** TaskList = 11            */
        public static int TASKLIST = 11;
        /** WorkOrderTxn = 12        */
        public static int WORKORDERTXN = 12;
        /** StandardOperation = 13   */
        public static int STANDARDOPERATION = 13;
        /** Routing = 14             */
        public static int ROUTING = 14;
        /******************Manufacturing**************/

        private static String[] DOC_TABLES = new String[] {
		"VAB_Order_Hdr_v", "VAM_Inv_InOut_Header_v", "VAB_Invoice_Target_v", "VAB_Project_Hdr_v",
        "VAB_RFQReply_v",
		"VAB_PaymentOptionCheck_vt", "VAB_PaymentOptionCheck_vt",  
		"VAB_DunningExeEntry_v", "VAM_InventoryTransfer", "VAM_Inventory" ,
        /******************Manufacturing**************/
        "VAM_WorkOrder_Header_v", "VAM_TaskList",
		"VAM_WorkOrderTxn_Header_V", "VAM_StandardOperation_Header_v", "VAM_Routing_Header_v"
        /******************Manufacturing**************/
        };
        private static String[] DOC_BASETABLES = new String[] {
		"VAB_Order", "VAM_Inv_InOut", "VAB_Invoice", "VAB_Project",
		"VAB_RFQReply",
		"VAB_PaymentOptionCheck", "VAB_PaymentOptionCheck", 
		"VAB_DunningExeEntry", "VAM_InventoryTransfer", "VAM_Inventory" ,
        /******************Manufacturing**************/
         "VAM_WorkOrder", "VAM_TaskList",
		"VAM_WorkOrderTransaction", "VAM_StandardOperation", "VAM_Routing"
        /******************Manufacturing**************/
        
        };
        private static String[] DOC_IDS = new String[] {
		"VAB_Order_ID", "VAM_Inv_InOut_ID", "VAB_Invoice_ID", "VAB_Project_ID",
		"VAB_RFQReply_ID",
		"VAB_PaymentOptionCheck_ID", "VAB_PaymentOptionCheck_ID", 
		"VAB_DunningExeEntry_ID", "VAM_InventoryTransfer_ID",  "VAM_Inventory_ID" ,
         /******************Manufacturing**************/
  //        "VAM_WorkOrder_ID", "VAM_TaskList_ID",
		//"VAM_WorkOrderTransaction_ID", "VAM_StandardOperation_ID", "VAM_Routing_ID"
          /******************Manufacturing**************/
        
        };
        private static int[] DOC_TABLE_ID = new int[] {
		X_VAB_Order.Table_ID, X_VAM_Inv_InOut.Table_ID, X_VAB_Invoice.Table_ID, X_VAB_Project.Table_ID,
		X_VAB_RFQReply.Table_ID,
		X_VAB_PaymentOptionCheck.Table_ID, X_VAB_PaymentOptionCheck.Table_ID, 
		X_VAB_DunningExeEntry.Table_ID, X_VAM_InventoryTransfer.Table_ID, X_VAM_Inventory.Table_ID ,
        /******************Manufacturing**************/
  //       X_VAM_WorkOrder.Table_ID, X_VAM_TaskList.Table_ID,
		//X_VAM_WorkOrderTransaction.Table_ID, X_VAM_StandardOperation.Table_ID, X_VAM_Routing.Table_ID
        /******************Manufacturing**************/
        
        };

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ctx">current context</param>
        /// <param name="pi">process info</param>
        /// <returns>same class object</returns>
        static public ReportEngine_N Get(Ctx ctx, ProcessInfo pi)
        {
            int VAF_Client_ID = (int)pi.GetVAF_Client_ID();
            //
            int VAF_TableView_ID = 0;
            int VAF_ReportView_ID = 0;
            String TableName = null;
            String whereClause = "";
            String orderbyClause = "";
            int VAF_Print_Rpt_Layout_ID = 0;
            bool IsForm = false;
            int Client_ID = -1;

            //	Get VAF_TableView_ID and TableName
            String sql = "SELECT rv.VAF_ReportView_ID,rv.WhereClause, "
                + " t.VAF_TableView_ID,t.TableName, pf.VAF_Print_Rpt_Layout_ID, pf.IsForm, pf.VAF_Client_ID, rv.OrderByClause "
                + "FROM VAF_JInstance pi"
                + " INNER JOIN VAF_Job p ON (pi.VAF_Job_ID=p.VAF_Job_ID)"
                + " INNER JOIN VAF_ReportView rv ON (p.VAF_ReportView_ID=rv.VAF_ReportView_ID)"
                + " INNER JOIN VAF_TableView t ON (rv.VAF_TableView_ID=t.VAF_TableView_ID)"
                + " LEFT OUTER JOIN VAF_Print_Rpt_Layout pf ON (p.VAF_ReportView_ID=pf.VAF_ReportView_ID AND pf.VAF_Client_ID IN (0,'" + VAF_Client_ID + "')) "
                + "WHERE pi.VAF_JInstance_ID='" + pi.GetVAF_JInstance_ID() + "' "		//	#2
                + "ORDER BY pf.VAF_Client_ID DESC, pf.IsDefault DESC";	//	own first
            IDataReader dr = null;
            try
            {
                dr = DataBase.DB.ExecuteReader(sql);
                //	Just get first 
                if (dr.Read())
                {
                    VAF_ReportView_ID = Utility.Util.GetValueOfInt(dr[0].ToString());		//	required
                    whereClause = dr[1].ToString();
                    orderbyClause = dr["OrderByClause"].ToString();
                    if (string.IsNullOrEmpty(whereClause))
                        whereClause = "";
                    //
                    VAF_TableView_ID = Utility.Util.GetValueOfInt(dr[2].ToString());
                    TableName = dr[3].ToString();			//	required for query
                    VAF_Print_Rpt_Layout_ID = Utility.Util.GetValueOfInt(dr[4].ToString());		//	required
                    IsForm = "Y".Equals(dr[5].ToString());	//	required
                    Client_ID = Utility.Util.GetValueOfInt(dr[6].ToString());
                }
                dr.Close();
            }
            catch (Exception e1)
            {
                if (dr != null)
                {
                    dr.Close();
                }
                log.Log(Level.SEVERE, "(1) - " + sql, e1);
            }
            //	Nothing found
            if (VAF_ReportView_ID == 0)
            {
                //	Check Print format in Report Directly
                sql = "SELECT t.VAF_TableView_ID,t.TableName, pf.VAF_Print_Rpt_Layout_ID, pf.IsForm "
                    + "FROM VAF_JInstance pi"
                    + " INNER JOIN VAF_Job p ON (pi.VAF_Job_ID=p.VAF_Job_ID)"
                    + " INNER JOIN VAF_Print_Rpt_Layout pf ON (p.VAF_Print_Rpt_Layout_ID=pf.VAF_Print_Rpt_Layout_ID)"
                    + " INNER JOIN VAF_TableView t ON (pf.VAF_TableView_ID=t.VAF_TableView_ID) "
                    + "WHERE pi.VAF_JInstance_ID='" + pi.GetVAF_JInstance_ID() + "'";
                IDataReader idr = null;
                try
                {
                    idr = DataBase.DB.ExecuteReader(sql);
                    while (idr.Read())
                    {
                        whereClause = "";
                        VAF_TableView_ID = Utility.Util.GetValueOfInt(idr[0].ToString());
                        TableName = idr[1].ToString();			//	required for query
                        VAF_Print_Rpt_Layout_ID = Utility.Util.GetValueOfInt(idr[2].ToString());		//	required
                        IsForm = "Y".Equals(idr[3].ToString());	//	required
                        Client_ID = VAF_Client_ID;
                    }
                    idr.Close();
                }
                catch (Exception e)
                {
                    if (idr != null)
                    {
                        idr.Close();
                    }
                    log.Severe(e.ToString());
                }
                if (VAF_Print_Rpt_Layout_ID == 0)
                {
                    return null;
                }
            }

            //  Create Query from Parameters
            Query query = null;
            if (IsForm && pi.GetRecord_ID() != 0)	//	Form = one record
                query = Query.GetEqualQuery(TableName + "_ID", pi.GetRecord_ID());
            else
                query = Query.Get(ctx, pi.GetVAF_JInstance_ID(), TableName);

            //  Add to static where clause from ReportView
            if (whereClause.Length != 0)
                query.AddRestriction(whereClause);

            //Added by Jagmohan Bhatt on 3-Feb-2010 for order by 
            //if (orderbyClause.Length != 0)
            //    query.AddRestriction(" Order By " + orderbyClause);


            //	Get Print Format
            MVAFPrintRptLayout format = null;
            //Object so = pi.getSerializableObject();
            //if (so instanceof MPrintFormat)
            //	format = (MPrintFormat)so;
            if (format == null && VAF_Print_Rpt_Layout_ID != 0)
            {
                //	We have a PrintFormat with the correct Client
                if (Client_ID == VAF_Client_ID)
                    format = MVAFPrintRptLayout.Get(ctx, VAF_Print_Rpt_Layout_ID, false);
                else
                    format = MVAFPrintRptLayout.CopyToClient(ctx, VAF_Print_Rpt_Layout_ID, VAF_Client_ID);
            }
            if (format != null)
            {
                format.SetName(Env.TrimModulePrefix(format.GetName()));
            }

            if (format != null && format.GetItemCount() == 0)
            {

                format.Delete(true);
                format = null;
            }
            //	Create Format
            if (format == null && VAF_ReportView_ID != 0)
                format = MVAFPrintRptLayout.CreateFromReportView(ctx, VAF_ReportView_ID, pi.GetTitle());
            if (format == null)
                return null;
            //
            PrintInfo info = new PrintInfo(pi);
            info.SetVAF_TableView_ID(VAF_TableView_ID);

            if (VAF_ReportView_ID > 0)
            {
                format.IsGridReport = true;
                format.PageNo = 1;
            }


            return new ReportEngine_N(ctx, format, query, info);
        }
        /// <summary>
        /// Gets the document according to order id
        /// </summary>
        /// <param name="VAB_Order_ID">order id</param>
        /// <returns>array of int</returns>
        private static int[] GetDocumentWhat(int VAB_Order_ID)
        {
            int[] what = new int[2];
            what[0] = ORDER;
            what[1] = VAB_Order_ID;
            //
            String sql = "SELECT dt.DocSubTypeSO "
                + "FROM VAB_DocTypes dt, VAB_Order o "
                + "WHERE o.VAB_DocTypes_ID=dt.VAB_DocTypes_ID"
                + " AND o.VAB_Order_ID='" + VAB_Order_ID + "'";
            String DocSubTypeSO = null;
            IDataReader dr = null;
            try
            {
                dr = DataBase.DB.ExecuteReader(sql);
                while (dr.Read())
                {
                    DocSubTypeSO = dr[0].ToString();
                }
                dr.Close();
            }
            catch (Exception e)
            {
                if (dr != null)
                {
                    dr.Close();
                }
                log.Severe(e.ToString());
                return null;		//	error
            }

            if (DocSubTypeSO == null)
                DocSubTypeSO = "";
            //	WalkIn Receipt, WalkIn Invoice,
            if (DocSubTypeSO.Equals("WR") || DocSubTypeSO.Equals("WI"))
                what[0] = INVOICE;
            //	WalkIn Pickup,
            else if (DocSubTypeSO.Equals("WP"))
                what[0] = SHIPMENT;
            //	Offer Binding, Offer Nonbinding, Standard Order
            else
                return what;

            //	Get Record_ID of Invoice/Receipt
            if (what[0] == INVOICE)
                sql = "SELECT VAB_Invoice_ID REC FROM VAB_Invoice WHERE VAB_Order_ID='" + VAB_Order_ID + "'"	//	1
                    + " ORDER BY VAB_Invoice_ID DESC";
            else
                sql = "SELECT VAM_Inv_InOut_ID REC FROM VAM_Inv_InOut WHERE VAB_Order_ID='" + VAB_Order_ID + "'" 	//	1
                    + " ORDER BY VAM_Inv_InOut_ID DESC";
            IDataReader idr = null;
            try
            {

                idr = DataBase.DB.ExecuteReader(sql);
               
                if (idr.Read())
                {
                    //bl = true;
                    //	if (i == 1 &&`1      ADialog.ask(0, null, what[0] == INVOICE ? "PrintOnlyRecentInvoice?" : "PrintOnlyRecentShipment?")) break;
                    what[1] = Utility.Util.GetValueOfInt(idr[0].ToString());
                }
                else
                {
                    //if (bl == true)//	No Document Found
                    what[0] = ORDER;
                }

                idr.Close();

            }
            catch (Exception e)
            {
                if (idr != null)
                {
                    idr.Close();
                }
                log.Severe(e.ToString());
                return null;
            }
            return what;
        }

        /// <summary>
        /// Get Document Print Engine for Document Type.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="type"></param>
        /// <param name="Record_ID"></param>
        /// <returns>Report Engine or null</returns>
        public static ReportEngine_N Get(Ctx ctx, int type, int Record_ID)
        {
            if (Record_ID < 1)
            {
                log.Log(Level.WARNING, "No PrintFormat for Record_ID=" + Record_ID
                        + ", Type=" + type);
                return null;
            }
            //	Order - Print Shipment or Invoice
            if (type == ORDER)
            {
                int[] what = GetDocumentWhat(Record_ID);
                if (what != null)
                {
                    type = what[0];
                    Record_ID = what[1];
                }
            }	//	Order
            //
            //	String JobName = DOC_BASETABLES[type] + "_Print";
            int VAF_Print_Rpt_Layout_ID = 0;
            int VAB_BusinessPartner_ID = 0;
            String DocumentNo = null;
            int copies = 1;

            //	Language
            VAModelAD.Model.MVAFClient client = VAModelAD.Model.MVAFClient.Get(ctx);
            Language language = client.GetLanguage();
            //	Get Document Info
            String sql = null;
            if (type == CHECK)
                sql = "SELECT bad.Check_PrintFormat_ID,"								//	1
                    + "	c.IsMultiLingualDocument,bp.VAF_Language,bp.VAB_BusinessPartner_ID,d.DocumentNo "		//	2..5
                    + "FROM VAB_PaymentOptionCheck d"
                    + " INNER JOIN VAB_PaymentOption ps ON (d.VAB_PaymentOption_ID=ps.VAB_PaymentOption_ID)"
                    + " INNER JOIN VAB_Bank_AcctDoc bad ON (ps.VAB_Bank_Acct_ID=bad.VAB_Bank_Acct_ID AND d.PaymentRule=bad.PaymentRule)"
                    + " INNER JOIN VAF_Client c ON (d.VAF_Client_ID=c.VAF_Client_ID)"
                    + " INNER JOIN VAB_BusinessPartner bp ON (d.VAB_BusinessPartner_ID=bp.VAB_BusinessPartner_ID) "
                    + "WHERE d.VAB_PaymentOptionCheck_ID=@recordid";		//	info from BankAccount
            else if (type == DUNNING)
                sql = "SELECT dl.Dunning_PrintFormat_ID,"
                    + " c.IsMultiLingualDocument,bp.VAF_Language,bp.VAB_BusinessPartner_ID,dr.DunningDate "
                    + "FROM VAB_DunningExeEntry d"
                    + " INNER JOIN VAF_Client c ON (d.VAF_Client_ID=c.VAF_Client_ID)"
                    + " INNER JOIN VAB_BusinessPartner bp ON (d.VAB_BusinessPartner_ID=bp.VAB_BusinessPartner_ID)"
                    + " INNER JOIN VAB_DunningExe dr ON (d.VAB_DunningExe_ID=dr.VAB_DunningExe_ID)"
                    + " INNER JOIN VAB_DunningStep dl ON (dl.VAB_DunningStep_ID=dr.VAB_DunningStep_ID) "
                    + "WHERE d.VAB_DunningExeEntry_ID=@recordid";			//	info from Dunning
            else if (type == REMITTANCE)
                sql = "SELECT pf.Remittance_PrintFormat_ID,"
                    + " c.IsMultiLingualDocument,bp.VAF_Language,bp.VAB_BusinessPartner_ID,d.DocumentNo "
                    + "FROM VAB_PaymentOptionCheck d"
                    + " INNER JOIN VAF_Client c ON (d.VAF_Client_ID=c.VAF_Client_ID)"
                    + " INNER JOIN VAF_Print_Rpt_Page pf ON (c.VAF_Client_ID=pf.VAF_Client_ID)"
                    + " INNER JOIN VAB_BusinessPartner bp ON (d.VAB_BusinessPartner_ID=bp.VAB_BusinessPartner_ID) "
                    + "WHERE d.VAB_PaymentOptionCheck_ID=@recordid"		//	info from PrintForm
                    + " AND pf.VAF_Org_ID IN (0,d.VAF_Org_ID) ORDER BY pf.VAF_Org_ID DESC";
            else if (type == PROJECT)
                sql = "SELECT pf.Project_PrintFormat_ID,"
                    + " c.IsMultiLingualDocument,bp.VAF_Language,bp.VAB_BusinessPartner_ID,d.Value "
                    + "FROM VAB_Project d"
                    + " INNER JOIN VAF_Client c ON (d.VAF_Client_ID=c.VAF_Client_ID)"
                    + " INNER JOIN VAF_Print_Rpt_Page pf ON (c.VAF_Client_ID=pf.VAF_Client_ID)"
                    + " LEFT OUTER JOIN VAB_BusinessPartner bp ON (d.VAB_BusinessPartner_ID=bp.VAB_BusinessPartner_ID) "
                    + "WHERE d.VAB_Project_ID=@recordid"					//	info from PrintForm
                    + " AND pf.VAF_Org_ID IN (0,d.VAF_Org_ID) ORDER BY pf.VAF_Org_ID DESC";
            else if (type == RFQ)
                sql = "SELECT COALESCE(t.VAF_Print_Rpt_Layout_ID, pf.VAF_Print_Rpt_Layout_ID),"
                    + " c.IsMultiLingualDocument,bp.VAF_Language,bp.VAB_BusinessPartner_ID,rr.Name "
                    + "FROM VAB_RFQReply rr"
                    + " INNER JOIN VAB_RFQ r ON (rr.VAB_RFQ_ID=r.VAB_RFQ_ID)"
                    + " INNER JOIN VAB_RFQ_Subject t ON (r.VAB_RFQ_Subject_ID=t.VAB_RFQ_Subject_ID)"
                    + " INNER JOIN VAF_Client c ON (rr.VAF_Client_ID=c.VAF_Client_ID)"
                    + " INNER JOIN VAB_BusinessPartner bp ON (rr.VAB_BusinessPartner_ID=bp.VAB_BusinessPartner_ID),"
                    + " VAF_Print_Rpt_Layout pf "
                    + "WHERE pf.VAF_Client_ID IN (0,rr.VAF_Client_ID)"
                    + " AND pf.VAF_TableView_ID=725 AND pf.IsTableBased='N'"	//	from RfQ PrintFormat
                    + " AND rr.VAB_RFQReply_ID=@recordid "				//	Info from RfQTopic
                    + "ORDER BY t.VAF_Print_Rpt_Layout_ID, pf.VAF_Client_ID DESC, pf.VAF_Org_ID DESC";
            else if (type == MOVEMENT)
                sql = "SELECT pf.Movement_PrintFormat_ID,"
                    + " c.IsMultiLingualDocument, COALESCE(dt.DocumentCopies,0) "
                    + "FROM VAM_InventoryTransfer d"
                    + " INNER JOIN VAF_Client c ON (d.VAF_Client_ID=c.VAF_Client_ID)"
                    + " INNER JOIN VAF_Print_Rpt_Page pf ON (d.VAF_Client_ID=pf.VAF_Client_ID OR pf.VAF_Client_ID=0)"
                    + " LEFT OUTER JOIN VAB_DocTypes dt ON (d.VAB_DocTypes_ID=dt.VAB_DocTypes_ID) "
                    + "WHERE d.VAM_InventoryTransfer_ID=@recordid"                 //  info from PrintForm
                    + " AND pf.VAF_Org_ID IN (0,d.VAF_Org_ID) AND pf.Movement_PrintFormat_ID IS NOT NULL "
                    + "ORDER BY pf.VAF_Client_ID DESC, pf.VAF_Org_ID DESC";
            else if (type == INVENTORY)
                sql = "SELECT pf.Inventory_PrintFormat_ID,"
                    + " c.IsMultiLingualDocument, COALESCE(dt.DocumentCopies,0) "
                    + "FROM VAM_Inventory d"
                    + " INNER JOIN VAF_Client c ON (d.VAF_Client_ID=c.VAF_Client_ID)"
                    + " INNER JOIN VAF_Print_Rpt_Page pf ON (d.VAF_Client_ID=pf.VAF_Client_ID OR pf.VAF_Client_ID=0)"
                    + " LEFT OUTER JOIN VAB_DocTypes dt ON (d.VAB_DocTypes_ID=dt.VAB_DocTypes_ID) "
                    + "WHERE d.VAM_Inventory_ID=@recordid"                 //  info from PrintForm
                    + " AND pf.VAF_Org_ID IN (0,d.VAF_Org_ID) AND pf.Inventory_PrintFormat_ID IS NOT NULL "
                    + "ORDER BY pf.VAF_Client_ID DESC,  pf.VAF_Org_ID DESC";
            /****************Manfacturing***********************/
            else if (type == WORKORDER)
                sql = "SELECT COALESCE(dt.VAF_Print_Rpt_Layout_ID,pf.WorkOrder_PrintFormat_ID), "
                    + " c.IsMultiLingualDocument, COALESCE(dt.DocumentCopies,0), "
                    + " dt.VAF_Print_Rpt_Layout_ID "
                    + "FROM VAM_WorkOrder d"
                    + " INNER JOIN VAF_Client c ON (d.VAF_Client_ID=c.VAF_Client_ID)"
                    + " INNER JOIN VAF_Print_Rpt_Page pf ON (d.VAF_Client_ID=pf.VAF_Client_ID OR pf.VAF_Client_ID=0)"
                    + " LEFT OUTER JOIN VAB_DocTypes dt ON (d.VAB_DocTypes_ID=dt.VAB_DocTypes_ID) "
                    + "WHERE d.VAM_WorkOrder_ID=@recordid"                 //  info from PrintForm
                    + " AND pf.VAF_Org_ID IN (0,d.VAF_Org_ID) "
                    + "ORDER BY pf.VAF_Client_ID DESC,  pf.VAF_Org_ID DESC";
            else if (type == WORKORDERTXN)
                sql = "SELECT COALESCE(dt.VAF_Print_Rpt_Layout_ID,pf.WorkOrderTxn_PrintFormat_ID), "
                    + " c.IsMultiLingualDocument, COALESCE(dt.DocumentCopies,0), "
                    + " dt.VAF_Print_Rpt_Layout_ID "
                    + "FROM VAM_WorkOrderTransaction d"
                    + " INNER JOIN VAF_Client c ON (d.VAF_Client_ID=c.VAF_Client_ID)"
                    + " INNER JOIN VAF_Print_Rpt_Page pf ON (d.VAF_Client_ID=pf.VAF_Client_ID OR pf.VAF_Client_ID=0)"
                    + " LEFT OUTER JOIN VAB_DocTypes dt ON (d.VAB_DocTypes_ID=dt.VAB_DocTypes_ID) "
                    + "WHERE d.VAM_WorkOrderTransaction_ID=@recordid"                 //  info from PrintForm
                    + " AND pf.VAF_Org_ID IN (0,d.VAF_Org_ID) "
                    + "ORDER BY pf.VAF_Client_ID DESC,  pf.VAF_Org_ID DESC";
            else if (type == STANDARDOPERATION)
                sql = "SELECT pf.StdOperation_PrintFormat_ID, "
                    + " c.IsMultiLingualDocument"
                    + " FROM VAM_StandardOperation d"
                    + " INNER JOIN VAF_Client c ON (d.VAF_Client_ID=c.VAF_Client_ID)"
                    + " INNER JOIN VAF_Print_Rpt_Page pf ON (d.VAF_Client_ID=pf.VAF_Client_ID OR pf.VAF_Client_ID=0)"
                    + " INNER JOIN M_Operation op ON (d.M_Operation_ID=op.M_Operation_ID) "
                    + " WHERE d.VAM_StandardOperation_ID=@recordid" // info from PrintForm
                    + " AND pf.VAF_Org_ID IN (0,d.VAF_Org_ID) "
                    + "ORDER BY pf.VAF_Client_ID DESC, pf.VAF_Org_ID DESC";
            else if (type == ROUTING)
                sql = "SELECT pf.Routing_PrintFormat_ID, "
                    + " c.IsMultiLingualDocument"
                    + " FROM VAM_Routing d"
                    + " INNER JOIN VAF_Client c ON (d.VAF_Client_ID=c.VAF_Client_ID)"
                    + " INNER JOIN VAF_Print_Rpt_Page pf ON (d.VAF_Client_ID=pf.VAF_Client_ID OR pf.VAF_Client_ID=0)"
                    + " LEFT OUTER JOIN VAM_RoutingOperation ro ON (d.VAM_Routing_ID=ro.VAM_Routing_ID) "
                    + " WHERE d.VAM_Routing_ID=@recordid" // info from PrintForm
                    + " AND pf.VAF_Org_ID IN (0,d.VAF_Org_ID) "
                    + "ORDER BY pf.VAF_Client_ID DESC, pf.VAF_Org_ID DESC";
            else if (type == TASKLIST)
                sql = " SELECT dt.DocBaseType, pf.RPL_TList_PrintFormat_ID, " 			//1..2
                    + " pf.PUT_TList_PrintFormat_ID, pf.PCK_CluTList_PrintFormat_ID, "	//3..4
                    + " pf.PCK_OrdTList_PrintFormat_ID, M.PickMethod, "					//5..6
                    + " c.IsMultiLingualDocument, COALESCE(dt.DocumentCopies,0), "		//7..8
                    + " dt.VAF_Print_Rpt_Layout_ID"											//9
                    + " FROM VAM_TaskList M "
                    + " INNER JOIN VAF_Client c ON (M.VAF_Client_ID=c.VAF_Client_ID)"
                    + " INNER JOIN VAF_Print_Rpt_Page pf ON (M.VAF_Client_ID=pf.VAF_Client_ID OR pf.VAF_Client_ID=0)"
                    + " LEFT OUTER JOIN VAB_DocTypes dt ON (M.VAB_DocTypes_ID=dt.VAB_DocTypes_ID) "
                    + " WHERE M.VAM_TaskList_ID=@recordid"
                    + " AND pf.VAF_Org_ID IN (0,M.VAF_Org_ID) "
                    + " ORDER BY pf.VAF_Client_ID DESC,  pf.VAF_Org_ID DESC";
            /****************Manfacturing***********************/
            else	//	Get PrintFormat from Org or 0 of document client
            {
                sql = "SELECT pf.Order_PrintFormat_ID,pf.Shipment_PrintFormat_ID,"		//	1..2
                    //	Prio: 1. BPartner 2. DocType, 3. PrintFormat (Org)	//	see InvoicePrint
                    + " COALESCE (bp.Invoice_PrintFormat_ID,dt.VAF_Print_Rpt_Layout_ID,pf.Invoice_PrintFormat_ID)," // 3
                    + " pf.Project_PrintFormat_ID, pf.Remittance_PrintFormat_ID,"		//	4..5
                    + " c.IsMultiLingualDocument, bp.VAF_Language,"						//	6..7
                    + " COALESCE(dt.DocumentCopies,0)+COALESCE(bp.DocumentCopies,1), " 	// 	8
                    + " dt.VAF_Print_Rpt_Layout_ID,bp.VAB_BusinessPartner_ID,d.DocumentNo "			//	9..11
                    + "FROM " + DOC_BASETABLES[type] + " d"
                    + " INNER JOIN VAF_Client c ON (d.VAF_Client_ID=c.VAF_Client_ID)"
                    + " INNER JOIN VAF_Print_Rpt_Page pf ON (c.VAF_Client_ID=pf.VAF_Client_ID)"
                    + " INNER JOIN VAB_BusinessPartner bp ON (d.VAB_BusinessPartner_ID=bp.VAB_BusinessPartner_ID)"
                    + " LEFT OUTER JOIN VAB_DocTypes dt ON (d.VAB_DocTypes_ID=dt.VAB_DocTypes_ID) "
                    + "WHERE d." + DOC_IDS[type] + "=@recordid"			//	info from PrintForm
                    + " AND pf.VAF_Org_ID IN (0,d.VAF_Org_ID) "
                    + "ORDER BY pf.VAF_Org_ID DESC";
            }
            //
            IDataReader dr=null;
            try
            {
                SqlParameter[] param = new SqlParameter[1];
                param[0] = new SqlParameter("@recordid", Record_ID);
                dr = DataBase.DB.ExecuteReader(sql, param);
                if (dr.Read())
                {
                    if (type == CHECK || type == DUNNING || type == REMITTANCE
                        || type == PROJECT || type == RFQ)
                    {
                        VAF_Print_Rpt_Layout_ID = Utility.Util.GetValueOfInt(dr[0]);// rs.getInt(1);
                        copies = 1;
                        //	Set Language when enabled
                        String VAF_Language = Utility.Util.GetValueOfString(dr[2]);// rs.getString(3);
                        if (VAF_Language != null)// && "Y".equals(rs.getString(2)))	//	IsMultiLingualDocument
                            language = Language.GetLanguage(VAF_Language);
                        VAB_BusinessPartner_ID = Utility.Util.GetValueOfInt(dr[3]);// rs.getInt(4);
                        if (type == DUNNING)
                        {
                            DateTime? ts = Utility.Util.GetValueOfDateTime(dr[4]);// rs.getTimestamp(5);
                            DocumentNo = ts.ToString();
                        }
                        else
                            DocumentNo = Utility.Util.GetValueOfString(dr[4]);// rs.getString(5);
                    }
                    else if (type == MOVEMENT || type == INVENTORY)
                    {
                        VAF_Print_Rpt_Layout_ID = Utility.Util.GetValueOfInt(dr[0]);// rs.getInt(1);
                        copies = Utility.Util.GetValueOfInt(dr[2]);// rs.getInt(3);
                        if (copies == 0)
                            copies = 1;
                    }
                    /******************Manufacturing**************/
                    else if (type == WORKORDER || type == WORKORDERTXN)
                    {
                        int pfVAF_Print_Rpt_Layout_ID = Utility.Util.GetValueOfInt(dr[0]);
                        VAF_Print_Rpt_Layout_ID = Utility.Util.GetValueOfInt(dr[3]);
                        if (VAF_Print_Rpt_Layout_ID == 0)
                            VAF_Print_Rpt_Layout_ID = pfVAF_Print_Rpt_Layout_ID;

                        copies = Utility.Util.GetValueOfInt(dr[2]);
                        if (copies == 0)
                            copies = 1;
                        String VAF_Language = Utility.Util.GetValueOfString(dr[1]);
                        if (VAF_Language != null) // && "Y".equals(rs.getString(6)))	//	IsMultiLingualDocument
                            language = Language.GetLanguage(VAF_Language);
                    }
                    else if (type == STANDARDOPERATION || type == ROUTING)
                    {
                        VAF_Print_Rpt_Layout_ID = Utility.Util.GetValueOfInt(dr[0]);
                        copies = 1;
                        /*String VAF_Language = rs.getString(2);
                        if (VAF_Language != null) // && "Y".equals(rs.getString(6))) // IsMultiLingualDocument
                        language = Language.getLanguage(VAF_Language);*/
                    }
                    else if (type == TASKLIST)
                    {
                        String docBaseType = Utility.Util.GetValueOfString(dr[0]);
                        int replFormatID = Utility.Util.GetValueOfInt(dr[1]);
                        int putFormatID = Utility.Util.GetValueOfInt(dr[2]);
                        int cpickFormatID = Utility.Util.GetValueOfInt(dr[3]);
                        int opickFormatID = Utility.Util.GetValueOfInt(dr[4]);
                        String pmethod = Utility.Util.GetValueOfString(dr[5]);
                        copies = Utility.Util.GetValueOfInt(dr[7]);
                        if (copies == 0)
                            copies = 1;
                        VAF_Print_Rpt_Layout_ID = Utility.Util.GetValueOfInt(dr[8]);
                        if (VAF_Print_Rpt_Layout_ID == 0)
                        {
                            if (docBaseType.ToUpper().Equals("RPL"))
                            {
                                VAF_Print_Rpt_Layout_ID = replFormatID;
                            }
                            else if (docBaseType.ToUpper().Equals("PUT"))
                            {
                                VAF_Print_Rpt_Layout_ID = putFormatID;
                            }
                            else if (docBaseType.ToUpper().Equals("PCK"))
                            {
                                if (pmethod.ToUpper().Equals("C"))
                                {
                                    VAF_Print_Rpt_Layout_ID = cpickFormatID;
                                }
                                else
                                {
                                    VAF_Print_Rpt_Layout_ID = opickFormatID;
                                }
                            }
                        }

                    }
                    /******************Manufacturing**************/
                    else
                    {
                        //	Set PrintFormat
                        VAF_Print_Rpt_Layout_ID = Utility.Util.GetValueOfInt(dr[type]);// rs.getInt(type + 1);
                        if (Utility.Util.GetValueOfInt(dr[8].ToString()) != 0)		//	VAB_DocTypes.VAF_Print_Rpt_Layout_ID
                            VAF_Print_Rpt_Layout_ID = Utility.Util.GetValueOfInt(dr[8].ToString());// rs.getInt(9);
                        copies = Utility.Util.GetValueOfInt(dr[7].ToString());// rs.getInt(8);
                        //	Set Language when enabled
                        String VAF_Language = Utility.Util.GetValueOfString(dr[6].ToString());// rs.getString(7);
                        if (VAF_Language != null) // && "Y".equals(rs.getString(6)))	//	IsMultiLingualDocument
                            language = Language.GetLanguage(VAF_Language);
                        VAB_BusinessPartner_ID = Utility.Util.GetValueOfInt(dr[9]);// rs.getInt(10);
                        DocumentNo = Utility.Util.GetValueOfString(dr[10]);// rs.getString(11);
                    }
                }
                dr.Close();
            }
            catch (Exception e)
            {
                if (dr != null)
                {
                    dr.Close();
                }
                log.Log(Level.SEVERE, "Record_ID=" + Record_ID + ", SQL=" + sql, e);
            }
            if (VAF_Print_Rpt_Layout_ID == 0)
            {
                log.Log(Level.SEVERE, "No PrintFormat found for Type=" + type + ", Record_ID=" + Record_ID);
                return null;
            }

            //	Get Format & Data
            MVAFPrintRptLayout format = MVAFPrintRptLayout.Get(ctx, VAF_Print_Rpt_Layout_ID, false);
            format.SetLanguage(language);		//	BP Language if Multi-Lingual
            //	if (!Env.isBaseLanguage(language, DOC_TABLES[type]))
            format.SetTranslationLanguage(language);


            /*   Set Culture according to BPartner Language */

            System.Globalization.CultureInfo cInfo = new System.Globalization.CultureInfo(language.GetVAF_Language().Replace('_','-'));

            

            System.Threading.Thread.CurrentThread.CurrentCulture = cInfo;
            System.Threading.Thread.CurrentThread.CurrentUICulture = cInfo;

            /*** END  *******/



            //	query
            Query query = new Query(DOC_TABLES[type]);
            query.AddRestriction(DOC_IDS[type], Query.EQUAL, Utility.Util.GetValueOfInt(Record_ID));
            //	log.config( "ReportCtrl.startDocumentPrint - " + format, query + " - " + language.getVAF_Language());
            //
            if (DocumentNo == null || DocumentNo.Length == 0)
                DocumentNo = "DocPrint";
            PrintInfo info = new PrintInfo(
                DocumentNo,
                DOC_TABLE_ID[type],
                Record_ID,
                VAB_BusinessPartner_ID);
            info.SetCopies(copies);
            info.SetDocumentCopy(false);		//	true prints "Copy" on second

            //	Engine
            ReportEngine_N re = new ReportEngine_N(ctx, format, query, info);

            //cInfo = new System.Globalization.CultureInfo("en-US");
            //System.Threading.Thread.CurrentThread.CurrentCulture = cInfo;
            //System.Threading.Thread.CurrentThread.CurrentUICulture = cInfo;

            return re;
        }


        public static void PrintConfirm(int type, int Record_ID)
        {
            StringBuilder sql = new StringBuilder();
            if (type == ORDER || type == SHIPMENT || type == INVOICE)
                sql.Append("UPDATE ").Append(DOC_BASETABLES[type])
                    .Append(" SET DatePrinted=SysDate, IsPrinted='Y' WHERE ")
                    .Append(DOC_IDS[type]).Append("=").Append(Record_ID);
            //
            if (sql.Length > 0)
            {
                int no = DataBase.DB.ExecuteQuery(sql.ToString(), null, null);
                if (no != 1)
                    log.Log(Level.SEVERE, "Updated records=" + no + " - should be just one");
            }
        }	//	printConfirm

        #region "Create CSV File"

        public bool CreateCSV(FileStream fs, char delimiter, Language language)
        {
            return CreateCSV(new StreamWriter(fs,Encoding.UTF8), delimiter, language);
        }

        public bool CreateCSV(StreamWriter writer, char delimiter, Language language)
        {
            if (delimiter == 0)
                delimiter = '\t';

            try
            {
                //Check if any of the ID has a child print format

                //changes made by Jagmohan Bhatt :- Date: 13-july-2010
                for (int col = 0; col < m_printFormat.GetItemCount(); col++)
                {
                    MVAFPrintRptLItem item = m_printFormat.GetItem(col);
                    if (item.IsTypePrintFormat())
                    {
                        //isAnyHasChild = true;
                        int VAF_Column_ID = item.GetVAF_Column_ID();
                        m_printFormat = MVAFPrintRptLayout.Get(GetCtx(), item.GetVAF_Print_Rpt_LayoutChild_ID(), true);
                        Object obj = m_printData.GetNode(VAF_Column_ID, false);

                        PrintDataElement dataElement = (PrintDataElement)obj;
                        String recordString = dataElement.GetValueKey();

                        Query query = new Query(m_printFormat.GetVAF_TableView_ID());
                        query.AddRestriction(item.GetColumnName(), Query.EQUAL, int.Parse(recordString));
                        m_printFormat.SetTranslationViewQuery(query);

                        SetPrintData(query);
                    }
                }
                //changes made by Jagmohan Bhatt :- Date: 13-july-2010
                StringBuilder sb = new StringBuilder();
                for (int row = -1; row < m_printData.GetRowCount(); row++)
                {
                   // StringBuilder sb = new StringBuilder();
                    if (row != -1)
                        m_printData.SetRowIndex(row);

                    //	for all columns
                    bool first = true;	//	first column to print
                    for (int col = 0; col < m_printFormat.GetItemCount(); col++)
                    {
                        MVAFPrintRptLItem item = m_printFormat.GetItem(col);
                        if (item.IsPrinted())
                        {
                            if (first)
                                first = false;
                            else
                                sb.Append(delimiter);
                            if (row == -1)
                                CreateCSVvalue(sb, delimiter, m_printFormat.GetItem(col).GetPrintName(language));
                            else
                            {
                                Object obj = m_printData.GetNode(item.GetVAF_Column_ID(), false);
                                String data = "";
                                if (obj == null)
                                {
                                }
                                else if (obj.GetType() == typeof(PrintDataElement))
                                {
                                    PrintDataElement pde = (PrintDataElement)obj;
                                    if (pde.IsPKey())
                                        data = pde.GetValueAsString();
                                    else
                                        data = pde.GetValueDisplay(language);	//	formatted
                                }
                                else if (obj.GetType() == typeof(PrintData))
                                {
                                }
                                else
                                {
                                }
                                CreateCSVvalue(sb, delimiter, data);
                            }
                        }
                    }
                    //writer.Write(sb.ToString());
                    //writer.Write(Env.NL);
                    sb.Append('\n');
                }
                writer.Write(sb.ToString());
                writer.Write(Env.NL);
                writer.Flush();
                writer.Close();
                return true;
            }
            catch (Exception e)
            {
                log.Severe("ReportEngine_N_CreateCSV_" + e.ToString());
            }

            return true;

        }

        /// <summary>
        /// Add Content to CSV string.
        /// Encapsulate/mask content in " if required
        /// </summary>
        /// <param name="sb">StringBuffer to add to</param>
        /// <param name="delimiter">delimiter</param>
        /// <param name="content">value</param>
        private void CreateCSVvalue(StringBuilder sb, char delimiter, String content)
        {
            //	nothing to add
            if (content == null || content.Length == 0)
                return;
            //
            bool needMask = false;
            StringBuilder buff = new StringBuilder();
            char[] chars = content.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (c == '"')
                {
                    needMask = true;
                    buff.Append(c);		//	repeat twice
                }	//	mask if any control character
                else if (!needMask && (c == delimiter || !Char.IsLetterOrDigit(c)))
                    needMask = true;
                buff.Append(c);
            }

            //	Optionally mask value
            if (needMask)
                sb.Append('"').Append(buff).Append('"');
            else
                sb.Append(buff);
        }	//	addCSVColumnValue
        #endregion

        #region "Create HTML File"
        public bool CreateHTML(FileStream fs, bool onlyTable, Language language)
        {
            CreateHTML(new StreamWriter(fs), onlyTable, language);
            return false;
        }

        /// <summary>
        /// Create the HTML Report
        /// </summary>
        /// <param name="writer">StreamWrite Object</param>
        /// <param name="onlyTable">does not in operation rightn now</param>
        /// <param name="language">current language</param>
        /// <returns>HTML Content</returns>
        public bool CreateHTML(StreamWriter writer, bool onlyTable, Language language)
        {
            MVAFPrintRptTblLayout tf = m_printFormat.GetTableFormat();
            MVAFPrintRptFont printFont = MVAFPrintRptFont.Get(m_printFormat.GetVAF_Print_Rpt_Font_ID());
            tf.SetStandard_Font(printFont.GetFont());
            StringBuilder sb = new StringBuilder(@"<html><head><title>Report : " + m_printData.GetTableName() + "</title></head><body>");

            sb.Append(@"<table cellpadding='2' cellspacing='0' width='100%'>");
            for (int row = -1; row < m_printData.GetRowCount(); row++)
            {
                if (row != -1)
                    m_printData.SetRowIndex(row);

                int i = 0;
                for (int col = 0; col < m_printFormat.GetItemCount(); col++)
                {
                    MVAFPrintRptLItem item = m_printFormat.GetItem(col);
                    if (item.GetVAF_Print_Rpt_Font_ID() != 0)
                    {
                        MVAFPrintRptFont font = MVAFPrintRptFont.Get(item.GetVAF_Print_Rpt_Font_ID());
                    }
                    if (item.IsPrinted())
                    {
                        i = i + 1;
                        if (row == -1)
                        {
                            if (i == 1)
                                sb.Append(CreateHeaderRow(tf, item, true));

                            sb.Append("<td" + CreateHeaderRow(tf, item, false) + ">");
                            sb.Append(Utility.Util.MaskHTML(item.GetPrintName(language)));
                            sb.Append("</td>");

                        }
                        else
                        {
                            if ((i == 1) && (row != -1))
                                sb.Append("</tr>").Append(CreateDataRow(tf, item, true));

                            sb.Append("<td" + CreateDataRow(tf, item, false) + ">");
                            Object obj = m_printData.GetNode(item.GetVAF_Column_ID(), false);
                            if (obj == null)
                                sb.Append(@"&nbsp;");
                            else if (obj.GetType() == typeof(PrintDataElement))
                            {
                                String value = ((PrintDataElement)obj).GetValueDisplay(language);	//	formatted
                                sb.Append(Utility.Util.MaskHTML(value));
                            }
                            else if (obj.GetType() == typeof(PrintData))
                            {
                                //ignore contained data
                            }
                            sb.Append("</td>");
                        }
                    }
                }
                sb.Append("</tr>");
            }
            sb.Append("</table></body></html>");

            writer.Write(sb.ToString());
            writer.Flush();
            writer.Close();

            return true;
        }

        private string CreateHeaderRow(MVAFPrintRptTblLayout tbf, MVAFPrintRptLItem item, bool row)
        {
            StringBuilder sb = new StringBuilder("");
            if (row)
            {
                sb.Append("<tr");
                sb.Append(@" style='height:28px;background-color:" + System.Drawing.ColorTranslator.ToHtml(tbf.GetHeaderBG_Color()));
                sb.Append(";font-family:" + tbf.GetHeader_Font().Name);
                sb.Append(";color:" + System.Drawing.ColorTranslator.ToHtml(tbf.GetHeaderFG_Color()));
                sb.Append(";font-size:" + tbf.GetHeader_Font().Size);
                if (tbf.GetHeader_Font().Style == System.Drawing.FontStyle.Bold)
                    sb.Append(";font-weight:bold");
                if (tbf.GetHeader_Font().Style == System.Drawing.FontStyle.Italic)
                    sb.Append(";font-style:italic");

                sb.Append("'>");
            }
            else
            {
                if (tbf.IsPaintHeaderLines())
                {
                    sb.Append(" style='");
                    sb.Append("border-color:" + ColorTranslator.ToHtml(tbf.GetHeaderLine_Color()));
                    sb.Append(";border-width:" + tbf.GetHdrStroke() + "px");
                    sb.Append(";border-top-style:" + tbf.GetHeader_Stroke());
                    sb.Append(";border-bottom-style:" + tbf.GetHeader_Stroke());

                    sb.Append("'");
                }
            }
            //sb.Append(">");
            return sb.ToString();
        }


        private string CreateDataRow(MVAFPrintRptTblLayout tbf, MVAFPrintRptLItem item, bool row)
        {

            StringBuilder sb = new StringBuilder("");
            if (row)
            {
                sb.Append("<tr ");

                if (m_printData.IsFunctionRow())
                {
                    sb.Append(@" style='height:25px;background-color:" + System.Drawing.ColorTranslator.ToHtml(tbf.GetFunctBG_Color()));
                    sb.Append(";font-family:" + tbf.GetFunct_Font().Name);
                    sb.Append(";color:" + System.Drawing.ColorTranslator.ToHtml(tbf.GetFunctFG_Color()));
                    sb.Append(";font-size:" + tbf.GetFunct_Font().Size);
                    if (tbf.GetFunct_Font().Style == System.Drawing.FontStyle.Bold)
                        sb.Append(";font-weight:bold");
                    if (tbf.GetFunct_Font().Style == System.Drawing.FontStyle.Italic)
                        sb.Append(";font-style:italic");

                    sb.Append("'");
                }

                sb.Append(">");
            }
            else    //data rows
            {
                sb.Append(" style='");
                if (m_printData.IsFunctionRow())
                {
                    sb.Append("border-color:" + ColorTranslator.ToHtml(tbf.GetHeaderLine_Color()));
                    sb.Append(";border-width:" + tbf.GetHdrStroke() + "px");
                    sb.Append(";border-bottom-style:" + tbf.GetHeader_Stroke());

                }
                else
                {

                    sb.Append("font-family:" + tbf.GetStandard_Font().Name);
                    sb.Append(";font-size:" + tbf.GetStandard_Font().Size);
                    sb.Append(";width:" + item.GetMaxWidth() + "px");
                    if (tbf.GetStandard_Font().Style == FontStyle.Bold)
                        sb.Append(";font-weight:bold");
                    if (tbf.GetStandard_Font().Style == FontStyle.Italic)
                        sb.Append(";font-style:italic");

                }
                sb.Append("'");
            }
            return sb.ToString();
        }

        #endregion

        public string GetCsvReportFilePath(string data)
        {
            return null;
        }
        public string GetRtfReportFilePath(string data)
        {
            return null;
        }
        
    }
}
