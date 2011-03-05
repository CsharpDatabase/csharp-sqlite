using System.Diagnostics;

namespace Community.CsharpSqlite
{
#if TCLSH
  using tcl.lang;
  using Tcl_Interp = tcl.lang.Interp;
#endif

  public partial class Sqlite3
  {
    /*
    ** 2007 May 7
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
    ** This file contains code used for testing the SQLite system.
    ** None of the code in this file goes into a deliverable build.
    **
    ** The focus of this file is providing the TCL testing layer
    ** access to compile-time constants.
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  SQLITE_SOURCE_ID: 2010-12-07 20:14:09 a586a4deeb25330037a49df295b36aaf624d0f45
    **
    **  $Header$
    *************************************************************************
    */

#if TCLSH
    //#include "sqliteLimit.h"

    //#include "sqliteInt.h"
    //#include "tcl.h"
    //#include <stdlib.h>
    //#include <string.h>

    /*** Macro to stringify the results of the evaluation a pre-processor
    ** macro. i.e. so that STRINGVALUE(SQLITE_NOMEM) -> "7".
    */
    //#define STRINGVALUE2(x) #x
    //#define STRINGVALUE(x) STRINGVALUE2(x)
    static string STRINGVALUE( int x ) { return x.ToString(); }

    /*
    ** This routine sets entries in the global ::sqlite_options() array variable
    ** according to the compile-time configuration of the database.  Test
    ** procedures use this to determine when tests should be omitted.
    */
    static void set_options( Tcl_Interp interp )
    {

      TCL.Tcl_SetVar2( interp, "sqlite_options", "malloc", "0", TCL.TCL_GLOBAL_ONLY );

#if SQLITE_32BIT_ROWID
TCL.Tcl_SetVar2( interp, "sqlite_options", "rowid32", "1", TCL.TCL_GLOBAL_ONLY );
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "rowid32", "0", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_CASE_SENSITIVE_LIKE
TCL.Tcl_SetVar2(interp, "sqlite_options","casesensitivelike","1",TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "casesensitivelike", "0", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_DEBUG
      TCL.Tcl_SetVar2( interp, "sqlite_options", "debug", "1", TCL.TCL_GLOBAL_ONLY );
#else
TCL.Tcl_SetVar2( interp, "sqlite_options", "debug", "0", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_DISABLE_DIRSYNC
TCL.Tcl_SetVar2(interp, "sqlite_options", "dirsync", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "dirsync", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_DISABLE_LFS
      TCL.Tcl_SetVar2( interp, "sqlite_options", "lfs", "0", TCL.TCL_GLOBAL_ONLY );
#else
TCL.Tcl_SetVar2( interp, "sqlite_options", "lfs", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if FALSE // /* def SQLITE_MEMDEBUG */
TCL.Tcl_SetVar2( interp, "sqlite_options", "memdebug", "1", TCL.TCL_GLOBAL_ONLY );
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "memdebug", "0", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_ENABLE_MEMSYS3
TCL.Tcl_SetVar2(interp, "sqlite_options", "mem3", "1", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "mem3", "0", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_ENABLE_MEMSYS5
TCL.Tcl_SetVar2(interp, "sqlite_options", "mem5", "1", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "mem5", "0", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_MUTEX_OMIT
      TCL.Tcl_SetVar2( interp, "sqlite_options", "mutex", "0", TCL.TCL_GLOBAL_ONLY );
#else
TCL.Tcl_SetVar2( interp, "sqlite_options", "mutex", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_ALTERTABLE
TCL.Tcl_SetVar2(interp, "sqlite_options", "altertable", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "altertable", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_ANALYZE
TCL.Tcl_SetVar2(interp, "sqlite_options", "analyze", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "analyze", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_ENABLE_ATOMIC_WRITE
TCL.Tcl_SetVar2(interp, "sqlite_options", "atomicwrite", "1", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "atomicwrite", "0", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_ATTACH
TCL.Tcl_SetVar2(interp, "sqlite_options", "attach", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "attach", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_AUTHORIZATION
      TCL.Tcl_SetVar2( interp, "sqlite_options", "auth", "0", TCL.TCL_GLOBAL_ONLY );
#else
TCL.Tcl_SetVar2(interp, "sqlite_options", "auth", "1", TCL.TCL_GLOBAL_ONLY);
#endif

#if SQLITE_OMIT_AUTOINCREMENT
TCL.Tcl_SetVar2(interp, "sqlite_options", "autoinc", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "autoinc", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_AUTOMATIC_INDEX
TCL.Tcl_SetVar2(interp, "sqlite_options", "autoindex", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "autoindex", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_AUTORESET
Tcl_SetVar2(interp, "sqlite_options", "autoreset", "0", TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "autoreset", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_AUTOVACUUM
TCL.Tcl_SetVar2( interp, "sqlite_options", "autovacuum", "0", TCL.TCL_GLOBAL_ONLY );
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "autovacuum", "1", TCL.TCL_GLOBAL_ONLY );
#endif // * SQLITE_OMIT_AUTOVACUUM */
#if  !SQLITE_DEFAULT_AUTOVACUUM
      TCL.Tcl_SetVar2( interp, "sqlite_options", "default_autovacuum", "0", TCL.TCL_GLOBAL_ONLY );
#else
TCL.Tcl_SetVar2(interp,"sqlite_options","default_autovacuum",
STRINGVALUE(SQLITE_DEFAULT_AUTOVACUUM), TCL.TCL_GLOBAL_ONLY);
#endif

#if SQLITE_OMIT_BETWEEN_OPTIMIZATION
TCL.Tcl_SetVar2(interp, "sqlite_options", "between_opt", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "between_opt", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_BUILTIN_TEST
TCL.Tcl_SetVar2(interp, "sqlite_options", "builtin_test", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "builtin_test", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_BLOB_LITERAL
TCL.Tcl_SetVar2(interp, "sqlite_options", "bloblit", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "bloblit", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_CAST
TCL.Tcl_SetVar2(interp, "sqlite_options", "cast", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "cast", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_CHECK
TCL.Tcl_SetVar2(interp, "sqlite_options", "check", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "check", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_ENABLE_COLUMN_METADATA
TCL.Tcl_SetVar2(interp, "sqlite_options", "columnmetadata", "1", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "columnmetadata", "0", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_ENABLE_OVERSIZE_CELL_CHECK
      TCL.Tcl_SetVar2( interp, "sqlite_options", "oversize_cell_check", "1",
      TCL.TCL_GLOBAL_ONLY );
#else
TCL.Tcl_SetVar2(interp, "sqlite_options", "oversize_cell_check", "0",
TCL.TCL_GLOBAL_ONLY);
#endif

#if SQLITE_OMIT_COMPILEOPTION_DIAGS
TCL.Tcl_SetVar2(interp, "sqlite_options", "compileoption_diags", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "compileoption_diags", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_COMPLETE
TCL.Tcl_SetVar2(interp, "sqlite_options", "complete", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "complete", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_COMPOUND_SELECT
TCL.Tcl_SetVar2(interp, "sqlite_options", "compound", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "compound", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_CONFLICT_CLAUSE
TCL.Tcl_SetVar2(interp, "sqlite_options", "conflict", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "conflict", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if  SQLITE_OS_UNIX
TCL.Tcl_SetVar2(interp, "sqlite_options", "crashtest", "1", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "crashtest", "0", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_DATETIME_FUNCS
TCL.Tcl_SetVar2(interp, "sqlite_options", "datetime", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "datetime", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_DECLTYPE
TCL.Tcl_SetVar2(interp, "sqlite_options", "decltype", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "decltype", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_DEPRECATED
      TCL.Tcl_SetVar2( interp, "sqlite_options", "deprecated", "0", TCL.TCL_GLOBAL_ONLY );
#else
TCL.Tcl_SetVar2( interp, "sqlite_options", "deprecated", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_DISKIO
TCL.Tcl_SetVar2(interp, "sqlite_options", "diskio", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "diskio", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_EXPLAIN
TCL.Tcl_SetVar2(interp, "sqlite_options", "explain", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "explain", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_FLOATING_POINT
TCL.Tcl_SetVar2(interp, "sqlite_options", "floatingpoint", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "floatingpoint", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_FOREIGN_KEY
TCL.Tcl_SetVar2(interp, "sqlite_options", "foreignkey", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "foreignkey", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_ENABLE_FTS1
TCL.Tcl_SetVar2(interp, "sqlite_options", "fts1", "1", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "fts1", "0", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_ENABLE_FTS2
TCL.Tcl_SetVar2(interp, "sqlite_options", "fts2", "1", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "fts2", "0", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_ENABLE_FTS3
TCL.Tcl_SetVar2(interp, "sqlite_options", "fts3", "1", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "fts3", "0", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_GET_TABLE
      TCL.Tcl_SetVar2( interp, "sqlite_options", "gettable", "0", TCL.TCL_GLOBAL_ONLY );
#else
TCL.Tcl_SetVar2(interp, "sqlite_options", "gettable", "1", TCL.TCL_GLOBAL_ONLY);
#endif

#if SQLITE_ENABLE_ICU
TCL.Tcl_SetVar2(interp, "sqlite_options", "icu", "1", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "icu", "0", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_INCRBLOB
      TCL.Tcl_SetVar2( interp, "sqlite_options", "incrblob", "0", TCL.TCL_GLOBAL_ONLY );
#else
TCL.Tcl_SetVar2( interp, "sqlite_options", "incrblob", "1", TCL.TCL_GLOBAL_ONLY );
#endif // * SQLITE_OMIT_AUTOVACUUM */

#if SQLITE_OMIT_INTEGRITY_CHECK
TCL.Tcl_SetVar2(interp, "sqlite_options", "integrityck", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "integrityck", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if  !SQLITE_DEFAULT_FILE_FORMAT //SQLITE_DEFAULT_FILE_FORMAT && SQLITE_DEFAULT_FILE_FORMAT==1
      TCL.Tcl_SetVar2( interp, "sqlite_options", "legacyformat", "1", TCL.TCL_GLOBAL_ONLY );
#else
TCL.Tcl_SetVar2( interp, "sqlite_options", "legacyformat", "0", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_LIKE_OPTIMIZATION
TCL.Tcl_SetVar2(interp, "sqlite_options", "like_opt", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "like_opt", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_LOAD_EXTENSION
TCL.Tcl_SetVar2( interp, "sqlite_options", "load_ext", "0", TCL.TCL_GLOBAL_ONLY );
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "load_ext", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_LOCALTIME
TCL.Tcl_SetVar2(interp, "sqlite_options", "localtime", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "localtime", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_LOOKASIDE
      TCL.Tcl_SetVar2( interp, "sqlite_options", "lookaside", "0", TCL.TCL_GLOBAL_ONLY );
#else
TCL.Tcl_SetVar2(interp, "sqlite_options", "lookaside", "1", TCL.TCL_GLOBAL_ONLY);
#endif

      TCL.Tcl_SetVar2( interp, "sqlite_options", "long_double", "0", TCL.TCL_GLOBAL_ONLY );
      //sizeof(LONGDOUBLE_TYPE)>sizeof(double) ? "1" : "0",
      //TCL.TCL_GLOBAL_ONLY);

#if SQLITE_OMIT_MEMORYDB
TCL.Tcl_SetVar2(interp, "sqlite_options", "memorydb", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "memorydb", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_ENABLE_MEMORY_MANAGEMENT
TCL.Tcl_SetVar2(interp, "sqlite_options", "memorymanage", "1", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "memorymanage", "0", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_OR_OPTIMIZATION
TCL.Tcl_SetVar2(interp, "sqlite_options", "or_opt", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "or_opt", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_PAGER_PRAGMAS
TCL.Tcl_SetVar2(interp, "sqlite_options", "pager_pragmas", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "pager_pragmas", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if  SQLITE_OMIT_PRAGMA || SQLITE_OMIT_FLAG_PRAGMAS
TCL.Tcl_SetVar2(interp, "sqlite_options", "pragma", "0", TCL.TCL_GLOBAL_ONLY);
TCL.Tcl_SetVar2(interp, "sqlite_options", "integrityck", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "pragma", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_PROGRESS_CALLBACK
TCL.Tcl_SetVar2(interp, "sqlite_options", "progress", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "progress", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_REINDEX
TCL.Tcl_SetVar2(interp, "sqlite_options", "reindex", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "reindex", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_ENABLE_RTREE
TCL.Tcl_SetVar2(interp, "sqlite_options", "rtree", "1", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "rtree", "0", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_SCHEMA_PRAGMAS
TCL.Tcl_SetVar2(interp, "sqlite_options", "schema_pragmas", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "schema_pragmas", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_SCHEMA_VERSION_PRAGMAS
TCL.Tcl_SetVar2(interp, "sqlite_options", "schema_version", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "schema_version", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_ENABLE_STAT2
      TCL.Tcl_SetVar2( interp, "sqlite_options", "stat2", "1", TCL.TCL_GLOBAL_ONLY );
#else
TCL.Tcl_SetVar2( interp, "sqlite_options", "stat2", "0", TCL.TCL_GLOBAL_ONLY );
#endif

#if !(SQLITE_ENABLE_LOCKING_STYLE)
#  if (__APPLE__)
//#    define SQLITE_ENABLE_LOCKING_STYLE 1
#  else
      //#    define SQLITE_ENABLE_LOCKING_STYLE 0
#  endif
#endif
#if SQLITE_ENABLE_LOCKING_STYLE && (__APPLE__)
TCL.Tcl_SetVar2(interp,"sqlite_options","lock_proxy_pragmas","1",TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "lock_proxy_pragmas", "0", TCL.TCL_GLOBAL_ONLY );
#endif
#if (SQLITE_PREFER_PROXY_LOCKING) && (__APPLE__)
TCL.Tcl_SetVar2(interp,"sqlite_options","prefer_proxy_locking","1",TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "prefer_proxy_locking", "0", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_SHARED_CACHE
      TCL.Tcl_SetVar2( interp, "sqlite_options", "shared_cache", "0", TCL.TCL_GLOBAL_ONLY );
#else
TCL.Tcl_SetVar2( interp, "sqlite_options", "shared_cache", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_SUBQUERY
TCL.Tcl_SetVar2(interp, "sqlite_options", "subquery", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "subquery", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_TCL_VARIABLE
TCL.Tcl_SetVar2(interp, "sqlite_options", "tclvar", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "tclvar", "1", TCL.TCL_GLOBAL_ONLY );
#endif

      TCL.Tcl_SetVar2( interp, "sqlite_options", "threadsafe",
      STRINGVALUE( SQLITE_THREADSAFE ), TCL.TCL_GLOBAL_ONLY );
      Debug.Assert( sqlite3_threadsafe() == SQLITE_THREADSAFE );

#if SQLITE_OMIT_TEMPDB
TCL.Tcl_SetVar2(interp, "sqlite_options", "tempdb", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "tempdb", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_TRACE
TCL.Tcl_SetVar2(interp, "sqlite_options", "trace", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "trace", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_TRIGGER
TCL.Tcl_SetVar2(interp, "sqlite_options", "trigger", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "trigger", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_TRUNCATE_OPTIMIZATION
TCL.Tcl_SetVar2(interp, "sqlite_options", "truncate_opt", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "truncate_opt", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_UTF16
      TCL.Tcl_SetVar2( interp, "sqlite_options", "utf16", "0", TCL.TCL_GLOBAL_ONLY );
#else
TCL.Tcl_SetVar2(interp, "sqlite_options", "utf16", "1", TCL.TCL_GLOBAL_ONLY);
#endif

#if  SQLITE_OMIT_VACUUM || SQLITE_OMIT_ATTACH
TCL.Tcl_SetVar2(interp, "sqlite_options", "vacuum", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "vacuum", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_VIEW
TCL.Tcl_SetVar2(interp, "sqlite_options", "view", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "view", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_OMIT_VIRTUALTABLE
      TCL.Tcl_SetVar2( interp, "sqlite_options", "vtab", "0", TCL.TCL_GLOBAL_ONLY );
#else
TCL.Tcl_SetVar2(interp, "sqlite_options", "vtab", "1", TCL.TCL_GLOBAL_ONLY);
#endif

#if SQLITE_OMIT_WAL
      TCL.Tcl_SetVar2( interp, "sqlite_options", "wal", "0", TCL.TCL_GLOBAL_ONLY );
#else
TCL.Tcl_SetVar2(interp, "sqlite_options", "wal", "1", TCL.TCL_GLOBAL_ONLY);
#endif

#if SQLITE_OMIT_WSD
TCL.Tcl_SetVar2(interp, "sqlite_options", "wsd", "0", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "wsd", "1", TCL.TCL_GLOBAL_ONLY );
#endif

#if (SQLITE_ENABLE_UPDATE_DELETE_LIMIT) && !(SQLITE_OMIT_SUBQUERY)
TCL.Tcl_SetVar2( interp, "sqlite_options", "update_delete_limit", "1", TCL.TCL_GLOBAL_ONLY );
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "update_delete_limit", "0", TCL.TCL_GLOBAL_ONLY );
#endif

#if (SQLITE_ENABLE_UNLOCK_NOTIFY)
TCL.Tcl_SetVar2(interp, "sqlite_options", "unlock_notify", "1", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "unlock_notify", "0", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_SECURE_DELETE
TCL.Tcl_SetVar2(interp, "sqlite_options", "secure_delete", "1", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "secure_delete", "0", TCL.TCL_GLOBAL_ONLY );
#endif

#if SQLITE_MULTIPLEX_EXT_OVWR
TCL.Tcl_SetVar2(interp, "sqlite_options", "multiplex_ext_overwrite", "1", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "multiplex_ext_overwrite", "0", TCL.TCL_GLOBAL_ONLY );
#endif

#if YYTRACKMAXSTACKDEPTH
TCL.Tcl_SetVar2(interp, "sqlite_options", "yytrackmaxstackdepth", "1", TCL.TCL_GLOBAL_ONLY);
#else
      TCL.Tcl_SetVar2( interp, "sqlite_options", "yytrackmaxstackdepth", "0", TCL.TCL_GLOBAL_ONLY );
#endif

      //#define LINKVAR(x) { \
      //    const int cv_ ## x = SQLITE_ ## x; \
      //    TCL.Tcl_LinkVar(interp, "SQLITE_" #x, (char *)&(cv_ ## x), \
      //                TCL.Tcl_LINK_INT | TCL.Tcl_LINK_READ_ONLY); }


      //LINKVAR( MAX_LENGTH );
      //LINKVAR( MAX_COLUMN );
      //LINKVAR( MAX_SQL_LENGTH );
      //LINKVAR( MAX_EXPR_DEPTH );
      //LINKVAR( MAX_COMPOUND_SELECT );
      //LINKVAR( MAX_VDBE_OP );
      //LINKVAR( MAX_FUNCTION_ARG );
      //LINKVAR( MAX_VARIABLE_NUMBER );
      //LINKVAR( MAX_PAGE_SIZE );
      //LINKVAR( MAX_PAGE_COUNT );
      //LINKVAR( MAX_LIKE_PATTERN_LENGTH );
      //LINKVAR( MAX_TRIGGER_DEPTH );
      //LINKVAR( DEFAULT_TEMP_CACHE_SIZE );
      //LINKVAR( DEFAULT_CACHE_SIZE );
      //LINKVAR( DEFAULT_PAGE_SIZE );
      //LINKVAR( DEFAULT_FILE_FORMAT );
      //LINKVAR( MAX_ATTACHED );

      TCL.Tcl_LinkVar( interp, "SQLITE_MAX_LENGTH", SQLITE_MAX_LENGTH, VarFlags.SQLITE3_LINK_INT | VarFlags.SQLITE3_LINK_READ_ONLY );
      TCL.Tcl_LinkVar( interp, "SQLITE_MAX_COLUMN", SQLITE_MAX_COLUMN, VarFlags.SQLITE3_LINK_INT | VarFlags.SQLITE3_LINK_READ_ONLY );
      TCL.Tcl_LinkVar( interp, "SQLITE_MAX_SQL_LENGTH", SQLITE_MAX_SQL_LENGTH, VarFlags.SQLITE3_LINK_INT | VarFlags.SQLITE3_LINK_READ_ONLY );
      TCL.Tcl_LinkVar( interp, "SQLITE_MAX_EXPR_DEPTH", SQLITE_MAX_EXPR_DEPTH, VarFlags.SQLITE3_LINK_INT | VarFlags.SQLITE3_LINK_READ_ONLY );
      TCL.Tcl_LinkVar( interp, "SQLITE_MAX_COMPOUND_SELECT", SQLITE_MAX_COMPOUND_SELECT, VarFlags.SQLITE3_LINK_INT | VarFlags.SQLITE3_LINK_READ_ONLY );
      TCL.Tcl_LinkVar( interp, "SQLITE_MAX_VDBE_OP", SQLITE_MAX_VDBE_OP, VarFlags.SQLITE3_LINK_INT | VarFlags.SQLITE3_LINK_READ_ONLY );
      TCL.Tcl_LinkVar( interp, "SQLITE_MAX_FUNCTION_ARG", SQLITE_MAX_FUNCTION_ARG, VarFlags.SQLITE3_LINK_INT | VarFlags.SQLITE3_LINK_READ_ONLY );
      TCL.Tcl_LinkVar( interp, "SQLITE_MAX_VARIABLE_NUMBER", SQLITE_MAX_VARIABLE_NUMBER, VarFlags.SQLITE3_LINK_INT | VarFlags.SQLITE3_LINK_READ_ONLY );
      TCL.Tcl_LinkVar( interp, "SQLITE_MAX_PAGE_SIZE", SQLITE_MAX_PAGE_SIZE, VarFlags.SQLITE3_LINK_INT | VarFlags.SQLITE3_LINK_READ_ONLY );
      TCL.Tcl_LinkVar( interp, "SQLITE_MAX_PAGE_COUNT", SQLITE_MAX_PAGE_COUNT, VarFlags.SQLITE3_LINK_INT | VarFlags.SQLITE3_LINK_READ_ONLY );
      TCL.Tcl_LinkVar( interp, "SQLITE_MAX_LIKE_PATTERN_LENGTH", SQLITE_MAX_LIKE_PATTERN_LENGTH, VarFlags.SQLITE3_LINK_INT | VarFlags.SQLITE3_LINK_READ_ONLY );
      TCL.Tcl_LinkVar( interp, "SQLITE_MAX_TRIGGER_DEPTH", SQLITE_MAX_TRIGGER_DEPTH, VarFlags.SQLITE3_LINK_INT | VarFlags.SQLITE3_LINK_READ_ONLY );
      TCL.Tcl_LinkVar( interp, "SQLITE_DEFAULT_TEMP_CACHE_SIZE", SQLITE_DEFAULT_TEMP_CACHE_SIZE, VarFlags.SQLITE3_LINK_INT | VarFlags.SQLITE3_LINK_READ_ONLY );
      TCL.Tcl_LinkVar( interp, "SQLITE_DEFAULT_CACHE_SIZE", SQLITE_DEFAULT_CACHE_SIZE, VarFlags.SQLITE3_LINK_INT | VarFlags.SQLITE3_LINK_READ_ONLY );
      TCL.Tcl_LinkVar( interp, "SQLITE_DEFAULT_PAGE_SIZE", SQLITE_DEFAULT_PAGE_SIZE, VarFlags.SQLITE3_LINK_INT | VarFlags.SQLITE3_LINK_READ_ONLY );
      TCL.Tcl_LinkVar( interp, "SQLITE_DEFAULT_FILE_FORMAT", SQLITE_DEFAULT_FILE_FORMAT, VarFlags.SQLITE3_LINK_INT | VarFlags.SQLITE3_LINK_READ_ONLY );
      TCL.Tcl_LinkVar( interp, "SQLITE_MAX_ATTACHED", SQLITE_MAX_ATTACHED, VarFlags.SQLITE3_LINK_INT | VarFlags.SQLITE3_LINK_READ_ONLY );

      {
        int cv_TEMP_STORE = SQLITE_TEMP_STORE;
        TCL.Tcl_LinkVar( interp, "TEMP_STORE", cv_TEMP_STORE, VarFlags.SQLITE3_LINK_INT | VarFlags.SQLITE3_LINK_READ_ONLY );
      }
    }


    /*
    ** Register commands with the TCL interpreter.
    */
    public static int Sqliteconfig_Init( Tcl_Interp interp )
    {
      set_options( interp );
      return TCL.TCL_OK;
    }
#endif
  }
}
