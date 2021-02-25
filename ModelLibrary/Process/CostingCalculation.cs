﻿/********************************************************
    * Project Name   : VAdvantage
    * Class Name     : CostingCalculation
    * Purpose        : Calculate Cost for Products
    * Class Used     : ProcessEngine.SvrProcess
    * Chronological    Development
    * Amit Bansal     19-May-2016
******************************************************/


using System;
using System.Collections.Generic;
using System.Data;
//using System.Data.OracleClient;
using System.Linq;
using System.Text;
using VAdvantage.DataBase;
using VAdvantage.Logging;
using VAdvantage.Model;
using VAdvantage.ProcessEngine;
using VAdvantage.Utility;

namespace VAdvantage.Process
{
    public class CostingCalculation : SvrProcess
    {
        private StringBuilder sql = new StringBuilder();
        //Trx trx = Trx.GetTrx(Trx.CreateTrxName("Costing"));
        private static VLogger _log = VLogger.GetVLogger(typeof(CostingCalculation).FullName);

        DateTime? currentDate = DateTime.Now;
        DateTime? minDateRecord;
        //String _TrxDate = null;

        DataSet dsRecord = null;
        //DataSet dsInOut = null;
        //DataSet dsInvoice = null;
        //DataSet dsInventory = null;
        //DataSet dsMovement = null;
        //DataSet dsProductionExecution = null;
        DataSet dsChildRecord = null;
        //DataSet dsMatchPO = null;
        //DataRow[] dataMatchPO = null;
        //DataRow[] dataRow = null;

        Decimal quantity = 0;
        decimal currentCostPrice = 0;
        decimal amt = 0;
        // Order Line amt included (taxable amt + tax amt + surcharge amt)
        Decimal ProductOrderLineCost = 0;
        Decimal ProductOrderPriceActual = 0;
        // Invoice Line amt included (taxable amt + tax amt + surcharge amt)
        Decimal ProductInvoiceLineCost = 0;
        Decimal ProductInvoicePriceActual = 0;

        MVAMInventory inventory = null;
        MVAMInventoryLine inventoryLine = null;

        MVAMInventoryTransfer movement = null;
        MVAMInvTrfLine movementLine = null;
        //MWarehouse warehouse = null;
        MVAMLocator locatorTo = null; // is used to get "to warehouse" reference and "to org" reference for getting cost from prodyc costs 
        Decimal toCurrentCostPrice = 0; // is used to maintain cost of "move to" 

        MVAMInvInOut inout = null;
        MVAMInvInOutLine inoutLine = null;
        MVABOrderLine orderLine = null;
        MVABOrder order = null;

        MVABInvoice invoice = null;
        MVABInvoiceLine invoiceLine = null;
        bool isCostAdjustableOnLost = false;

        MVAMProduct product = null;

        //MVAMMatchPO match = null;
        MVAMMatchInvoice matchInvoice = null;
        X_VAM_MatchInvoiceCostTrack matchInvCostReverse = null;

        int table_WrkOdrTrnsctionLine = 0;
        MVAFTableView tbl_WrkOdrTrnsctionLine = null;
        int table_WrkOdrTransaction = 0;
        MVAFTableView tbl_WrkOdrTransaction = null;
        PO po_WrkOdrTransaction = null;
        String woTrxType = null;
        PO po_WrkOdrTrnsctionLine = null;
        int table_AssetDisposal = 0;
        MVAFTableView tbl_AssetDisposal = null;
        PO po_AssetDisposal = null;


        //Production
        int CountCostNotAvialable = 1;

        string conversionNotFoundInvoice = "";
        string conversionNotFoundInOut = "";
        string conversionNotFoundInventory = "";
        string conversionNotFoundMovement = "";
        string conversionNotFoundProductionExecution = "";
        string conversionNotFoundInvoice1 = "";
        string conversionNotFoundInOut1 = "";
        string conversionNotFoundInventory1 = "";
        string conversionNotFoundMovement1 = "";
        string conversionNotFoundProductionExecution1 = "";
        string conversionNotFound = "";

        protected override void Prepare()
        {
            ;
        }

        protected override string DoIt()
        {
            try
            {
                _log.Info("Cost Calculation Start on " + DateTime.Now);

                // check Manufacturing Modeule exist or not
                //int count = Util.GetValueOfInt(DB.ExecuteScalar("SELECT COUNT(VAF_MODULEINFO_ID) FROM VAF_MODULEINFO WHERE PREFIX='VAMFG_' AND Isactive = 'Y' "));
                int count = Env.IsModuleInstalled("VAMFG_") ? 1 : 0;

                // check VAFAM Modeule exist or not
                int countVAFAM = Env.IsModuleInstalled("VAFAM_") ? 1 : 0;

                // check Manufacturing Modeule exist or not
                //int countGOM01 = Util.GetValueOfInt(DB.ExecuteScalar("SELECT COUNT(VAF_MODULEINFO_ID) FROM VAF_MODULEINFO WHERE PREFIX='GOM01_' AND Isactive = 'Y' "));
                int countGOM01 = Env.IsModuleInstalled("GOM01_") ? 1 : 0;

                // check IsCostAdjustmentOnLost exist on product 
                sql.Clear();
                sql.Append(@"SELECT COUNT(*) FROM VAF_Column WHERE IsActive = 'Y' AND 
                                       VAF_TableView_ID =  ( SELECT VAF_TableView_ID FROM VAF_TableView WHERE IsActive = 'Y' AND TableName LIKE 'VAM_Product' ) 
                                       AND ColumnName = 'IsCostAdjustmentOnLost' ");
                int countColumnExist = Util.GetValueOfInt(DB.ExecuteScalar(sql.ToString(), null, null));
                sql.Clear();

                // min date record from the transaction window
                minDateRecord = SerachMinDate(count);

                if (count > 0)
                {
                    table_WrkOdrTransaction = Util.GetValueOfInt(DB.ExecuteScalar("SELECT VAF_TABLEVIEW_ID  FROM VAF_TABLEVIEW WHERE tablename = 'VAMFG_M_WrkOdrTransaction' AND IsActive = 'Y' "));
                    tbl_WrkOdrTransaction = new MVAFTableView(GetCtx(), table_WrkOdrTransaction, null);

                    table_WrkOdrTrnsctionLine = Util.GetValueOfInt(DB.ExecuteScalar("SELECT VAF_TABLEVIEW_ID  FROM VAF_TABLEVIEW WHERE tablename = 'VAMFG_M_WrkOdrTrnsctionLine' AND IsActive = 'Y' "));
                    tbl_WrkOdrTrnsctionLine = new MVAFTableView(GetCtx(), table_WrkOdrTrnsctionLine, null);
                }
                if (countVAFAM > 0)
                {
                    table_AssetDisposal = Util.GetValueOfInt(DB.ExecuteScalar("SELECT VAF_TABLEVIEW_ID  FROM VAF_TABLEVIEW WHERE tablename = 'VAFAM_AssetDisposal' AND IsActive = 'Y' "));
                    tbl_AssetDisposal = new MVAFTableView(GetCtx(), table_AssetDisposal, null);
                }

                int diff = (int)(Math.Ceiling((DateTime.Now.Date - minDateRecord.Value.Date).TotalDays));

                for (int days = 0; days <= diff; days++)
                {
                    if (days != 0)
                    {
                        minDateRecord = minDateRecord.Value.AddDays(1);
                    }

                    _log.Info("Cost Calculation Start for " + minDateRecord);

                    sql.Clear();
                    sql.Append(@"SELECT * FROM ( 
                    SELECT vaf_client_id , vaf_org_id , isactive , to_char(created, 'DD-MON-YY HH24:MI:SS') as created , createdby , to_char(updated, 'DD-MON-YY HH24:MI:SS') as updated , updatedby ,  
                           documentno , VAM_Inv_InOut_id AS Record_Id , issotrx ,  isreturntrx , ''  AS IsInternalUse, 'VAM_Inv_InOut' AS TableName,
                           docstatus, movementdate AS DateAcct , iscostcalculated , isreversedcostcalculated 
                     FROM VAM_Inv_InOut WHERE dateacct   = " + GlobalVariable.TO_DATE(minDateRecord, true) + @" AND isactive = 'Y'
                           AND ((docstatus IN ('CO' , 'CL') AND iscostcalculated = 'N' ) OR (docstatus IN ('RE') AND iscostcalculated = 'Y'
                           AND ISREVERSEDCOSTCALCULATED= 'N' AND description LIKE '%{->%')) 
                    UNION
                         SELECT vaf_client_id , vaf_org_id , isactive , to_char(created, 'DD-MON-YY HH24:MI:SS') as created , createdby ,  to_char(updated, 'DD-MON-YY HH24:MI:SS') as updated , updatedby ,
                                documentno , VAB_Invoice_id AS Record_Id , issotrx , isreturntrx , '' AS IsInternalUse, 'VAB_Invoice' AS TableName,
                                docstatus, DateAcct AS DateAcct, iscostcalculated , isreversedcostcalculated 
                         FROM VAB_Invoice WHERE dateacct = " + GlobalVariable.TO_DATE(minDateRecord, true) + @" AND isactive     = 'Y'
                              AND ((docstatus IN ('CO' , 'CL') AND iscostcalculated = 'N' ) OR (docstatus  IN ('RE') AND iscostcalculated = 'Y'
                              AND ISREVERSEDCOSTCALCULATED= 'N' AND description LIKE '%{->%')) 
                         UNION 
                         SELECT i.vaf_client_id ,  i.vaf_org_id , i.isactive , to_char(mi.created, 'DD-MON-YY HH24:MI:SS') as created ,  i.createdby ,  TO_CHAR(mi.updated, 'DD-MON-YY HH24:MI:SS') AS updated ,
                                i.updatedby ,  mi.documentno ,  VAM_MatchInvoice_Id AS Record_Id ,  i.issotrx ,  i.isreturntrx ,  '' AS IsInternalUse,  'VAM_MatchInvoice' AS TableName,
                                i.docstatus, i.DateAcct AS DateAcct,  mi.iscostcalculated ,  i.isreversedcostcalculated
                         FROM VAM_MatchInvoice mi INNER JOIN VAB_InvoiceLine il ON il.VAB_InvoiceLine_id = mi.VAB_InvoiceLine_id INNER JOIN VAB_Invoice i ON i.VAB_Invoice_id       = il.VAB_Invoice_id
                              WHERE mi.dateacct = " + GlobalVariable.TO_DATE(minDateRecord, true) + @" AND i.isactive        = 'Y' AND (i.docstatus       IN ('CO' , 'CL') AND mi.iscostcalculated = 'N' )

                         UNION 
                         SELECT i.vaf_client_id ,  i.vaf_org_id , i.isactive ,to_char(mi.created, 'DD-MON-YY HH24:MI:SS') as  created ,  i.createdby ,  TO_CHAR(mi.updated, 'DD-MON-YY HH24:MI:SS') AS updated ,
                                i.updatedby ,  null AS documentno ,  VAM_MatchInvoiceoiceCostTrack_Id AS Record_Id ,  i.issotrx ,  i.isreturntrx ,  '' AS IsInternalUse,  'VAM_MatchInvoiceoiceCostTrack' AS TableName,
                                i.docstatus,i.DateAcct AS DateAcct,  mi.iscostcalculated ,  mi.iscostimmediate AS isreversedcostcalculated
                          FROM VAM_MatchInvoiceoiceCostTrack mi INNER JOIN VAB_InvoiceLine il ON il.VAB_InvoiceLine_id = mi.VAB_InvoiceLine_id INNER JOIN VAB_Invoice i ON i.VAB_Invoice_id       = il.VAB_Invoice_id
                          WHERE mi.updated >= " + GlobalVariable.TO_DATE(minDateRecord, true) + @" AND mi.updated < " + GlobalVariable.TO_DATE(minDateRecord.Value.AddDays(1), true) + @"  AND i.isactive        = 'Y' AND (i.docstatus       IN ('RE' , 'VO') )

                         UNION
                         SELECT vaf_client_id , vaf_org_id , isactive ,to_char(created, 'DD-MON-YY HH24:MI:SS') as created , createdby ,  to_char(updated, 'DD-MON-YY HH24:MI:SS') as updated , updatedby ,
                                documentno , VAM_Inventory_id AS Record_Id , '' AS issotrx , '' AS isreturntrx , IsInternalUse, 'VAM_Inventory' AS TableName,
                                docstatus, movementdate AS DateAcct ,  iscostcalculated ,  isreversedcostcalculated 
                         FROM VAM_Inventory WHERE movementdate = " + GlobalVariable.TO_DATE(minDateRecord, true) + @" AND isactive       = 'Y'
                              AND ((docstatus   IN ('CO' , 'CL') AND iscostcalculated = 'N' ) OR (docstatus IN ('RE') AND iscostcalculated = 'Y'
                              AND ISREVERSEDCOSTCALCULATED= 'N' AND description LIKE '%{->%')) 
                         UNION
                         SELECT vaf_client_id , vaf_org_id , isactive , to_char(created, 'DD-MON-YY HH24:MI:SS') as created , createdby ,  to_char(updated, 'DD-MON-YY HH24:MI:SS') as updated , updatedby , 
                                documentno ,  VAM_InventoryTransfer_id AS Record_Id , '' AS issotrx , ''  AS isreturntrx , ''  AS IsInternalUse,  'VAM_InventoryTransfer'  AS TableName,
                                docstatus, movementdate AS DateAcct ,  iscostcalculated ,  isreversedcostcalculated 
                         FROM VAM_InventoryTransfer WHERE movementdate = " + GlobalVariable.TO_DATE(minDateRecord, true) + @" AND isactive       = 'Y'
                               AND ((docstatus   IN ('CO' , 'CL') AND iscostcalculated = 'N' ) OR (docstatus IN ('RE') AND iscostcalculated        = 'Y'
                               AND ISREVERSEDCOSTCALCULATED= 'N' AND description LIKE '%{->%'))
                         UNION
                         SELECT vaf_client_id , vaf_org_id , isactive ,to_char(created, 'DD-MON-YY HH24:MI:SS') as created , createdby ,  to_char(updated, 'DD-MON-YY HH24:MI:SS') as updated , updatedby , 
                                name AS documentno ,  VAM_Production_ID AS Record_Id , IsReversed AS issotrx , ''  AS isreturntrx , ''  AS IsInternalUse,  'VAM_Production'  AS TableName,
                                '' AS docstatus , movementdate AS DateAcct ,  iscostcalculated ,  isreversedcostcalculated 
                         FROM VAM_Production WHERE movementdate = " + GlobalVariable.TO_DATE(minDateRecord, true) + @" AND isactive       = 'Y'
                               AND ((PROCESSED = 'Y' AND iscostcalculated = 'N' AND IsReversed = 'N' ) OR (PROCESSED = 'Y' AND iscostcalculated  = 'Y'
                               AND ISREVERSEDCOSTCALCULATED= 'N' AND IsReversed = 'Y' AND Name LIKE '%{->%'))");
                    if (count > 0)
                    {
                        sql.Append(@" UNION
                        SELECT vaf_client_id , vaf_org_id , isactive ,to_char(created, 'DD-MON-YY HH24:MI:SS') as created , createdby ,  to_char(updated, 'DD-MON-YY HH24:MI:SS') as updated , updatedby , 
                                DOCUMENTNO ,  VAMFG_M_WrkOdrTransaction_id AS Record_Id , '' AS issotrx , '' AS isreturntrx , '' AS IsInternalUse,  'VAMFG_M_WrkOdrTransaction'  AS TableName,
                                docstatus , vamfg_dateacct AS DateAcct , iscostcalculated ,  isreversedcostcalculated 
                         FROM VAMFG_M_WrkOdrTransaction WHERE VAMFG_WorkOrderTxnType IN ('CI', 'CR' , 'AI' , 'AR') AND vamfg_dateacct = " + GlobalVariable.TO_DATE(minDateRecord, true) + @" 
                              AND isactive  = 'Y' AND ((docstatus IN ('CO' , 'CL') AND iscostcalculated = 'N' ) OR (docstatus IN ('RE' , 'VO') AND iscostcalculated = 'Y'
                              AND ISREVERSEDCOSTCALCULATED  = 'N' AND VAMFG_description LIKE '%{->%')) ");
                    }
                    if (countVAFAM > 0)
                    {
                        sql.Append(@" UNION
                        SELECT vaf_client_id , vaf_org_id , isactive ,to_char(created, 'DD-MON-YY HH24:MI:SS') as created , createdby ,  to_char(updated, 'DD-MON-YY HH24:MI:SS') as updated , updatedby ,
                                documentno , VAFAM_AssetDisposal_ID AS Record_Id , '' AS issotrx , '' AS isreturntrx ,'' AS IsInternalUse, 'VAFAM_AssetDisposal' AS TableName,
                                docstatus , vafam_trxdate AS DateAcct ,  iscostcalculated ,  isreversedcostcalculated 
                         FROM VAFAM_AssetDisposal WHERE vafam_trxdate = " + GlobalVariable.TO_DATE(minDateRecord, true) + @" AND isactive  = 'Y'
                              AND ((docstatus   IN ('CO' , 'CL') AND iscostcalculated = 'N' ) OR (docstatus IN ('RE' , 'VO') AND iscostcalculated = 'Y'
                              AND ISREVERSEDCOSTCALCULATED= 'N' AND ReversalDoc_ID != 0) )");
                    }
                    sql.Append(@" ) t order by dateacct , created");
                    dsRecord = DB.ExecuteDataset(sql.ToString(), null, Get_Trx());

                    // Complete Record
                    if (dsRecord != null && dsRecord.Tables.Count > 0 && dsRecord.Tables[0].Rows.Count > 0)
                    {
                        for (int z = 0; z < dsRecord.Tables[0].Rows.Count; z++)
                        {
                            // for checking - costing calculate on completion or not
                            // IsCostImmediate = true - calculate cost on completion else through process
                            MVAFClient client = MVAFClient.Get(GetCtx(), Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["vaf_client_id"]));

                            #region Cost Calculation For Material Receipt
                            try
                            {
                                if (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["TableName"]) == "VAM_Inv_InOut" &&
                                    Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["issotrx"]) == "N" &&
                                    Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["isreturntrx"]) == "N" &&
                                    (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["docstatus"]) == "CO" ||
                                     Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["docstatus"]) == "CL"))
                                {
                                    inout = new MVAMInvInOut(GetCtx(), Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]), Get_Trx());

                                    sql.Clear();
                                    if (inout.GetDescription() != null && inout.GetDescription().Contains("{->"))
                                    {
                                        sql.Append("SELECT * FROM VAM_Inv_InOutLine WHERE IsActive = 'Y' AND iscostcalculated = 'Y' AND IsReversedCostCalculated = 'N' " +
                                                    " AND VAM_Inv_InOut_ID = " + inout.GetVAM_Inv_InOut_ID() + " ORDER BY Line ");
                                    }
                                    else
                                    {
                                        sql.Append("SELECT * FROM VAM_Inv_InOutLine WHERE IsActive = 'Y' AND iscostcalculated = 'N' " +
                                                     " AND VAM_Inv_InOut_ID = " + inout.GetVAM_Inv_InOut_ID() + " ORDER BY Line ");
                                    }
                                    dsChildRecord = DB.ExecuteDataset(sql.ToString(), null, Get_Trx());
                                    if (dsChildRecord != null && dsChildRecord.Tables.Count > 0 && dsChildRecord.Tables[0].Rows.Count > 0)
                                    {
                                        for (int j = 0; j < dsChildRecord.Tables[0].Rows.Count; j++)
                                        {
                                            try
                                            {
                                                inoutLine = new MVAMInvInOutLine(GetCtx(), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_Inv_InOutLine_ID"]), Get_Trx());
                                                orderLine = new MVABOrderLine(GetCtx(), inoutLine.GetVAB_OrderLine_ID(), null);
                                                if (orderLine != null && orderLine.GetVAB_Order_ID() > 0)
                                                {
                                                    order = new MVABOrder(GetCtx(), orderLine.GetVAB_Order_ID(), null);
                                                    if (order.GetDocStatus() != "VO")
                                                    {
                                                        if (orderLine != null && orderLine.GetVAB_Order_ID() > 0 && orderLine.GetQtyOrdered() == 0)
                                                            continue;
                                                    }
                                                }
                                                product = new MVAMProduct(GetCtx(), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_Product_ID"]), Get_Trx());
                                                if (product.GetProductType() == "I") // for Item Type product
                                                {
                                                    bool isUpdatePostCurrentcostPriceFromMR = MVAMProductCostElement.IsPOCostingmethod(GetCtx(), inout.GetVAF_Client_ID(), product.GetVAM_Product_ID(), Get_Trx());

                                                    #region Material Receipt
                                                    if (!inout.IsSOTrx() && !inout.IsReturnTrx())
                                                    {
                                                        if (orderLine == null || orderLine.GetVAB_OrderLine_ID() == 0) //MR Without PO
                                                        {
                                                            #region MR Without PO
                                                            if (!client.IsCostImmediate() || !inoutLine.IsCostImmediate() || inoutLine.GetCurrentCostPrice() == 0)
                                                            {
                                                                // get price from VAM_ProductCost (Current Cost Price)
                                                                currentCostPrice = 0;
                                                                currentCostPrice = MVAMVAMProductCost.GetproductCostAndQtyMaterial(inoutLine.GetVAF_Client_ID(), inoutLine.GetVAF_Org_ID(),
                                                                    inoutLine.GetVAM_Product_ID(), inoutLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), inout.GetVAM_Warehouse_ID(), false);
                                                                inoutLine.SetCurrentCostPrice(currentCostPrice);
                                                                if (!inoutLine.Save(Get_Trx()))
                                                                {
                                                                    ValueNamePair pp = VLogger.RetrieveError();
                                                                    _log.Info("Error found for Material Receipt for this Line ID = " + inoutLine.GetVAM_Inv_InOutLine_ID() +
                                                                               " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                    Get_Trx().Rollback();
                                                                }
                                                            }
                                                            if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), inout.GetVAF_Client_ID(), inout.GetVAF_Org_ID(), product, inoutLine.GetVAM_PFeature_SetInstance_ID(),
                                                           "Material Receipt", null, inoutLine, null, null, null, 0, inoutLine.GetMovementQty(), Get_Trx(), out conversionNotFoundInOut))
                                                            {
                                                                if (!conversionNotFoundInOut1.Contains(conversionNotFoundInOut))
                                                                {
                                                                    conversionNotFoundInOut1 += conversionNotFoundInOut + " , ";
                                                                }
                                                                _log.Info("Cost not Calculated for Material Receipt for this Line ID = " + inoutLine.GetVAM_Inv_InOutLine_ID());
                                                            }
                                                            else
                                                            {
                                                                if (inoutLine.GetCurrentCostPrice() == 0 || isUpdatePostCurrentcostPriceFromMR)
                                                                {
                                                                    // get price from VAM_ProductCost (Current Cost Price)
                                                                    currentCostPrice = 0;
                                                                    currentCostPrice = MVAMVAMProductCost.GetproductCostAndQtyMaterial(inoutLine.GetVAF_Client_ID(), inoutLine.GetVAF_Org_ID(),
                                                                        inoutLine.GetVAM_Product_ID(), inoutLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), inout.GetVAM_Warehouse_ID(), false);
                                                                }
                                                                if (inoutLine.GetCurrentCostPrice() == 0)
                                                                {
                                                                    inoutLine.SetCurrentCostPrice(currentCostPrice);
                                                                }
                                                                if (isUpdatePostCurrentcostPriceFromMR && inoutLine.GetPostCurrentCostPrice() == 0)
                                                                {
                                                                    inoutLine.SetPostCurrentCostPrice(currentCostPrice);
                                                                }
                                                                if (inout.GetDescription() != null && inout.GetDescription().Contains("{->"))
                                                                {
                                                                    inoutLine.SetIsReversedCostCalculated(true);
                                                                }
                                                                inoutLine.SetIsCostCalculated(true);
                                                                if (client.IsCostImmediate() && !inoutLine.IsCostImmediate())
                                                                {
                                                                    inoutLine.SetIsCostImmediate(true);
                                                                }
                                                                if (!inoutLine.Save(Get_Trx()))
                                                                {
                                                                    ValueNamePair pp = VLogger.RetrieveError();
                                                                    _log.Info("Error found for Material Receipt for this Line ID = " + inoutLine.GetVAM_Inv_InOutLine_ID() +
                                                                               " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                    Get_Trx().Rollback();
                                                                }
                                                                else
                                                                {
                                                                    _log.Fine("Cost Calculation updated for VAM_Inv_InOutLine = " + inoutLine.GetVAM_Inv_InOutLine_ID());
                                                                    Get_Trx().Commit();
                                                                }
                                                            }
                                                            #endregion
                                                        }
                                                        else
                                                        {
                                                            #region MR With PO
                                                            if (!client.IsCostImmediate() || !inoutLine.IsCostImmediate() || inoutLine.GetCurrentCostPrice() == 0)
                                                            {
                                                                // get price from VAM_ProductCost (Current Cost Price)
                                                                currentCostPrice = 0;
                                                                currentCostPrice = MVAMVAMProductCost.GetproductCostAndQtyMaterial(inoutLine.GetVAF_Client_ID(), inoutLine.GetVAF_Org_ID(),
                                                                    inoutLine.GetVAM_Product_ID(), inoutLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), inout.GetVAM_Warehouse_ID(), false);
                                                                inoutLine.SetCurrentCostPrice(currentCostPrice);
                                                                if (!inoutLine.Save(Get_Trx()))
                                                                {
                                                                    ValueNamePair pp = VLogger.RetrieveError();
                                                                    _log.Info("Error found for Material Receipt for this Line ID = " + inoutLine.GetVAM_Inv_InOutLine_ID() +
                                                                               " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                    Get_Trx().Rollback();
                                                                }
                                                            }

                                                            ProductOrderLineCost = orderLine.GetProductLineCost(orderLine);
                                                            ProductOrderPriceActual = ProductOrderLineCost / orderLine.GetQtyEntered();
                                                            amt = 0;
                                                            if (isCostAdjustableOnLost && inoutLine.GetMovementQty() < orderLine.GetQtyOrdered() && order.GetDocStatus() != "VO")
                                                            {
                                                                amt = ProductOrderLineCost;
                                                            }
                                                            else if (!isCostAdjustableOnLost && inoutLine.GetMovementQty() < orderLine.GetQtyOrdered() && order.GetDocStatus() != "VO")
                                                            {
                                                                amt = Decimal.Multiply(Decimal.Divide(ProductOrderLineCost, orderLine.GetQtyOrdered()), inoutLine.GetMovementQty());
                                                            }
                                                            else if (order.GetDocStatus() != "VO")
                                                            {
                                                                amt = Decimal.Multiply(Decimal.Divide(ProductOrderLineCost, orderLine.GetQtyOrdered()), inoutLine.GetMovementQty());
                                                            }
                                                            else if (order.GetDocStatus() == "VO")
                                                            {
                                                                amt = Decimal.Multiply(ProductOrderPriceActual, inoutLine.GetQtyEntered());
                                                            }

                                                            if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), inout.GetVAF_Client_ID(), inout.GetVAF_Org_ID(), product, inoutLine.GetVAM_PFeature_SetInstance_ID(),
                                                               "Material Receipt", null, inoutLine, null, null, null, amt,
                                                               inoutLine.GetMovementQty(), Get_Trx(), out conversionNotFoundInOut))
                                                            {
                                                                if (!conversionNotFoundInOut1.Contains(conversionNotFoundInOut))
                                                                {
                                                                    conversionNotFoundInOut1 += conversionNotFoundInOut + " , ";
                                                                }
                                                                _log.Info("Cost not Calculated for Material Receipt for this Line ID = " + inoutLine.GetVAM_Inv_InOutLine_ID());
                                                            }
                                                            else
                                                            {
                                                                if (inoutLine.GetCurrentCostPrice() == 0 || isUpdatePostCurrentcostPriceFromMR)
                                                                {
                                                                    // get price from VAM_ProductCost (Current Cost Price)
                                                                    currentCostPrice = 0;
                                                                    currentCostPrice = MVAMVAMProductCost.GetproductCostAndQtyMaterial(inoutLine.GetVAF_Client_ID(), inoutLine.GetVAF_Org_ID(),
                                                                        inoutLine.GetVAM_Product_ID(), inoutLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), inout.GetVAM_Warehouse_ID(), false);
                                                                }
                                                                if (inoutLine.GetCurrentCostPrice() == 0)
                                                                {
                                                                    inoutLine.SetCurrentCostPrice(currentCostPrice);
                                                                }
                                                                if (isUpdatePostCurrentcostPriceFromMR && inoutLine.GetPostCurrentCostPrice() == 0)
                                                                {
                                                                    inoutLine.SetPostCurrentCostPrice(currentCostPrice);
                                                                }
                                                                if (inout.GetDescription() != null && inout.GetDescription().Contains("{->"))
                                                                {
                                                                    inoutLine.SetIsReversedCostCalculated(true);
                                                                }
                                                                inoutLine.SetIsCostCalculated(true);
                                                                if (client.IsCostImmediate() && !inoutLine.IsCostImmediate())
                                                                {
                                                                    inoutLine.SetIsCostImmediate(true);
                                                                }
                                                                if (!inoutLine.Save(Get_Trx()))
                                                                {
                                                                    ValueNamePair pp = VLogger.RetrieveError();
                                                                    _log.Info("Error found for Material Receipt for this Line ID = " + inoutLine.GetVAM_Inv_InOutLine_ID() +
                                                                               " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                    Get_Trx().Rollback();
                                                                }
                                                                else
                                                                {
                                                                    _log.Fine("Cost Calculation updated for VAM_Inv_InOutLine = " + inoutLine.GetVAM_Inv_InOutLine_ID());
                                                                    Get_Trx().Commit();
                                                                }
                                                            }
                                                            #endregion
                                                        }
                                                    }
                                                    #endregion
                                                }
                                            }
                                            catch
                                            {

                                            }
                                        }
                                    }
                                    sql.Clear();
                                    if (inout.GetDescription() != null && inout.GetDescription().Contains("{->"))
                                    {
                                        sql.Append("SELECT COUNT(*) FROM VAM_Inv_InOutLine WHERE IsReversedCostCalculated = 'N' AND IsActive = 'Y' AND VAM_Inv_InOut_ID = " + inout.GetVAM_Inv_InOut_ID());
                                    }
                                    else
                                    {
                                        sql.Append("SELECT COUNT(*) FROM VAM_Inv_InOutLine WHERE IsCostCalculated = 'N' AND IsActive = 'Y' AND VAM_Inv_InOut_ID = " + inout.GetVAM_Inv_InOut_ID());
                                    }
                                    if (Util.GetValueOfInt(DB.ExecuteScalar(sql.ToString(), null, Get_Trx())) <= 0)
                                    {
                                        if (inout.GetDescription() != null && inout.GetDescription().Contains("{->"))
                                        {
                                            inout.SetIsReversedCostCalculated(true);
                                        }
                                        inout.SetIsCostCalculated(true);
                                        if (!inout.Save(Get_Trx()))
                                        {
                                            ValueNamePair pp = VLogger.RetrieveError();
                                            _log.Info("Error found for saving VAM_Inv_InOut for this Record ID = " + inout.GetVAM_Inv_InOut_ID() +
                                                       " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());

                                        }
                                        else
                                        {
                                            _log.Fine("Cost Calculation updated for VAM_Inv_InOut = " + inout.GetVAM_Inv_InOut_ID());
                                            Get_Trx().Commit();
                                        }
                                    }
                                    continue;
                                }
                            }
                            catch { }
                            #endregion

                            #region Cost Calculation for SO / PO(not to be executed) / CRMA / VRMA
                            try
                            {
                                if (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["TableName"]) == "VAB_Invoice" &&
                                    (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["docstatus"]) == "CO" ||
                                     Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["docstatus"]) == "CL"))
                                {
                                    invoice = new MVABInvoice(GetCtx(), Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]), Get_Trx());

                                    sql.Clear();
                                    if (invoice.GetDescription() != null && invoice.GetDescription().Contains("{->"))
                                    {
                                        sql.Append("SELECT * FROM VAB_InvoiceLine WHERE IsActive = 'Y' AND iscostcalculated = 'Y' AND IsReversedCostCalculated = 'N' " +
                                                     " AND VAB_Invoice_ID = " + invoice.GetVAB_Invoice_ID() + " ORDER BY VAM_Product_ID DESC ");
                                    }
                                    else
                                    {
                                        sql.Append("SELECT * FROM VAB_InvoiceLine WHERE IsActive = 'Y' AND iscostcalculated = 'N' " +
                                                     " AND VAB_Invoice_ID = " + invoice.GetVAB_Invoice_ID() + " ORDER BY VAM_Product_ID DESC ");
                                    }
                                    dsChildRecord = DB.ExecuteDataset(sql.ToString(), null, Get_Trx());
                                    if (dsChildRecord != null && dsChildRecord.Tables.Count > 0 && dsChildRecord.Tables[0].Rows.Count > 0)
                                    {
                                        for (int j = 0; j < dsChildRecord.Tables[0].Rows.Count; j++)
                                        {
                                            try
                                            {
                                                product = new MVAMProduct(GetCtx(), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_Product_ID"]), Get_Trx());
                                                invoiceLine = new MVABInvoiceLine(GetCtx(), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAB_InvoiceLine_ID"]), Get_Trx());
                                                if (invoiceLine != null && invoiceLine.Get_ID() > 0)
                                                {
                                                    ProductInvoiceLineCost = invoiceLine.GetProductLineCost(invoiceLine);
                                                    ProductInvoicePriceActual = ProductInvoiceLineCost / invoiceLine.GetQtyEntered();
                                                }
                                                if (invoiceLine != null && invoiceLine.GetVAB_Invoice_ID() > 0 && invoiceLine.GetQtyInvoiced() == 0)
                                                    continue;
                                                if (invoiceLine.GetVAB_OrderLine_ID() > 0)
                                                {
                                                    if (invoiceLine.GetVAB_Charge_ID() > 0)
                                                    {
                                                        #region Landed Cost Allocation
                                                        if (!invoice.IsSOTrx() && !invoice.IsReturnTrx())
                                                        {
                                                            if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), invoice.GetVAF_Client_ID(), invoice.GetVAF_Org_ID(), null,
                                                                0, "Invoice(Vendor)", null, null, null, invoiceLine, null, ProductInvoiceLineCost, 0, Get_Trx(), out conversionNotFoundInvoice))
                                                            {
                                                                if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                                                {
                                                                    conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                                                }
                                                                _log.Info("Cost not Calculated for Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                            }
                                                            else
                                                            {
                                                                if (invoice.GetDescription() != null && invoice.GetDescription().Contains("{->"))
                                                                {
                                                                    invoiceLine.SetIsReversedCostCalculated(true);
                                                                }
                                                                invoiceLine.SetIsCostCalculated(true);
                                                                if (client.IsCostImmediate() && !invoiceLine.IsCostImmediate())
                                                                {
                                                                    invoiceLine.SetIsCostImmediate(true);
                                                                }
                                                                if (!invoiceLine.Save(Get_Trx()))
                                                                {
                                                                    ValueNamePair pp = VLogger.RetrieveError();
                                                                    _log.Info("Error found for saving Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID() +
                                                                               " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                    Get_Trx().Rollback();
                                                                }
                                                                else
                                                                {
                                                                    _log.Fine("Cost Calculation updated for m_invoiceline = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                                    Get_Trx().Commit();
                                                                }
                                                            }
                                                        }
                                                        #endregion
                                                    }
                                                    else
                                                    {
                                                        #region for Expense type product
                                                        if (product.GetProductType() == "E" && product.GetVAM_Product_ID() > 0)
                                                        {
                                                            if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), invoice.GetVAF_Client_ID(), invoice.GetVAF_Org_ID(), product, 0,
                                                                 "Invoice(Vendor)", null, null, null, invoiceLine, null, ProductInvoiceLineCost, 0, Get_Trx(), out conversionNotFoundInvoice))
                                                            {
                                                                if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                                                {
                                                                    conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                                                }
                                                                _log.Info("Cost not Calculated for Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                            }
                                                            else
                                                            {
                                                                if (invoice.GetDescription() != null && invoice.GetDescription().Contains("{->"))
                                                                {
                                                                    invoiceLine.SetIsReversedCostCalculated(true);
                                                                }
                                                                invoiceLine.SetIsCostCalculated(true);
                                                                if (client.IsCostImmediate() && !invoiceLine.IsCostImmediate())
                                                                {
                                                                    invoiceLine.SetIsCostImmediate(true);
                                                                }
                                                                if (!invoiceLine.Save(Get_Trx()))
                                                                {
                                                                    ValueNamePair pp = VLogger.RetrieveError();
                                                                    _log.Info("Error found for saving Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID() +
                                                                               " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                    Get_Trx().Rollback();
                                                                }
                                                                else
                                                                {
                                                                    _log.Fine("Cost Calculation updated for m_invoiceline = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                                    Get_Trx().Commit();
                                                                }
                                                            }
                                                        }
                                                        #endregion

                                                        #region  for Item Type product
                                                        else if (product.GetProductType() == "I" && product.GetVAM_Product_ID() > 0)
                                                        {
                                                            if (countColumnExist > 0)
                                                            {
                                                                isCostAdjustableOnLost = product.IsCostAdjustmentOnLost();
                                                            }
                                                            MVABOrderLine ol1 = null;
                                                            MVABOrder order1 = new MVABOrder(GetCtx(), invoice.GetVAB_Order_ID(), Get_Trx());
                                                            ol1 = new MVABOrderLine(GetCtx(), invoiceLine.GetVAB_OrderLine_ID(), Get_Trx());
                                                            ProductOrderLineCost = ol1.GetProductLineCost(ol1);
                                                            ProductOrderPriceActual = ProductOrderLineCost / ol1.GetQtyEntered();

                                                            if (order1.GetVAB_Order_ID() == 0)
                                                            {
                                                                //ol1 = new MVABOrderLine(GetCtx(), invoiceLine.GetVAB_OrderLine_ID(), Get_Trx());
                                                                order1 = new MVABOrder(GetCtx(), ol1.GetVAB_Order_ID(), Get_Trx());
                                                            }

                                                            #region  Sales Cycle
                                                            if (order1.IsSOTrx() && !order1.IsReturnTrx())
                                                            {
                                                                if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), invoice.GetVAF_Client_ID(), invoice.GetVAF_Org_ID(), product, invoiceLine.GetVAM_PFeature_SetInstance_ID(),
                                                                      "Invoice(Customer)", null, null, null, invoiceLine, null, Decimal.Negate(ProductInvoiceLineCost), Decimal.Negate(invoiceLine.GetQtyInvoiced()),
                                                                      Get_Trx(), out conversionNotFoundInvoice))
                                                                {
                                                                    if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                                                    {
                                                                        conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                                                    }
                                                                    _log.Info("Cost not Calculated for Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                                }
                                                                else
                                                                {
                                                                    if (invoice.GetDescription() != null && invoice.GetDescription().Contains("{->"))
                                                                    {
                                                                        invoiceLine.SetIsReversedCostCalculated(true);
                                                                    }
                                                                    invoiceLine.SetIsCostCalculated(true);
                                                                    if (client.IsCostImmediate() && !invoiceLine.IsCostImmediate())
                                                                    {
                                                                        invoiceLine.SetIsCostImmediate(true);
                                                                    }
                                                                    if (!invoiceLine.Save(Get_Trx()))
                                                                    {
                                                                        ValueNamePair pp = VLogger.RetrieveError();
                                                                        _log.Info("Error found for saving Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID() +
                                                                                   " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                        Get_Trx().Rollback();
                                                                    }
                                                                    else
                                                                    {
                                                                        _log.Fine("Cost Calculation updated for m_invoiceline = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                                        Get_Trx().Commit();
                                                                    }
                                                                }
                                                            }
                                                            #endregion

                                                            #region Purchase Cycle (not to be executed)
                                                            else if (!order1.IsSOTrx() && !order1.IsReturnTrx() && 0 == 1)
                                                            {
                                                                // calculate cost of MR first if not calculate which is linked with that invoice line
                                                                if (invoiceLine.GetVAM_Inv_InOutLine_ID() > 0)
                                                                {
                                                                    inoutLine = new MVAMInvInOutLine(GetCtx(), invoiceLine.GetVAM_Inv_InOutLine_ID(), Get_Trx());
                                                                    if (!inoutLine.IsCostCalculated())
                                                                    {
                                                                        if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), inoutLine.GetVAF_Client_ID(), inoutLine.GetVAF_Org_ID(), product, inoutLine.GetVAM_PFeature_SetInstance_ID(),
                                                                    "Material Receipt", null, inoutLine, null, invoiceLine, null,
                                                                    order1 != null && order1.GetDocStatus() != "VO" ? Decimal.Multiply(Decimal.Divide(ProductOrderLineCost, ol1.GetQtyOrdered()), inoutLine.GetMovementQty())
                                                                    : Decimal.Multiply(ProductOrderPriceActual, inoutLine.GetQtyEntered()),
                                                                    inoutLine.GetMovementQty(), Get_Trx(), out conversionNotFoundInOut))
                                                                        {
                                                                            if (!conversionNotFoundInOut1.Contains(conversionNotFoundInOut))
                                                                            {
                                                                                conversionNotFoundInOut1 += conversionNotFoundInOut + " , ";
                                                                            }
                                                                            _log.Info("Cost not Calculated for Material Receipt for this Line ID = " + inoutLine.GetVAM_Inv_InOutLine_ID());
                                                                            continue;
                                                                        }
                                                                        else
                                                                        {
                                                                            inoutLine.SetIsCostCalculated(true);
                                                                            if (!inoutLine.Save(Get_Trx()))
                                                                            {
                                                                                ValueNamePair pp = VLogger.RetrieveError();
                                                                                _log.Info("Error found for saving Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID() +
                                                                                           " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                                Get_Trx().Rollback();
                                                                                continue;
                                                                            }
                                                                            else
                                                                            {
                                                                                Get_Trx().Commit();
                                                                            }
                                                                        }
                                                                    }

                                                                    // when isCostAdjustableOnLost = true on product and movement qty on MR is less than invoice qty then consider MR qty else invoice qty
                                                                    if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), invoice.GetVAF_Client_ID(), invoice.GetVAF_Org_ID(), product, invoiceLine.GetVAM_PFeature_SetInstance_ID(),
                                                                          "Invoice(Vendor)", null, null, null, invoiceLine, null, ProductInvoiceLineCost,
                                                                          countColumnExist > 0 && isCostAdjustableOnLost && invoiceLine.GetVAM_Inv_InOutLine_ID() > 0 && inoutLine.GetMovementQty() < invoiceLine.GetQtyInvoiced() ? inoutLine.GetMovementQty() : invoiceLine.GetQtyInvoiced(),
                                                                          Get_Trx(), out conversionNotFoundInvoice))
                                                                    {
                                                                        if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                                                        {
                                                                            conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                                                        }
                                                                        _log.Info("Cost not Calculated for Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                                    }
                                                                    else
                                                                    {
                                                                        if (invoice.GetDescription() != null && invoice.GetDescription().Contains("{->"))
                                                                        {
                                                                            invoiceLine.SetIsReversedCostCalculated(true);
                                                                        }
                                                                        invoiceLine.SetIsCostCalculated(true);
                                                                        if (client.IsCostImmediate() && !invoiceLine.IsCostImmediate())
                                                                        {
                                                                            invoiceLine.SetIsCostImmediate(true);
                                                                        }
                                                                        if (!invoiceLine.Save(Get_Trx()))
                                                                        {
                                                                            ValueNamePair pp = VLogger.RetrieveError();
                                                                            _log.Info("Error found for saving Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID() +
                                                                                       " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                            Get_Trx().Rollback();
                                                                        }
                                                                        else
                                                                        {
                                                                            _log.Fine("Cost Calculation updated for m_invoiceline = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                                            Get_Trx().Commit();
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                            #endregion

                                                            #region CRMA
                                                            else if (order1.IsSOTrx() && order1.IsReturnTrx())
                                                            {
                                                                if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), invoice.GetVAF_Client_ID(), invoice.GetVAF_Org_ID(), product, invoiceLine.GetVAM_PFeature_SetInstance_ID(),
                                                                  "Invoice(Customer)", null, null, null, invoiceLine, null, ProductInvoiceLineCost,
                                                                  invoiceLine.GetQtyInvoiced(), Get_Trx(), out conversionNotFoundInvoice))
                                                                {
                                                                    if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                                                    {
                                                                        conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                                                    }
                                                                    _log.Info("Cost not Calculated for Invoice(Customer) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                                }
                                                                else
                                                                {
                                                                    if (invoice.GetDescription() != null && invoice.GetDescription().Contains("{->"))
                                                                    {
                                                                        invoiceLine.SetIsReversedCostCalculated(true);
                                                                    }
                                                                    invoiceLine.SetIsCostCalculated(true);
                                                                    if (client.IsCostImmediate() && !invoiceLine.IsCostImmediate())
                                                                    {
                                                                        invoiceLine.SetIsCostImmediate(true);
                                                                    }
                                                                    if (!invoiceLine.Save(Get_Trx()))
                                                                    {
                                                                        ValueNamePair pp = VLogger.RetrieveError();
                                                                        _log.Info("Error found for saving Invoice(Customer) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID() +
                                                                                   " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                        Get_Trx().Rollback();
                                                                    }
                                                                    else
                                                                    {
                                                                        _log.Fine("Cost Calculation updated for m_invoiceline = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                                        Get_Trx().Commit();
                                                                    }
                                                                }
                                                            }
                                                            #endregion

                                                            #region VRMA
                                                            else if (!order1.IsSOTrx() && order1.IsReturnTrx())
                                                            {
                                                                //change 12-5-2016
                                                                // when Ap Credit memo is alone then we will do a impact on costing.
                                                                // this is bcz of giving discount for particular product
                                                                // discount is given only when document type having setting as "Treat As Discount" = True
                                                                MVABDocTypes docType = new MVABDocTypes(GetCtx(), invoice.GetVAB_DocTypesTarget_ID(), Get_Trx());
                                                                if (docType.GetDocBaseType() == "APC" && docType.IsTreatAsDiscount() && invoiceLine.GetVAB_OrderLine_ID() == 0 && invoiceLine.GetVAM_Inv_InOutLine_ID() == 0 && invoiceLine.GetVAM_Product_ID() > 0)
                                                                {
                                                                    if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), invoice.GetVAF_Client_ID(), invoice.GetVAF_Org_ID(), product, invoiceLine.GetVAM_PFeature_SetInstance_ID(),
                                                                      "Invoice(Vendor)", null, null, null, invoiceLine, null, Decimal.Negate(ProductInvoiceLineCost), Decimal.Negate(invoiceLine.GetQtyInvoiced()),
                                                                      Get_Trx(), out conversionNotFoundInvoice))
                                                                    {
                                                                        if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                                                        {
                                                                            conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                                                        }
                                                                        _log.Info("Cost not Calculated for Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                                    }
                                                                    else
                                                                    {
                                                                        if (invoice.GetDescription() != null && invoice.GetDescription().Contains("{->"))
                                                                        {
                                                                            invoiceLine.SetIsReversedCostCalculated(true);
                                                                        }
                                                                        if (invoiceLine.Get_ColumnIndex("PostCurrentCostPrice") >= 0 && invoiceLine.GetPostCurrentCostPrice() == 0)
                                                                        {
                                                                            // get post cost after invoice cost calculation and update on invoice
                                                                            currentCostPrice = MVAMVAMProductCost.GetproductCosts(invoiceLine.GetVAF_Client_ID(), invoiceLine.GetVAF_Org_ID(),
                                                                                                                            product.GetVAM_Product_ID(), invoiceLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx());
                                                                            invoiceLine.SetPostCurrentCostPrice(currentCostPrice);
                                                                        }
                                                                        invoiceLine.SetIsCostCalculated(true);
                                                                        if (client.IsCostImmediate() && !invoiceLine.IsCostImmediate())
                                                                        {
                                                                            invoiceLine.SetIsCostImmediate(true);
                                                                        }
                                                                        if (!invoiceLine.Save(Get_Trx()))
                                                                        {
                                                                            ValueNamePair pp = VLogger.RetrieveError();
                                                                            _log.Info("Error found for saving Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID() +
                                                                                       " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                            Get_Trx().Rollback();
                                                                        }
                                                                        else
                                                                        {
                                                                            _log.Fine("Cost Calculation updated for m_invoiceline = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                                            Get_Trx().Commit();
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                            #endregion
                                                        }
                                                        #endregion
                                                    }
                                                }
                                                else
                                                {
                                                    #region for Landed Cost Allocation
                                                    if (invoiceLine.GetVAB_Charge_ID() > 0)
                                                    {
                                                        if (!invoice.IsSOTrx() && !invoice.IsReturnTrx())
                                                        {
                                                            if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), invoice.GetVAF_Client_ID(), invoice.GetVAF_Org_ID(), null, 0,
                                                                "Invoice(Vendor)", null, null, null, invoiceLine, null, ProductInvoiceLineCost, 0, Get_TrxName(), out conversionNotFoundInvoice))
                                                            {
                                                                if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                                                {
                                                                    conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                                                }
                                                                _log.Info("Cost not Calculated for Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                            }
                                                            else
                                                            {
                                                                if (invoice.GetDescription() != null && invoice.GetDescription().Contains("{->"))
                                                                {
                                                                    invoiceLine.SetIsReversedCostCalculated(true);
                                                                }
                                                                invoiceLine.SetIsCostCalculated(true);
                                                                if (client.IsCostImmediate() && !invoiceLine.IsCostImmediate())
                                                                {
                                                                    invoiceLine.SetIsCostImmediate(true);
                                                                }
                                                                if (!invoiceLine.Save(Get_Trx()))
                                                                {
                                                                    ValueNamePair pp = VLogger.RetrieveError();
                                                                    _log.Info("Error found for saving Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID() +
                                                                               " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                    Get_Trx().Rollback();
                                                                }
                                                                else
                                                                {
                                                                    _log.Fine("Cost Calculation updated for m_invoiceline = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                                    Get_Trx().Commit();
                                                                }
                                                            }
                                                        }
                                                    }
                                                    #endregion

                                                    #region for Expense type product
                                                    if (product.GetProductType() == "E" && product.GetVAM_Product_ID() > 0)
                                                    {
                                                        if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), invoice.GetVAF_Client_ID(), invoice.GetVAF_Org_ID(), product, 0,
                                                            "Invoice(Vendor)", null, null, null, invoiceLine, null, ProductInvoiceLineCost, 0, Get_TrxName(), out conversionNotFoundInvoice))
                                                        {
                                                            if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                                            {
                                                                conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                                            }
                                                            _log.Info("Cost not Calculated for Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                        }
                                                        else
                                                        {
                                                            if (invoice.GetDescription() != null && invoice.GetDescription().Contains("{->"))
                                                            {
                                                                invoiceLine.SetIsReversedCostCalculated(true);
                                                            }
                                                            invoiceLine.SetIsCostCalculated(true);
                                                            if (client.IsCostImmediate() && !invoiceLine.IsCostImmediate())
                                                            {
                                                                invoiceLine.SetIsCostImmediate(true);
                                                            }
                                                            if (!invoiceLine.Save(Get_Trx()))
                                                            {
                                                                ValueNamePair pp = VLogger.RetrieveError();
                                                                _log.Info("Error found for saving Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID() +
                                                                           " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                Get_Trx().Rollback();
                                                            }
                                                            else
                                                            {
                                                                _log.Fine("Cost Calculation updated for m_invoiceline = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                                Get_Trx().Commit();
                                                            }
                                                        }
                                                    }
                                                    #endregion

                                                    #region  for Item Type product
                                                    else if (product.GetProductType() == "I" && product.GetVAM_Product_ID() > 0)
                                                    {
                                                        if (countColumnExist > 0)
                                                        {
                                                            isCostAdjustableOnLost = product.IsCostAdjustmentOnLost();
                                                        }

                                                        #region Sales Order
                                                        if (invoice.IsSOTrx() && !invoice.IsReturnTrx())
                                                        {
                                                            if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), invoice.GetVAF_Client_ID(), invoice.GetVAF_Org_ID(), product, invoiceLine.GetVAM_PFeature_SetInstance_ID(),
                                                                  "Invoice(Customer)", null, null, null, invoiceLine, null, Decimal.Negate(ProductInvoiceLineCost), Decimal.Negate(invoiceLine.GetQtyInvoiced()),
                                                                  Get_Trx(), out conversionNotFoundInvoice))
                                                            {
                                                                if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                                                {
                                                                    conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                                                }
                                                                _log.Info("Cost not Calculated for Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                            }
                                                            else
                                                            {
                                                                if (invoice.GetDescription() != null && invoice.GetDescription().Contains("{->"))
                                                                {
                                                                    invoiceLine.SetIsReversedCostCalculated(true);
                                                                }
                                                                invoiceLine.SetIsCostCalculated(true);
                                                                if (client.IsCostImmediate() && !invoiceLine.IsCostImmediate())
                                                                {
                                                                    invoiceLine.SetIsCostImmediate(true);
                                                                }
                                                                if (!invoiceLine.Save(Get_Trx()))
                                                                {
                                                                    ValueNamePair pp = VLogger.RetrieveError();
                                                                    _log.Info("Error found for saving Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID() +
                                                                               " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                    Get_Trx().Rollback();
                                                                }
                                                                else
                                                                {
                                                                    _log.Fine("Cost Calculation updated for m_invoiceline = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                                    Get_Trx().Commit();
                                                                }
                                                            }
                                                        }
                                                        #endregion

                                                        #region Purchase Cycle (not to be executed)
                                                        else if (!invoice.IsSOTrx() && !invoice.IsReturnTrx() && 0 == 1)
                                                        {
                                                            // calculate cost of MR first if not calculate which is linked with that invoice line
                                                            if (invoiceLine.GetVAM_Inv_InOutLine_ID() > 0)
                                                            {
                                                                inoutLine = new MVAMInvInOutLine(GetCtx(), invoiceLine.GetVAM_Inv_InOutLine_ID(), Get_Trx());
                                                                if (!inoutLine.IsCostCalculated())
                                                                {
                                                                    if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), GetVAF_Client_ID(), inoutLine.GetVAF_Org_ID(), product, inoutLine.GetVAM_PFeature_SetInstance_ID(),
                                                                "Material Receipt", null, inoutLine, null, invoiceLine, null, 0, inoutLine.GetMovementQty(), Get_Trx(), out conversionNotFoundInOut))
                                                                    {
                                                                        if (!conversionNotFoundInOut1.Contains(conversionNotFoundInOut))
                                                                        {
                                                                            conversionNotFoundInOut1 += conversionNotFoundInOut + " , ";
                                                                        }
                                                                        _log.Info("Cost not Calculated for Material Receipt for this Line ID = " + inoutLine.GetVAM_Inv_InOutLine_ID());
                                                                        continue;
                                                                    }
                                                                    else
                                                                    {
                                                                        inoutLine.SetIsCostCalculated(true);
                                                                        if (!inoutLine.Save(Get_Trx()))
                                                                        {
                                                                            ValueNamePair pp = VLogger.RetrieveError();
                                                                            _log.Info("Error found for saving Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID() +
                                                                                       " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                            Get_Trx().Rollback();
                                                                            continue;
                                                                        }
                                                                        else
                                                                        {
                                                                            Get_Trx().Commit();
                                                                        }
                                                                    }
                                                                }
                                                            }

                                                            // when isCostAdjustableOnLost = true on product and movement qty on MR is less than invoice qty then consider MR qty else invoice qty
                                                            if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), invoice.GetVAF_Client_ID(), invoice.GetVAF_Org_ID(), product, invoiceLine.GetVAM_PFeature_SetInstance_ID(),
                                                                  "Invoice(Vendor)", null, null, null, invoiceLine, null, ProductInvoiceLineCost,
                                                                  countColumnExist > 0 && isCostAdjustableOnLost && invoiceLine.GetVAM_Inv_InOutLine_ID() > 0 && inoutLine.GetMovementQty() < invoiceLine.GetQtyInvoiced() ? inoutLine.GetMovementQty() : invoiceLine.GetQtyInvoiced(),
                                                                  Get_Trx(), out conversionNotFoundInvoice))
                                                            {
                                                                if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                                                {
                                                                    conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                                                }
                                                                _log.Info("Cost not Calculated for Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                            }
                                                            else
                                                            {
                                                                if (invoice.GetDescription() != null && invoice.GetDescription().Contains("{->"))
                                                                {
                                                                    invoiceLine.SetIsReversedCostCalculated(true);
                                                                }
                                                                invoiceLine.SetIsCostCalculated(true);
                                                                if (client.IsCostImmediate() && !invoiceLine.IsCostImmediate())
                                                                {
                                                                    invoiceLine.SetIsCostImmediate(true);
                                                                }
                                                                if (!invoiceLine.Save(Get_Trx()))
                                                                {
                                                                    ValueNamePair pp = VLogger.RetrieveError();
                                                                    _log.Info("Error found for saving Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID() +
                                                                               " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                    Get_Trx().Rollback();
                                                                }
                                                                else
                                                                {
                                                                    _log.Fine("Cost Calculation updated for m_invoiceline = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                                    Get_Trx().Commit();
                                                                }
                                                            }
                                                        }
                                                        #endregion

                                                        #region CRMA
                                                        else if (invoice.IsSOTrx() && invoice.IsReturnTrx())
                                                        {
                                                            if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), invoice.GetVAF_Client_ID(), invoice.GetVAF_Org_ID(), product, invoiceLine.GetVAM_PFeature_SetInstance_ID(),
                                                              "Invoice(Customer)", null, null, null, invoiceLine, null, ProductInvoiceLineCost, invoiceLine.GetQtyInvoiced(),
                                                              Get_Trx(), out conversionNotFoundInvoice))
                                                            {
                                                                if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                                                {
                                                                    conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                                                }
                                                                _log.Info("Cost not Calculated for Invoice(Customer) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                            }
                                                            else
                                                            {
                                                                if (invoice.GetDescription() != null && invoice.GetDescription().Contains("{->"))
                                                                {
                                                                    invoiceLine.SetIsReversedCostCalculated(true);
                                                                }
                                                                invoiceLine.SetIsCostCalculated(true);
                                                                if (client.IsCostImmediate() && !invoiceLine.IsCostImmediate())
                                                                {
                                                                    invoiceLine.SetIsCostImmediate(true);
                                                                }
                                                                if (!invoiceLine.Save(Get_Trx()))
                                                                {
                                                                    ValueNamePair pp = VLogger.RetrieveError();
                                                                    _log.Info("Error found for saving Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID() +
                                                                               " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                    Get_Trx().Rollback();
                                                                }
                                                                else
                                                                {
                                                                    _log.Fine("Cost Calculation updated for m_invoiceline = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                                    Get_Trx().Commit();
                                                                }
                                                            }
                                                        }
                                                        #endregion

                                                        #region VRMA
                                                        else if (!invoice.IsSOTrx() && invoice.IsReturnTrx())
                                                        {
                                                            // when Ap Credit memo is alone then we will do a impact on costing.
                                                            // this is bcz of giving discount for particular product
                                                            // discount is given only when document type having setting as "Treat As Discount" = True
                                                            MVABDocTypes docType = new MVABDocTypes(GetCtx(), invoice.GetVAB_DocTypesTarget_ID(), Get_Trx());
                                                            if (docType.GetDocBaseType() == "APC" && docType.IsTreatAsDiscount() && invoiceLine.GetVAB_OrderLine_ID() == 0 && invoiceLine.GetVAM_Inv_InOutLine_ID() == 0 && invoiceLine.GetVAM_Product_ID() > 0)
                                                            {
                                                                if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), invoice.GetVAF_Client_ID(), invoice.GetVAF_Org_ID(), product, invoiceLine.GetVAM_PFeature_SetInstance_ID(),
                                                                  "Invoice(Vendor)", null, null, null, invoiceLine, null, Decimal.Negate(ProductInvoiceLineCost), Decimal.Negate(invoiceLine.GetQtyInvoiced()),
                                                                  Get_Trx(), out conversionNotFoundInvoice))
                                                                {
                                                                    if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                                                    {
                                                                        conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                                                    }
                                                                    _log.Info("Cost not Calculated for Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                                }
                                                                else
                                                                {
                                                                    if (invoice.GetDescription() != null && invoice.GetDescription().Contains("{->"))
                                                                    {
                                                                        invoiceLine.SetIsReversedCostCalculated(true);
                                                                    }
                                                                    if (invoiceLine.Get_ColumnIndex("PostCurrentCostPrice") >= 0 && invoiceLine.GetPostCurrentCostPrice() == 0)
                                                                    {
                                                                        // get post cost after invoice cost calculation and update on invoice
                                                                        currentCostPrice = MVAMVAMProductCost.GetproductCosts(invoiceLine.GetVAF_Client_ID(), invoiceLine.GetVAF_Org_ID(),
                                                                                                                        product.GetVAM_Product_ID(), invoiceLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx());
                                                                        invoiceLine.SetPostCurrentCostPrice(currentCostPrice);
                                                                    }
                                                                    invoiceLine.SetIsCostCalculated(true);
                                                                    if (client.IsCostImmediate() && !invoiceLine.IsCostImmediate())
                                                                    {
                                                                        invoiceLine.SetIsCostImmediate(true);
                                                                    }
                                                                    if (!invoiceLine.Save(Get_Trx()))
                                                                    {
                                                                        ValueNamePair pp = VLogger.RetrieveError();
                                                                        _log.Info("Error found for saving Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID() +
                                                                                   " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                        Get_Trx().Rollback();
                                                                    }
                                                                    else
                                                                    {
                                                                        _log.Fine("Cost Calculation updated for m_invoiceline = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                                        Get_Trx().Commit();
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        #endregion
                                                    }
                                                    #endregion
                                                }
                                            }
                                            catch { }
                                        }
                                    }
                                    sql.Clear();
                                    if (invoice.GetDescription() != null && invoice.GetDescription().Contains("{->"))
                                    {
                                        sql.Append("SELECT COUNT(*) FROM VAB_InvoiceLine WHERE IsReversedCostCalculated = 'N' AND IsActive = 'Y' AND VAB_Invoice_ID = " + invoice.GetVAB_Invoice_ID());
                                    }
                                    else
                                    {
                                        sql.Append("SELECT COUNT(*) FROM VAB_InvoiceLine WHERE IsCostCalculated = 'N' AND IsActive = 'Y' AND VAB_Invoice_ID = " + invoice.GetVAB_Invoice_ID());
                                    }
                                    if (Util.GetValueOfInt(DB.ExecuteScalar(sql.ToString(), null, Get_Trx())) <= 0)
                                    {
                                        if (invoice.GetDescription() != null && invoice.GetDescription().Contains("{->"))
                                        {
                                            invoice.SetIsReversedCostCalculated(true);
                                        }
                                        invoice.SetIsCostCalculated(true);
                                        if (!invoice.Save(Get_Trx()))
                                        {
                                            ValueNamePair pp = VLogger.RetrieveError();
                                            _log.Info("Error found for saving VAB_Invoice for this Record ID = " + invoice.GetVAB_Invoice_ID() +
                                                       " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                        }
                                        else
                                        {
                                            _log.Fine("Cost Calculation updated for m_invoice = " + invoice.GetVAB_Invoice_ID());
                                            Get_Trx().Commit();
                                        }
                                    }
                                    continue;
                                }
                            }
                            catch { }
                            #endregion

                            #region Cost Calculation for  PO Cycle
                            try
                            {
                                if (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["TableName"]) == "VAM_MatchInvoice" &&
                                    (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["issotrx"]) == "N" &&
                                     Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["isreturntrx"]) == "N"))
                                {
                                    matchInvoice = new MVAMMatchInvoice(GetCtx(), Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]), Get_Trx());
                                    inoutLine = new MVAMInvInOutLine(GetCtx(), matchInvoice.GetVAM_Inv_InOutLine_ID(), Get_Trx());
                                    invoiceLine = new MVABInvoiceLine(GetCtx(), matchInvoice.GetVAB_InvoiceLine_ID(), Get_Trx());
                                    invoice = new MVABInvoice(GetCtx(), invoiceLine.GetVAB_Invoice_ID(), Get_Trx());
                                    product = new MVAMProduct(GetCtx(), invoiceLine.GetVAM_Product_ID(), Get_Trx());
                                    int VAM_Warehouse_ID = inoutLine.GetVAM_Warehouse_ID();
                                    if (inoutLine.GetVAB_OrderLine_ID() > 0)
                                    {
                                        orderLine = new MVABOrderLine(GetCtx(), inoutLine.GetVAB_OrderLine_ID(), Get_Trx());
                                        order = new MVABOrder(GetCtx(), orderLine.GetVAB_Order_ID(), Get_Trx());
                                        ProductOrderLineCost = orderLine.GetProductLineCost(orderLine);
                                        ProductOrderPriceActual = ProductOrderLineCost / orderLine.GetQtyEntered();
                                    }
                                    ProductInvoiceLineCost = invoiceLine.GetProductLineCost(invoiceLine);
                                    if (product.GetProductType() == "I" && product.GetVAM_Product_ID() > 0)
                                    {
                                        bool isUpdatePostCurrentcostPriceFromMR = MVAMProductCostElement.IsPOCostingmethod(GetCtx(), product.GetVAF_Client_ID(), product.GetVAM_Product_ID(), Get_Trx());
                                        if (countColumnExist > 0)
                                        {
                                            isCostAdjustableOnLost = product.IsCostAdjustmentOnLost();
                                        }

                                        // calculate cost of MR first if not calculate which is linked with that invoice line
                                        if (!inoutLine.IsCostCalculated())
                                        {
                                            #region calculate cost of MR first if not calculate which is linked with that invoice line
                                            if (inoutLine.GetCurrentCostPrice() == 0)
                                            {
                                                // get price from VAM_ProductCost (Current Cost Price)
                                                currentCostPrice = 0;
                                                currentCostPrice = MVAMVAMProductCost.GetproductCostAndQtyMaterial(inoutLine.GetVAF_Client_ID(), inoutLine.GetVAF_Org_ID(),
                                                    inoutLine.GetVAM_Product_ID(), inoutLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), VAM_Warehouse_ID, false);
                                                _log.Info("product cost " + inoutLine.GetVAM_Product_ID() + " - " + currentCostPrice);
                                                DB.ExecuteQuery("UPDATE VAM_Inv_InOutLine SET CurrentCostPrice = " + currentCostPrice + " WHERE VAM_Inv_InOutLine_ID = " + inoutLine.GetVAM_Inv_InOutLine_ID(), null, Get_Trx());
                                            }
                                            if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), inoutLine.GetVAF_Client_ID(), inoutLine.GetVAF_Org_ID(), product, inoutLine.GetVAM_PFeature_SetInstance_ID(),
                                                "Material Receipt", null, inoutLine, null, invoiceLine, null,
                                                order != null && order.GetDocStatus() != "VO" ? Decimal.Multiply(Decimal.Divide(ProductOrderLineCost, orderLine.GetQtyOrdered()), inoutLine.GetMovementQty())
                                                : Decimal.Multiply(ProductOrderPriceActual, inoutLine.GetQtyEntered()),
                                        inoutLine.GetMovementQty(), Get_Trx(), out conversionNotFoundInOut))
                                            {
                                                if (!conversionNotFoundInOut1.Contains(conversionNotFoundInOut))
                                                {
                                                    conversionNotFoundInOut1 += conversionNotFoundInOut + " , ";
                                                }
                                                _log.Info("Cost not Calculated for Material Receipt for this Line ID = " + inoutLine.GetVAM_Inv_InOutLine_ID());
                                                continue;
                                            }
                                            else
                                            {
                                                if (isUpdatePostCurrentcostPriceFromMR || inoutLine.GetCurrentCostPrice() == 0)
                                                {
                                                    // get price from VAM_ProductCost (Current Cost Price)
                                                    currentCostPrice = 0;
                                                    currentCostPrice = MVAMVAMProductCost.GetproductCostAndQtyMaterial(inoutLine.GetVAF_Client_ID(), inoutLine.GetVAF_Org_ID(),
                                                        inoutLine.GetVAM_Product_ID(), inoutLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), VAM_Warehouse_ID, false);
                                                }
                                                if (inoutLine.GetCurrentCostPrice() == 0)
                                                {
                                                    DB.ExecuteQuery("UPDATE VAM_Inv_InOutLine SET CurrentCostPrice = " + currentCostPrice + " WHERE VAM_Inv_InOutLine_ID = " + inoutLine.GetVAM_Inv_InOutLine_ID(), null, Get_Trx());
                                                }
                                                if (isUpdatePostCurrentcostPriceFromMR && inoutLine.GetPostCurrentCostPrice() == 0)
                                                {
                                                    inoutLine.SetPostCurrentCostPrice(currentCostPrice);
                                                }
                                                inoutLine.SetIsCostCalculated(true);
                                                if (!inoutLine.Save(Get_Trx()))
                                                {
                                                    ValueNamePair pp = VLogger.RetrieveError();
                                                    _log.Info("Error found for saving Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID() +
                                                               " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                    Get_Trx().Rollback();
                                                    continue;
                                                }
                                                else
                                                {
                                                    Get_Trx().Commit();
                                                }
                                            }
                                            #endregion
                                        }

                                        if (matchInvoice.Get_ColumnIndex("CurrentCostPrice") >= 0)
                                        {
                                            // get pre cost before invoice cost calculation and update on match invoice
                                            currentCostPrice = MVAMVAMProductCost.GetproductCostAndQtyMaterial(invoiceLine.GetVAF_Client_ID(), invoiceLine.GetVAF_Org_ID(),
                                                               product.GetVAM_Product_ID(), invoiceLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), VAM_Warehouse_ID, false);
                                            DB.ExecuteQuery(@"UPDATE VAM_MatchInvoice SET CurrentCostPrice =
                                                              CASE WHEN CurrentCostPrice <> 0 THEN CurrentCostPrice ELSE " + currentCostPrice +
                                                             @" END WHERE VAM_MatchInvoice_ID = " + matchInvoice.GetVAM_MatchInvoice_ID(), null, Get_Trx());
                                        }

                                        // when isCostAdjustableOnLost = true on product and movement qty on MR is less than invoice qty then consider MR qty else invoice qty
                                        if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), invoiceLine.GetVAF_Client_ID(), invoiceLine.GetVAF_Org_ID(), product, invoiceLine.GetVAM_PFeature_SetInstance_ID(),
                                              "Invoice(Vendor)", null, inoutLine, null, invoiceLine, null,
                                            isCostAdjustableOnLost && matchInvoice.GetQty() < invoiceLine.GetQtyInvoiced() ? ProductInvoiceLineCost : Decimal.Multiply(Decimal.Divide(ProductInvoiceLineCost, invoiceLine.GetQtyInvoiced()), matchInvoice.GetQty()),
                                              matchInvoice.GetQty(),
                                              Get_Trx(), out conversionNotFoundInvoice))
                                        {
                                            if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                            {
                                                conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                            }
                                            _log.Info("Cost not Calculated for Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                        }
                                        else
                                        {
                                            if (invoice.GetDescription() != null && invoice.GetDescription().Contains("{->"))
                                            {
                                                invoiceLine.SetIsReversedCostCalculated(true);
                                            }
                                            invoiceLine.SetIsCostCalculated(true);
                                            if (client.IsCostImmediate() && !invoiceLine.IsCostImmediate())
                                            {
                                                invoiceLine.SetIsCostImmediate(true);
                                            }
                                            if (!invoiceLine.Save(Get_Trx()))
                                            {
                                                ValueNamePair pp = VLogger.RetrieveError();
                                                _log.Info("Error found for saving Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID() +
                                                           " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                Get_Trx().Rollback();
                                            }
                                            else
                                            {
                                                _log.Fine("Cost Calculation updated for m_invoiceline = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                Get_Trx().Commit();

                                                if (matchInvoice.Get_ColumnIndex("PostCurrentCostPrice") >= 0)
                                                {
                                                    // get post cost after invoice cost calculation and update on match invoice
                                                    currentCostPrice = MVAMVAMProductCost.GetproductCostAndQtyMaterial(invoiceLine.GetVAF_Client_ID(), invoiceLine.GetVAF_Org_ID(),
                                                                       product.GetVAM_Product_ID(), invoiceLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), VAM_Warehouse_ID, false);
                                                    matchInvoice.SetPostCurrentCostPrice(currentCostPrice);
                                                }
                                                // set is cost calculation true on match invoice
                                                matchInvoice.SetIsCostCalculated(true);
                                                if (!matchInvoice.Save(Get_Trx()))
                                                {
                                                    ValueNamePair pp = VLogger.RetrieveError();
                                                    _log.Info("Error found for saving Invoice(Vendor) for this Line ID = " + matchInvoice.GetVAB_InvoiceLine_ID() +
                                                               " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                    Get_Trx().Rollback();
                                                }
                                                else
                                                {
                                                    Get_Trx().Commit();
                                                    // update the latest cost ON MR (Post Cost)
                                                    if (!isUpdatePostCurrentcostPriceFromMR)
                                                    {
                                                        DB.ExecuteQuery("UPDATE VAM_Inv_InOutLine SET PostCurrentCostPrice = " + matchInvoice.GetPostCurrentCostPrice() +
                                                                @" WHERE VAM_Inv_InOutLine_ID = " + inoutLine.GetVAM_Inv_InOutLine_ID(), null, Get_Trx());
                                                        Get_Trx().Commit();
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    continue;
                                }
                            }
                            catch { }
                            #endregion

                            #region Cost Calculation for Physical Inventory
                            try
                            {
                                if (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["TableName"]) == "VAM_Inventory" &&
                                    (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["docstatus"]) == "CO" ||
                                     Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["docstatus"]) == "CL") &&
                                    Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["IsInternalUse"]) == "N")
                                {
                                    inventory = new MVAMInventory(GetCtx(), Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]), Get_Trx());
                                    sql.Clear();
                                    if (inventory.GetDescription() != null && inventory.GetDescription().Contains("{->"))
                                    {
                                        sql.Append("SELECT * FROM VAM_InventoryLine WHERE IsActive = 'Y' AND iscostcalculated = 'Y'  AND IsReversedCostCalculated = 'N' " +
                                                  " AND VAM_Inventory_ID = " + inventory.GetVAM_Inventory_ID() + " ORDER BY Line ");
                                    }
                                    else
                                    {
                                        sql.Append("SELECT * FROM VAM_InventoryLine WHERE IsActive = 'Y' AND iscostcalculated = 'N' " +
                                                     " AND VAM_Inventory_ID = " + inventory.GetVAM_Inventory_ID() + " ORDER BY Line ");
                                    }
                                    dsChildRecord = DB.ExecuteDataset(sql.ToString(), null, Get_Trx());
                                    if (dsChildRecord != null && dsChildRecord.Tables.Count > 0 && dsChildRecord.Tables[0].Rows.Count > 0)
                                    {
                                        for (int j = 0; j < dsChildRecord.Tables[0].Rows.Count; j++)
                                        {
                                            product = new MVAMProduct(GetCtx(), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_Product_ID"]), Get_Trx());
                                            inventoryLine = new MVAMInventoryLine(GetCtx(), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_InventoryLine_ID"]), Get_Trx());
                                            if (product.GetProductType() == "I") // for Item Type product
                                            {
                                                quantity = 0;
                                                if (inventory.IsInternalUse())
                                                {
                                                    #region for Internal use inventory

                                                    #region get price from VAM_ProductCost (Current Cost Price)
                                                    if (!client.IsCostImmediate())
                                                    {
                                                        // get price from VAM_ProductCost (Current Cost Price)
                                                        currentCostPrice = 0;
                                                        currentCostPrice = MVAMVAMProductCost.GetproductCosts(inventoryLine.GetVAF_Client_ID(), inventoryLine.GetVAF_Org_ID(),
                                                            inventoryLine.GetVAM_Product_ID(), inventoryLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), inventory.GetVAM_Warehouse_ID());
                                                        DB.ExecuteQuery("UPDATE VAM_InventoryLine SET CurrentCostPrice = " + currentCostPrice + @"
                                                                          WHERE VAM_InventoryLine_ID = " + inventoryLine.GetVAM_InventoryLine_ID(), null, Get_Trx());
                                                    }
                                                    #endregion

                                                    quantity = Decimal.Negate(inventoryLine.GetQtyInternalUse());
                                                    if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), inventory.GetVAF_Client_ID(), inventory.GetVAF_Org_ID(), product, inventoryLine.GetVAM_PFeature_SetInstance_ID(),
                                                   "Internal Use Inventory", inventoryLine, null, null, null, null, 0, quantity, Get_Trx(), out conversionNotFoundInventory))
                                                    {
                                                        if (!conversionNotFoundInventory1.Contains(conversionNotFoundInventory))
                                                        {
                                                            conversionNotFoundInventory1 += conversionNotFoundInventory + " , ";
                                                        }
                                                        _log.Info("Cost not Calculated for Internal Use Inventory for this Line ID = " + inventoryLine.GetVAM_InventoryLine_ID());
                                                    }
                                                    else
                                                    {
                                                        if (inventory.GetDescription() != null && inventory.GetDescription().Contains("{->"))
                                                        {
                                                            inventoryLine.SetIsReversedCostCalculated(true);
                                                        }
                                                        inventoryLine.SetIsCostCalculated(true);
                                                        if (client.IsCostImmediate() && !inventoryLine.IsCostImmediate())
                                                        {
                                                            inventoryLine.SetIsCostImmediate(true);
                                                        }
                                                        // when post current cost price is ZERO, than need to update cost here 
                                                        if (inventoryLine.GetPostCurrentCostPrice() == 0)
                                                        {
                                                            currentCostPrice = MVAMVAMProductCost.GetproductCosts(inventoryLine.GetVAF_Client_ID(), inventoryLine.GetVAF_Org_ID(),
                                                              inventoryLine.GetVAM_Product_ID(), inventoryLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), inventory.GetVAM_Warehouse_ID());
                                                            inventoryLine.SetPostCurrentCostPrice(currentCostPrice);
                                                        }
                                                        if (!inventoryLine.Save(Get_Trx()))
                                                        {
                                                            ValueNamePair pp = VLogger.RetrieveError();
                                                            _log.Info("Error found for saving Internal Use Inventory for this Line ID = " + inventoryLine.GetVAM_InventoryLine_ID() +
                                                                       " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                            Get_Trx().Rollback();
                                                        }
                                                        else
                                                        {
                                                            _log.Fine("Cost Calculation updated for VAM_InventoryLine = " + inventoryLine.GetVAM_InventoryLine_ID());
                                                            Get_Trx().Commit();
                                                        }
                                                    }
                                                    #endregion
                                                }
                                                else
                                                {
                                                    #region for Physical Inventory

                                                    #region get price from VAM_ProductCost (Current Cost Price)
                                                    if (!client.IsCostImmediate() || inventoryLine.GetCurrentCostPrice() == 0)
                                                    {
                                                        // get price from VAM_ProductCost (Current Cost Price)
                                                        currentCostPrice = 0;
                                                        //if (Decimal.Subtract(inventoryLine.GetQtyCount(), inventoryLine.GetQtyBook()) < 0)
                                                        //{
                                                        // stock reduce
                                                        currentCostPrice = MVAMVAMProductCost.GetproductCosts(inventoryLine.GetVAF_Client_ID(), inventoryLine.GetVAF_Org_ID(),
                                                            inventoryLine.GetVAM_Product_ID(), inventoryLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), inventory.GetVAM_Warehouse_ID());
                                                        //}
                                                        //else
                                                        //{
                                                        //    // stock increase
                                                        //    currentCostPrice = MVAMVAMProductCost.GetproductCostAndQtyMaterial(inventoryLine.GetVAF_Client_ID(), inventoryLine.GetVAF_Org_ID(),
                                                        //    inventoryLine.GetVAM_Product_ID(), inventoryLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), inventory.GetVAM_Warehouse_ID(), false);
                                                        //}
                                                        DB.ExecuteQuery("UPDATE VAM_InventoryLine SET CurrentCostPrice = " + currentCostPrice + @"
                                                                           WHERE VAM_InventoryLine_ID = " + inventoryLine.GetVAM_InventoryLine_ID(), null, Get_Trx());
                                                    }
                                                    #endregion

                                                    quantity = Decimal.Subtract(inventoryLine.GetQtyCount(), inventoryLine.GetQtyBook());
                                                    if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), inventory.GetVAF_Client_ID(), inventory.GetVAF_Org_ID(), product, inventoryLine.GetVAM_PFeature_SetInstance_ID(),
                                                   "Physical Inventory", inventoryLine, null, null, null, null, 0, quantity, Get_Trx(), out conversionNotFoundInventory))
                                                    {
                                                        if (!conversionNotFoundInventory1.Contains(conversionNotFoundInventory))
                                                        {
                                                            conversionNotFoundInventory1 += conversionNotFoundInventory + " , ";
                                                        }
                                                        _log.Info("Cost not Calculated for Physical Inventory for this Line ID = " + inventoryLine.GetVAM_InventoryLine_ID());
                                                    }
                                                    else
                                                    {
                                                        if (inventory.GetDescription() != null && inventory.GetDescription().Contains("{->"))
                                                        {
                                                            inventoryLine.SetIsReversedCostCalculated(true);
                                                        }
                                                        inventoryLine.SetIsCostCalculated(true);
                                                        // when post current cost price is ZERO, than need to update cost here 
                                                        if (inventoryLine.GetPostCurrentCostPrice() == 0)
                                                        {
                                                            //if (Decimal.Subtract(inventoryLine.GetQtyCount(), inventoryLine.GetQtyBook()) < 0)
                                                            //{
                                                            currentCostPrice = MVAMVAMProductCost.GetproductCosts(inventoryLine.GetVAF_Client_ID(), inventoryLine.GetVAF_Org_ID(),
                                                          inventoryLine.GetVAM_Product_ID(), inventoryLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), inventory.GetVAM_Warehouse_ID());
                                                            //}
                                                            //else
                                                            //{
                                                            //    currentCostPrice = MVAMVAMProductCost.GetproductCostAndQtyMaterial(inventoryLine.GetVAF_Client_ID(), inventoryLine.GetVAF_Org_ID(),
                                                            //   inventoryLine.GetVAM_Product_ID(), inventoryLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), inventory.GetVAM_Warehouse_ID(), false);
                                                            //}
                                                            inventoryLine.SetPostCurrentCostPrice(currentCostPrice);
                                                        }
                                                        if (client.IsCostImmediate() && !inventoryLine.IsCostImmediate())
                                                        {
                                                            inventoryLine.SetIsCostImmediate(true);
                                                        }
                                                        if (!inventoryLine.Save(Get_Trx()))
                                                        {
                                                            ValueNamePair pp = VLogger.RetrieveError();
                                                            _log.Info("Error found for saving Internal Use Inventory for this Line ID = " + inventoryLine.GetVAM_InventoryLine_ID() +
                                                                       " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                            Get_Trx().Rollback();
                                                        }
                                                        else
                                                        {
                                                            _log.Fine("Cost Calculation updated for VAM_InventoryLine = " + inventoryLine.GetVAM_InventoryLine_ID());
                                                            Get_Trx().Commit();
                                                        }
                                                    }
                                                    #endregion
                                                }
                                            }
                                        }
                                    }
                                    sql.Clear();
                                    if (inventory.GetDescription() != null && inventory.GetDescription().Contains("{->"))
                                    {
                                        sql.Append("SELECT COUNT(*) FROM VAM_InventoryLine WHERE IsReversedCostCalculated = 'N' AND IsActive = 'Y' AND VAM_Inventory_ID = " + inventory.GetVAM_Inventory_ID());
                                    }
                                    else
                                    {
                                        sql.Append("SELECT COUNT(*) FROM VAM_InventoryLine WHERE IsCostCalculated = 'N' AND IsActive = 'Y' AND VAM_Inventory_ID = " + inventory.GetVAM_Inventory_ID());
                                    }
                                    if (Util.GetValueOfInt(DB.ExecuteScalar(sql.ToString(), null, Get_Trx())) <= 0)
                                    {
                                        if (inventory.GetDescription() != null && inventory.GetDescription().Contains("{->"))
                                        {
                                            inventory.SetIsReversedCostCalculated(true);
                                        }
                                        inventory.SetIsCostCalculated(true);
                                        if (!inventory.Save(Get_Trx()))
                                        {
                                            ValueNamePair pp = VLogger.RetrieveError();
                                            if (pp != null)
                                                _log.Info("Error found for saving Internal Use Inventory for this Record ID = " + inventory.GetVAM_Inventory_ID() +
                                                           " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                        }
                                        else
                                        {
                                            _log.Fine("Cost Calculation updated for VAM_Inventory = " + inventoryLine.GetVAM_Inventory_ID());
                                            Get_Trx().Commit();
                                        }
                                    }
                                    continue;
                                }
                            }
                            catch { }
                            #endregion

                            #region Cost Calculation for  Internal use inventory
                            try
                            {
                                if (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["TableName"]) == "VAM_Inventory" &&
                                    (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["docstatus"]) == "CO" ||
                                     Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["docstatus"]) == "CL") &&
                                    Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["IsInternalUse"]) == "Y")
                                {
                                    inventory = new MVAMInventory(GetCtx(), Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]), Get_Trx());
                                    sql.Clear();
                                    if (inventory.GetDescription() != null && inventory.GetDescription().Contains("{->"))
                                    {
                                        sql.Append("SELECT * FROM VAM_InventoryLine WHERE IsActive = 'Y' AND iscostcalculated = 'Y'  AND IsReversedCostCalculated = 'N' " +
                                                  " AND VAM_Inventory_ID = " + inventory.GetVAM_Inventory_ID() + " ORDER BY Line ");
                                    }
                                    else
                                    {
                                        sql.Append("SELECT * FROM VAM_InventoryLine WHERE IsActive = 'Y' AND iscostcalculated = 'N' " +
                                                     " AND VAM_Inventory_ID = " + inventory.GetVAM_Inventory_ID() + " ORDER BY Line ");
                                    }
                                    dsChildRecord = DB.ExecuteDataset(sql.ToString(), null, Get_Trx());
                                    if (dsChildRecord != null && dsChildRecord.Tables.Count > 0 && dsChildRecord.Tables[0].Rows.Count > 0)
                                    {
                                        for (int j = 0; j < dsChildRecord.Tables[0].Rows.Count; j++)
                                        {
                                            product = new MVAMProduct(GetCtx(), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_Product_ID"]), Get_Trx());
                                            inventoryLine = new MVAMInventoryLine(GetCtx(), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_InventoryLine_ID"]), Get_Trx());
                                            if (product.GetProductType() == "I") // for Item Type product
                                            {
                                                quantity = 0;
                                                if (inventory.IsInternalUse())
                                                {
                                                    #region for Internal use inventory

                                                    #region get price from VAM_ProductCost (Current Cost Price)
                                                    if (!client.IsCostImmediate() || inventoryLine.GetCurrentCostPrice() == 0)
                                                    {
                                                        // get price from VAM_ProductCost (Current Cost Price)
                                                        currentCostPrice = 0;
                                                        currentCostPrice = MVAMVAMProductCost.GetproductCosts(inventoryLine.GetVAF_Client_ID(), inventoryLine.GetVAF_Org_ID(),
                                                            inventoryLine.GetVAM_Product_ID(), inventoryLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), inventory.GetVAM_Warehouse_ID());
                                                        DB.ExecuteQuery("UPDATE VAM_InventoryLine SET CurrentCostPrice = " + currentCostPrice + @"
                                                                         WHERE VAM_InventoryLine_ID = " + inventoryLine.GetVAM_InventoryLine_ID(), null, Get_Trx());
                                                    }
                                                    #endregion

                                                    quantity = Decimal.Negate(inventoryLine.GetQtyInternalUse());
                                                    if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), inventory.GetVAF_Client_ID(), inventory.GetVAF_Org_ID(), product, inventoryLine.GetVAM_PFeature_SetInstance_ID(),
                                                   "Internal Use Inventory", inventoryLine, null, null, null, null, 0, quantity, Get_Trx(), out conversionNotFoundInventory))
                                                    {
                                                        if (!conversionNotFoundInventory1.Contains(conversionNotFoundInventory))
                                                        {
                                                            conversionNotFoundInventory1 += conversionNotFoundInventory + " , ";
                                                        }
                                                        _log.Info("Cost not Calculated for Internal Use Inventory for this Line ID = " + inventoryLine.GetVAM_InventoryLine_ID());
                                                    }
                                                    else
                                                    {
                                                        // when post current cost price is ZERO, than need to update cost here 
                                                        if (inventoryLine.GetPostCurrentCostPrice() == 0)
                                                        {
                                                            currentCostPrice = MVAMVAMProductCost.GetproductCosts(inventoryLine.GetVAF_Client_ID(), inventoryLine.GetVAF_Org_ID(),
                                                              inventoryLine.GetVAM_Product_ID(), inventoryLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), inventory.GetVAM_Warehouse_ID());
                                                            inventoryLine.SetPostCurrentCostPrice(currentCostPrice);
                                                        }
                                                        if (inventory.GetDescription() != null && inventory.GetDescription().Contains("{->"))
                                                        {
                                                            inventoryLine.SetIsReversedCostCalculated(true);
                                                        }
                                                        inventoryLine.SetIsCostCalculated(true);
                                                        if (client.IsCostImmediate() && !inventoryLine.IsCostImmediate())
                                                        {
                                                            inventoryLine.SetIsCostImmediate(true);
                                                        }
                                                        if (!inventoryLine.Save(Get_Trx()))
                                                        {
                                                            ValueNamePair pp = VLogger.RetrieveError();
                                                            _log.Info("Error found for saving Internal Use Inventory for this Line ID = " + inventoryLine.GetVAM_InventoryLine_ID() +
                                                                       " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                            Get_Trx().Rollback();
                                                        }
                                                        else
                                                        {
                                                            _log.Fine("Cost Calculation updated for VAM_InventoryLine = " + inventoryLine.GetVAM_InventoryLine_ID());
                                                            Get_Trx().Commit();
                                                        }
                                                    }
                                                    #endregion
                                                }
                                                else
                                                {
                                                    #region for Physical Inventory

                                                    #region get price from VAM_ProductCost (Current Cost Price)
                                                    if (!client.IsCostImmediate())
                                                    {
                                                        // get price from VAM_ProductCost (Current Cost Price)
                                                        currentCostPrice = 0;
                                                        currentCostPrice = MVAMVAMProductCost.GetproductCosts(inventoryLine.GetVAF_Client_ID(), inventoryLine.GetVAF_Org_ID(),
                                                            inventoryLine.GetVAM_Product_ID(), inventoryLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), inventory.GetVAM_Warehouse_ID());
                                                        DB.ExecuteQuery("UPDATE VAM_InventoryLine SET CurrentCostPrice = " + currentCostPrice + @"
                                                                         WHERE VAM_InventoryLine_ID = " + inventoryLine.GetVAM_InventoryLine_ID(), null, Get_Trx());
                                                    }
                                                    #endregion


                                                    quantity = Decimal.Subtract(inventoryLine.GetQtyCount(), inventoryLine.GetQtyBook());
                                                    if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), inventory.GetVAF_Client_ID(), inventory.GetVAF_Org_ID(), product, inventoryLine.GetVAM_PFeature_SetInstance_ID(),
                                                   "Physical Inventory", inventoryLine, null, null, null, null, 0, quantity, Get_Trx(), out conversionNotFoundInventory))
                                                    {
                                                        if (!conversionNotFoundInventory1.Contains(conversionNotFoundInventory))
                                                        {
                                                            conversionNotFoundInventory1 += conversionNotFoundInventory + " , ";
                                                        }
                                                        _log.Info("Cost not Calculated for Physical Inventory for this Line ID = " + inventoryLine.GetVAM_InventoryLine_ID());
                                                    }
                                                    else
                                                    {
                                                        if (inventory.GetDescription() != null && inventory.GetDescription().Contains("{->"))
                                                        {
                                                            inventoryLine.SetIsReversedCostCalculated(true);
                                                        }
                                                        inventoryLine.SetIsCostCalculated(true);
                                                        if (client.IsCostImmediate() && !inventoryLine.IsCostImmediate())
                                                        {
                                                            inventoryLine.SetIsCostImmediate(true);
                                                        }
                                                        // when post current cost price is ZERO, than need to update cost here 
                                                        if (inventoryLine.GetPostCurrentCostPrice() == 0)
                                                        {
                                                            currentCostPrice = MVAMVAMProductCost.GetproductCosts(inventoryLine.GetVAF_Client_ID(), inventoryLine.GetVAF_Org_ID(),
                                                              inventoryLine.GetVAM_Product_ID(), inventoryLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), inventory.GetVAM_Warehouse_ID());
                                                            inventoryLine.SetPostCurrentCostPrice(currentCostPrice);
                                                        }
                                                        if (!inventoryLine.Save(Get_Trx()))
                                                        {
                                                            ValueNamePair pp = VLogger.RetrieveError();
                                                            _log.Info("Error found for saving Internal Use Inventory for this Line ID = " + inventoryLine.GetVAM_InventoryLine_ID() +
                                                                       " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                            Get_Trx().Rollback();
                                                        }
                                                        else
                                                        {
                                                            _log.Fine("Cost Calculation updated for VAM_InventoryLine = " + inventoryLine.GetVAM_InventoryLine_ID());
                                                            Get_Trx().Commit();
                                                        }
                                                    }
                                                    #endregion
                                                }
                                            }
                                        }
                                    }
                                    sql.Clear();
                                    if (inventory.GetDescription() != null && inventory.GetDescription().Contains("{->"))
                                    {
                                        sql.Append("SELECT COUNT(*) FROM VAM_InventoryLine WHERE IsReversedCostCalculated = 'N' AND IsActive = 'Y' AND VAM_Inventory_ID = " + inventory.GetVAM_Inventory_ID());
                                    }
                                    else
                                    {
                                        sql.Append("SELECT COUNT(*) FROM VAM_InventoryLine WHERE IsCostCalculated = 'N' AND IsActive = 'Y' AND VAM_Inventory_ID = " + inventory.GetVAM_Inventory_ID());
                                    }
                                    if (Util.GetValueOfInt(DB.ExecuteScalar(sql.ToString(), null, Get_Trx())) <= 0)
                                    {
                                        if (inventory.GetDescription() != null && inventory.GetDescription().Contains("{->"))
                                        {
                                            inventory.SetIsReversedCostCalculated(true);
                                        }
                                        inventory.SetIsCostCalculated(true);
                                        if (!inventory.Save(Get_Trx()))
                                        {
                                            ValueNamePair pp = VLogger.RetrieveError();
                                            if (pp != null)
                                                _log.Info("Error found for saving Internal Use Inventory for this Record ID = " + inventory.GetVAM_Inventory_ID() +
                                                           " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                        }
                                        else
                                        {
                                            _log.Fine("Cost Calculation updated for VAM_Inventory = " + inventory.GetVAM_Inventory_ID());
                                            Get_Trx().Commit();
                                        }
                                    }
                                    continue;
                                }
                            }
                            catch { }
                            #endregion

                            #region Cost Calculation for Asset Disposal
                            try
                            {
                                if (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["TableName"]).Equals("VAFAM_AssetDisposal"))

                                {
                                    po_AssetDisposal = tbl_AssetDisposal.GetPO(GetCtx(), Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]), Get_Trx());
                                    product = new MVAMProduct(GetCtx(), Util.GetValueOfInt(po_AssetDisposal.Get_Value("VAM_Product_ID")), Get_Trx());
                                    if (product.GetProductType() == "I") // for Item Type product
                                    {
                                        quantity = 0;
                                        quantity = Decimal.Negate(Util.GetValueOfDecimal(po_AssetDisposal.Get_Value("VAFAM_Qty")));
                                        if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), Util.GetValueOfInt(po_AssetDisposal.Get_Value("VAF_Client_ID")), Util.GetValueOfInt(po_AssetDisposal.Get_Value("VAF_Org_ID")),
                                            product, Util.GetValueOfInt(po_AssetDisposal.Get_Value("GetVAM_PFeature_SetInstance_ID")), "AssetDisposal", null, null, null, null, po_AssetDisposal, 0, quantity, Get_Trx(), out conversionNotFoundInventory))
                                        {
                                            _log.Info("Cost not Calculated for AssetDisposal_ID = " + Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]));
                                        }
                                        else
                                        {
                                            if (Util.GetValueOfInt(po_AssetDisposal.Get_Value("ReversalDoc_ID")) > 0)
                                            {
                                                DB.ExecuteQuery("UPDATE VAFAM_AssetDisposal SET IsReversedCostCalculated='Y' WHERE VAFAM_AssetDisposal_ID = " + Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]), null, Get_Trx());
                                            }
                                            else
                                            {
                                                DB.ExecuteQuery("UPDATE VAFAM_AssetDisposal SET ISCostCalculated='Y' WHERE VAFAM_AssetDisposal_ID = " + Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]), null, Get_Trx());
                                            }
                                            _log.Fine("Cost Calculation updated for VAFAM_AssetDispoal= " + Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]));
                                            Get_Trx().Commit();
                                        }
                                    }
                                }
                            }
                            catch { }
                            #endregion


                            #region Cost Calculation for Inventory Move
                            try
                            {
                                if (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["TableName"]) == "VAM_InventoryTransfer" &&
                                   (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["docstatus"]) == "CO" ||
                                    Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["docstatus"]) == "CL"))
                                {
                                    movement = new MVAMInventoryTransfer(GetCtx(), Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]), Get_Trx());

                                    sql.Clear();
                                    if (movement.GetDescription() != null && movement.GetDescription().Contains("{->"))
                                    {
                                        sql.Append("SELECT * FROM VAM_InvTrf_Line WHERE IsActive = 'Y' AND iscostcalculated = 'Y'  AND IsReversedCostCalculated = 'N' " +
                                                  " AND VAM_InventoryTransfer_ID = " + movement.GetVAM_InventoryTransfer_ID() + " ORDER BY Line ");
                                    }
                                    else
                                    {
                                        sql.Append("SELECT * FROM VAM_InvTrf_Line WHERE IsActive = 'Y' AND iscostcalculated = 'N' " +
                                                     " AND VAM_InventoryTransfer_ID = " + movement.GetVAM_InventoryTransfer_ID() + " ORDER BY Line ");
                                    }
                                    dsChildRecord = DB.ExecuteDataset(sql.ToString(), null, Get_Trx());
                                    if (dsChildRecord != null && dsChildRecord.Tables.Count > 0 && dsChildRecord.Tables[0].Rows.Count > 0)
                                    {
                                        for (int j = 0; j < dsChildRecord.Tables[0].Rows.Count; j++)
                                        {
                                            product = new MVAMProduct(GetCtx(), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_Product_ID"]), Get_Trx());
                                            movementLine = new MVAMInvTrfLine(GetCtx(), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_InvTrf_Line_ID"]), Get_Trx());
                                            locatorTo = MVAMLocator.Get(GetCtx(), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_LocatorTo_ID"]));

                                            #region get price from VAM_ProductCost (Current Cost Price)
                                            if (!client.IsCostImmediate() || movementLine.GetCurrentCostPrice() == 0 || movementLine.GetToCurrentCostPrice() == 0)
                                            {
                                                // get price from VAM_ProductCost (Current Cost Price)
                                                currentCostPrice = MVAMVAMProductCost.GetproductCosts(movementLine.GetVAF_Client_ID(), movementLine.GetVAF_Org_ID(),
                                                    movementLine.GetVAM_Product_ID(), movementLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), movement.GetDTD001_MWarehouseSource_ID());

                                                // For To Warehouse
                                                toCurrentCostPrice = MVAMVAMProductCost.GetproductCosts(movementLine.GetVAF_Client_ID(), locatorTo.GetVAF_Org_ID(),
                                                   movementLine.GetVAM_Product_ID(), movementLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), locatorTo.GetVAM_Warehouse_ID());

                                                DB.ExecuteQuery("UPDATE VAM_InvTrf_Line SET  CurrentCostPrice = CASE WHEN CurrentCostPrice <> 0 THEN CurrentCostPrice ELSE " + currentCostPrice +
                                                    @" END , ToCurrentCostPrice = CASE WHEN ToCurrentCostPrice <> 0 THEN ToCurrentCostPrice ELSE " + toCurrentCostPrice + @"
                                               END  WHERE VAM_InvTrf_Line_ID = " + movementLine.GetVAM_InvTrf_Line_ID(), null, Get_Trx());
                                            }
                                            #endregion
                                            // for Item Type product 
                                            if (product.GetProductType() == "I") // && movement.GetVAF_Org_ID() != warehouse.GetVAF_Org_ID()
                                            {
                                                #region for inventory move
                                                if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), movement.GetVAF_Client_ID(), movement.GetVAF_Org_ID(), product, movementLine.GetVAM_PFeature_SetInstance_ID(),
                                                    "Inventory Move", null, null, movementLine, null, null, 0, movementLine.GetMovementQty(), Get_Trx(), out conversionNotFoundMovement))
                                                {
                                                    if (!conversionNotFoundMovement1.Contains(conversionNotFoundMovement))
                                                    {
                                                        conversionNotFoundMovement1 += conversionNotFoundMovement + " , ";
                                                    }
                                                    _log.Info("Cost not Calculated for Inventory Move for this Line ID = " + movementLine.GetVAM_InvTrf_Line_ID());
                                                }
                                                else
                                                {
                                                    if (movementLine.GetPostCurrentCostPrice() == 0)
                                                    {
                                                        // get price from VAM_ProductCost (Current Cost Price)
                                                        currentCostPrice = 0;
                                                        currentCostPrice = MVAMVAMProductCost.GetproductCosts(movementLine.GetVAF_Client_ID(), movementLine.GetVAF_Org_ID(),
                                                            movementLine.GetVAM_Product_ID(), movementLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), movement.GetDTD001_MWarehouseSource_ID());
                                                        movementLine.SetPostCurrentCostPrice(currentCostPrice);
                                                    }
                                                    if (movementLine.GetToPostCurrentCostPrice() == 0)
                                                    {
                                                        // For To Warehouse
                                                        toCurrentCostPrice = MVAMVAMProductCost.GetproductCosts(movementLine.GetVAF_Client_ID(), locatorTo.GetVAF_Org_ID(),
                                                           movementLine.GetVAM_Product_ID(), movementLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), locatorTo.GetVAM_Warehouse_ID());
                                                        movementLine.SetToPostCurrentCostPrice(toCurrentCostPrice);
                                                    }
                                                    if (movement.GetDescription() != null && movement.GetDescription().Contains("{->"))
                                                    {
                                                        movementLine.SetIsReversedCostCalculated(true);
                                                    }
                                                    movementLine.SetIsCostCalculated(true);
                                                    if (client.IsCostImmediate() && !movementLine.IsCostImmediate())
                                                    {
                                                        movementLine.SetIsCostImmediate(true);
                                                    }
                                                    if (!movementLine.Save(Get_Trx()))
                                                    {
                                                        ValueNamePair pp = VLogger.RetrieveError();
                                                        _log.Info("Error found for saving Inventory Move for this Line ID = " + movementLine.GetVAM_InvTrf_Line_ID() +
                                                                   " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                        Get_Trx().Rollback();
                                                    }
                                                    else
                                                    {
                                                        _log.Fine("Cost Calculation updated for VAM_InvTrf_Line = " + movementLine.GetVAM_InvTrf_Line_ID());
                                                        Get_Trx().Commit();
                                                    }
                                                }
                                                #endregion
                                            }
                                        }
                                    }
                                    sql.Clear();
                                    if (movement.GetDescription() != null && movement.GetDescription().Contains("{->"))
                                    {
                                        sql.Append("SELECT COUNT(*) FROM VAM_InvTrf_Line WHERE IsReversedCostCalculated = 'N' AND IsActive = 'Y' AND VAM_InventoryTransfer_ID = " + movement.GetVAM_InventoryTransfer_ID());
                                    }
                                    else
                                    {
                                        sql.Append("SELECT COUNT(*) FROM VAM_InvTrf_Line WHERE IsCostCalculated = 'N' AND IsActive = 'Y' AND VAM_InventoryTransfer_ID = " + movement.GetVAM_InventoryTransfer_ID());
                                    }
                                    if (Util.GetValueOfInt(DB.ExecuteScalar(sql.ToString(), null, Get_Trx())) <= 0)
                                    {
                                        if (movement.GetDescription() != null && movement.GetDescription().Contains("{->"))
                                        {
                                            movement.SetIsReversedCostCalculated(true);
                                        }
                                        movement.SetIsCostCalculated(true);
                                        if (!movement.Save(Get_Trx()))
                                        {
                                            ValueNamePair pp = VLogger.RetrieveError();
                                            _log.Info("Error found for saving Inventory Move for this Record ID = " + movement.GetVAM_InventoryTransfer_ID() +
                                                       " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                        }
                                        else
                                        {
                                            _log.Fine("Cost Calculation updated for VAM_InventoryTransfer = " + movement.GetVAM_InventoryTransfer_ID());
                                            Get_Trx().Commit();
                                        }
                                    }
                                    continue;
                                }
                            }
                            catch { }
                            #endregion

                            #region Cost Calculation for Production
                            try
                            {
                                if (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["TableName"]) == "VAM_Production")
                                {
                                    #region calculate/update cost of components (Here IsSotrx means IsReversed --> on production header)
                                    if (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["issotrx"]).Equals("N"))
                                    {
                                        IDbConnection dbConnection = Get_Trx().GetConnection();
                                        if (dbConnection != null)
                                        {
                                            // execute procedure for updating cost of components
                                            //OracleCommand cmd = (OracleCommand)dbConnection.CreateCommand();
                                            //cmd.CommandType = CommandType.StoredProcedure;
                                            //cmd.Connection = (OracleConnection)dbConnection;
                                            //cmd.CommandText = "UpdateProductionLineWithCost";
                                            //cmd.Parameters.Add("p_Record_Id", Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]));

                                            //cmd.ExecuteNonQuery();
                                        }
                                    }
                                    #endregion

                                    // count -> is there any record having cost not available on production line except finished good
                                    // if not found, then we will calculate cost of finished good else not.
                                    CountCostNotAvialable = 1;
                                    CountCostNotAvialable = Util.GetValueOfInt(DB.ExecuteScalar(@"SELECT COUNT(*) FROM VAM_ProductionLine WHERE NVL(amt ,0) = 0  AND isactive = 'Y' AND VAM_Product_id NOT IN
                                                         (SELECT VAM_Product_ID FROM VAM_ProductionPlan WHERE VAM_Production_id = "
                                                          + Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]) + @"  AND isactive = 'Y' )
                                                        AND VAM_Production_id = " + Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]), null, Get_Trx()));

                                    if (CountCostNotAvialable == 0 || countGOM01 > 0)
                                    {
                                        // get record from production line based on production id
                                        sql.Clear();
                                        sql.Append(@"SELECT pl.VAM_ProductionLine_ID, pl.VAF_Client_ID, pl.VAF_Org_ID, p.MovementDate,  pl.VAM_Product_ID, 
                                                        pl.VAM_PFeature_SetInstance_ID, pl.MovementQty, pl.VAM_Locator_ID, wh.IsDisallowNegativeInv,  pl.VAM_Warehouse_ID ,
                                                        p.IsCostCalculated, p.IsReversedCostCalculated,  p.IsReversed
                                                FROM VAM_Production p INNER JOIN VAM_ProductionPlan pp  ON pp.VAM_Production_id = pp.VAM_Production_id
                                                     INNER JOIN VAM_ProductionLine pl ON pl.VAM_ProductionPlan_id = pp.VAM_ProductionPlan_id
                                                     INNER JOIN VAM_Product prod  ON pl.VAM_Product_id = prod.VAM_Product_id
                                                     INNER JOIN VAM_Locator loc ON loc.VAM_Locator_id = pl.VAM_Locator_id
                                                     INNER JOIN VAM_Warehouse wh ON loc.VAM_Warehouse_id     = wh.VAM_Warehouse_id
                                                WHERE p.VAM_Production_ID   =pp.VAM_Production_ID AND pp.VAM_ProductionPlan_ID=pl.VAM_ProductionPlan_ID
                                                      AND pp.VAM_Production_ID    =" + Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]) + @"
                                                      AND pl.VAM_Product_ID = prod.VAM_Product_ID AND prod.ProductType ='I' 
                                                      AND pl.VAM_Locator_ID = loc.VAM_Locator_ID AND loc.VAM_Warehouse_ID    = wh.VAM_Warehouse_ID
                                                ORDER BY pp.Line,  pl.Line");
                                        dsChildRecord = DB.ExecuteDataset(sql.ToString(), null, Get_Trx());
                                        if (dsChildRecord != null && dsChildRecord.Tables.Count > 0 && dsChildRecord.Tables[0].Rows.Count > 0)
                                        {
                                            for (int j = 0; j < dsChildRecord.Tables[0].Rows.Count; j++)
                                            {
                                                #region Create & Open connection and Execute Procedure
                                                try
                                                {
                                                    IDbConnection dbConnection = Get_Trx().GetConnection();
                                                    if (dbConnection != null)
                                                    {
                                                        // execute procedure for calculating cost
                                                        //OracleCommand cmd = (OracleCommand)dbConnection.CreateCommand();
                                                        //cmd.CommandType = CommandType.StoredProcedure;
                                                        //cmd.Connection = (OracleConnection)dbConnection;
                                                        //cmd.CommandText = "createcostqueueNotFRPT";
                                                        //cmd.Parameters.Add("p_VAM_Product_ID", Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_Product_ID"]));
                                                        //cmd.Parameters.Add("p_VAM_PFeature_SetInstance_ID", Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_PFeature_SetInstance_ID"]));
                                                        //cmd.Parameters.Add("p_VAF_Org_ID", Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAF_Org_ID"]));
                                                        //cmd.Parameters.Add("p_VAF_Client_ID", Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAF_Client_ID"]));
                                                        //cmd.Parameters.Add("p_Quantity", Util.GetValueOfDecimal(dsChildRecord.Tables[0].Rows[j]["MovementQty"]));
                                                        //cmd.Parameters.Add("p_VAM_ProductionLine_ID", Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_ProductionLine_ID"]));
                                                        //cmd.Parameters.Add("p_VAM_Warehouse_ID", Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_Warehouse_ID"]));
                                                        //cmd.ExecuteNonQuery();

                                                        // update prodution header 
                                                        if (Util.GetValueOfString(dsChildRecord.Tables[0].Rows[j]["IsCostCalculated"]).Equals("N"))
                                                            DB.ExecuteQuery("UPDATE VAM_Production SET IsCostCalculated='Y' WHERE VAM_Production_ID= " + Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]), null, Get_Trx());

                                                        if (Util.GetValueOfString(dsChildRecord.Tables[0].Rows[j]["IsCostCalculated"]).Equals("Y") &&
                                                            !Util.GetValueOfString(dsChildRecord.Tables[0].Rows[j]["IsReversedCostCalculated"]).Equals("N") && Util.GetValueOfString(dsChildRecord.Tables[0].Rows[j]["IsReversed"]).Equals("Y"))
                                                            DB.ExecuteQuery("UPDATE VAM_Production SET IsReversedCostCalculated='Y' WHERE VAM_Production_ID= " + Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]), null, Get_Trx());
                                                    }
                                                }
                                                catch
                                                {
                                                    Get_Trx().Rollback();
                                                }
                                                #endregion
                                            }
                                        }
                                    }
                                    Get_Trx().Commit();
                                    continue;
                                }
                            }
                            catch
                            {
                                Get_Trx().Rollback();
                            }
                            #endregion

                            #region Cost Calculation For  Return to Vendor
                            try
                            {
                                if (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["TableName"]) == "VAM_Inv_InOut" &&
                                    Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["issotrx"]) == "N" &&
                                    Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["isreturntrx"]) == "Y" &&
                                    (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["docstatus"]) == "CO" ||
                                     Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["docstatus"]) == "CL"))
                                {
                                    inout = new MVAMInvInOut(GetCtx(), Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]), Get_Trx());

                                    sql.Clear();
                                    if (inout.GetDescription() != null && inout.GetDescription().Contains("{->"))
                                    {
                                        sql.Append("SELECT * FROM VAM_Inv_InOutLine WHERE IsActive = 'Y' AND iscostcalculated = 'Y'  AND IsReversedCostCalculated = 'N' " +
                                                  " AND VAM_Inv_InOut_ID = " + inout.GetVAM_Inv_InOut_ID() + " ORDER BY Line ");
                                    }
                                    else
                                    {
                                        sql.Append("SELECT * FROM VAM_Inv_InOutLine WHERE IsActive = 'Y' AND iscostcalculated = 'N' " +
                                                     " AND VAM_Inv_InOut_ID = " + inout.GetVAM_Inv_InOut_ID() + " ORDER BY Line ");
                                    }
                                    dsChildRecord = DB.ExecuteDataset(sql.ToString(), null, Get_Trx());
                                    if (dsChildRecord != null && dsChildRecord.Tables.Count > 0 && dsChildRecord.Tables[0].Rows.Count > 0)
                                    {
                                        for (int j = 0; j < dsChildRecord.Tables[0].Rows.Count; j++)
                                        {
                                            try
                                            {
                                                inoutLine = new MVAMInvInOutLine(GetCtx(), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_Inv_InOutLine_ID"]), Get_Trx());
                                                orderLine = new MVABOrderLine(GetCtx(), inoutLine.GetVAB_OrderLine_ID(), null);
                                                if (orderLine != null && orderLine.GetVAB_Order_ID() > 0)
                                                {
                                                    order = new MVABOrder(GetCtx(), orderLine.GetVAB_Order_ID(), null);
                                                    if (order.GetDocStatus() != "VO")
                                                    {
                                                        if (orderLine != null && orderLine.GetVAB_Order_ID() > 0 && orderLine.GetQtyOrdered() == 0)
                                                            continue;
                                                    }
                                                }
                                                product = new MVAMProduct(GetCtx(), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_Product_ID"]), Get_Trx());
                                                if (product.GetProductType() == "I") // for Item Type product
                                                {
                                                    #region  Return To Vendor
                                                    if (!inout.IsSOTrx() && inout.IsReturnTrx())
                                                    {
                                                        if (inout.GetOrig_Order_ID() == 0 || orderLine == null || orderLine.GetVAB_OrderLine_ID() == 0)
                                                        {
                                                            #region Return to Vendor against without Vendor RMA

                                                            #region get price from VAM_ProductCost (Current Cost Price)
                                                            if (!client.IsCostImmediate() || inoutLine.GetCurrentCostPrice() == 0)
                                                            {
                                                                // get price from VAM_ProductCost (Current Cost Price)
                                                                currentCostPrice = 0;
                                                                currentCostPrice = MVAMVAMProductCost.GetproductCosts(inoutLine.GetVAF_Client_ID(), inoutLine.GetVAF_Org_ID(),
                                                                    inoutLine.GetVAM_Product_ID(), inoutLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), inout.GetVAM_Warehouse_ID());
                                                                DB.ExecuteQuery("UPDATE VAM_Inv_InOutLine SET CurrentCostPrice = " + currentCostPrice +
                                                                        @" WHERE VAM_Inv_InOutLine_ID = " + inoutLine.GetVAM_Inv_InOutLine_ID(), null, Get_Trx());
                                                            }
                                                            #endregion

                                                            if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), inout.GetVAF_Client_ID(), inout.GetVAF_Org_ID(), product, inoutLine.GetVAM_PFeature_SetInstance_ID(),
                                                           "Return To Vendor", null, inoutLine, null, null, null, 0, Decimal.Negate(inoutLine.GetMovementQty()), Get_TrxName(), out conversionNotFoundInOut))
                                                            {
                                                                if (!conversionNotFoundInOut1.Contains(conversionNotFoundInOut))
                                                                {
                                                                    conversionNotFoundInOut1 += conversionNotFoundInOut + " , ";
                                                                }
                                                                _log.Info("Cost not Calculated for Return To Vendor for this Line ID = " + inoutLine.GetVAM_Inv_InOutLine_ID());
                                                            }
                                                            else
                                                            {
                                                                if (inoutLine.GetCurrentCostPrice() == 0)
                                                                {
                                                                    // get price from VAM_ProductCost (Current Cost Price)
                                                                    currentCostPrice = 0;
                                                                    currentCostPrice = MVAMVAMProductCost.GetproductCosts(inoutLine.GetVAF_Client_ID(), inoutLine.GetVAF_Org_ID(),
                                                                        inoutLine.GetVAM_Product_ID(), inoutLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), inout.GetVAM_Warehouse_ID());
                                                                    inoutLine.SetCurrentCostPrice(currentCostPrice);
                                                                }
                                                                if (inout.GetDescription() != null && inout.GetDescription().Contains("{->"))
                                                                {
                                                                    inoutLine.SetIsReversedCostCalculated(true);
                                                                }
                                                                inoutLine.SetIsCostCalculated(true);
                                                                if (client.IsCostImmediate() && !inoutLine.IsCostImmediate())
                                                                {
                                                                    inoutLine.SetIsCostImmediate(true);
                                                                }
                                                                if (!inoutLine.Save(Get_Trx()))
                                                                {
                                                                    ValueNamePair pp = VLogger.RetrieveError();
                                                                    _log.Info("Error found for Return To Vendor for this Line ID = " + inoutLine.GetVAM_Inv_InOutLine_ID() +
                                                                               " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                    Get_Trx().Rollback();
                                                                }
                                                                else
                                                                {
                                                                    _log.Fine("Cost Calculation updated for VAM_Inv_InOutLine = " + inoutLine.GetVAM_Inv_InOutLine_ID());
                                                                    Get_Trx().Commit();
                                                                }
                                                            }
                                                            #endregion
                                                        }
                                                        else
                                                        {
                                                            #region Return to Vendor against with Vendor RMA
                                                            ProductOrderLineCost = orderLine.GetProductLineCost(orderLine);
                                                            ProductOrderPriceActual = ProductOrderLineCost / orderLine.GetQtyEntered();
                                                            amt = 0;
                                                            if (isCostAdjustableOnLost && inoutLine.GetMovementQty() < orderLine.GetQtyOrdered() && order.GetDocStatus() != "VO")
                                                            {
                                                                if (inoutLine.GetMovementQty() < 0)
                                                                    amt = ProductOrderLineCost;
                                                                else
                                                                    amt = Decimal.Negate(ProductOrderLineCost);
                                                            }
                                                            else if (!isCostAdjustableOnLost && inoutLine.GetMovementQty() < orderLine.GetQtyOrdered() && order.GetDocStatus() != "VO")
                                                            {
                                                                amt = Decimal.Multiply(Decimal.Divide(ProductOrderLineCost, orderLine.GetQtyOrdered()), Decimal.Negate(inoutLine.GetMovementQty()));
                                                            }
                                                            else if (order.GetDocStatus() != "VO")
                                                            {
                                                                amt = Decimal.Multiply(Decimal.Divide(ProductOrderLineCost, orderLine.GetQtyOrdered()), Decimal.Negate(inoutLine.GetMovementQty()));
                                                            }
                                                            else if (order.GetDocStatus() == "VO")
                                                            {
                                                                amt = Decimal.Multiply(ProductOrderPriceActual, Decimal.Negate(inoutLine.GetQtyEntered()));
                                                            }

                                                            if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), inout.GetVAF_Client_ID(), inout.GetVAF_Org_ID(), product, inoutLine.GetVAM_PFeature_SetInstance_ID(),
                                                                "Return To Vendor", null, inoutLine, null, null, null, amt,
                                                                Decimal.Negate(inoutLine.GetMovementQty()),
                                                                Get_TrxName(), out conversionNotFoundInOut))
                                                            {
                                                                if (!conversionNotFoundInOut1.Contains(conversionNotFoundInOut))
                                                                {
                                                                    conversionNotFoundInOut1 += conversionNotFoundInOut + " , ";
                                                                }
                                                                _log.Info("Cost not Calculated for Return To Vendor for this Line ID = " + inoutLine.GetVAM_Inv_InOutLine_ID());
                                                            }
                                                            else
                                                            {
                                                                if (inout.GetDescription() != null && inout.GetDescription().Contains("{->"))
                                                                {
                                                                    inoutLine.SetIsReversedCostCalculated(true);
                                                                }
                                                                inoutLine.SetIsCostCalculated(true);
                                                                if (client.IsCostImmediate() && !inoutLine.IsCostImmediate())
                                                                {
                                                                    inoutLine.SetIsCostImmediate(true);
                                                                }
                                                                if (!inoutLine.Save(Get_Trx()))
                                                                {
                                                                    ValueNamePair pp = VLogger.RetrieveError();
                                                                    _log.Info("Error found for Return To Vendor for this Line ID = " + inoutLine.GetVAM_Inv_InOutLine_ID() +
                                                                               " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                    Get_Trx().Rollback();
                                                                }
                                                                else
                                                                {
                                                                    _log.Fine("Cost Calculation updated for VAM_Inv_InOutLine = " + inoutLine.GetVAM_Inv_InOutLine_ID());
                                                                    Get_Trx().Commit();
                                                                }
                                                            }
                                                            #endregion
                                                        }
                                                    }
                                                    #endregion
                                                }
                                            }
                                            catch { }
                                        }
                                    }
                                    sql.Clear();
                                    if (inout.GetDescription() != null && inout.GetDescription().Contains("{->"))
                                    {
                                        sql.Append("SELECT COUNT(*) FROM VAM_Inv_InOutLine WHERE IsReversedCostCalculated = 'N' AND IsActive = 'Y' AND VAM_Inv_InOut_ID = " + inout.GetVAM_Inv_InOut_ID());
                                    }
                                    else
                                    {
                                        sql.Append("SELECT COUNT(*) FROM VAM_Inv_InOutLine WHERE IsCostCalculated = 'N' AND IsActive = 'Y' AND VAM_Inv_InOut_ID = " + inout.GetVAM_Inv_InOut_ID());
                                    }
                                    if (Util.GetValueOfInt(DB.ExecuteScalar(sql.ToString(), null, Get_Trx())) <= 0)
                                    {
                                        if (inout.GetDescription() != null && inout.GetDescription().Contains("{->"))
                                        {
                                            inout.SetIsReversedCostCalculated(true);
                                        }
                                        inout.SetIsCostCalculated(true);
                                        if (!inout.Save(Get_Trx()))
                                        {
                                            ValueNamePair pp = VLogger.RetrieveError();
                                            _log.Info("Error found for saving VAM_Inv_InOut for this Record ID = " + inout.GetVAM_Inv_InOut_ID() +
                                                       " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                        }
                                        else
                                        {
                                            _log.Fine("Cost Calculation updated for VAM_Inv_InOut = " + inout.GetVAM_Inv_InOut_ID());
                                            Get_Trx().Commit();
                                        }
                                    }
                                    continue;
                                }
                            }
                            catch { }
                            #endregion

                            #region Cost Calculation Against AP Credit Memo - During Return Cycle of Purchase
                            try
                            {
                                if (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["TableName"]) == "VAM_MatchInvoice" &&
                                    (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["issotrx"]) == "N" &&
                                     Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["isreturntrx"]) == "Y"))
                                {
                                    matchInvoice = new MVAMMatchInvoice(GetCtx(), Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]), Get_Trx());
                                    inoutLine = new MVAMInvInOutLine(GetCtx(), matchInvoice.GetVAM_Inv_InOutLine_ID(), Get_Trx());
                                    invoiceLine = new MVABInvoiceLine(GetCtx(), matchInvoice.GetVAB_InvoiceLine_ID(), Get_Trx());
                                    invoice = new MVABInvoice(GetCtx(), invoiceLine.GetVAB_Invoice_ID(), Get_Trx());
                                    product = new MVAMProduct(GetCtx(), invoiceLine.GetVAM_Product_ID(), Get_Trx());
                                    bool isUpdatePostCurrentcostPriceFromMR = MVAMProductCostElement.IsPOCostingmethod(GetCtx(), GetVAF_Client_ID(), product.GetVAM_Product_ID(), Get_Trx());
                                    ProductInvoiceLineCost = invoiceLine.GetProductLineCost(invoiceLine);

                                    if (inoutLine.GetVAB_OrderLine_ID() > 0)
                                    {
                                        orderLine = new MVABOrderLine(GetCtx(), inoutLine.GetVAB_OrderLine_ID(), Get_Trx());
                                        order = new MVABOrder(GetCtx(), orderLine.GetVAB_Order_ID(), Get_Trx());
                                    }
                                    if (product.GetProductType() == "I" && product.GetVAM_Product_ID() > 0)
                                    {
                                        if (countColumnExist > 0)
                                        {
                                            isCostAdjustableOnLost = product.IsCostAdjustmentOnLost();
                                        }

                                        if (inoutLine.IsCostCalculated())
                                        {
                                            // when isCostAdjustableOnLost = true on product and movement qty on MR is less than invoice qty then consider MR qty else invoice qty
                                            if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), invoiceLine.GetVAF_Client_ID(), invoiceLine.GetVAF_Org_ID(), product, invoiceLine.GetVAM_PFeature_SetInstance_ID(),
                                                  "Invoice(Vendor)-Return", null, inoutLine, null, invoiceLine, null,
                                                isCostAdjustableOnLost && matchInvoice.GetQty() < invoiceLine.GetQtyInvoiced() ? Decimal.Negate(ProductInvoiceLineCost) : Decimal.Negate(Decimal.Multiply(Decimal.Divide(ProductInvoiceLineCost, invoiceLine.GetQtyInvoiced()), matchInvoice.GetQty())),
                                                 Decimal.Negate(matchInvoice.GetQty()), Get_Trx(), out conversionNotFoundInvoice))
                                            {
                                                if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                                {
                                                    conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                                }
                                                _log.Info("Cost not Calculated for Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                            }
                                            else
                                            {
                                                if (invoice.GetDescription() != null && invoice.GetDescription().Contains("{->"))
                                                {
                                                    invoiceLine.SetIsReversedCostCalculated(true);
                                                }
                                                invoiceLine.SetIsCostCalculated(true);
                                                if (client.IsCostImmediate() && !invoiceLine.IsCostImmediate())
                                                {
                                                    invoiceLine.SetIsCostImmediate(true);
                                                }
                                                if (!invoiceLine.Save(Get_Trx()))
                                                {
                                                    ValueNamePair pp = VLogger.RetrieveError();
                                                    _log.Info("Error found for saving Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID() +
                                                               " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                    Get_Trx().Rollback();
                                                }
                                                else
                                                {
                                                    _log.Fine("Cost Calculation updated for m_invoiceline = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                    Get_Trx().Commit();

                                                    // set is cost calculation true on match invoice
                                                    matchInvoice.SetIsCostCalculated(true);
                                                    if (matchInvoice.Get_ColumnIndex("PostCurrentCostPrice") >= 0)
                                                    {
                                                        // get cost from Product Cost after cost calculation
                                                        currentCostPrice = MVAMVAMProductCost.GetproductCosts(GetVAF_Client_ID(), GetVAF_Org_ID(),
                                                                                                 product.GetVAM_Product_ID(), invoiceLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), inoutLine.GetVAM_Warehouse_ID());
                                                        matchInvoice.SetPostCurrentCostPrice(currentCostPrice);
                                                    }
                                                    if (!matchInvoice.Save(Get_Trx()))
                                                    {
                                                        ValueNamePair pp = VLogger.RetrieveError();
                                                        _log.Info("Error found for saving Invoice(Vendor) for this Line ID = " + matchInvoice.GetVAB_InvoiceLine_ID() +
                                                                   " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                        Get_Trx().Rollback();
                                                    }
                                                    else
                                                    {
                                                        Get_Trx().Commit();
                                                        // update the Post current price after Invoice receving on inoutline
                                                        if (!isUpdatePostCurrentcostPriceFromMR)
                                                        {
                                                            DB.ExecuteQuery(@"UPDATE VAM_Inv_InOutLine SET PostCurrentCostPrice =   " + currentCostPrice +
                                                                            @" WHERE VAM_Inv_InOutLine_ID = " + matchInvoice.GetVAM_Inv_InOutLine_ID(), null, Get_Trx());
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    continue;
                                }
                            }
                            catch { }
                            #endregion

                            #region Cost Calculation For shipment
                            try
                            {
                                if (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["TableName"]) == "VAM_Inv_InOut" &&
                                    Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["issotrx"]) == "Y" &&
                                    Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["isreturntrx"]) == "N" &&
                                    (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["docstatus"]) == "CO" ||
                                     Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["docstatus"]) == "CL"))
                                {
                                    inout = new MVAMInvInOut(GetCtx(), Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]), Get_Trx());

                                    sql.Clear();
                                    if (inout.GetDescription() != null && inout.GetDescription().Contains("{->"))
                                    {
                                        sql.Append("SELECT * FROM VAM_Inv_InOutLine WHERE IsActive = 'Y' AND iscostcalculated = 'Y'  AND IsReversedCostCalculated = 'N' " +
                                                  " AND VAM_Inv_InOut_ID = " + inout.GetVAM_Inv_InOut_ID() + " ORDER BY Line ");
                                    }
                                    else
                                    {
                                        sql.Append("SELECT * FROM VAM_Inv_InOutLine WHERE IsActive = 'Y' AND iscostcalculated = 'N' " +
                                                     " AND VAM_Inv_InOut_ID = " + inout.GetVAM_Inv_InOut_ID() + " ORDER BY Line ");
                                    }
                                    dsChildRecord = DB.ExecuteDataset(sql.ToString(), null, Get_Trx());
                                    if (dsChildRecord != null && dsChildRecord.Tables.Count > 0 && dsChildRecord.Tables[0].Rows.Count > 0)
                                    {
                                        for (int j = 0; j < dsChildRecord.Tables[0].Rows.Count; j++)
                                        {
                                            try
                                            {
                                                inoutLine = new MVAMInvInOutLine(GetCtx(), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_Inv_InOutLine_ID"]), Get_Trx());
                                                orderLine = new MVABOrderLine(GetCtx(), inoutLine.GetVAB_OrderLine_ID(), null);
                                                if (orderLine != null && orderLine.GetVAB_Order_ID() > 0)
                                                {
                                                    order = new MVABOrder(GetCtx(), orderLine.GetVAB_Order_ID(), null);
                                                    if (order.GetDocStatus() != "VO")
                                                    {
                                                        if (orderLine != null && orderLine.GetVAB_Order_ID() > 0 && orderLine.GetQtyOrdered() == 0)
                                                            continue;
                                                    }
                                                    ProductOrderLineCost = orderLine.GetProductLineCost(orderLine);
                                                    ProductOrderPriceActual = ProductOrderLineCost / orderLine.GetQtyEntered();
                                                }
                                                product = new MVAMProduct(GetCtx(), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_Product_ID"]), Get_Trx());
                                                if (product.GetProductType() == "I") // for Item Type product
                                                {
                                                    #region shipment
                                                    if (inout.IsSOTrx() && !inout.IsReturnTrx())
                                                    {
                                                        if (inout.GetVAB_Order_ID() <= 0)
                                                        {
                                                            break;
                                                        }

                                                        #region get price from VAM_ProductCost (Current Cost Price)
                                                        if (!client.IsCostImmediate() || inoutLine.GetCurrentCostPrice() == 0)
                                                        {
                                                            // get price from VAM_ProductCost (Current Cost Price)
                                                            currentCostPrice = 0;
                                                            currentCostPrice = MVAMVAMProductCost.GetproductCosts(inoutLine.GetVAF_Client_ID(), inoutLine.GetVAF_Org_ID(),
                                                                inoutLine.GetVAM_Product_ID(), inoutLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), inout.GetVAM_Warehouse_ID());
                                                            DB.ExecuteQuery("UPDATE VAM_Inv_InOutLine SET CurrentCostPrice = " + currentCostPrice +
                                                                          @" WHERE VAM_Inv_InOutLine_ID = " + inoutLine.GetVAM_Inv_InOutLine_ID(), null, Get_Trx());
                                                        }
                                                        #endregion

                                                        if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), inout.GetVAF_Client_ID(), inout.GetVAF_Org_ID(), product, inoutLine.GetVAM_PFeature_SetInstance_ID(),
                                                             "Shipment", null, inoutLine, null, null, null,
                                                             order.GetDocStatus() != "VO" ? Decimal.Multiply(Decimal.Divide(ProductOrderLineCost, orderLine.GetQtyOrdered()), Decimal.Negate(inoutLine.GetMovementQty()))
                                                             : Decimal.Multiply(ProductOrderPriceActual, Decimal.Negate(inoutLine.GetQtyEntered())),
                                                             Decimal.Negate(inoutLine.GetMovementQty()),
                                                             Get_Trx(), out conversionNotFoundInOut))
                                                        {
                                                            if (!conversionNotFoundInOut1.Contains(conversionNotFoundInOut))
                                                            {
                                                                conversionNotFoundInOut1 += conversionNotFoundInOut + " , ";
                                                            }
                                                            _log.Info("Cost not Calculated for Customer Return for this Line ID = " + inoutLine.GetVAM_Inv_InOutLine_ID());
                                                        }
                                                        else
                                                        {
                                                            if (inoutLine.GetCurrentCostPrice() == 0)
                                                            {
                                                                // get price from VAM_ProductCost (Current Cost Price)
                                                                currentCostPrice = 0;
                                                                currentCostPrice = MVAMVAMProductCost.GetproductCosts(inoutLine.GetVAF_Client_ID(), inoutLine.GetVAF_Org_ID(),
                                                                    inoutLine.GetVAM_Product_ID(), inoutLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), inout.GetVAM_Warehouse_ID());
                                                                inoutLine.SetCurrentCostPrice(currentCostPrice);
                                                            }
                                                            if (inout.GetDescription() != null && inout.GetDescription().Contains("{->"))
                                                            {
                                                                inoutLine.SetIsReversedCostCalculated(true);
                                                            }
                                                            inoutLine.SetIsCostCalculated(true);
                                                            if (client.IsCostImmediate() && !inoutLine.IsCostImmediate())
                                                            {
                                                                inoutLine.SetIsCostImmediate(true);
                                                            }
                                                            if (!inoutLine.Save(Get_Trx()))
                                                            {
                                                                ValueNamePair pp = VLogger.RetrieveError();
                                                                _log.Info("Error found for Customer Return for this Line ID = " + inoutLine.GetVAM_Inv_InOutLine_ID() +
                                                                           " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                Get_Trx().Rollback();
                                                            }
                                                            else
                                                            {
                                                                _log.Fine("Cost Calculation updated for VAM_Inv_InOutLine = " + inoutLine.GetVAM_Inv_InOutLine_ID());
                                                                Get_Trx().Commit();
                                                            }
                                                        }
                                                    }
                                                    #endregion
                                                }
                                            }
                                            catch { }
                                        }
                                    }
                                    sql.Clear();
                                    if (inout.GetDescription() != null && inout.GetDescription().Contains("{->"))
                                    {
                                        sql.Append("SELECT COUNT(*) FROM VAM_Inv_InOutLine WHERE IsReversedCostCalculated = 'N' AND IsActive = 'Y' AND VAM_Inv_InOut_ID = " + inout.GetVAM_Inv_InOut_ID());
                                    }
                                    else
                                    {
                                        sql.Append("SELECT COUNT(*) FROM VAM_Inv_InOutLine WHERE IsCostCalculated = 'N' AND IsActive = 'Y' AND VAM_Inv_InOut_ID = " + inout.GetVAM_Inv_InOut_ID());
                                    }
                                    if (Util.GetValueOfInt(DB.ExecuteScalar(sql.ToString(), null, Get_Trx())) <= 0)
                                    {
                                        if (inout.GetDescription() != null && inout.GetDescription().Contains("{->"))
                                        {
                                            inout.SetIsReversedCostCalculated(true);
                                        }
                                        inout.SetIsCostCalculated(true);
                                        if (!inout.Save(Get_Trx()))
                                        {
                                            ValueNamePair pp = VLogger.RetrieveError();
                                            _log.Info("Error found for saving VAM_Inv_InOut for this Record ID = " + inout.GetVAM_Inv_InOut_ID() +
                                                       " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                        }
                                        else
                                        {
                                            _log.Fine("Cost Calculation updated for VAM_Inv_InOut = " + inout.GetVAM_Inv_InOut_ID());
                                            Get_Trx().Commit();
                                        }
                                    }
                                    continue;
                                }
                            }
                            catch { }
                            #endregion

                            #region Cost Calculation For Customer Return
                            try
                            {
                                if (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["TableName"]) == "VAM_Inv_InOut" &&
                                    Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["issotrx"]) == "Y" &&
                                    Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["isreturntrx"]) == "Y" &&
                                    (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["docstatus"]) == "CO" ||
                                     Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["docstatus"]) == "CL"))
                                {
                                    inout = new MVAMInvInOut(GetCtx(), Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]), Get_Trx());

                                    sql.Clear();
                                    if (inout.GetDescription() != null && inout.GetDescription().Contains("{->"))
                                    {
                                        sql.Append("SELECT * FROM VAM_Inv_InOutLine WHERE IsActive = 'Y' AND iscostcalculated = 'Y'  AND IsReversedCostCalculated = 'N' " +
                                                  " AND VAM_Inv_InOut_ID = " + inout.GetVAM_Inv_InOut_ID() + " ORDER BY Line ");
                                    }
                                    else
                                    {
                                        sql.Append("SELECT * FROM VAM_Inv_InOutLine WHERE IsActive = 'Y' AND iscostcalculated = 'N' " +
                                                     " AND VAM_Inv_InOut_ID = " + inout.GetVAM_Inv_InOut_ID() + " ORDER BY Line ");
                                    }
                                    dsChildRecord = DB.ExecuteDataset(sql.ToString(), null, Get_Trx());
                                    if (dsChildRecord != null && dsChildRecord.Tables.Count > 0 && dsChildRecord.Tables[0].Rows.Count > 0)
                                    {
                                        for (int j = 0; j < dsChildRecord.Tables[0].Rows.Count; j++)
                                        {
                                            try
                                            {
                                                inoutLine = new MVAMInvInOutLine(GetCtx(), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["M_InOutLine_ID"]), Get_Trx());
                                                orderLine = new MVABOrderLine(GetCtx(), inoutLine.GetVAB_OrderLine_ID(), null);
                                                if (orderLine != null && orderLine.GetVAB_Order_ID() > 0)
                                                {
                                                    order = new MVABOrder(GetCtx(), orderLine.GetVAB_Order_ID(), null);
                                                    if (order.GetDocStatus() != "VO")
                                                    {
                                                        if (orderLine != null && orderLine.GetVAB_Order_ID() > 0 && orderLine.GetQtyOrdered() == 0)
                                                            continue;
                                                    }
                                                    ProductOrderLineCost = orderLine.GetProductLineCost(orderLine);
                                                    ProductOrderPriceActual = ProductOrderLineCost / orderLine.GetQtyEntered();
                                                }
                                                product = new MVAMProduct(GetCtx(), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_Product_ID"]), Get_Trx());
                                                if (product.GetProductType() == "I") // for Item Type product
                                                {
                                                    #region Customer Return
                                                    if (inout.IsSOTrx() && inout.IsReturnTrx())
                                                    {
                                                        if (inout.GetOrig_Order_ID() <= 0)
                                                        {
                                                            break;
                                                        }

                                                        #region get price from VAM_ProductCost (Current Cost Price)
                                                        if (!client.IsCostImmediate() || inoutLine.GetCurrentCostPrice() == 0)
                                                        {
                                                            // get price from VAM_ProductCost (Current Cost Price)
                                                            currentCostPrice = 0;
                                                            currentCostPrice = MVAMVAMProductCost.GetproductCostAndQtyMaterial(inoutLine.GetVAF_Client_ID(), inoutLine.GetVAF_Org_ID(),
                                                                inoutLine.GetVAM_Product_ID(), inoutLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), inout.GetVAM_Warehouse_ID(), false);
                                                            DB.ExecuteQuery("UPDATE VAM_Inv_InOutLine SET CurrentCostPrice = " + currentCostPrice +
                                                                           @" WHERE VAM_Inv_InOutLine_ID = " + inoutLine.GetVAM_Inv_InOutLine_ID(), null, Get_Trx());
                                                        }
                                                        #endregion

                                                        if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), inout.GetVAF_Client_ID(), inout.GetVAF_Org_ID(), product, inoutLine.GetVAM_PFeature_SetInstance_ID(),
                                                              "Customer Return", null, inoutLine, null, null, null,
                                                              order.GetDocStatus() != "VO" ? Decimal.Multiply(Decimal.Divide(ProductOrderLineCost, orderLine.GetQtyOrdered()), inoutLine.GetMovementQty())
                                                            : Decimal.Multiply(ProductOrderPriceActual, inoutLine.GetQtyEntered()),
                                                              inoutLine.GetMovementQty(),
                                                              Get_Trx(), out conversionNotFoundInOut))
                                                        {
                                                            if (!conversionNotFoundInOut1.Contains(conversionNotFoundInOut))
                                                            {
                                                                conversionNotFoundInOut1 += conversionNotFoundInOut + " , ";
                                                            }
                                                            _log.Info("Cost not Calculated for Customer Return for this Line ID = " + inoutLine.GetVAM_Inv_InOutLine_ID());
                                                        }
                                                        else
                                                        {
                                                            if (inoutLine.GetCurrentCostPrice() == 0)
                                                            {
                                                                // get price from VAM_ProductCost (Current Cost Price)
                                                                currentCostPrice = 0;
                                                                currentCostPrice = MVAMVAMProductCost.GetproductCostAndQtyMaterial(inoutLine.GetVAF_Client_ID(), inoutLine.GetVAF_Org_ID(),
                                                                    inoutLine.GetVAM_Product_ID(), inoutLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), inout.GetVAM_Warehouse_ID(), false);
                                                                inoutLine.SetCurrentCostPrice(currentCostPrice);
                                                            }
                                                            if (inout.GetDescription() != null && inout.GetDescription().Contains("{->"))
                                                            {
                                                                inoutLine.SetIsReversedCostCalculated(true);
                                                            }
                                                            inoutLine.SetIsCostCalculated(true);
                                                            if (client.IsCostImmediate() && !inoutLine.IsCostImmediate())
                                                            {
                                                                inoutLine.SetIsCostImmediate(true);
                                                            }
                                                            if (!inoutLine.Save(Get_Trx()))
                                                            {
                                                                ValueNamePair pp = VLogger.RetrieveError();
                                                                _log.Info("Error found for Customer Return for this Line ID = " + inoutLine.GetVAM_Inv_InOutLine_ID() +
                                                                           " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                Get_Trx().Rollback();
                                                            }
                                                            else
                                                            {
                                                                _log.Fine("Cost Calculation updated for VAM_Inv_InOutLine = " + inoutLine.GetVAM_Inv_InOutLine_ID());
                                                                Get_Trx().Commit();
                                                            }
                                                        }
                                                    }
                                                    #endregion
                                                }
                                            }
                                            catch { }
                                        }
                                    }
                                    sql.Clear();
                                    if (inout.GetDescription() != null && inout.GetDescription().Contains("{->"))
                                    {
                                        sql.Append("SELECT COUNT(*) FROM VAM_Inv_InOutLine WHERE IsReversedCostCalculated = 'N' AND IsActive = 'Y' AND VAM_Inv_InOut_ID = " + inout.GetVAM_Inv_InOut_ID());
                                    }
                                    else
                                    {
                                        sql.Append("SELECT COUNT(*) FROM VAM_Inv_InOutLine WHERE IsCostCalculated = 'N' AND IsActive = 'Y' AND VAM_Inv_InOut_ID = " + inout.GetVAM_Inv_InOut_ID());
                                    }
                                    if (Util.GetValueOfInt(DB.ExecuteScalar(sql.ToString(), null, Get_Trx())) <= 0)
                                    {
                                        if (inout.GetDescription() != null && inout.GetDescription().Contains("{->"))
                                        {
                                            inout.SetIsReversedCostCalculated(true);
                                        }
                                        inout.SetIsCostCalculated(true);
                                        if (!inout.Save(Get_Trx()))
                                        {
                                            ValueNamePair pp = VLogger.RetrieveError();
                                            _log.Info("Error found for saving VAM_Inv_InOut for this Record ID = " + inout.GetVAM_Inv_InOut_ID() +
                                                       " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                        }
                                        else
                                        {
                                            _log.Fine("Cost Calculation updated for VAM_Inv_InOut = " + inout.GetVAM_Inv_InOut_ID());
                                            Get_Trx().Commit();
                                        }
                                    }
                                    continue;
                                }
                            }
                            catch { }
                            #endregion

                            #region Component Reduce for Production Execution
                            try
                            {
                                if (count > 0)
                                {
                                    if (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["TableName"]) == "VAMFG_M_WrkOdrTransaction" &&
                                    (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["docstatus"]) == "CO" ||
                                     Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["docstatus"]) == "CL"))
                                    {
                                        po_WrkOdrTransaction = tbl_WrkOdrTransaction.GetPO(GetCtx(), Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]), Get_Trx());
                                        // Production Execution Transaction Type
                                        woTrxType = Util.GetValueOfString(po_WrkOdrTransaction.Get_Value("VAMFG_WorkOrderTxnType"));
                                        sql.Clear();
                                        if (Util.GetValueOfString(po_WrkOdrTransaction.Get_Value("VAMFG_Description")) != null &&
                                            Util.GetValueOfString(po_WrkOdrTransaction.Get_Value("VAMFG_Description")).Contains("{->"))
                                        {
                                            sql.Append("SELECT * FROM VAMFG_M_WrkOdrTrnsctionLine WHERE IsActive = 'Y' AND iscostcalculated = 'Y' AND IsReversedCostCalculated = 'N' " +
                                                        " AND VAMFG_M_WrkOdrTransaction_ID = " + Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]) +
                                                        " ORDER BY VAMFG_Line ");
                                        }
                                        else
                                        {
                                            sql.Append("SELECT * FROM VAMFG_M_WrkOdrTrnsctionLine WHERE IsActive = 'Y' AND iscostcalculated = 'N' " +
                                                         " AND VAMFG_M_WrkOdrTransaction_ID = " + Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]) +
                                                         " ORDER BY VAMFG_Line ");
                                        }
                                        dsChildRecord = DB.ExecuteDataset(sql.ToString(), null, Get_Trx());
                                        if (dsChildRecord != null && dsChildRecord.Tables.Count > 0 && dsChildRecord.Tables[0].Rows.Count > 0)
                                        {
                                            for (int j = 0; j < dsChildRecord.Tables[0].Rows.Count; j++)
                                            {
                                                try
                                                {
                                                    po_WrkOdrTrnsctionLine = tbl_WrkOdrTrnsctionLine.GetPO(GetCtx(), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAMFG_M_WrkOdrTrnsctionLine_ID"]), Get_Trx());

                                                    product = new MVAMProduct(GetCtx(), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_Product_ID"]), Get_Trx());

                                                    #region get price from VAM_ProductCost (Current Cost Price)
                                                    // get price from VAM_ProductCost (Current Cost Price)
                                                    if (Util.GetValueOfDecimal(po_WrkOdrTrnsctionLine.Get_Value("CurrentCostPrice")) == 0 &&
                                                        !(woTrxType.Equals(ViennaAdvantage.Model.X_VAMFG_M_WrkOdrTransaction.VAMFG_WORKORDERTXNTYPE_3_TransferAssemblyToStore)
                                                        || woTrxType.Equals(ViennaAdvantage.Model.X_VAMFG_M_WrkOdrTransaction.VAMFG_WORKORDERTXNTYPE_AssemblyReturnFroMVAMInventory)))
                                                    {
                                                        currentCostPrice = 0;
                                                        currentCostPrice = MVAMVAMProductCost.GetproductCosts(Util.GetValueOfInt(po_WrkOdrTrnsctionLine.Get_Value("VAF_Client_ID")), Util.GetValueOfInt(po_WrkOdrTrnsctionLine.Get_Value("VAF_Org_ID")),
                                                            Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_Product_ID"]), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_PFeature_SetInstance_ID"]), Get_Trx());
                                                        po_WrkOdrTrnsctionLine.Set_Value("CurrentCostPrice", currentCostPrice);
                                                        if (!po_WrkOdrTrnsctionLine.Save(Get_Trx()))
                                                        {
                                                            ValueNamePair pp = VLogger.RetrieveError();
                                                            _log.Info("Error found for Production execution Line for this Line ID = " + Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAMFG_M_WrkOdrTrnsctionLine_ID"]) +
                                                                       " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                            Get_Trx().Rollback();
                                                        }
                                                    }
                                                    else if (woTrxType.Equals(ViennaAdvantage.Model.X_VAMFG_M_WrkOdrTransaction.VAMFG_WORKORDERTXNTYPE_3_TransferAssemblyToStore)
                                                        || woTrxType.Equals(ViennaAdvantage.Model.X_VAMFG_M_WrkOdrTransaction.VAMFG_WORKORDERTXNTYPE_AssemblyReturnFroMVAMInventory))
                                                    {
                                                        // when product having checkbox "IsBAsedOnRollup" then not to calculate cot of finished Good
                                                        if (product.IsBasedOnRollup())
                                                        {
                                                            continue;
                                                        }

                                                        currentCostPrice = GetCostForProductionFinishedGood(Util.GetValueOfInt(po_WrkOdrTransaction.Get_Value("VAMFG_VAM_WorkOrder_ID")), Get_Trx());

                                                        // if currentCostPrice is ZERO, then not to calculate cost of finished Good
                                                        if (currentCostPrice == 0)
                                                        {
                                                            continue;
                                                        }

                                                        // Update cost on Record
                                                        DB.ExecuteQuery(@"UPDATE VAMFG_M_WrkOdrTransaction SET CurrentCostPrice = " + currentCostPrice + @" 
                                                                        WHERE VAMFG_M_WrkOdrTransaction_ID = " + Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]), null, Get_Trx());
                                                    }
                                                    #endregion

                                                    // ComponentIssueToWorkOrder / AssemblyReturnFroMVAMInventory
                                                    if (woTrxType.Equals(ViennaAdvantage.Model.X_VAMFG_M_WrkOdrTransaction.VAMFG_WORKORDERTXNTYPE_1_ComponentIssueToWorkOrder)
                                                        || woTrxType.Equals(ViennaAdvantage.Model.X_VAMFG_M_WrkOdrTransaction.VAMFG_WORKORDERTXNTYPE_AssemblyReturnFroMVAMInventory))
                                                    {
                                                        if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), Util.GetValueOfInt(po_WrkOdrTrnsctionLine.Get_Value("VAF_Client_ID")),
                                                            Util.GetValueOfInt(po_WrkOdrTrnsctionLine.Get_Value("VAF_Org_ID")), product, 0,
                                                            woTrxType.Equals(ViennaAdvantage.Model.X_VAMFG_M_WrkOdrTransaction.VAMFG_WORKORDERTXNTYPE_AssemblyReturnFroMVAMInventory) ? "PE-FinishGood" : "Production Execution",
                                                            null, null, null, null, po_WrkOdrTrnsctionLine,
                                                             woTrxType.Equals(ViennaAdvantage.Model.X_VAMFG_M_WrkOdrTransaction.VAMFG_WORKORDERTXNTYPE_AssemblyReturnFroMVAMInventory) ? currentCostPrice : 0,
                                                            countGOM01 > 0 ? Decimal.Negate(Util.GetValueOfDecimal(po_WrkOdrTrnsctionLine.Get_Value("GOM01_ActualQuantity"))) :
                                                            Decimal.Negate(Util.GetValueOfDecimal(po_WrkOdrTrnsctionLine.Get_Value("VAMFG_QtyEntered"))),
                                                            Get_Trx(), out conversionNotFoundInOut))
                                                        {
                                                            if (!conversionNotFoundProductionExecution1.Contains(conversionNotFoundProductionExecution))
                                                            {
                                                                conversionNotFoundProductionExecution1 += conversionNotFoundProductionExecution + " , ";
                                                            }
                                                            _log.Info("Cost not Calculated for Production Execution for this Line ID = " + Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAMFG_M_WrkOdrTrnsctionLine_ID"]));
                                                        }
                                                        else
                                                        {
                                                            if (Util.GetValueOfString(po_WrkOdrTransaction.Get_Value("VAMFG_Description")) != null && Util.GetValueOfString(po_WrkOdrTransaction.Get_Value("VAMFG_Description")).Contains("{->"))
                                                            {
                                                                po_WrkOdrTrnsctionLine.Set_Value("IsReversedCostCalculated", true);
                                                            }
                                                            po_WrkOdrTrnsctionLine.Set_Value("IsCostCalculated", true);
                                                            if (!po_WrkOdrTrnsctionLine.Save(Get_Trx()))
                                                            {
                                                                ValueNamePair pp = VLogger.RetrieveError();
                                                                _log.Info("Error found for Production Execution for this Line ID = " + Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAMFG_M_WrkOdrTrnsctionLine_ID"]) +
                                                                           " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                Get_Trx().Rollback();
                                                            }
                                                            else
                                                            {
                                                                _log.Fine("Cost Calculation updated for Production Execution line ID = " + Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAMFG_M_WrkOdrTrnsctionLine_ID"]));
                                                                Get_Trx().Commit();
                                                            }
                                                        }
                                                    }
                                                    // ComponentReturnFromWorkOrder / TransferAssemblyToStore
                                                    else if (woTrxType.Equals(ViennaAdvantage.Model.X_VAMFG_M_WrkOdrTransaction.VAMFG_WORKORDERTXNTYPE_ComponentReturnFromWorkOrder)
                                                        || woTrxType.Equals(ViennaAdvantage.Model.X_VAMFG_M_WrkOdrTransaction.VAMFG_WORKORDERTXNTYPE_3_TransferAssemblyToStore))
                                                    {
                                                        if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), Util.GetValueOfInt(po_WrkOdrTrnsctionLine.Get_Value("VAF_Client_ID")),
                                                            Util.GetValueOfInt(po_WrkOdrTrnsctionLine.Get_Value("VAF_Org_ID")), product, 0,
                                                            woTrxType.Equals(ViennaAdvantage.Model.X_VAMFG_M_WrkOdrTransaction.VAMFG_WORKORDERTXNTYPE_3_TransferAssemblyToStore) ? "PE-FinishGood" : "Production Execution",
                                                            null, null, null, null, po_WrkOdrTrnsctionLine,
                                                            woTrxType.Equals(ViennaAdvantage.Model.X_VAMFG_M_WrkOdrTransaction.VAMFG_WORKORDERTXNTYPE_3_TransferAssemblyToStore) ? currentCostPrice : 0,
                                                            countGOM01 > 0 ? Util.GetValueOfDecimal(po_WrkOdrTrnsctionLine.Get_Value("GOM01_ActualQuantity")) :
                                                            Util.GetValueOfDecimal(po_WrkOdrTrnsctionLine.Get_Value("VAMFG_QtyEntered")), Get_Trx(), out conversionNotFoundInOut))
                                                        {
                                                            if (!conversionNotFoundProductionExecution1.Contains(conversionNotFoundProductionExecution))
                                                            {
                                                                conversionNotFoundProductionExecution1 += conversionNotFoundProductionExecution + " , ";
                                                            }
                                                            _log.Info("Cost not Calculated for Production Execution for this Line ID = " + Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAMFG_M_WrkOdrTrnsctionLine_ID"]));
                                                        }
                                                        else
                                                        {
                                                            if (Util.GetValueOfString(po_WrkOdrTransaction.Get_Value("VAMFG_Description")) != null && Util.GetValueOfString(po_WrkOdrTransaction.Get_Value("VAMFG_Description")).Contains("{->"))
                                                            {
                                                                po_WrkOdrTrnsctionLine.Set_Value("IsReversedCostCalculated", true);
                                                            }
                                                            po_WrkOdrTrnsctionLine.Set_Value("IsCostCalculated", true);
                                                            if (!po_WrkOdrTrnsctionLine.Save(Get_Trx()))
                                                            {
                                                                ValueNamePair pp = VLogger.RetrieveError();
                                                                _log.Info("Error found for Production Execution for this Line ID = " + Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAMFG_M_WrkOdrTrnsctionLine_ID"]) +
                                                                           " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                Get_Trx().Rollback();
                                                            }
                                                            else
                                                            {
                                                                _log.Fine("Cost Calculation updated for Production Execution line ID = " + Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAMFG_M_WrkOdrTrnsctionLine_ID"]));
                                                                Get_Trx().Commit();
                                                            }
                                                        }
                                                    }
                                                }
                                                catch { }
                                            }
                                        }
                                        sql.Clear();
                                        if (Util.GetValueOfString(po_WrkOdrTransaction.Get_Value("VAMFG_Description")) != null &&
                                            Util.GetValueOfString(po_WrkOdrTransaction.Get_Value("VAMFG_Description")).Contains("{->"))
                                        {
                                            sql.Append(@"SELECT COUNT(*) FROM VAMFG_M_WrkOdrTrnsctionLine WHERE IsReversedCostCalculated = 'N'
                                                     AND IsActive = 'Y' AND VAMFG_M_WrkOdrTransaction_ID = " + Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]));
                                        }
                                        else
                                        {
                                            sql.Append(@"SELECT COUNT(*) FROM VAMFG_M_WrkOdrTrnsctionLine WHERE IsCostCalculated = 'N' AND IsActive = 'Y'
                                           AND VAMFG_M_WrkOdrTransaction_ID = " + Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]));
                                        }
                                        if (Util.GetValueOfInt(DB.ExecuteScalar(sql.ToString(), null, Get_Trx())) <= 0)
                                        {
                                            if (Util.GetValueOfString(po_WrkOdrTransaction.Get_Value("VAMFG_Description")) != null &&
                                                Util.GetValueOfString(po_WrkOdrTransaction.Get_Value("VAMFG_Description")).Contains("{->"))
                                            {
                                                po_WrkOdrTransaction.Set_Value("IsReversedCostCalculated", true);
                                            }
                                            po_WrkOdrTransaction.Set_Value("IsCostCalculated", true);
                                            if (!po_WrkOdrTransaction.Save(Get_Trx()))
                                            {
                                                ValueNamePair pp = VLogger.RetrieveError();
                                                _log.Info("Error found for saving Production execution for this Record ID = " + Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]) +
                                                                                                           " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                            }
                                            else
                                            {
                                                _log.Fine("Cost Calculation updated for Production Execution = " + Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]));
                                                Get_Trx().Commit();
                                            }
                                        }
                                        continue;
                                    }
                                }
                            }
                            catch { }


                            #endregion

                            //Reverse Record

                            #region Component Reduce for Production Execution
                            try
                            {
                                if (count > 0)
                                {
                                    if (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["TableName"]) == "VAMFG_M_WrkOdrTransaction" &&
                                        (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["docstatus"]) == "RE" ||
                                        Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["docstatus"]) == "VO"))
                                    {
                                        po_WrkOdrTransaction = tbl_WrkOdrTransaction.GetPO(GetCtx(), Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]), Get_Trx());
                                        // Production Execution Transaction Type
                                        woTrxType = Util.GetValueOfString(po_WrkOdrTransaction.Get_Value("VAMFG_WorkOrderTxnType"));
                                        sql.Clear();
                                        if (Util.GetValueOfString(po_WrkOdrTransaction.Get_Value("VAMFG_Description")) != null &&
                                            Util.GetValueOfString(po_WrkOdrTransaction.Get_Value("VAMFG_Description")).Contains("{->"))
                                        {
                                            sql.Append("SELECT * FROM VAMFG_M_WrkOdrTrnsctionLine WHERE IsActive = 'Y' AND iscostcalculated = 'Y' AND IsReversedCostCalculated = 'N' " +
                                                        " AND VAMFG_M_WrkOdrTransaction_ID = " + Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]) +
                                                        " ORDER BY VAMFG_Line ");
                                        }
                                        else
                                        {
                                            sql.Append("SELECT * FROM VAMFG_M_WrkOdrTrnsctionLine WHERE IsActive = 'Y' AND iscostcalculated = 'N' " +
                                                         " AND VAMFG_M_WrkOdrTransaction_ID = " + Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]) +
                                                         " ORDER BY VAMFG_Line ");
                                        }
                                        dsChildRecord = DB.ExecuteDataset(sql.ToString(), null, Get_Trx());
                                        if (dsChildRecord != null && dsChildRecord.Tables.Count > 0 && dsChildRecord.Tables[0].Rows.Count > 0)
                                        {
                                            for (int j = 0; j < dsChildRecord.Tables[0].Rows.Count; j++)
                                            {
                                                try
                                                {
                                                    po_WrkOdrTrnsctionLine = tbl_WrkOdrTrnsctionLine.GetPO(GetCtx(), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAMFG_M_WrkOdrTrnsctionLine_ID"]), Get_Trx());

                                                    product = new MVAMProduct(GetCtx(), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_Product_ID"]), Get_Trx());

                                                    #region get price from VAM_ProductCost (Current Cost Price)
                                                    // get price from VAM_ProductCost (Current Cost Price)
                                                    if (Util.GetValueOfDecimal(po_WrkOdrTrnsctionLine.Get_Value("CurrentCostPrice")) == 0 &&
                                                        !(woTrxType.Equals(ViennaAdvantage.Model.X_VAMFG_M_WrkOdrTransaction.VAMFG_WORKORDERTXNTYPE_AssemblyReturnFroMVAMInventory)
                                                        || woTrxType.Equals(ViennaAdvantage.Model.X_VAMFG_M_WrkOdrTransaction.VAMFG_WORKORDERTXNTYPE_3_TransferAssemblyToStore)))
                                                    {
                                                        currentCostPrice = 0;
                                                        currentCostPrice = MVAMVAMProductCost.GetproductCosts(Util.GetValueOfInt(po_WrkOdrTrnsctionLine.Get_Value("VAF_Client_ID")), Util.GetValueOfInt(po_WrkOdrTrnsctionLine.Get_Value("VAF_Org_ID")),
                                                            Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_Product_ID"]), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_PFeature_SetInstance_ID"]), Get_Trx());
                                                        po_WrkOdrTrnsctionLine.Set_Value("CurrentCostPrice", currentCostPrice);
                                                        if (!po_WrkOdrTrnsctionLine.Save(Get_Trx()))
                                                        {
                                                            ValueNamePair pp = VLogger.RetrieveError();
                                                            _log.Info("Error found for Production execution Line for this Line ID = " + Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAMFG_M_WrkOdrTrnsctionLine_ID"]) +
                                                                                                                                   " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                            Get_Trx().Rollback();
                                                        }
                                                    }
                                                    else if (woTrxType.Equals(ViennaAdvantage.Model.X_VAMFG_M_WrkOdrTransaction.VAMFG_WORKORDERTXNTYPE_AssemblyReturnFroMVAMInventory)
                                                        || woTrxType.Equals(ViennaAdvantage.Model.X_VAMFG_M_WrkOdrTransaction.VAMFG_WORKORDERTXNTYPE_3_TransferAssemblyToStore))
                                                    {
                                                        // when product having checkbox "IsBAsedOnRollup" then not to calculate cot of finished Good
                                                        if (product.IsBasedOnRollup())
                                                        {
                                                            continue;
                                                        }

                                                        currentCostPrice = GetCostForProductionFinishedGood(Util.GetValueOfInt(po_WrkOdrTransaction.Get_Value("VAMFG_VAM_WorkOrder_ID")), Get_Trx());

                                                        // if currentCostPrice is ZERO, then not to calculate cost of finished Good
                                                        if (currentCostPrice == 0)
                                                        {
                                                            continue;
                                                        }

                                                        // Update cost on Record
                                                        DB.ExecuteQuery(@"UPDATE VAMFG_M_WrkOdrTransaction SET CurrentCostPrice = " + currentCostPrice + @" 
                                                                        WHERE VAMFG_M_WrkOdrTransaction_ID = " + Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]), null, Get_Trx());
                                                    }
                                                    #endregion

                                                    if (woTrxType.Equals(ViennaAdvantage.Model.X_VAMFG_M_WrkOdrTransaction.VAMFG_WORKORDERTXNTYPE_1_ComponentIssueToWorkOrder)
                                                        || woTrxType.Equals(ViennaAdvantage.Model.X_VAMFG_M_WrkOdrTransaction.VAMFG_WORKORDERTXNTYPE_AssemblyReturnFroMVAMInventory))
                                                    {
                                                        if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), Util.GetValueOfInt(po_WrkOdrTrnsctionLine.Get_Value("VAF_Client_ID")),
                                                            Util.GetValueOfInt(po_WrkOdrTrnsctionLine.Get_Value("VAF_Org_ID")), product, 0,
                                                            woTrxType.Equals(ViennaAdvantage.Model.X_VAMFG_M_WrkOdrTransaction.VAMFG_WORKORDERTXNTYPE_AssemblyReturnFroMVAMInventory) ? "PE-FinishGood" : "Production Execution",
                                                            null, null, null, null, po_WrkOdrTrnsctionLine,
                                                            woTrxType.Equals(ViennaAdvantage.Model.X_VAMFG_M_WrkOdrTransaction.VAMFG_WORKORDERTXNTYPE_AssemblyReturnFroMVAMInventory) ? currentCostPrice : 0,
                                                            countGOM01 > 0 ? Decimal.Negate(Util.GetValueOfDecimal(po_WrkOdrTrnsctionLine.Get_Value("GOM01_ActualQuantity"))) :
                                                            Decimal.Negate(Util.GetValueOfDecimal(po_WrkOdrTrnsctionLine.Get_Value("VAMFG_QtyEntered"))), Get_Trx(), out conversionNotFoundInOut))
                                                        {
                                                            if (!conversionNotFoundProductionExecution1.Contains(conversionNotFoundProductionExecution))
                                                            {
                                                                conversionNotFoundProductionExecution1 += conversionNotFoundProductionExecution + " , ";
                                                            }
                                                            _log.Info("Cost not Calculated for Production Execution for this Line ID = " + Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAMFG_M_WrkOdrTrnsctionLine_ID"]));
                                                        }
                                                        else
                                                        {
                                                            if (Util.GetValueOfString(po_WrkOdrTransaction.Get_Value("VAMFG_Description")) != null && Util.GetValueOfString(po_WrkOdrTransaction.Get_Value("VAMFG_Description")).Contains("{->"))
                                                            {
                                                                po_WrkOdrTrnsctionLine.Set_Value("IsReversedCostCalculated", true);
                                                            }
                                                            po_WrkOdrTrnsctionLine.Set_Value("IsCostCalculated", true);
                                                            if (!po_WrkOdrTrnsctionLine.Save(Get_Trx()))
                                                            {
                                                                ValueNamePair pp = VLogger.RetrieveError();
                                                                _log.Info("Error found for Production Execution for this Line ID = " + Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAMFG_M_WrkOdrTrnsctionLine_ID"]) +
                                                                                                                                           " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                Get_Trx().Rollback();
                                                            }
                                                            else
                                                            {
                                                                _log.Fine("Cost Calculation updated for Production Execution line ID = " + Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAMFG_M_WrkOdrTrnsctionLine_ID"]));
                                                                Get_Trx().Commit();
                                                            }
                                                        }
                                                    }
                                                    else if (woTrxType.Equals(ViennaAdvantage.Model.X_VAMFG_M_WrkOdrTransaction.VAMFG_WORKORDERTXNTYPE_ComponentReturnFromWorkOrder)
                                                        || woTrxType.Equals(ViennaAdvantage.Model.X_VAMFG_M_WrkOdrTransaction.VAMFG_WORKORDERTXNTYPE_3_TransferAssemblyToStore))
                                                    {
                                                        if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), Util.GetValueOfInt(po_WrkOdrTrnsctionLine.Get_Value("VAF_Client_ID")),
                                                            Util.GetValueOfInt(po_WrkOdrTrnsctionLine.Get_Value("VAF_Org_ID")), product, 0,
                                                            woTrxType.Equals(ViennaAdvantage.Model.X_VAMFG_M_WrkOdrTransaction.VAMFG_WORKORDERTXNTYPE_3_TransferAssemblyToStore) ? "PE-FinishGood" : "Production Execution",
                                                            null, null, null, null, po_WrkOdrTrnsctionLine,
                                                            woTrxType.Equals(ViennaAdvantage.Model.X_VAMFG_M_WrkOdrTransaction.VAMFG_WORKORDERTXNTYPE_3_TransferAssemblyToStore) ? currentCostPrice : 0,
                                                            countGOM01 > 0 ? Util.GetValueOfDecimal(po_WrkOdrTrnsctionLine.Get_Value("GOM01_ActualQuantity")) :
                                                            Util.GetValueOfDecimal(po_WrkOdrTrnsctionLine.Get_Value("VAMFG_QtyEntered")), Get_Trx(), out conversionNotFoundInOut))
                                                        {
                                                            if (!conversionNotFoundProductionExecution1.Contains(conversionNotFoundProductionExecution))
                                                            {
                                                                conversionNotFoundProductionExecution1 += conversionNotFoundProductionExecution + " , ";
                                                            }
                                                            _log.Info("Cost not Calculated for Production Execution for this Line ID = " + Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAMFG_M_WrkOdrTrnsctionLine_ID"]));
                                                        }
                                                        else
                                                        {
                                                            if (Util.GetValueOfString(po_WrkOdrTransaction.Get_Value("VAMFG_Description")) != null && Util.GetValueOfString(po_WrkOdrTransaction.Get_Value("VAMFG_Description")).Contains("{->"))
                                                            {
                                                                po_WrkOdrTrnsctionLine.Set_Value("IsReversedCostCalculated", true);
                                                            }
                                                            po_WrkOdrTrnsctionLine.Set_Value("IsCostCalculated", true);
                                                            if (!po_WrkOdrTrnsctionLine.Save(Get_Trx()))
                                                            {
                                                                ValueNamePair pp = VLogger.RetrieveError();
                                                                _log.Info("Error found for Production Execution for this Line ID = " + Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAMFG_M_WrkOdrTrnsctionLine_ID"]) +
                                                                                                                                           " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                Get_Trx().Rollback();
                                                            }
                                                            else
                                                            {
                                                                _log.Fine("Cost Calculation updated for Production Execution line ID = " + Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAMFG_M_WrkOdrTrnsctionLine_ID"]));
                                                                Get_Trx().Commit();
                                                            }
                                                        }
                                                    }
                                                }
                                                catch { }
                                            }
                                        }
                                        sql.Clear();
                                        if (Util.GetValueOfString(po_WrkOdrTransaction.Get_Value("VAMFG_Description")) != null &&
                                            Util.GetValueOfString(po_WrkOdrTransaction.Get_Value("VAMFG_Description")).Contains("{->"))
                                        {
                                            sql.Append(@"SELECT COUNT(*) FROM VAMFG_M_WrkOdrTrnsctionLine WHERE IsReversedCostCalculated = 'N'
                                                     AND IsActive = 'Y' AND VAMFG_M_WrkOdrTransaction_ID = " + Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]));
                                        }
                                        else
                                        {
                                            sql.Append(@"SELECT COUNT(*) FROM VAMFG_M_WrkOdrTrnsctionLine WHERE IsCostCalculated = 'N' AND IsActive = 'Y'
                                           AND VAMFG_M_WrkOdrTransaction_ID = " + Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]));
                                        }
                                        if (Util.GetValueOfInt(DB.ExecuteScalar(sql.ToString(), null, Get_Trx())) <= 0)
                                        {
                                            if (Util.GetValueOfString(po_WrkOdrTransaction.Get_Value("VAMFG_Description")) != null &&
                                                Util.GetValueOfString(po_WrkOdrTransaction.Get_Value("VAMFG_Description")).Contains("{->"))
                                            {
                                                po_WrkOdrTransaction.Set_Value("IsReversedCostCalculated", true);
                                            }
                                            po_WrkOdrTransaction.Set_Value("IsCostCalculated", true);
                                            if (!po_WrkOdrTransaction.Save(Get_Trx()))
                                            {
                                                ValueNamePair pp = VLogger.RetrieveError();
                                                _log.Info("Error found for saving Production execution for this Record ID = " + Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]) +
                                                                                                           " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                            }
                                            else
                                            {
                                                _log.Fine("Cost Calculation updated for Production Execution = " + Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]));
                                                Get_Trx().Commit();
                                            }
                                        }
                                        continue;
                                    }
                                }
                            }
                            catch { }


                            #endregion

                            #region Cost Calculation For Customer Return
                            try
                            {
                                if (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["TableName"]) == "VAM_Inv_InOut" &&
                                   Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["issotrx"]) == "Y" &&
                                   Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["isreturntrx"]) == "Y" &&
                                   Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["docstatus"]) == "RE")
                                {
                                    inout = new MVAMInvInOut(GetCtx(), Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]), Get_Trx());

                                    sql.Clear();
                                    if (inout.GetDescription() != null && inout.GetDescription().Contains("{->"))
                                    {
                                        sql.Append("SELECT * FROM VAM_Inv_InOutLine WHERE IsActive = 'Y' AND iscostcalculated = 'Y'  AND IsReversedCostCalculated = 'N' " +
                                                  " AND VAM_Inv_InOut_ID = " + inout.GetVAM_Inv_InOut_ID() + " ORDER BY Line ");
                                    }
                                    else
                                    {
                                        sql.Append("SELECT * FROM VAM_Inv_InOutLine WHERE IsActive = 'Y' AND iscostcalculated = 'N' " +
                                                     " AND VAM_Inv_InOut_ID = " + inout.GetVAM_Inv_InOut_ID() + " ORDER BY Line ");
                                    }
                                    dsChildRecord = DB.ExecuteDataset(sql.ToString(), null, Get_Trx());
                                    if (dsChildRecord != null && dsChildRecord.Tables.Count > 0 && dsChildRecord.Tables[0].Rows.Count > 0)
                                    {
                                        for (int j = 0; j < dsChildRecord.Tables[0].Rows.Count; j++)
                                        {
                                            try
                                            {
                                                inoutLine = new MVAMInvInOutLine(GetCtx(), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_Inv_InOutLine_ID"]), Get_Trx());
                                                orderLine = new MVABOrderLine(GetCtx(), inoutLine.GetVAB_OrderLine_ID(), null);
                                                // when we void order then we set qty Ordered as 0
                                                if (orderLine != null && orderLine.GetVAB_Order_ID() > 0)
                                                {
                                                    order = new MVABOrder(GetCtx(), orderLine.GetVAB_Order_ID(), null);
                                                    if (order.GetDocStatus() != "VO")
                                                    {
                                                        if (orderLine != null && orderLine.GetVAB_Order_ID() > 0 && orderLine.GetQtyOrdered() == 0)
                                                            continue;
                                                    }
                                                    ProductOrderLineCost = orderLine.GetProductLineCost(orderLine);
                                                    ProductOrderPriceActual = ProductOrderLineCost / orderLine.GetQtyEntered();
                                                }
                                                product = new MVAMProduct(GetCtx(), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_Product_ID"]), Get_Trx());
                                                if (product.GetProductType() == "I") // for Item Type product
                                                {
                                                    #region Customer Return
                                                    if (inout.IsSOTrx() && inout.IsReturnTrx())
                                                    {
                                                        if (inout.GetOrig_Order_ID() <= 0)
                                                        {
                                                            break;
                                                        }

                                                        #region get price from VAM_ProductCost (Current Cost Price)
                                                        if (!client.IsCostImmediate())
                                                        {
                                                            // get price from VAM_ProductCost (Current Cost Price)
                                                            currentCostPrice = MVAMVAMProductCost.GetproductCostAndQtyMaterial(inoutLine.GetVAF_Client_ID(), inoutLine.GetVAF_Org_ID(),
                                                                               inoutLine.GetVAM_Product_ID(), inoutLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), inout.GetVAM_Warehouse_ID(), false);
                                                            DB.ExecuteQuery("UPDATE VAM_Inv_InOutLine SET CurrentCostPrice = " + currentCostPrice +
                                                                            @" WHERE VAM_Inv_InOutLine_ID = " + inoutLine.GetVAM_Inv_InOutLine_ID(), null, Get_Trx());
                                                        }
                                                        #endregion

                                                        if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), inout.GetVAF_Client_ID(), inout.GetVAF_Org_ID(), product, inoutLine.GetVAM_PFeature_SetInstance_ID(),
                                                            "Customer Return", null, inoutLine, null, null, null,
                                                            order.GetDocStatus() != "VO" ? Decimal.Multiply(Decimal.Divide(ProductOrderLineCost, orderLine.GetQtyOrdered()), inoutLine.GetMovementQty())
                                                            : Decimal.Multiply(ProductOrderPriceActual, inoutLine.GetQtyEntered()),
                                                            inoutLine.GetMovementQty(),
                                                              Get_Trx(), out conversionNotFoundInOut))
                                                        {
                                                            if (!conversionNotFoundInOut1.Contains(conversionNotFoundInOut))
                                                            {
                                                                conversionNotFoundInOut1 += conversionNotFoundInOut + " , ";
                                                            }
                                                            _log.Info("Cost not Calculated for Customer Return for this Line ID = " + inoutLine.GetVAM_Inv_InOutLine_ID());
                                                        }
                                                        else
                                                        {
                                                            if (inout.GetDescription() != null && inout.GetDescription().Contains("{->"))
                                                            {
                                                                inoutLine.SetIsReversedCostCalculated(true);
                                                            }
                                                            inoutLine.SetIsCostCalculated(true);
                                                            if (client.IsCostImmediate() && !inoutLine.IsCostImmediate())
                                                            {
                                                                inoutLine.SetIsCostImmediate(true);
                                                            }
                                                            if (!inoutLine.Save(Get_Trx()))
                                                            {
                                                                ValueNamePair pp = VLogger.RetrieveError();
                                                                _log.Info("Error found for Customer Return for this Line ID = " + inoutLine.GetVAM_Inv_InOutLine_ID() +
                                                                                                                                           " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                Get_Trx().Rollback();
                                                            }
                                                            else
                                                            {
                                                                _log.Fine("Cost Calculation updated for VAM_Inv_InOutLine = " + inoutLine.GetVAM_Inv_InOutLine_ID());
                                                                Get_Trx().Commit();
                                                            }
                                                        }
                                                    }
                                                    #endregion
                                                }
                                            }
                                            catch { }
                                        }
                                    }
                                    sql.Clear();
                                    if (inout.GetDescription() != null && inout.GetDescription().Contains("{->"))
                                    {
                                        sql.Append("SELECT COUNT(*) FROM VAM_Inv_InOutLine WHERE IsReversedCostCalculated = 'N' AND IsActive = 'Y' AND VAM_Inv_InOut_ID = " + inout.GetVAM_Inv_InOut_ID());
                                    }
                                    else
                                    {
                                        sql.Append("SELECT COUNT(*) FROM VAM_Inv_InOutLine WHERE IsCostCalculated = 'N' AND IsActive = 'Y' AND VAM_Inv_InOut_ID = " + inout.GetVAM_Inv_InOut_ID());
                                    }
                                    if (Util.GetValueOfInt(DB.ExecuteScalar(sql.ToString(), null, Get_Trx())) <= 0)
                                    {
                                        if (inout.GetDescription() != null && inout.GetDescription().Contains("{->"))
                                        {
                                            inout.SetIsReversedCostCalculated(true);
                                        }
                                        inout.SetIsCostCalculated(true);
                                        if (!inout.Save(Get_Trx()))
                                        {
                                            ValueNamePair pp = VLogger.RetrieveError();
                                            _log.Info("Error found for saving VAM_Inv_InOut for this Record ID = " + inout.GetVAM_Inv_InOut_ID() +
                                                                                                   " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                        }
                                        else
                                        {
                                            _log.Fine("Cost Calculation updated for VAM_Inv_InOut = " + inout.GetVAM_Inv_InOut_ID());
                                            Get_Trx().Commit();
                                        }
                                    }
                                    continue;
                                }
                            }
                            catch { }
                            #endregion

                            #region Cost Calculation For shipment
                            try
                            {
                                if (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["TableName"]) == "VAM_Inv_InOut" &&
                                   Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["issotrx"]) == "Y" &&
                                   Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["isreturntrx"]) == "N" &&
                                   Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["docstatus"]) == "RE")
                                {
                                    inout = new MVAMInvInOut(GetCtx(), Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]), Get_Trx());

                                    sql.Clear();
                                    if (inout.GetDescription() != null && inout.GetDescription().Contains("{->"))
                                    {
                                        sql.Append("SELECT * FROM VAM_Inv_InOutLine WHERE IsActive = 'Y' AND iscostcalculated = 'Y'  AND IsReversedCostCalculated = 'N' " +
                                                  " AND VAM_Inv_InOut_ID = " + inout.GetVAM_Inv_InOut_ID() + " ORDER BY Line ");
                                    }
                                    else
                                    {
                                        sql.Append("SELECT * FROM VAM_Inv_InOutLine WHERE IsActive = 'Y' AND iscostcalculated = 'N' " +
                                                     " AND VAM_Inv_InOut_ID = " + inout.GetVAM_Inv_InOut_ID() + " ORDER BY Line ");
                                    }
                                    dsChildRecord = DB.ExecuteDataset(sql.ToString(), null, Get_Trx());
                                    if (dsChildRecord != null && dsChildRecord.Tables.Count > 0 && dsChildRecord.Tables[0].Rows.Count > 0)
                                    {
                                        for (int j = 0; j < dsChildRecord.Tables[0].Rows.Count; j++)
                                        {
                                            try
                                            {
                                                inoutLine = new MVAMInvInOutLine(GetCtx(), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_Inv_InOutLine_ID"]), Get_Trx());
                                                orderLine = new MVABOrderLine(GetCtx(), inoutLine.GetVAB_OrderLine_ID(), null);
                                                if (orderLine != null && orderLine.GetVAB_Order_ID() > 0)
                                                {
                                                    order = new MVABOrder(GetCtx(), orderLine.GetVAB_Order_ID(), null);
                                                    if (order.GetDocStatus() != "VO")
                                                    {
                                                        if (orderLine != null && orderLine.GetVAB_Order_ID() > 0 && orderLine.GetQtyOrdered() == 0)
                                                            continue;
                                                    }
                                                    ProductOrderLineCost = orderLine.GetProductLineCost(orderLine);
                                                    ProductOrderPriceActual = ProductOrderLineCost / orderLine.GetQtyEntered();
                                                }
                                                product = new MVAMProduct(GetCtx(), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_Product_ID"]), Get_Trx());
                                                if (product.GetProductType() == "I") // for Item Type product
                                                {
                                                    #region shipment
                                                    if (inout.IsSOTrx() && !inout.IsReturnTrx())
                                                    {
                                                        if (inout.GetVAB_Order_ID() <= 0)
                                                        {
                                                            break;
                                                        }

                                                        #region get price from VAM_ProductCost (Current Cost Price)
                                                        if (!client.IsCostImmediate())
                                                        {
                                                            // get price from VAM_ProductCost (Current Cost Price)
                                                            currentCostPrice = 0;
                                                            currentCostPrice = MVAMVAMProductCost.GetproductCosts(inoutLine.GetVAF_Client_ID(), inoutLine.GetVAF_Org_ID(),
                                                                inoutLine.GetVAM_Product_ID(), inoutLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), inout.GetVAM_Warehouse_ID());
                                                            DB.ExecuteQuery("UPDATE VAM_Inv_InOutLine SET CurrentCostPrice = " + currentCostPrice +
                                                                          @" WHERE VAM_Inv_InOutLine_ID = " + inoutLine.GetVAM_Inv_InOutLine_ID(), null, Get_Trx());
                                                        }
                                                        #endregion

                                                        if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), inout.GetVAF_Client_ID(), inout.GetVAF_Org_ID(), product, inoutLine.GetVAM_PFeature_SetInstance_ID(),
                                                             "Shipment", null, inoutLine, null, null, null,
                                                             order.GetDocStatus() != "VO" ? Decimal.Multiply(Decimal.Divide(ProductOrderLineCost, orderLine.GetQtyOrdered()), Decimal.Negate(inoutLine.GetMovementQty()))
                                                             : Decimal.Multiply(ProductOrderPriceActual, Decimal.Negate(inoutLine.GetQtyEntered())),
                                                             Decimal.Negate(inoutLine.GetMovementQty()),
                                                             Get_Trx(), out conversionNotFoundInOut))
                                                        {
                                                            if (!conversionNotFoundInOut1.Contains(conversionNotFoundInOut))
                                                            {
                                                                conversionNotFoundInOut1 += conversionNotFoundInOut + " , ";
                                                            }
                                                            _log.Info("Cost not Calculated for Customer Return for this Line ID = " + inoutLine.GetVAM_Inv_InOutLine_ID());
                                                        }
                                                        else
                                                        {
                                                            if (inout.GetDescription() != null && inout.GetDescription().Contains("{->"))
                                                            {
                                                                inoutLine.SetIsReversedCostCalculated(true);
                                                            }
                                                            inoutLine.SetIsCostCalculated(true);
                                                            if (client.IsCostImmediate() && !inoutLine.IsCostImmediate())
                                                            {
                                                                inoutLine.SetIsCostImmediate(true);
                                                            }
                                                            if (!inoutLine.Save(Get_Trx()))
                                                            {
                                                                ValueNamePair pp = VLogger.RetrieveError();
                                                                _log.Info("Error found for Customer Return for this Line ID = " + inoutLine.GetVAM_Inv_InOutLine_ID() +
                                                                                                                                           " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                Get_Trx().Rollback();
                                                            }
                                                            else
                                                            {
                                                                _log.Fine("Cost Calculation updated for VAM_Inv_InOutLine = " + inoutLine.GetVAM_Inv_InOutLine_ID());
                                                                Get_Trx().Commit();
                                                            }
                                                        }
                                                    }
                                                    #endregion
                                                }
                                            }
                                            catch { }
                                        }
                                    }
                                    sql.Clear();
                                    if (inout.GetDescription() != null && inout.GetDescription().Contains("{->"))
                                    {
                                        sql.Append("SELECT COUNT(*) FROM VAM_Inv_InOutLine WHERE IsReversedCostCalculated = 'N' AND IsActive = 'Y' AND VAM_Inv_InOut_ID = " + inout.GetVAM_Inv_InOut_ID());
                                    }
                                    else
                                    {
                                        sql.Append("SELECT COUNT(*) FROM VAM_Inv_InOutLine WHERE IsCostCalculated = 'N' AND IsActive = 'Y' AND VAM_Inv_InOut_ID = " + inout.GetVAM_Inv_InOut_ID());
                                    }
                                    if (Util.GetValueOfInt(DB.ExecuteScalar(sql.ToString(), null, Get_Trx())) <= 0)
                                    {
                                        if (inout.GetDescription() != null && inout.GetDescription().Contains("{->"))
                                        {
                                            inout.SetIsReversedCostCalculated(true);
                                        }
                                        inout.SetIsCostCalculated(true);
                                        if (!inout.Save(Get_Trx()))
                                        {
                                            ValueNamePair pp = VLogger.RetrieveError();
                                            _log.Info("Error found for saving VAM_Inv_InOut for this Record ID = " + inout.GetVAM_Inv_InOut_ID() +
                                                                                                   " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                        }
                                        else
                                        {
                                            _log.Fine("Cost Calculation updated for VAM_Inv_InOut = " + inout.GetVAM_Inv_InOut_ID());
                                            Get_Trx().Commit();
                                        }
                                    }
                                    continue;
                                }
                            }
                            catch { }
                            #endregion

                            #region Cost Calculation Against AP Credit Memo - During Return Cycle of Purchase - Reverse
                            try
                            {
                                if (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["TableName"]) == "VAM_MatchInvoiceoiceCostTrack" &&
                                    (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["issotrx"]) == "N" &&
                                     Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["isreturntrx"]) == "Y"))
                                {
                                    matchInvCostReverse = new X_VAM_MatchInvoiceCostTrack(GetCtx(), Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]), Get_Trx());
                                    inoutLine = new MVAMInvInOutLine(GetCtx(), matchInvCostReverse.GetVAM_Inv_InOutLine_ID(), Get_Trx());
                                    invoiceLine = new MVABInvoiceLine(GetCtx(), matchInvCostReverse.GetRev_VAB_InvoiceLine_ID(), Get_Trx());
                                    invoice = new MVABInvoice(GetCtx(), invoiceLine.GetVAB_Invoice_ID(), Get_Trx());
                                    ProductInvoiceLineCost = invoiceLine.GetProductLineCost(invoiceLine);

                                    product = new MVAMProduct(GetCtx(), invoiceLine.GetVAM_Product_ID(), Get_Trx());
                                    if (inoutLine.GetVAB_OrderLine_ID() > 0)
                                    {
                                        orderLine = new MVABOrderLine(GetCtx(), inoutLine.GetVAB_OrderLine_ID(), Get_Trx());
                                        order = new MVABOrder(GetCtx(), orderLine.GetVAB_Order_ID(), Get_Trx());
                                    }
                                    if (product.GetProductType() == "I" && product.GetVAM_Product_ID() > 0)
                                    {
                                        if (countColumnExist > 0)
                                        {
                                            isCostAdjustableOnLost = product.IsCostAdjustmentOnLost();
                                        }
                                        // when isCostAdjustableOnLost = true on product and movement qty on MR is less than invoice qty then consider MR qty else invoice qty
                                        if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), invoiceLine.GetVAF_Client_ID(), invoiceLine.GetVAF_Org_ID(), product, invoiceLine.GetVAM_PFeature_SetInstance_ID(),
                                              "Invoice(Vendor)-Return", null, inoutLine, null, invoiceLine, null,
                                            isCostAdjustableOnLost && matchInvCostReverse.GetQty() < Decimal.Negate(invoiceLine.GetQtyInvoiced()) ? Decimal.Negate(ProductInvoiceLineCost) : (Decimal.Multiply(Decimal.Divide(ProductInvoiceLineCost, invoiceLine.GetQtyInvoiced()), matchInvCostReverse.GetQty())),
                                             matchInvCostReverse.GetQty(),
                                              Get_Trx(), out conversionNotFoundInvoice))
                                        {
                                            if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                            {
                                                conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                            }
                                            _log.Info("Cost not Calculated for Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                        }
                                        else
                                        {
                                            invoiceLine.SetIsReversedCostCalculated(true);
                                            invoiceLine.SetIsCostCalculated(true);
                                            if (client.IsCostImmediate() && !invoiceLine.IsCostImmediate())
                                            {
                                                invoiceLine.SetIsCostImmediate(true);
                                            }
                                            if (!invoiceLine.Save(Get_Trx()))
                                            {
                                                ValueNamePair pp = VLogger.RetrieveError();
                                                _log.Info("Error found for saving Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID() +
                                                                                                           " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                Get_Trx().Rollback();
                                            }
                                            else
                                            {
                                                _log.Fine("Cost Calculation updated for m_invoiceline = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                Get_Trx().Commit();

                                                // set is cost calculation true on match invoice
                                                if (!matchInvCostReverse.Delete(true, Get_Trx()))
                                                {
                                                    ValueNamePair pp = VLogger.RetrieveError();
                                                    _log.Info(" Delete Record VAM_MatchInvoiceoiceCostTrack -- Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                    Get_Trx().Rollback();
                                                }
                                                else
                                                {
                                                    Get_Trx().Commit();
                                                }
                                            }
                                        }
                                    }
                                    continue;
                                }
                            }
                            catch { }
                            #endregion

                            #region Cost Calculation For  Return to Vendor
                            try
                            {
                                if (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["TableName"]) == "VAM_Inv_InOut" &&
                                   Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["issotrx"]) == "N" &&
                                   Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["isreturntrx"]) == "Y" &&
                                   Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["docstatus"]) == "RE")
                                {
                                    inout = new MVAMInvInOut(GetCtx(), Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]), Get_Trx());

                                    sql.Clear();
                                    if (inout.GetDescription() != null && inout.GetDescription().Contains("{->"))
                                    {
                                        sql.Append("SELECT * FROM VAM_Inv_InOutLine WHERE IsActive = 'Y' AND iscostcalculated = 'Y'  AND IsReversedCostCalculated = 'N' " +
                                                  " AND VAM_Inv_InOut_ID = " + inout.GetVAM_Inv_InOut_ID() + " ORDER BY Line ");
                                    }
                                    else
                                    {
                                        sql.Append("SELECT * FROM VAM_Inv_InOutLine WHERE IsActive = 'Y' AND iscostcalculated = 'N' " +
                                                     " AND VAM_Inv_InOut_ID = " + inout.GetVAM_Inv_InOut_ID() + " ORDER BY Line ");
                                    }
                                    dsChildRecord = DB.ExecuteDataset(sql.ToString(), null, Get_Trx());
                                    if (dsChildRecord != null && dsChildRecord.Tables.Count > 0 && dsChildRecord.Tables[0].Rows.Count > 0)
                                    {
                                        for (int j = 0; j < dsChildRecord.Tables[0].Rows.Count; j++)
                                        {
                                            try
                                            {
                                                inoutLine = new MVAMInvInOutLine(GetCtx(), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_Inv_InOutLine_ID"]), Get_Trx());
                                                orderLine = new MVABOrderLine(GetCtx(), inoutLine.GetVAB_OrderLine_ID(), null);
                                                if (orderLine != null && orderLine.GetVAB_Order_ID() > 0)
                                                {
                                                    order = new MVABOrder(GetCtx(), orderLine.GetVAB_Order_ID(), null);
                                                    if (order.GetDocStatus() != "VO")
                                                    {
                                                        if (orderLine != null && orderLine.GetVAB_Order_ID() > 0 && orderLine.GetQtyOrdered() == 0)
                                                            continue;
                                                    }
                                                }
                                                product = new MVAMProduct(GetCtx(), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_Product_ID"]), Get_Trx());
                                                if (product.GetProductType() == "I") // for Item Type product
                                                {
                                                    #region  Return To Vendor
                                                    if (!inout.IsSOTrx() && inout.IsReturnTrx())
                                                    {
                                                        if (inout.GetOrig_Order_ID() == 0 || orderLine == null || orderLine.GetVAB_OrderLine_ID() == 0)
                                                        {
                                                            #region Return to Vendor against without Vendor RMA

                                                            #region get price from VAM_ProductCost (Current Cost Price)
                                                            if (!client.IsCostImmediate())
                                                            {
                                                                // get price from VAM_ProductCost (Current Cost Price)
                                                                currentCostPrice = MVAMVAMProductCost.GetproductCosts(inoutLine.GetVAF_Client_ID(), inoutLine.GetVAF_Org_ID(),
                                                                   inoutLine.GetVAM_Product_ID(), inoutLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), inout.GetVAM_Warehouse_ID());
                                                                DB.ExecuteQuery("UPDATE VAM_Inv_InOutLine SET CurrentCostPrice = " + currentCostPrice +
                                                                        @" WHERE VAM_Inv_InOutLine_ID = " + inoutLine.GetVAM_Inv_InOutLine_ID(), null, Get_Trx());
                                                            }
                                                            #endregion

                                                            if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), inout.GetVAF_Client_ID(), inout.GetVAF_Org_ID(), product, inoutLine.GetVAM_PFeature_SetInstance_ID(),
                                                           "Return To Vendor", null, inoutLine, null, null, null, 0, Decimal.Negate(inoutLine.GetMovementQty()), Get_TrxName(), out conversionNotFoundInOut))
                                                            {
                                                                if (!conversionNotFoundInOut1.Contains(conversionNotFoundInOut))
                                                                {
                                                                    conversionNotFoundInOut1 += conversionNotFoundInOut + " , ";
                                                                }
                                                                _log.Info("Cost not Calculated for Return To Vendor for this Line ID = " + inoutLine.GetVAM_Inv_InOutLine_ID());
                                                            }
                                                            else
                                                            {
                                                                if (inout.GetDescription() != null && inout.GetDescription().Contains("{->"))
                                                                {
                                                                    inoutLine.SetIsReversedCostCalculated(true);
                                                                }
                                                                inoutLine.SetIsCostCalculated(true);
                                                                if (client.IsCostImmediate() && !inoutLine.IsCostImmediate())
                                                                {
                                                                    inoutLine.SetIsCostImmediate(true);
                                                                }
                                                                if (!inoutLine.Save(Get_Trx()))
                                                                {
                                                                    ValueNamePair pp = VLogger.RetrieveError();
                                                                    _log.Info("Error found for Return To Vendor for this Line ID = " + inoutLine.GetVAM_Inv_InOutLine_ID() +
                                                                                                                                                   " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                    Get_Trx().Rollback();
                                                                }
                                                                else
                                                                {
                                                                    _log.Fine("Cost Calculation updated for VAM_Inv_InOutLine = " + inoutLine.GetVAM_Inv_InOutLine_ID());
                                                                    Get_Trx().Commit();
                                                                }
                                                            }
                                                            #endregion
                                                        }
                                                        else
                                                        {
                                                            #region Return to Vendor against with Vendor RMA
                                                            amt = 0;
                                                            ProductOrderLineCost = orderLine.GetProductLineCost(orderLine);
                                                            ProductOrderPriceActual = ProductOrderLineCost / orderLine.GetQtyEntered();
                                                            if (isCostAdjustableOnLost && inoutLine.GetMovementQty() < orderLine.GetQtyOrdered() && order.GetDocStatus() != "VO")
                                                            {
                                                                if (inoutLine.GetMovementQty() < 0)
                                                                    amt = ProductOrderLineCost;
                                                                else
                                                                    amt = Decimal.Negate(ProductOrderLineCost);
                                                            }
                                                            else if (!isCostAdjustableOnLost && inoutLine.GetMovementQty() < orderLine.GetQtyOrdered() && order.GetDocStatus() != "VO")
                                                            {
                                                                amt = Decimal.Multiply(Decimal.Divide(ProductOrderLineCost, orderLine.GetQtyOrdered()), Decimal.Negate(inoutLine.GetMovementQty()));
                                                            }
                                                            else if (order.GetDocStatus() != "VO")
                                                            {
                                                                amt = Decimal.Multiply(Decimal.Divide(ProductOrderLineCost, orderLine.GetQtyOrdered()), Decimal.Negate(inoutLine.GetMovementQty()));
                                                            }
                                                            else if (order.GetDocStatus() == "VO")
                                                            {
                                                                amt = Decimal.Multiply(ProductOrderPriceActual, Decimal.Negate(inoutLine.GetQtyEntered()));
                                                            }

                                                            if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), inout.GetVAF_Client_ID(), inout.GetVAF_Org_ID(), product, inoutLine.GetVAM_PFeature_SetInstance_ID(),
                                                                "Return To Vendor", null, inoutLine, null, null, null, amt, Decimal.Negate(inoutLine.GetMovementQty()),
                                                                Get_TrxName(), out conversionNotFoundInOut))
                                                            {
                                                                if (!conversionNotFoundInOut.Contains(conversionNotFoundInOut))
                                                                {
                                                                    conversionNotFoundInOut += conversionNotFoundInOut + " , ";
                                                                }
                                                                _log.Info("Cost not Calculated for Return To Vendor for this Line ID = " + inoutLine.GetVAM_Inv_InOutLine_ID());
                                                            }
                                                            else
                                                            {
                                                                if (inout.GetDescription() != null && inout.GetDescription().Contains("{->"))
                                                                {
                                                                    inoutLine.SetIsReversedCostCalculated(true);
                                                                }
                                                                inoutLine.SetIsCostCalculated(true);
                                                                if (client.IsCostImmediate() && !inoutLine.IsCostImmediate())
                                                                {
                                                                    inoutLine.SetIsCostImmediate(true);
                                                                }
                                                                if (!inoutLine.Save(Get_Trx()))
                                                                {
                                                                    ValueNamePair pp = VLogger.RetrieveError();
                                                                    _log.Info("Error found for Return To Vendor for this Line ID = " + inoutLine.GetVAM_Inv_InOutLine_ID() +
                                                                                                                                                   " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                    Get_Trx().Rollback();
                                                                }
                                                                else
                                                                {
                                                                    _log.Fine("Cost Calculation updated for VAM_Inv_InOutLine = " + inoutLine.GetVAM_Inv_InOutLine_ID());
                                                                    Get_Trx().Commit();
                                                                }
                                                            }
                                                            #endregion
                                                        }
                                                    }
                                                    #endregion
                                                }
                                            }
                                            catch { }
                                        }
                                    }
                                    sql.Clear();
                                    if (inout.GetDescription() != null && inout.GetDescription().Contains("{->"))
                                    {
                                        sql.Append("SELECT COUNT(*) FROM VAM_Inv_InOutLine WHERE IsReversedCostCalculated = 'N' AND IsActive = 'Y' AND VAM_Inv_InOut_ID = " + inout.GetVAM_Inv_InOut_ID());
                                    }
                                    else
                                    {
                                        sql.Append("SELECT COUNT(*) FROM VAM_Inv_InOutLine WHERE IsCostCalculated = 'N' AND IsActive = 'Y' AND VAM_Inv_InOut_ID = " + inout.GetVAM_Inv_InOut_ID());
                                    }
                                    if (Util.GetValueOfInt(DB.ExecuteScalar(sql.ToString(), null, Get_Trx())) <= 0)
                                    {
                                        if (inout.GetDescription() != null && inout.GetDescription().Contains("{->"))
                                        {
                                            inout.SetIsReversedCostCalculated(true);
                                        }
                                        inout.SetIsCostCalculated(true);
                                        if (!inout.Save(Get_Trx()))
                                        {
                                            ValueNamePair pp = VLogger.RetrieveError();
                                            _log.Info("Error found for saving VAM_Inv_InOut for this Record ID = " + inout.GetVAM_Inv_InOut_ID() +
                                                                                                   " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                        }
                                        else
                                        {
                                            _log.Fine("Cost Calculation updated for VAM_Inv_InOut = " + inout.GetVAM_Inv_InOut_ID());
                                            Get_Trx().Commit();
                                        }
                                    }
                                    continue;
                                }
                            }
                            catch { }
                            #endregion

                            #region Cost Calculation for Inventory Move
                            try
                            {
                                if (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["TableName"]) == "VAM_InventoryTransfer" &&
                                  Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["docstatus"]) == "RE")
                                {
                                    movement = new MVAMInventoryTransfer(GetCtx(), Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]), Get_Trx());

                                    sql.Clear();
                                    if (movement.GetDescription() != null && movement.GetDescription().Contains("{->"))
                                    {
                                        sql.Append("SELECT * FROM VAM_InvTrf_Line WHERE IsActive = 'Y' AND iscostcalculated = 'Y'  AND IsReversedCostCalculated = 'N' " +
                                                  " AND VAM_InventoryTransfer_ID = " + movement.GetVAM_InventoryTransfer_ID() + " ORDER BY Line ");
                                    }
                                    else
                                    {
                                        sql.Append("SELECT * FROM VAM_InvTrf_Line WHERE IsActive = 'Y' AND iscostcalculated = 'N' " +
                                                     " AND VAM_InventoryTransfer_ID = " + movement.GetVAM_InventoryTransfer_ID() + " ORDER BY Line ");
                                    }
                                    dsChildRecord = DB.ExecuteDataset(sql.ToString(), null, Get_Trx());
                                    if (dsChildRecord != null && dsChildRecord.Tables.Count > 0 && dsChildRecord.Tables[0].Rows.Count > 0)
                                    {
                                        for (int j = 0; j < dsChildRecord.Tables[0].Rows.Count; j++)
                                        {
                                            product = new MVAMProduct(GetCtx(), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_Product_ID"]), Get_Trx());
                                            movementLine = new MVAMInvTrfLine(GetCtx(), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_InvTrf_Line_ID"]), Get_Trx());

                                            // for Item Type product 
                                            if (product.GetProductType() == "I") // && movement.GetVAF_Org_ID() != warehouse.GetVAF_Org_ID()
                                            {
                                                #region for inventory move

                                                #region get price from VAM_ProductCost (Current Cost Price)
                                                if (!client.IsCostImmediate())
                                                {
                                                    // get price from VAM_ProductCost (Current Cost Price)
                                                    //currentCostPrice = 0;
                                                    //currentCostPrice = MVAMVAMProductCost.GetproductCosts(movementLine.GetVAF_Client_ID(), movementLine.GetVAF_Org_ID(),
                                                    //    movementLine.GetVAM_Product_ID(), movementLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx());
                                                    //movementLine.SetCurrentCostPrice(currentCostPrice);
                                                    //if (!movementLine.Save(Get_Trx()))
                                                    //{
                                                    //    ValueNamePair pp = VLogger.RetrieveError();
                                                    //    _log.Info("Error found for Movement Line for this Line ID = " + movementLine.GetVAM_InvTrf_Line_ID() +
                                                    //               " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                    //    Get_Trx().Rollback();
                                                    //}
                                                }
                                                #endregion

                                                if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), movement.GetVAF_Client_ID(), movement.GetVAF_Org_ID(), product, movementLine.GetVAM_PFeature_SetInstance_ID(),
                                                    "Inventory Move", null, null, movementLine, null, null, 0, movementLine.GetMovementQty(), Get_Trx(), out conversionNotFoundMovement))
                                                {
                                                    if (!conversionNotFoundMovement1.Contains(conversionNotFoundMovement))
                                                    {
                                                        conversionNotFoundMovement1 += conversionNotFoundMovement + " , ";
                                                    }
                                                    _log.Info("Cost not Calculated for Inventory Move for this Line ID = " + movementLine.GetVAM_InvTrf_Line_ID());
                                                }
                                                else
                                                {
                                                    if (movement.GetDescription() != null && movement.GetDescription().Contains("{->"))
                                                    {
                                                        movementLine.SetIsReversedCostCalculated(true);
                                                    }
                                                    movementLine.SetIsCostCalculated(true);
                                                    if (client.IsCostImmediate() && !movementLine.IsCostImmediate())
                                                    {
                                                        movementLine.SetIsCostImmediate(true);
                                                    }
                                                    if (!movementLine.Save(Get_Trx()))
                                                    {
                                                        ValueNamePair pp = VLogger.RetrieveError();
                                                        _log.Info("Error found for saving Inventory Move for this Line ID = " + movementLine.GetVAM_InvTrf_Line_ID() +
                                                                                                                           " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                        Get_Trx().Rollback();
                                                    }
                                                    else
                                                    {
                                                        _log.Fine("Cost Calculation updated for VAM_InvTrf_Line = " + movementLine.GetVAM_InvTrf_Line_ID());
                                                        Get_Trx().Commit();
                                                    }
                                                }
                                                #endregion
                                            }
                                        }
                                    }
                                    sql.Clear();
                                    if (movement.GetDescription() != null && movement.GetDescription().Contains("{->"))
                                    {
                                        sql.Append("SELECT COUNT(*) FROM VAM_InvTrf_Line WHERE IsReversedCostCalculated = 'N' AND IsActive = 'Y' AND VAM_InventoryTransfer_ID = " + movement.GetVAM_InventoryTransfer_ID());
                                    }
                                    else
                                    {
                                        sql.Append("SELECT COUNT(*) FROM VAM_InvTrf_Line WHERE IsCostCalculated = 'N' AND IsActive = 'Y' AND VAM_InventoryTransfer_ID = " + movement.GetVAM_InventoryTransfer_ID());
                                    }
                                    if (Util.GetValueOfInt(DB.ExecuteScalar(sql.ToString(), null, Get_Trx())) <= 0)
                                    {
                                        if (movement.GetDescription() != null && movement.GetDescription().Contains("{->"))
                                        {
                                            movement.SetIsReversedCostCalculated(true);
                                        }
                                        movement.SetIsCostCalculated(true);
                                        if (!movement.Save(Get_Trx()))
                                        {
                                            ValueNamePair pp = VLogger.RetrieveError();
                                            _log.Info("Error found for saving Internal Use Inventory for this Record ID = " + movement.GetVAM_InventoryTransfer_ID() +
                                                                                                   " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                        }
                                        else
                                        {
                                            _log.Fine("Cost Calculation updated for VAM_InventoryTransfer = " + movement.GetVAM_InventoryTransfer_ID());
                                            Get_Trx().Commit();
                                        }
                                    }
                                    continue;
                                }
                            }
                            catch { }
                            #endregion

                            #region Cost Calculation for  Internal use inventory
                            try
                            {
                                if (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["TableName"]) == "VAM_Inventory" &&
                                   Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["docstatus"]) == "RE" &&
                                   Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["IsInternalUse"]) == "Y")
                                {
                                    inventory = new MVAMInventory(GetCtx(), Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]), Get_Trx());
                                    sql.Clear();
                                    if (inventory.GetDescription() != null && inventory.GetDescription().Contains("{->"))
                                    {
                                        sql.Append("SELECT * FROM VAM_InventoryLine WHERE IsActive = 'Y' AND iscostcalculated = 'Y'  AND IsReversedCostCalculated = 'N' " +
                                                  " AND VAM_Inventory_ID = " + inventory.GetVAM_Inventory_ID() + " ORDER BY Line ");
                                    }
                                    else
                                    {
                                        sql.Append("SELECT * FROM VAM_InventoryLine WHERE IsActive = 'Y' AND iscostcalculated = 'N' " +
                                                     " AND VAM_Inventory_ID = " + inventory.GetVAM_Inventory_ID() + " ORDER BY Line ");
                                    }
                                    dsChildRecord = DB.ExecuteDataset(sql.ToString(), null, Get_Trx());
                                    if (dsChildRecord != null && dsChildRecord.Tables.Count > 0 && dsChildRecord.Tables[0].Rows.Count > 0)
                                    {
                                        for (int j = 0; j < dsChildRecord.Tables[0].Rows.Count; j++)
                                        {
                                            product = new MVAMProduct(GetCtx(), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_Product_ID"]), Get_Trx());
                                            inventoryLine = new MVAMInventoryLine(GetCtx(), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_InventoryLine_ID"]), Get_Trx());
                                            if (product.GetProductType() == "I") // for Item Type product
                                            {
                                                quantity = 0;
                                                if (inventory.IsInternalUse())
                                                {
                                                    #region for Internal use inventory

                                                    #region get price from VAM_ProductCost (Current Cost Price)
                                                    if (!client.IsCostImmediate())
                                                    {
                                                        // get price from VAM_ProductCost (Current Cost Price)
                                                        //currentCostPrice = 0;
                                                        //currentCostPrice = MVAMVAMProductCost.GetproductCosts(inventoryLine.GetVAF_Client_ID(), inventoryLine.GetVAF_Org_ID(),
                                                        //    inventoryLine.GetVAM_Product_ID(), inventoryLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx());
                                                        //inventoryLine.SetCurrentCostPrice(currentCostPrice);
                                                        //if (!inventoryLine.Save(Get_Trx()))
                                                        //{
                                                        //    ValueNamePair pp = VLogger.RetrieveError();
                                                        //    _log.Info("Error found for Internal Use Inventory Line for this Line ID = " + inventoryLine.GetVAM_InventoryLine_ID() +
                                                        //               " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                        //    Get_Trx().Rollback();
                                                        //}
                                                    }
                                                    #endregion

                                                    quantity = Decimal.Negate(inventoryLine.GetQtyInternalUse());
                                                    // Change by mohit - Client id and organization was passed from context but neede to be passed from document itself as done in several other documents.-27/06/2017
                                                    if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), inventory.GetVAF_Client_ID(), inventory.GetVAF_Org_ID(), product, inventoryLine.GetVAM_PFeature_SetInstance_ID(),
                                                   "Internal Use Inventory", inventoryLine, null, null, null, null, 0, quantity, Get_Trx(), out conversionNotFoundInventory))
                                                    {
                                                        if (!conversionNotFoundInventory1.Contains(conversionNotFoundInventory))
                                                        {
                                                            conversionNotFoundInventory1 += conversionNotFoundInventory + " , ";
                                                        }
                                                        _log.Info("Cost not Calculated for Internal Use Inventory for this Line ID = " + inventoryLine.GetVAM_InventoryLine_ID());
                                                    }
                                                    else
                                                    {
                                                        if (inventory.GetDescription() != null && inventory.GetDescription().Contains("{->"))
                                                        {
                                                            inventoryLine.SetIsReversedCostCalculated(true);
                                                        }
                                                        inventoryLine.SetIsCostCalculated(true);
                                                        if (client.IsCostImmediate() && !inventoryLine.IsCostImmediate())
                                                        {
                                                            inventoryLine.SetIsCostImmediate(true);
                                                        }
                                                        if (!inventoryLine.Save(Get_Trx()))
                                                        {
                                                            ValueNamePair pp = VLogger.RetrieveError();
                                                            _log.Info("Error found for saving Internal Use Inventory for this Line ID = " + inventoryLine.GetVAM_InventoryLine_ID() +
                                                                                                                                   " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                            Get_Trx().Rollback();
                                                        }
                                                        else
                                                        {
                                                            _log.Fine("Cost Calculation updated for VAM_InventoryLine = " + inventoryLine.GetVAM_InventoryLine_ID());
                                                            Get_Trx().Commit();
                                                        }
                                                    }
                                                    #endregion
                                                }
                                                else
                                                {
                                                    #region for Physical Inventory

                                                    #region get price from VAM_ProductCost (Current Cost Price)
                                                    if (!client.IsCostImmediate())
                                                    {
                                                        // get price from VAM_ProductCost (Current Cost Price)
                                                        //currentCostPrice = 0;
                                                        //currentCostPrice = MVAMVAMProductCost.GetproductCosts(inventoryLine.GetVAF_Client_ID(), inventoryLine.GetVAF_Org_ID(),
                                                        //    inventoryLine.GetVAM_Product_ID(), inventoryLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx());
                                                        //inventoryLine.SetCurrentCostPrice(currentCostPrice);
                                                        //if (!inventoryLine.Save(Get_Trx()))
                                                        //{
                                                        //    ValueNamePair pp = VLogger.RetrieveError();
                                                        //    _log.Info("Error found for Physical Line for this Line ID = " + inventoryLine.GetVAM_InventoryLine_ID() +
                                                        //               " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                        //    Get_Trx().Rollback();
                                                        //}
                                                    }
                                                    #endregion

                                                    quantity = Decimal.Subtract(inventoryLine.GetQtyCount(), inventoryLine.GetQtyBook());
                                                    if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), inventory.GetVAF_Client_ID(), inventory.GetVAF_Org_ID(), product, inventoryLine.GetVAM_PFeature_SetInstance_ID(),
                                                   "Physical Inventory", inventoryLine, null, null, null, null, 0, quantity, Get_Trx(), out conversionNotFoundInventory))
                                                    {
                                                        if (!conversionNotFoundInventory1.Contains(conversionNotFoundInventory))
                                                        {
                                                            conversionNotFoundInventory1 += conversionNotFoundInventory + " , ";
                                                        }
                                                        _log.Info("Cost not Calculated for Physical Inventory for this Line ID = " + inventoryLine.GetVAM_InventoryLine_ID());
                                                    }
                                                    else
                                                    {
                                                        if (inventory.GetDescription() != null && inventory.GetDescription().Contains("{->"))
                                                        {
                                                            inventoryLine.SetIsReversedCostCalculated(true);
                                                        }
                                                        inventoryLine.SetIsCostCalculated(true);
                                                        if (client.IsCostImmediate() && !inventoryLine.IsCostImmediate())
                                                        {
                                                            inventoryLine.SetIsCostImmediate(true);
                                                        }
                                                        if (!inventoryLine.Save(Get_Trx()))
                                                        {
                                                            ValueNamePair pp = VLogger.RetrieveError();
                                                            _log.Info("Error found for saving Internal Use Inventory for this Line ID = " + inventoryLine.GetVAM_InventoryLine_ID() +
                                                                                                                                   " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                            Get_Trx().Rollback();
                                                        }
                                                        else
                                                        {
                                                            _log.Fine("Cost Calculation updated for VAM_InventoryLine = " + inventoryLine.GetVAM_InventoryLine_ID());
                                                            Get_Trx().Commit();
                                                        }
                                                    }
                                                    #endregion
                                                }
                                            }
                                        }
                                    }
                                    sql.Clear();
                                    if (inventory.GetDescription() != null && inventory.GetDescription().Contains("{->"))
                                    {
                                        sql.Append("SELECT COUNT(*) FROM VAM_InventoryLine WHERE IsReversedCostCalculated = 'N' AND IsActive = 'Y' AND VAM_Inventory_ID = " + inventory.GetVAM_Inventory_ID());
                                    }
                                    else
                                    {
                                        sql.Append("SELECT COUNT(*) FROM VAM_InventoryLine WHERE IsCostCalculated = 'N' AND IsActive = 'Y' AND VAM_Inventory_ID = " + inventory.GetVAM_Inventory_ID());
                                    }
                                    if (Util.GetValueOfInt(DB.ExecuteScalar(sql.ToString(), null, Get_Trx())) <= 0)
                                    {
                                        if (inventory.GetDescription() != null && inventory.GetDescription().Contains("{->"))
                                        {
                                            inventory.SetIsReversedCostCalculated(true);
                                        }
                                        inventory.SetIsCostCalculated(true);
                                        if (!inventory.Save(Get_Trx()))
                                        {
                                            ValueNamePair pp = VLogger.RetrieveError();
                                            if (pp != null)
                                                _log.Info("Error found for saving Internal Use Inventory for this Record ID = " + inventory.GetVAM_Inventory_ID() +
                                                           " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                        }
                                        else
                                        {
                                            _log.Fine("Cost Calculation updated for VAM_Inventory = " + inventory.GetVAM_Inventory_ID());
                                            Get_Trx().Commit();
                                        }
                                    }
                                    continue;
                                }
                            }
                            catch { }
                            #endregion

                            #region Cost Calculation for Physical Inventory
                            try
                            {
                                if (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["TableName"]) == "VAM_Inventory" &&
                                  Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["docstatus"]) == "RE" &&
                                  Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["IsInternalUse"]) == "N")
                                {
                                    inventory = new MVAMInventory(GetCtx(), Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]), Get_Trx());
                                    sql.Clear();
                                    if (inventory.GetDescription() != null && inventory.GetDescription().Contains("{->"))
                                    {
                                        sql.Append("SELECT * FROM VAM_InventoryLine WHERE IsActive = 'Y' AND iscostcalculated = 'Y'  AND IsReversedCostCalculated = 'N' " +
                                                  " AND VAM_Inventory_ID = " + inventory.GetVAM_Inventory_ID() + " ORDER BY Line ");
                                    }
                                    else
                                    {
                                        sql.Append("SELECT * FROM VAM_InventoryLine WHERE IsActive = 'Y' AND iscostcalculated = 'N' " +
                                                     " AND VAM_Inventory_ID = " + inventory.GetVAM_Inventory_ID() + " ORDER BY Line ");
                                    }
                                    dsChildRecord = DB.ExecuteDataset(sql.ToString(), null, Get_Trx());
                                    if (dsChildRecord != null && dsChildRecord.Tables.Count > 0 && dsChildRecord.Tables[0].Rows.Count > 0)
                                    {
                                        for (int j = 0; j < dsChildRecord.Tables[0].Rows.Count; j++)
                                        {
                                            product = new MVAMProduct(GetCtx(), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_Product_ID"]), Get_Trx());
                                            inventoryLine = new MVAMInventoryLine(GetCtx(), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_InventoryLine_ID"]), Get_Trx());
                                            if (product.GetProductType() == "I") // for Item Type product
                                            {
                                                quantity = 0;
                                                if (inventory.IsInternalUse())
                                                {
                                                    #region for Internal use inventory

                                                    #region get price from VAM_ProductCost (Current Cost Price)
                                                    if (!client.IsCostImmediate())
                                                    {
                                                        // get price from VAM_ProductCost (Current Cost Price)
                                                        //currentCostPrice = 0;
                                                        //currentCostPrice = MVAMVAMProductCost.GetproductCosts(inventoryLine.GetVAF_Client_ID(), inventoryLine.GetVAF_Org_ID(),
                                                        //    inventoryLine.GetVAM_Product_ID(), inventoryLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx());
                                                        //inventoryLine.SetCurrentCostPrice(currentCostPrice);
                                                        //if (!inventoryLine.Save(Get_Trx()))
                                                        //{
                                                        //    ValueNamePair pp = VLogger.RetrieveError();
                                                        //    _log.Info("Error found for Internal Use Inventory Line for this Line ID = " + inventoryLine.GetVAM_InventoryLine_ID() +
                                                        //               " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                        //    Get_Trx().Rollback();
                                                        //}
                                                    }
                                                    #endregion

                                                    quantity = Decimal.Negate(inventoryLine.GetQtyInternalUse());
                                                    // Change by mohit - Client id and organization was passed from context but neede to be passed from document itself as done in several other documents.-27/06/2017
                                                    if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), inventory.GetVAF_Client_ID(), inventory.GetVAF_Org_ID(), product, inventoryLine.GetVAM_PFeature_SetInstance_ID(),
                                                   "Internal Use Inventory", inventoryLine, null, null, null, null, 0, quantity, Get_Trx(), out conversionNotFoundInventory))
                                                    {
                                                        if (!conversionNotFoundInventory1.Contains(conversionNotFoundInventory))
                                                        {
                                                            conversionNotFoundInventory1 += conversionNotFoundInventory + " , ";
                                                        }
                                                        _log.Info("Cost not Calculated for Internal Use Inventory for this Line ID = " + inventoryLine.GetVAM_InventoryLine_ID());
                                                    }
                                                    else
                                                    {
                                                        if (inventory.GetDescription() != null && inventory.GetDescription().Contains("{->"))
                                                        {
                                                            inventoryLine.SetIsReversedCostCalculated(true);
                                                        }
                                                        inventoryLine.SetIsCostCalculated(true);
                                                        if (client.IsCostImmediate() && !inventoryLine.IsCostImmediate())
                                                        {
                                                            inventoryLine.SetIsCostImmediate(true);
                                                        }
                                                        if (!inventoryLine.Save(Get_Trx()))
                                                        {
                                                            ValueNamePair pp = VLogger.RetrieveError();
                                                            _log.Info("Error found for saving Internal Use Inventory for this Line ID = " + inventoryLine.GetVAM_InventoryLine_ID() +
                                                                                                                                   " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                            Get_Trx().Rollback();
                                                        }
                                                        else
                                                        {
                                                            _log.Fine("Cost Calculation updated for VAM_InventoryLine = " + inventoryLine.GetVAM_InventoryLine_ID());
                                                            Get_Trx().Commit();
                                                        }
                                                    }
                                                    #endregion
                                                }
                                                else
                                                {
                                                    #region for Physical Inventory

                                                    #region get price from VAM_ProductCost (Current Cost Price)
                                                    if (!client.IsCostImmediate())
                                                    {
                                                        // get price from VAM_ProductCost (Current Cost Price)
                                                        //currentCostPrice = 0;
                                                        //currentCostPrice = MVAMVAMProductCost.GetproductCosts(inventoryLine.GetVAF_Client_ID(), inventoryLine.GetVAF_Org_ID(),
                                                        //    inventoryLine.GetVAM_Product_ID(), inventoryLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx());
                                                        //inventoryLine.SetCurrentCostPrice(currentCostPrice);
                                                        //if (!inventoryLine.Save(Get_Trx()))
                                                        //{
                                                        //    ValueNamePair pp = VLogger.RetrieveError();
                                                        //    _log.Info("Error found for Physical Inventory Line for this Line ID = " + inventoryLine.GetVAM_InventoryLine_ID() +
                                                        //               " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                        //    Get_Trx().Rollback();
                                                        //}
                                                    }
                                                    #endregion

                                                    quantity = Decimal.Subtract(inventoryLine.GetQtyCount(), inventoryLine.GetQtyBook());
                                                    if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), inventory.GetVAF_Client_ID(), inventory.GetVAF_Org_ID(), product, inventoryLine.GetVAM_PFeature_SetInstance_ID(),
                                                   "Physical Inventory", inventoryLine, null, null, null, null, 0, quantity, Get_Trx(), out conversionNotFoundInventory))
                                                    {
                                                        if (!conversionNotFoundInventory1.Contains(conversionNotFoundInventory))
                                                        {
                                                            conversionNotFoundInventory1 += conversionNotFoundInventory + " , ";
                                                        }
                                                        _log.Info("Cost not Calculated for Physical Inventory for this Line ID = " + inventoryLine.GetVAM_InventoryLine_ID());
                                                    }
                                                    else
                                                    {
                                                        if (inventory.GetDescription() != null && inventory.GetDescription().Contains("{->"))
                                                        {
                                                            inventoryLine.SetIsReversedCostCalculated(true);
                                                        }
                                                        inventoryLine.SetIsCostCalculated(true);
                                                        if (client.IsCostImmediate() && !inventoryLine.IsCostImmediate())
                                                        {
                                                            inventoryLine.SetIsCostImmediate(true);
                                                        }
                                                        if (!inventoryLine.Save(Get_Trx()))
                                                        {
                                                            ValueNamePair pp = VLogger.RetrieveError();
                                                            _log.Info("Error found for saving Internal Use Inventory for this Line ID = " + inventoryLine.GetVAM_InventoryLine_ID() +
                                                                                                                                   " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                            Get_Trx().Rollback();
                                                        }
                                                        else
                                                        {
                                                            _log.Fine("Cost Calculation updated for VAM_InventoryLine = " + inventoryLine.GetVAM_InventoryLine_ID());
                                                            Get_Trx().Commit();
                                                        }
                                                    }
                                                    #endregion
                                                }
                                            }
                                        }
                                    }
                                    sql.Clear();
                                    if (inventory.GetDescription() != null && inventory.GetDescription().Contains("{->"))
                                    {
                                        sql.Append("SELECT COUNT(*) FROM VAM_InventoryLine WHERE IsReversedCostCalculated = 'N' AND IsActive = 'Y' AND VAM_Inventory_ID = " + inventory.GetVAM_Inventory_ID());
                                    }
                                    else
                                    {
                                        sql.Append("SELECT COUNT(*) FROM VAM_InventoryLine WHERE IsCostCalculated = 'N' AND IsActive = 'Y' AND VAM_Inventory_ID = " + inventory.GetVAM_Inventory_ID());
                                    }
                                    if (Util.GetValueOfInt(DB.ExecuteScalar(sql.ToString(), null, Get_Trx())) <= 0)
                                    {
                                        if (inventory.GetDescription() != null && inventory.GetDescription().Contains("{->"))
                                        {
                                            inventory.SetIsReversedCostCalculated(true);
                                        }
                                        inventory.SetIsCostCalculated(true);
                                        if (!inventory.Save(Get_Trx()))
                                        {
                                            ValueNamePair pp = VLogger.RetrieveError();
                                            if (pp != null)
                                                _log.Info("Error found for saving Internal Use Inventory for this Record ID = " + inventory.GetVAM_Inventory_ID() +
                                                           " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                        }
                                        else
                                        {
                                            _log.Fine("Cost Calculation updated for VAM_Inventory = " + inventory.GetVAM_Inventory_ID());
                                            Get_Trx().Commit();
                                        }
                                    }
                                    continue;
                                }
                            }
                            catch { }
                            #endregion

                            #region Cost Calculation for  PO Cycle Reverse
                            try
                            {
                                if (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["TableName"]) == "VAM_MatchInvoiceoiceCostTrack" &&
                                    (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["issotrx"]) == "N" &&
                                     Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["isreturntrx"]) == "N"))
                                {
                                    matchInvCostReverse = new X_VAM_MatchInvoiceCostTrack(GetCtx(), Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]), Get_Trx());
                                    inoutLine = new MVAMInvInOutLine(GetCtx(), matchInvCostReverse.GetVAM_Inv_InOutLine_ID(), Get_Trx());
                                    invoiceLine = new MVABInvoiceLine(GetCtx(), matchInvCostReverse.GetRev_VAB_InvoiceLine_ID(), Get_Trx());
                                    invoice = new MVABInvoice(GetCtx(), invoiceLine.GetVAB_Invoice_ID(), Get_Trx());
                                    ProductInvoiceLineCost = invoiceLine.GetProductLineCost(invoiceLine);

                                    product = new MVAMProduct(GetCtx(), invoiceLine.GetVAM_Product_ID(), Get_Trx());
                                    if (inoutLine.GetVAB_OrderLine_ID() > 0)
                                    {
                                        orderLine = new MVABOrderLine(GetCtx(), inoutLine.GetVAB_OrderLine_ID(), Get_Trx());
                                        order = new MVABOrder(GetCtx(), orderLine.GetVAB_Order_ID(), Get_Trx());
                                    }
                                    if (product.GetProductType() == "I" && product.GetVAM_Product_ID() > 0)
                                    {
                                        if (countColumnExist > 0)
                                        {
                                            isCostAdjustableOnLost = product.IsCostAdjustmentOnLost();
                                        }
                                        // when isCostAdjustableOnLost = true on product and movement qty on MR is less than invoice qty then consider MR qty else invoice qty
                                        if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), invoiceLine.GetVAF_Client_ID(), invoiceLine.GetVAF_Org_ID(), product, invoiceLine.GetVAM_PFeature_SetInstance_ID(),
                                              "Invoice(Vendor)", null, inoutLine, null, invoiceLine, null,
                                            isCostAdjustableOnLost && matchInvCostReverse.GetQty() < Decimal.Negate(invoiceLine.GetQtyInvoiced()) ? ProductInvoiceLineCost : Decimal.Negate(Decimal.Multiply(Decimal.Divide(ProductInvoiceLineCost, invoiceLine.GetQtyInvoiced()), matchInvCostReverse.GetQty())),
                                             decimal.Negate(matchInvCostReverse.GetQty()),
                                              Get_Trx(), out conversionNotFoundInvoice))
                                        {
                                            if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                            {
                                                conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                            }
                                            _log.Info("Cost not Calculated for Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                        }
                                        else
                                        {
                                            invoiceLine.SetIsReversedCostCalculated(true);
                                            invoiceLine.SetIsCostCalculated(true);
                                            if (client.IsCostImmediate() && !invoiceLine.IsCostImmediate())
                                            {
                                                invoiceLine.SetIsCostImmediate(true);
                                            }
                                            if (!invoiceLine.Save(Get_Trx()))
                                            {
                                                ValueNamePair pp = VLogger.RetrieveError();
                                                _log.Info("Error found for saving Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID() +
                                                                                                           " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                Get_Trx().Rollback();
                                            }
                                            else
                                            {
                                                _log.Fine("Cost Calculation updated for m_invoiceline = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                Get_Trx().Commit();

                                                // update the Post current price after Invoice receving on inoutline
                                                DB.ExecuteQuery(@"UPDATE VAM_Inv_InOutLine SET PostCurrentCostPrice = 0 
                                                                  WHERE VAM_Inv_InOutLine_ID = " + matchInvCostReverse.GetVAM_Inv_InOutLine_ID(), null, Get_Trx());

                                                // set is cost calculation true on match invoice
                                                if (!matchInvCostReverse.Delete(true, Get_Trx()))
                                                {
                                                    ValueNamePair pp = VLogger.RetrieveError();
                                                    _log.Info(" Delete Record VAM_MatchInvoiceoiceCostTrack -- Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                    Get_Trx().Rollback();
                                                }
                                                else
                                                {
                                                    Get_Trx().Commit();
                                                }
                                            }
                                        }
                                    }
                                    continue;
                                }
                            }
                            catch { }
                            #endregion

                            #region Cost Calculation for SO / PO / CRMA / VRMA
                            try
                            {
                                if (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["TableName"]) == "VAB_Invoice" &&
                                   Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["docstatus"]) == "RE")
                                {
                                    invoice = new MVABInvoice(GetCtx(), Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]), Get_Trx());

                                    sql.Clear();
                                    if (invoice.GetDescription() != null && invoice.GetDescription().Contains("{->"))
                                    {
                                        sql.Append("SELECT * FROM VAB_InvoiceLine WHERE IsActive = 'Y' AND iscostcalculated = 'Y' AND IsReversedCostCalculated = 'N' " +
                                                     " AND VAB_Invoice_ID = " + invoice.GetVAB_Invoice_ID() + " ORDER BY VAM_Product_ID DESC ");
                                    }
                                    else
                                    {
                                        sql.Append("SELECT * FROM VAB_InvoiceLine WHERE IsActive = 'Y' AND iscostcalculated = 'N' " +
                                                     " AND VAB_Invoice_ID = " + invoice.GetVAB_Invoice_ID() + " ORDER BY VAM_Product_ID DESC ");
                                    }
                                    dsChildRecord = DB.ExecuteDataset(sql.ToString(), null, Get_Trx());
                                    if (dsChildRecord != null && dsChildRecord.Tables.Count > 0 && dsChildRecord.Tables[0].Rows.Count > 0)
                                    {
                                        for (int j = 0; j < dsChildRecord.Tables[0].Rows.Count; j++)
                                        {
                                            try
                                            {
                                                product = new MVAMProduct(GetCtx(), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_Product_ID"]), Get_Trx());
                                                invoiceLine = new MVABInvoiceLine(GetCtx(), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAB_InvoiceLine_ID"]), Get_Trx());
                                                if (invoiceLine != null && invoiceLine.GetVAB_Invoice_ID() > 0 && invoiceLine.GetQtyInvoiced() == 0)
                                                    continue;

                                                ProductInvoiceLineCost = invoiceLine.GetProductLineCost(invoiceLine);

                                                if (invoiceLine.GetVAB_OrderLine_ID() > 0)
                                                {
                                                    if (invoiceLine.GetVAB_Charge_ID() > 0)
                                                    {
                                                        #region Landed Cost Allocation
                                                        if (!invoice.IsSOTrx() && !invoice.IsReturnTrx())
                                                        {
                                                            if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), invoice.GetVAF_Client_ID(), invoice.GetVAF_Org_ID(), null, 0, "Invoice(Vendor)",
                                                                null, null, null, invoiceLine, null, ProductInvoiceLineCost, 0, Get_Trx(), out conversionNotFoundInvoice))
                                                            {
                                                                if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                                                {
                                                                    conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                                                }
                                                                _log.Info("Cost not Calculated for Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                            }
                                                            else
                                                            {
                                                                if (invoice.GetDescription() != null && invoice.GetDescription().Contains("{->"))
                                                                {
                                                                    invoiceLine.SetIsReversedCostCalculated(true);
                                                                }
                                                                invoiceLine.SetIsCostCalculated(true);
                                                                if (client.IsCostImmediate() && !invoiceLine.IsCostImmediate())
                                                                {
                                                                    invoiceLine.SetIsCostImmediate(true);
                                                                }
                                                                if (!invoiceLine.Save(Get_Trx()))
                                                                {
                                                                    ValueNamePair pp = VLogger.RetrieveError();
                                                                    _log.Info("Error found for saving Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID() +
                                                                                                                                                   " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                    Get_Trx().Rollback();
                                                                }
                                                                else
                                                                {
                                                                    _log.Fine("Cost Calculation updated for VAB_InvoiceLine = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                                    Get_Trx().Commit();
                                                                }
                                                            }
                                                        }
                                                        #endregion
                                                    }
                                                    else
                                                    {
                                                        #region for Expense type product
                                                        if (product.GetProductType() == "E" && product.GetVAM_Product_ID() > 0)
                                                        {
                                                            if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), invoice.GetVAF_Client_ID(), invoice.GetVAF_Org_ID(), product, 0,
                                                                 "Invoice(Vendor)", null, null, null, invoiceLine, null, ProductInvoiceLineCost, 0, Get_Trx(), out conversionNotFoundInvoice))
                                                            {
                                                                if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                                                {
                                                                    conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                                                }
                                                                _log.Info("Cost not Calculated for Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                            }
                                                            else
                                                            {
                                                                if (invoice.GetDescription() != null && invoice.GetDescription().Contains("{->"))
                                                                {
                                                                    invoiceLine.SetIsReversedCostCalculated(true);
                                                                }
                                                                invoiceLine.SetIsCostCalculated(true);
                                                                if (client.IsCostImmediate() && !invoiceLine.IsCostImmediate())
                                                                {
                                                                    invoiceLine.SetIsCostImmediate(true);
                                                                }
                                                                if (!invoiceLine.Save(Get_Trx()))
                                                                {
                                                                    ValueNamePair pp = VLogger.RetrieveError();
                                                                    _log.Info("Error found for saving Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID() +
                                                                                                                                                   " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                    Get_Trx().Rollback();
                                                                }
                                                                else
                                                                {
                                                                    _log.Fine("Cost Calculation updated for VAB_InvoiceLine = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                                    Get_Trx().Commit();
                                                                }
                                                            }
                                                        }
                                                        #endregion

                                                        #region  for Item Type product
                                                        else if (product.GetProductType() == "I" && product.GetVAM_Product_ID() > 0)
                                                        {
                                                            if (countColumnExist > 0)
                                                            {
                                                                isCostAdjustableOnLost = product.IsCostAdjustmentOnLost();
                                                            }

                                                            MVABOrder order1 = new MVABOrder(GetCtx(), invoice.GetVAB_Order_ID(), Get_Trx());
                                                            if (order1.GetVAB_Order_ID() == 0)
                                                            {
                                                                MVABOrderLine ol1 = new MVABOrderLine(GetCtx(), invoiceLine.GetVAB_OrderLine_ID(), Get_Trx());
                                                                order1 = new MVABOrder(GetCtx(), ol1.GetVAB_Order_ID(), Get_Trx());
                                                            }

                                                            #region  Sales Order
                                                            if (order1.IsSOTrx() && !order1.IsReturnTrx())
                                                            {
                                                                if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), invoice.GetVAF_Client_ID(), invoice.GetVAF_Org_ID(), product, invoiceLine.GetVAM_PFeature_SetInstance_ID(),
                                                                      "Invoice(Customer)", null, null, null, invoiceLine, null, Decimal.Negate(ProductInvoiceLineCost), Decimal.Negate(invoiceLine.GetQtyInvoiced()),
                                                                      Get_Trx(), out conversionNotFoundInvoice))
                                                                {
                                                                    if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                                                    {
                                                                        conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                                                    }
                                                                    _log.Info("Cost not Calculated for Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                                }
                                                                else
                                                                {
                                                                    if (invoice.GetDescription() != null && invoice.GetDescription().Contains("{->"))
                                                                    {
                                                                        invoiceLine.SetIsReversedCostCalculated(true);
                                                                    }
                                                                    invoiceLine.SetIsCostCalculated(true);
                                                                    if (client.IsCostImmediate() && !invoiceLine.IsCostImmediate())
                                                                    {
                                                                        invoiceLine.SetIsCostImmediate(true);
                                                                    }
                                                                    if (!invoiceLine.Save(Get_Trx()))
                                                                    {
                                                                        ValueNamePair pp = VLogger.RetrieveError();
                                                                        _log.Info("Error found for saving Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID() +
                                                                                                                                                           " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                        Get_Trx().Rollback();
                                                                    }
                                                                    else
                                                                    {
                                                                        _log.Fine("Cost Calculation updated for VAB_InvoiceLine = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                                        Get_Trx().Commit();
                                                                    }
                                                                }
                                                            }
                                                            #endregion

                                                            #region Purchase Order
                                                            else if (!order1.IsSOTrx() && !order1.IsReturnTrx() && 0 == 1)
                                                            {
                                                                // when isCostAdjustableOnLost = true on product and movement qty on MR is less than invoice qty then consider MR qty else invoice qty
                                                                if (invoiceLine.GetVAM_Inv_InOutLine_ID() > 0)
                                                                {
                                                                    inoutLine = new MVAMInvInOutLine(GetCtx(), invoiceLine.GetVAM_Inv_InOutLine_ID(), Get_Trx());
                                                                }
                                                                if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), invoice.GetVAF_Client_ID(), invoice.GetVAF_Org_ID(), product, invoiceLine.GetVAM_PFeature_SetInstance_ID(),
                                                                      "Invoice(Vendor)", null, null, null, invoiceLine, null, ProductInvoiceLineCost,
                                                                      countColumnExist > 0 && isCostAdjustableOnLost && invoiceLine.GetVAM_Inv_InOutLine_ID() > 0 && inoutLine.GetMovementQty() < (invoice.GetDescription() != null && invoice.GetDescription().Contains("{->") ? decimal.Negate(invoiceLine.GetQtyInvoiced()) : invoiceLine.GetQtyInvoiced()) ? (invoice.GetDescription() != null && invoice.GetDescription().Contains("{->") ? Decimal.Negate(inoutLine.GetMovementQty()) : inoutLine.GetMovementQty()) : invoiceLine.GetQtyInvoiced(),
                                                                      Get_Trx(), out conversionNotFoundInvoice))
                                                                {
                                                                    if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                                                    {
                                                                        conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                                                    }
                                                                    _log.Info("Cost not Calculated for Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                                }
                                                                else
                                                                {
                                                                    if (invoice.GetDescription() != null && invoice.GetDescription().Contains("{->"))
                                                                    {
                                                                        invoiceLine.SetIsReversedCostCalculated(true);
                                                                    }
                                                                    invoiceLine.SetIsCostCalculated(true);
                                                                    if (client.IsCostImmediate() && !invoiceLine.IsCostImmediate())
                                                                    {
                                                                        invoiceLine.SetIsCostImmediate(true);
                                                                    }
                                                                    if (!invoiceLine.Save(Get_Trx()))
                                                                    {
                                                                        ValueNamePair pp = VLogger.RetrieveError();
                                                                        _log.Info("Error found for saving Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID() +
                                                                                                                                                           " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                        Get_Trx().Rollback();
                                                                    }
                                                                    else
                                                                    {
                                                                        _log.Fine("Cost Calculation updated for VAB_InvoiceLine = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                                        Get_Trx().Commit();
                                                                    }
                                                                }
                                                            }
                                                            #endregion

                                                            #region CRMA
                                                            else if (order1.IsSOTrx() && order1.IsReturnTrx())
                                                            {
                                                                if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), invoice.GetVAF_Client_ID(), invoice.GetVAF_Org_ID(), product, invoiceLine.GetVAM_PFeature_SetInstance_ID(),
                                                                  "Invoice(Customer)", null, null, null, invoiceLine, null, ProductInvoiceLineCost, invoiceLine.GetQtyInvoiced(), Get_Trx(), out conversionNotFoundInvoice))
                                                                {
                                                                    if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                                                    {
                                                                        conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                                                    }
                                                                    _log.Info("Cost not Calculated for Invoice(Customer) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                                }
                                                                else
                                                                {
                                                                    if (invoice.GetDescription() != null && invoice.GetDescription().Contains("{->"))
                                                                    {
                                                                        invoiceLine.SetIsReversedCostCalculated(true);
                                                                    }
                                                                    invoiceLine.SetIsCostCalculated(true);
                                                                    if (client.IsCostImmediate() && !invoiceLine.IsCostImmediate())
                                                                    {
                                                                        invoiceLine.SetIsCostImmediate(true);
                                                                    }
                                                                    if (!invoiceLine.Save(Get_Trx()))
                                                                    {
                                                                        ValueNamePair pp = VLogger.RetrieveError();
                                                                        _log.Info("Error found for saving Invoice(Customer) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID() +
                                                                                                                                                           " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                        Get_Trx().Rollback();
                                                                    }
                                                                    else
                                                                    {
                                                                        _log.Fine("Cost Calculation updated for VAB_InvoiceLine = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                                        Get_Trx().Commit();
                                                                    }
                                                                }
                                                            }
                                                            #endregion

                                                            #region VRMA
                                                            else if (!order1.IsSOTrx() && order1.IsReturnTrx())
                                                            {
                                                                //change 12-5-2016
                                                                // when Ap Credit memo is alone then we will do a impact on costing.
                                                                // this is bcz of giving discount for particular product
                                                                // discount is given only when document type having setting as "Treat As Discount" = True
                                                                MVABDocTypes docType = new MVABDocTypes(GetCtx(), invoice.GetVAB_DocTypesTarget_ID(), Get_Trx());
                                                                if (docType.GetDocBaseType() == "APC" && docType.IsTreatAsDiscount() && invoiceLine.GetVAB_OrderLine_ID() == 0 && invoiceLine.GetVAM_Inv_InOutLine_ID() == 0 && invoiceLine.GetVAM_Product_ID() > 0)
                                                                {
                                                                    if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), invoice.GetVAF_Client_ID(), invoice.GetVAF_Org_ID(), product, invoiceLine.GetVAM_PFeature_SetInstance_ID(),
                                                                      "Invoice(Vendor)", null, null, null, invoiceLine, null, Decimal.Negate(ProductInvoiceLineCost), Decimal.Negate(invoiceLine.GetQtyInvoiced()),
                                                                      Get_Trx(), out conversionNotFoundInvoice))
                                                                    {
                                                                        if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                                                        {
                                                                            conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                                                        }
                                                                        _log.Info("Cost not Calculated for Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                                    }
                                                                    else
                                                                    {
                                                                        if (invoice.GetDescription() != null && invoice.GetDescription().Contains("{->"))
                                                                        {
                                                                            invoiceLine.SetIsReversedCostCalculated(true);
                                                                        }
                                                                        invoiceLine.SetIsCostCalculated(true);
                                                                        if (client.IsCostImmediate() && !invoiceLine.IsCostImmediate())
                                                                        {
                                                                            invoiceLine.SetIsCostImmediate(true);
                                                                        }
                                                                        if (!invoiceLine.Save(Get_Trx()))
                                                                        {
                                                                            ValueNamePair pp = VLogger.RetrieveError();
                                                                            _log.Info("Error found for saving Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID() +
                                                                                                                                                                   " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                            Get_Trx().Rollback();
                                                                        }
                                                                        else
                                                                        {
                                                                            _log.Fine("Cost Calculation updated for VAB_InvoiceLine = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                                            Get_Trx().Commit();
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                            #endregion
                                                        }
                                                        #endregion
                                                    }
                                                }
                                                else
                                                {
                                                    #region for Landed Cost Allocation
                                                    if (invoiceLine.GetVAB_Charge_ID() > 0)
                                                    {
                                                        if (!invoice.IsSOTrx() && !invoice.IsReturnTrx())
                                                        {
                                                            if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), invoice.GetVAF_Client_ID(), invoice.GetVAF_Org_ID(), null, 0,
                                                                "Invoice(Vendor)", null, null, null, invoiceLine, null, ProductInvoiceLineCost, 0, Get_TrxName(), out conversionNotFoundInvoice))
                                                            {
                                                                if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                                                {
                                                                    conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                                                }
                                                                _log.Info("Cost not Calculated for Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                            }
                                                            else
                                                            {
                                                                if (invoice.GetDescription() != null && invoice.GetDescription().Contains("{->"))
                                                                {
                                                                    invoiceLine.SetIsReversedCostCalculated(true);
                                                                }
                                                                invoiceLine.SetIsCostCalculated(true);
                                                                if (client.IsCostImmediate() && !invoiceLine.IsCostImmediate())
                                                                {
                                                                    invoiceLine.SetIsCostImmediate(true);
                                                                }
                                                                if (!invoiceLine.Save(Get_Trx()))
                                                                {
                                                                    ValueNamePair pp = VLogger.RetrieveError();
                                                                    _log.Info("Error found for saving Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID() +
                                                                                                                                                   " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                    Get_Trx().Rollback();
                                                                }
                                                                else
                                                                {
                                                                    _log.Fine("Cost Calculation updated for VAB_InvoiceLine = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                                    Get_Trx().Commit();
                                                                }
                                                            }
                                                        }
                                                    }
                                                    #endregion

                                                    #region for Expense type product
                                                    if (product.GetProductType() == "E" && product.GetVAM_Product_ID() > 0)
                                                    {
                                                        if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), invoice.GetVAF_Client_ID(), invoice.GetVAF_Org_ID(), product, 0,
                                                            "Invoice(Vendor)", null, null, null, invoiceLine, null, ProductInvoiceLineCost, 0, Get_TrxName(), out conversionNotFoundInvoice))
                                                        {
                                                            if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                                            {
                                                                conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                                            }
                                                            _log.Info("Cost not Calculated for Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                        }
                                                        else
                                                        {
                                                            if (invoice.GetDescription() != null && invoice.GetDescription().Contains("{->"))
                                                            {
                                                                invoiceLine.SetIsReversedCostCalculated(true);
                                                            }
                                                            invoiceLine.SetIsCostCalculated(true);
                                                            if (client.IsCostImmediate() && !invoiceLine.IsCostImmediate())
                                                            {
                                                                invoiceLine.SetIsCostImmediate(true);
                                                            }
                                                            if (!invoiceLine.Save(Get_Trx()))
                                                            {
                                                                ValueNamePair pp = VLogger.RetrieveError();
                                                                _log.Info("Error found for saving Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID() +
                                                                                                                                           " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                Get_Trx().Rollback();
                                                            }
                                                            else
                                                            {
                                                                _log.Fine("Cost Calculation updated for VAB_InvoiceLine = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                                Get_Trx().Commit();
                                                            }
                                                        }
                                                    }
                                                    #endregion

                                                    #region  for Item Type product
                                                    else if (product.GetProductType() == "I" && product.GetVAM_Product_ID() > 0)
                                                    {
                                                        if (countColumnExist > 0)
                                                        {
                                                            isCostAdjustableOnLost = product.IsCostAdjustmentOnLost();
                                                        }

                                                        #region Sales Order
                                                        if (invoice.IsSOTrx() && !invoice.IsReturnTrx())
                                                        {
                                                            if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), invoice.GetVAF_Client_ID(), invoice.GetVAF_Org_ID(), product, invoiceLine.GetVAM_PFeature_SetInstance_ID(),
                                                                  "Invoice(Customer)", null, null, null, invoiceLine, null, Decimal.Negate(ProductInvoiceLineCost), Decimal.Negate(invoiceLine.GetQtyInvoiced()),
                                                                  Get_Trx(), out conversionNotFoundInvoice))
                                                            {
                                                                if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                                                {
                                                                    conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                                                }
                                                                _log.Info("Cost not Calculated for Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                            }
                                                            else
                                                            {
                                                                if (invoice.GetDescription() != null && invoice.GetDescription().Contains("{->"))
                                                                {
                                                                    invoiceLine.SetIsReversedCostCalculated(true);
                                                                }
                                                                invoiceLine.SetIsCostCalculated(true);
                                                                if (client.IsCostImmediate() && !invoiceLine.IsCostImmediate())
                                                                {
                                                                    invoiceLine.SetIsCostImmediate(true);
                                                                }
                                                                if (!invoiceLine.Save(Get_Trx()))
                                                                {
                                                                    ValueNamePair pp = VLogger.RetrieveError();
                                                                    _log.Info("Error found for saving Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID() +
                                                                                                                                                   " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                    Get_Trx().Rollback();
                                                                }
                                                                else
                                                                {
                                                                    _log.Fine("Cost Calculation updated for VAB_InvoiceLine = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                                    Get_Trx().Commit();
                                                                }
                                                            }
                                                        }
                                                        #endregion

                                                        #region Purchase Order
                                                        else if (!invoice.IsSOTrx() && !invoice.IsReturnTrx() && 0 == 1)
                                                        {
                                                            if (invoiceLine.GetVAM_Inv_InOutLine_ID() > 0)
                                                            {
                                                                inoutLine = new MVAMInvInOutLine(GetCtx(), invoiceLine.GetVAM_Inv_InOutLine_ID(), Get_Trx());
                                                            }
                                                            if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), invoice.GetVAF_Client_ID(), invoice.GetVAF_Org_ID(), product, invoiceLine.GetVAM_PFeature_SetInstance_ID(),
                                                                  "Invoice(Vendor)", null, null, null, invoiceLine, null, ProductInvoiceLineCost,
                                                                  countColumnExist > 0 && isCostAdjustableOnLost && invoiceLine.GetVAM_Inv_InOutLine_ID() > 0 && inoutLine.GetMovementQty() < (invoice.GetDescription() != null && invoice.GetDescription().Contains("{->") ? decimal.Negate(invoiceLine.GetQtyInvoiced()) : invoiceLine.GetQtyInvoiced()) ? (invoice.GetDescription() != null && invoice.GetDescription().Contains("{->") ? Decimal.Negate(inoutLine.GetMovementQty()) : inoutLine.GetMovementQty()) : invoiceLine.GetQtyInvoiced(),
                                                                  Get_Trx(), out conversionNotFoundInvoice))
                                                            {
                                                                if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                                                {
                                                                    conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                                                }
                                                                _log.Info("Cost not Calculated for Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                            }
                                                            else
                                                            {
                                                                if (invoice.GetDescription() != null && invoice.GetDescription().Contains("{->"))
                                                                {
                                                                    invoiceLine.SetIsReversedCostCalculated(true);
                                                                }
                                                                invoiceLine.SetIsCostCalculated(true);
                                                                if (client.IsCostImmediate() && !invoiceLine.IsCostImmediate())
                                                                {
                                                                    invoiceLine.SetIsCostImmediate(true);
                                                                }
                                                                if (!invoiceLine.Save(Get_Trx()))
                                                                {
                                                                    ValueNamePair pp = VLogger.RetrieveError();
                                                                    _log.Info("Error found for saving Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID() +
                                                                                                                                                   " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                    Get_Trx().Rollback();
                                                                }
                                                                else
                                                                {
                                                                    _log.Fine("Cost Calculation updated for VAB_InvoiceLine = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                                    Get_Trx().Commit();
                                                                }
                                                            }
                                                        }
                                                        #endregion

                                                        #region CRMA
                                                        else if (invoice.IsSOTrx() && invoice.IsReturnTrx())
                                                        {
                                                            if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), invoice.GetVAF_Client_ID(), invoice.GetVAF_Org_ID(), product, invoiceLine.GetVAM_PFeature_SetInstance_ID(),
                                                              "Invoice(Customer)", null, null, null, invoiceLine, null, ProductInvoiceLineCost, invoiceLine.GetQtyInvoiced(),
                                                              Get_Trx(), out conversionNotFoundInvoice))
                                                            {
                                                                if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                                                {
                                                                    conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                                                }
                                                                _log.Info("Cost not Calculated for Invoice(Customer) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                            }
                                                            else
                                                            {
                                                                if (invoice.GetDescription() != null && invoice.GetDescription().Contains("{->"))
                                                                {
                                                                    invoiceLine.SetIsReversedCostCalculated(true);
                                                                }
                                                                invoiceLine.SetIsCostCalculated(true);
                                                                if (client.IsCostImmediate() && !invoiceLine.IsCostImmediate())
                                                                {
                                                                    invoiceLine.SetIsCostImmediate(true);
                                                                }
                                                                if (!invoiceLine.Save(Get_Trx()))
                                                                {
                                                                    ValueNamePair pp = VLogger.RetrieveError();
                                                                    _log.Info("Error found for saving Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID() +
                                                                                                                                                   " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                    Get_Trx().Rollback();
                                                                }
                                                                else
                                                                {
                                                                    _log.Fine("Cost Calculation updated for VAB_InvoiceLine = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                                    Get_Trx().Commit();
                                                                }
                                                            }
                                                        }
                                                        #endregion

                                                        #region VRMA
                                                        else if (!invoice.IsSOTrx() && invoice.IsReturnTrx())
                                                        {
                                                            // when Ap Credit memo is alone then we will do a impact on costing.
                                                            // this is bcz of giving discount for particular product
                                                            // discount is given only when document type having setting as "Treat As Discount" = True
                                                            MVABDocTypes docType = new MVABDocTypes(GetCtx(), invoice.GetVAB_DocTypesTarget_ID(), Get_Trx());
                                                            if (docType.GetDocBaseType() == "APC" && docType.IsTreatAsDiscount() && invoiceLine.GetVAB_OrderLine_ID() == 0 && invoiceLine.GetVAM_Inv_InOutLine_ID() == 0 && invoiceLine.GetVAM_Product_ID() > 0)
                                                            {
                                                                if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), invoice.GetVAF_Client_ID(), invoice.GetVAF_Org_ID(), product, invoiceLine.GetVAM_PFeature_SetInstance_ID(),
                                                                  "Invoice(Vendor)", null, null, null, invoiceLine, null, Decimal.Negate(ProductInvoiceLineCost), Decimal.Negate(invoiceLine.GetQtyInvoiced()),
                                                                  Get_Trx(), out conversionNotFoundInvoice))
                                                                {
                                                                    if (!conversionNotFoundInvoice1.Contains(conversionNotFoundInvoice))
                                                                    {
                                                                        conversionNotFoundInvoice1 += conversionNotFoundInvoice + " , ";
                                                                    }
                                                                    _log.Info("Cost not Calculated for Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                                }
                                                                else
                                                                {
                                                                    if (invoice.GetDescription() != null && invoice.GetDescription().Contains("{->"))
                                                                    {
                                                                        invoiceLine.SetIsReversedCostCalculated(true);
                                                                    }
                                                                    invoiceLine.SetIsCostCalculated(true);
                                                                    if (client.IsCostImmediate() && !invoiceLine.IsCostImmediate())
                                                                    {
                                                                        invoiceLine.SetIsCostImmediate(true);
                                                                    }
                                                                    if (!invoiceLine.Save(Get_Trx()))
                                                                    {
                                                                        ValueNamePair pp = VLogger.RetrieveError();
                                                                        _log.Info("Error found for saving Invoice(Vendor) for this Line ID = " + invoiceLine.GetVAB_InvoiceLine_ID() +
                                                                                                                                                           " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                        Get_Trx().Rollback();
                                                                    }
                                                                    else
                                                                    {
                                                                        _log.Fine("Cost Calculation updated for VAB_InvoiceLine = " + invoiceLine.GetVAB_InvoiceLine_ID());
                                                                        Get_Trx().Commit();
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        #endregion
                                                    }
                                                    #endregion
                                                }
                                            }
                                            catch { }
                                        }
                                    }
                                    sql.Clear();
                                    if (invoice.GetDescription() != null && invoice.GetDescription().Contains("{->"))
                                    {
                                        sql.Append("SELECT COUNT(*) FROM VAB_InvoiceLine WHERE IsReversedCostCalculated = 'N' AND IsActive = 'Y' AND VAB_Invoice_ID = " + invoice.GetVAB_Invoice_ID());
                                    }
                                    else
                                    {
                                        sql.Append("SELECT COUNT(*) FROM VAB_InvoiceLine WHERE IsCostCalculated = 'N' AND IsActive = 'Y' AND VAB_Invoice_ID = " + invoice.GetVAB_Invoice_ID());
                                    }
                                    if (Util.GetValueOfInt(DB.ExecuteScalar(sql.ToString(), null, Get_Trx())) <= 0)
                                    {
                                        if (invoice.GetDescription() != null && invoice.GetDescription().Contains("{->"))
                                        {
                                            invoice.SetIsReversedCostCalculated(true);
                                        }
                                        invoice.SetIsCostCalculated(true);
                                        if (!invoice.Save(Get_Trx()))
                                        {
                                            ValueNamePair pp = VLogger.RetrieveError();
                                            _log.Info("Error found for saving VAB_Invoice for this Record ID = " + invoice.GetVAB_Invoice_ID() +
                                                                                                   " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                        }
                                        else
                                        {
                                            _log.Fine("Cost Calculation updated for VAB_Invoice = " + invoice.GetVAB_Invoice_ID());
                                            Get_Trx().Commit();
                                        }
                                    }
                                    continue;
                                }
                            }
                            catch { }
                            #endregion

                            #region Cost Calculation For Material Receipt
                            try
                            {
                                if (Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["TableName"]) == "VAM_Inv_InOut" &&
                                   Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["issotrx"]) == "N" &&
                                   Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["isreturntrx"]) == "N" &&
                                   Util.GetValueOfString(dsRecord.Tables[0].Rows[z]["docstatus"]) == "RE")
                                {
                                    inout = new MVAMInvInOut(GetCtx(), Util.GetValueOfInt(dsRecord.Tables[0].Rows[z]["Record_Id"]), Get_Trx());

                                    sql.Clear();
                                    if (inout.GetDescription() != null && inout.GetDescription().Contains("{->"))
                                    {
                                        sql.Append("SELECT * FROM VAM_Inv_InOutLine WHERE IsActive = 'Y' AND iscostcalculated = 'Y' AND IsReversedCostCalculated = 'N' " +
                                                    " AND VAM_Inv_InOut_ID = " + inout.GetVAM_Inv_InOut_ID() + " ORDER BY Line ");
                                    }
                                    else
                                    {
                                        sql.Append("SELECT * FROM VAM_Inv_InOutLine WHERE IsActive = 'Y' AND iscostcalculated = 'N' " +
                                                     " AND VAM_Inv_InOut_ID = " + inout.GetVAM_Inv_InOut_ID() + " ORDER BY Line ");
                                    }
                                    dsChildRecord = DB.ExecuteDataset(sql.ToString(), null, Get_Trx());
                                    if (dsChildRecord != null && dsChildRecord.Tables.Count > 0 && dsChildRecord.Tables[0].Rows.Count > 0)
                                    {
                                        for (int j = 0; j < dsChildRecord.Tables[0].Rows.Count; j++)
                                        {
                                            try
                                            {
                                                inoutLine = new MVAMInvInOutLine(GetCtx(), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_Inv_InOutLine_ID"]), Get_Trx());
                                                orderLine = new MVABOrderLine(GetCtx(), inoutLine.GetVAB_OrderLine_ID(), null);
                                                if (orderLine != null && orderLine.GetVAB_Order_ID() > 0)
                                                {
                                                    order = new MVABOrder(GetCtx(), orderLine.GetVAB_Order_ID(), null);
                                                    if (order.GetDocStatus() != "VO")
                                                    {
                                                        if (orderLine.GetQtyOrdered() == 0)
                                                            continue;
                                                    }
                                                }
                                                product = new MVAMProduct(GetCtx(), Util.GetValueOfInt(dsChildRecord.Tables[0].Rows[j]["VAM_Product_ID"]), Get_Trx());
                                                if (product.GetProductType() == "I") // for Item Type product
                                                {
                                                    #region Material Receipt
                                                    if (!inout.IsSOTrx() && !inout.IsReturnTrx())
                                                    {
                                                        if (orderLine == null || orderLine.GetVAB_OrderLine_ID() == 0)
                                                        {
                                                            #region get price from VAM_ProductCost (Current Cost Price)
                                                            if (!client.IsCostImmediate())
                                                            {
                                                                // get price from VAM_ProductCost (Current Cost Price)
                                                                currentCostPrice = 0;
                                                                currentCostPrice = MVAMVAMProductCost.GetproductCostAndQtyMaterial(inoutLine.GetVAF_Client_ID(), inoutLine.GetVAF_Org_ID(),
                                                                    inoutLine.GetVAM_Product_ID(), inoutLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), inout.GetVAM_Warehouse_ID(), false);
                                                                DB.ExecuteQuery("UPDATE VAM_Inv_InOutLine SET CurrentCostPrice = " + currentCostPrice +
                                                                        @" WHERE VAM_Inv_InOutLine_ID = " + inoutLine.GetVAM_Inv_InOutLine_ID(), null, Get_Trx());
                                                            }
                                                            #endregion

                                                            if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), inout.GetVAF_Client_ID(), inout.GetVAF_Org_ID(), product, inoutLine.GetVAM_PFeature_SetInstance_ID(),
                                                           "Material Receipt", null, inoutLine, null, null, null, 0, inoutLine.GetMovementQty(), Get_Trx(), out conversionNotFoundInOut))
                                                            {
                                                                if (!conversionNotFoundInOut1.Contains(conversionNotFoundInOut))
                                                                {
                                                                    conversionNotFoundInOut1 += conversionNotFoundInOut + " , ";
                                                                }
                                                                _log.Info("Cost not Calculated for Material Receipt for this Line ID = " + inoutLine.GetVAM_Inv_InOutLine_ID());
                                                            }
                                                            else
                                                            {
                                                                if (inout.GetDescription() != null && inout.GetDescription().Contains("{->"))
                                                                {
                                                                    inoutLine.SetIsReversedCostCalculated(true);
                                                                }
                                                                inoutLine.SetIsCostCalculated(true);
                                                                if (client.IsCostImmediate() && !inoutLine.IsCostImmediate())
                                                                {
                                                                    inoutLine.SetIsCostImmediate(true);
                                                                }
                                                                if (!inoutLine.Save(Get_Trx()))
                                                                {
                                                                    ValueNamePair pp = VLogger.RetrieveError();
                                                                    _log.Info("Error found for Material Receipt for this Line ID = " + inoutLine.GetVAM_Inv_InOutLine_ID() +
                                                                                                                                                   " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                    Get_Trx().Rollback();
                                                                }
                                                                else
                                                                {
                                                                    _log.Fine("Cost Calculation updated for VAM_Inv_InOutLine = " + inoutLine.GetVAM_Inv_InOutLine_ID());
                                                                    Get_Trx().Commit();
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            #region get price from VAM_ProductCost (Current Cost Price)
                                                            if (!client.IsCostImmediate())
                                                            {
                                                                // get price from VAM_ProductCost (Current Cost Price)
                                                                currentCostPrice = MVAMVAMProductCost.GetproductCostAndQtyMaterial(inoutLine.GetVAF_Client_ID(), inoutLine.GetVAF_Org_ID(),
                                                                   inoutLine.GetVAM_Product_ID(), inoutLine.GetVAM_PFeature_SetInstance_ID(), Get_Trx(), inout.GetVAM_Warehouse_ID(), false);
                                                                DB.ExecuteQuery("UPDATE VAM_Inv_InOutLine SET CurrentCostPrice = " + currentCostPrice +
                                                                        @" WHERE VAM_Inv_InOutLine_ID = " + inoutLine.GetVAM_Inv_InOutLine_ID(), null, Get_Trx());
                                                            }
                                                            #endregion

                                                            amt = 0;
                                                            ProductOrderLineCost = orderLine.GetProductLineCost(orderLine);
                                                            ProductOrderPriceActual = ProductOrderLineCost / orderLine.GetQtyEntered();
                                                            if (isCostAdjustableOnLost && inoutLine.GetMovementQty() < orderLine.GetQtyOrdered() && order.GetDocStatus() != "VO")
                                                            {
                                                                if (inoutLine.GetMovementQty() > 0)
                                                                    amt = ProductOrderLineCost;
                                                                else
                                                                    amt = Decimal.Negate(ProductOrderLineCost);
                                                            }
                                                            else if (!isCostAdjustableOnLost && inoutLine.GetMovementQty() < orderLine.GetQtyOrdered() && order.GetDocStatus() != "VO")
                                                            {
                                                                amt = Decimal.Multiply(Decimal.Divide(ProductOrderLineCost, orderLine.GetQtyOrdered()), inoutLine.GetMovementQty());
                                                            }
                                                            else if (order.GetDocStatus() != "VO")
                                                            {
                                                                amt = Decimal.Multiply(Decimal.Divide(ProductOrderLineCost, orderLine.GetQtyOrdered()), inoutLine.GetMovementQty());
                                                            }
                                                            else if (order.GetDocStatus() == "VO")
                                                            {
                                                                amt = Decimal.Multiply(ProductOrderPriceActual, inoutLine.GetQtyEntered());
                                                            }

                                                            if (!MVAMProductCostQueue.CreateProductCostsDetails(GetCtx(), inout.GetVAF_Client_ID(), inout.GetVAF_Org_ID(), product, inoutLine.GetVAM_PFeature_SetInstance_ID(),
                                                               "Material Receipt", null, inoutLine, null, null, null, amt, inoutLine.GetMovementQty(),
                                                               Get_Trx(), out conversionNotFoundInOut))
                                                            {
                                                                if (!conversionNotFoundInOut1.Contains(conversionNotFoundInOut))
                                                                {
                                                                    conversionNotFoundInOut1 += conversionNotFoundInOut + " , ";
                                                                }
                                                                _log.Info("Cost not Calculated for Material Receipt for this Line ID = " + inoutLine.GetVAM_Inv_InOutLine_ID());
                                                            }
                                                            else
                                                            {
                                                                if (inout.GetDescription() != null && inout.GetDescription().Contains("{->"))
                                                                {
                                                                    inoutLine.SetIsReversedCostCalculated(true);
                                                                }
                                                                inoutLine.SetIsCostCalculated(true);
                                                                if (client.IsCostImmediate() && !inoutLine.IsCostImmediate())
                                                                {
                                                                    inoutLine.SetIsCostImmediate(true);
                                                                }
                                                                if (!inoutLine.Save(Get_Trx()))
                                                                {
                                                                    ValueNamePair pp = VLogger.RetrieveError();
                                                                    _log.Info("Error found for Material Receipt for this Line ID = " + inoutLine.GetVAM_Inv_InOutLine_ID() +
                                                                                                                                                   " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                                                    Get_Trx().Rollback();
                                                                }
                                                                else
                                                                {
                                                                    _log.Fine("Cost Calculation updated for VAM_Inv_InOutLine = " + inoutLine.GetVAM_Inv_InOutLine_ID());
                                                                    Get_Trx().Commit();
                                                                }
                                                            }
                                                        }
                                                    }
                                                    #endregion
                                                }
                                            }
                                            catch { }
                                        }
                                    }
                                    sql.Clear();
                                    if (inout.GetDescription() != null && inout.GetDescription().Contains("{->"))
                                    {
                                        sql.Append("SELECT COUNT(*) FROM VAM_Inv_InOutLine WHERE IsReversedCostCalculated = 'N' AND IsActive = 'Y' AND VAM_Inv_InOut_ID = " + inout.GetVAM_Inv_InOut_ID());
                                    }
                                    else
                                    {
                                        sql.Append("SELECT COUNT(*) FROM VAM_Inv_InOutLine WHERE IsCostCalculated = 'N' AND IsActive = 'Y' AND VAM_Inv_InOut_ID = " + inout.GetVAM_Inv_InOut_ID());
                                    }
                                    if (Util.GetValueOfInt(DB.ExecuteScalar(sql.ToString(), null, Get_Trx())) <= 0)
                                    {
                                        if (inout.GetDescription() != null && inout.GetDescription().Contains("{->"))
                                        {
                                            inout.SetIsReversedCostCalculated(true);
                                        }
                                        inout.SetIsCostCalculated(true);
                                        if (!inout.Save(Get_Trx()))
                                        {
                                            ValueNamePair pp = VLogger.RetrieveError();
                                            _log.Info("Error found for saving VAM_Inv_InOut for this Record ID = " + inout.GetVAM_Inv_InOut_ID() +
                                                                                                   " Error Name is " + pp.GetName() + " And Error Type is " + pp.GetType());
                                        }
                                        else
                                        {
                                            _log.Fine("Cost Calculation updated for VAM_Inv_InOut = " + inout.GetVAM_Inv_InOut_ID());
                                            Get_Trx().Commit();
                                        }
                                    }
                                    continue;
                                }
                            }
                            catch { }
                            #endregion

                        }
                    }
                }

            }
            catch (Exception ex)
            {
                _log.Info("Error Occured during costing " + ex.ToString());
                if (dsRecord != null)
                    dsRecord.Dispose();
                if (dsChildRecord != null)
                    dsChildRecord.Dispose();
            }
            finally
            {
                if (!string.IsNullOrEmpty(conversionNotFoundInOut1))
                {
                    conversionNotFoundInOut = Msg.GetMsg(GetCtx(), "ConvNotForMVAMInvInOut") + conversionNotFoundInOut1;
                }
                if (!string.IsNullOrEmpty(conversionNotFoundInvoice1))
                {
                    conversionNotFoundInvoice = Msg.GetMsg(GetCtx(), "ConvNotForInvoice") + conversionNotFoundInvoice1;
                }
                if (!string.IsNullOrEmpty(conversionNotFoundInventory1))
                {
                    conversionNotFoundInventory = Msg.GetMsg(GetCtx(), "ConvNotForInventry") + conversionNotFoundInventory1;
                }
                if (!string.IsNullOrEmpty(conversionNotFoundMovement1))
                {
                    conversionNotFoundMovement = Msg.GetMsg(GetCtx(), "ConvNotForMove") + conversionNotFoundMovement1;
                }
                if (!string.IsNullOrEmpty(conversionNotFoundProductionExecution1))
                {
                    conversionNotFoundProductionExecution = Msg.GetMsg(GetCtx(), "ConvNotForProduction") + conversionNotFoundProductionExecution1;
                }

                conversionNotFound = conversionNotFoundInOut + "\n" + conversionNotFoundInvoice + "\n" +
                                     conversionNotFoundInventory + "\n" + conversionNotFoundMovement + "\n" +
                                     conversionNotFoundProductionExecution;

                if (dsRecord != null)
                    dsRecord.Dispose();
                if (dsChildRecord != null)
                    dsChildRecord.Dispose();
                _log.Info("Successfully Ended Cost Calculation ");
            }
            return conversionNotFound;
        }

        public DateTime? SerachMinDate(int count)
        {
            DateTime? minDate;
            DateTime? tempDate;
            try
            {
                sql.Clear();
                sql.Append("SELECT Min(MovementDate) FROM VAM_Inventory WHERE isactive = 'Y' AND ((docstatus IN ('CO' , 'CL') AND iscostcalculated = 'N') OR (docstatus IN ('RE') AND iscostcalculated = 'Y' AND ISREVERSEDCOSTCALCULATED= 'N' AND description like '%{->%'))");
                minDate = Util.GetValueOfDateTime(DB.ExecuteScalar(sql.ToString(), null, Get_Trx()));

                sql.Clear();
                sql.Append("SELECT Min(MovementDate) FROM VAM_InventoryTransfer WHERE isactive = 'Y' AND ((docstatus IN ('CO' , 'CL') AND iscostcalculated = 'N') OR (docstatus IN ('RE') AND iscostcalculated = 'Y' AND ISREVERSEDCOSTCALCULATED= 'N' AND description like '%{->%'))");
                tempDate = Util.GetValueOfDateTime(DB.ExecuteScalar(sql.ToString(), null, Get_Trx()));
                if (minDate == null || (Util.GetValueOfDateTime(minDate) > Util.GetValueOfDateTime(tempDate) && tempDate != null))
                {
                    minDate = tempDate;
                }

                // Production 
                sql.Clear();
                sql.Append(@"SELECT Min(MovementDate) FROM VAM_Production WHERE isactive = 'Y' AND 
                              ((PROCESSED = 'Y' AND iscostcalculated = 'N' AND IsReversed = 'N' ) OR 
                               (PROCESSED = 'Y' AND iscostcalculated  = 'Y' AND ISREVERSEDCOSTCALCULATED= 'N' AND IsReversed = 'Y' AND Name LIKE '%{->%'))");
                tempDate = Util.GetValueOfDateTime(DB.ExecuteScalar(sql.ToString(), null, Get_Trx()));
                if (minDate == null || (Util.GetValueOfDateTime(minDate) > Util.GetValueOfDateTime(tempDate) && tempDate != null))
                {
                    minDate = tempDate;
                }

                sql.Clear();
                sql.Append("SELECT Min(DateAcct) FROM VAM_MatchInvoice WHERE isactive = 'Y' AND iscostcalculated = 'N'");
                tempDate = Util.GetValueOfDateTime(DB.ExecuteScalar(sql.ToString(), null, Get_Trx()));
                if (minDate == null || (Util.GetValueOfDateTime(minDate) > Util.GetValueOfDateTime(tempDate) && tempDate != null))
                {
                    minDate = tempDate;
                }

                try
                {
                    sql.Clear();
                    sql.Append("SELECT Min(Updated) FROM VAM_MatchInvoiceoiceCostTrack WHERE isactive = 'Y'");
                    tempDate = Util.GetValueOfDateTime(DB.ExecuteScalar(sql.ToString(), null, Get_Trx()));
                    if (minDate == null || (Util.GetValueOfDateTime(minDate) > Util.GetValueOfDateTime(tempDate) && tempDate != null))
                    {
                        minDate = tempDate;
                    }
                }
                catch { }
                try
                {
                    sql.Clear();
                    sql.Append("SELECT Min(VAFAM_trxdate) FROM VAFAM_AssetDisposal WHERE isactive = 'Y' AND ((docstatus IN ('CO' , 'CL') AND iscostcalculated = 'N') OR (docstatus IN ('RE') AND iscostcalculated = 'Y' AND ISREVERSEDCOSTCALCULATED= 'N'))");
                    tempDate = Util.GetValueOfDateTime(DB.ExecuteScalar(sql.ToString(), null, Get_Trx()));
                    if (minDate == null || (Util.GetValueOfDateTime(minDate) > Util.GetValueOfDateTime(tempDate) && tempDate != null))
                    {
                        minDate = tempDate;
                    }
                }
                catch { }

                sql.Clear();
                sql.Append("SELECT Min(DateAcct) FROM VAB_Invoice WHERE isactive = 'Y' AND ((docstatus IN ('CO' , 'CL') AND iscostcalculated = 'N') OR (docstatus IN ('RE') AND iscostcalculated = 'Y' AND ISREVERSEDCOSTCALCULATED= 'N' AND description like '%{->%'))");
                tempDate = Util.GetValueOfDateTime(DB.ExecuteScalar(sql.ToString(), null, Get_Trx()));
                if (minDate == null || (Util.GetValueOfDateTime(minDate) > Util.GetValueOfDateTime(tempDate) && tempDate != null))
                {
                    minDate = tempDate;
                }

                sql.Clear();
                sql.Append("SELECT Min(DateAcct) FROM VAM_Inv_InOut WHERE isactive = 'Y' AND ((docstatus IN ('CO' , 'CL') AND iscostcalculated = 'N') OR (docstatus IN ('RE') AND iscostcalculated = 'Y' AND ISREVERSEDCOSTCALCULATED= 'N' AND description like '%{->%'))");
                tempDate = Util.GetValueOfDateTime(DB.ExecuteScalar(sql.ToString(), null, Get_Trx()));
                if (minDate == null || (Util.GetValueOfDateTime(minDate) > Util.GetValueOfDateTime(tempDate) && tempDate != null))
                {
                    minDate = tempDate;
                }

                if (count > 0)
                {
                    try
                    {
                        // when Manufacuring Module is downloaded
                        sql.Clear();
                        sql.Append(@"SELECT Min(VAMFG_DateAcct) FROM VAMFG_M_WrkOdrTransaction WHERE VAMFG_WorkOrderTxnType IN ('CI', 'CR') AND  isactive = 'Y' AND
                             ((docstatus IN ('CO' , 'CL') AND iscostcalculated = 'N') 
                             OR (docstatus IN ('RE') AND iscostcalculated = 'Y' AND ISREVERSEDCOSTCALCULATED= 'N' AND VAMFG_description like '%{->%'))");
                        tempDate = Util.GetValueOfDateTime(DB.ExecuteScalar(sql.ToString(), null, Get_Trx()));
                    }
                    catch { }
                    if (minDate == null || (Util.GetValueOfDateTime(minDate) > Util.GetValueOfDateTime(tempDate) && tempDate != null))
                    {
                        minDate = tempDate;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return minDate;
        }

        /// <summary>
        /// Get - sum of all component whose available on "Component Issue To Work Order" transaction
        /// </summary>
        /// <param name="VAMFG_VAM_WorkOrder_ID">production Order</param>
        /// <param name="trxName">transaction</param>
        /// <returns>cost of finished good</returns>
        private Decimal GetCostForProductionFinishedGood(int VAMFG_VAM_WorkOrder_ID, Trx trxName)
        {
            decimal currentcostprice = 0;

            // check any record havoing Zero cost, then return with ZERO Value
            String sql = @"SELECT COUNT(VAMFG_M_WrkOdrTrnsctionLine_ID) as NotFoundCurrentCost
                             FROM VAMFG_M_WrkOdrTransaction wot
                             INNER JOIN VAMFG_VAM_WorkOrder wo ON wo.VAMFG_VAM_WorkOrder_ID = wot.VAMFG_VAM_WorkOrder_ID
                             INNER JOIN VAMFG_M_WrkOdrTrnsctionLine wotl ON wot.VAMFG_M_WrkOdrTransaction_ID = wotl.VAMFG_M_WrkOdrTransaction_ID
                           WHERE wotl.IsActive = 'Y' AND wot.VAMFG_VAM_WorkOrder_ID = " + VAMFG_VAM_WorkOrder_ID +
                             @" AND wot.VAMFG_WorkOrderTxnType = 'CI' AND NVL(wotl.currentcostprice , 0) = 0 AND wot.DocStatus IN ('CO'  , 'CL') GROUP BY wot.VAMFG_QtyEntered ";
            if (VAdvantage.Utility.Util.GetValueOfDecimal(DB.ExecuteScalar(sql, null, trxName)) == 0)
            {
                // sum of all component whose available on "Component Issue To Work Order" transaction
                sql = @"SELECT ROUND((SUM(wotl.VAMFG_QtyEntered * wotl.CurrentCostPrice) / wot.VAMFG_QtyEntered) , 10) as Currenctcost
                             FROM VAMFG_M_WrkOdrTransaction wot
                             INNER JOIN VAMFG_VAM_WorkOrder wo ON wo.VAMFG_VAM_WorkOrder_ID = wot.VAMFG_VAM_WorkOrder_ID
                             INNER JOIN VAMFG_M_WrkOdrTrnsctionLine wotl ON wot.VAMFG_M_WrkOdrTransaction_ID = wotl.VAMFG_M_WrkOdrTransaction_ID
                           WHERE wotl.IsActive = 'Y' AND wot.VAMFG_VAM_WorkOrder_ID = " + VAMFG_VAM_WorkOrder_ID +
                                 @" AND wot.VAMFG_WorkOrderTxnType = 'CI' AND wot.DocStatus IN ('CO'  , 'CL') GROUP BY wot.VAMFG_QtyEntered ";
                currentcostprice = VAdvantage.Utility.Util.GetValueOfDecimal(DB.ExecuteScalar(sql, null, trxName));
            }
            return currentcostprice;
        }

    }
}
