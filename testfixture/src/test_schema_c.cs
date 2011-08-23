using System;
using System.Diagnostics;
using System.Text;

using i64 = System.Int64;
using u8 = System.Byte;

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
    ** 2006 June 10
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
    *************************************************************************
    ** Included in SQLite3 port to C#-SQLite; 2008 Noah B Hart
    ** C#-SQLite is an independent reimplementation of the SQLite software library
    **
    ** SQLITE_SOURCE_ID: 2011-06-23 19:49:22 4374b7e83ea0a3fbc3691f9c0c936272862f32f2
    **
    *************************************************************************
    */

    /* The code in this file defines a sqlite3 virtual-table module that
    ** provides a read-only view of the current database schema. There is one
    ** row in the schema table for each column in the database schema.
    */
    //#define SCHEMA \
    //"CREATE TABLE x("                                                            \
    //  "database,"          /* Name of database (i.e. main, temp etc.) */         \
    //  "tablename,"         /* Name of table */                                   \
    //  "cid,"               /* Column number (from left-to-right, 0 upward) */    \
    //  "name,"              /* Column name */                                     \
    //  "type,"              /* Specified type (i.e. VARCHAR(32)) */               \
    //  "not_null,"          /* Boolean. True if NOT NULL was specified */         \
    //  "dflt_value,"        /* Default value for this column */                   \
    //  "pk"                 /* True if this column is part of the primary key */  \
    //")"
    const string SCHEMA = "CREATE TABLE x(" +
      "database," +
      "tablename," +
      "cid," +
      "name," +
      "type," +
      "not_null," +
      "dflt_value," +
      "pk" +
    ")";

    /* If SQLITE_TEST is defined this code is preprocessed for use as part
** of the sqlite test binary "testfixture". Otherwise it is preprocessed
** to be compiled into an sqlite dynamic extension.
*/
    //#if SQLITE_TEST
    //  #include "sqliteInt.h"
    //  #include "tcl.h"
    //#else
    //  #include "sqlite3ext.h"
    //  SQLITE_EXTENSION_INIT1
    //#endif

    //#include <stdlib.h>
    //#include <string.h>
    //#include <Debug.Assert.h>

    //typedef struct schema_vtab schema_vtab;
    //typedef struct schema_cursor schema_cursor;

    /* A schema table object */
    class schema_vtab : sqlite3_vtab
    {
      //  sqlite3_vtab base;
      public sqlite3 db;
    };

    /* A schema table cursor object */
    class schema_cursor : sqlite3_vtab_cursor
    {
      //sqlite3_vtab_cursor base;
      public sqlite3_stmt pDbList;
      public sqlite3_stmt pTableList;
      public sqlite3_stmt pColumnList;
      public int rowid;
    };

    /*
    ** None of this works unless we have virtual tables.
    */
#if !SQLITE_OMIT_VIRTUALTABLE

    /*
** Table destructor for the schema module.
*/
    static int schemaDestroy( ref object pVtab )
    {
      pVtab = null;//sqlite3_free(pVtab);
      return 0;
    }

    /*
    ** Table constructor for the schema module.
    */
    static int schemaCreate(
      sqlite3 db,
      object pAux,
      int argc,
      string[] argv,
      out sqlite3_vtab ppVtab,
      out string pzErr
    )
    {
      int rc = SQLITE_NOMEM;
      schema_vtab pVtab = new schema_vtab();//sqlite3_malloc(sizeof(schema_vtab));
      if ( pVtab != null )
      {
        //memset(pVtab, 0, sizeof(schema_vtab));
        pVtab.db = db;
#if !SQLITE_OMIT_VIRTUALTABLE
        rc = sqlite3_declare_vtab( db, SCHEMA );
#endif
      }
      ppVtab = (sqlite3_vtab)pVtab;
      pzErr = "";
      return rc;
    }

    /*
    ** Open a new cursor on the schema table.
    */
    static int schemaOpen( sqlite3_vtab pVTab, out sqlite3_vtab_cursor ppCursor )
    {
      int rc = SQLITE_NOMEM;
      schema_cursor pCur;
      pCur = new schema_cursor();//pCur = sqlite3_malloc(sizeof(schema_cursor));
      //if ( pCur != null )
      //{
        //memset(pCur, 0, sizeof(schema_cursor));
        ppCursor = (sqlite3_vtab_cursor)pCur;
        rc = SQLITE_OK;
      //}
      return rc;
    }

    /*
    ** Close a schema table cursor.
    */
    static int schemaClose( ref sqlite3_vtab_cursor cur )
    {
      schema_cursor pCur = (schema_cursor)cur;
      sqlite3_finalize( pCur.pDbList );
      sqlite3_finalize( pCur.pTableList );
      sqlite3_finalize( pCur.pColumnList );
      //sqlite3_free( pCur );
      cur = null;//
      return SQLITE_OK;
    }

    /*
    ** Retrieve a column of data.
    */
    static int schemaColumn( sqlite3_vtab_cursor cur, sqlite3_context ctx, int i )
    {
      schema_cursor pCur = (schema_cursor)cur;
      switch ( i )
      {
        case 0:
          sqlite3_result_value( ctx, sqlite3_column_value( pCur.pDbList, 1 ) );
          break;
        case 1:
          sqlite3_result_value( ctx, sqlite3_column_value( pCur.pTableList, 0 ) );
          break;
        default:
          sqlite3_result_value( ctx, sqlite3_column_value( pCur.pColumnList, i - 2 ) );
          break;
      }
      return SQLITE_OK;
    }

    /*
    ** Retrieve the current rowid.
    */
    static int schemaRowid( sqlite3_vtab_cursor cur, out sqlite_int64 pRowid )
    {
      schema_cursor pCur = (schema_cursor)cur;
      pRowid = pCur.rowid;
      return SQLITE_OK;
    }

    static int finalize( ref sqlite3_stmt ppStmt )
    {
      int rc = sqlite3_finalize( ppStmt );
      ppStmt = null;
      return rc;
    }

    static int schemaEof( sqlite3_vtab_cursor cur )
    {
      schema_cursor pCur = (schema_cursor)cur;
      return ( pCur.pDbList != null ? 0 : 1 );
    }

    /*
    ** Advance the cursor to the next row.
    */
    static int schemaNext( sqlite3_vtab_cursor cur )
    {
      int rc = SQLITE_OK;
      schema_cursor pCur = (schema_cursor)cur;
      schema_vtab pVtab = (schema_vtab)( cur.pVtab );
      string zSql = null;

      while ( null == pCur.pColumnList || SQLITE_ROW != sqlite3_step( pCur.pColumnList ) )
      {
        if ( SQLITE_OK != ( rc = finalize( ref pCur.pColumnList ) ) )
          goto next_exit;

        while ( null == pCur.pTableList || SQLITE_ROW != sqlite3_step( pCur.pTableList ) )
        {
          if ( SQLITE_OK != ( rc = finalize( ref pCur.pTableList ) ) )
            goto next_exit;

          Debug.Assert( pCur.pDbList !=null);
          while ( SQLITE_ROW != sqlite3_step( pCur.pDbList ) )
          {
            rc = finalize( ref pCur.pDbList );
            goto next_exit;
          }

          /* Set zSql to the SQL to pull the list of tables from the 
          ** sqlite_master (or sqlite_temp_master) table of the database
          ** identfied by the row pointed to by the SQL statement pCur.pDbList
          ** (iterating through a "PRAGMA database_list;" statement).
          */
          if ( sqlite3_column_int( pCur.pDbList, 0 ) == 1 )
          {
            zSql = sqlite3_mprintf(
                "SELECT name FROM sqlite_temp_master WHERE type='table'"
            );
          }
          else
          {
            sqlite3_stmt pDbList = pCur.pDbList;
            zSql = sqlite3_mprintf(
                "SELECT name FROM %Q.sqlite_master WHERE type='table'",
                 sqlite3_column_text( pDbList, 1 )
            );
          }
          //if( !zSql ){
          //  rc = SQLITE_NOMEM;
          //  goto next_exit;
          //}
          rc = sqlite3_prepare( pVtab.db, zSql, -1, ref pCur.pTableList, 0 );
          //sqlite3_free(zSql);
          if ( rc != SQLITE_OK )
            goto next_exit;
        }

        /* Set zSql to the SQL to the table_info pragma for the table currently
        ** identified by the rows pointed to by statements pCur.pDbList and
        ** pCur.pTableList.
        */
        zSql = sqlite3_mprintf( "PRAGMA %Q.table_info(%Q)",
            sqlite3_column_text( pCur.pDbList, 1 ),
            sqlite3_column_text( pCur.pTableList, 0 )
        );

        //if( !Sql ){
        //  rc = SQLITE_NOMEM;
        //  goto next_exit;
        //}
        rc = sqlite3_prepare( pVtab.db, zSql, -1, ref pCur.pColumnList, 0 );
        //sqlite3_free(zSql);
        if ( rc != SQLITE_OK )
          goto next_exit;
      }
      pCur.rowid++;

next_exit:
      /* TODO: Handle rc */
      return rc;
    }

    /*
    ** Reset a schema table cursor.
    */
    static int schemaFilter(
      sqlite3_vtab_cursor pVtabCursor,
      int idxNum,
      string idxStr,
      int argc,
      sqlite3_value[] argv
    )
    {
      int rc;
      schema_vtab pVtab = (schema_vtab)( pVtabCursor.pVtab );
      schema_cursor pCur = (schema_cursor)pVtabCursor;
      pCur.rowid = 0;
      finalize( ref pCur.pTableList );
      finalize( ref pCur.pColumnList );
      finalize( ref pCur.pDbList );
      rc = sqlite3_prepare( pVtab.db, "PRAGMA database_list", -1, ref pCur.pDbList, 0 );
      return ( rc == SQLITE_OK ? schemaNext( pVtabCursor ) : rc );
    }

    /*
    ** Analyse the WHERE condition.
    */
    static int schemaBestIndex( sqlite3_vtab tab, ref sqlite3_index_info pIdxInfo )
    {
      return SQLITE_OK;
    }

    /*
    ** A virtual table module that merely echos method calls into TCL
    ** variables.
    */
    static sqlite3_module schemaModule = new sqlite3_module(
      0,                           /* iVersion */
      schemaCreate,
      schemaCreate,
      schemaBestIndex,
      schemaDestroy,
      schemaDestroy,
      schemaOpen,                  /* xOpen - open a cursor */
      schemaClose,                 /* xClose - close a cursor */
      schemaFilter,                /* xFilter - configure scan constraints */
      schemaNext,                  /* xNext - advance a cursor */
      schemaEof,                   /* xEof */
      schemaColumn,                /* xColumn - read data */
      schemaRowid,                 /* xRowid - read data */
      null,                           /* xUpdate */
      null,                           /* xBegin */
      null,                           /* xSync */
      null,                           /* xCommit */
      null,                           /* xRollback */
      null,                           /* xFindMethod */
      null                            /* xRename */
      );

#endif //* !defined(SQLITE_OMIT_VIRTUALTABLE) */

#if SQLITE_TEST

    /*
** Decode a pointer to an sqlite3 object.
*/
    //extern int getDbPointer(Tcl_Interp *interp, const char *zA, sqlite3 **ppDb);

    /*
    ** Register the schema virtual table module.
    */
    static int register_schema_module(
      ClientData clientData, /* Not used */
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
#if !SQLITE_OMIT_VIRTUALTABLE
      sqlite3_create_module( db, "schema", schemaModule, null );
#endif
      return TCL.TCL_OK;
    }

    /*
    ** Register commands with the TCL interpreter.
    */
    static public int Sqlitetestschema_Init( Tcl_Interp interp )
    {
      //static struct {
      //   char *zName;
      //   Tcl_ObjCmdProc *xProc;
      //   void *clientData;
      //} 
      _aObjCmd[] aObjCmd = new _aObjCmd[]{
     new _aObjCmd( "register_schema_module", register_schema_module, 0 ),
  };
      int i;
      for ( i = 0; i < aObjCmd.Length; i++ )//sizeof(aObjCmd)/sizeof(aObjCmd[0]); i++)
      {
        TCL.Tcl_CreateObjCommand( interp, aObjCmd[i].zName,
            aObjCmd[i].xProc, aObjCmd[i].clientData, null );
      }
      return TCL.TCL_OK;
    }

#else

/*
** Extension load function.
*/
int sqlite3_extension_init(
  sqlite3 *db, 
  char **pzErrMsg, 
  const sqlite3_api_routines *pApi
){
  SQLITE_EXTENSION_INIT2(pApi);
#if !SQLITE_OMIT_VIRTUALTABLE
  sqlite3_create_module(db, "schema", &schemaModule, 0);
#endif
  return 0;
}

#endif
  }
#endif
}
