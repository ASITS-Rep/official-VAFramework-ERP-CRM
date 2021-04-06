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
/** Generated Model for VAF_RecrodType
 *  @author Jagmohan Bhatt (generated) 
 *  @version Vienna Framework 1.1.1 - $Id$ */
public class X_VAF_RecrodType : PO
{
public X_VAF_RecrodType (Context ctx, int VAF_RecrodType_ID, Trx trxName) : base (ctx, VAF_RecrodType_ID, trxName)
{
/** if (VAF_RecrodType_ID == 0)
{
SetVAF_RecrodType_ID (0);	// @SQL=SELECT NVL(MAX(VAF_RecrodType_ID),999999)+1 FROM VAF_RecrodType WHERE VAF_RecrodType_ID > 1000
SetRecordType (null);	// U
SetName (null);
}
 */
}
public X_VAF_RecrodType (Ctx ctx, int VAF_RecrodType_ID, Trx trxName) : base (ctx, VAF_RecrodType_ID, trxName)
{
/** if (VAF_RecrodType_ID == 0)
{
SetVAF_RecrodType_ID (0);	// @SQL=SELECT NVL(MAX(VAF_RecrodType_ID),999999)+1 FROM VAF_RecrodType WHERE VAF_RecrodType_ID > 1000
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
public X_VAF_RecrodType (Context ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName)
{
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAF_RecrodType (Ctx ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName)
{
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAF_RecrodType (Ctx ctx, IDataReader dr, Trx trxName) : base(ctx, dr, trxName)
{
}
/** Static Constructor 
 Set Table ID By Table Name
 added by ->Harwinder */
static X_VAF_RecrodType()
{
 Table_ID = Get_Table_ID(Table_Name);
 model = new KeyNamePair(Table_ID,Table_Name);
}
/** Serial Version No */
//static long serialVersionUID = 27562514361201L;
/** Last Updated Timestamp 7/29/2010 1:07:24 PM */
public static long updatedMS = 1280389044412L;
/** VAF_TableView_ID=882 */
public static int Table_ID;
 // =882;

/** TableName=VAF_RecrodType */
public static String Table_Name="VAF_RecrodType";

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
StringBuilder sb = new StringBuilder ("X_VAF_RecrodType[").Append(Get_ID()).Append("]");
return sb.ToString();
}
/** Set Entity Type.
@param VAF_RecrodType_ID System Entity Type */
public void SetVAF_RecrodType_ID (int VAF_RecrodType_ID)
{
if (VAF_RecrodType_ID < 1) throw new ArgumentException ("VAF_RecrodType_ID is mandatory.");
Set_ValueNoCheck ("VAF_RecrodType_ID", VAF_RecrodType_ID);
}
/** Get Entity Type.
@return System Entity Type */
public int GetVAF_RecrodType_ID() 
{
Object ii = Get_Value("VAF_RecrodType_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set BinaryData.
@param BinaryData Binary Data */
public void SetBinaryData (Byte[] BinaryData)
{
Set_Value ("BinaryData", BinaryData);
}
/** Get BinaryData.
@return Binary Data */
public Byte[] GetBinaryData() 
{
return (Byte[])Get_Value("BinaryData");
}
/** Set Classpath.
@param Classpath Extension Classpath */
public void SetClasspath (String Classpath)
{
if (Classpath != null && Classpath.Length > 255)
{
log.Warning("Length > 255 - truncated");
Classpath = Classpath.Substring(0,255);
}
Set_Value ("Classpath", Classpath);
}
/** Get Classpath.
@return Extension Classpath */
public String GetClasspath() 
{
return (String)Get_Value("Classpath");
}
/** Set Create Component.
@param CreateComponent Create Component */
public void SetCreateComponent (String CreateComponent)
{
if (CreateComponent != null && CreateComponent.Length > 1)
{
log.Warning("Length > 1 - truncated");
CreateComponent = CreateComponent.Substring(0,1);
}
Set_Value ("CreateComponent", CreateComponent);
}
/** Get Create Component.
@return Create Component */
public String GetCreateComponent() 
{
return (String)Get_Value("CreateComponent");
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
/** Set Entity Type.
@param RecordType Dictionary Entity Type;
 Determines ownership and synchronization */
public void SetRecordType (String RecordType)
{
if (RecordType == null) throw new ArgumentException ("RecordType is mandatory.");
if (RecordType.Length > 4)
{
log.Warning("Length > 4 - truncated");
RecordType = RecordType.Substring(0,4);
}
Set_ValueNoCheck ("RecordType", RecordType);
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
/** Set License Text.
@param LicenseText Text of the License of the Component */
public void SetLicenseText (String LicenseText)
{
Set_Value ("LicenseText", LicenseText);
}
/** Get License Text.
@return Text of the License of the Component */
public String GetLicenseText() 
{
return (String)Get_Value("LicenseText");
}
/** Set ModelPackage.
@param ModelPackage Java Package of the model classes */
public void SetModelPackage (String ModelPackage)
{
if (ModelPackage != null && ModelPackage.Length > 255)
{
log.Warning("Length > 255 - truncated");
ModelPackage = ModelPackage.Substring(0,255);
}
Set_Value ("ModelPackage", ModelPackage);
}
/** Get ModelPackage.
@return Java Package of the model classes */
public String GetModelPackage() 
{
return (String)Get_Value("ModelPackage");
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
/** Set Process Now.
@param Processing Process Now */
public void SetProcessing (Boolean Processing)
{
Set_Value ("Processing", Processing);
}
/** Get Process Now.
@return Process Now */
public Boolean IsProcessing() 
{
Object oo = Get_Value("Processing");
if (oo != null) 
{
 if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo);
 return "Y".Equals(oo);
}
return false;
}
/** Set Record ID.
@param Record_ID Direct internal record ID */
public void SetRecord_ID (int Record_ID)
{
if (Record_ID <= 0) Set_ValueNoCheck ("Record_ID", null);
else
Set_ValueNoCheck ("Record_ID", Record_ID);
}
/** Get Record ID.
@return Direct internal record ID */
public int GetRecord_ID() 
{
Object ii = Get_Value("Record_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set Summary.
@param Summary Textual summary of this request */
public void SetSummary (String Summary)
{
if (Summary != null && Summary.Length > 2000)
{
log.Warning("Length > 2000 - truncated");
Summary = Summary.Substring(0,2000);
}
Set_Value ("Summary", Summary);
}
/** Get Summary.
@return Textual summary of this request */
public String GetSummary() 
{
return (String)Get_Value("Summary");
}
/** Set Version.
@param Version Version of the table definition */
public void SetVersion (String Version)
{
if (Version != null && Version.Length > 20)
{
log.Warning("Length > 20 - truncated");
Version = Version.Substring(0,20);
}
Set_Value ("Version", Version);
}
/** Get Version.
@return Version of the table definition */
public String GetVersion() 
{
return (String)Get_Value("Version");
}
}

}
