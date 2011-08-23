using System;
using System.Diagnostics;


namespace Community.CsharpSqlite
{
#if TCLSH
  using tcl.lang;
  using sqlite_int64 = System.Int64;
  using sqlite3_stmt = Sqlite3.Vdbe;
  using sqlite3_value = Sqlite3.Mem;
  using Tcl_Interp = tcl.lang.Interp;
  using Tcl_Obj = tcl.lang.TclObject;
  using ClientData = System.Object;

  public partial class Sqlite3
  {
  /*
** 2006 June 13
**
** The author disclaims copyright to this source code.  In place of
** a legal notice, here is a blessing:
**
**    May you do good and not evil.
**    May you find forgiveness for yourself and forgive others.
**    May you share freely, never taking more than you give.
**
*************************************************************************
** Code for testing the virtual table interfaces.  This code
** is not included in the SQLite library.  It is used for automated
** testing of the SQLite library.
**
** The emphasis of this file is a virtual table that provides
** access to TCL variables.
*/
//#include "sqliteInt.h"
//#include "tcl.h"
//#include <stdlib.h>
//#include <string.h>

#if !SQLITE_OMIT_VIRTUALTABLE

//typedef struct tclvar_vtab tclvar_vtab;
//typedef struct tclvar_cursor tclvar_cursor;

/* 
** A tclvar virtual-table object 
*/
class tclvar_vtab : sqlite3_vtab {
  //sqlite3_vtab base;
  public Tcl_Interp interp;
};

/* A tclvar cursor object */
    class tclvar_cursor : sqlite3_vtab_cursor {
  //sqlite3_vtab_cursor base;

  public Tcl_Obj pList1;     /* Result of [info vars ?pattern?] */
  public Tcl_Obj pList2;     /* Result of [array names [lindex $pList1 $i1]] */
  public int i1;             /* Current item in pList1 */
  public int i2;             /* Current item (if any) in pList2 */
};

/* Methods for the tclvar module */
static int tclvarConnect(
      sqlite3 db,
      object pAux,
      int argc,
      string[] argv,
      out sqlite3_vtab ppVtab,
      out string pzErr
){
  tclvar_vtab pVtab;
  string zSchema = 
     "CREATE TABLE whatever(name TEXT, arrayname TEXT, value TEXT)";
  pVtab = new tclvar_vtab();//sqlite3MallocZero( sizeof(*pVtab) );
  //if( pVtab==0 ) return SQLITE_NOMEM;
  ppVtab = pVtab;//*ppVtab = pVtab.base;
  pVtab.interp = (Tcl_Interp)pAux;
  sqlite3_declare_vtab(db, zSchema);
  pzErr = "";
  return SQLITE_OK;
}
/* Note that for this virtual table, the xCreate and xConnect
** methods are identical. */

static int tclvarDisconnect(ref object pVtab){
  //sqlite3_free(pVtab);
  pVtab = null;
  return SQLITE_OK;
}
/* The xDisconnect and xDestroy methods are also the same */

/*
** Open a new tclvar cursor.
*/
static int tclvarOpen( sqlite3_vtab pVTab, out sqlite3_vtab_cursor ppCursor )
{
  //tclvar_cursor pCur;
  //pCur = sqlite3MallocZero(sizeof(tclvar_cursor));
  //*ppCursor = pCur.base;
  ppCursor = new tclvar_cursor();
  return SQLITE_OK;
}

/*
** Close a tclvar cursor.
*/
static int tclvarClose( ref sqlite3_vtab_cursor cur )
{
  tclvar_cursor pCur = (tclvar_cursor )cur;
  if( pCur.pList1 != null){
    TCL.Tcl_DecrRefCount(ref pCur.pList1);
  }
  if( pCur.pList2 != null){
    TCL.Tcl_DecrRefCount( ref pCur.pList2 );
  }
  cur = null;//sqlite3_free(pCur);
  return SQLITE_OK;
}

/*
** Returns 1 if data is ready, or 0 if not.
*/
static int next2(Tcl_Interp interp, tclvar_cursor pCur, Tcl_Obj pObj){
  Tcl_Obj p;

  if( pObj != null){
    if( null==pCur.pList2 ){
      p = TCL.Tcl_NewStringObj("array names", -1);
      TCL.Tcl_IncrRefCount(p);
      TCL.Tcl_ListObjAppendElement(null, p, pObj);
      TCL.Tcl_EvalObjEx(interp, p, TCL.TCL_EVAL_GLOBAL);
      TCL.Tcl_DecrRefCount(ref p);
      pCur.pList2 = TCL.Tcl_GetObjResult(interp);
      TCL.Tcl_IncrRefCount(pCur.pList2);
      Debug.Assert( pCur.i2 == 0 );
    }
    else
    {
      int n = 0;
      pCur.i2++;
      TCL.Tcl_ListObjLength(null, pCur.pList2, out n);
      if( pCur.i2>=n ){
        TCL.Tcl_DecrRefCount(ref pCur.pList2);
        pCur.pList2 = null;
        pCur.i2 = 0;
        return 0;
      }
    }
  }

  return 1;
}

static int tclvarNext(sqlite3_vtab_cursor cur){
  Tcl_Obj pObj = null;
  int n = 0;
  int ok = 0;

  tclvar_cursor pCur = (tclvar_cursor )cur;
  Tcl_Interp interp = ((tclvar_vtab )(cur.pVtab)).interp;

  TCL.Tcl_ListObjLength( null, pCur.pList1, out n );
  while( 0==ok && pCur.i1<n ){
    TCL.Tcl_ListObjIndex( null, pCur.pList1, pCur.i1, out pObj );
    ok = next2(interp, pCur, pObj);
    if( 0==ok ){
      pCur.i1++;
    }
  }

  return 0;
}

static int tclvarFilter(
  sqlite3_vtab_cursor pVtabCursor, 
  int idxNum, string idxStr,
  int argc, sqlite3_value[] argv
){
  tclvar_cursor pCur = (tclvar_cursor )pVtabCursor;
  Tcl_Interp interp = ((tclvar_vtab )(pVtabCursor.pVtab)).interp;

  Tcl_Obj p = TCL.Tcl_NewStringObj("info vars", -1);
  TCL.Tcl_IncrRefCount(p);

  Debug.Assert( argc==0 || argc==1 );
  if( argc==1 ){
    Tcl_Obj pArg = TCL.Tcl_NewStringObj((string)sqlite3_value_text(argv[0]), -1);
    TCL.Tcl_ListObjAppendElement(null, p, pArg);
  }
  TCL.Tcl_EvalObjEx(interp, p, TCL.TCL_EVAL_GLOBAL);
  if( pCur.pList1 !=null){
    TCL.Tcl_DecrRefCount(ref pCur.pList1);
  }
  if( pCur.pList2 !=null ){
    TCL.Tcl_DecrRefCount(ref pCur.pList2);
    pCur.pList2 = null;
  }
  pCur.i1 = 0;
  pCur.i2 = 0;
  pCur.pList1 = TCL.Tcl_GetObjResult(interp);
  TCL.Tcl_IncrRefCount(pCur.pList1);
  Debug.Assert( pCur.i1==0 && pCur.i2==0 && pCur.pList2==null );

  TCL.Tcl_DecrRefCount(ref p);
  return tclvarNext(pVtabCursor);
}

static int tclvarColumn(sqlite3_vtab_cursor cur, sqlite3_context ctx, int i){
  Tcl_Obj p1=null;
  Tcl_Obj p2=null;
  string z1; 
  string z2 = "";
  tclvar_cursor pCur = (tclvar_cursor)cur;
  Tcl_Interp interp = ((tclvar_vtab )cur.pVtab).interp;

  TCL.Tcl_ListObjIndex( interp, pCur.pList1, pCur.i1, out p1 );
  TCL.Tcl_ListObjIndex( interp, pCur.pList2, pCur.i2, out p2 );
  z1 = TCL.Tcl_GetString(p1);
  if( p2!=null ){
    z2 = TCL.Tcl_GetString(p2);
  }
  switch (i) {
    case 0: {
      sqlite3_result_text(ctx, z1, -1, SQLITE_TRANSIENT);
      break;
    }
    case 1: {
      sqlite3_result_text(ctx, z2, -1, SQLITE_TRANSIENT);
      break;
    }
    case 2: {
      Tcl_Obj pVal = TCL.Tcl_GetVar2Ex( interp, z1, z2 == "" ? null : z2, (TCL.VarFlag)TCL.TCL_GLOBAL_ONLY );
      sqlite3_result_text( ctx, TCL.Tcl_GetString( pVal ), -1, SQLITE_TRANSIENT );
      break;
    }
  }
  return SQLITE_OK;
}

static int tclvarRowid( sqlite3_vtab_cursor cur, out sqlite_int64 pRowid )
{
  pRowid = 0;
  return SQLITE_OK;
}

static int tclvarEof(sqlite3_vtab_cursor cur){
  tclvar_cursor pCur = (tclvar_cursor)cur;
  return ( pCur.pList2 != null ? 0 : 1 );
}

static int tclvarBestIndex( sqlite3_vtab tab, ref sqlite3_index_info pIdxInfo )
{
  int ii;

  for(ii=0; ii<pIdxInfo.nConstraint; ii++){
    sqlite3_index_constraint  pCons = pIdxInfo.aConstraint[ii];
    if( pCons.iColumn==0 && pCons.usable
           && pCons.op==SQLITE_INDEX_CONSTRAINT_EQ ){
      sqlite3_index_constraint_usage pUsage = new sqlite3_index_constraint_usage();
      pUsage = pIdxInfo.aConstraintUsage[ii];
      pUsage.omit = false;
      pUsage.argvIndex = 1;
      return SQLITE_OK;
    }
  }

  for(ii=0; ii<pIdxInfo.nConstraint; ii++){
    sqlite3_index_constraint pCons = pIdxInfo.aConstraint[ii];
    if( pCons.iColumn==0 && pCons.usable
           && pCons.op==SQLITE_INDEX_CONSTRAINT_MATCH ){
      sqlite3_index_constraint_usage pUsage = new sqlite3_index_constraint_usage();
      pUsage = pIdxInfo.aConstraintUsage[ii];
      pUsage.omit = true;
      pUsage.argvIndex = 1;
      return SQLITE_OK;
    }
  }

  return SQLITE_OK;
}

/*
** A virtual table module that provides read-only access to a
** Tcl global variable namespace.
*/
static sqlite3_module tclvarModule = new sqlite3_module(
  0,                         /* iVersion */
  tclvarConnect,
  tclvarConnect,
  tclvarBestIndex,
  tclvarDisconnect, 
  tclvarDisconnect,
  tclvarOpen,                  /* xOpen - open a cursor */
  tclvarClose,                 /* xClose - close a cursor */
  tclvarFilter,                /* xFilter - configure scan constraints */
  tclvarNext,                  /* xNext - advance a cursor */
  tclvarEof,                   /* xEof - check for end of scan */
  tclvarColumn,                /* xColumn - read data */
  tclvarRowid,                 /* xRowid - read data */
  null,                        /* xUpdate */
  null,                        /* xBegin */
  null,                        /* xSync */
  null,                        /* xCommit */
  null,                        /* xRollback */
  null,                        /* xFindMethod */
  null                         /* xRename */
);

/*
** Decode a pointer to an sqlite3 object.
*/
//extern int getDbPointer(Tcl_Interp *interp, string zA, sqlite3 **ppDb);

/*
** Register the echo virtual table module.
*/
static int register_tclvar_module(
  ClientData clientData,/* Pointer to sqlite3_enable_XXX function */
  Tcl_Interp interp,    /* The TCL interpreter that invoked this command */
  int objc,             /* Number of arguments */
  Tcl_Obj[] objv        /* Command arguments */
){
  sqlite3 db=null;
  if( objc!=2 ){
    TCL.Tcl_WrongNumArgs(interp, 1, objv, "DB");
    return TCL.TCL_ERROR;
  }
  if( getDbPointer(interp, TCL.Tcl_GetString(objv[1]), out db) !=0) return TCL.TCL_ERROR;
#if !SQLITE_OMIT_VIRTUALTABLE
  sqlite3_create_module(db, "tclvar", tclvarModule, interp);
#endif
  return TCL.TCL_OK;
}

#endif


/*
** Register commands with the TCL interpreter.
*/
static public int Sqlitetesttclvar_Init(Tcl_Interp interp){
#if !SQLITE_OMIT_VIRTUALTABLE
  //static struct {
  //   char *zName;
  //   Tcl_ObjCmdProc *xProc;
  //   void clientData;
  //} 
  _aObjCmd[] aObjCmd = new _aObjCmd[]  {
     new _aObjCmd( "register_tclvar_module",   register_tclvar_module, 0 ),
  };
  int i;
  for(i=0; i<aObjCmd.Length;i++)//sizeof(aObjCmd)/sizeof(aObjCmd[0]); i++)
  {
    TCL.Tcl_CreateObjCommand(interp, aObjCmd[i].zName, 
        aObjCmd[i].xProc, aObjCmd[i].clientData, null);
  }
#endif
  return TCL.TCL_OK;
}
  }
#endif
}
