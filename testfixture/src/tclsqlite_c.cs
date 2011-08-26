using System;
using System.Diagnostics;
using sqlite_int64 = System.Int64;
using sqlite_u3264 = System.UInt64;
using u32 = System.UInt32;

namespace Community.CsharpSqlite
{
#if TCLSH
  using tcl.lang;
  using ClientData = System.Object;

#if !SQLITE_OMIT_INCRBLOB
using sqlite3_blob = sqlite.Incrblob;
#endif
  using sqlite3_stmt = Sqlite3.Vdbe;
  using Tcl_DString = tcl.lang.TclString;
  using Tcl_Interp = tcl.lang.Interp;
  using Tcl_Obj = tcl.lang.TclObject;
  using Tcl_WideInt = System.Int64;

  using sqlite3_value = Sqlite3.Mem;
  using System.IO;
  using System.Text;

  public partial class Sqlite3
  {
    /*
    ** 2001 September 15
    **
    ** The author disclaims copyright to this source code.  In place of
    ** a legal notice, here is a blessing:
    **
    **    May you do good and not evil.
    **    May you find forgiveness for yourself and forgive others.
    **    May you share freely, never taking more than you give.
    **
    *************************************************************************
    ** A TCL Interface to SQLite.  Append this file to sqlite3.c and
    ** compile the whole thing to build a TCL-enabled version of SQLite.
    **
    ** Compile-time options:
    **
    **  -DTCLSH=1             Add a "main()" routine that works as a tclsh.
    **
    **  -DSQLITE_TCLMD5       When used in conjuction with -DTCLSH=1, add
    **                        four new commands to the TCL interpreter for
    **                        generating MD5 checksums:  md5, md5file,
    **                        md5-10x8, and md5file-10x8.
    **
    **  -DSQLITE_TEST         When used in conjuction with -DTCLSH=1, add
    **                        hundreds of new commands used for testing
    **                        SQLite.  This option implies -DSQLITE_TCLMD5.
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  SQLITE_SOURCE_ID: 2011-06-23 19:49:22 4374b7e83ea0a3fbc3691f9c0c936272862f32f2
    **
    *************************************************************************
    */
    //#include "tcl.h"
    //#include <errno.h>

    /*
    ** Some additional include files are needed if this file is not
    ** appended to the amalgamation.
    */
#if !SQLITE_AMALGAMATION
    //# include "sqlite3.h"
    //# include <stdlib.h>
    //# include <string.h>
    //# include <Debug.Assert.h>
    //  typedef unsigned char u8;
#endif

    /*
* Windows needs to know which symbols to export.  Unix does not.
* BUILD_sqlite should be undefined for Unix.
*/
#if BUILD_sqlite
//#undef TCL.Tcl_STORAGE_CLASS
//#define TCL.Tcl_STORAGE_CLASS DLLEXPORT
#endif // * BUILD_sqlite */

    const int NUM_PREPARED_STMTS = 10;//#define NUM_PREPARED_STMTS 10
    const int MAX_PREPARED_STMTS = 100;//#define MAX_PREPARED_STMTS 100

    /*
    ** If TCL uses UTF-8 and SQLite is configured to use iso8859, then we
    ** have to do a translation when going between the two.  Set the
    ** UTF_TRANSLATION_NEEDED macro to indicate that we need to do
    ** this translation.
    */
#if Tcl_UTF_MAX && !SQLITE_UTF8
//# define UTF_TRANSLATION_NEEDED 1
#endif

    /*
** New SQL functions can be created as TCL scripts.  Each such function
** is described by an instance of the following structure.
*/
    //typedef struct SqlFunc SqlFunc;
    public class SqlFunc
    {
      public Tcl_Interp interp;   /* The TCL interpret to execute the function */
      public Tcl_Obj pScript;     /* The Tcl_Obj representation of the script */
      public int useEvalObjv;     /* True if it is safe to use TCL.Tcl_EvalObjv */
      public string zName;        /* Name of this function */
      public SqlFunc pNext;       /* Next function on the list of them all */
    }

    /*
    ** New collation sequences function can be created as TCL scripts.  Each such
    ** function is described by an instance of the following structure.
    */
    //typedef struct SqlCollate SqlCollate;
    public class SqlCollate
    {
      public Tcl_Interp interp;   /* The TCL interpret to execute the function */
      public string zScript;      /* The script to be run */
      public SqlCollate pNext;    /* Next function on the list of them all */
    }

    /*
    ** Prepared statements are cached for faster execution.  Each prepared
    ** statement is described by an instance of the following structure.
    */
    //typedef struct SqlPreparedStmt SqlPreparedStmt;
    public class SqlPreparedStmt
    {
      public SqlPreparedStmt pNext;  /* Next in linked list */
      public SqlPreparedStmt pPrev;  /* Previous on the list */
      public sqlite3_stmt pStmt;     /* The prepared statement */
      public Mem[] aMem;             /* Original Memory Values to be reused */
      public int nSql;               /* chars in zSql[] */
      public string zSql;            /* Text of the SQL statement */
      public int nParm;              /* Size of apParm array */
      public Tcl_Obj[] apParm;       /* Array of referenced object pointers */
    }

    //typedef struct IncrblobChannel IncrblobChannel;

    /*
    ** There is one instance of this structure for each SQLite database
    ** that has been opened by the SQLite TCL interface.
    */
    //typedef struct SqliteDb SqliteDb;
    public class SqliteDb : object
    {
      public sqlite3 db;                /* The "real" database structure. MUST BE FIRST */
      public Tcl_Interp interp;         /* The interpreter used for this database */
      public string zBusy;              /* The busy callback routine */
      public string zCommit;            /* The commit hook callback routine */
      public string zTrace;             /* The trace callback routine */
      public string zProfile;           /* The profile callback routine */
      public string zProgress;          /* The progress callback routine */
      public string zAuth;              /* The authorization callback routine */
      public int disableAuth;           /* Disable the authorizer if it exists */
      public string zNull = "";         /* Text to substitute for an SQL NULL value */
      public SqlFunc pFunc;             /* List of SQL functions */
      public Tcl_Obj pUpdateHook;       /* Update hook script (if any) */
      public Tcl_Obj pRollbackHook;     /* Rollback hook script (if any) */
      public Tcl_Obj pWalHook;          /* WAL hook script (if any) */
      public Tcl_Obj pUnlockNotify;     /* Unlock notify script (if any) */
      public SqlCollate pCollate;       /* List of SQL collation functions */
      public int rc;                    /* Return code of most recent sqlite3_exec() */
      public Tcl_Obj pCollateNeeded;    /* Collation needed script */
      public SqlPreparedStmt stmtList;  /* List of prepared statements*/
      public SqlPreparedStmt stmtLast;  /* Last statement in the list */
      public int maxStmt;               /* The next maximum number of stmtList */
      public int nStmt;                 /* Number of statements in stmtList */
#if !SQLITE_OMIT_INCRBLOB
public IncrblobChannel pIncrblob; /* Linked list of open incrblob channels */
#endif
      public int nStep, nSort, nIndex;  /* Statistics for most recent operation */
      public int nTransaction;          /* Number of nested [transaction] methods */
    }

#if !SQLITE_OMIT_INCRBLOB
class IncrblobChannel
{
public sqlite3_blob pBlob;      /* sqlite3 blob handle */
public SqliteDb pDb;            /* Associated database connection */
public int iSeek;               /* Current seek offset */
public Tcl_Channel channel;     /* Channel identifier */
public IncrblobChannel pNext;   /* Linked list of all open incrblob channels */
public IncrblobChannel pPrev;   /* Linked list of all open incrblob channels */
}
#endif


    /*
** Compute a string length that is limited to what can be stored in
** lower 30 bits of a 32-bit signed integer.
*/
    static int strlen30( StringBuilder z )
    {
      //string z2 = z;
      //while( *z2 ){ z2++; }
      return 0x3fffffff & z.Length;
    }

    static int strlen30( string z )
    {
      //string z2 = z;
      //while( *z2 ){ z2++; }
      return 0x3fffffff & z.Length;
    }


#if !SQLITE_OMIT_INCRBLOB
/*
** Close all incrblob channels opened using database connection pDb.
** This is called when shutting down the database connection.
*/
static void closeIncrblobChannels( SqliteDb pDb )
{
IncrblobChannel p;
IncrblobChannel pNext;

for ( p = pDb.pIncrblob ; p != null ; p = pNext )
{
pNext = p.pNext;

/* Note: Calling unregister here call TCL.Tcl_Close on the incrblob channel,
** which deletes the IncrblobChannel structure at p. So do not
** call TCL.Tcl_Free() here.
*/
TCL.Tcl_UnregisterChannel( pDb.interp, p.channel );
}
}

/*
** Close an incremental blob channel.
*/
//static int incrblobClose(object instanceData, Tcl_Interp interp){
//  IncrblobChannel p = (IncrblobChannel )instanceData;
//  int rc = sqlite3_blob_close(p.pBlob);
//  sqlite3 db = p.pDb.db;

//  /* Remove the channel from the SqliteDb.pIncrblob list. */
//  if( p.pNext ){
//    p.pNext.pPrev = p.pPrev;
//  }
//  if( p.pPrev ){
//    p.pPrev.pNext = p.pNext;
//  }
//  if( p.pDb.pIncrblob==p ){
//    p.pDb.pIncrblob = p.pNext;
//  }

//  /* Free the IncrblobChannel structure */
//  TCL.Tcl_Free((char )p);

//  if( rc!=SQLITE_OK ){
//    TCL.Tcl_SetResult(interp, (char )sqlite3_errmsg(db), TCL.Tcl_VOLATILE);
//    return TCL.TCL_ERROR;
//  }
//  return TCL.TCL_OK;
//}

/*
** Read data from an incremental blob channel.
*/
//static int incrblobInput(
//  object instanceData,
//  char *buf,
//  int bufSize,
//  int *errorCodePtr
//){
//  IncrblobChannel p = (IncrblobChannel )instanceData;
//  int nRead = bufSize;         /* Number of bytes to read */
//  int nBlob;                   /* Total size of the blob */
//  int rc;                      /* sqlite error code */

//  nBlob = sqlite3_blob_bytes(p.pBlob);
//  if( (p.iSeek+nRead)>nBlob ){
//    nRead = nBlob-p.iSeek;
//  }
//  if( nRead<=0 ){
//    return 0;
//  }

//  rc = sqlite3_blob_read(p.pBlob, (void )buf, nRead, p.iSeek);
//  if( rc!=SQLITE_OK ){
//    *errorCodePtr = rc;
//    return -1;
//  }

//  p.iSeek += nRead;
//  return nRead;
//}

/*
** Write data to an incremental blob channel.
*/
//static int incrblobOutput(
//  object instanceData,
//  string buf,
//  int toWrite,
//  int *errorCodePtr
//){
//  IncrblobChannel p = (IncrblobChannel )instanceData;
//  int nWrite = toWrite;        /* Number of bytes to write */
//  int nBlob;                   /* Total size of the blob */
//  int rc;                      /* sqlite error code */

//  nBlob = sqlite3_blob_bytes(p.pBlob);
//  if( (p.iSeek+nWrite)>nBlob ){
//    *errorCodePtr = EINVAL;
//    return -1;
//  }
//  if( nWrite<=0 ){
//    return 0;
//  }

//  rc = sqlite3_blob_write(p.pBlob, (void )buf, nWrite, p.iSeek);
//  if( rc!=SQLITE_OK ){
//    *errorCodePtr = EIO;
//    return -1;
//  }

//  p.iSeek += nWrite;
//  return nWrite;
//}

/*
** Seek an incremental blob channel.
*/
//static int incrblobSeek(
//  object instanceData,
//  long offset,
//  int seekMode,
//  int *errorCodePtr
//){
//  IncrblobChannel p = (IncrblobChannel )instanceData;

//  switch( seekMode ){
//    case SEEK_SET:
//      p.iSeek = offset;
//      break;
//    case SEEK_CUR:
//      p.iSeek += offset;
//      break;
//    case SEEK_END:
//      p.iSeek = sqlite3_blob_bytes(p.pBlob) + offset;
//      break;

//    default: Debug.Assert(!"Bad seekMode");
//  }

//  return p.iSeek;
//}


//static void incrblobWatch(object instanceData, int mode){
//  /* NO-OP */
//}
//static int incrblobHandle(object instanceData, int dir, object *hPtr){
//  return TCL.TCL_ERROR;
//}

static TCL.Tcl_ChannelType IncrblobChannelType = {
"incrblob",                        /* typeName                             */
TCL.Tcl_CHANNEL_VERSION_2,             /* version                              */
incrblobClose,                     /* closeProc                            */
incrblobInput,                     /* inputProc                            */
incrblobOutput,                    /* outputProc                           */
incrblobSeek,                      /* seekProc                             */
0,                                 /* setOptionProc                        */
0,                                 /* getOptionProc                        */
incrblobWatch,                     /* watchProc (this is a no-op)          */
incrblobHandle,                    /* getHandleProc (always returns error) */
0,                                 /* close2Proc                           */
0,                                 /* blockModeProc                        */
0,                                 /* flushProc                            */
0,                                 /* handlerProc                          */
0,                                 /* wideSeekProc                         */
};

/*
** Create a new incrblob channel.
*/
static int count = 0;
static int createIncrblobChannel(
Tcl_Interp interp,
SqliteDb pDb,
string zDb,
string zTable,
string zColumn,
sqlite_int64 iRow,
int isReadonly
){
IncrblobChannel p;
sqlite3 db = pDb.db;
sqlite3_blob pBlob;
int rc;
int flags = TCL.Tcl_READABLE|(isReadonly ? 0 : TCL.Tcl_WRITABLE);

/* This variable is used to name the channels: "incrblob_[incr count]" */
//static int count = 0;
string zChannel = "";//string[64];

rc = sqlite3_blob_open(db, zDb, zTable, zColumn, iRow, !isReadonly, pBlob);
if( rc!=SQLITE_OK ){
TCL.Tcl_SetResult(interp, sqlite3_errmsg(pDb.db), TCL.Tcl_VOLATILE);
return TCL.TCL_ERROR;
}

p = new IncrblobChannel();//(IncrblobChannel )Tcl_Alloc(sizeof(IncrblobChannel));
p.iSeek = 0;
p.pBlob = pBlob;

sqlite3_snprintf(64, zChannel, "incrblob_%d", ++count);
p.channel = TCL.Tcl_CreateChannel(IncrblobChannelType, zChannel, p, flags);
TCL.Tcl_RegisterChannel(interp, p.channel);

/* Link the new channel into the SqliteDb.pIncrblob list. */
p.pNext = pDb.pIncrblob;
p.pPrev = null;
if( p.pNext!=null ){
p.pNext.pPrev = p;
}
pDb.pIncrblob = p;
p.pDb = pDb;

TCL.Tcl_SetResult(interp, Tcl_GetChannelName(p.channel), TCL.Tcl_VOLATILE);
return TCL.TCL_OK;
}
#else  // * else clause for "#if !SQLITE_OMIT_INCRBLOB" */
    //#define closeIncrblobChannels(pDb)
    static void closeIncrblobChannels( SqliteDb pDb )
    {
    }
#endif

    /*
** Look at the script prefix in pCmd.  We will be executing this script
** after first appending one or more arguments.  This routine analyzes
** the script to see if it is safe to use TCL.Tcl_EvalObjv() on the script
** rather than the more general TCL.Tcl_EvalEx().  TCL.Tcl_EvalObjv() is much
** faster.
**
** Scripts that are safe to use with TCL.Tcl_EvalObjv() consists of a
** command name followed by zero or more arguments with no [...] or $
** or {...} or ; to be seen anywhere.  Most callback scripts consist
** of just a single procedure name and they meet this requirement.
*/
    static int safeToUseEvalObjv( Tcl_Interp interp, Tcl_Obj pCmd )
    {
      /* We could try to do something with TCL.Tcl_Parse().  But we will instead
      ** just do a search for forbidden characters.  If any of the forbidden
      ** characters appear in pCmd, we will report the string as unsafe.
      */
      string z;
      int n = 0;
      z = TCL.Tcl_GetStringFromObj( pCmd, out n );
      while ( n-- > 0 )
      {
        int c = z[n];// *( z++ );
        if ( c == '$' || c == '[' || c == ';' )
          return 0;
      }
      return 1;
    }

    /*
    ** Find an SqlFunc structure with the given name.  Or create a new
    ** one if an existing one cannot be found.  Return a pointer to the
    ** structure.
    */
    static SqlFunc findSqlFunc( SqliteDb pDb, string zName )
    {
      SqlFunc p, pNew;
      int i;
      pNew = new SqlFunc();//(SqlFunc)Tcl_Alloc( sizeof(*pNew) + strlen30(zName) + 1 );
      //pNew.zName = (char)&pNew[1];
      //for(i=0; zName[i]; i++){ pNew.zName[i] = tolower(zName[i]); }
      //pNew.zName[i] = 0;
      pNew.zName = zName.ToLower();
      for ( p = pDb.pFunc; p != null; p = p.pNext )
      {
        if ( p.zName == pNew.zName )
        {
          //Tcl_Free((char)pNew);
          return p;
        }
      }
      pNew.interp = pDb.interp;
      pNew.pScript = null;
      pNew.pNext = pDb.pFunc;
      pDb.pFunc = pNew;
      return pNew;
    }

    /*
    ** Finalize and free a list of prepared statements
    */
    static void flushStmtCache( SqliteDb pDb )
    {
      SqlPreparedStmt pPreStmt;

      while ( pDb.stmtList != null )
      {
        sqlite3_finalize( pDb.stmtList.pStmt );
        pPreStmt = pDb.stmtList;
        pDb.stmtList = pDb.stmtList.pNext;
        TCL.Tcl_Free( ref pPreStmt );
      }
      pDb.nStmt = 0;
      pDb.stmtLast = null;
    }

    /*
    ** TCL calls this procedure when an sqlite3 database command is
    ** deleted.
    */
    static void DbDeleteCmd( ref object db )
    {
      SqliteDb pDb = (SqliteDb)db;
      flushStmtCache( pDb );
      closeIncrblobChannels( pDb );
      sqlite3_close( pDb.db );
      while ( pDb.pFunc != null )
      {
        SqlFunc pFunc = pDb.pFunc;
        pDb.pFunc = pFunc.pNext;
        TCL.Tcl_DecrRefCount( ref pFunc.pScript );
        TCL.Tcl_Free( ref pFunc );
      }
      while ( pDb.pCollate != null )
      {
        SqlCollate pCollate = pDb.pCollate;
        pDb.pCollate = pCollate.pNext;
        TCL.Tcl_Free( ref pCollate );
      }
      if ( pDb.zBusy != null )
      {
        TCL.Tcl_Free( ref pDb.zBusy );
      }
      if ( pDb.zTrace != null )
      {
        TCL.Tcl_Free( ref pDb.zTrace );
      }
      if ( pDb.zProfile != null )
      {
        TCL.Tcl_Free( ref pDb.zProfile );
      }
      if ( pDb.zAuth != null )
      {
        TCL.Tcl_Free( ref pDb.zAuth );
      }
      if ( pDb.zNull != null )
      {
        TCL.Tcl_Free( ref pDb.zNull );
      }
      if ( pDb.pUpdateHook != null )
      {
        TCL.Tcl_DecrRefCount( ref pDb.pUpdateHook );
      }
      if ( pDb.pRollbackHook != null )
      {
        TCL.Tcl_DecrRefCount( ref pDb.pRollbackHook );
      }
      if ( pDb.pWalHook != null )
      {
        TCL.Tcl_DecrRefCount( ref pDb.pWalHook );
      }
      if ( pDb.pCollateNeeded != null )
      {
        TCL.Tcl_DecrRefCount( ref pDb.pCollateNeeded );
      }
      TCL.Tcl_Free( ref pDb );
    }

    /*
    ** This routine is called when a database file is locked while trying
    ** to execute SQL.
    */
    static int DbBusyHandler( object cd, int nTries )
    {
      SqliteDb pDb = (SqliteDb)cd;
      int rc;
      StringBuilder zVal = new StringBuilder( 30 );//char zVal[30];

      sqlite3_snprintf( 30, zVal, "%d", nTries );
      rc = TCL.Tcl_VarEval( pDb.interp, pDb.zBusy, " ", zVal.ToString(), null );
      if ( rc != TCL.TCL_OK || atoi( TCL.Tcl_GetStringResult( pDb.interp ) ) != 0 )
      {
        return 0;
      }
      return 1;
    }

#if !SQLITE_OMIT_PROGRESS_CALLBACK
    /*
** This routine is invoked as the 'progress callback' for the database.
*/
    static int DbProgressHandler( object cd )
    {
      SqliteDb pDb = (SqliteDb)cd;
      int rc;

      Debug.Assert( pDb.zProgress != null );
      rc = TCL.Tcl_Eval( pDb.interp, pDb.zProgress );
      if ( rc != TCL.TCL_OK || atoi( TCL.Tcl_GetStringResult( pDb.interp ) ) != 0 )
      {
        return 1;
      }
      return 0;
    }
#endif

#if !SQLITE_OMIT_TRACE
    /*
** This routine is called by the SQLite trace handler whenever a new
** block of SQL is executed.  The TCL script in pDb.zTrace is executed.
*/
    static void DbTraceHandler( object cd, string zSql )
    {
      SqliteDb pDb = (SqliteDb)cd;
      TclObject str = null;

      TCL.Tcl_DStringInit( out str );
      TCL.Tcl_DStringAppendElement( str, pDb.zTrace );
      TCL.Tcl_DStringAppendElement( str, " {" + zSql + "}" );
      TCL.Tcl_EvalObjEx( pDb.interp, str, 0 );// TCL.Tcl_Eval( pDb.interp, TCL.Tcl_DStringValue( ref str ) );
      TCL.Tcl_DStringFree( ref str );
      TCL.Tcl_ResetResult( pDb.interp );
    }
#endif

#if !SQLITE_OMIT_TRACE
    /*
** This routine is called by the SQLite profile handler after a statement
** SQL has executed.  The TCL script in pDb.zProfile is evaluated.
*/
    static void DbProfileHandler( object cd, string zSql, sqlite_int64 tm )
    {
      SqliteDb pDb = (SqliteDb)cd;
      TclObject str = null;
      StringBuilder zTm = new StringBuilder( 100 );//char zTm[100];

      sqlite3_snprintf( 100, zTm, "%lld", tm );
      TCL.Tcl_DStringInit( out str );
      TCL.Tcl_DStringAppendElement( str, pDb.zProfile );
      TCL.Tcl_DStringAppendElement( str, " {" + zSql + "}" );
      TCL.Tcl_DStringAppendElement( str, " {" + zTm.ToString() + "}" );
      TCL.Tcl_Eval( pDb.interp, str.ToString() );
      TCL.Tcl_DStringFree( ref str );
      TCL.Tcl_ResetResult( pDb.interp );
    }
#endif

    /*
** This routine is called when a transaction is committed.  The
** TCL script in pDb.zCommit is executed.  If it returns non-zero or
** if it throws an exception, the transaction is rolled back instead
** of being committed.
*/
    static int DbCommitHandler( object cd )
    {
      SqliteDb pDb = (SqliteDb)cd;
      int rc;

      rc = TCL.Tcl_Eval( pDb.interp, pDb.zCommit );
      if ( rc != TCL.TCL_OK || atoi( TCL.Tcl_GetStringResult( pDb.interp ) ) != 0 )
      {
        return 1;
      }
      return 0;
    }

    static void DbRollbackHandler( object _object )
    {
      SqliteDb pDb = (SqliteDb)_object;
      Debug.Assert( pDb.pRollbackHook != null );
      if ( TCL.TCL_OK != TCL.Tcl_EvalObjEx( pDb.interp, pDb.pRollbackHook, 0 ) )
      {
        TCL.Tcl_BackgroundError( pDb.interp );
      }
    }

    /*
    ** This procedure handles wal_hook callbacks.
    */
    static int DbWalHandler(
    object clientData,
    sqlite3 db,
    string zDb,
    int nEntry
    )
    {
      int ret = SQLITE_OK;
      Tcl_Obj p;
      SqliteDb pDb = (SqliteDb)clientData;
      Tcl_Interp interp = pDb.interp;
      Debug.Assert( pDb.pWalHook != null );

      p = TCL.Tcl_DuplicateObj( pDb.pWalHook );
      TCL.Tcl_IncrRefCount( p );
      TCL.Tcl_ListObjAppendElement( interp, p, TCL.Tcl_NewStringObj( zDb, -1 ) );
      TCL.Tcl_ListObjAppendElement( interp, p, TCL.Tcl_NewIntObj( nEntry ) );
      if ( TCL.TCL_OK != TCL.Tcl_EvalObjEx( interp, p, 0 )
      || TCL.TCL_OK != TCL.Tcl_GetIntFromObj( interp, TCL.Tcl_GetObjResult( interp ), out ret )
      )
      {
        TCL.Tcl_BackgroundError( interp );
      }
      TCL.Tcl_DecrRefCount( ref p );

      return ret;
    }

#if (SQLITE_TEST) && (SQLITE_ENABLE_UNLOCK_NOTIFY)
static void setTestUnlockNotifyVars(Tcl_Interp interp, int iArg, int nArg){
char zBuf[64];
sprintf(zBuf, "%d", iArg);
Tcl_SetVar(interp, "sqlite_unlock_notify_arg", zBuf, TCL_GLOBAL_ONLY);
sprintf(zBuf, "%d", nArg);
Tcl_SetVar(interp, "sqlite_unlock_notify_argcount", zBuf, TCL_GLOBAL_ONLY);
}
#else
    //# define setTestUnlockNotifyVars(x,y,z)
#endif

#if SQLITE_ENABLE_UNLOCK_NOTIFY
static void DbUnlockNotify(void **apArg, int nArg){
int i;
for(i=0; i<nArg; i++){
const int flags = (TCL_EVAL_GLOBAL|TCL_EVAL_DIRECT);
SqliteDb *pDb = (SqliteDb )apArg[i];
setTestUnlockNotifyVars(pDb.interp, i, nArg);
Debug.Assert( pDb.pUnlockNotify);
Tcl_EvalObjEx(pDb.interp, pDb.pUnlockNotify, flags);
Tcl_DecrRefCount(pDb.pUnlockNotify);
pDb.pUnlockNotify = 0;
}
}
#endif

    static void DbUpdateHandler(
    object p,
    int op,
    string zDb,
    string zTbl,
    sqlite_int64 rowid
    )
    {
      SqliteDb pDb = (SqliteDb)p;
      Tcl_Obj pCmd;

      Debug.Assert( pDb.pUpdateHook != null );
      Debug.Assert( op == SQLITE_INSERT || op == SQLITE_UPDATE || op == SQLITE_DELETE );

      pCmd = TCL.Tcl_DuplicateObj( pDb.pUpdateHook );
      TCL.Tcl_IncrRefCount( pCmd );
      TCL.Tcl_ListObjAppendElement( null, pCmd, TCL.Tcl_NewStringObj(
      ( ( op == SQLITE_INSERT ) ? "INSERT" : ( op == SQLITE_UPDATE ) ? "UPDATE" : "DELETE" ), -1 ) );
      TCL.Tcl_ListObjAppendElement( null, pCmd, TCL.Tcl_NewStringObj( zDb, -1 ) );
      TCL.Tcl_ListObjAppendElement( null, pCmd, TCL.Tcl_NewStringObj( zTbl, -1 ) );
      TCL.Tcl_ListObjAppendElement( null, pCmd, TCL.Tcl_NewWideIntObj( rowid ) );
      TCL.Tcl_EvalObjEx( pDb.interp, pCmd, TCL.TCL_EVAL_DIRECT );
      TCL.Tcl_DecrRefCount( ref pCmd );
    }

    static void tclCollateNeeded(
    object pCtx,
    sqlite3 db,
    int enc,
    string zName
    )
    {
      SqliteDb pDb = (SqliteDb)pCtx;
      Tcl_Obj pScript = TCL.Tcl_DuplicateObj( pDb.pCollateNeeded );
      TCL.Tcl_IncrRefCount( pScript );
      TCL.Tcl_ListObjAppendElement( null, pScript, TCL.Tcl_NewStringObj( zName, -1 ) );
      TCL.Tcl_EvalObjEx( pDb.interp, pScript, 0 );
      TCL.Tcl_DecrRefCount( ref pScript );
    }

    /*
    ** This routine is called to evaluate an SQL collation function implemented
    ** using TCL script.
    */
    static int tclSqlCollate(
    object pCtx,
    int nA,
    string zA,
    int nB,
    string zB
    )
    {
      SqlCollate p = (SqlCollate)pCtx;
      Tcl_Obj pCmd;

      pCmd = TCL.Tcl_NewStringObj( p.zScript, -1 );
      TCL.Tcl_IncrRefCount( pCmd );
      TCL.Tcl_ListObjAppendElement( p.interp, pCmd, TCL.Tcl_NewStringObj( zA, nA ) );
      TCL.Tcl_ListObjAppendElement( p.interp, pCmd, TCL.Tcl_NewStringObj( zB, nB ) );
      TCL.Tcl_EvalObjEx( p.interp, pCmd, TCL.TCL_EVAL_DIRECT );
      TCL.Tcl_DecrRefCount( ref pCmd );
      return ( atoi( TCL.Tcl_GetStringResult( p.interp ) ) );
    }

    /*
    ** This routine is called to evaluate an SQL function implemented
    ** using TCL script.
    */
    static void tclSqlFunc( sqlite3_context context, int argc, sqlite3_value[] argv )
    {
      SqlFunc p = (SqlFunc)sqlite3_user_data( context );
      Tcl_Obj pCmd = null;
      int i;
      int rc;

      if ( argc == 0 )
      {
        /* If there are no arguments to the function, call TCL.Tcl_EvalObjEx on the
        ** script object directly.  This allows the TCL compiler to generate
        ** bytecode for the command on the first invocation and thus make
        ** subsequent invocations much faster. */
        pCmd = p.pScript;
        TCL.Tcl_IncrRefCount( pCmd );
        rc = TCL.Tcl_EvalObjEx( p.interp, pCmd, 0 );
        TCL.Tcl_DecrRefCount( ref pCmd );
      }
      else
      {
        /* If there are arguments to the function, make a shallow copy of the
        ** script object, lappend the arguments, then evaluate the copy.
        **
        ** By "shallow" copy, we mean a only the outer list Tcl_Obj is duplicated.
        ** The new Tcl_Obj contains pointers to the original list elements.
        ** That way, when TCL.Tcl_EvalObjv() is run and shimmers the first element
        ** of the list to tclCmdNameType, that alternate representation will
        ** be preserved and reused on the next invocation.
        */
        Tcl_Obj[] aArg = null;
        int nArg = 0;
        if ( TCL.Tcl_ListObjGetElements( p.interp, p.pScript, out nArg, out aArg ) )
        {
          sqlite3_result_error( context, TCL.Tcl_GetStringResult( p.interp ), -1 );
          return;
        }
        pCmd = TCL.Tcl_NewListObj( nArg, aArg );
        TCL.Tcl_IncrRefCount( pCmd );
        for ( i = 0; i < argc; i++ )
        {
          sqlite3_value pIn = argv[i];
          Tcl_Obj pVal;

          /* Set pVal to contain the i'th column of this row. */
          switch ( sqlite3_value_type( pIn ) )
          {
            case SQLITE_BLOB:
              {
                int bytes = sqlite3_value_bytes( pIn );
                pVal = TCL.Tcl_NewByteArrayObj( sqlite3_value_blob( pIn ), bytes );
                break;
              }
            case SQLITE_INTEGER:
              {
                sqlite_int64 v = sqlite3_value_int64( pIn );
                if ( v >= -2147483647 && v <= 2147483647 )
                {
                  pVal = TCL.Tcl_NewIntObj( (int)v );
                }
                else
                {
                  pVal = TCL.Tcl_NewWideIntObj( v );
                }
                break;
              }
            case SQLITE_FLOAT:
              {
                double r = sqlite3_value_double( pIn );
                pVal = TCL.Tcl_NewDoubleObj( r );
                break;
              }
            case SQLITE_NULL:
              {
                pVal = TCL.Tcl_NewStringObj( "", 0 );
                break;
              }
            default:
              {
                int bytes = sqlite3_value_bytes( pIn );
                pVal = TCL.Tcl_NewStringObj( sqlite3_value_text( pIn ), bytes );
                break;
              }
          }
          rc = TCL.Tcl_ListObjAppendElement( p.interp, pCmd, pVal ) ? 1 : 0;
          if ( rc != 0 )
          {
            TCL.Tcl_DecrRefCount( ref pCmd );
            sqlite3_result_error( context, TCL.Tcl_GetStringResult( p.interp ), -1 );
            return;
          }
        }
        if ( p.useEvalObjv == 0 )
        {
          /* TCL.Tcl_EvalObjEx() will automatically call TCL.Tcl_EvalObjv() if pCmd
          ** is a list without a string representation.  To prevent this from
          ** happening, make sure pCmd has a valid string representation */
          TCL.Tcl_GetString( pCmd );
        }
        rc = TCL.Tcl_EvalObjEx( p.interp, pCmd, TCL.TCL_EVAL_DIRECT );
        TCL.Tcl_DecrRefCount( ref pCmd );
      }

      if ( rc != 0 && rc != TCL.TCL_RETURN )
      {
        sqlite3_result_error( context, TCL.Tcl_GetStringResult( p.interp ), -1 );
      }
      else
      {
        Tcl_Obj pVar = TCL.Tcl_GetObjResult( p.interp );
        int n = 0;
        string data = "";
        Tcl_WideInt v = 0;
        double r = 0;
        string zType = pVar.typePtr;//string zType = (pVar.typePtr ? pVar.typePtr.name : "");
        if ( zType == "bytearray" )
        { //&& pVar.bytes==0 ){
          /* Only return a BLOB type if the Tcl variable is a bytearray and
          ** has no string representation. */
          data = Encoding.UTF8.GetString( TCL.Tcl_GetByteArrayFromObj( pVar, out n ) );
          sqlite3_result_blob( context, data, n, null );
        }
        else if ( zType == "boolean" )
        {
          TCL.Tcl_GetIntFromObj( null, pVar, out n );
          sqlite3_result_int( context, n );
        }
        else if ( zType == "wideint" ||
        zType == "int" || Int64.TryParse( pVar.ToString(), out v ) )
        {
          TCL.Tcl_GetWideIntFromObj( null, pVar, out v );
          sqlite3_result_int64( context, v );
        }
        else if ( zType == "double" || Double.TryParse( pVar.ToString(), out r ) )
        {
          TCL.Tcl_GetDoubleFromObj( null, pVar, out r );
          sqlite3_result_double( context, r );
        }
        else
        {
          data = TCL.Tcl_GetStringFromObj( pVar, n );
          n = data.Length;
          sqlite3_result_text( context, data, n, SQLITE_TRANSIENT );
        }
      }
    }

#if !SQLITE_OMIT_AUTHORIZATION
/*
** This is the authentication function.  It appends the authentication
** type code and the two arguments to zCmd[] then invokes the result
** on the interpreter.  The reply is examined to determine if the
** authentication fails or succeeds.
*/
static int auth_callback(
object pArg,
int code,
const string zArg1,
const string zArg2,
const string zArg3,
const string zArg4
){
string zCode;
TCL.Tcl_DString str;
int rc;
const string zReply;
SqliteDb pDb = (SqliteDb)pArg;
if( pdb.disableAuth ) return SQLITE_OK;

switch( code ){
case SQLITE_COPY              : zCode="SQLITE_COPY"; break;
case SQLITE_CREATE_INDEX      : zCode="SQLITE_CREATE_INDEX"; break;
case SQLITE_CREATE_TABLE      : zCode="SQLITE_CREATE_TABLE"; break;
case SQLITE_CREATE_TEMP_INDEX : zCode="SQLITE_CREATE_TEMP_INDEX"; break;
case SQLITE_CREATE_TEMP_TABLE : zCode="SQLITE_CREATE_TEMP_TABLE"; break;
case SQLITE_CREATE_TEMP_TRIGGER: zCode="SQLITE_CREATE_TEMP_TRIGGER"; break;
case SQLITE_CREATE_TEMP_VIEW  : zCode="SQLITE_CREATE_TEMP_VIEW"; break;
case SQLITE_CREATE_TRIGGER    : zCode="SQLITE_CREATE_TRIGGER"; break;
case SQLITE_CREATE_VIEW       : zCode="SQLITE_CREATE_VIEW"; break;
case SQLITE_DELETE            : zCode="SQLITE_DELETE"; break;
case SQLITE_DROP_INDEX        : zCode="SQLITE_DROP_INDEX"; break;
case SQLITE_DROP_TABLE        : zCode="SQLITE_DROP_TABLE"; break;
case SQLITE_DROP_TEMP_INDEX   : zCode="SQLITE_DROP_TEMP_INDEX"; break;
case SQLITE_DROP_TEMP_TABLE   : zCode="SQLITE_DROP_TEMP_TABLE"; break;
case SQLITE_DROP_TEMP_TRIGGER : zCode="SQLITE_DROP_TEMP_TRIGGER"; break;
case SQLITE_DROP_TEMP_VIEW    : zCode="SQLITE_DROP_TEMP_VIEW"; break;
case SQLITE_DROP_TRIGGER      : zCode="SQLITE_DROP_TRIGGER"; break;
case SQLITE_DROP_VIEW         : zCode="SQLITE_DROP_VIEW"; break;
case SQLITE_INSERT            : zCode="SQLITE_INSERT"; break;
case SQLITE_PRAGMA            : zCode="SQLITE_PRAGMA"; break;
case SQLITE_READ              : zCode="SQLITE_READ"; break;
case SQLITE_SELECT            : zCode="SQLITE_SELECT"; break;
case SQLITE_TRANSACTION       : zCode="SQLITE_TRANSACTION"; break;
case SQLITE_UPDATE            : zCode="SQLITE_UPDATE"; break;
case SQLITE_ATTACH            : zCode="SQLITE_ATTACH"; break;
case SQLITE_DETACH            : zCode="SQLITE_DETACH"; break;
case SQLITE_ALTER_TABLE       : zCode="SQLITE_ALTER_TABLE"; break;
case SQLITE_REINDEX           : zCode="SQLITE_REINDEX"; break;
case SQLITE_ANALYZE           : zCode="SQLITE_ANALYZE"; break;
case SQLITE_CREATE_VTABLE     : zCode="SQLITE_CREATE_VTABLE"; break;
case SQLITE_DROP_VTABLE       : zCode="SQLITE_DROP_VTABLE"; break;
case SQLITE_FUNCTION          : zCode="SQLITE_FUNCTION"; break;
case SQLITE_SAVEPOINT         : zCode="SQLITE_SAVEPOINT"; break;
default                       : zCode="????"; break;
}
TCL.Tcl_DStringInit(&str);
TCL.Tcl_DStringAppend(&str, pDb.zAuth, -1);
TCL.Tcl_DStringAppendElement(&str, zCode);
TCL.Tcl_DStringAppendElement(&str, zArg1 ? zArg1 : "");
TCL.Tcl_DStringAppendElement(&str, zArg2 ? zArg2 : "");
TCL.Tcl_DStringAppendElement(&str, zArg3 ? zArg3 : "");
TCL.Tcl_DStringAppendElement(&str, zArg4 ? zArg4 : "");
rc = TCL.Tcl_GlobalEval(pDb.interp, TCL.Tcl_DStringValue(&str));
TCL.Tcl_DStringFree(&str);
zReply = TCL.Tcl_GetStringResult(pDb.interp);
if( strcmp(zReply,"SQLITE_OK")==0 ){
rc = SQLITE_OK;
}else if( strcmp(zReply,"SQLITE_DENY")==0 ){
rc = SQLITE_DENY;
}else if( strcmp(zReply,"SQLITE_IGNORE")==0 ){
rc = SQLITE_IGNORE;
}else{
rc = 999;
}
return rc;
}
#endif // * SQLITE_OMIT_AUTHORIZATION */

    /*
** zText is a pointer to text obtained via an sqlite3_result_text()
** or similar interface. This routine returns a Tcl string object,
** reference count set to 0, containing the text. If a translation
** between iso8859 and UTF-8 is required, it is preformed.
*/
    static Tcl_Obj dbTextToObj( string zText )
    {
      Tcl_Obj pVal;
#if UTF_TRANSLATION_NEEDED
//TCL.Tcl_DString dCol;
//TCL.Tcl_DStringInit(&dCol);
//TCL.Tcl_ExternalToUtfDString(NULL, zText, -1, dCol);
//pVal = TCL.Tcl_NewStringObj(Tcl_DStringValue(&dCol), -1);
//TCL.Tcl_DStringFree(ref dCol);
if (zText.Length == Encoding.UTF8.GetByteCount(zText)) pVal = TCL.Tcl_NewStringObj( zText, -1 );
else pVal = TCL.Tcl_NewStringObj( zText, -1 );
#else
      pVal = TCL.Tcl_NewStringObj( zText, -1 );
#endif
      return pVal;
    }

    /*
    ** This routine reads a line of text from FILE in, stores
    ** the text in memory obtained from malloc() and returns a pointer
    ** to the text.  NULL is returned at end of file, or if malloc()
    ** fails.
    **
    ** The interface is like "readline" but no command-line editing
    ** is done.
    **
    ** copied from shell.c from '.import' command
    */
    //static char *local_getline(string zPrompt, FILE *in){
    //  string zLine;
    //  int nLine;
    //  int n;
    //  int eol;

    //  nLine = 100;
    //  zLine = malloc( nLine );
    //  if( zLine==0 ) return 0;
    //  n = 0;
    //  eol = 0;
    //  while( !eol ){
    //    if( n+100>nLine ){
    //      nLine = nLine*2 + 100;
    //      zLine = realloc(zLine, nLine);
    //      if( zLine==0 ) return 0;
    //    }
    //    if( fgets(&zLine[n], nLine - n, in)==0 ){
    //      if( n==0 ){
    //        free(zLine);
    //        return 0;
    //      }
    //      zLine[n] = 0;
    //      eol = 1;
    //      break;
    //    }
    //    while( zLine[n] ){ n++; }
    //    if( n>0 && zLine[n-1]=='\n' ){
    //      n--;
    //      zLine[n] = 0;
    //      eol = 1;
    //    }
    //  }
    //  zLine = realloc( zLine, n+1 );
    //  return zLine;
    //}


    /*
    ** This function is part of the implementation of the command:
    **
    **   $db transaction [-deferred|-immediate|-exclusive] SCRIPT
    **
    ** It is invoked after evaluating the script SCRIPT to commit or rollback
    ** the transaction or savepoint opened by the [transaction] command.
    */
    static int DbTransPostCmd(
    object data,                 /* data[0] is the Sqlite3Db* for $db */
    Tcl_Interp interp,             /* Tcl interpreter */
    int result                     /* Result of evaluating SCRIPT */
    )
    {
      string[] azEnd = {
"RELEASE _tcl_transaction",        /* rc==TCL_ERROR, nTransaction!=0 */
"COMMIT",                          /* rc!=TCL_ERROR, nTransaction==0 */
"ROLLBACK TO _tcl_transaction ; RELEASE _tcl_transaction",
"ROLLBACK"                         /* rc==TCL_ERROR, nTransaction==0 */
};
      SqliteDb pDb = (SqliteDb)data;
      int rc = result;
      string zEnd;

      pDb.nTransaction--;
      zEnd = azEnd[( ( rc == TCL.TCL_ERROR ) ? 1 : 0 ) * 2 + ( ( pDb.nTransaction == 0 ) ? 1 : 0 )];

      pDb.disableAuth++;
      if ( sqlite3_exec( pDb.db, zEnd, 0, 0, 0 ) != 0 )
      {
        /* This is a tricky scenario to handle. The most likely cause of an
        ** error is that the exec() above was an attempt to commit the 
        ** top-level transaction that returned SQLITE_BUSY. Or, less likely,
        ** that an IO-error has occured. In either case, throw a Tcl exception
        ** and try to rollback the transaction.
        **
        ** But it could also be that the user executed one or more BEGIN, 
        ** COMMIT, SAVEPOINT, RELEASE or ROLLBACK commands that are confusing
        ** this method's logic. Not clear how this would be best handled.
        */
        if ( rc != TCL.TCL_ERROR )
        {
          TCL.Tcl_AppendResult( interp, sqlite3_errmsg( pDb.db ), 0 );
          rc = TCL.TCL_ERROR;
        }
        sqlite3_exec( pDb.db, "ROLLBACK", 0, 0, 0 );
      }
      pDb.disableAuth--;

      return rc;
    }

    /*
    ** Search the cache for a prepared-statement object that implements the
    ** first SQL statement in the buffer pointed to by parameter zIn. If
    ** no such prepared-statement can be found, allocate and prepare a new
    ** one. In either case, bind the current values of the relevant Tcl
    ** variables to any $var, :var or @var variables in the statement. Before
    ** returning, set *ppPreStmt to point to the prepared-statement object.
    **
    ** Output parameter *pzOut is set to point to the next SQL statement in
    ** buffer zIn, or to the '\0' byte at the end of zIn if there is no
    ** next statement.
    **
    ** If successful, TCL_OK is returned. Otherwise, TCL_ERROR is returned
    ** and an error message loaded into interpreter pDb.interp.
    */
    static int dbPrepareAndBind(
    SqliteDb pDb,                 /* Database object */
    string zIn,                   /* SQL to compile */
    ref string pzOut,             /* OUT: Pointer to next SQL statement */
    ref SqlPreparedStmt ppPreStmt /* OUT: Object used to cache statement */
    )
    {
      string zSql = zIn;              /* Pointer to first SQL statement in zIn */
      sqlite3_stmt pStmt = null;      /* Prepared statement object */
      SqlPreparedStmt pPreStmt;       /* Pointer to cached statement */
      int nSql;                       /* Length of zSql in bytes */
      int nVar = 0;                   /* Number of variables in statement */
      int iParm = 0;                  /* Next free entry in apParm */
      int i;
      Tcl_Interp interp = pDb.interp;

      pzOut = null;
      ppPreStmt = null;

      /* Trim spaces from the start of zSql and calculate the remaining length. */
      zSql = zSql.TrimStart(); //while ( isspace( zSql[0] ) ) { zSql++; }
      nSql = strlen30( zSql );

      for ( pPreStmt = pDb.stmtList; pPreStmt != null; pPreStmt = pPreStmt.pNext )
      {
        int n = pPreStmt.nSql;
        if ( nSql >= n
        && zSql.StartsWith(pPreStmt.zSql)
        && ( nSql == n /* zSql[n]==0 */|| zSql[n - 1] == ';' )
        )
        {
          pStmt = pPreStmt.pStmt;
          /* Restore aMem values */
          if ( pStmt.aMem.Length < pPreStmt.aMem.Length )
            Array.Resize( ref pStmt.aMem, pPreStmt.aMem.Length );
          for ( int ix = 0; ix < pPreStmt.aMem.Length; ix++ )
          {
            pPreStmt.aMem[ix].CopyTo( ref pStmt.aMem[ix] );
          }

          pzOut = zSql.Substring( pPreStmt.nSql );

          /* When a prepared statement is found, unlink it from the
          ** cache list.  It will later be added back to the beginning
          ** of the cache list in order to implement LRU replacement.
          */
          if ( pPreStmt.pPrev != null )
          {
            pPreStmt.pPrev.pNext = pPreStmt.pNext;
          }
          else
          {
            pDb.stmtList = pPreStmt.pNext;
          }
          if ( pPreStmt.pNext != null )
          {
            pPreStmt.pNext.pPrev = pPreStmt.pPrev;
          }
          else
          {
            pDb.stmtLast = pPreStmt.pPrev;
          }
          pDb.nStmt--;
          nVar = sqlite3_bind_parameter_count( pStmt );
          break;
        }
      }

      /* If no prepared statement was found. Compile the SQL text. Also allocate
      ** a new SqlPreparedStmt structure.  */
      if ( pPreStmt == null )
      {
        int nByte;

        if ( SQLITE_OK != sqlite3_prepare_v2( pDb.db, zSql, -1, ref pStmt, ref pzOut ) )
        {
          TCL.Tcl_SetObjResult( interp, dbTextToObj( sqlite3_errmsg( pDb.db ) ) );
          pPreStmt = new SqlPreparedStmt();// (SqlPreparedStmt)Tcl_Alloc( nByte );
          return TCL.TCL_ERROR;
        }
        if ( pStmt == null )
        {
          if ( SQLITE_OK != sqlite3_errcode( pDb.db ) )
          {
            /* A compile-time error in the statement. */
            TCL.Tcl_SetObjResult( interp, dbTextToObj( sqlite3_errmsg( pDb.db ) ) );
            return TCL.TCL_ERROR;
          }
          else
          {
            /* The statement was a no-op.  Continue to the next statement
            ** in the SQL string.
            */
            return TCL.TCL_OK;
          }
        }

        Debug.Assert( pPreStmt == null );
        nVar = sqlite3_bind_parameter_count( pStmt );
        //nByte = sizeof(SqlPreparedStmt) + nVar*sizeof(Tcl_Obj );
        pPreStmt = new SqlPreparedStmt();// (SqlPreparedStmt)Tcl_Alloc( nByte );
        //memset(pPreStmt, 0, nByte);

        pPreStmt.pStmt = pStmt;
        pPreStmt.nSql = ( zSql.Length - pzOut.Length );
        pPreStmt.zSql = sqlite3_sql( pStmt );
        pPreStmt.apParm = new TclObject[nVar];//pPreStmt[1];
      }
      Debug.Assert( pPreStmt != null );
      Debug.Assert( strlen30( pPreStmt.zSql ) == pPreStmt.nSql );
      Debug.Assert( zSql.StartsWith( pPreStmt.zSql ) );

      /* Bind values to parameters that begin with $ or : */
      for ( i = 1; i <= nVar; i++ )
      {
        string zVar = sqlite3_bind_parameter_name( pStmt, i );
        if ( !String.IsNullOrEmpty( zVar ) && ( zVar[0] == '$' || zVar[0] == ':' || zVar[0] == '@' ) )
        {
          Tcl_Obj pVar = TCL.Tcl_GetVar2Ex( interp, zVar.Substring( 1 ), null, 0 );
          if ( pVar != null && pVar.typePtr != "null" )
          {
            int n = 0;
            string data;
            string zType = pVar.typePtr;
            //char c = zType[0];
            if ( zVar[0] == '@' ||
            ( zType == "bytearray" ) )// TODO -- && pVar.bytes == 0 ) )
            {
              /* Load a BLOB type if the Tcl variable is a bytearray and
              ** it has no string representation or the host
              ** parameter name begins with "@". */
              if ( zVar[0] == '@' || pVar.stringRep == null )
                sqlite3_bind_blob( pStmt, i, TCL.Tcl_GetByteArrayFromObj( pVar, out n ), n, SQLITE_STATIC );
              else
                sqlite3_bind_text( pStmt, i, TCL.Tcl_GetStringFromObj( pVar, out n ), n, SQLITE_STATIC );
              TCL.Tcl_IncrRefCount( pVar );
              pPreStmt.apParm[iParm++] = pVar;
            }
            else if ( zType == "boolean" )
            {
              TCL.Tcl_GetIntFromObj( interp, pVar, out n );
              sqlite3_bind_int( pStmt, i, n );
            }
            else if ( zType == "double" )
            {
              double r = 0;
              TCL.Tcl_GetDoubleFromObj( interp, pVar, out r );
              sqlite3_bind_double( pStmt, i, r );
            }
            else if ( zType == "wideint" ||
             zType == "int" )
            {
              Tcl_WideInt v = 0;
              TCL.Tcl_GetWideIntFromObj( interp, pVar, out v );
              sqlite3_bind_int64( pStmt, i, v );
            }
            else
            {
              data = TCL.Tcl_GetStringFromObj( pVar, out n );
              sqlite3_bind_text( pStmt, i, data, n, SQLITE_STATIC );
              TCL.Tcl_IncrRefCount( pVar );
              pPreStmt.apParm[iParm++] = pVar;
            }
          }
          else
          {
            sqlite3_bind_null( pStmt, i );
          }
        }
      }
      pPreStmt.nParm = iParm;
      /* save aMem values for later reuse */
      pPreStmt.aMem = new Mem[pPreStmt.pStmt.aMem.Length];
      for ( int ix = 0; ix < pPreStmt.pStmt.aMem.Length; ix++ )
      {
        pPreStmt.pStmt.aMem[ix].CopyTo( ref pPreStmt.aMem[ix] );
      }
      ppPreStmt = pPreStmt;

      return TCL.TCL_OK;
    }


    /*
    ** Release a statement reference obtained by calling dbPrepareAndBind().
    ** There should be exactly one call to this function for each call to
    ** dbPrepareAndBind().
    **
    ** If the discard parameter is non-zero, then the statement is deleted
    ** immediately. Otherwise it is added to the LRU list and may be returned
    ** by a subsequent call to dbPrepareAndBind().
    */
    static void dbReleaseStmt(
    SqliteDb pDb,                  /* Database handle */
    SqlPreparedStmt pPreStmt,      /* Prepared statement handle to release */
    int discard                    /* True to delete (not cache) the pPreStmt */
    )
    {
      int i;

      /* Free the bound string and blob parameters */
      for ( i = 0; i < pPreStmt.nParm; i++ )
      {
        TCL.Tcl_DecrRefCount( ref pPreStmt.apParm[i] );
      }
      pPreStmt.nParm = 0;

      if ( pDb.maxStmt <= 0 || discard != 0 )
      {
        /* If the cache is turned off, deallocated the statement */
        sqlite3_finalize( pPreStmt.pStmt );
        TCL.Tcl_Free( ref pPreStmt );
      }
      else
      {
        /* Add the prepared statement to the beginning of the cache list. */
        pPreStmt.pNext = pDb.stmtList;
        pPreStmt.pPrev = null;
        if ( pDb.stmtList != null )
        {
          pDb.stmtList.pPrev = pPreStmt;
        }
        pDb.stmtList = pPreStmt;
        if ( pDb.stmtLast == null )
        {
          Debug.Assert( pDb.nStmt == 0 );
          pDb.stmtLast = pPreStmt;
        }
        else
        {
          Debug.Assert( pDb.nStmt > 0 );
        }
        pDb.nStmt++;

        /* If we have too many statement in cache, remove the surplus from 
        ** the end of the cache list.  */
        while ( pDb.nStmt > pDb.maxStmt )
        {
          sqlite3_finalize( pDb.stmtLast.pStmt );
          pDb.stmtLast = pDb.stmtLast.pPrev;
          TCL.Tcl_Free( ref pDb.stmtLast.pNext );
          pDb.stmtLast.pNext = null;
          pDb.nStmt--;
        }
      }
    }

    /*
    ** Structure used with dbEvalXXX() functions:
    **
    **   dbEvalInit()
    **   dbEvalStep()
    **   dbEvalFinalize()
    **   dbEvalRowInfo()
    **   dbEvalColumnValue()
    */
    //typedef struct DbEvalContext DbEvalContext;
    public class DbEvalContext
    {
      public SqliteDb pDb;                   /* Database handle */
      public Tcl_Obj pSql;                   /* Object holding string zSql */
      public string zSql;                    /* Remaining SQL to execute */
      public SqlPreparedStmt pPreStmt;       /* Current statement */
      public int nCol;                       /* Number of columns returned by pStmt */
      public Tcl_Obj pArray;                 /* Name of array variable */
      public Tcl_Obj[] apColName;            /* Array of column names */

      public void Clear()
      {
        pDb = null;
        pSql = null;
        zSql = null;
        pPreStmt = null;
        pArray = null;
        apColName = null;

      }
    };

    /*
    ** Release any cache of column names currently held as part of
    ** the DbEvalContext structure passed as the first argument.
    */
    static void dbReleaseColumnNames( DbEvalContext p )
    {
      if ( p.apColName != null )
      {
        int i;
        for ( i = 0; i < p.nCol; i++ )
        {
          TCL.Tcl_DecrRefCount( ref p.apColName[i] );
        }
        TCL.Tcl_Free( ref p.apColName );
        p.apColName = null;
      }
      p.nCol = 0;
    }

    /*
    ** Initialize a DbEvalContext structure.
    **
    ** If pArray is not NULL, then it contains the name of a Tcl array
    ** variable. The "*" member of this array is set to a list containing
    ** the names of the columns returned by the statement as part of each
    ** call to dbEvalStep(), in order from left to right. e.g. if the names 
    ** of the returned columns are a, b and c, it does the equivalent of the 
    ** tcl command:
    **
    **     set ${pArray}() {a b c}
    */
    static void dbEvalInit(
    DbEvalContext p,               /* Pointer to structure to initialize */
    SqliteDb pDb,                  /* Database handle */
    Tcl_Obj pSql,                  /* Object containing SQL script */
    Tcl_Obj pArray                 /* Name of Tcl array to set () element of */
    )
    {
      if ( p != null )
        p.Clear();// memset( p, 0, sizeof( DbEvalContext ) );
      p.pDb = pDb;
      p.zSql = TCL.Tcl_GetString( pSql );
      p.pSql = pSql;
      TCL.Tcl_IncrRefCount( pSql );
      if ( pArray != null )
      {
        p.pArray = pArray;
        TCL.Tcl_IncrRefCount( pArray );
      }
    }

    /*
    ** Obtain information about the row that the DbEvalContext passed as the
    ** first argument currently points to.
    */
    static void dbEvalRowInfo(
    DbEvalContext p,               /* Evaluation context */
    out int pnCol,                 /* OUT: Number of column names */
    out Tcl_Obj[] papColName       /* OUT: Array of column names */
    )
    {
      /* Compute column names */
      if ( null == p.apColName )
      {
        sqlite3_stmt pStmt = p.pPreStmt.pStmt;
        int i;                        /* Iterator variable */
        int nCol;                     /* Number of columns returned by pStmt */
        Tcl_Obj[] apColName = null;   /* Array of column names */

        p.nCol = nCol = sqlite3_column_count( pStmt );
        if ( nCol > 0 )// && ( papColName != null || p.pArray != null ) )
        {
          apColName = new TclObject[nCol];// (Tcl_Obj*)Tcl_Alloc( sizeof( Tcl_Obj* ) * nCol );
          for ( i = 0; i < nCol; i++ )
          {
            apColName[i] = dbTextToObj( sqlite3_column_name( pStmt, i ) );
            TCL.Tcl_IncrRefCount( apColName[i] );
          }
          p.apColName = apColName;
        }

        /* If results are being stored in an array variable, then create
        ** the array() entry for that array
        */
        if ( p.pArray != null )
        {
          Tcl_Interp interp = p.pDb.interp;
          Tcl_Obj pColList = TCL.Tcl_NewObj();
          Tcl_Obj pStar = TCL.Tcl_NewStringObj( "*", -1 );

          for ( i = 0; i < nCol; i++ )
          {
            TCL.Tcl_ListObjAppendElement( interp, pColList, apColName[i] );
          }
          TCL.Tcl_IncrRefCount( pStar );
          TCL.Tcl_ObjSetVar2( interp, p.pArray, pStar, pColList, 0 );
          TCL.Tcl_DecrRefCount( ref pStar );
        }
      }

      //if ( papColName != null )
      {
        papColName = p.apColName;
      }
      //if ( pnCol !=0) 
      {
        pnCol = p.nCol;
      }
    }

    /*
    ** Return one of TCL_OK, TCL_BREAK or TCL_ERROR. If TCL_ERROR is
    ** returned, then an error message is stored in the interpreter before
    ** returning.
    **
    ** A return value of TCL_OK means there is a row of data available. The
    ** data may be accessed using dbEvalRowInfo() and dbEvalColumnValue(). This
    ** is analogous to a return of SQLITE_ROW from sqlite3_step(). If TCL_BREAK
    ** is returned, then the SQL script has finished executing and there are
    ** no further rows available. This is similar to SQLITE_DONE.
    */
    static int dbEvalStep( DbEvalContext p )
    {
      while ( !String.IsNullOrEmpty( p.zSql ) || p.pPreStmt != null )
      {
        int rc;
        if ( p.pPreStmt == null )
        {
          rc = dbPrepareAndBind( p.pDb, p.zSql, ref p.zSql, ref p.pPreStmt );
          if ( rc != TCL.TCL_OK )
            return rc;
        }
        else
        {
          int rcs;
          SqliteDb pDb = p.pDb;
          SqlPreparedStmt pPreStmt = p.pPreStmt;
          sqlite3_stmt pStmt = pPreStmt.pStmt;

          rcs = sqlite3_step( pStmt );
          if ( rcs == SQLITE_ROW )
          {
            return TCL.TCL_OK;
          }
          if ( p.pArray != null )
          {
            TclObject[] pDummy;
            int iDummy;
            dbEvalRowInfo( p, out iDummy, out pDummy );
          }
          rcs = sqlite3_reset( pStmt );

          pDb.nStep = sqlite3_stmt_status( pStmt, SQLITE_STMTSTATUS_FULLSCAN_STEP, 1 );
          pDb.nSort = sqlite3_stmt_status( pStmt, SQLITE_STMTSTATUS_SORT, 1 );
          pDb.nIndex = sqlite3_stmt_status( pStmt, SQLITE_STMTSTATUS_AUTOINDEX, 1 );
          dbReleaseColumnNames( p );
          p.pPreStmt = null;

          if ( rcs != SQLITE_OK )
          {
            /* If a run-time error occurs, report the error and stop reading
            ** the SQL.  */
            TCL.Tcl_SetObjResult( pDb.interp, dbTextToObj( sqlite3_errmsg( pDb.db ) ) );
            dbReleaseStmt( pDb, pPreStmt, 1 );
            return TCL.TCL_ERROR;
          }
          else
          {
            dbReleaseStmt( pDb, pPreStmt, 0 );
          }
        }
      }

      /* Finished */
      return TCL.TCL_BREAK;
    }

    /*
    ** Free all resources currently held by the DbEvalContext structure passed
    ** as the first argument. There should be exactly one call to this function
    ** for each call to dbEvalInit().
    */
    static void dbEvalFinalize( DbEvalContext p )
    {
      if ( p.pPreStmt != null )
      {
        sqlite3_reset( p.pPreStmt.pStmt );
        dbReleaseStmt( p.pDb, p.pPreStmt, 1 );
        p.pPreStmt = null;
      }
      if ( p.pArray != null )
      {
        TCL.Tcl_DecrRefCount( ref p.pArray );
        p.pArray = null;
      }
      TCL.Tcl_DecrRefCount( ref p.pSql );
      dbReleaseColumnNames( p );
    }

    /*
    ** Return a pointer to a Tcl_Obj structure with ref-count 0 that contains
    ** the value for the iCol'th column of the row currently pointed to by
    ** the DbEvalContext structure passed as the first argument.
    */
    static Tcl_Obj dbEvalColumnValue( DbEvalContext p, int iCol )
    {
      sqlite3_stmt pStmt = p.pPreStmt.pStmt;
      switch ( sqlite3_column_type( pStmt, iCol ) )
      {
        case SQLITE_BLOB:
          {
            int bytes = sqlite3_column_bytes( pStmt, iCol );
            byte[] zBlob = sqlite3_column_blob( pStmt, iCol );
            if ( null == zBlob )
              bytes = 0;
            return TCL.Tcl_NewByteArrayObj( zBlob, bytes );
          }
        case SQLITE_INTEGER:
          {
            sqlite_int64 v = sqlite3_column_int64( pStmt, iCol );
            if ( v >= -2147483647 && v <= 2147483647 )
            {
              return TCL.Tcl_NewIntObj( (int)v );
            }
            else
            {
              return TCL.Tcl_NewWideIntObj( v );
            }
          }
        case SQLITE_FLOAT:
          {
            return TCL.Tcl_NewDoubleObj( sqlite3_column_double( pStmt, iCol ) );
          }
        case SQLITE_NULL:
          {
            return dbTextToObj( p.pDb.zNull );
          }
      }

      return dbTextToObj( sqlite3_column_text( pStmt, iCol ) );
    }

    /*
    ** If using Tcl version 8.6 or greater, use the NR functions to avoid
    ** recursive evalution of scripts by the [db eval] and [db trans]
    ** commands. Even if the headers used while compiling the extension
    ** are 8.6 or newer, the code still tests the Tcl version at runtime.
    ** This allows stubs-enabled builds to be used with older Tcl libraries.
    */
#if TCL_MAJOR_VERSION//>8 || (TCL_MAJOR_VERSION==8 && TCL_MINOR_VERSION>=6)
//# define SQLITE_TCL_NRE 1
static int DbUseNre(void){
int major, minor;
Tcl_GetVersion(&major, &minor, 0, 0);
return( (major==8 && minor>=6) || major>8 );
}
#else
    /* 
** Compiling using headers earlier than 8.6. In this case NR cannot be
** used, so DbUseNre() to always return zero. Add #defines for the other
** Tcl_NRxxx() functions to prevent them from causing compilation errors,
** even though the only invocations of them are within conditional blocks 
** of the form:
**
**   if( DbUseNre() ) { ... }
*/
    const int SQLITE_TCL_NRE = 0;                         //# define SQLITE_TCL_NRE 0
    static bool DbUseNre()
    {
      return false;
    }                     //# define DbUseNre() 0
    //# define Tcl_NRAddCallback(a,b,c,d,e,f) 0
    //# define Tcl_NREvalObj(a,b,c) 0
    //# define Tcl_NRCreateCommand(a,b,c,d,e,f) 0
#endif

    /*
** This function is part of the implementation of the command:
**
**   $db eval SQL ?ARRAYNAME? SCRIPT
*/
    static int DbEvalNextCmd(
    object[] data,                   /* data[0] is the (DbEvalContext) */
    Tcl_Interp interp,           /* Tcl interpreter */
    int result                       /* Result so far */
    )
    {
      int rc = result;                     /* Return code */

      /* The first element of the data[] array is a pointer to a DbEvalContext
      ** structure allocated using TCL.Tcl_Alloc(). The second element of data[]
      ** is a pointer to a TCL.Tcl_Obj containing the script to run for each row
      ** returned by the queries encapsulated in data[0]. */
      DbEvalContext p = (DbEvalContext)data[0];
      Tcl_Obj pScript = (Tcl_Obj)data[1];
      Tcl_Obj pArray = p.pArray;

      while ( ( rc == TCL.TCL_OK || rc == TCL.TCL_CONTINUE ) && TCL.TCL_OK == ( rc = dbEvalStep( p ) ) )
      {
        int i;
        int nCol;
        Tcl_Obj[] apColName;
        dbEvalRowInfo( p, out nCol, out apColName );
        for ( i = 0; i < nCol; i++ )
        {
          Tcl_Obj pVal = dbEvalColumnValue( p, i );
          if ( pArray == null )
          {
            TCL.Tcl_ObjSetVar2( interp, apColName[i], null, pVal, 0 );
          }
          else
          {
            TCL.Tcl_ObjSetVar2( interp, pArray, apColName[i], pVal, 0 );
          }
        }

        /* The required interpreter variables are now populated with the data 
        ** from the current row. If using NRE, schedule callbacks to evaluate
        ** script pScript, then to invoke this function again to fetch the next
        ** row (or clean up if there is no next row or the script throws an
        ** exception). After scheduling the callbacks, return control to the 
        ** caller.
        **
        ** If not using NRE, evaluate pScript directly and continue with the
        ** next iteration of this while(...) loop.  */
        if ( DbUseNre() )
        {
          Debugger.Break();
          //TCL.Tcl_NRAddCallback(interp, DbEvalNextCmd, (void)p, (void)pScript, 0, 0);
          //return TCL.Tcl_NREvalObj(interp, pScript, 0);
        }
        else
        {
          rc = TCL.Tcl_EvalObjEx( interp, pScript, 0 );
        }
      }

      TCL.Tcl_DecrRefCount( ref pScript );
      dbEvalFinalize( p );
      TCL.Tcl_Free( ref p );

      if ( rc == TCL.TCL_OK || rc == TCL.TCL_BREAK )
      {
        TCL.Tcl_ResetResult( interp );
        rc = TCL.TCL_OK;
      }
      return rc;
    }

    /*
    ** The "sqlite" command below creates a new Tcl command for each
    ** connection it opens to an SQLite database.  This routine is invoked
    ** whenever one of those connection-specific commands is executed
    ** in Tcl.  For example, if you run Tcl code like this:
    **
    **       sqlite3 db1  "my_database"
    **       db1 close
    **
    ** The first command opens a connection to the "my_database" database
    ** and calls that connection "db1".  The second command causes this
    ** subroutine to be invoked.
    */
    enum DB_enum
    {
      DB_AUTHORIZER,
      DB_BACKUP,
      DB_BUSY,
      DB_CACHE,
      DB_CHANGES,
      DB_CLOSE,
      DB_COLLATE,
      DB_COLLATION_NEEDED,
      DB_COMMIT_HOOK,
      DB_COMPLETE,
      DB_COPY,
      DB_ENABLE_LOAD_EXTENSION,
      DB_ERRORCODE,
      DB_EVAL,
      DB_EXISTS,
      DB_FUNCTION,
      DB_INCRBLOB,
      DB_INTERRUPT,
      DB_LAST_INSERT_ROWID,
      DB_NULLVALUE,
      DB_ONECOLUMN,
      DB_PROFILE,
      DB_PROGRESS,
      DB_REKEY,
      DB_RESTORE,
      DB_ROLLBACK_HOOK,
      DB_STATUS,
      DB_TIMEOUT,
      DB_TOTAL_CHANGES,
      DB_TRACE,
      DB_TRANSACTION,
      DB_UNLOCK_NOTIFY,
      DB_UPDATE_HOOK,
      DB_VERSION,
      DB_WAL_HOOK
    };

    enum TTYPE_enum
    {
      TTYPE_DEFERRED,
      TTYPE_EXCLUSIVE,
      TTYPE_IMMEDIATE
    };

    static int DbObjCmd( object cd, Tcl_Interp interp, int objc, Tcl_Obj[] objv )
    {
      SqliteDb pDb = (SqliteDb)cd;
      int choice = 0;
      int rc = TCL.TCL_OK;
      string[] DB_strs = {
"authorizer",         "backup",            "busy",
"cache",              "changes",           "close",
"collate",            "collation_needed",  "commit_hook",
"complete",           "copy",              "enable_load_extension",
"errorcode",          "eval",              "exists",
"function",           "incrblob",          "interrupt",
"last_insert_rowid",  "nullvalue",         "onecolumn",
"profile",            "progress",          "rekey",
"restore",            "rollback_hook",     "status",
"timeout",            "total_changes",     "trace",
"transaction",        "unlock_notify",     "update_hook",
"version",            "wal_hook"
};

      /* don't leave trailing commas on DB_enum, it confuses the AIX xlc compiler */
      if ( objc < 2 )
      {
        TCL.Tcl_WrongNumArgs( interp, 1, objv, "SUBCOMMAND ..." );
        return TCL.TCL_ERROR;
      }
      if ( TCL.Tcl_GetIndexFromObj( interp, objv[1], DB_strs, "option", 0, out choice ) )
      {
        return TCL.TCL_ERROR;
      }

      switch ( choice )
      {

        /*    $db authorizer ?CALLBACK?
        **
        ** Invoke the given callback to authorize each SQL operation as it is
        ** compiled.  5 arguments are appended to the callback before it is
        ** invoked:
        **
        **   (1) The authorization type (ex: SQLITE_CREATE_TABLE, SQLITE_INSERT, ...)
        **   (2) First descriptive name (depends on authorization type)
        **   (3) Second descriptive name
        **   (4) Name of the database (ex: "main", "temp")
        **   (5) Name of trigger that is doing the access
        **
        ** The callback should return on of the following strings: SQLITE_OK,
        ** SQLITE_IGNORE, or SQLITE_DENY.  Any other return value is an error.
        **
        ** If this method is invoked with no arguments, the current authorization
        ** callback string is returned.
        */
        case (int)DB_enum.DB_AUTHORIZER:
          {
#if SQLITE_OMIT_AUTHORIZATION
            TCL.Tcl_AppendResult( interp, "authorization not available in this build" );
            return TCL.TCL_RETURN;
#else
if( objc>3 ){
TCL.Tcl_WrongNumArgs(interp, 2, objv, "?CALLBACK?");
return TCL.TCL_ERROR;
}else if( objc==2 ){
if( pDb.zAuth ){
TCL.Tcl_AppendResult(interp, pDb.zAuth);
}
}else{
string zAuth;
int len;
if( pDb.zAuth ){
TCL.Tcl_Free(pDb.zAuth);
}
zAuth = TCL.Tcl_GetStringFromObj(objv[2], len);
if( zAuth && len>0 ){
pDb.zAuth = TCL.Tcl_Alloc( len + 1 );
memcpy(pDb.zAuth, zAuth, len+1);
}else{
pDb.zAuth = 0;
}
if( pDb.zAuth ){
pDb.interp = interp;
sqlite3_set_authorizer(pDb.db, auth_callback, pDb);
}else{
sqlite3_set_authorizer(pDb.db, 0, 0);
}
}
break;
#endif
          }

        /*    $db backup ?DATABASE? FILENAME
        **
        ** Open or create a database file named FILENAME.  Transfer the
        ** content of local database DATABASE (default: "main") into the
        ** FILENAME database.
        */
        case (int)DB_enum.DB_BACKUP:
          {
            string zDestFile;
            string zSrcDb;
            sqlite3 pDest = null;
            sqlite3_backup pBackup;

            if ( objc == 3 )
            {
              zSrcDb = "main";
              zDestFile = TCL.Tcl_GetString( objv[2] );
            }
            else if ( objc == 4 )
            {
              zSrcDb = TCL.Tcl_GetString( objv[2] );
              zDestFile = TCL.Tcl_GetString( objv[3] );
            }
            else
            {
              TCL.Tcl_WrongNumArgs( interp, 2, objv, "?DATABASE? FILENAME" );
              return TCL.TCL_ERROR;
            }
            rc = sqlite3_open( zDestFile, out pDest );
            if ( rc != SQLITE_OK )
            {
              TCL.Tcl_AppendResult( interp, "cannot open target database: ",
              sqlite3_errmsg( pDest ) );
              sqlite3_close( pDest );
              return TCL.TCL_ERROR;
            }
            pBackup = sqlite3_backup_init( pDest, "main", pDb.db, zSrcDb );
            if ( pBackup == null )
            {
              TCL.Tcl_AppendResult( interp, "backup failed: ",
              sqlite3_errmsg( pDest ) );
              sqlite3_close( pDest );
              return TCL.TCL_ERROR;
            }
#if SQLITE_HAS_CODEC
            if ( pBackup.pSrc.pBt.pPager.pCodec != null )
            {
              pBackup.pDest.pBt.pPager.xCodec = pBackup.pSrc.pBt.pPager.xCodec;
              pBackup.pDest.pBt.pPager.xCodecFree = pBackup.pSrc.pBt.pPager.xCodecFree;
              pBackup.pDest.pBt.pPager.xCodecSizeChng = pBackup.pSrc.pBt.pPager.xCodecSizeChng;
              pBackup.pDest.pBt.pPager.pCodec = pBackup.pSrc.pBt.pPager.pCodec.Copy();
            }
#endif
            while ( ( rc = sqlite3_backup_step( pBackup, 100 ) ) == SQLITE_OK )
            {
            }
            sqlite3_backup_finish( pBackup );
            if ( rc == SQLITE_DONE )
            {
              rc = TCL.TCL_OK;
            }
            else
            {
              TCL.Tcl_AppendResult( interp, "backup failed: ",
              sqlite3_errmsg( pDest ) );
              rc = TCL.TCL_ERROR;
            }
            sqlite3_close( pDest );
            break;
          }

        //  /*    $db busy ?CALLBACK?
        //  **
        //  ** Invoke the given callback if an SQL statement attempts to open
        //  ** a locked database file.
        //  */
        case (int)DB_enum.DB_BUSY:
          {
            if ( objc > 3 )
            {
              TCL.Tcl_WrongNumArgs( interp, 2, objv, "CALLBACK" );
              return TCL.TCL_ERROR;
            }
            else if ( objc == 2 )
            {
              if ( pDb.zBusy != null )
              {
                TCL.Tcl_AppendResult( interp, pDb.zBusy );
              }
            }
            else
            {
              string zBusy;
              int len = 0;
              if ( pDb.zBusy != null )
              {
                TCL.Tcl_Free( ref pDb.zBusy );
              }
              zBusy = TCL.Tcl_GetStringFromObj( objv[2], out len );
              if ( zBusy != null && len > 0 )
              {
                //pDb.zBusy = TCL.Tcl_Alloc( len + 1 );
                pDb.zBusy = zBusy;// memcpy( pDb.zBusy, zBusy, len + 1 );
              }
              else
              {
                pDb.zBusy = null;
              }
              if ( pDb.zBusy != null )
              {
                pDb.interp = interp;
                sqlite3_busy_handler( pDb.db, (dxBusy)DbBusyHandler, pDb );
              }
              else
              {
                sqlite3_busy_handler( pDb.db, null, null );
              }
            }
            break;
          }

        //  /*     $db cache flush
        //  **     $db cache size n
        //  **
        //  ** Flush the prepared statement cache, or set the maximum number of
        //  ** cached statements.
        //  */
        case (int)DB_enum.DB_CACHE:
          {
            string subCmd;
            int n = 0;

            if ( objc <= 2 )
            {
              TCL.Tcl_WrongNumArgs( interp, 1, objv, "cache option ?arg?" );
              return TCL.TCL_ERROR;
            }
            subCmd = TCL.Tcl_GetStringFromObj( objv[2], 0 );
            if ( subCmd == "flush" )
            {
              if ( objc != 3 )
              {
                TCL.Tcl_WrongNumArgs( interp, 2, objv, "flush" );
                return TCL.TCL_ERROR;
              }
              else
              {
                flushStmtCache( pDb );
              }
            }
            else if ( subCmd == "size" )
            {
              if ( objc != 4 )
              {
                TCL.Tcl_WrongNumArgs( interp, 2, objv, "size n" );
                return TCL.TCL_ERROR;
              }
              else
              {
                if ( TCL.TCL_ERROR == ( TCL.Tcl_GetIntFromObj( interp, objv[3], out n ) != TCL.TCL_OK ? TCL.TCL_ERROR : TCL.TCL_OK ) )
                {
                  TCL.Tcl_AppendResult( interp, "cannot convert \"",
                   TCL.Tcl_GetStringFromObj( objv[3], 0 ), "\" to integer", 0 );
                  return TCL.TCL_ERROR;
                }
                else
                {
                  if ( n < 0 )
                  {
                    flushStmtCache( pDb );
                    n = 0;
                  }
                  else if ( n > MAX_PREPARED_STMTS )
                  {
                    n = MAX_PREPARED_STMTS;
                  }
                  pDb.maxStmt = n;
                }
              }
            }
            else
            {
              TCL.Tcl_AppendResult( interp, "bad option \"",
              TCL.Tcl_GetStringFromObj( objv[2], 0 ), "\": must be flush or size", null );
              return TCL.TCL_ERROR;
            }
            break;
          }

        /*     $db changes
        **
        ** Return the number of rows that were modified, inserted, or deleted by
        ** the most recent INSERT, UPDATE or DELETE statement, not including
        ** any changes made by trigger programs.
        */
        case (int)DB_enum.DB_CHANGES:
          {
            Tcl_Obj pResult;
            if ( objc != 2 )
            {
              TCL.Tcl_WrongNumArgs( interp, 2, objv, "" );
              return TCL.TCL_ERROR;
            }
            pResult = TCL.Tcl_GetObjResult( interp );
            TCL.Tcl_SetResult( interp, sqlite3_changes( pDb.db ).ToString(), 0 );
            break;
          }

        /*    $db close
        **
        ** Shutdown the database
        */
        case (int)DB_enum.DB_CLOSE:
          {
            TCL.Tcl_DeleteCommand( interp, TCL.Tcl_GetStringFromObj( objv[0], 0 ) );
            break;
          }

        /*
        **     $db collate NAME SCRIPT
        **
        ** Create a new SQL collation function called NAME.  Whenever
        ** that function is called, invoke SCRIPT to evaluate the function.
        */
        case (int)DB_enum.DB_COLLATE:
          {
            SqlCollate pCollate;
            string zName;
            string zScript;
            int nScript = 0;
            if ( objc != 4 )
            {
              TCL.Tcl_WrongNumArgs( interp, 2, objv, "NAME SCRIPT" );
              return TCL.TCL_ERROR;
            }
            zName = TCL.Tcl_GetStringFromObj( objv[2], 0 );
            zScript = TCL.Tcl_GetStringFromObj( objv[3], nScript );
            pCollate = new SqlCollate();//(SqlCollate)Tcl_Alloc( sizeof(*pCollate) + nScript + 1 );
            //if ( pCollate == null ) return TCL.TCL_ERROR;
            pCollate.interp = interp;
            pCollate.pNext = pDb.pCollate;
            pCollate.zScript = zScript; // pCollate[1];
            pDb.pCollate = pCollate;
            //memcpy( pCollate.zScript, zScript, nScript + 1 );
            if ( sqlite3_create_collation( pDb.db, zName, SQLITE_UTF8,
            pCollate, (dxCompare)tclSqlCollate ) != 0 )
            {
              TCL.Tcl_SetResult( interp, sqlite3_errmsg( pDb.db ), TCL.TCL_VOLATILE );
              return TCL.TCL_ERROR;
            }
            break;
          }

        /*
        **     $db collation_needed SCRIPT
        **
        ** Create a new SQL collation function called NAME.  Whenever
        ** that function is called, invoke SCRIPT to evaluate the function.
        */
        case (int)DB_enum.DB_COLLATION_NEEDED:
          {
            if ( objc != 3 )
            {
              TCL.Tcl_WrongNumArgs( interp, 2, objv, "SCRIPT" );
              return TCL.TCL_ERROR;
            }
            if ( pDb.pCollateNeeded != null )
            {
              TCL.Tcl_DecrRefCount( ref pDb.pCollateNeeded );
            }
            pDb.pCollateNeeded = TCL.Tcl_DuplicateObj( objv[2] );
            TCL.Tcl_IncrRefCount( pDb.pCollateNeeded );
            sqlite3_collation_needed( pDb.db, (object)pDb, (dxCollNeeded)tclCollateNeeded );
            break;
          }

        /*    $db commit_hook ?CALLBACK?
        **
        ** Invoke the given callback just before committing every SQL transaction.
        ** If the callback throws an exception or returns non-zero, then the
        ** transaction is aborted.  If CALLBACK is an empty string, the callback
        ** is disabled.
        */
        case (int)DB_enum.DB_COMMIT_HOOK:
          {
            if ( objc > 3 )
            {
              TCL.Tcl_WrongNumArgs( interp, 2, objv, "?CALLBACK?" );
              return TCL.TCL_ERROR;
            }
            else if ( objc == 2 )
            {
              if ( pDb.zCommit != null )
              {
                TCL.Tcl_AppendResult( interp, pDb.zCommit );
              }
            }
            else
            {
              string zCommit;
              int len = 0;
              if ( pDb.zCommit != null )
              {
                TCL.Tcl_Free( ref pDb.zCommit );
              }
              zCommit = TCL.Tcl_GetStringFromObj( objv[2], out len );
              if ( zCommit != null && len > 0 )
              {
                pDb.zCommit = zCommit;// TCL.Tcl_Alloc( len + 1 );
                //memcpy( pDb.zCommit, zCommit, len + 1 );
              }
              else
              {
                pDb.zCommit = null;
              }
              if ( pDb.zCommit != null )
              {
                pDb.interp = interp;
                sqlite3_commit_hook( pDb.db, DbCommitHandler, pDb );
              }
              else
              {
                sqlite3_commit_hook( pDb.db, null, null );
              }
            }
            break;
          }

        /*    $db complete SQL
        **
        ** Return TRUE if SQL is a complete SQL statement.  Return FALSE if
        ** additional lines of input are needed.  This is similar to the
        ** built-in "info complete" command of Tcl.
        */
        case (int)DB_enum.DB_COMPLETE:
          {
#if !SQLITE_OMIT_COMPLETE
            Tcl_Obj pResult;
            int isComplete;
            if ( objc != 3 )
            {
              TCL.Tcl_WrongNumArgs( interp, 2, objv, "SQL" );
              return TCL.TCL_ERROR;
            }
            isComplete = sqlite3_complete( TCL.Tcl_GetStringFromObj( objv[2], 0 ) );
            pResult = TCL.Tcl_GetObjResult( interp );
            TCL.Tcl_SetBooleanObj( pResult, isComplete );
#endif
            break;
          }

        /*    $db copy conflict-algorithm table filename ?SEPARATOR? ?NULLINDICATOR?
        **
        ** Copy data into table from filename, optionally using SEPARATOR
        ** as column separators.  If a column contains a null string, or the
        ** value of NULLINDICATOR, a NULL is inserted for the column.
        ** conflict-algorithm is one of the sqlite conflict algorithms:
        **    rollback, abort, fail, ignore, replace
        ** On success, return the number of lines processed, not necessarily same
        ** as 'db changes' due to conflict-algorithm selected.
        **
        ** This code is basically an implementation/enhancement of
        ** the sqlite3 shell.c ".import" command.
        **
        ** This command usage is equivalent to the sqlite2.x COPY statement,
        ** which imports file data into a table using the PostgreSQL COPY file format:
        **   $db copy $conflit_algo $table_name $filename \t \\N
        */
        case (int)DB_enum.DB_COPY:
          {
            string zTable;              /* Insert data into this table */
            string zFile;               /* The file from which to extract data */
            string zConflict;           /* The conflict algorithm to use */
            sqlite3_stmt pStmt = null;  /* A statement */
            int nCol;                   /* Number of columns in the table */
            int nByte;                  /* Number of bytes in an SQL string */
            int i, j;                   /* Loop counters */
            int nSep;                   /* Number of bytes in zSep[] */
            int nNull;                  /* Number of bytes in zNull[] */
            StringBuilder zSql = new StringBuilder( 200 );         /* An SQL statement */
            string zLine;               /* A single line of input from the file */
            string[] azCol;             /* zLine[] broken up into columns */
            string zCommit;             /* How to commit changes */
            TextReader _in;             /* The input file */
            int lineno = 0;             /* Line number of input file */
            StringBuilder zLineNum = new StringBuilder( 80 ); /* Line number print buffer */
            Tcl_Obj pResult;            /* interp result */

            string zSep;
            string zNull;
            if ( objc < 5 || objc > 7 )
            {
              TCL.Tcl_WrongNumArgs( interp, 2, objv,
              "CONFLICT-ALGORITHM TABLE FILENAME ?SEPARATOR? ?NULLINDICATOR?" );
              return TCL.TCL_ERROR;
            }
            if ( objc >= 6 )
            {
              zSep = TCL.Tcl_GetStringFromObj( objv[5], 0 );
            }
            else
            {
              zSep = "\t";
            }
            if ( objc >= 7 )
            {
              zNull = TCL.Tcl_GetStringFromObj( objv[6], 0 );
            }
            else
            {
              zNull = "";
            }
            zConflict = TCL.Tcl_GetStringFromObj( objv[2], 0 );
            zTable = TCL.Tcl_GetStringFromObj( objv[3], 0 );
            zFile = TCL.Tcl_GetStringFromObj( objv[4], 0 );
            nSep = strlen30( zSep );
            nNull = strlen30( zNull );
            if ( nSep == 0 )
            {
              TCL.Tcl_AppendResult( interp, "Error: non-null separator required for copy" );
              return TCL.TCL_ERROR;
            }
            if ( zConflict != "rollback" &&
            zConflict != "abort" &&
            zConflict != "fail" &&
            zConflict != "ignore" &&
            zConflict != "replace" )
            {
              TCL.Tcl_AppendResult( interp, "Error: \"", zConflict,
              "\", conflict-algorithm must be one of: rollback, " +
              "abort, fail, ignore, or replace", 0 );
              return TCL.TCL_ERROR;
            }
            zSql.Append( sqlite3_mprintf( "SELECT * FROM '%q'", zTable ) );
            if ( zSql == null )
            {
              TCL.Tcl_AppendResult( interp, "Error: no such table: ", zTable );
              return TCL.TCL_ERROR;
            }
            nByte = strlen30( zSql );
            rc = sqlite3_prepare( pDb.db, zSql.ToString(), -1, ref pStmt, 0 );
            sqlite3DbFree( null, ref zSql );
            if ( rc != 0 )
            {
              TCL.Tcl_AppendResult( interp, "Error: ", sqlite3_errmsg( pDb.db ) );
              nCol = 0;
            }
            else
            {
              nCol = sqlite3_column_count( pStmt );
            }
            sqlite3_finalize( pStmt );
            if ( nCol == 0 )
            {
              return TCL.TCL_ERROR;
            }
            //zSql.Append(malloc( nByte + 50 + nCol*2 );
            //if( zSql==0 ) {
            //  TCL.Tcl_AppendResult(interp, "Error: can't malloc()");
            //  return TCL.TCL_ERROR;
            //}
            sqlite3_snprintf( nByte + 50, zSql, "INSERT OR %q INTO '%q' VALUES(?",
            zConflict, zTable );
            j = strlen30( zSql );
            for ( i = 1; i < nCol; i++ )
            {
              //zSql+=[j++] = ',';
              //zSql[j++] = '?';
              zSql.Append( ",?" );
            }
            //zSql[j++] = ')';
            //zSql[j] = "";
            zSql.Append( ")" );
            rc = sqlite3_prepare( pDb.db, zSql.ToString(), -1, ref pStmt, 0 );
            //free(zSql);
            if ( rc != 0 )
            {
              TCL.Tcl_AppendResult( interp, "Error: ", sqlite3_errmsg( pDb.db ) );
              sqlite3_finalize( pStmt );
              return TCL.TCL_ERROR;
            }
            _in = new StreamReader( zFile );//fopen(zFile, "rb");
            if ( _in == null )
            {
              TCL.Tcl_AppendResult( interp, "Error: cannot open file: ", zFile );
              sqlite3_finalize( pStmt );
              return TCL.TCL_ERROR;
            }
            azCol = new string[nCol + 1];//malloc( sizeof(azCol[0])*(nCol+1) );
            if ( azCol == null )
            {
              TCL.Tcl_AppendResult( interp, "Error: can't malloc()" );
              _in.Close();//fclose(_in);
              return TCL.TCL_ERROR;
            }
            sqlite3_exec( pDb.db, "BEGIN", 0, 0, 0 );
            zCommit = "COMMIT";
            while ( ( zLine = _in.ReadLine() ) != null )//local_getline(0, _in))!=0 )
            {
              string z;
              i = 0;
              lineno++;
              azCol = zLine.Split( zSep[0] );
              //for(i=0, z=zLine; *z; z++){
              //  if( *z==zSep[0] && strncmp(z, zSep, nSep)==0 ){
              //    *z = 0;
              //    i++;
              //    if( i<nCol ){
              //      azCol[i] = z[nSep];
              //      z += nSep-1;
              //    }
              //  }
              //}
              if ( azCol.Length != nCol )
              {
                StringBuilder zErr = new StringBuilder( 200 );
                int nErr = strlen30( zFile ) + 200;
                //zErr = malloc(nErr);
                //if( zErr ){
                sqlite3_snprintf( nErr, zErr,
                "Error: %s line %d: expected %d columns of data but found %d",
                zFile, lineno, nCol, i + 1 );
                TCL.Tcl_AppendResult( interp, zErr );
                //  free(zErr);
                //}
                zCommit = "ROLLBACK";
                break;
              }
              for ( i = 0; i < nCol; i++ )
              {
                /* check for null data, if so, bind as null */
                if ( ( nNull > 0 && azCol[i] == zNull )
                || strlen30( azCol[i] ) == 0
                )
                {
                  sqlite3_bind_null( pStmt, i + 1 );
                }
                else
                {
                  sqlite3_bind_text( pStmt, i + 1, azCol[i], -1, SQLITE_STATIC );
                }
              }
              sqlite3_step( pStmt );
              rc = sqlite3_reset( pStmt );
              //free(zLine);
              if ( rc != SQLITE_OK )
              {
                TCL.Tcl_AppendResult( interp, "Error: ", sqlite3_errmsg( pDb.db ) );
                zCommit = "ROLLBACK";
                break;
              }
            }
            //free(azCol);
            _in.Close();// fclose( _in );
            sqlite3_finalize( pStmt );
            sqlite3_exec( pDb.db, zCommit, 0, 0, 0 );

            if ( zCommit[0] == 'C' )
            {
              /* success, set result as number of lines processed */
              pResult = TCL.Tcl_GetObjResult( interp );
              TCL.Tcl_SetIntObj( pResult, lineno );
              rc = TCL.TCL_OK;
            }
            else
            {
              /* failure, append lineno where failed */
              sqlite3_snprintf( 80, zLineNum, "%d", lineno );
              TCL.Tcl_AppendResult( interp, ", failed while processing line: ", zLineNum );
              rc = TCL.TCL_ERROR;
            }
            break;
          }

        /*
        **    $db enable_load_extension BOOLEAN
        **
        ** Turn the extension loading feature on or off.  It if off by
        ** default.
        */
        case (int)DB_enum.DB_ENABLE_LOAD_EXTENSION:
          {
#if !SQLITE_OMIT_LOAD_EXTENSION
            bool onoff = false;
            if ( objc != 3 )
            {
              TCL.Tcl_WrongNumArgs( interp, 2, objv, "BOOLEAN" );
              return TCL.TCL_ERROR;
            }
            if ( TCL.Tcl_GetBooleanFromObj( interp, objv[2], out onoff ) )
            {
              return TCL.TCL_ERROR;
            }
            sqlite3_enable_load_extension( pDb.db, onoff ? 1 : 0 );
            break;
#else
TCL.Tcl_AppendResult(interp, "extension loading is turned off at compile-time",
   0);
return TCL.TCL_ERROR;
#endif
          }

        /*
        **    $db errorcode
        **
        ** Return the numeric error code that was returned by the most recent
        ** call to sqlite3_exec().
        */
        case (int)DB_enum.DB_ERRORCODE:
          {
            TCL.Tcl_SetObjResult( interp, TCL.Tcl_NewIntObj( sqlite3_errcode( pDb.db ) ) );
            break;
          }

        /*
        **    $db exists $sql
        **    $db onecolumn $sql
        **
        ** The onecolumn method is the equivalent of:
        **     lindex [$db eval $sql] 0
        */
        case (int)DB_enum.DB_EXISTS:
        case (int)DB_enum.DB_ONECOLUMN:
          {
            DbEvalContext sEval = new DbEvalContext();
            ;
            if ( objc != 3 )
            {
              TCL.Tcl_WrongNumArgs( interp, 2, objv, "SQL" );
              return TCL.TCL_ERROR;
            }
            dbEvalInit( sEval, pDb, objv[2], null );
            rc = dbEvalStep( sEval );
            if ( choice == (int)DB_enum.DB_ONECOLUMN )
            {
              if ( rc == TCL.TCL_OK )
              {
                TCL.Tcl_SetObjResult( interp, dbEvalColumnValue( sEval, 0 ) );
              }
            }
            else if ( rc == TCL.TCL_BREAK || rc == TCL.TCL_OK )
            {
              TCL.Tcl_SetObjResult( interp, TCL.Tcl_NewBooleanObj( ( rc == TCL.TCL_OK ? 1 : 0 ) ) );
            }
            dbEvalFinalize( sEval );

            if ( rc == TCL.TCL_BREAK )
            {
              rc = TCL.TCL_OK;
            }
            break;
          }

        /*
        **    $db eval $sql ?array? ?{  ...code... }?
        **
        ** The SQL statement in $sql is evaluated.  For each row, the values are
        ** placed in elements of the array named "array" and ...code... is executed.
        ** If "array" and "code" are omitted, then no callback is every invoked.
        ** If "array" is an empty string, then the values are placed in variables
        ** that have the same name as the fields extracted by the query.
        */
        case (int)DB_enum.DB_EVAL:
          {
            if ( objc < 3 || objc > 5 )
            {
              TCL.Tcl_WrongNumArgs( interp, 2, objv, "SQL ?ARRAY-NAME? ?SCRIPT?" );
              return TCL.TCL_ERROR;
            }

            if ( objc == 3 )
            {
              DbEvalContext sEval = new DbEvalContext();
              Tcl_Obj pRet = TCL.Tcl_NewObj();
              TCL.Tcl_IncrRefCount( pRet );
              dbEvalInit( sEval, pDb, objv[2], null );
              //Console.WriteLine( objv[2].ToString() );
              while ( TCL.TCL_OK == ( rc = dbEvalStep( sEval ) ) )
              {
                int i;
                int nCol;
                TclObject[] pDummy;
                dbEvalRowInfo( sEval, out nCol, out pDummy );
                for ( i = 0; i < nCol; i++ )
                {
                  TCL.Tcl_ListObjAppendElement( interp, pRet, dbEvalColumnValue( sEval, i ) );
                }
              }
              dbEvalFinalize( sEval );
              if ( rc == TCL.TCL_BREAK )
              {
                TCL.Tcl_SetObjResult( interp, pRet );
                rc = TCL.TCL_OK;
              }
              TCL.Tcl_DecrRefCount( ref pRet );
            }
            else
            {
              cd = new object[2];
              DbEvalContext p;
              Tcl_Obj pArray = null;
              Tcl_Obj pScript;

              if ( objc == 5 && !String.IsNullOrEmpty( TCL.Tcl_GetString( objv[3] ) ) )
              {
                pArray = objv[3];
              }
              pScript = objv[objc - 1];
              TCL.Tcl_IncrRefCount( pScript );

              p = new DbEvalContext();// (DbEvalContext)Tcl_Alloc( sizeof( DbEvalContext ) );
              dbEvalInit( p, pDb, objv[2], pArray );

              ( (object[])cd )[0] = p;
              ( (object[])cd )[1] = pScript;
              rc = DbEvalNextCmd( (object[])cd, interp, TCL.TCL_OK );
            }
            break;
          }

        /*
        **     $db function NAME [-argcount N] SCRIPT
        **
        ** Create a new SQL function called NAME.  Whenever that function is
        ** called, invoke SCRIPT to evaluate the function.
        */
        case (int)DB_enum.DB_FUNCTION:
          {
            SqlFunc pFunc;
            Tcl_Obj pScript;
            string zName;
            int nArg = -1;
            if ( objc == 6 )
            {
              string z = TCL.Tcl_GetString( objv[3] );
              int n = strlen30( z );
              if ( n > 2 && z.StartsWith( "-argcount" ) )//strncmp( z, "-argcount", n ) == 0 )
              {
                if ( TCL.TCL_OK != TCL.Tcl_GetIntFromObj( interp, objv[4], out nArg ) )
                  return TCL.TCL_ERROR;
                if ( nArg < 0 )
                {
                  TCL.Tcl_AppendResult( interp, "number of arguments must be non-negative" );
                  return TCL.TCL_ERROR;
                }
              }
              pScript = objv[5];
            }
            else if ( objc != 4 )
            {
              TCL.Tcl_WrongNumArgs( interp, 2, objv, "NAME [-argcount N] SCRIPT" );
              return TCL.TCL_ERROR;
            }
            else
            {
              pScript = objv[3];
            }
            zName = TCL.Tcl_GetStringFromObj( objv[2], 0 );
            pFunc = findSqlFunc( pDb, zName );
            if ( pFunc == null )
              return TCL.TCL_ERROR;
            if ( pFunc.pScript != null )
            {
              TCL.Tcl_DecrRefCount( ref pFunc.pScript );
            }
            pFunc.pScript = pScript;
            TCL.Tcl_IncrRefCount( pScript );
            pFunc.useEvalObjv = safeToUseEvalObjv( interp, pScript );
            rc = sqlite3_create_function( pDb.db, zName, nArg, SQLITE_UTF8,
            pFunc, tclSqlFunc, null, null );
            if ( rc != SQLITE_OK )
            {
              rc = TCL.TCL_ERROR;
              TCL.Tcl_SetResult( interp, sqlite3_errmsg( pDb.db ), TCL.TCL_VOLATILE );
            }
            break;
          }

        /*
        **     $db incrblob ?-readonly? ?DB? TABLE COLUMN ROWID
        */
        case (int)DB_enum.DB_INCRBLOB:
          {
#if SQLITE_OMIT_INCRBLOB
            TCL.Tcl_AppendResult( interp, "incrblob not available in this build" );
            return TCL.TCL_ERROR;
#else
int isReadonly = 0;
string zDb = "main" ;
string zTable;
string zColumn;
long iRow = 0;

/* Check for the -readonly option */
if ( objc > 3 && TCL.Tcl_GetString( objv[2] ) == "-readonly" )
{
isReadonly = 1;
}

if ( objc != ( 5 + isReadonly ) && objc != ( 6 + isReadonly ) )
{
TCL.Tcl_WrongNumArgs( interp, 2, objv, "?-readonly? ?DB? TABLE COLUMN ROWID" );
return TCL.TCL_ERROR;
}

if ( objc == ( 6 + isReadonly ) )
{
zDb =  TCL.Tcl_GetString( objv[2] )  ;
}
zTable = TCL.Tcl_GetString( objv[objc - 3] );
zColumn =  TCL.Tcl_GetString( objv[objc - 2] )  ;
rc = TCL.Tcl_GetWideIntFromObj( interp, objv[objc - 1], out iRow ) ? 1 : 0;

if ( rc == TCL.TCL_OK )
{
rc = createIncrblobChannel(
interp, pDb, zDb, zTable, zColumn, iRow, isReadonly
);
}
break;
#endif
          }
        /*
        **     $db interrupt
        **
        ** Interrupt the execution of the inner-most SQL interpreter.  This
        ** causes the SQL statement to return an error of SQLITE_INTERRUPT.
        */
        case (int)DB_enum.DB_INTERRUPT:
          {
            sqlite3_interrupt( pDb.db );
            break;
          }

        /*
        **     $db nullvalue ?STRING?
        **
        ** Change text used when a NULL comes back from the database. If ?STRING?
        ** is not present, then the current string used for NULL is returned.
        ** If STRING is present, then STRING is returned.
        **
        */
        case (int)DB_enum.DB_NULLVALUE:
          {
            if ( objc != 2 && objc != 3 )
            {
              TCL.Tcl_WrongNumArgs( interp, 2, objv, "NULLVALUE" );
              return TCL.TCL_ERROR;
            }
            if ( objc == 3 )
            {
              int len = 0;
              string zNull = TCL.Tcl_GetStringFromObj( objv[2], out len );
              if ( pDb.zNull != null )
              {
                TCL.Tcl_Free( ref pDb.zNull );
              }
              if ( zNull != null && len > 0 )
              {
                pDb.zNull = zNull;
                //pDb.zNull = TCL.Tcl_Alloc( len + 1 );
                //memcpy(pDb->zNull, zNull, len);
                //pDb.zNull[len] = '\0';
              }
              else
              {
                pDb.zNull = null;
              }
            }
            TCL.Tcl_SetObjResult( interp, dbTextToObj( pDb.zNull ) );
            break;
          }

        /*
        **     $db last_insert_rowid
        **
        ** Return an integer which is the ROWID for the most recent insert.
        */
        case (int)DB_enum.DB_LAST_INSERT_ROWID:
          {
            Tcl_Obj pResult;
            Tcl_WideInt rowid;
            if ( objc != 2 )
            {
              TCL.Tcl_WrongNumArgs( interp, 2, objv, "" );
              return TCL.TCL_ERROR;
            }
            rowid = sqlite3_last_insert_rowid( pDb.db );
            pResult = TCL.Tcl_GetObjResult( interp );
            TCL.Tcl_SetLongObj( pResult, rowid );
            break;
          }

        /*
        ** The DB_ONECOLUMN method is implemented together with DB_EXISTS.
        */

        /*    $db progress ?N CALLBACK?
        **
        ** Invoke the given callback every N virtual machine opcodes while executing
        ** queries.
        */
        case (int)DB_enum.DB_PROGRESS:
          {
            if ( objc == 2 )
            {
              if ( !String.IsNullOrEmpty( pDb.zProgress ) )
              {
                TCL.Tcl_AppendResult( interp, pDb.zProgress );
              }
            }
            else if ( objc == 4 )
            {
              string zProgress;
              int len = 0;
              int N = 0;
              if ( TCL.TCL_OK != TCL.Tcl_GetIntFromObj( interp, objv[2], out N ) )
              {
                return TCL.TCL_ERROR;
              };
              if ( !String.IsNullOrEmpty( pDb.zProgress ) )
              {
                TCL.Tcl_Free( ref pDb.zProgress );
              }
              zProgress = TCL.Tcl_GetStringFromObj( objv[3], len );
              if ( !String.IsNullOrEmpty( zProgress ) )
              {
                //pDb.zProgress = TCL.Tcl_Alloc( len + 1 );
                //memcpy( pDb.zProgress, zProgress, len + 1 );
                pDb.zProgress = zProgress;
              }
              else
              {
                pDb.zProgress = null;
              }
#if !SQLITE_OMIT_PROGRESS_CALLBACK
              if ( !String.IsNullOrEmpty( pDb.zProgress ) )
              {
                pDb.interp = interp;
                sqlite3_progress_handler( pDb.db, N, DbProgressHandler, pDb );
              }
              else
              {
                sqlite3_progress_handler( pDb.db, 0, null, 0 );
              }
#endif
            }
            else
            {
              TCL.Tcl_WrongNumArgs( interp, 2, objv, "N CALLBACK" );
              return TCL.TCL_ERROR;
            }
            break;
          }

        /*    $db profile ?CALLBACK?
        **
        ** Make arrangements to invoke the CALLBACK routine after each SQL statement
        ** that has run.  The text of the SQL and the amount of elapse time are
        ** appended to CALLBACK before the script is run.
        */
        case (int)DB_enum.DB_PROFILE:
          {
            if ( objc > 3 )
            {
              TCL.Tcl_WrongNumArgs( interp, 2, objv, "?CALLBACK?" );
              return TCL.TCL_ERROR;
            }
            else if ( objc == 2 )
            {
              if ( !String.IsNullOrEmpty( pDb.zProfile ) )
              {
                TCL.Tcl_AppendResult( interp, pDb.zProfile );
              }
            }
            else
            {
              string zProfile;
              int len = 0;
              if ( !String.IsNullOrEmpty( pDb.zProfile ) )
              {
                TCL.Tcl_Free( ref pDb.zProfile );
              }
              zProfile = TCL.Tcl_GetStringFromObj( objv[2], out len );
              if ( !String.IsNullOrEmpty( zProfile ) && len > 0 )
              {
                //pDb.zProfile = TCL.Tcl_Alloc( len + 1 );
                //memcpy( pDb.zProfile, zProfile, len + 1 );
                pDb.zProfile = zProfile;
              }
              else
              {
                pDb.zProfile = null;
              }
#if !SQLITE_OMIT_TRACE && !(SQLITE_OMIT_FLOATING_POINT)
              if ( !String.IsNullOrEmpty( pDb.zProfile ) )
              {
                pDb.interp = interp;
                sqlite3_profile( pDb.db, DbProfileHandler, pDb );
              }
              else
              {
                sqlite3_profile( pDb.db, null, null );
              }
#endif
            }
            break;
          }

        /*
        **     $db rekey KEY
        **
        ** Change the encryption key on the currently open database.
        */
        case (int)DB_enum.DB_REKEY:
          {
            int nKey = 0;
            string pKey;
            if ( objc != 3 )
            {
              TCL.Tcl_WrongNumArgs( interp, 2, objv, "KEY" );
              return TCL.TCL_ERROR;
            }
            pKey = TCL.Tcl_GetStringFromObj( objv[2], out nKey );
#if SQLITE_HAS_CODEC
            rc = sqlite3_rekey( pDb.db, pKey, nKey );
            if ( rc != 0 )
            {
              TCL.Tcl_AppendResult( interp, sqlite3ErrStr( rc ) );
              rc = TCL.TCL_ERROR;
            }
#endif
            break;
          }

        /*    $db restore ?DATABASE? FILENAME
        **
        ** Open a database file named FILENAME.  Transfer the content
        ** of FILENAME into the local database DATABASE (default: "main").
        */
        case (int)DB_enum.DB_RESTORE:
          {
            string zSrcFile;
            string zDestDb;
            sqlite3 pSrc = null;
            sqlite3_backup pBackup;
            int nTimeout = 0;

            if ( objc == 3 )
            {
              zDestDb = "main";
              zSrcFile = TCL.Tcl_GetString( objv[2] );
            }
            else if ( objc == 4 )
            {
              zDestDb = TCL.Tcl_GetString( objv[2] );
              zSrcFile = TCL.Tcl_GetString( objv[3] );
            }
            else
            {
              TCL.Tcl_WrongNumArgs( interp, 2, objv, "?DATABASE? FILENAME" );
              return TCL.TCL_ERROR;
            }
            rc = sqlite3_open_v2( zSrcFile, out pSrc, SQLITE_OPEN_READONLY, null );
            if ( rc != SQLITE_OK )
            {
              TCL.Tcl_AppendResult( interp, "cannot open source database: ",
              sqlite3_errmsg( pSrc ) );
              sqlite3_close( pSrc );
              return TCL.TCL_ERROR;
            }
            pBackup = sqlite3_backup_init( pDb.db, zDestDb, pSrc, "main" );
            if ( pBackup == null )
            {
              TCL.Tcl_AppendResult( interp, "restore failed: ",
              sqlite3_errmsg( pDb.db ) );
              sqlite3_close( pSrc );
              return TCL.TCL_ERROR;
            }

#if SQLITE_HAS_CODEC
            if ( pBackup.pDestDb.aDb[0].pBt.pBt.pPager.pCodec != null )
            {
              pBackup.pSrc.pBt.pPager.xCodec = pBackup.pDestDb.aDb[0].pBt.pBt.pPager.xCodec;
              pBackup.pSrc.pBt.pPager.xCodecFree = pBackup.pDestDb.aDb[0].pBt.pBt.pPager.xCodecFree;
              pBackup.pSrc.pBt.pPager.xCodecSizeChng = pBackup.pDestDb.aDb[0].pBt.pBt.pPager.xCodecSizeChng;
              pBackup.pSrc.pBt.pPager.pCodec = pBackup.pDestDb.aDb[0].pBt.pBt.pPager.pCodec.Copy();
              if ( pBackup.pDest.GetHashCode() != pBackup.pDestDb.aDb[0].GetHashCode() ) // Not Main Database
              {
                pBackup.pDest.pBt.pPager.xCodec = pBackup.pDestDb.aDb[0].pBt.pBt.pPager.xCodec;
                pBackup.pDest.pBt.pPager.xCodecFree = pBackup.pDestDb.aDb[0].pBt.pBt.pPager.xCodecFree;
                pBackup.pDest.pBt.pPager.xCodecSizeChng = pBackup.pDestDb.aDb[0].pBt.pBt.pPager.xCodecSizeChng;
                pBackup.pDest.pBt.pPager.pCodec = pBackup.pDestDb.aDb[0].pBt.pBt.pPager.pCodec.Copy();
              }
            }
#endif
            while ( ( rc = sqlite3_backup_step( pBackup, 100 ) ) == SQLITE_OK
            || rc == SQLITE_BUSY )
            {
              if ( rc == SQLITE_BUSY )
              {
                if ( nTimeout++ >= 3 )
                  break;
                sqlite3_sleep( 100 );
              }
            }
            sqlite3_backup_finish( pBackup );
            if ( rc == SQLITE_DONE )
            {
              rc = TCL.TCL_OK;
            }
            else if ( rc == SQLITE_BUSY || rc == SQLITE_LOCKED )
            {
              TCL.Tcl_AppendResult( interp, "restore failed: source database busy"
              );
              rc = TCL.TCL_ERROR;
            }
            else
            {
              TCL.Tcl_AppendResult( interp, "restore failed: ",
              sqlite3_errmsg( pDb.db ) );
              rc = TCL.TCL_ERROR;
            }
            sqlite3_close( pSrc );
            break;
          }

        /*
        **     $db status (step|sort|autoindex)
        **
        ** Display SQLITE_STMTSTATUS_FULLSCAN_STEP or
        ** SQLITE_STMTSTATUS_SORT for the most recent eval.
        */
        case (int)DB_enum.DB_STATUS:
          {
            int v;
            string zOp;
            if ( objc != 3 )
            {
              TCL.Tcl_WrongNumArgs( interp, 2, objv, "(step|sort|autoindex)" );
              return TCL.TCL_ERROR;
            }
            zOp = TCL.Tcl_GetString( objv[2] );
            if ( zOp == "step" )
            {
              v = pDb.nStep;
            }
            else if ( zOp == "sort" )
            {
              v = pDb.nSort;
            }
            else if ( zOp == "autoindex" )
            {
              v = pDb.nIndex;
            }
            else
            {
              TCL.Tcl_AppendResult( interp, "bad argument: should be autoindex, step or sort" );
              return TCL.TCL_ERROR;
            }
            TCL.Tcl_SetObjResult( interp, TCL.Tcl_NewIntObj( v ) );
            break;
          }

        /*
        **     $db timeout MILLESECONDS
        **
        ** Delay for the number of milliseconds specified when a file is locked.
        */
        case (int)DB_enum.DB_TIMEOUT:
          {
            int ms = 0;
            if ( objc != 3 )
            {
              TCL.Tcl_WrongNumArgs( interp, 2, objv, "MILLISECONDS" );
              return TCL.TCL_ERROR;
            }
            if ( TCL.TCL_OK != TCL.Tcl_GetIntFromObj( interp, objv[2], out ms ) )
              return TCL.TCL_ERROR;
            sqlite3_busy_timeout( pDb.db, ms );
            break;
          }

        /*
        **     $db total_changes
        **
        ** Return the number of rows that were modified, inserted, or deleted
        ** since the database handle was created.
        */
        case (int)DB_enum.DB_TOTAL_CHANGES:
          {
            Tcl_Obj pResult;
            if ( objc != 2 )
            {
              TCL.Tcl_WrongNumArgs( interp, 2, objv, "" );
              return TCL.TCL_ERROR;
            }
            pResult = TCL.Tcl_GetObjResult( interp );
            TCL.Tcl_SetIntObj( pResult, sqlite3_total_changes( pDb.db ) );
            break;
          }

        /*    $db trace ?CALLBACK?
        **
        ** Make arrangements to invoke the CALLBACK routine for each SQL statement
        ** that is executed.  The text of the SQL is appended to CALLBACK before
        ** it is executed.
        */
        case (int)DB_enum.DB_TRACE:
          {
            if ( objc > 3 )
            {
              TCL.Tcl_WrongNumArgs( interp, 2, objv, "?CALLBACK?" );
              return TCL.TCL_ERROR;
            }
            else if ( objc == 2 )
            {
              if ( pDb.zTrace != null )
              {
                TCL.Tcl_AppendResult( interp, pDb.zTrace );
              }
            }
            else
            {
              string zTrace;
              int len = 0;
              if ( pDb.zTrace != null )
              {
                TCL.Tcl_Free( ref pDb.zTrace );
              }
              zTrace = TCL.Tcl_GetStringFromObj( objv[2], out len );
              if ( zTrace != null && len > 0 )
              {
                //pDb.zTrace = TCL.Tcl_Alloc( len + 1 );
                pDb.zTrace = zTrace;//memcpy( pDb.zTrace, zTrace, len + 1 );
              }
              else
              {
                pDb.zTrace = null;
              }
#if !SQLITE_OMIT_TRACE && !(SQLITE_OMIT_FLOATING_POINT)
              if ( pDb.zTrace != null )
              {
                pDb.interp = interp;
                sqlite3_trace( pDb.db, (dxTrace)DbTraceHandler, pDb );
              }
              else
              {
                sqlite3_trace( pDb.db, null, null );
              }
#endif
            }
            break;
          }

        //  /*    $db transaction [-deferred|-immediate|-exclusive] SCRIPT
        //  **
        //  ** Start a new transaction (if we are not already in the midst of a
        //  ** transaction) and execute the TCL script SCRIPT.  After SCRIPT
        //  ** completes, either commit the transaction or roll it back if SCRIPT
        //  ** throws an exception.  Or if no new transation was started, do nothing.
        //  ** pass the exception on up the stack.
        //  **
        //  ** This command was inspired by Dave Thomas's talk on Ruby at the
        //  ** 2005 O'Reilly Open Source Convention (OSCON).
        //  */
        case (int)DB_enum.DB_TRANSACTION:
          {
            Tcl_Obj pScript;
            string zBegin = "SAVEPOINT _tcl_transaction";
            if ( objc != 3 && objc != 4 )
            {
              TCL.Tcl_WrongNumArgs( interp, 2, objv, "[TYPE] SCRIPT" );
              return TCL.TCL_ERROR;
            }
            if ( pDb.nTransaction == 0 && objc == 4 )
            {
              string[] TTYPE_strs = { "deferred", "exclusive", "immediate", null };

              int ttype = 0;
              if ( TCL.Tcl_GetIndexFromObj( interp, objv[2], TTYPE_strs, "transaction type",
                        0, out ttype ) )
              {
                return TCL.TCL_ERROR;
              }
              switch ( ttype )
              {
                case (int)TTYPE_enum.TTYPE_DEFERRED:    /* no-op */
                  ;
                  break;
                case (int)TTYPE_enum.TTYPE_EXCLUSIVE:
                  zBegin = "BEGIN EXCLUSIVE";
                  break;
                case (int)TTYPE_enum.TTYPE_IMMEDIATE:
                  zBegin = "BEGIN IMMEDIATE";
                  break;
              }
            }
            pScript = objv[objc - 1];

            /* Run the SQLite BEGIN command to open a transaction or savepoint. */
            pDb.disableAuth++;
            rc = sqlite3_exec( pDb.db, zBegin, 0, 0, 0 );
            pDb.disableAuth--;
            if ( rc != SQLITE_OK )
            {
              TCL.Tcl_AppendResult( interp, sqlite3_errmsg( pDb.db ) );
              return TCL.TCL_ERROR;
            }
            pDb.nTransaction++;
            /* If using NRE, schedule a callback to invoke the script pScript, then
            ** a second callback to commit (or rollback) the transaction or savepoint
            ** opened above. If not using NRE, evaluate the script directly, then
            ** call function DbTransPostCmd() to commit (or rollback) the transaction 
            ** or savepoint.  */
            if ( DbUseNre() )
            {
              Debugger.Break();
              //Tcl_NRAddCallback( interp, DbTransPostCmd, cd, 0, 0, 0 );
              //Tcl_NREvalObj(interp, pScript, 0);
            }
            else
            {
              rc = DbTransPostCmd( cd, interp, TCL.Tcl_EvalObjEx( interp, pScript, 0 ) );
            }
            break;
          }

        /*
        **    $db unlock_notify ?script?
        */
        case (int)DB_enum.DB_UNLOCK_NOTIFY:
          {
#if !SQLITE_ENABLE_UNLOCK_NOTIFY
            TCL.Tcl_AppendResult( interp, "unlock_notify not available in this build", 0 );
            rc = TCL.TCL_ERROR;
#else
if( objc!=2 && objc!=3 ){
Tcl_WrongNumArgs(interp, 2, objv, "?SCRIPT?");
rc = TCL.Tcl_ERROR;
}else{
void (*xNotify)(void **, int) = 0;
void *pNotifyArg = 0;

if( pDb.pUnlockNotify ){
Tcl_DecrRefCount(pDb.pUnlockNotify);
pDb.pUnlockNotify = 0;
}

if( objc==3 ){
xNotify = DbUnlockNotify;
pNotifyArg = (void )pDb;
pDb.pUnlockNotify = objv[2];
Tcl_IncrRefCount(pDb.pUnlockNotify);
}

if( sqlite3_unlock_notify(pDb.db, xNotify, pNotifyArg) ){
Tcl_AppendResult(interp, sqlite3_errmsg(pDb.db), 0);
rc = TCL.Tcl_ERROR;
}
}
#endif
            break;
          }
        /*
        **    $db wal_hook ?script?
        **    $db update_hook ?script?
        **    $db rollback_hook ?script?
        */
        case (int)DB_enum.DB_WAL_HOOK:
        case (int)DB_enum.DB_UPDATE_HOOK:
        case (int)DB_enum.DB_ROLLBACK_HOOK:
          {

            /* set ppHook to point at pUpdateHook or pRollbackHook, depending on
            ** whether [$db update_hook] or [$db rollback_hook] was invoked.
            */
            Tcl_Obj ppHook;
            if ( choice == (int)DB_enum.DB_UPDATE_HOOK )
            {
              ppHook = pDb.pUpdateHook;
            }
            else if ( choice == (int)DB_enum.DB_WAL_HOOK )
            {
              ppHook = pDb.pWalHook;
            }
            else
            {
              ppHook = pDb.pRollbackHook;
            }

            if ( objc != 2 && objc != 3 )
            {
              TCL.Tcl_WrongNumArgs( interp, 2, objv, "?SCRIPT?" );
              return TCL.TCL_ERROR;
            }
            if ( ppHook != null )
            {
              TCL.Tcl_SetObjResult( interp, ppHook );
              if ( objc == 3 )
              {
                TCL.Tcl_DecrRefCount( ref ppHook );
                ppHook = null;
              }
            }
            if ( objc == 3 )
            {
              Debug.Assert( null == ppHook );
              if ( objv[2] != null )//TCL.Tcl_GetCharLength( objv[2] ) > 0 )
              {
                ppHook = objv[2];
                TCL.Tcl_IncrRefCount( ppHook );
              }
            }
            if ( choice == (int)DB_enum.DB_UPDATE_HOOK )
            {
              pDb.pUpdateHook = ppHook;
            }
            else
            {
              pDb.pRollbackHook = ppHook;
            }
            sqlite3_update_hook( pDb.db, ( pDb.pUpdateHook != null ? (dxUpdateCallback)DbUpdateHandler : null ), pDb );
            sqlite3_rollback_hook( pDb.db, ( pDb.pRollbackHook != null ? (dxRollbackCallback)DbRollbackHandler : null ), pDb );
            sqlite3_wal_hook( pDb.db, ( pDb.pWalHook != null ? (dxWalCallback)DbWalHandler : null ), pDb );

            break;
          }

        /*    $db version
        **
        ** Return the version string for this database.
        */
        case (int)DB_enum.DB_VERSION:
          {
            TCL.Tcl_SetResult( interp, sqlite3_libversion(), TCL.TCL_STATIC );
            break;
          }

        default:
          Debug.Assert( false, "Missing switch:" + objv[1].ToString() );
          break;
      } /* End of the SWITCH statement */
      return rc;
    }

#if SQLITE_TCL_NRE
/*
** Adaptor that provides an objCmd interface to the NRE-enabled
** interface implementation.
*/
static int DbObjCmdAdaptor(
void *cd,
Tcl_Interp interp,
int objc,
Tcl_Obj *const*objv
){
return TCL.TCL_NRCallObjProc(interp, DbObjCmd, cd, objc, objv);
}
#endif //* SQLITE_TCL_NRE */
    /*
**   sqlite3 DBNAME FILENAME ?-vfs VFSNAME? ?-key KEY? ?-readonly BOOLEAN?
**                           ?-create BOOLEAN? ?-nomutex BOOLEAN?
**
** This is the main Tcl command.  When the "sqlite" Tcl command is
** invoked, this routine runs to process that command.
**
** The first argument, DBNAME, is an arbitrary name for a new
** database connection.  This command creates a new command named
** DBNAME that is used to control that connection.  The database
** connection is deleted when the DBNAME command is deleted.
**
** The second argument is the name of the database file.
**
*/
    static int DbMain( object cd, Tcl_Interp interp, int objc, Tcl_Obj[] objv )
    {
      SqliteDb p;
      string pKey = null;
      int nKey = 0;
      string zArg;
      string zErrMsg;
      int i;
      string zFile;
      string zVfs = null;
      int flags;
      Tcl_DString translatedFilename;
      /* In normal use, each TCL interpreter runs in a single thread.  So
      ** by default, we can turn of mutexing on SQLite database connections.
      ** However, for testing purposes it is useful to have mutexes turned
      ** on.  So, by default, mutexes default off.  But if compiled with
      ** SQLITE_TCL_DEFAULT_FULLMUTEX then mutexes default on.
      */
#if SQLITE_TCL_DEFAULT_FULLMUTEX
flags = SQLITE_OPEN_READWRITE | SQLITE_OPEN_CREATE | SQLITE_OPEN_FULLMUTEX;
#else
      flags = SQLITE_OPEN_READWRITE | SQLITE_OPEN_CREATE | SQLITE_OPEN_NOMUTEX;
#endif
      if ( objc == 2 )
      {
        zArg = TCL.Tcl_GetStringFromObj( objv[1], 0 );
        if ( zArg == "-version" )
        {
          TCL.Tcl_AppendResult( interp, sqlite3_version, null );
          return TCL.TCL_OK;
        }
        if ( zArg == "-has-codec" )
        {
#if SQLITE_HAS_CODEC
          TCL.Tcl_AppendResult( interp, "1" );
#else
TCL.Tcl_AppendResult( interp, "0", null );
#endif
          return TCL.TCL_OK;
        }
        if ( zArg == "-tcl-uses-utf" )
        {
          TCL.Tcl_AppendResult( interp, "1", null );
          return TCL.TCL_OK;
        }
      }
      for ( i = 3; i + 1 < objc; i += 2 )
      {
        zArg = TCL.Tcl_GetString( objv[i] );
        if ( zArg == "-key" )
        {
          pKey = TCL.Tcl_GetStringFromObj( objv[i + 1], out nKey );
        }
        else if ( zArg == "-vfs" )
        {
          zVfs = TCL.Tcl_GetString( objv[i + 1] );
        }
        else if ( zArg == "-readonly" )
        {
          bool b = false;
          if ( TCL.Tcl_GetBooleanFromObj( interp, objv[i + 1], out b ) )
            return TCL.TCL_ERROR;
          if ( b )
          {
            flags &= ~( SQLITE_OPEN_READWRITE | SQLITE_OPEN_CREATE );
            flags |= SQLITE_OPEN_READONLY;
          }
          else
          {
            flags &= ~SQLITE_OPEN_READONLY;
            flags |= SQLITE_OPEN_READWRITE;
          }
        }
        else if ( zArg == "-create" )
        {
          bool b = false;
          if ( TCL.Tcl_GetBooleanFromObj( interp, objv[i + 1], out b ) )
            return TCL.TCL_ERROR;
          if ( b && ( flags & SQLITE_OPEN_READONLY ) == 0 )
          {
            flags |= SQLITE_OPEN_CREATE;
          }
          else
          {
            flags &= ~SQLITE_OPEN_CREATE;
          }
        }
        else if ( zArg == "-nomutex" )
        {
          bool b = false;
          if ( TCL.Tcl_GetBooleanFromObj( interp, objv[i + 1], out b ) )
            return TCL.TCL_ERROR;
          if ( b )
          {
            flags |= SQLITE_OPEN_NOMUTEX;
            flags &= ~SQLITE_OPEN_FULLMUTEX;
          }
          else
          {
            flags &= ~SQLITE_OPEN_NOMUTEX;
          }
        }
        else if ( zArg == "-fullmutex" )//strcmp( zArg, "-fullmutex" ) == 0 )
        {
          bool b = false;
          if ( TCL.Tcl_GetBooleanFromObj( interp, objv[i + 1], out b ) )
            return TCL.TCL_ERROR;
          if ( b )
          {
            flags |= SQLITE_OPEN_FULLMUTEX;
            flags &= ~SQLITE_OPEN_NOMUTEX;
          }
          else
          {
            flags &= ~SQLITE_OPEN_FULLMUTEX;
          }
        }
        else
        {
          TCL.Tcl_AppendResult( interp, "unknown option: ", zArg, null );
          return TCL.TCL_ERROR;
        }
      }
      if ( objc < 3 || ( objc & 1 ) != 1 )
      {
        TCL.Tcl_WrongNumArgs( interp, 1, objv,
        "HANDLE FILENAME ?-vfs VFSNAME? ?-readonly BOOLEAN? ?-create BOOLEAN? ?-nomutex BOOLEAN? ?-fullmutex BOOLEAN?"
#if SQLITE_HAS_CODEC
 + " ?-key CODECKEY?"
#endif
 );
        return TCL.TCL_ERROR;
      }
      zErrMsg = "";
      p = new SqliteDb();//(SqliteDb)Tcl_Alloc( sizeof(*p) );
      if ( p == null )
      {
        TCL.Tcl_SetResult( interp, "malloc failed", TCL.TCL_STATIC );
        return TCL.TCL_ERROR;
      }
      //memset(p, 0, sizeof(*p));
      zFile = TCL.Tcl_GetStringFromObj( objv[2], 0 );
      //zFile = TCL.Tcl_TranslateFileName( interp, zFile, ref translatedFilename );
      sqlite3_open_v2( zFile, out p.db, flags, zVfs );
      //Tcl_DStringFree( ref translatedFilename );
      if ( SQLITE_OK != sqlite3_errcode( p.db ) )
      {
        zErrMsg = sqlite3_errmsg( p.db );// sqlite3_mprintf( "%s", sqlite3_errmsg( p.db ) );
        sqlite3_close( p.db );
        p.db = null;
      }
#if SQLITE_HAS_CODEC
      if ( p.db != null )
      {
        sqlite3_key( p.db, pKey, nKey );
      }
#endif
      if ( p.db == null )
      {
        TCL.Tcl_SetResult( interp, zErrMsg, TCL.TCL_VOLATILE );
        TCL.Tcl_Free( ref p );
        zErrMsg = "";// sqlite3DbFree( db, ref zErrMsg );
        return TCL.TCL_ERROR;
      }
      p.maxStmt = NUM_PREPARED_STMTS;
      p.interp = interp;
      zArg = TCL.Tcl_GetStringFromObj( objv[1], 0 );
      if ( DbUseNre() )
      {
        Debugger.Break();
        //Tcl_NRCreateCommand(interp, zArg, DbObjCmdAdaptor, DbObjCmd,
        //                    p, DbDeleteCmd);
      }
      else
      {
        TCL.Tcl_CreateObjCommand( interp, zArg, (Interp.dxObjCmdProc)DbObjCmd, p, (Interp.dxCmdDeleteProc)DbDeleteCmd );
      }
      return TCL.TCL_OK;
    }

    /*
    ** Provide a dummy TCL.Tcl_InitStubs if we are using this as a static
    ** library.
    */
#if !USE_TCL_STUBS
    //# undef  TCL.Tcl_InitStubs
    static void Tcl_InitStubs( Tcl_Interp interp, string s, int i )
    {
    }
#endif

    /*
** Make sure we have a PACKAGE_VERSION macro defined.  This will be
** defined automatically by the TEA makefile.  But other makefiles
** do not define it.
*/
#if !PACKAGE_VERSION
    public static string PACKAGE_VERSION;//# define PACKAGE_VERSION SQLITE_VERSION
#endif


    /*
** Initialize this module.
**
** This Tcl module contains only a single new Tcl command named "sqlite".
** (Hence there is no namespace.  There is no point in using a namespace
** if the extension only supplies one new name!)  The "sqlite" command is
** used to open a new SQLite database.  See the DbMain() routine above
** for additional information.
**
** The EXTERN macros are required by TCL in order to work on windows.
*/
    //int Sqlite3_Init(Tcl_Interp interp){
    static public int Sqlite3_Init( Tcl_Interp interp )
    {
      PACKAGE_VERSION = SQLITE_VERSION;
      Tcl_InitStubs( interp, "tclsharp 8.4", 0 );
      TCL.Tcl_CreateObjCommand( interp, "sqlite3", (Interp.dxObjCmdProc)DbMain, null, null );
      TCL.Tcl_PkgProvide( interp, "sqlite3", PACKAGE_VERSION );

#if !SQLITE_3_SUFFIX_ONLY
      /* The "sqlite" alias is undocumented.  It is here only to support
** legacy scripts.  All new scripts should use only the "sqlite3"
** command.
*/
      TCL.Tcl_CreateObjCommand( interp, "sqlite", (Interp.dxObjCmdProc)DbMain, null, null );
#endif
      return TCL.TCL_OK;
    }
    //int Tclsqlite3_Init(Tcl_Interp interp){ return Sqlite3_Init(interp); }
    //int Sqlite3_SafeInit(Tcl_Interp interp){ return TCL.TCL_OK; }
    //int Tclsqlite3_SafeInit(Tcl_Interp interp){ return TCL.TCL_OK; }
    //int Sqlite3_Unload(Tcl_Interp interp, int flags){ return TCL.TCL_OK; }
    //int Tclsqlite3_Unload(Tcl_Interp interp, int flags){ return TCL.TCL_OK; }
    //int Sqlite3_SafeUnload(Tcl_Interp interp, int flags){ return TCL.TCL_OK; }
    //int Tclsqlite3_SafeUnload(Tcl_Interp interp, int flags){ return TCL.TCL_OK;}


#if !SQLITE_3_SUFFIX_ONLY
    //int Sqlite_Init(Tcl_Interp interp){ return Sqlite3_Init(interp); }
    //int Tclsqlite_Init(Tcl_Interp interp){ return Sqlite3_Init(interp); }
    //int Sqlite_SafeInit(Tcl_Interp interp){ return TCL.TCL_OK; }
    //int Tclsqlite_SafeInit(Tcl_Interp interp){ return TCL.TCL_OK; }
    //int Sqlite_Unload(Tcl_Interp interp, int flags){ return TCL.TCL_OK; }
    //int Tclsqlite_Unload(Tcl_Interp interp, int flags){ return TCL.TCL_OK; }
    //int Sqlite_SafeUnload(Tcl_Interp interp, int flags){ return TCL.TCL_OK; }
    //int Tclsqlite_SafeUnload(Tcl_Interp interp, int flags){ return TCL.TCL_OK;}
#endif

#if TCLSH
    /*****************************************************************************
** All of the code that follows is used to build standalone TCL interpreters
** that are statically linked with SQLite.  Enable these by compiling
** with -DTCLSH=n where n can be 1 or 2.  An n of 1 generates a standard
** tclsh but with SQLite built in.  An n of 2 generates the SQLite space
** analysis program.
*/

#if (SQLITE_TEST) || (SQLITE_TCLMD5)
    /*
* This code implements the MD5 message-digest algorithm.
* The algorithm is due to Ron Rivest.  This code was
* written by Colin Plumb in 1993, no copyright is claimed.
* This code is in the public domain; do with it what you wish.
*
* Equivalent code is available from RSA Data Security, Inc.
* This code has been tested against that, and is equivalent,
* except that you don't need to include two pages of legalese
* with every copy.
*
* To compute the message digest of a chunk of bytes, declare an
* MD5Context structure, pass it to MD5Init, call MD5Update as
* needed on buffers full of bytes, and then call MD5Final, which
* will fill a supplied 16-byte array with the digest.
*/

    /*
    * If compiled on a machine that doesn't have a 32-bit integer,
    * you just set "uint32" to the appropriate datatype for an
    * unsigned 32-bit integer.  For example:
    *
    *       cc -Duint32='unsigned long' md5.c
    *
    */
    //#if !uint32
    //#  define uint32 unsigned int
    //#endif

    //struct MD5Context {
    //  int isInit;
    //  uint32 buf[4];
    //  uint32 bits[2];
    //  unsigned char in[64];
    //};
    //typedef struct MD5Context MD5Context;
    class MD5Context
    {
      public bool isInit;
      public u32[] buf = new u32[4];
      public u32[] bits = new u32[2];
      public u32[] _in = new u32[64];
      public Mem _Mem;
    };

    /*
    * Note: this code is harmless on little-endian machines.
    */
    //static void byteReverse (unsigned char *buf, unsigned longs){
    //        uint32 t;
    //        do {
    //                t = (uint32)((unsigned)buf[3]<<8 | buf[2]) << 16 |
    //                            ((unsigned)buf[1]<<8 | buf[0]);
    //                *(uint32 )buf = t;
    //                buf += 4;
    //        } while (--longs);
    //}

    /* The four core functions - F1 is optimized somewhat */

    delegate u32 dxF1234( u32 x, u32 y, u32 z );

    //* #define F1(x, y, z) (x & y | ~x & z) */
    //#define F1(x, y, z) (z ^ (x & (y ^ z)))
    static u32 F1( u32 x, u32 y, u32 z )
    {
      return ( z ^ ( x & ( y ^ z ) ) );
    }

    //#define F2(x, y, z) F1(z, x, y)
    static u32 F2( u32 x, u32 y, u32 z )
    {
      return F1( z, x, y );
    }

    //#define F3(x, y, z) (x ^ y ^ z)
    static u32 F3( u32 x, u32 y, u32 z )
    {
      return ( x ^ y ^ z );
    }

    //#define F4(x, y, z) (y ^ (x | ~z))
    static u32 F4( u32 x, u32 y, u32 z )
    {
      return ( y ^ ( x | ~z ) );
    }

    ///* This is the central step in the MD5 algorithm. */
    //#define MD5STEP(f, w, x, y, z, data, s) \
    //        ( w += f(x, y, z) + data,  w = w<<s | w>>(32-s),  w += x )
    static void MD5STEP( dxF1234 f, ref u32 w, u32 x, u32 y, u32 z, u32 data, byte s )
    {
      w += f( x, y, z ) + data;
      w = w << s | w >> ( 32 - s );
      w += x;
    }

    /*
    * The core of the MD5 algorithm, this alters an existing MD5 hash to
    * reflect the addition of 16 longwords of new data.  MD5Update blocks
    * the data and converts bytes into longwords for this routine.
    */
    static void MD5Transform( u32[] buf, u32[] _in )
    {
      u32 a, b, c, d;

      a = buf[0];
      b = buf[1];
      c = buf[2];
      d = buf[3];

      MD5STEP( F1, ref a, b, c, d, _in[0] + 0xd76aa478, 7 );
      MD5STEP( F1, ref d, a, b, c, _in[1] + 0xe8c7b756, 12 );
      MD5STEP( F1, ref c, d, a, b, _in[2] + 0x242070db, 17 );
      MD5STEP( F1, ref b, c, d, a, _in[3] + 0xc1bdceee, 22 );
      MD5STEP( F1, ref a, b, c, d, _in[4] + 0xf57c0faf, 7 );
      MD5STEP( F1, ref d, a, b, c, _in[5] + 0x4787c62a, 12 );
      MD5STEP( F1, ref c, d, a, b, _in[6] + 0xa8304613, 17 );
      MD5STEP( F1, ref b, c, d, a, _in[7] + 0xfd469501, 22 );
      MD5STEP( F1, ref a, b, c, d, _in[8] + 0x698098d8, 7 );
      MD5STEP( F1, ref d, a, b, c, _in[9] + 0x8b44f7af, 12 );
      MD5STEP( F1, ref c, d, a, b, _in[10] + 0xffff5bb1, 17 );
      MD5STEP( F1, ref b, c, d, a, _in[11] + 0x895cd7be, 22 );
      MD5STEP( F1, ref a, b, c, d, _in[12] + 0x6b901122, 7 );
      MD5STEP( F1, ref d, a, b, c, _in[13] + 0xfd987193, 12 );
      MD5STEP( F1, ref c, d, a, b, _in[14] + 0xa679438e, 17 );
      MD5STEP( F1, ref b, c, d, a, _in[15] + 0x49b40821, 22 );

      MD5STEP( F2, ref a, b, c, d, _in[1] + 0xf61e2562, 5 );
      MD5STEP( F2, ref d, a, b, c, _in[6] + 0xc040b340, 9 );
      MD5STEP( F2, ref c, d, a, b, _in[11] + 0x265e5a51, 14 );
      MD5STEP( F2, ref b, c, d, a, _in[0] + 0xe9b6c7aa, 20 );
      MD5STEP( F2, ref a, b, c, d, _in[5] + 0xd62f105d, 5 );
      MD5STEP( F2, ref d, a, b, c, _in[10] + 0x02441453, 9 );
      MD5STEP( F2, ref c, d, a, b, _in[15] + 0xd8a1e681, 14 );
      MD5STEP( F2, ref b, c, d, a, _in[4] + 0xe7d3fbc8, 20 );
      MD5STEP( F2, ref a, b, c, d, _in[9] + 0x21e1cde6, 5 );
      MD5STEP( F2, ref d, a, b, c, _in[14] + 0xc33707d6, 9 );
      MD5STEP( F2, ref c, d, a, b, _in[3] + 0xf4d50d87, 14 );
      MD5STEP( F2, ref b, c, d, a, _in[8] + 0x455a14ed, 20 );
      MD5STEP( F2, ref a, b, c, d, _in[13] + 0xa9e3e905, 5 );
      MD5STEP( F2, ref d, a, b, c, _in[2] + 0xfcefa3f8, 9 );
      MD5STEP( F2, ref c, d, a, b, _in[7] + 0x676f02d9, 14 );
      MD5STEP( F2, ref b, c, d, a, _in[12] + 0x8d2a4c8a, 20 );

      MD5STEP( F3, ref a, b, c, d, _in[5] + 0xfffa3942, 4 );
      MD5STEP( F3, ref d, a, b, c, _in[8] + 0x8771f681, 11 );
      MD5STEP( F3, ref c, d, a, b, _in[11] + 0x6d9d6122, 16 );
      MD5STEP( F3, ref b, c, d, a, _in[14] + 0xfde5380c, 23 );
      MD5STEP( F3, ref a, b, c, d, _in[1] + 0xa4beea44, 4 );
      MD5STEP( F3, ref d, a, b, c, _in[4] + 0x4bdecfa9, 11 );
      MD5STEP( F3, ref c, d, a, b, _in[7] + 0xf6bb4b60, 16 );
      MD5STEP( F3, ref b, c, d, a, _in[10] + 0xbebfbc70, 23 );
      MD5STEP( F3, ref a, b, c, d, _in[13] + 0x289b7ec6, 4 );
      MD5STEP( F3, ref d, a, b, c, _in[0] + 0xeaa127fa, 11 );
      MD5STEP( F3, ref c, d, a, b, _in[3] + 0xd4ef3085, 16 );
      MD5STEP( F3, ref b, c, d, a, _in[6] + 0x04881d05, 23 );
      MD5STEP( F3, ref a, b, c, d, _in[9] + 0xd9d4d039, 4 );
      MD5STEP( F3, ref d, a, b, c, _in[12] + 0xe6db99e5, 11 );
      MD5STEP( F3, ref c, d, a, b, _in[15] + 0x1fa27cf8, 16 );
      MD5STEP( F3, ref b, c, d, a, _in[2] + 0xc4ac5665, 23 );

      MD5STEP( F4, ref a, b, c, d, _in[0] + 0xf4292244, 6 );
      MD5STEP( F4, ref d, a, b, c, _in[7] + 0x432aff97, 10 );
      MD5STEP( F4, ref c, d, a, b, _in[14] + 0xab9423a7, 15 );
      MD5STEP( F4, ref b, c, d, a, _in[5] + 0xfc93a039, 21 );
      MD5STEP( F4, ref a, b, c, d, _in[12] + 0x655b59c3, 6 );
      MD5STEP( F4, ref d, a, b, c, _in[3] + 0x8f0ccc92, 10 );
      MD5STEP( F4, ref c, d, a, b, _in[10] + 0xffeff47d, 15 );
      MD5STEP( F4, ref b, c, d, a, _in[1] + 0x85845dd1, 21 );
      MD5STEP( F4, ref a, b, c, d, _in[8] + 0x6fa87e4f, 6 );
      MD5STEP( F4, ref d, a, b, c, _in[15] + 0xfe2ce6e0, 10 );
      MD5STEP( F4, ref c, d, a, b, _in[6] + 0xa3014314, 15 );
      MD5STEP( F4, ref b, c, d, a, _in[13] + 0x4e0811a1, 21 );
      MD5STEP( F4, ref a, b, c, d, _in[4] + 0xf7537e82, 6 );
      MD5STEP( F4, ref d, a, b, c, _in[11] + 0xbd3af235, 10 );
      MD5STEP( F4, ref c, d, a, b, _in[2] + 0x2ad7d2bb, 15 );
      MD5STEP( F4, ref b, c, d, a, _in[9] + 0xeb86d391, 21 );

      buf[0] += a;
      buf[1] += b;
      buf[2] += c;
      buf[3] += d;
    }

    /*
    * Start MD5 accumulation.  Set bit count to 0 and buffer to mysterious
    * initialization constants.
    */
    static void MD5Init( MD5Context ctx )
    {
      ctx.isInit = true;
      ctx.buf[0] = 0x67452301;
      ctx.buf[1] = 0xefcdab89;
      ctx.buf[2] = 0x98badcfe;
      ctx.buf[3] = 0x10325476;
      ctx.bits[0] = 0;
      ctx.bits[1] = 0;
    }

    /*
    * Update context to reflect the concatenation of another buffer full
    * of bytes.
    */
    static void MD5Update( MD5Context pCtx, byte[] buf, int len )
    {

      MD5Context ctx = (MD5Context)pCtx;
      int t;

      /* Update bitcount */

      t = (int)ctx.bits[0];
      if ( ( ctx.bits[0] = (u32)( t + ( (u32)len << 3 ) ) ) < t )
        ctx.bits[1]++; /* Carry from low to high */
      ctx.bits[1] += (u32)( len >> 29 );

      t = ( t >> 3 ) & 0x3f;    /* Bytes already in shsInfo.data */

      /* Handle any leading odd-sized chunks */

      int _buf = 0; // Offset into buffer
      int p = t; //Offset into ctx._in
      if ( t != 0 )
      {
        //byte p = (byte)ctx._in + t;
        t = 64 - t;
        if ( len < t )
        {
          Buffer.BlockCopy( buf, _buf, ctx._in, p, len );// memcpy( p, buf, len );
          return;
        }
        Buffer.BlockCopy( buf, _buf, ctx._in, p, t ); //memcpy( p, buf, t );
        //byteReverse(ctx._in, 16);
        MD5Transform( ctx.buf, ctx._in );
        _buf += t;// buf += t;
        len -= t;
      }

      /* Process data in 64-byte chunks */

      while ( len >= 64 )
      {
        Buffer.BlockCopy( buf, _buf, ctx._in, 0, 64 );//memcpy( ctx._in, buf, 64 );
        //byteReverse(ctx._in, 16);
        MD5Transform( ctx.buf, ctx._in );
        _buf += 64;// buf += 64;
        len -= 64;
      }

      /* Handle any remaining bytes of data. */

      Buffer.BlockCopy( buf, _buf, ctx._in, 0, len ); //memcpy( ctx._in, buf, len );
    }

    /*
    * Final wrapup - pad to 64-byte boundary with the bit pattern
    * 1 0* (64-bit count of bits processed, MSB-first)
    */

    static void MD5Final( byte[] digest, MD5Context pCtx )
    {
      MD5Context ctx = pCtx;
      int count;
      int p;

      /* Compute number of bytes mod 64 */
      count = (int)( ctx.bits[0] >> 3 ) & 0x3F;

      /* Set the first char of padding to 0x80.  This is safe since there is
      always at least one byte free */
      p = count;
      ctx._in[p++] = 0x80;

      /* Bytes of padding needed to make 64 bytes */
      count = 64 - 1 - count;

      /* Pad out to 56 mod 64 */
      if ( count < 8 )
      {
        /* Two lots of padding:  Pad the first block to 64 bytes */
        Array.Clear( ctx._in, p, count );//memset(p, 0, count);
        //byteReverse( ctx._in, 16 );
        MD5Transform( ctx.buf, ctx._in );

        /* Now fill the next block with 56 bytes */
        Array.Clear( ctx._in, 0, 56 );//memset(ctx._in, 0, 56);
      }
      else
      {
        /* Pad block to 56 bytes */
        Array.Clear( ctx._in, p, count - 8 );//memset(p, 0, count-8);
      }
      //byteReverse( ctx._in, 14 );

      /* Append length in bits and transform */
      ctx._in[14] = (byte)ctx.bits[0];
      ctx._in[15] = (byte)ctx.bits[1];

      MD5Transform( ctx.buf, ctx._in );
      //byteReverse( ctx.buf, 4 );
      Buffer.BlockCopy( ctx.buf, 0, digest, 0, 16 );//memcpy(digest, ctx.buf, 16);
      //memset(ctx, 0, sizeof(ctx));    /* In case it is sensitive */
      Array.Clear( ctx._in, 0, ctx._in.Length );
      Array.Clear( ctx.bits, 0, ctx.bits.Length );
      Array.Clear( ctx.buf, 0, ctx.buf.Length );
      ctx._Mem = null;
    }

    /*
    ** Convert a 128-bit MD5 digest into a 32-digit base-16 number.
    */
    static void DigestToBase16( byte[] digest, byte[] zBuf )
    {
      string zEncode = "0123456789abcdef";
      int i, j;

      for ( j = i = 0; i < 16; i++ )
      {
        int a = digest[i];
        zBuf[j++] = (byte)zEncode[( a >> 4 ) & 0xf];
        zBuf[j++] = (byte)zEncode[a & 0xf];
      }
      if ( j < zBuf.Length )
        zBuf[j] = 0;
    }


    /*
    ** Convert a 128-bit MD5 digest into sequency of eight 5-digit integers
    ** each representing 16 bits of the digest and separated from each
    ** other by a "-" character.
    */
    //static void MD5DigestToBase10x8(unsigned char digest[16], char zDigest[50]){
    //  int i, j;
    //  unsigned int x;
    //  for(i=j=0; i<16; i+=2){
    //    x = digest[i]*256 + digest[i+1];
    //    if( i>0 ) zDigest[j++] = '-';
    //    sprintf(&zDigest[j], "%05u", x);
    //    j += 5;
    //  }
    //  zDigest[j] = 0;
    //}

    /*
    ** A TCL command for md5.  The argument is the text to be hashed.  The
    ** Result is the hash in base64.  
    */
    static int md5_cmd( object cd, Tcl_Interp interp, int argc, Tcl_Obj[] argv )
    {
      MD5Context ctx = new MD5Context();
      byte[] digest = new byte[16];
      byte[] zBuf = new byte[32];


      if ( argc != 2 )
      {
        TCL.Tcl_AppendResult( interp, "wrong # args: should be \"", argv[0],
        " TEXT\"" );
        return TCL.TCL_ERROR;
      }
      MD5Init( ctx );
      MD5Update( ctx, Encoding.UTF8.GetBytes( argv[1].ToString() ), Encoding.UTF8.GetByteCount( argv[1].ToString() ) );
      MD5Final( digest, ctx );
      DigestToBase16( digest, zBuf );
      TCL.Tcl_AppendResult( interp, Encoding.UTF8.GetString( zBuf, 0, zBuf.Length ) );
      return TCL.TCL_OK;
    }


    /*
    ** A TCL command to take the md5 hash of a file.  The argument is the
    ** name of the file.
    */
    static int md5file_cmd( object cd, Tcl_Interp interp, int argc, Tcl_Obj[] argv )
    {
      StreamReader _in = null;
      byte[] digest = new byte[16];
      StringBuilder zBuf = new StringBuilder( 10240 );

      if ( argc != 2 )
      {
        TCL.Tcl_AppendResult( interp, "wrong # args: should be \"", argv[0],
        " FILENAME\"", 0 );
        return TCL.TCL_ERROR;
      }
      Debugger.Break(); // TODO --   _in = fopen( argv[1], "rb" );
      if ( _in == null )
      {
        TCL.Tcl_AppendResult( interp, "unable to open file \"", argv[1],
        "\" for reading", 0 );
        return TCL.TCL_ERROR;
      }
      Debugger.Break(); // TODO
      //MD5Init( ctx );
      //for(;;){
      //  int n;
      //  n = fread(zBuf, 1, zBuf.Capacity, _in);
      //  if( n<=0 ) break;
      //  MD5Update(ctx, zBuf.ToString(), (unsigned)n);
      //}
      //fclose(_in);
      //MD5Final(digest, ctx);
      //  DigestToBase16(digest, zBuf);
      //Tcl_AppendResult( interp, zBuf );
      return TCL.TCL_OK;
    }

    /*
    ** Register the four new TCL commands for generating MD5 checksums
    ** with the TCL interpreter.
    */
    static public int Md5_Init( Tcl_Interp interp )
    {
      TCL.Tcl_CreateCommand( interp, "md5", md5_cmd, null, null );
      //Tcl_CreateCommand(interp, "md5-10x8", (Tcl_CmdProc)md5_cmd,
      //                  MD5DigestToBase10x8, 0);

      TCL.Tcl_CreateCommand( interp, "md5file", md5file_cmd, null, null );

      //Tcl_CreateCommand(interp, "md5file-10x8", (Tcl_CmdProc)md5file_cmd,
      //                  MD5DigestToBase10x8, 0);
      return TCL.TCL_OK;
    }
#endif //* defined(SQLITE_TEST) || defined(SQLITE_TCLMD5) */

#if (SQLITE_TEST)
    /*
** During testing, the special md5sum() aggregate function is available.
** inside SQLite.  The following routines implement that function.
*/
    static void md5step( sqlite3_context context, int argc, sqlite3_value[] argv )
    {
      MD5Context p = null;
      int i;
      if ( argc < 1 )
        return;
      Mem pMem = sqlite3_aggregate_context( context, 1 );//sizeof(*p));
      if ( pMem._MD5Context == null )
      {
        pMem._MD5Context = new MD5Context();
        ( (MD5Context)pMem._MD5Context )._Mem = pMem;
      }
      p = (MD5Context)pMem._MD5Context;
      if ( p == null )
        return;
      if ( !p.isInit )
      {
        MD5Init( p );
      }
      for ( i = 0; i < argc; i++ )
      {
        byte[] zData = sqlite3_value_text( argv[i] ) == null ? null : Encoding.UTF8.GetBytes( sqlite3_value_text( argv[i] ) );
        if ( zData != null )
        {
          MD5Update( p, zData, zData.Length );
        }
      }
    }

    static void md5finalize( sqlite3_context context )
    {
      MD5Context p;
      byte[] digest = new byte[16];
      byte[] zBuf = new byte[33];
      Mem pMem = sqlite3_aggregate_context( context, 0 );
      if ( pMem != null )
      {
        p = (MD5Context)pMem._MD5Context;
        MD5Final( digest, p );
      }
      DigestToBase16( digest, zBuf );
      sqlite3_result_text( context, Encoding.UTF8.GetString( zBuf, 0, zBuf.Length ), -1, SQLITE_TRANSIENT );
    }

    static int Md5_Register( sqlite3 db, ref string dummy1, sqlite3_api_routines dummy2 )
    {
      int rc = sqlite3_create_function( db, "md5sum", -1, SQLITE_UTF8, 0, null,
      md5step, md5finalize );
      sqlite3_overload_function( db, "md5sum", -1 ); /* To exercise this API */
      return rc;
    }

#endif //* defined(SQLITE_TEST) */


    /*
** If the macro TCLSH is one, then put in code this for the
** "main" routine that will initialize Tcl and take input from
** standard input, or if a file is named on the command line
** the TCL interpreter reads and evaluates that file.
*/
#if TCLSH//==1
    //static char zMainloop[] =
    //  "set line {}\n"
    //  "while {![eof stdin]} {\n"
    //    "if {$line!=\"\"} {\n"
    //      "puts -nonewline \"> \"\n"
    //    "} else {\n"
    //      "puts -nonewline \"% \"\n"
    //    "}\n"
    //    "flush stdout\n"
    //    "append line [gets stdin]\n"
    //    "if {[info complete $line]} {\n"
    //      "if {[catch {uplevel #0 $line} result]} {\n"
    //        "puts stderr \"Error: $result\"\n"
    //      "} elseif {$result!=\"\"} {\n"
    //        "puts $result\n"
    //      "}\n"
    //      "set line {}\n"
    //    "} else {\n"
    //      "append line \\n\n"
    //    "}\n"
    //  "}\n"
    //;
#endif

    //#if TCLSH==2
    //static char zMainloop[] = 
    //#include "spaceanal_tcl.h"
    //;
    //#endif
    //#define TCLSH_MAIN main   /* Needed to fake out mktclapp */
#if SQLITE_TEST
    //static void init_all(Tcl_Interp );
    static int init_all_cmd(
    ClientData cd,
    Tcl_Interp interp,
    int objc,
    Tcl_Obj[] objv
    )
    {

      Tcl_Interp slave;
      if ( objc != 2 )
      {
        TCL.Tcl_WrongNumArgs( interp, 1, objv, "SLAVE" );
        return TCL.TCL_ERROR;
      }

      slave = null;//  TCL.Tcl_GetSlave( interp, TCL.Tcl_GetString( objv[1] ) );
      if ( slave == null )
      {
        return TCL.TCL_ERROR;
      }

      init_all( slave );
      return TCL.TCL_OK;
    }
#endif

    /*
** Configure the interpreter passed as the first argument to have access
** to the commands and linked variables that make up:
**
**   * the [sqlite3] extension itself, 
**
**   * If SQLITE_TCLMD5 or SQLITE_TEST is defined, the Md5 commands, and
**
**   * If SQLITE_TEST is set, the various test interfaces used by the Tcl
**     test suite.
*/
    static void init_all( Tcl_Interp interp )
    {
      Sqlite3_Init( interp );
#if (SQLITE_TEST) || (SQLITE_TCLMD5)
      Md5_Init( interp );
#endif
#if SQLITE_TEST
      //{
      //extern int Sqliteconfig_Init(Tcl_Interp);
      //extern int Sqlitetest1_Init(Tcl_Interp);
      //extern int Sqlitetest2_Init(Tcl_Interp);
      //extern int Sqlitetest3_Init(Tcl_Interp);
      //extern int Sqlitetest4_Init(Tcl_Interp);
      //extern int Sqlitetest5_Init(Tcl_Interp);
      //extern int Sqlitetest6_Init(Tcl_Interp);
      //extern int Sqlitetest7_Init(Tcl_Interp);
      //extern int Sqlitetest8_Init(Tcl_Interp);
      //extern int Sqlitetest9_Init(Tcl_Interp);
      //extern int Sqlitetestasync_Init(Tcl_Interp);
      //extern int Sqlitetest_autoext_Init(Tcl_Interp);
      //extern int Sqlitetest_demovfs_Init(Tcl_Interp );
      //extern int Sqlitetest_func_Init(Tcl_Interp);
      //extern int Sqlitetest_hexio_Init(Tcl_Interp);
      //extern int Sqlitetest_malloc_Init(Tcl_Interp);
      //extern int Sqlitetest_mutex_Init(Tcl_Interp);
      //extern int Sqlitetestschema_Init(Tcl_Interp);
      //extern int Sqlitetestsse_Init(Tcl_Interp);
      //extern int Sqlitetesttclvar_Init(Tcl_Interp);
      //extern int SqlitetestThread_Init(Tcl_Interp);
      //extern int SqlitetestOnefile_Init();
      //extern int SqlitetestOsinst_Init(Tcl_Interp);
      //extern int Sqlitetestbackup_Init(Tcl_Interp);
      //extern int Sqlitetestintarray_Init(Tcl_Interp);
      //extern int Sqlitetestvfs_Init(Tcl_Interp );
      //extern int SqlitetestStat_Init(Tcl_Interp);
      //extern int Sqlitetestrtree_Init(Tcl_Interp);
      //extern int Sqlitequota_Init(Tcl_Interp);
      //extern int Sqlitemultiplex_Init(Tcl_Interp);
      //extern int SqliteSuperlock_Init(Tcl_Interp);
      //extern int SqlitetestSyscall_Init(Tcl_Interp);
      //extern int Sqlitetestfuzzer_Init(Tcl_Interp);
      //extern int Sqlitetestwholenumber_Init(Tcl_Interp);

#if (SQLITE_ENABLE_FTS3) || (SQLITE_ENABLE_FTS4)
    //extern int Sqlitetestfts3_Init(Tcl_Interp interp);
#endif

#if SQLITE_ENABLE_ZIPVFS
//    extern int Zipvfs_Init(Tcl_Interp);
//    Zipvfs_Init(interp);
#endif

      Sqliteconfig_Init( interp );
      Sqlitetest1_Init( interp );
      Sqlitetest2_Init( interp );
      Sqlitetest3_Init( interp );

      //Threads not implemented under C#
      //Sqlitetest4_Init(interp);

      //TODO implemented under C#
      //Sqlitetest5_Init(interp);

      //Simulated Crashtests not implemented under C#
      //Sqlitetest6_Init(interp);

      //client/server version (Unix Only) not implemented under C#
      //Sqlitetest7_Init(interp);

      //virtual table interface not implemented under C#
      //Sqlitetest8_Init(interp);

      Sqlitetest9_Init( interp );

      //asynchronous IO extension interface not implemented under C#
      //Sqlitetestasync_Init(interp);

      //sqlite3_auto_extension() function not implemented under C#
      //Sqlitetest_autoext_Init(interp);

      //VFS not implemented under C#
      //Sqlitetest_demovfs_Init(interp);

      Sqlitetest_func_Init( interp );
      Sqlitetest_hexio_Init( interp );
      Sqlitetest_malloc_Init( interp );
      Sqlitetest_mutex_Init( interp );

      //virtual table interfaces not implemented under C#
      //Sqlitetestschema_Init(interp);

      //virtual table interfaces not implemented under C#
      //Sqlitetesttclvar_Init(interp);

      //Threads not implemented under C#
      //SqlitetestThread_Init(interp);

      //VFS not implemented under C#
      //SqlitetestOnefile_Init(interp);

      //VFS not implemented under C#
      //SqlitetestOsinst_Init(interp);

      Sqlitetestbackup_Init( interp );

      //virtual table interfaces not implemented under C#
      //Sqlitetestintarray_Init(interp);

      //VFS not implemented under C#
      //Sqlitetestvfs_Init(interp);

      //virtual table interfaces not implemented under C#
      //SqlitetestStat_Init(interp);
      //Sqlitetestrtree_Init( interp );
      //Sqlitequota_Init( interp );
      //Sqlitemultiplex_Init( interp );
      //SqliteSuperlock_Init( interp );
      //SqlitetestSyscall_Init( interp );
      Sqlitetestfuzzer_Init( interp );
      //Sqlitetestwholenumber_Init( interp );

#if (SQLITE_ENABLE_FTS3) || (SQLITE_ENABLE_FTS4)
    //Sqlitetestfts3_Init(interp);
#endif
      TCL.Tcl_CreateObjCommand( interp, "load_testfixture_extensions", init_all_cmd, 0, null );

#if SQLITE_SSE
Sqlitetestsse_Init(interp);
#endif
    }
#endif
  }

#if FALSE
//#define TCLSH_MAIN main   /* Needed to fake out mktclapp */
int TCLSH_MAIN(int argc, char **argv){
Tcl_Interp interp;

/* Call sqlite3_shutdown() once before doing anything else. This is to
** test that sqlite3_shutdown() can be safely called by a process before
** sqlite3_initialize() is. */
sqlite3_shutdown();

#if TCLSH//TCLSH==2
sqlite3_config(SQLITE_CONFIG_SINGLETHREAD);
#endif
Tcl_FindExecutable(argv[0]);

interp = TCL.Tcl_CreateInterp();
init_all(interp);
if( argc>=2 ){
int i;
char zArgc[32];
sqlite3_snprintf(sizeof(zArgc), zArgc, "%d", argc-(3-TCLSH));
Tcl_SetVar(interp,"argc", zArgc, TCL_GLOBAL_ONLY);
Tcl_SetVar(interp,"argv0",argv[1],TCL_GLOBAL_ONLY);
Tcl_SetVar(interp,"argv", "", TCL_GLOBAL_ONLY);
for(i=3-TCLSH; i<argc; i++){
Tcl_SetVar(interp, "argv", argv[i],
TCL_GLOBAL_ONLY | TCL_LIST_ELEMENT | TCL_APPEND_VALUE);
}
if( TCLSH==1 && Tcl_EvalFile(interp, argv[1])!=TCL_OK ){
string zInfo = TCL.Tcl_GetVar(interp, "errorInfo", TCL_GLOBAL_ONLY);
if( zInfo==0 ) zInfo = TCL.Tcl_GetStringResult(interp);
fprintf(stderr,"%s: %s\n", *argv, zInfo);
return 1;
}
}
if( TCLSH==2 || argc<=1 ){
Tcl_GlobalEval(interp, zMainloop);
}
return 0;
}
#endif
#endif // * TCLSH */
#endif // NO_TCL
}

