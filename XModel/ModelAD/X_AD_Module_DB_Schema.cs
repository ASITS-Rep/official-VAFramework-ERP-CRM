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
/** Generated Model for VAF_Module_DB_Schema
 *  @author Jagmohan Bhatt (generated) 
 *  @version Vienna Framework 1.1.1 - $Id$ */
public class X_VAF_Module_DB_Schema : PO
{
public X_VAF_Module_DB_Schema (Context ctx, int VAF_Module_DB_Schema_ID, Trx trxName) : base (ctx, VAF_Module_DB_Schema_ID, trxName)
{
/** if (VAF_Module_DB_Schema_ID == 0)
{
SetVAF_ModuleInfo_ID (0);
SetVAF_Module_DB_Schema_ID (0);
}
 */
}
public X_VAF_Module_DB_Schema (Ctx ctx, int VAF_Module_DB_Schema_ID, Trx trxName) : base (ctx, VAF_Module_DB_Schema_ID, trxName)
{
/** if (VAF_Module_DB_Schema_ID == 0)
{
SetVAF_ModuleInfo_ID (0);
SetVAF_Module_DB_Schema_ID (0);
}
 */
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAF_Module_DB_Schema (Context ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName)
{
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAF_Module_DB_Schema (Ctx ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName)
{
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_VAF_Module_DB_Schema (Ctx ctx, IDataReader dr, Trx trxName) : base(ctx, dr, trxName)
{
}
/** Static Constructor 
 Set Table ID By Table Name
 added by ->Harwinder */
static X_VAF_Module_DB_Schema()
{
 Table_ID = Get_Table_ID(Table_Name);
 model = new KeyNamePair(Table_ID,Table_Name);
}
/** Serial Version No */
//static long serialVersionUID = 27622812283665L;
/** Last Updated Timestamp 6/26/2012 10:32:46 AM */
public static long updatedMS = 1340686966876L;
/** VAF_TableView_ID=1000060 */
public static int Table_ID;
 // =1000060;

/** TableName=VAF_Module_DB_Schema */
public static String Table_Name="VAF_Module_DB_Schema";

protected static KeyNamePair model;
protected Decimal accessLevel = new Decimal(7);
/** AccessLevel
@return 7 - System - Client - Org 
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
StringBuilder sb = new StringBuilder ("X_VAF_Module_DB_Schema[").Append(Get_ID()).Append("]");
return sb.ToString();
}
/** Set Module.
@param VAF_ModuleInfo_ID Module */
public void SetVAF_ModuleInfo_ID (int VAF_ModuleInfo_ID)
{
if (VAF_ModuleInfo_ID < 1) throw new ArgumentException ("VAF_ModuleInfo_ID is mandatory.");
Set_ValueNoCheck ("VAF_ModuleInfo_ID", VAF_ModuleInfo_ID);
}
/** Get Module.
@return Module */
public int GetVAF_ModuleInfo_ID() 
{
Object ii = Get_Value("VAF_ModuleInfo_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set VAF_Module_DB_Schema_ID.
@param VAF_Module_DB_Schema_ID VAF_Module_DB_Schema_ID */
public void SetVAF_Module_DB_Schema_ID (int VAF_Module_DB_Schema_ID)
{
if (VAF_Module_DB_Schema_ID < 1) throw new ArgumentException ("VAF_Module_DB_Schema_ID is mandatory.");
Set_ValueNoCheck ("VAF_Module_DB_Schema_ID", VAF_Module_DB_Schema_ID);
}
/** Get VAF_Module_DB_Schema_ID.
@return VAF_Module_DB_Schema_ID */
public int GetVAF_Module_DB_Schema_ID() 
{
Object ii = Get_Value("VAF_Module_DB_Schema_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set Table.
@param VAF_TableView_ID Database Table information */
public void SetVAF_TableView_ID (int VAF_TableView_ID)
{
if (VAF_TableView_ID <= 0) Set_Value ("VAF_TableView_ID", null);
else
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
/** Set Generate XML Files.
@param GenerateXMLFiles Generate XML Files */
public void SetGenerateXMLFiles (String GenerateXMLFiles)
{
if (GenerateXMLFiles != null && GenerateXMLFiles.Length > 50)
{
log.Warning("Length > 50 - truncated");
GenerateXMLFiles = GenerateXMLFiles.Substring(0,50);
}
Set_Value ("GenerateXMLFiles", GenerateXMLFiles);
}
/** Get Generate XML Files.
@return Generate XML Files */
public String GetGenerateXMLFiles() 
{
return (String)Get_Value("GenerateXMLFiles");
}
/** Set Name.
@param Name Alphanumeric identifier of the entity */
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
@return Alphanumeric identifier of the entity */
public String GetName() 
{
return (String)Get_Value("Name");
}
/** Set Record ID.
@param Record_ID Direct internal record ID */
public void SetRecord_ID (int Record_ID)
{
if (Record_ID <= 0) Set_Value ("Record_ID", null);
else
Set_Value ("Record_ID", Record_ID);
}
/** Get Record ID.
@return Direct internal record ID */
public int GetRecord_ID() 
{
Object ii = Get_Value("Record_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}

/** Set Name.
@param Name Alphanumeric identifier of the entity */
public void SetTableName(String TableName)
{
    if (TableName != null && TableName.Length > 50)
    {
        log.Warning("Length > 50 - truncated");
        TableName = TableName.Substring(0, 50);
    }
    Set_Value("TableName", TableName);
}
/** Get Name.
@return Alphanumeric identifier of the entity */
public String GetTableName()
{
    return (String)Get_Value("TableName");
}

}

}
