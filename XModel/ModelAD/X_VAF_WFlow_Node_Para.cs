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
/** Generated Model for VAF_WFlow_Node_Para
 *  @author Jagmohan Bhatt (generated) 
 *  @version Vienna Framework 1.1.1 - $Id$ */
public class X_VAF_WFlow_Node_Para : PO
{
public X_VAF_WFlow_Node_Para (Context ctx, int VAF_WFlow_Node_Para_ID, Trx trxName) : base (ctx, VAF_WFlow_Node_Para_ID, trxName)
{
/** if (VAF_WFlow_Node_Para_ID == 0)
{
SetVAF_WFlow_Node_ID (0);
SetVAF_WFlow_Node_Para_ID (0);
SetRecordType (null);	// U
}
 */
}
public X_VAF_WFlow_Node_Para (Ctx ctx, int VAF_WFlow_Node_Para_ID, Trx trxName) : base (ctx, VAF_WFlow_Node_Para_ID, trxName)
{
/** if (VAF_WFlow_Node_Para_ID == 0)
{
SetVAF_WFlow_Node_ID (0);
SetVAF_WFlow_Node_Para_ID (0);
SetRecordType (null);	// U
}
 */
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAF_WFlow_Node_Para (Context ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName)
{
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAF_WFlow_Node_Para (Ctx ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName)
{
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAF_WFlow_Node_Para (Ctx ctx, IDataReader dr, Trx trxName) : base(ctx, dr, trxName)
{
}
/** Static Constructor 
 Set Table ID By Table Name
 added by ->Harwinder */
static X_VAF_WFlow_Node_Para()
{
 Table_ID = Get_Table_ID(Table_Name);
 model = new KeyNamePair(Table_ID,Table_Name);
}
/** Serial Version No */
//static long serialVersionUID 27562514366279L;
/** Last Updated Timestamp 7/29/2010 1:07:29 PM */
public static long updatedMS = 1280389049490L;
/** VAF_TableView_ID=643 */
public static int Table_ID;
 // =643;

/** TableName=VAF_WFlow_Node_Para */
public static String Table_Name="VAF_WFlow_Node_Para";

protected static KeyNamePair model;
protected Decimal accessLevel = new Decimal(6);
/** AccessLevel
@return 6 - System - Client 
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
StringBuilder sb = new StringBuilder ("X_VAF_WFlow_Node_Para[").Append(Get_ID()).Append("]");
return sb.ToString();
}
/** Set Process Parameter.
@param VAF_Job_Para_ID Process Parameter */
public void SetVAF_Job_Para_ID (int VAF_Job_Para_ID)
{
if (VAF_Job_Para_ID <= 0) Set_Value ("VAF_Job_Para_ID", null);
else
Set_Value ("VAF_Job_Para_ID", VAF_Job_Para_ID);
}
/** Get Process Parameter.
@return Process Parameter */
public int GetVAF_Job_Para_ID() 
{
Object ii = Get_Value("VAF_Job_Para_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set Node.
@param VAF_WFlow_Node_ID Workflow Node (activity), step or process */
public void SetVAF_WFlow_Node_ID (int VAF_WFlow_Node_ID)
{
if (VAF_WFlow_Node_ID < 1) throw new ArgumentException ("VAF_WFlow_Node_ID is mandatory.");
Set_ValueNoCheck ("VAF_WFlow_Node_ID", VAF_WFlow_Node_ID);
}
/** Get Node.
@return Workflow Node (activity), step or process */
public int GetVAF_WFlow_Node_ID() 
{
Object ii = Get_Value("VAF_WFlow_Node_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Get Record ID/ColumnName
@return ID/ColumnName pair */
public KeyNamePair GetKeyNamePair() 
{
return new KeyNamePair(Get_ID(), GetVAF_WFlow_Node_ID().ToString());
}
/** Set Workflow Node Parameter.
@param VAF_WFlow_Node_Para_ID Workflow Node Execution Parameter */
public void SetVAF_WFlow_Node_Para_ID (int VAF_WFlow_Node_Para_ID)
{
if (VAF_WFlow_Node_Para_ID < 1) throw new ArgumentException ("VAF_WFlow_Node_Para_ID is mandatory.");
Set_ValueNoCheck ("VAF_WFlow_Node_Para_ID", VAF_WFlow_Node_Para_ID);
}
/** Get Workflow Node Parameter.
@return Workflow Node Execution Parameter */
public int GetVAF_WFlow_Node_Para_ID() 
{
Object ii = Get_Value("VAF_WFlow_Node_Para_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set Attribute Name.
@param AttributeName Name of the Attribute */
public void SetAttributeName (String AttributeName)
{
if (AttributeName != null && AttributeName.Length > 60)
{
log.Warning("Length > 60 - truncated");
AttributeName = AttributeName.Substring(0,60);
}
Set_Value ("AttributeName", AttributeName);
}
/** Get Attribute Name.
@return Name of the Attribute */
public String GetAttributeName() 
{
return (String)Get_Value("AttributeName");
}
/** Set Attribute Value.
@param AttributeValue Value of the Attribute */
public void SetAttributeValue (String AttributeValue)
{
if (AttributeValue != null && AttributeValue.Length > 60)
{
log.Warning("Length > 60 - truncated");
AttributeValue = AttributeValue.Substring(0,60);
}
Set_Value ("AttributeValue", AttributeValue);
}
/** Get Attribute Value.
@return Value of the Attribute */
public String GetAttributeValue() 
{
return (String)Get_Value("AttributeValue");
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
}

}
