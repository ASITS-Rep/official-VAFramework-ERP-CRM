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
/** Generated Model for VAF_DBViewElement
 *  @author Jagmohan Bhatt (generated) 
 *  @version Vienna Framework 1.1.1 - $Id$ */
public class X_VAF_DBViewElement : PO
{
public X_VAF_DBViewElement (Context ctx, int VAF_DBViewElement_ID, Trx trxName) : base (ctx, VAF_DBViewElement_ID, trxName)
{
/** if (VAF_DBViewElement_ID == 0)
{
SetVAF_DBViewElement_ID (0);
SetRecordType (null);	// U
SetFromClause (null);
SetName (null);
SetReferenced_Table_ID (0);
SetSeqNo (0);	// @SQL=SELECT COALESCE(MAX(SeqNo),0)+10 AS DefaultValue FROM VAF_DBViewElement WHERE AD_View_ID=@AD_View_ID@
}
 */
}
public X_VAF_DBViewElement (Ctx ctx, int VAF_DBViewElement_ID, Trx trxName) : base (ctx, VAF_DBViewElement_ID, trxName)
{
/** if (VAF_DBViewElement_ID == 0)
{
SetVAF_DBViewElement_ID (0);
SetRecordType (null);	// U
SetFromClause (null);
SetName (null);
SetReferenced_Table_ID (0);
SetSeqNo (0);	// @SQL=SELECT COALESCE(MAX(SeqNo),0)+10 AS DefaultValue FROM VAF_DBViewElement WHERE AD_View_ID=@AD_View_ID@
}
 */
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAF_DBViewElement (Context ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName)
{
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAF_DBViewElement (Ctx ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName)
{
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAF_DBViewElement (Ctx ctx, IDataReader dr, Trx trxName) : base(ctx, dr, trxName)
{
}
/** Static Constructor 
 Set Table ID By Table Name
 added by ->Harwinder */
static X_VAF_DBViewElement()
{
 Table_ID = Get_Table_ID(Table_Name);
 model = new KeyNamePair(Table_ID,Table_Name);
}
/** Serial Version No */
//static long serialVersionUID 27562514365918L;
/** Last Updated Timestamp 7/29/2010 1:07:29 PM */
public static long updatedMS = 1280389049129L;
/** VAF_TableView_ID=934 */
public static int Table_ID;
 // =934;

/** TableName=VAF_DBViewElement */
public static String Table_Name="VAF_DBViewElement";

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
StringBuilder sb = new StringBuilder ("X_VAF_DBViewElement[").Append(Get_ID()).Append("]");
return sb.ToString();
}
/** Set Table.
@param VAF_TableView_ID Database Table information */
public void SetVAF_TableView_ID (int VAF_TableView_ID)
{
if (VAF_TableView_ID <= 0) Set_ValueNoCheck ("VAF_TableView_ID", null);
else
Set_ValueNoCheck ("VAF_TableView_ID", VAF_TableView_ID);
}
/** Get Table.
@return Database Table information */
public int GetVAF_TableView_ID() 
{
Object ii = Get_Value("VAF_TableView_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Get Record ID/ColumnName
@return ID/ColumnName pair */
public KeyNamePair GetKeyNamePair() 
{
return new KeyNamePair(Get_ID(), GetVAF_TableView_ID().ToString());
}
/** Set View Component.
@param VAF_DBViewElement_ID Component (Select statement) of the view */
public void SetVAF_DBViewElement_ID (int VAF_DBViewElement_ID)
{
if (VAF_DBViewElement_ID < 1) throw new ArgumentException ("VAF_DBViewElement_ID is mandatory.");
Set_ValueNoCheck ("VAF_DBViewElement_ID", VAF_DBViewElement_ID);
}
/** Get View Component.
@return Component (Select statement) of the view */
public int GetVAF_DBViewElement_ID() 
{
Object ii = Get_Value("VAF_DBViewElement_ID");
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
/** Set Sql FROM.
@param FromClause SQL FROM clause */
public void SetFromClause (String FromClause)
{
if (FromClause == null) throw new ArgumentException ("FromClause is mandatory.");
if (FromClause.Length > 2000)
{
log.Warning("Length > 2000 - truncated");
FromClause = FromClause.Substring(0,2000);
}
Set_Value ("FromClause", FromClause);
}
/** Get Sql FROM.
@return SQL FROM clause */
public String GetFromClause() 
{
return (String)Get_Value("FromClause");
}
/** Set Comment.
@param Help Comment, Help or Hint */
public void SetHelp (String Help)
{
if (Help != null && Help.Length > 2000)
{
log.Warning("Length > 2000 - truncated");
Help = Help.Substring(0,2000);
}
Set_Value ("Help", Help);
}
/** Get Comment.
@return Comment, Help or Hint */
public String GetHelp() 
{
return (String)Get_Value("Help");
}
/** Set Name.
@param Name Alphanumeric identifier of the entity */
public void SetName (String Name)
{
if (Name == null) throw new ArgumentException ("Name is mandatory.");
if (Name.Length > 120)
{
log.Warning("Length > 120 - truncated");
Name = Name.Substring(0,120);
}
Set_Value ("Name", Name);
}
/** Get Name.
@return Alphanumeric identifier of the entity */
public String GetName() 
{
return (String)Get_Value("Name");
}
/** Set Other SQL Clause.
@param OtherClause Other SQL Clause */
public void SetOtherClause (String OtherClause)
{
if (OtherClause != null && OtherClause.Length > 2000)
{
log.Warning("Length > 2000 - truncated");
OtherClause = OtherClause.Substring(0,2000);
}
Set_Value ("OtherClause", OtherClause);
}
/** Get Other SQL Clause.
@return Other SQL Clause */
public String GetOtherClause() 
{
return (String)Get_Value("OtherClause");
}

/** Referenced_Table_ID VAF_Control_Ref_ID=415 */
public static int REFERENCED_TABLE_ID_VAF_Control_Ref_ID=415;
/** Set Referenced Table.
@param Referenced_Table_ID Referenced Table */
public void SetReferenced_Table_ID (int Referenced_Table_ID)
{
if (Referenced_Table_ID < 1) throw new ArgumentException ("Referenced_Table_ID is mandatory.");
Set_Value ("Referenced_Table_ID", Referenced_Table_ID);
}
/** Get Referenced Table.
@return Referenced Table */
public int GetReferenced_Table_ID() 
{
Object ii = Get_Value("Referenced_Table_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set Sequence.
@param SeqNo Method of ordering elements;
 lowest number comes first */
public void SetSeqNo (int SeqNo)
{
Set_Value ("SeqNo", SeqNo);
}
/** Get Sequence.
@return Method of ordering elements;
 lowest number comes first */
public int GetSeqNo() 
{
Object ii = Get_Value("SeqNo");
if (ii == null) return 0;
return Convert.ToInt32(ii);
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
