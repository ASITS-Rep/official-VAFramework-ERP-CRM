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
/** Generated Model for VAF_ReportView
 *  @author Jagmohan Bhatt (generated) 
 *  @version Vienna Framework 1.1.1 - $Id$ */
public class X_VAF_ReportView : PO
{
public X_VAF_ReportView (Context ctx, int VAF_ReportView_ID, Trx trxName) : base (ctx, VAF_ReportView_ID, trxName)
{
/** if (VAF_ReportView_ID == 0)
{
SetVAF_ReportView_ID (0);
SetVAF_TableView_ID (0);
SetRecordType (null);	// U
SetName (null);
}
 */
}
public X_VAF_ReportView (Ctx ctx, int VAF_ReportView_ID, Trx trxName) : base (ctx, VAF_ReportView_ID, trxName)
{
/** if (VAF_ReportView_ID == 0)
{
SetVAF_ReportView_ID (0);
SetVAF_TableView_ID (0);
SetRecordType (null);	// U
SetName (null);
}
 */
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAF_ReportView (Context ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName)
{
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAF_ReportView (Ctx ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName)
{
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAF_ReportView (Ctx ctx, IDataReader dr, Trx trxName) : base(ctx, dr, trxName)
{
}
/** Static Constructor 
 Set Table ID By Table Name
 added by ->Harwinder */
static X_VAF_ReportView()
{
 Table_ID = Get_Table_ID(Table_Name);
 model = new KeyNamePair(Table_ID,Table_Name);
}
/** Serial Version No */
//static long serialVersionUID = 27562514363787L;
/** Last Updated Timestamp 7/29/2010 1:07:27 PM */
public static long updatedMS = 1280389046998L;
/** VAF_TableView_ID=361 */
public static int Table_ID;
 // =361;

/** TableName=VAF_ReportView */
public static String Table_Name="VAF_ReportView";

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
StringBuilder sb = new StringBuilder ("X_VAF_ReportView[").Append(Get_ID()).Append("]");
return sb.ToString();
}
/** Set Report View.
@param VAF_ReportView_ID View used to generate this report */
public void SetVAF_ReportView_ID (int VAF_ReportView_ID)
{
if (VAF_ReportView_ID < 1) throw new ArgumentException ("VAF_ReportView_ID is mandatory.");
Set_ValueNoCheck ("VAF_ReportView_ID", VAF_ReportView_ID);
}
/** Get Report View.
@return View used to generate this report */
public int GetVAF_ReportView_ID() 
{
Object ii = Get_Value("VAF_ReportView_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set Table.
@param VAF_TableView_ID Database Table information */
public void SetVAF_TableView_ID (int VAF_TableView_ID)
{
if (VAF_TableView_ID < 1) throw new ArgumentException ("VAF_TableView_ID is mandatory.");
Set_Value ("VAF_TableView_ID", VAF_TableView_ID);
}
/** Get Table.
@return Database Table information */
public int GetVAF_TableView_ID() 
{
Object ii = Get_Value("VAF_TableView_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set Description.
@param Description Optional short description of the record */
public void SetDescription (String Description)
{
if (Description != null && Description.Length > 255)
{
log.Warning("Length > 255 - truncated");
Description = Description.Substring(0,255);
}
Set_Value ("Description", Description);
}
/** Get Description.
@return Optional short description of the record */
public String GetDescription() 
{
return (String)Get_Value("Description");
}

/** RecordType VAF_Control_Ref_ID=389 */
public static int RecordType_VAF_Control_Ref_ID=389;
/** Set Entity Type.
@param RecordType Dictionary Entity Type;
 Determines ownership and synchronization */
public void SetRecordType (String RecordType)
{
if (RecordType.Length > 4)
{
log.Warning("Length > 4 - truncated");
RecordType = RecordType.Substring(0,4);
}
Set_Value ("RecordType", RecordType);
}
/** Get Entity Type.
@return Dictionary Entity Type;
 Determines ownership and synchronization */
public String GetRecordType() 
{
return (String)Get_Value("RecordType");
}
/** Set Name.
@param Name Alphanumeric identifier of the entity */
public void SetName (String Name)
{
if (Name == null) throw new ArgumentException ("Name is mandatory.");
if (Name.Length > 60)
{
log.Warning("Length > 60 - truncated");
Name = Name.Substring(0,60);
}
Set_Value ("Name", Name);
}
/** Get Name.
@return Alphanumeric identifier of the entity */
public String GetName() 
{
return (String)Get_Value("Name");
}
/** Get Record ID/ColumnName
@return ID/ColumnName pair */
public KeyNamePair GetKeyNamePair() 
{
return new KeyNamePair(Get_ID(), GetName());
}
/** Set Sql ORDER BY.
@param OrderByClause Fully qualified ORDER BY clause */
public void SetOrderByClause (String OrderByClause)
{
if (OrderByClause != null && OrderByClause.Length > 2000)
{
log.Warning("Length > 2000 - truncated");
OrderByClause = OrderByClause.Substring(0,2000);
}
Set_Value ("OrderByClause", OrderByClause);
}
/** Get Sql ORDER BY.
@return Fully qualified ORDER BY clause */
public String GetOrderByClause() 
{
return (String)Get_Value("OrderByClause");
}
/** Set Sql WHERE.
@param WhereClause Fully qualified SQL WHERE clause */
public void SetWhereClause (String WhereClause)
{
if (WhereClause != null && WhereClause.Length > 2000)
{
log.Warning("Length > 2000 - truncated");
WhereClause = WhereClause.Substring(0,2000);
}
Set_Value ("WhereClause", WhereClause);
}
/** Get Sql WHERE.
@return Fully qualified SQL WHERE clause */
public String GetWhereClause() 
{
return (String)Get_Value("WhereClause");
}
}

}
