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
/** Generated Model for W_ClickCount
 *  @author Jagmohan Bhatt (generated) 
 *  @version Vienna Framework 1.1.1 - $Id$ */
public class X_W_ClickCount : PO
{
public X_W_ClickCount (Context ctx, int W_ClickCount_ID, Trx trxName) : base (ctx, W_ClickCount_ID, trxName)
{
/** if (W_ClickCount_ID == 0)
{
SetName (null);
SetTargetURL (null);
SetW_ClickCount_ID (0);
}
 */
}
public X_W_ClickCount (Ctx ctx, int W_ClickCount_ID, Trx trxName) : base (ctx, W_ClickCount_ID, trxName)
{
/** if (W_ClickCount_ID == 0)
{
SetName (null);
SetTargetURL (null);
SetW_ClickCount_ID (0);
}
 */
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_W_ClickCount (Context ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName)
{
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_W_ClickCount (Ctx ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName)
{
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_W_ClickCount (Ctx ctx, IDataReader dr, Trx trxName) : base(ctx, dr, trxName)
{
}
/** Static Constructor 
 Set Table ID By Table Name
 added by ->Harwinder */
static X_W_ClickCount()
{
 Table_ID = Get_Table_ID(Table_Name);
 model = new KeyNamePair(Table_ID,Table_Name);
}
/** Serial Version No */
//static long serialVersionUID 27562514384960L;
/** Last Updated Timestamp 7/29/2010 1:07:48 PM */
public static long updatedMS = 1280389068171L;
/** VAF_TableView_ID=553 */
public static int Table_ID;
 // =553;

/** TableName=W_ClickCount */
public static String Table_Name="W_ClickCount";

protected static KeyNamePair model;
protected Decimal accessLevel = new Decimal(3);
/** AccessLevel
@return 3 - Client - Org 
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
StringBuilder sb = new StringBuilder ("X_W_ClickCount[").Append(Get_ID()).Append("]");
return sb.ToString();
}

/** C_BPartner_ID VAF_Control_Ref_ID=232 */
public static int C_BPARTNER_ID_VAF_Control_Ref_ID=232;
/** Set Business Partner.
@param C_BPartner_ID Identifies a Business Partner */
public void SetC_BPartner_ID (int C_BPartner_ID)
{
if (C_BPartner_ID <= 0) Set_Value ("C_BPartner_ID", null);
else
Set_Value ("C_BPartner_ID", C_BPartner_ID);
}
/** Get Business Partner.
@return Identifies a Business Partner */
public int GetC_BPartner_ID() 
{
Object ii = Get_Value("C_BPartner_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set Counter.
@param Counter Count Value */
public void SetCounter (int Counter)
{
throw new ArgumentException ("Counter Is virtual column");
}
/** Get Counter.
@return Count Value */
public int GetCounter() 
{
Object ii = Get_Value("Counter");
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
/** Set Target URL.
@param TargetURL URL for the Target */
public void SetTargetURL (String TargetURL)
{
if (TargetURL == null) throw new ArgumentException ("TargetURL is mandatory.");
if (TargetURL.Length > 120)
{
log.Warning("Length > 120 - truncated");
TargetURL = TargetURL.Substring(0,120);
}
Set_Value ("TargetURL", TargetURL);
}
/** Get Target URL.
@return URL for the Target */
public String GetTargetURL() 
{
return (String)Get_Value("TargetURL");
}
/** Set Click Count.
@param W_ClickCount_ID Web Click Management */
public void SetW_ClickCount_ID (int W_ClickCount_ID)
{
if (W_ClickCount_ID < 1) throw new ArgumentException ("W_ClickCount_ID is mandatory.");
Set_ValueNoCheck ("W_ClickCount_ID", W_ClickCount_ID);
}
/** Get Click Count.
@return Web Click Management */
public int GetW_ClickCount_ID() 
{
Object ii = Get_Value("W_ClickCount_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
}

}
