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
/** Generated Model for VAF_ContextScope
 *  @author Jagmohan Bhatt (generated) 
 *  @version Vienna Framework 1.1.1 - $Id$ */
public class X_VAF_ContextScope : PO
{
public X_VAF_ContextScope (Context ctx, int VAF_ContextScope_ID, Trx trxName) : base (ctx, VAF_ContextScope_ID, trxName)
{
/** if (VAF_ContextScope_ID == 0)
{
SetVAF_ContextScope_ID (0);
SetRecordType (null);	// U
SetIsSOTrx (false);
SetName (null);
}
 */
}
public X_VAF_ContextScope (Ctx ctx, int VAF_ContextScope_ID, Trx trxName) : base (ctx, VAF_ContextScope_ID, trxName)
{
/** if (VAF_ContextScope_ID == 0)
{
SetVAF_ContextScope_ID (0);
SetRecordType (null);	// U
SetIsSOTrx (false);
SetName (null);
}
 */
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAF_ContextScope (Context ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName)
{
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAF_ContextScope (Ctx ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName)
{
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAF_ContextScope (Ctx ctx, IDataReader dr, Trx trxName) : base(ctx, dr, trxName)
{
}
/** Static Constructor 
 Set Table ID By Table Name
 added by ->Harwinder */
static X_VAF_ContextScope()
{
 Table_ID = Get_Table_ID(Table_Name);
 model = new KeyNamePair(Table_ID,Table_Name);
}
/** Serial Version No */
//static long serialVersionUID = 27562514361075L;
/** Last Updated Timestamp 7/29/2010 1:07:24 PM */
public static long updatedMS = 1280389044286L;
/** VAF_TableView_ID=926 */
public static int Table_ID;
 // =926;

/** TableName=VAF_ContextScope */
public static String Table_Name="VAF_ContextScope";

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
StringBuilder sb = new StringBuilder ("X_VAF_ContextScope[").Append(Get_ID()).Append("]");
return sb.ToString();
}
/** Set Context Area.
@param VAF_ContextScope_ID Business Domain Area Terminology */
public void SetVAF_ContextScope_ID (int VAF_ContextScope_ID)
{
if (VAF_ContextScope_ID < 1) throw new ArgumentException ("VAF_ContextScope_ID is mandatory.");
Set_ValueNoCheck ("VAF_ContextScope_ID", VAF_ContextScope_ID);
}
/** Get Context Area.
@return Business Domain Area Terminology */
public int GetVAF_ContextScope_ID() 
{
Object ii = Get_Value("VAF_ContextScope_ID");
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
/** Set Sales Transaction.
@param IsSOTrx This is a Sales Transaction */
public void SetIsSOTrx (Boolean IsSOTrx)
{
Set_Value ("IsSOTrx", IsSOTrx);
}
/** Get Sales Transaction.
@return This is a Sales Transaction */
public Boolean IsSOTrx() 
{
Object oo = Get_Value("IsSOTrx");
if (oo != null) 
{
 if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo);
 return "Y".Equals(oo);
}
return false;
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
}

}
