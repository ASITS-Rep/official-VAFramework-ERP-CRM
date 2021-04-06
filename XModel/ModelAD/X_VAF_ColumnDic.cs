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
/** Generated Model for VAF_ColumnDic
 *  @author Jagmohan Bhatt (generated) 
 *  @version Vienna Framework 1.1.1 - $Id$ */
public class X_VAF_ColumnDic : PO
{
public X_VAF_ColumnDic (Context ctx, int VAF_ColumnDic_ID, Trx trxName) : base (ctx, VAF_ColumnDic_ID, trxName)
{
/** if (VAF_ColumnDic_ID == 0)
{
SetVAF_ColumnDic_ID (0);
SetColumnName (null);
SetRecordType (null);	// U
SetName (null);
SetPrintName (null);
}
 */
}
public X_VAF_ColumnDic (Ctx ctx, int VAF_ColumnDic_ID, Trx trxName) : base (ctx, VAF_ColumnDic_ID, trxName)
{
/** if (VAF_ColumnDic_ID == 0)
{
SetVAF_ColumnDic_ID (0);
SetColumnName (null);
SetRecordType (null);	// U
SetName (null);
SetPrintName (null);
}
 */
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAF_ColumnDic (Context ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName)
{
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAF_ColumnDic (Ctx ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName)
{
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAF_ColumnDic (Ctx ctx, IDataReader dr, Trx trxName) : base(ctx, dr, trxName)
{
}
/** Static Constructor 
 Set Table ID By Table Name
 added by ->Harwinder */
static X_VAF_ColumnDic()
{
 Table_ID = Get_Table_ID(Table_Name);
 model = new KeyNamePair(Table_ID,Table_Name);
}
/** Serial Version No */
//static long serialVersionUID = 27562514361138L;
/** Last Updated Timestamp 7/29/2010 1:07:24 PM */
public static long updatedMS = 1280389044349L;
/** VAF_TableView_ID=276 */
public static int Table_ID;
 // =276;

/** TableName=VAF_ColumnDic */
public static String Table_Name="VAF_ColumnDic";

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
StringBuilder sb = new StringBuilder ("X_VAF_ColumnDic[").Append(Get_ID()).Append("]");
return sb.ToString();
}
/** Set System Element.
@param VAF_ColumnDic_ID System Element enables the central maintenance of column description and help. */
public void SetVAF_ColumnDic_ID (int VAF_ColumnDic_ID)
{
if (VAF_ColumnDic_ID < 1) throw new ArgumentException ("VAF_ColumnDic_ID is mandatory.");
Set_ValueNoCheck ("VAF_ColumnDic_ID", VAF_ColumnDic_ID);
}
/** Get System Element.
@return System Element enables the central maintenance of column description and help. */
public int GetVAF_ColumnDic_ID() 
{
Object ii = Get_Value("VAF_ColumnDic_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}

/** VAF_Control_Ref_ID VAF_Control_Ref_ID=1 */
public static int VAF_CONTROL_REF_ID_VAF_Control_Ref_ID=1;
/** Set Reference.
@param VAF_Control_Ref_ID System Reference and Validation */
public void SetVAF_Control_Ref_ID (int VAF_Control_Ref_ID)
{
if (VAF_Control_Ref_ID <= 0) Set_Value ("VAF_Control_Ref_ID", null);
else
Set_Value ("VAF_Control_Ref_ID", VAF_Control_Ref_ID);
}
/** Get Reference.
@return System Reference and Validation */
public int GetVAF_Control_Ref_ID() 
{
Object ii = Get_Value("VAF_Control_Ref_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}

/** VAF_Control_Ref_Value_ID VAF_Control_Ref_ID=4 */
public static int VAF_CONTROL_REF_VALUE_ID_VAF_Control_Ref_ID=4;
/** Set Reference Key.
@param VAF_Control_Ref_Value_ID Required to specify, if data type is Table or List */
public void SetVAF_Control_Ref_Value_ID (int VAF_Control_Ref_Value_ID)
{
if (VAF_Control_Ref_Value_ID <= 0) Set_Value ("VAF_Control_Ref_Value_ID", null);
else
Set_Value ("VAF_Control_Ref_Value_ID", VAF_Control_Ref_Value_ID);
}
/** Get Reference Key.
@return Required to specify, if data type is Table or List */
public int GetVAF_Control_Ref_Value_ID() 
{
Object ii = Get_Value("VAF_Control_Ref_Value_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set Dynamic Validation.
@param VAF_DataVal_Rule_ID Dynamic Validation Rule */
public void SetVAF_DataVal_Rule_ID (int VAF_DataVal_Rule_ID)
{
if (VAF_DataVal_Rule_ID <= 0) Set_Value ("VAF_DataVal_Rule_ID", null);
else
Set_Value ("VAF_DataVal_Rule_ID", VAF_DataVal_Rule_ID);
}
/** Get Dynamic Validation.
@return Dynamic Validation Rule */
public int GetVAF_DataVal_Rule_ID() 
{
Object ii = Get_Value("VAF_DataVal_Rule_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set DB Column Name.
@param ColumnName Name of the column in the database */
public void SetColumnName (String ColumnName)
{
if (ColumnName == null) throw new ArgumentException ("ColumnName is mandatory.");
if (ColumnName.Length > 40)
{
log.Warning("Length > 40 - truncated");
ColumnName = ColumnName.Substring(0,40);
}
Set_Value ("ColumnName", ColumnName);
}
/** Get DB Column Name.
@return Name of the column in the database */
public String GetColumnName() 
{
return (String)Get_Value("ColumnName");
}
/** Get Record ID/ColumnName
@return ID/ColumnName pair */
public KeyNamePair GetKeyNamePair() 
{
return new KeyNamePair(Get_ID(), GetColumnName());
}

/** DBDataType VAF_Control_Ref_ID=422 */
public static int DBDATATYPE_VAF_Control_Ref_ID=422;
/** Binary LOB = B */
public static String DBDATATYPE_BinaryLOB = "B";
/** Character Fixed = C */
public static String DBDATATYPE_CharacterFixed = "C";
/** Decimal = D */
public static String DBDATATYPE_Decimal = "D";
/** Integer = I */
public static String DBDATATYPE_Integer = "I";
/** Character LOB = L */
public static String DBDATATYPE_CharacterLOB = "L";
/** Number = N */
public static String DBDATATYPE_Number = "N";
/** Timestamp = T */
public static String DBDATATYPE_Timestamp = "T";
/** Character Variable = V */
public static String DBDATATYPE_CharacterVariable = "V";
/** Is test a valid value.
@param test testvalue
@returns true if valid **/
public bool IsDBDataTypeValid (String test)
{
return test == null || test.Equals("B") || test.Equals("C") || test.Equals("D") || test.Equals("I") || test.Equals("L") || test.Equals("N") || test.Equals("T") || test.Equals("V");
}
/** Set Data Type.
@param DBDataType Database Data Type */
public void SetDBDataType (String DBDataType)
{
if (!IsDBDataTypeValid(DBDataType))
throw new ArgumentException ("DBDataType Invalid value - " + DBDataType + " - Reference_ID=422 - B - C - D - I - L - N - T - V");
if (DBDataType != null && DBDataType.Length > 1)
{
log.Warning("Length > 1 - truncated");
DBDataType = DBDataType.Substring(0,1);
}
Set_Value ("DBDataType", DBDataType);
}
/** Get Data Type.
@return Database Data Type */
public String GetDBDataType() 
{
return (String)Get_Value("DBDataType");
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
/** Set Length.
@param FieldLength Length of the column in the database */
public void SetFieldLength (int FieldLength)
{
Set_Value ("FieldLength", FieldLength);
}
/** Get Length.
@return Length of the column in the database */
public int GetFieldLength() 
{
Object ii = Get_Value("FieldLength");
if (ii == null) return 0;
return Convert.ToInt32(ii);
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
/** Set PO Description.
@param PO_Description Description in PO Screens */
public void SetPO_Description (String PO_Description)
{
if (PO_Description != null && PO_Description.Length > 255)
{
log.Warning("Length > 255 - truncated");
PO_Description = PO_Description.Substring(0,255);
}
Set_Value ("PO_Description", PO_Description);
}
/** Get PO Description.
@return Description in PO Screens */
public String GetPO_Description() 
{
return (String)Get_Value("PO_Description");
}
/** Set PO Help.
@param PO_Help Help for PO Screens */
public void SetPO_Help (String PO_Help)
{
if (PO_Help != null && PO_Help.Length > 2000)
{
log.Warning("Length > 2000 - truncated");
PO_Help = PO_Help.Substring(0,2000);
}
Set_Value ("PO_Help", PO_Help);
}
/** Get PO Help.
@return Help for PO Screens */
public String GetPO_Help() 
{
return (String)Get_Value("PO_Help");
}
/** Set PO Name.
@param PO_Name Name on PO Screens */
public void SetPO_Name (String PO_Name)
{
if (PO_Name != null && PO_Name.Length > 60)
{
log.Warning("Length > 60 - truncated");
PO_Name = PO_Name.Substring(0,60);
}
Set_Value ("PO_Name", PO_Name);
}
/** Get PO Name.
@return Name on PO Screens */
public String GetPO_Name() 
{
return (String)Get_Value("PO_Name");
}
/** Set PO Print name.
@param PO_PrintName Print name on PO Screens/Reports */
public void SetPO_PrintName (String PO_PrintName)
{
if (PO_PrintName != null && PO_PrintName.Length > 60)
{
log.Warning("Length > 60 - truncated");
PO_PrintName = PO_PrintName.Substring(0,60);
}
Set_Value ("PO_PrintName", PO_PrintName);
}
/** Get PO Print name.
@return Print name on PO Screens/Reports */
public String GetPO_PrintName() 
{
return (String)Get_Value("PO_PrintName");
}
/** Set Print Text.
@param PrintName The label text to be printed on a document or correspondence. */
public void SetPrintName (String PrintName)
{
if (PrintName == null) throw new ArgumentException ("PrintName is mandatory.");
if (PrintName.Length > 60)
{
log.Warning("Length > 60 - truncated");
PrintName = PrintName.Substring(0,60);
}
Set_Value ("PrintName", PrintName);
}
/** Get Print Text.
@return The label text to be printed on a document or correspondence. */
public String GetPrintName() 
{
return (String)Get_Value("PrintName");
}
}

}
