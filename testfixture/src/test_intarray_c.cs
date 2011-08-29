using System.Diagnostics;

using u8 = System.Byte;
using u32 = System.UInt32;

namespace Community.CsharpSqlite
{
#if TCLSH
  using tcl.lang;
  using DbPage = Sqlite3.PgHdr;
  using sqlite_int64 = System.Int64;
  using sqlite3_int64 = System.Int64;
  using sqlite3_stmt = Sqlite3.Vdbe;
  using sqlite3_value = Sqlite3.Mem;
  using Tcl_CmdInfo = tcl.lang.WrappedCommand;
  using Tcl_Interp = tcl.lang.Interp;
  using Tcl_Obj = tcl.lang.TclObject;
  using ClientData = System.Object;
  using System;
  using System.Text;
#endif

  public partial class Sqlite3
  {
    /*
    ** 2009 November 10
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
    ** This file implements a read-only VIRTUAL TABLE that contains the
    ** content of a C-language array of integer values.  See the corresponding
    ** header file for full details.
    *************************************************************************
    ** Included in SQLite3 port to C#-SQLite; 2008 Noah B Hart
    ** C#-SQLite is an independent reimplementation of the SQLite software library
    **
    ** SQLITE_SOURCE_ID: 2011-06-23 19:49:22 4374b7e83ea0a3fbc3691f9c0c936272862f32f2
    **
    *************************************************************************
    */
    //#include "test_intarray.h"
    //#include <string.h>
    //#include <Debug.Assert.h>


    /*
    ** Definition of the sqlite3_intarray object.
    **
    ** The internal representation of an intarray object is subject
    ** to change, is not externally visible, and should be used by
    ** the implementation of intarray only.  This object is opaque
    ** to users.
    */
    class sqlite3_intarray : sqlite3_vtab
    {
      public int n;                    /* Number of elements in the array */
      public sqlite3_int64[] a;        /* Contents of the array */
      public dxFree xFree;//void (*xFree)(void*);     /* Function used to free a[] */
    };

    /* Objects used internally by the virtual table implementation */
    //typedef struct intarray_vtab intarray_vtab;
    //typedef struct intarray_cursor intarray_cursor;

    /* A intarray table object */
    class intarray_vtab : sqlite3_vtab
    {
      //sqlite3_vtab base;            /* Base class */
      public sqlite3_intarray pContent;   /* Content of the integer array */
    };

    /* A intarray cursor object */
    class intarray_cursor : sqlite3_vtab_cursor
    {
      //sqlite3_vtab_cursor base;    /* Base class */
      public int i;                       /* Current cursor position */
    };

    /*
    ** None of this works unless we have virtual tables.
    */
#if !SQLITE_OMIT_VIRTUALTABLE

    /*
** Free an sqlite3_intarray object.
*/
    static int intarrayFree( ref object p )
    {
      //if ( ( (sqlite3_intarray)p ).xFree != null )
      //{
      //  p.xFree( ref p.a );
      //}
      p = null;//sqlite3_free(p);
      return 0;
    }

    /*
    ** Table destructor for the intarray module.
    */
    static int intarrayDestroy( ref object p )
    {
      //intarray_vtab *pVtab = (intarray_vtab*)p;
      //sqlite3_free(pVtab);
      p = null;
      return 0;
    }

    /*
    ** Table constructor for the intarray module.
    */
    static int intarrayCreate(
      sqlite3 db,               /* Database where module is created */
      object pAux,              /* clientdata for the module */
      int argc,                 /* Number of arguments */
      string[] argv,   /* Value for all arguments */
      out sqlite3_vtab ppVtab,  /* Write the new virtual table object here */
      out string pzErr          /* Put error message text here */
    )
    {
      int rc = SQLITE_NOMEM;
      intarray_vtab pVtab = new intarray_vtab();//sqlite3_malloc(sizeof(intarray_vtab));

      if ( pVtab != null )
      {
        //memset(pVtab, 0, sizeof(intarray_vtab));
        pVtab.pContent = (sqlite3_intarray)pAux;
        rc = sqlite3_declare_vtab( db, "CREATE TABLE x(value INTEGER PRIMARY KEY)" );
      }
      ppVtab = (sqlite3_vtab)pVtab;
      pzErr = "";
      return rc;
    }

    /*
    ** Open a new cursor on the intarray table.
    */
    static int intarrayOpen( sqlite3_vtab pVTab, out sqlite3_vtab_cursor ppCursor )
    {
      int rc = SQLITE_NOMEM;
      intarray_cursor pCur = new intarray_cursor();//
      //pCur = sqlite3_malloc(sizeof(intarray_cursor));
      //if ( pCur != null )
      {
        //memset(pCur, 0, sizeof(intarray_cursor));
        ppCursor = (sqlite3_vtab_cursor)pCur;
        rc = SQLITE_OK;
      }
      return rc;
    }

    /*
    ** Close a intarray table cursor.
    */
    static int intarrayClose( ref sqlite3_vtab_cursor cur )
    {
      //intarray_cursor *pCur = (intarray_cursor *)cur;
      //sqlite3_free(pCur);
      cur = null;
      return SQLITE_OK;
    }

    /*
    ** Retrieve a column of data.
    */
    static int intarrayColumn( sqlite3_vtab_cursor cur, sqlite3_context ctx, int i )
    {
      intarray_cursor pCur = (intarray_cursor)cur;
      intarray_vtab pVtab = (intarray_vtab)cur.pVtab;
      if ( pCur.i >= 0 && pCur.i < pVtab.pContent.n )
      {
        sqlite3_result_int64( ctx, pVtab.pContent.a[pCur.i] );
      }
      return SQLITE_OK;
    }

    /*
    ** Retrieve the current rowid.
    */
    static int intarrayRowid( sqlite3_vtab_cursor cur, out sqlite_int64 pRowid )
    {
      intarray_cursor pCur = (intarray_cursor)cur;
      pRowid = pCur.i;
      return SQLITE_OK;
    }

    static int intarrayEof( sqlite3_vtab_cursor cur )
    {
      intarray_cursor pCur = (intarray_cursor)cur;
      intarray_vtab pVtab = (intarray_vtab)cur.pVtab;
      return pCur.i >= pVtab.pContent.n ? 1 : 0;
    }

    /*
    ** Advance the cursor to the next row.
    */
    static int intarrayNext( sqlite3_vtab_cursor cur )
    {
      intarray_cursor pCur = (intarray_cursor)cur;
      pCur.i++;
      return SQLITE_OK;
    }

    /*
    ** Reset a intarray table cursor.
    */
    static int intarrayFilter(
      sqlite3_vtab_cursor pVtabCursor,
      int idxNum, string idxStr,
      int argc, sqlite3_value[] argv
    )
    {
      intarray_cursor pCur = (intarray_cursor)pVtabCursor;
      pCur.i = 0;
      return SQLITE_OK;
    }

    /*
    ** Analyse the WHERE condition.
    */
    static int intarrayBestIndex( sqlite3_vtab tab, ref sqlite3_index_info pIdxInfo )
    {
      return SQLITE_OK;
    }

    /*
    ** A virtual table module that merely echos method calls into TCL
    ** variables.
    */
    static sqlite3_module intarrayModule = new sqlite3_module(
  0,                           /* iVersion */
  intarrayCreate,              /* xCreate - create a new virtual table */
  intarrayCreate,              /* xConnect - connect to an existing vtab */
  intarrayBestIndex,           /* xBestIndex - find the best query index */
  intarrayDestroy,             /* xDisconnect - disconnect a vtab */
  intarrayDestroy,             /* xDestroy - destroy a vtab */
  intarrayOpen,                /* xOpen - open a cursor */
  intarrayClose,               /* xClose - close a cursor */
  intarrayFilter,              /* xFilter - configure scan constraints */
  intarrayNext,                /* xNext - advance a cursor */
  intarrayEof,                 /* xEof */
  intarrayColumn,              /* xColumn - read data */
  intarrayRowid,               /* xRowid - read data */
  null,                        /* xUpdate */
  null,                        /* xBegin */
  null,                        /* xSync */
  null,                        /* xCommit */
  null,                        /* xRollback */
  null,                        /* xFindMethod */
  null                         /* xRename */
);

#endif //* !defined(SQLITE_OMIT_VIRTUALTABLE) */

    /*
** Invoke this routine to create a specific instance of an intarray object.
** The new intarray object is returned by the 3rd parameter.
**
** Each intarray object corresponds to a virtual table in the TEMP table
** with a name of zName.
**
** Destroy the intarray object by dropping the virtual table.  If not done
** explicitly by the application, the virtual table will be dropped implicitly
** by the system when the database connection is closed.
*/
    static int sqlite3_intarray_create(
      sqlite3 db,
      string zName,
      out sqlite3_intarray ppReturn
    )
    {
      int rc = SQLITE_OK;
#if !SQLITE_OMIT_VIRTUALTABLE
      sqlite3_intarray p;

      ppReturn = p = new sqlite3_intarray();//sqlite3_malloc( sizeof(*p) );
      //if( p==0 ){
      //  return SQLITE_NOMEM;
      //}
      //memset(p, 0, sizeof(*p));
      rc = sqlite3_create_module_v2( db, zName, intarrayModule, p,
                                    intarrayFree );
      if ( rc == SQLITE_OK )
      {
        string zSql;
        zSql = sqlite3_mprintf( "CREATE VIRTUAL TABLE temp.%Q USING %Q",
                               zName, zName );
        rc = sqlite3_exec( db, zSql, 0, 0, 0 );
        //sqlite3_free(zSql);
      }
#endif
      return rc;
    }

    /*
    ** Bind a new array array of integers to a specific intarray object.
    **
    ** The array of integers bound must be unchanged for the duration of
    ** any query against the corresponding virtual table.  If the integer
    ** array does change or is deallocated undefined behavior will result.
    */
    static int sqlite3_intarray_bind(
      sqlite3_intarray pIntArray,    /* The intarray object to bind to */
      int nElements,                 /* Number of elements in the intarray */
      sqlite3_int64[] aElements,     /* Content of the intarray */
      dxFree xFree//void (*xFree)(void*)           /* How to dispose of the intarray when done */
    )
    {
      if ( pIntArray.xFree != null )
      {
        pIntArray.a = null;//pIntArray.xFree( pIntArray.a );
      }
      pIntArray.n = nElements;
      pIntArray.a = aElements;
      pIntArray.xFree = xFree;
      return SQLITE_OK;
    }


    /*****************************************************************************
    ** Everything below is interface for testing this module.
    */
#if SQLITE_TEST
    //#include <tcl.h>

    /*
    ** Routines to encode and decode pointers
    */
    //extern int getDbPointer(Tcl_Interp *interp, const char *zA, sqlite3 **ppDb);
    //extern void *sqlite3TestTextToPtr(const char*);
    //extern int sqlite3TestMakePointerStr(Tcl_Interp*, char *zPtr, void*);
    //extern const char *sqlite3TestErrorName(int);

    /*
    **    sqlite3_intarray_create  DB  NAME
    **
    ** Invoke the sqlite3_intarray_create interface.  A string that becomes
    ** the first parameter to sqlite3_intarray_bind.
    */
    static int test_intarray_create(
      ClientData clientData, /* Not used */
      Tcl_Interp interp,     /* The TCL interpreter that invoked this command */
      int objc,              /* Number of arguments */
      Tcl_Obj[] objv         /* Command arguments */
    )
    {
      sqlite3 db;
      string zName;
      sqlite3_intarray pArray;
      int rc = SQLITE_OK;
      StringBuilder zPtr = new StringBuilder( 100 );

      if ( objc != 3 )
      {
        TCL.Tcl_WrongNumArgs( interp, 1, objv, "DB" );
        return TCL.TCL_ERROR;
      }
      if ( getDbPointer( interp, TCL.Tcl_GetString( objv[1] ), out db ) != 0 )
        return TCL.TCL_ERROR;
      zName = TCL.Tcl_GetString( objv[2] );
#if !SQLITE_OMIT_VIRTUALTABLE
      rc = sqlite3_intarray_create( db, zName, out pArray );
#endif
      if ( rc != SQLITE_OK )
      {
        Debug.Assert( pArray == null );
        TCL.Tcl_AppendResult( interp, sqlite3TestErrorName( rc ), null );
        return TCL.TCL_ERROR;
      }
      sqlite3TestMakePointerStr( interp, zPtr, pArray );
      TCL.Tcl_AppendResult( interp, zPtr, null );
      return TCL.TCL_OK;
    }

    /*
    **    sqlite3_intarray_bind  INTARRAY  ?VALUE ...?
    **
    ** Invoke the sqlite3_intarray_bind interface on the given array of integers.
    */
    static int test_intarray_bind(
      ClientData clientData, /* Not used */
      Tcl_Interp interp,     /* The TCL interpreter that invoked this command */
      int objc,              /* Number of arguments */
      Tcl_Obj[] objv         /* Command arguments */
    )
    {
      sqlite3_intarray pArray;
      int rc = SQLITE_OK;
      int i, n;
      sqlite3_int64[] a;

      if ( objc < 2 )
      {
        TCL.Tcl_WrongNumArgs( interp, 1, objv, "INTARRAY" );
        return TCL.TCL_ERROR;
      }
      pArray = (sqlite3_intarray)sqlite3TestTextToPtr( interp, TCL.Tcl_GetString( objv[1] ) );
      n = objc - 2;
#if !SQLITE_OMIT_VIRTUALTABLE
      a = new sqlite3_int64[n];//sqlite3_malloc( sizeof(a[0])*n );
      //if( a==0 ){
      //  Tcl_AppendResult(interp, "SQLITE_NOMEM", (char*)0);
      //  return TCL_ERROR;
      //}
      for ( i = 0; i < n; i++ )
      {
        //a[i] = 0;
        TCL.Tcl_GetWideIntFromObj( null, objv[i + 2], out a[i] );
      }
      rc = sqlite3_intarray_bind( pArray, n, a, sqlite3_free );
      if ( rc != SQLITE_OK )
      {
        TCL.Tcl_AppendResult( interp, sqlite3TestErrorName( rc ), null );
        return TCL.TCL_ERROR;
      }
#endif
      return TCL.TCL_OK;
    }

    /*
    ** Register commands with the TCL interpreter.
    */
    static public int Sqlitetestintarray_Init( Tcl_Interp interp )
    {
      //static struct {
      //   char *zName;
      //   Tcl_ObjCmdProc *xProc;
      //   void *clientData;
      //} 
      _aObjCmd[] aObjCmd = new _aObjCmd[] {
     new _aObjCmd( "sqlite3_intarray_create", test_intarray_create, 0 ),
     new _aObjCmd(  "sqlite3_intarray_bind", test_intarray_bind, 0 ),
  };
      int i;
      for ( i = 0; i < aObjCmd.Length; i++ )//sizeof(aObjCmd)/sizeof(aObjCmd[0]); i++)
      {
        TCL.Tcl_CreateObjCommand( interp, aObjCmd[i].zName,
            aObjCmd[i].xProc, aObjCmd[i].clientData, null );
      }
      return TCL.TCL_OK;
    }

#endif //* SQLITE_TEST */
  }
}
