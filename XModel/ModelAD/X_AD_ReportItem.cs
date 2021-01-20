namespace VAdvantage.Model
{

/** Generated Model - DO NOT CHANGE */
using System;
using System.Text;
using VAdvantage.DataBase;
using VAdvantage.Common;
using VAdvantage.Classes;
using VAdvantage.Process;
using VAdvantage.Model;
using VAdvantage.Utility;
using System.Data;
/** Generated Model for VAF_ReportItem
 *  @author Jagmohan Bhatt (generated) 
 *  @version Vienna Framework 1.1.1 - $Id$ */
public class X_VAF_ReportItem : PO
{
public X_VAF_ReportItem (Context ctx, int VAF_ReportItem_ID, Trx trxName) : base (ctx, VAF_ReportItem_ID, trxName)
{
/** if (VAF_ReportItem_ID == 0)
{
SetVAF_ReportItem_ID (0);
SetVAF_ReportTable_ID (0);
}
 */
}
public X_VAF_ReportItem (Ctx ctx, int VAF_ReportItem_ID, Trx trxName) : base (ctx, VAF_ReportItem_ID, trxName)
{
/** if (VAF_ReportItem_ID == 0)
{
SetVAF_ReportItem_ID (0);
SetVAF_ReportTable_ID (0);
}
 */
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAF_ReportItem (Context ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName)
{
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAF_ReportItem (Ctx ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName)
{
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAF_ReportItem (Ctx ctx, IDataReader dr, Trx trxName) : base(ctx, dr, trxName)
{
}
/** Static Constructor 
 Set Table ID By Table Name
 added by ->Harwinder */
static X_VAF_ReportItem()
{
 Table_ID = Get_Table_ID(Table_Name);
 model = new KeyNamePair(Table_ID,Table_Name);
}
/** Serial Version No */
//static long serialVersionUID = 27657404046527L;
/** Last Updated Timestamp 7/31/2013 7:22:10 PM */
public static long updatedMS = 1375278729738L;
/** VAF_TableView_ID=1000728 */
public static int Table_ID;
 // =1000728;

/** TableName=VAF_ReportItem */
public static String Table_Name="VAF_ReportItem";

protected static KeyNamePair model;
protected Decimal accessLevel = new Decimal(4);
/** AccessLevel
@return 4 - System 
*/
protected override int Get_AccessLevel()
{
return Convert.ToInt32(accessLevel.ToString());
}
/** Load Meta Data
@param ctx context
@return PO Info
*/
protected override POInfo InitPO (Ctx ctx)
{
POInfo poi = POInfo.GetPOInfo (ctx, Table_ID);
return poi;
}
/** Load Meta Data
@param ctx context
@return PO Info
*/
protected override POInfo InitPO(Context ctx)
{
POInfo poi = POInfo.GetPOInfo (ctx, Table_ID);
return poi;
}
/** Info
@return info
*/
public override String ToString()
{
StringBuilder sb = new StringBuilder ("X_VAF_ReportItem[").Append(Get_ID()).Append("]");
return sb.ToString();
}
/** Set Column.
@param VAF_Column_ID Column in the table */
public void SetVAF_Column_ID (int VAF_Column_ID)
{
if (VAF_Column_ID <= 0) Set_Value ("VAF_Column_ID", null);
else
Set_Value ("VAF_Column_ID", VAF_Column_ID);
}
/** Get Column.
@return Column in the table */
public int GetVAF_Column_ID() 
{
Object ii = Get_Value("VAF_Column_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set Do Not Repeat.
@param AD_DoNotRepeat Do Not Repeat */
public void SetAD_DoNotRepeat (Boolean AD_DoNotRepeat)
{
Set_Value ("AD_DoNotRepeat", AD_DoNotRepeat);
}
/** Get Do Not Repeat.
@return Do Not Repeat */
public Boolean IsAD_DoNotRepeat() 
{
Object oo = Get_Value("AD_DoNotRepeat");
if (oo != null) 
{
 if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo);
 return "Y".Equals(oo);
}
return false;
}
/** Set Merge With Next Column.
@param AD_MergeWithNextColumn Merge With Next Column */
public void SetAD_MergeWithNextColumn (Boolean AD_MergeWithNextColumn)
{
Set_Value ("AD_MergeWithNextColumn", AD_MergeWithNextColumn);
}
/** Get Merge With Next Column.
@return Merge With Next Column */
public Boolean IsAD_MergeWithNextColumn() 
{
Object oo = Get_Value("AD_MergeWithNextColumn");
if (oo != null) 
{
 if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo);
 return "Y".Equals(oo);
}
return false;
}

/** VAF_Print_Rpt_LayoutType VAF_Control_Ref_ID=1000148 */
public static int VAF_PRINT_RPT_LAYOUTTYPE_VAF_Control_Ref_ID=1000148;
/** Only Label = L */
public static String VAF_PRINT_RPT_LAYOUTTYPE_OnlyLabel = "L";
/** Only Value = V */
public static String VAF_PRINT_RPT_LAYOUTTYPE_OnlyValue = "V";
/** Is test a valid value.
@param test testvalue
@returns true if valid **/
public bool IsVAF_Print_Rpt_LayoutTypeValid (String test)
{
return test == null || test.Equals("L") || test.Equals("V");
}
/** Set Print Format Type.
@param VAF_Print_Rpt_LayoutType Print Format Type */
public void SetVAF_Print_Rpt_LayoutType (String VAF_Print_Rpt_LayoutType)
{
if (!IsVAF_Print_Rpt_LayoutTypeValid(VAF_Print_Rpt_LayoutType))
throw new ArgumentException ("VAF_Print_Rpt_LayoutType Invalid value - " + VAF_Print_Rpt_LayoutType + " - Reference_ID=1000148 - L - V");
if (VAF_Print_Rpt_LayoutType != null && VAF_Print_Rpt_LayoutType.Length > 1)
{
log.Warning("Length > 1 - truncated");
VAF_Print_Rpt_LayoutType = VAF_Print_Rpt_LayoutType.Substring(0,1);
}
Set_Value ("VAF_Print_Rpt_LayoutType", VAF_Print_Rpt_LayoutType);
}
/** Get Print Format Type.
@return Print Format Type */
public String GetVAF_Print_Rpt_LayoutType() 
{
return (String)Get_Value("VAF_Print_Rpt_LayoutType");
}
/** Set VAF_ReportItem_ID.
@param VAF_ReportItem_ID VAF_ReportItem_ID */
public void SetVAF_ReportItem_ID (int VAF_ReportItem_ID)
{
if (VAF_ReportItem_ID < 1) throw new ArgumentException ("VAF_ReportItem_ID is mandatory.");
Set_ValueNoCheck ("VAF_ReportItem_ID", VAF_ReportItem_ID);
}
/** Get VAF_ReportItem_ID.
@return VAF_ReportItem_ID */
public int GetVAF_ReportItem_ID() 
{
Object ii = Get_Value("VAF_ReportItem_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set VAF_ReportTable_ID.
@param VAF_ReportTable_ID VAF_ReportTable_ID */
public void SetVAF_ReportTable_ID (int VAF_ReportTable_ID)
{
if (VAF_ReportTable_ID < 1) throw new ArgumentException ("VAF_ReportTable_ID is mandatory.");
Set_ValueNoCheck ("VAF_ReportTable_ID", VAF_ReportTable_ID);
}
/** Get VAF_ReportTable_ID.
@return VAF_ReportTable_ID */
public int GetVAF_ReportTable_ID() 
{
Object ii = Get_Value("VAF_ReportTable_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set Show As Header.
@param AD_ShowAsHeader Show As Header */
public void SetAD_ShowAsHeader (Boolean AD_ShowAsHeader)
{
Set_Value ("AD_ShowAsHeader", AD_ShowAsHeader);
}
/** Get Show As Header.
@return Show As Header */
public Boolean IsAD_ShowAsHeader() 
{
Object oo = Get_Value("AD_ShowAsHeader");
if (oo != null) 
{
 if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo);
 return "Y".Equals(oo);
}
return false;
}
/** Set Export.
@param Export_ID Export */
public void SetExport_ID (String Export_ID)
{
if (Export_ID != null && Export_ID.Length > 50)
{
log.Warning("Length > 50 - truncated");
Export_ID = Export_ID.Substring(0,50);
}
Set_Value ("Export_ID", Export_ID);
}
/** Get Export.
@return Export */
public String GetExport_ID() 
{
return (String)Get_Value("Export_ID");
}
/** Set Ascending Sort Order.
@param IsAscending Sort in Ascending Order */
public void SetIsAscending (Boolean IsAscending)
{
Set_Value ("IsAscending", IsAscending);
}
/** Get Ascending Sort Order.
@return Sort in Ascending Order */
public Boolean IsAscending() 
{
Object oo = Get_Value("IsAscending");
if (oo != null) 
{
 if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo);
 return "Y".Equals(oo);
}
return false;
}
/** Set Calculate Mean (μ).
@param IsAveraged Calculate Average of numeric content or length */
public void SetIsAveraged (Boolean IsAveraged)
{
Set_Value ("IsAveraged", IsAveraged);
}
/** Get Calculate Mean (μ).
@return Calculate Average of numeric content or length */
public Boolean IsAveraged() 
{
Object oo = Get_Value("IsAveraged");
if (oo != null) 
{
 if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo);
 return "Y".Equals(oo);
}
return false;
}
/** Set Centrally maintained.
@param IsCentrallyMaintained Information maintained in System Element table */
public void SetIsCentrallyMaintained (Boolean IsCentrallyMaintained)
{
Set_Value ("IsCentrallyMaintained", IsCentrallyMaintained);
}
/** Get Centrally maintained.
@return Information maintained in System Element table */
public Boolean IsCentrallyMaintained() 
{
Object oo = Get_Value("IsCentrallyMaintained");
if (oo != null) 
{
 if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo);
 return "Y".Equals(oo);
}
return false;
}
/** Set Calculate Count (№).
@param IsCounted Count number of not empty elements */
public void SetIsCounted (Boolean IsCounted)
{
Set_Value ("IsCounted", IsCounted);
}
/** Get Calculate Count (№).
@return Count number of not empty elements */
public Boolean IsCounted() 
{
Object oo = Get_Value("IsCounted");
if (oo != null) 
{
 if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo);
 return "Y".Equals(oo);
}
return false;
}
/** Set Calculate Deviation (σ).
@param IsDeviationCalc Calculate Standard Deviation */
public void SetIsDeviationCalc (Boolean IsDeviationCalc)
{
Set_Value ("IsDeviationCalc", IsDeviationCalc);
}
/** Get Calculate Deviation (σ).
@return Calculate Standard Deviation */
public Boolean IsDeviationCalc() 
{
Object oo = Get_Value("IsDeviationCalc");
if (oo != null) 
{
 if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo);
 return "Y".Equals(oo);
}
return false;
}
/** Set Group by.
@param IsGroupBy After a group change, totals, etc. are printed */
public void SetIsGroupBy (Boolean IsGroupBy)
{
Set_Value ("IsGroupBy", IsGroupBy);
}
/** Get Group by.
@return After a group change, totals, etc. are printed */
public Boolean IsGroupBy() 
{
Object oo = Get_Value("IsGroupBy");
if (oo != null) 
{
 if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo);
 return "Y".Equals(oo);
}
return false;
}
/** Set Calculate Maximum (↑).
@param IsMaxCalc Calculate the maximum amount */
public void SetIsMaxCalc (Boolean IsMaxCalc)
{
Set_Value ("IsMaxCalc", IsMaxCalc);
}
/** Get Calculate Maximum (↑).
@return Calculate the maximum amount */
public Boolean IsMaxCalc() 
{
Object oo = Get_Value("IsMaxCalc");
if (oo != null) 
{
 if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo);
 return "Y".Equals(oo);
}
return false;
}
/** Set Calculate Minimum (↓).
@param IsMinCalc Calculate the minimum amount */
public void SetIsMinCalc (Boolean IsMinCalc)
{
Set_Value ("IsMinCalc", IsMinCalc);
}
/** Get Calculate Minimum (↓).
@return Calculate the minimum amount */
public Boolean IsMinCalc() 
{
Object oo = Get_Value("IsMinCalc");
if (oo != null) 
{
 if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo);
 return "Y".Equals(oo);
}
return false;
}
/** Set Order by.
@param IsOrderBy Include in sort order */
public void SetIsOrderBy (Boolean IsOrderBy)
{
Set_Value ("IsOrderBy", IsOrderBy);
}
/** Get Order by.
@return Include in sort order */
public Boolean IsOrderBy() 
{
Object oo = Get_Value("IsOrderBy");
if (oo != null) 
{
 if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo);
 return "Y".Equals(oo);
}
return false;
}
/** Set Running Total.
@param IsRunningTotal Create a running total (sum) */
public void SetIsRunningTotal (Boolean IsRunningTotal)
{
Set_Value ("IsRunningTotal", IsRunningTotal);
}
/** Get Running Total.
@return Create a running total (sum) */
public Boolean IsRunningTotal() 
{
Object oo = Get_Value("IsRunningTotal");
if (oo != null) 
{
 if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo);
 return "Y".Equals(oo);
}
return false;
}
/** Set Calculate Sum (Σ).
@param IsSummarized Calculate the Sum of numeric content or length */
public void SetIsSummarized (Boolean IsSummarized)
{
Set_Value ("IsSummarized", IsSummarized);
}
/** Get Calculate Sum (Σ).
@return Calculate the Sum of numeric content or length */
public Boolean IsSummarized() 
{
Object oo = Get_Value("IsSummarized");
if (oo != null) 
{
 if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo);
 return "Y".Equals(oo);
}
return false;
}
/** Set Suppress Null.
@param IsSuppressNull Suppress columns or elements with NULL value */
public void SetIsSuppressNull (Boolean IsSuppressNull)
{
Set_Value ("IsSuppressNull", IsSuppressNull);
}
/** Get Suppress Null.
@return Suppress columns or elements with NULL value */
public Boolean IsSuppressNull() 
{
Object oo = Get_Value("IsSuppressNull");
if (oo != null) 
{
 if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo);
 return "Y".Equals(oo);
}
return false;
}
/** Set Calculate Variance (σ²).
@param IsVarianceCalc Calculate Variance */
public void SetIsVarianceCalc (Boolean IsVarianceCalc)
{
Set_Value ("IsVarianceCalc", IsVarianceCalc);
}
/** Get Calculate Variance (σ²).
@return Calculate Variance */
public Boolean IsVarianceCalc() 
{
Object oo = Get_Value("IsVarianceCalc");
if (oo != null) 
{
 if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo);
 return "Y".Equals(oo);
}
return false;
}
/** Set Name.
@param Name Name */
public void SetName (String Name)
{
if (Name != null && Name.Length > 50)
{
log.Warning("Length > 50 - truncated");
Name = Name.Substring(0,50);
}
Set_Value ("Name", Name);
}
/** Get Name.
@return Name */
public String GetName() 
{
return (String)Get_Value("Name");
}
/** Set Print Text.
@param PrintName The label text to be printed on a document or correspondence. */
public void SetPrintName (String PrintName)
{
if (PrintName != null && PrintName.Length > 50)
{
log.Warning("Length > 50 - truncated");
PrintName = PrintName.Substring(0,50);
}
Set_Value ("PrintName", PrintName);
}
/** Get Print Text.
@return The label text to be printed on a document or correspondence. */
public String GetPrintName() 
{
return (String)Get_Value("PrintName");
}
/** Set Record Sort No.
@param SortNo Determines in what order the records are displayed */
public void SetSortNo (String SortNo)
{
if (SortNo != null && SortNo.Length > 50)
{
log.Warning("Length > 50 - truncated");
SortNo = SortNo.Substring(0,50);
}
Set_Value ("SortNo", SortNo);
}
/** Get Record Sort No.
@return Determines in what order the records are displayed */
public String GetSortNo() 
{
return (String)Get_Value("SortNo");
}
}

}
