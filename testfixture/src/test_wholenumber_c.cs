using System.Diagnostics;
using unsigned = System.UInt32;
using sqlite_int64 = System.Int64;
using sqlite3_int64 = System.Int64;
using sqlite3_value_int64 = System.Int64;

namespace Community.CsharpSqlite
{
#if TCLSH
  using tcl.lang;
  using sqlite3_stmt = Sqlite3.Vdbe;
  using sqlite3_value = Sqlite3.Mem;
  using Tcl_Interp = tcl.lang.Interp;
  using Tcl_Obj = tcl.lang.TclObject;
  using ClientData = System.Object;

  public partial class Sqlite3
  {
    /*
    ** 2011 April 02
    **
    ** The author disclaims copyright to this source code.  In place of
    ** a legal notice, here is a blessing:
    **
    **    May you do good and not evil.
    **    May you find forgiveness for yourself and forgive others.
    **    May you share freely, never taking more than you give.
    **
    *************************************************************************
    **
    ** This file implements a virtual table that returns the whole numbers
    ** between 1 and 4294967295, inclusive.
    **
    ** Example:
    **
    **     CREATE VIRTUAL TABLE nums USING wholenumber;
    **     SELECT value FROM nums WHERE value<10;
    **
    ** Results in:
    **
    **     1 2 3 4 5 6 7 8 9
    */
    //#include "sqlite3.h"
    //#include <assert.h>
    //#include <string.h>

#if !SQLITE_OMIT_VIRTUALTABLE


    /* A wholenumber cursor object */
    //typedef struct wholenumber_cursor wholenumber_cursor;
    class wholenumber_cursor : sqlite3_vtab_cursor
    {
      //public sqlite3_vtab_cursor base;  /* Base class - must be first */
      public unsigned iValue;           /* Current value */
      public unsigned mxValue;          /* Maximum value */
    };

    /* Methods for the wholenumber module */
    static int wholenumberConnect(
    sqlite3 db,
    object pAux,
    int argc, string[] argv,
    out sqlite3_vtab ppVtab,
    out string pzErr
    )
    {
      sqlite3_vtab pNew;
      pNew = ppVtab = new sqlite3_vtab();//sqlite3_malloc( sizeof(*pNew) );
      //if ( pNew == null )
      //  return SQLITE_NOMEM;
      sqlite3_declare_vtab( db, "CREATE TABLE x(value)" );
      //memset(pNew, 0, sizeof(*pNew));
      pzErr = "";
      return SQLITE_OK;
    }
    /* Note that for this virtual table, the xCreate and xConnect
    ** methods are identical. */

    static int wholenumberDisconnect( ref object pVtab )
    {
      pVtab = null;//  sqlite3_free( pVtab );
      return SQLITE_OK;
    }
    /* The xDisconnect and xDestroy methods are also the same */


    /*
    ** Open a new wholenumber cursor.
    */
    static int wholenumberOpen( sqlite3_vtab p, out sqlite3_vtab_cursor ppCursor )
    {
      wholenumber_cursor pCur;
      pCur = new wholenumber_cursor();//sqlite3_malloc( sizeof(*pCur) );
      //if ( pCur == null )
      //  return SQLITE_NOMEM;
      //memset(pCur, 0, sizeof(*pCur));
      ppCursor = pCur;//.base;
      return SQLITE_OK;
    }

    /*
    ** Close a wholenumber cursor.
    */
    static int wholenumberClose( ref sqlite3_vtab_cursor cur )
    {
      cur = null;//  sqlite3_free( ref cur );
      return SQLITE_OK;
    }


    /*
    ** Advance a cursor to its next row of output
    */
    static int wholenumberNext( sqlite3_vtab_cursor cur )
    {
      wholenumber_cursor pCur = (wholenumber_cursor)cur;
      pCur.iValue++;
      return SQLITE_OK;
    }

    /*
    ** Return the value associated with a wholenumber.
    */
    static int wholenumberColumn(
    sqlite3_vtab_cursor cur,
    sqlite3_context ctx,
    int i
    )
    {
      wholenumber_cursor pCur = (wholenumber_cursor)cur;
      sqlite3_result_int64( ctx, pCur.iValue );
      return SQLITE_OK;
    }

    /*
    ** The rowid.
    */
    static int wholenumberRowid( sqlite3_vtab_cursor cur, out sqlite_int64 pRowid )
    {
      wholenumber_cursor pCur = (wholenumber_cursor)cur;
      pRowid = pCur.iValue;
      return SQLITE_OK;
    }

    /*
    ** When the wholenumber_cursor.rLimit value is 0 or less, that is a signal
    ** that the cursor has nothing more to output.
    */
    static int wholenumberEof( sqlite3_vtab_cursor cur )
    {
      wholenumber_cursor pCur = (wholenumber_cursor)cur;
      return ( pCur.iValue > pCur.mxValue || pCur.iValue == 0 ) ? 1 : 0;
    }

    /*
    ** Called to "rewind" a cursor back to the beginning so that
    ** it starts its output over again.  Always called at least once
    ** prior to any wholenumberColumn, wholenumberRowid, or wholenumberEof call.
    **
    **    idxNum   Constraints
    **    ------   ---------------------
    **      0      (none)
    **      1      value > $argv0
    **      2      value >= $argv0
    **      4      value < $argv0
    **      8      value <= $argv0
    **
    **      5      value > $argv0 AND value < $argv1
    **      6      value >= $argv0 AND value < $argv1
    **      9      value > $argv0 AND value <= $argv1
    **     10      value >= $argv0 AND value <= $argv1
    */
    static int wholenumberFilter(
    sqlite3_vtab_cursor pVtabCursor,
    int idxNum, string idxStr,
    int argc, sqlite3_value[] argv
    )
    {
      wholenumber_cursor pCur = (wholenumber_cursor)pVtabCursor;
      sqlite3_int64 v;
      int i = 0;
      pCur.iValue = 1;
      pCur.mxValue = 0xffffffff;  /* 4294967295 */
      if ( ( idxNum & 3 ) != 0 )
      {
        v = sqlite3_value_int64( argv[0] ) + ( idxNum & 1 );
        if ( v > pCur.iValue && v <= pCur.mxValue )
          pCur.iValue = (uint)v;
        i++;
      }
      if ( ( idxNum & 12 ) != 0 )
      {
        v = sqlite3_value_int64( argv[i] ) - ( ( idxNum >> 2 ) & 1 );
        if ( v >= pCur.iValue && v < pCur.mxValue )
          pCur.mxValue = (uint)v;
      }
      return SQLITE_OK;
    }

    /*
    ** Search for terms of these forms:
    **
    **  (1)  value > $value
    **  (2)  value >= $value
    **  (4)  value < $value
    **  (8)  value <= $value
    **
    ** idxNum is an ORed combination of 1 or 2 with 4 or 8.
    */
    static int wholenumberBestIndex(
    sqlite3_vtab vtab,
    ref sqlite3_index_info pIdxInfo
    )
    {
      int i;
      int idxNum = 0;
      int argvIdx = 1;
      int ltIdx = -1;
      int gtIdx = -1;
      sqlite3_index_constraint pConstraint;
      //pConstraint = pIdxInfo.aConstraint;
      for ( i = 0; i < pIdxInfo.nConstraint; i++ )//, pConstraint++)
      {
        pConstraint = pIdxInfo.aConstraint[i];
        if ( pConstraint.usable == false )
          continue;
        if ( ( idxNum & 3 ) == 0 && pConstraint.op == SQLITE_INDEX_CONSTRAINT_GT )
        {
          idxNum |= 1;
          ltIdx = i;
        }
        if ( ( idxNum & 3 ) == 0 && pConstraint.op == SQLITE_INDEX_CONSTRAINT_GE )
        {
          idxNum |= 2;
          ltIdx = i;
        }
        if ( ( idxNum & 12 ) == 0 && pConstraint.op == SQLITE_INDEX_CONSTRAINT_LT )
        {
          idxNum |= 4;
          gtIdx = i;
        }
        if ( ( idxNum & 12 ) == 0 && pConstraint.op == SQLITE_INDEX_CONSTRAINT_LE )
        {
          idxNum |= 8;
          gtIdx = i;
        }
      }
      pIdxInfo.idxNum = idxNum;
      if ( ltIdx >= 0 )
      {
        pIdxInfo.aConstraintUsage[ltIdx].argvIndex = argvIdx++;
        pIdxInfo.aConstraintUsage[ltIdx].omit = true;
      }
      if ( gtIdx >= 0 )
      {
        pIdxInfo.aConstraintUsage[gtIdx].argvIndex = argvIdx;
        pIdxInfo.aConstraintUsage[gtIdx].omit = true;
      }
      if ( pIdxInfo.nOrderBy == 1
      && pIdxInfo.aOrderBy[0].desc == false
      )
      {
        pIdxInfo.orderByConsumed = true;
      }
      pIdxInfo.estimatedCost = (double)1;
      return SQLITE_OK;
    }

    /*
    ** A virtual table module that provides read-only access to a
    ** Tcl global variable namespace.
    */
    static sqlite3_module wholenumberModule = new sqlite3_module(
    2,                         /* iVersion */
    (smdxCreateConnect)wholenumberConnect,
    (smdxCreateConnect)wholenumberConnect,
    (smdxBestIndex)wholenumberBestIndex,
    (smdxDisconnect)wholenumberDisconnect,
    (smdxDestroy)wholenumberDisconnect,
    (smdxOpen)wholenumberOpen,           /* xOpen - open a cursor */
    (smdxClose)wholenumberClose,          /* xClose - close a cursor */
    (smdxFilter)wholenumberFilter,         /* xFilter - configure scan constraints */
    (smdxNext)wholenumberNext,           /* xNext - advance a cursor */
    (smdxEof)wholenumberEof,            /* xEof - check for end of scan */
    (smdxColumn)wholenumberColumn,         /* xColumn - read data */
    (smdxRowid)wholenumberRowid,          /* xRowid - read data */
    null,                         /* xUpdate */
    null,                         /* xBegin */
    null,                         /* xSync */
    null,                         /* xCommit */
    null,                         /* xRollback */
    null,                         /* xFindMethod */
    null,                        /* xRename */
      /* The methods above are in version 1 of the sqlite_module object. Those 
      ** below are for version 2 and greater. */
    null,
    null,
    null
    );

#endif //* SQLITE_OMIT_VIRTUALTABLE */


    /*
** Register the wholenumber virtual table
*/
    static int wholenumber_register( sqlite3 db )
    {
      int rc = SQLITE_OK;
#if !SQLITE_OMIT_VIRTUALTABLE
      rc = sqlite3_create_module( db, "wholenumber", wholenumberModule, null );
#endif
      return rc;
    }

#if SQLITE_TEST
    //#include <tcl.h>
    /*
    ** Decode a pointer to an sqlite3 object.
    */
    //extern int getDbPointer(Tcl_Interp interp, string zA, sqlite3 **ppDb);

    /*
    ** Register the echo virtual table module.
    */
    static int register_wholenumber_module(
    ClientData clientData, /* Pointer to sqlite3_enable_XXX function */
    Tcl_Interp interp,     /* The TCL interpreter that invoked this command */
    int objc,              /* Number of arguments */
    Tcl_Obj[] objv         /* Command arguments */
    )
    {
      sqlite3 db = null;
      if ( objc != 2 )
      {
        TCL.Tcl_WrongNumArgs( interp, 1, objv, "DB" );
        return TCL.TCL_ERROR;
      }
      if ( getDbPointer( interp, TCL.Tcl_GetString( objv[1] ), out db ) != 0 )
        return TCL.TCL_ERROR;
      wholenumber_register( db );
      return TCL.TCL_OK;
    }


    //static class _aObjCmd {
    //   public string zName;
    //   public Tcl_ObjCmdProc xProc;
    //   public object clientData;
    //} 
    /*
    ** Register commands with the TCL interpreter.
    */
    static public int Sqlitetestwholenumber_Init( Tcl_Interp interp )
    {
      _aObjCmd[] aObjCmd = new _aObjCmd[] {
new _aObjCmd( "register_wholenumber_module",   register_wholenumber_module, 0 ),
};
      int i;
      for ( i = 0; i < aObjCmd.Length; i++ )
      {//sizeof(aObjCmd)/sizeof(aObjCmd[0]); i++){
        TCL.Tcl_CreateObjCommand( interp, aObjCmd[i].zName,
        aObjCmd[i].xProc, aObjCmd[i].clientData, null );
      }
      return TCL.TCL_OK;
    }

#endif //* SQLITE_TEST */
  }
#endif
}