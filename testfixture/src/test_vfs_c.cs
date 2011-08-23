using System;
using System.Diagnostics;
using System.Text;

using u8 = System.Byte;
using u32 = System.UInt32;

namespace Community.CsharpSqlite
{
#if TCLSH
  using tcl.lang;
  using sqlite_int64 = System.Int64;
  using sqlite3_int64 = System.Int64;
  using sqlite3_stmt = Community.CsharpSqlite.Sqlite3.Vdbe;
  using Tcl_Interp = tcl.lang.Interp;
  using Tcl_Obj = tcl.lang.TclObject;
  using ClientData = System.Object;

  public partial class Sqlite3
  {
/*
** 2010 May 05
**
** The author disclaims copyright to this source code.  In place of
** a legal notice, here is a blessing:
**
**    May you do good and not evil.
**    May you find forgiveness for yourself and forgive others.
**    May you share freely, never taking more than you give.
**
******************************************************************************
**
** This file contains the implementation of the Tcl [testvfs] command,
** used to create SQLite VFS implementations with various properties and
** instrumentation to support testing SQLite.
**
**   testvfs VFSNAME ?OPTIONS?
**
** Available options are:
**
**   -noshm      BOOLEAN        (True to omit shm methods. Default false)
**   -default    BOOLEAN        (True to make the vfs default. Default false)
**   -szosfile   INTEGER        (Value for sqlite3_vfs.szOsFile)
**   -mxpathname INTEGER        (Value for sqlite3_vfs.mxPathname)
**   -iversion   INTEGER        (Value for sqlite3_vfs.iVersion)
*/
#if SQLITE_TEST          //* This file is used for testing only */

//#include "sqlite3.h"
//#include "sqliteInt.h"

//typedef struct Testvfs Testvfs;
//typedef struct TestvfsShm TestvfsShm;
//typedef struct TestvfsBuffer TestvfsBuffer;
//typedef struct TestvfsFile TestvfsFile;
//typedef struct TestvfsFd TestvfsFd;

/*
** An open file handle.
*/
    class TestvfsFile : sqlite3_file
    {
  //public sqlite3_file base;           /* Base class.  Must be first */
  public TestvfsFd pFd;                 /* File data */
};
//#define tvfsGetFd(pFile) (((TestvfsFile )pFile)->pFd)
    static TestvfsFd tvfsGetFd( sqlite3_file pFile )
    {
      return ((TestvfsFile)pFile).pFd;
    }

class TestvfsFd {
  public sqlite3_vfs pVfs;              /* The VFS */
  public string zFilename;          /* Filename as passed to xOpen() */
  public sqlite3_file pReal;            /* The real, underlying file descriptor */
  public Tcl_Obj pShmId;                /* Shared memory id for Tcl callbacks */

  public TestvfsBuffer pShm;            /* Shared memory buffer */
  public u32 excllock;                   /* Mask of exclusive locks */
  public u32 sharedlock;                 /* Mask of shared locks */
  public TestvfsFd pNext;               /* Next handle opened on the same file */
};


//#define FAULT_INJECT_NONE       0
//#define FAULT_INJECT_TRANSIENT  1
//#define FAULT_INJECT_PERSISTENT 2
const int FAULT_INJECT_NONE = 0;
const int FAULT_INJECT_TRANSIENT = 1;
const int FAULT_INJECT_PERSISTENT = 2;

//typedef struct TestFaultInject TestFaultInject;
class TestFaultInject {
  public int iCnt;                       /* Remaining calls before fault injection */
  public int eFault;                     /* A FAULT_INJECT_* value */
  public int nFail;                      /* Number of faults injected */
};

/*
** An instance of this structure is allocated for each VFS created. The
** sqlite3_vfs.pAppData field of the VFS structure registered with SQLite
** is set to point to it.
*/
class Testvfs {
  public string zName;                  /* Name of this VFS */
  public sqlite3_vfs pParent;           /* The VFS to use for file IO */
  public sqlite3_vfs pVfs;              /* The testvfs registered with SQLite */
  public Tcl_Interp interp;             /* Interpreter to run script in */
  public Tcl_Obj pScript;               /* Script to execute */
  public TestvfsBuffer pBuffer;         /* List of shared buffers */
  public int isNoshm;

  public int mask;                       /* Mask controlling [script] and [ioerr] */

  public TestFaultInject ioerr_err;
  public TestFaultInject full_err;
  public TestFaultInject cantopen_err;

#if FALSE
  public int iIoerrCnt;
  public int ioerr;
  public int nIoerrFail;
  public int iFullCnt;
  public int fullerr;
  public int nFullFail;
#endif

  public int iDevchar;
  public int iSectorsize;
};

/*
** The Testvfs.mask variable is set to a combination of the following.
** If a bit is clear in Testvfs.mask, then calls made by SQLite to the 
** corresponding VFS method is ignored for purposes of:
**
**   + Simulating IO errors, and
**   + Invoking the Tcl callback script.
*/
//#define TESTVFS_SHMOPEN_MASK      0x00000001
//#define TESTVFS_SHMLOCK_MASK      0x00000010
//#define TESTVFS_SHMMAP_MASK       0x00000020
//#define TESTVFS_SHMBARRIER_MASK   0x00000040
//#define TESTVFS_SHMCLOSE_MASK     0x00000080
 const int  TESTVFS_SHMOPEN_MASK      =0x00000001;
 const int  TESTVFS_SHMLOCK_MASK      =0x00000010;
 const int  TESTVFS_SHMMAP_MASK       =0x00000020;
 const int  TESTVFS_SHMBARRIER_MASK   =0x00000040;
 const int  TESTVFS_SHMCLOSE_MASK     =0x00000080;

//#define TESTVFS_OPEN_MASK         0x00000100
//#define TESTVFS_SYNC_MASK         0x00000200
//#define TESTVFS_DELETE_MASK       0x00000400
//#define TESTVFS_CLOSE_MASK        0x00000800
//#define TESTVFS_WRITE_MASK        0x00001000
//#define TESTVFS_TRUNCATE_MASK     0x00002000
//#define TESTVFS_ACCESS_MASK       0x00004000
//#define TESTVFS_FULLPATHNAME_MASK 0x00008000
//#define TESTVFS_ALL_MASK          0x0001FFFF
 const int  TESTVFS_OPEN_MASK         =0x00000100;
 const int  TESTVFS_SYNC_MASK         =0x00000200;
 const int  TESTVFS_DELETE_MASK       =0x00000400;
 const int  TESTVFS_CLOSE_MASK        =0x00000800;
 const int  TESTVFS_WRITE_MASK        =0x00001000;
 const int  TESTVFS_TRUNCATE_MASK     =0x00002000;
 const int  TESTVFS_ACCESS_MASK       =0x00004000;
 const int  TESTVFS_FULLPATHNAME_MASK =0x00008000;
 const int  TESTVFS_ALL_MASK          =0x0001FFFF;

//#define TESTVFS_MAX_PAGES 1024
 const int  TESTVFS_MAX_PAGES          =1024;

/*
** A shared-memory buffer. There is one of these objects for each shared
** memory region opened by clients. If two clients open the same file,
** there are two TestvfsFile structures but only one TestvfsBuffer structure.
*/
class TestvfsBuffer {
  public string zFile;                   /* Associated file name */
  public int pgsz;                       /* Page size */
  public u8[] aPage = new u8[TESTVFS_MAX_PAGES];   /* Array of ckalloc'd pages */
  public TestvfsFd pFile;                /* List of open handles */
  public TestvfsBuffer pNext;            /* Next in linked list of all buffers */
};


//#define PARENTVFS(x) (((Testvfs )((x)->pAppData))->pParent)
static sqlite3_vfs PARENTVFS( sqlite3_vfs x )
{
  return ( (Testvfs)x.pAppData ).pParent;
}

//#define TESTVFS_MAX_ARGS 12
    const int TESTVFS_MAX_ARGS =12;


/*
** Method declarations for TestvfsFile.
*/
//static int tvfsClose(sqlite3_file);
//static int tvfsRead(sqlite3_file*, void*, int iAmt, sqlite3_int64 iOfst);
//static int tvfsWrite(sqlite3_file*,const void*,int iAmt, sqlite3_int64 iOfst);
//static int tvfsTruncate(sqlite3_file*, sqlite3_int64 size);
//static int tvfsSync(sqlite3_file*, int flags);
//static int tvfsFileSize(sqlite3_file*, sqlite3_int64 *pSize);
//static int tvfsLock(sqlite3_file*, int);
//static int tvfsUnlock(sqlite3_file*, int);
//static int tvfsCheckReservedLock(sqlite3_file*, int );
//static int tvfsFileControl(sqlite3_file*, int op, object  *pArg);
//static int tvfsSectorSize(sqlite3_file);
//static int tvfsDeviceCharacteristics(sqlite3_file);

///*
//** Method declarations for tvfs_vfs.
//*/
//static int tvfsOpen(sqlite3_vfs*, string , sqlite3_file*, int , int );
//static int tvfsDelete(sqlite3_vfs*, string zName, int syncDir);
//static int tvfsAccess(sqlite3_vfs*, string zName, int flags, int );
//static int tvfsFullPathname(sqlite3_vfs*, string zName, int, string zOut);
//#if !SQLITE_OMIT_LOAD_EXTENSION
//static void tvfsDlOpen(sqlite3_vfs*, string zFilename);
//static void tvfsDlError(sqlite3_vfs*, int nByte, string zErrMsg);
//static void (*tvfsDlSym(sqlite3_vfs*,void*, string zSymbol))(void);
//static void tvfsDlClose(sqlite3_vfs*, void);
//#endif //* SQLITE_OMIT_LOAD_EXTENSION */
//static int tvfsRandomness(sqlite3_vfs*, int nByte, string zOut);
//static int tvfsSleep(sqlite3_vfs*, int microseconds);
//static int tvfsCurrentTime(sqlite3_vfs*, double);

//static int tvfsShmOpen(sqlite3_file);
//static int tvfsShmLock(sqlite3_file*, int , int, int);
//static int tvfsShmMap(sqlite3_file*,int,int,int, object  volatile *);
//static void tvfsShmBarrier(sqlite3_file);
//static int tvfsShmUnmap(sqlite3_file*, int);

static sqlite3_io_methods tvfs_io_methods = new sqlite3_io_methods(
  2,                              /* iVersion */
  tvfsClose,                      /* xClose */
  tvfsRead,                       /* xRead */
  tvfsWrite,                      /* xWrite */
  tvfsTruncate,                   /* xTruncate */
  tvfsSync,                       /* xSync */
  tvfsFileSize,                   /* xFileSize */
  tvfsLock,                       /* xLock */
  tvfsUnlock,                     /* xUnlock */
  tvfsCheckReservedLock,          /* xCheckReservedLock */
  tvfsFileControl,                /* xFileControl */
  tvfsSectorSize,                 /* xSectorSize */
  tvfsDeviceCharacteristics,      /* xDeviceCharacteristics */
  tvfsShmMap,                     /* xShmMap */
  tvfsShmLock,                    /* xShmLock */
  tvfsShmBarrier,                 /* xShmBarrier */
  tvfsShmUnmap                    /* xShmUnmap */
);

  class errcode {
    public int eCode;
    public string zCode;

    public errcode(int eCode, string zCode){
      this.eCode=eCode;this.zCode=zCode;
    }
  }
    static int tvfsResultCode(Testvfs p, ref int pRc){
errcode[] aCode = new errcode[]  {
    new errcode( SQLITE_OK,     "SQLITE_OK"     ),
    new errcode( SQLITE_ERROR,  "SQLITE_ERROR"  ),
    new errcode( SQLITE_IOERR,  "SQLITE_IOERR"  ),
    new errcode( SQLITE_LOCKED, "SQLITE_LOCKED" ),
    new errcode( SQLITE_BUSY,   "SQLITE_BUSY"  )
  };

  string z;
  int i;

  z = TCL.Tcl_GetStringResult(p.interp);
  for(i=0; i<ArraySize(aCode); i++){
    if ( 0 == z.CompareTo( aCode[i].zCode ) )
    {
      pRc = aCode[i].eCode;
      return 1;
    }
  }

  return 0;
}

static int tvfsInjectFault(TestFaultInject p){
  int ret = 0;
  if ( p.eFault != 0 )
  {
    p.iCnt--;
    if( p.iCnt==0 || (p.iCnt<0 && p.eFault==FAULT_INJECT_PERSISTENT ) ){
      ret = 1;
      p.nFail++;
    }
  }
  return ret;
}


static int tvfsInjectIoerr(Testvfs p){
  return tvfsInjectFault(p.ioerr_err);
}

static int tvfsInjectFullerr(Testvfs p){
  return tvfsInjectFault(p.full_err);
}
static int tvfsInjectCantopenerr(Testvfs p){
  return tvfsInjectFault(p.cantopen_err);
}


static void tvfsExecTcl(
  Testvfs p, 
  string zMethod,
  Tcl_Obj arg1,
  Tcl_Obj arg2,
  Tcl_Obj arg3
){
  int rc;                         /* Return code from Tcl_EvalObj() */
  Tcl_Obj pEval;
  Debug.Assert( p.pScript!=null );

  Debug.Assert( zMethod != null );
  Debug.Assert( p != null );
  Debug.Assert( arg2 == null || arg1 != null );
  Debug.Assert( arg3 == null || arg2 != null );

  pEval = TCL.Tcl_DuplicateObj(p.pScript);
  TCL.Tcl_IncrRefCount(p.pScript);
  TCL.Tcl_ListObjAppendElement( p.interp, pEval, TCL.Tcl_NewStringObj( zMethod, -1 ) );
  if ( arg1!=null )
    TCL.Tcl_ListObjAppendElement( p.interp, pEval, arg1 );
  if ( arg2 !=null )
    TCL.Tcl_ListObjAppendElement( p.interp, pEval, arg2 );
  if ( arg3 != null )
    TCL.Tcl_ListObjAppendElement( p.interp, pEval, arg3 );

  rc = TCL.Tcl_EvalObjEx(p.interp, pEval, TCL.TCL_EVAL_GLOBAL);
  if ( rc != TCL.TCL_OK )
  {
    TCL.Tcl_BackgroundError( p.interp );
    TCL.Tcl_ResetResult( p.interp );
  }
}


/*
** Close an tvfs-file.
*/
static int tvfsClose(sqlite3_file pFile){
  int rc=0;
  TestvfsFile pTestfile = (TestvfsFile )pFile;
  TestvfsFd pFd = pTestfile.pFd;
  Testvfs p = (Testvfs )pFd.pVfs.pAppData;

  Debugger.Break(); //TODO
  //if( p.pScript != null && (p.mask&TESTVFS_CLOSE_MASK)!=0 ){
  //  tvfsExecTcl(p, "xClose", 
  //      Tcl_NewStringObj(pFd.zFilename, -1), pFd.pShmId, 0
  //  );
  //}

  //if( pFd.pShmId != null){
  //  Tcl_DecrRefCount(pFd.pShmId);
  //  pFd.pShmId = null;
  //}
  //if ( pFile.pMethods != null )
  //{
  //  ckfree((char )pFile.pMethods);
  //}
  //rc = sqlite3OsClose(pFd.pReal);
  //ckfree((char )pFd);
  //pTestfile.pFd = null;
  return rc;
}

/*
** Read data from an tvfs-file.
*/
static int tvfsRead(
  sqlite3_file pFile, 
  byte[] zBuf, 
  int iAmt, 
  sqlite_int64 iOfst
){
  TestvfsFd p = tvfsGetFd(pFile);
  return sqlite3OsRead(p.pReal, zBuf, iAmt, iOfst);
}

/*
** Write data to an tvfs-file.
*/
static int tvfsWrite(
  sqlite3_file pFile, 
  byte[] zBuf, 
  int iAmt, 
  sqlite_int64 iOfst
){
  int rc = SQLITE_OK;
  Debugger.Break();//TODO
  //TestvfsFd pFd = tvfsGetFd(pFile);
  //Testvfs p = (Testvfs )pFd.pVfs.pAppData;

  //if ( p.pScript != null && (p.mask & TESTVFS_WRITE_MASK) != 0 )
  //{
  //  tvfsExecTcl(p, "xWrite", 
  //      TCL.Tcl_NewStringObj(pFd.zFilename, -1), pFd.pShmId, null
  //  );
  //  tvfsResultCode(p, ref rc);
  //}

  //if( rc==SQLITE_OK && tvfsInjectFullerr(p)!=0 ){
  //  rc = SQLITE_FULL;
  //}
  //if ( rc == SQLITE_OK && (p.mask & TESTVFS_WRITE_MASK) != 0 && tvfsInjectIoerr( p ) != 0 )
  //{
  //  rc = SQLITE_IOERR;
  //}
  
  //if( rc==SQLITE_OK ){
  //  rc = sqlite3OsWrite(pFd.pReal, zBuf, iAmt, iOfst);
  //}
  return rc;
}

/*
** Truncate an tvfs-file.
*/
static int tvfsTruncate(sqlite3_file pFile, sqlite_int64 size){
  int rc = SQLITE_OK;
  TestvfsFd pFd = tvfsGetFd(pFile);
  Testvfs p = (Testvfs )pFd.pVfs.pAppData;

  if ( p.pScript != null && ( p.mask & TESTVFS_TRUNCATE_MASK ) != 0 )
  {
    tvfsExecTcl(p, "xTruncate", 
        TCL.Tcl_NewStringObj(pFd.zFilename, -1), pFd.pShmId,null
    );
    tvfsResultCode(p, ref rc);
  }
  
  if( rc==SQLITE_OK ){
    rc = sqlite3OsTruncate(pFd.pReal, size);
  }
  return rc;
}

/*
** Sync an tvfs-file.
*/
static int tvfsSync(sqlite3_file pFile, int flags){
  int rc = SQLITE_OK;
  TestvfsFd pFd = tvfsGetFd(pFile);
  Testvfs p = (Testvfs )pFd.pVfs.pAppData;

  if ( p.pScript != null && ( p.mask & TESTVFS_SYNC_MASK ) != 0 )
  {
    string zFlags = "";

    switch( flags ){
      case SQLITE_SYNC_NORMAL:
        zFlags = "normal";
        break;
      case SQLITE_SYNC_FULL:
        zFlags = "full";
        break;
      case SQLITE_SYNC_NORMAL|SQLITE_SYNC_DATAONLY:
        zFlags = "normal|dataonly";
        break;
      case SQLITE_SYNC_FULL|SQLITE_SYNC_DATAONLY:
        zFlags = "full|dataonly";
        break;
      default:
        Debug.Assert(false);
        break;
    }

    tvfsExecTcl(p, "xSync", 
        TCL.Tcl_NewStringObj(pFd.zFilename, -1), pFd.pShmId,
        TCL.Tcl_NewStringObj( zFlags, -1 )
    );
    tvfsResultCode(p, ref rc);
  }

  if( rc==SQLITE_OK && tvfsInjectFullerr(p)!=0 ) rc = SQLITE_FULL;

  if( rc==SQLITE_OK ){
    rc = sqlite3OsSync(pFd.pReal, flags);
  }

  return rc;
}

/*
** Return the current file-size of an tvfs-file.
*/
static int tvfsFileSize(sqlite3_file pFile, ref sqlite_int64 pSize){
  TestvfsFd p = tvfsGetFd(pFile);
  return sqlite3OsFileSize(p.pReal, ref pSize);
}

/*
** Lock an tvfs-file.
*/
static int tvfsLock(sqlite3_file pFile, int eLock){
  TestvfsFd p = tvfsGetFd(pFile);
  return sqlite3OsLock(p.pReal, eLock);
}

/*
** Unlock an tvfs-file.
*/
static int tvfsUnlock(sqlite3_file pFile, int eLock){
  TestvfsFd p = tvfsGetFd(pFile);
  return sqlite3OsUnlock(p.pReal, eLock);
}

/*
** Check if another file-handle holds a RESERVED lock on an tvfs-file.
*/
static int tvfsCheckReservedLock( sqlite3_file pFile, ref int pResOut )
{
  TestvfsFd p = tvfsGetFd(pFile);
  return sqlite3OsCheckReservedLock( p.pReal, ref pResOut );
}

/*
** File control method. For custom operations on an tvfs-file.
*/
static int tvfsFileControl( sqlite3_file pFile, int op, ref sqlite3_int64 pArg )
{
  TestvfsFd p = tvfsGetFd(pFile);
  return sqlite3OsFileControl( p.pReal, (u32)op, ref pArg );
}

/*
** Return the sector-size in bytes for an tvfs-file.
*/
static int tvfsSectorSize(sqlite3_file pFile){
  TestvfsFd pFd = tvfsGetFd(pFile);
  Testvfs p = (Testvfs )pFd.pVfs.pAppData;
  if( p.iSectorsize>=0 ){
    return p.iSectorsize;
  }
  return sqlite3OsSectorSize(pFd.pReal);
}

/*
** Return the device characteristic flags supported by an tvfs-file.
*/
static int tvfsDeviceCharacteristics(sqlite3_file pFile){
  TestvfsFd pFd = tvfsGetFd(pFile);
  Testvfs p = (Testvfs )pFd.pVfs.pAppData;
  if( p.iDevchar>=0 ){
    return p.iDevchar;
  }
  return sqlite3OsDeviceCharacteristics(pFd.pReal);
}

/*
** Open an tvfs file handle.
*/
static int tvfsOpen(
  sqlite3_vfs pVfs,
  string zName,
  sqlite3_file pFile,
  int flags,
  ref int pOutFlags
){
  int rc=0;
  Debugger.Break();//TODO
  //TestvfsFile pTestfile = (TestvfsFile)pFile;
  //TestvfsFd pFd;
  //Tcl_Obj pId = null;
  //Testvfs p = (Testvfs )pVfs.pAppData;

  //pFd = (TestvfsFd )ckalloc(sizeof(TestvfsFd) + PARENTVFS(pVfs).szOsFile);
  //pFd = new TestvfsFd();//  memset( pFd, 0, sizeof( TestvfsFd ) + PARENTVFS( pVfs ).szOsFile );
  //pFd.pShm = null;
  //pFd.pShmId = null;
  //pFd.zFilename = zName;
  //pFd.pVfs = pVfs;
  //pFd.pReal = (sqlite3_file )pFd[1];
  //pTestfile = new TestvfsFile();//  memset( pTestfile, 0, sizeof( TestvfsFile ) );
  //pTestfile.pFd = pFd;

  ///* Evaluate the Tcl script: 
  //**
  //**   SCRIPT xOpen FILENAME KEY-VALUE-ARGS
  //**
  //** If the script returns an SQLite error code other than SQLITE_OK, an
  //** error is returned to the caller. If it returns SQLITE_OK, the new
  //** connection is named "anon". Otherwise, the value returned by the
  //** script is used as the connection name.
  //*/
  //TCL.Tcl_ResetResult(p.interp);
  //if ( p.pScript != null && ( p.mask & TESTVFS_OPEN_MASK ) != 0 )
  //{
  //  Tcl_Obj pArg = TCL.Tcl_NewObj();
  //  TCL.Tcl_IncrRefCount( pArg );
  //  if( (flags&SQLITE_OPEN_MAIN_DB )!=0){
  //    string z = zName[strlen(zName)+1];
  //    while( *z ){
  //      TCL.Tcl_ListObjAppendElement( 0, pArg, TCL.Tcl_NewStringObj( z, -1 ) );
  //      z += strlen(z) + 1;
  //      TCL.Tcl_ListObjAppendElement( 0, pArg, TCL.Tcl_NewStringObj( z, -1 ) );
  //      z += strlen(z) + 1;
  //    }
  //  }
  //  tvfsExecTcl(p, "xOpen", TCL.Tcl_NewStringObj(pFd.zFilename, -1), pArg, null);
  //  TCL.Tcl_DecrRefCount( pArg );
  //  if( tvfsResultCode(p, ref rc)!=0 ){
  //    if( rc!=SQLITE_OK ) return rc;
  //  }else{
  //    pId = TCL.Tcl_GetObjResult(p.interp);
  //  }
  //}

  //if( (p.mask&TESTVFS_OPEN_MASK)!=0 &&  tvfsInjectIoerr(p) !=0) return SQLITE_IOERR;
  //if( tvfsInjectCantopenerr(p)!=0 ) return SQLITE_CANTOPEN;
  //if( tvfsInjectFullerr(p)!=0 ) return SQLITE_FULL;

  //if( null==pId ){
  //  pId = TCL.Tcl_NewStringObj("anon", -1);
  //}
  //TCL.Tcl_IncrRefCount( pId );
  //pFd.pShmId = pId;
  //TCL.Tcl_ResetResult( p.interp );

  //rc = sqlite3OsOpen(PARENTVFS(pVfs), zName, pFd.pReal, flags, pOutFlags);
  //if ( pFd.pReal.pMethods != null )
  //{
  //  sqlite3_io_methods pMethods;
  //  int nByte;

    //if( pVfs.iVersion>1 ){
    //  nByte = sizeof(sqlite3_io_methods);
    //}else{
    //  nByte = offsetof(sqlite3_io_methods, xShmMap);
    //}

    //pMethods = (sqlite3_io_methods)ckalloc( nByte );
    //memcpy(pMethods, &tvfs_io_methods, nByte);
    //pMethods.iVersion = pVfs.iVersion;
    //if( pVfs.iVersion>1 && ((Testvfs )pVfs.pAppData).isNoshm ){
    //  pMethods.xShmUnmap = 0;
    //  pMethods.xShmLock = 0;
    //  pMethods.xShmBarrier = 0;
    //  pMethods.xShmMap = 0;
    //}
  //  pFile.pMethods = pMethods;
  //}

  return rc;
}

/*
** Delete the file located at zPath. If the dirSync argument is true,
** ensure the file-system modifications are synced to disk before
** returning.
*/
static int tvfsDelete(sqlite3_vfs pVfs, string zPath, int dirSync){
  int rc = SQLITE_OK;
  Testvfs p = (Testvfs )pVfs.pAppData;

  if( p.pScript !=null && (p.mask&TESTVFS_DELETE_MASK)!=0 ){
    tvfsExecTcl(p, "xDelete",
        TCL.Tcl_NewStringObj( zPath, -1 ), TCL.Tcl_NewIntObj( dirSync ), null
    );
    tvfsResultCode(p, ref rc);
  }
  if( rc==SQLITE_OK ){
    rc = sqlite3OsDelete(PARENTVFS(pVfs), zPath, dirSync);
  }
  return rc;
}

/*
** Test for access permissions. Return true if the requested permission
** is available, or false otherwise.
*/
static int tvfsAccess(
  sqlite3_vfs pVfs, 
  string zPath, 
  int flags, 
  ref int pResOut
){
  Testvfs p = (Testvfs )pVfs.pAppData;
  if ( p.pScript != null && ( p.mask & TESTVFS_ACCESS_MASK ) != 0 )
  {
    int rc=0;
    string zArg = "";
    if( flags==SQLITE_ACCESS_EXISTS ) zArg = "SQLITE_ACCESS_EXISTS";
    if( flags==SQLITE_ACCESS_READWRITE ) zArg = "SQLITE_ACCESS_READWRITE";
    if( flags==SQLITE_ACCESS_READ ) zArg = "SQLITE_ACCESS_READ";
    tvfsExecTcl(p, "xAccess",
        TCL.Tcl_NewStringObj( zPath, -1 ), TCL.Tcl_NewStringObj( zArg, -1 ), null
    );
    if( tvfsResultCode(p, ref rc) !=0){
      if( rc!=SQLITE_OK ) return rc;
    }else{
      Tcl_Interp interp = p.interp;
      bool bTemp = false;
      if ( !TCL.Tcl_GetBooleanFromObj( null, TCL.Tcl_GetObjResult( interp ), out bTemp ) )
      {
        pResOut = bTemp ? 1 : 0;
        return SQLITE_OK;
      }
    }
  }
  return sqlite3OsAccess( PARENTVFS( pVfs ), zPath, flags, ref pResOut );
}

/*
** Populate buffer zOut with the full canonical pathname corresponding
** to the pathname in zPath. zOut is guaranteed to point to a buffer
** of at least (DEVSYM_MAX_PATHNAME+1) bytes.
*/
static int tvfsFullPathname(
  sqlite3_vfs pVfs, 
  string zPath, 
  int nOut, 
  StringBuilder zOut
){
  Testvfs p = (Testvfs )pVfs.pAppData;
  if ( p.pScript != null && ( p.mask & TESTVFS_FULLPATHNAME_MASK ) != 0 )
  {
    int rc=0;
    tvfsExecTcl(p, "xFullPathname", TCL.Tcl_NewStringObj(zPath, -1),null,null);
    if( tvfsResultCode(p, ref rc) !=0){
      if( rc!=SQLITE_OK ) return rc;
    }
  }
  return sqlite3OsFullPathname(PARENTVFS(pVfs), zPath, nOut, zOut);
}

#if !SQLITE_OMIT_LOAD_EXTENSION
/*
** Open the dynamic library located at zPath and return a handle.
*/
static IntPtr tvfsDlOpen(sqlite3_vfs pVfs, string zPath){
  return sqlite3OsDlOpen(PARENTVFS(pVfs), zPath);
}

/*
** Populate the buffer zErrMsg (size nByte bytes) with a human readable
** utf-8 string describing the most recent error encountered associated 
** with dynamic libraries.
*/
static void tvfsDlError(sqlite3_vfs pVfs, int nByte, string zErrMsg){
  sqlite3OsDlError(PARENTVFS(pVfs), nByte, zErrMsg);
}

/*
** Return a pointer to the symbol zSymbol in the dynamic library pHandle.
*/
static void tvfsDlSym(sqlite3_vfs pVfs, IntPtr p, string zSym){
  sqlite3OsDlSym(PARENTVFS(pVfs), p, ref zSym);
}

/*
** Close the dynamic library handle pHandle.
*/
static void tvfsDlClose( sqlite3_vfs pVfs, IntPtr pHandle )
{
  sqlite3OsDlClose(PARENTVFS(pVfs), pHandle);
}
#endif //* SQLITE_OMIT_LOAD_EXTENSION */

/*
** Populate the buffer pointed to by zBufOut with nByte bytes of 
** random data.
*/
static int tvfsRandomness(sqlite3_vfs pVfs, int nByte, byte[] zBufOut){
  return sqlite3OsRandomness(PARENTVFS(pVfs), nByte, zBufOut);
}

/*
** Sleep for nMicro microseconds. Return the number of microseconds 
** actually slept.
*/
static int tvfsSleep(sqlite3_vfs pVfs, int nMicro){
  return sqlite3OsSleep(PARENTVFS(pVfs), nMicro);
}

/*
** Return the current time as a Julian Day number in pTimeOut.
*/
static int tvfsCurrentTime(sqlite3_vfs pVfs, double pTimeOut){
  return PARENTVFS(pVfs).xCurrentTime(PARENTVFS(pVfs), ref pTimeOut);
}

static int tvfsShmOpen(sqlite3_file pFile){
  Testvfs p;
  int rc = SQLITE_OK;             /* Return code */
  Debugger.Break();//TODO
  //TestvfsBuffer pBuffer;         /* Buffer to open connection to */
  //TestvfsFd pFd;                 /* The testvfs file structure */

  //pFd = tvfsGetFd(pFile);
  //p = (Testvfs )pFd.pVfs.pAppData;
  //Debug.Assert( pFd.pShmId && pFd.pShm==null && pFd.pNext==null );

  ///* Evaluate the Tcl script: 
  //**
  //**   SCRIPT xShmOpen FILENAME
  //*/
  //TCL.Tcl_ResetResult(p.interp);
  //if ( p.pScript != null && ( p.mask & TESTVFS_SHMOPEN_MASK ) != 0 )
  //{
  //  tvfsExecTcl(p, "xShmOpen", TCL.Tcl_NewStringObj(pFd.zFilename, -1), 0, 0);
  //  if( tvfsResultCode(p, ref rc)!=0 ){
  //    if( rc!=SQLITE_OK ) return rc;
  //  }
  //}

  //Debug.Assert( rc==SQLITE_OK );
  //if ( ( p.mask & TESTVFS_SHMOPEN_MASK ) != 0 && tvfsInjectIoerr( p ) )
  //{
  //  return SQLITE_IOERR;
  //}

  ///* Search for a TestvfsBuffer. Create a new one if required. */
  //for(pBuffer=p.pBuffer; pBuffer!=null; pBuffer=pBuffer.pNext){
  //  if( 0==strcmp(pFd.zFilename, pBuffer.zFile) ) break;
  //}
  //if( null==pBuffer ){
  //  int nByte = sizeof(TestvfsBuffer) + strlen(pFd.zFilename) + 1;
  //  pBuffer = (TestvfsBuffer )ckalloc(nByte);
  //  memset(pBuffer, 0, nByte);
  //  pBuffer.zFile = (char )&pBuffer[1];
  //  strcpy(pBuffer.zFile, pFd.zFilename);
  //  pBuffer.pNext = p.pBuffer;
  //  p.pBuffer = pBuffer;
  //}

  ///* Connect the TestvfsBuffer to the new TestvfsShm handle and return. */
  //pFd.pNext = pBuffer.pFile;
  //pBuffer.pFile = pFd;
  //pFd.pShm = pBuffer;
  return SQLITE_OK;
}

static void tvfsAllocPage(TestvfsBuffer p, int iPage, int pgsz){
  Debugger.Break();//TODO
  //Debug.Assert( iPage < TESTVFS_MAX_PAGES );
  //if( p.aPage[iPage]==0 ){
  //  p.aPage[iPage] = (u8 )ckalloc(pgsz);
  //  memset(p.aPage[iPage], 0, pgsz);
  //  p.pgsz = pgsz;
  //}
}

static int tvfsShmMap(
  sqlite3_file pFile,            /* Handle open on database file */
  int iPage,                      /* Page to retrieve */
  int pgsz,                       /* Size of pages */
  int isWrite,                    /* True to extend file if necessary */
  out object pp                   /* OUT: Mapped memory */
){
  int rc = SQLITE_OK;
  Debugger.Break();//TODO
  pp = null;
  //TestvfsFd pFd = tvfsGetFd( pFile );
  //Testvfs p = (Testvfs )(pFd.pVfs.pAppData);

  //if( 0==pFd.pShm ){
  //  rc = tvfsShmOpen(pFile);
  //  if( rc!=SQLITE_OK ){
  //    return rc;
  //  }
  //}

  //if( p.pScript != null && (p.mask&TESTVFS_SHMMAP_MASK )!=0){
  //  Tcl_Obj pArg = TCL.Tcl_NewObj();
  //  Tcl_IncrRefCount(pArg);
  //  Tcl_ListObjAppendElement(p.interp, pArg, TCL.Tcl_NewIntObj(iPage));
  //  Tcl_ListObjAppendElement(p.interp, pArg, TCL.Tcl_NewIntObj(pgsz));
  //  Tcl_ListObjAppendElement(p.interp, pArg, TCL.Tcl_NewIntObj(isWrite));
  //  tvfsExecTcl(p, "xShmMap", 
  //      Tcl_NewStringObj(pFd.pShm.zFile, -1), pFd.pShmId, pArg
  //  );
  //  tvfsResultCode(p, ref rc);
  //  Tcl_DecrRefCount(pArg);
  //}
  //if( rc==SQLITE_OK && (p.mask&TESTVFS_SHMMAP_MASK  )!=0&& tvfsInjectIoerr(p) ){
  //  rc = SQLITE_IOERR;
  //}

  //if( rc==SQLITE_OK && isWrite && !pFd.pShm.aPage[iPage] ){
  //  tvfsAllocPage(pFd.pShm, iPage, pgsz);
  //}
  //pp = pFd.pShm.aPage[iPage];

  return rc;
}


static int tvfsShmLock(
  sqlite3_file pFile,
  int ofst,
  int n,
  int flags
){
  int rc = SQLITE_OK;
  Debugger.Break();//TODO
  //TestvfsFd pFd = tvfsGetFd(pFile);
  //Testvfs p = (Testvfs )(pFd.pVfs.pAppData);
  //int nLock;
  //StringBuilder zLock =new StringBuilder(80);//char zLock[80];

  //if( p.pScript !=null && (p.mask&TESTVFS_SHMLOCK_MASK)!=0 ){
  //  sqlite3_snprintf(sizeof(zLock), zLock, "%d %d", ofst, n);
  //  nLock = strlen(zLock);
  //  if( flags & SQLITE_SHM_LOCK ){
  //    strcpy(&zLock[nLock], " lock");
  //  }else{
  //    strcpy(&zLock[nLock], " unlock");
  //  }
  //  nLock += strlen(&zLock[nLock]);
  //  if( flags & SQLITE_SHM_SHARED ){
  //    strcpy(&zLock[nLock], " shared");
  //  }else{
  //    strcpy(&zLock[nLock], " exclusive");
  //  }
  //  tvfsExecTcl(p, "xShmLock", 
  //      Tcl_NewStringObj(pFd.pShm.zFile, -1), pFd.pShmId,
  //      Tcl_NewStringObj(zLock, -1)
  //  );
  //  tvfsResultCode(p, ref rc);
  //}

  //if( rc==SQLITE_OK && (p.mask&TESTVFS_SHMLOCK_MASK  )!=0&& tvfsInjectIoerr(p) ){
  //  rc = SQLITE_IOERR;
  //}

  //if( rc==SQLITE_OK ){
  //  int isLock = (flags & SQLITE_SHM_LOCK);
  //  int isExcl = (flags & SQLITE_SHM_EXCLUSIVE);
  //  u32 mask = (((1<<n)-1) << ofst);
  //  if( isLock ){
  //    TestvfsFd p2;
  //    for(p2=pFd.pShm.pFile; p2; p2=p2.pNext){
  //      if( p2==pFd ) continue;
  //      if( (p2.excllock&mask) || (isExcl && p2.sharedlock&mask) ){
  //        rc = SQLITE_BUSY;
  //        break;
  //      }
  //    }
  //    if( rc==SQLITE_OK ){
  //      if( isExcl )  pFd.excllock |= mask;
  //      if( null==isExcl ) pFd.sharedlock |= mask;
  //    }
  //  }else{
  //    if( isExcl )  pFd.excllock &= (~mask);
  //    if( null==isExcl ) pFd.sharedlock &= (~mask);
  //  }
  //}

  return rc;
}

static void tvfsShmBarrier(sqlite3_file pFile){
  Debugger.Break();//TODO
  //TestvfsFd pFd = tvfsGetFd(pFile);
  //Testvfs p = (Testvfs )(pFd.pVfs.pAppData);

  //if ( p.pScript != null && ( p.mask & TESTVFS_SHMBARRIER_MASK ) != 0 )
  //{
  //  tvfsExecTcl(p, "xShmBarrier", 
  //      Tcl_NewStringObj(pFd.pShm.zFile, -1), pFd.pShmId, 0
  //  );
  //}
}

static int tvfsShmUnmap(
  sqlite3_file pFile,
  int deleteFlag
){
  int rc = SQLITE_OK;
  Debugger.Break();//TODO
  //TestvfsFd pFd = tvfsGetFd( pFile );
  //Testvfs p = (Testvfs )(pFd.pVfs.pAppData);
  //TestvfsBuffer pBuffer = pFd.pShm;
  //TestvfsFd ppFd;

  //if( null==pBuffer ) return SQLITE_OK;
  //Debug.Assert( pFd.pShmId && pFd.pShm );

  //if ( p.pScript != null && ( p.mask & TESTVFS_SHMCLOSE_MASK ) != 0 )
  //{
  //  tvfsExecTcl(p, "xShmUnmap", 
  //      Tcl_NewStringObj(pFd.pShm.zFile, -1), pFd.pShmId, 0
  //  );
  //  tvfsResultCode(p, ref rc);
  //}

  //for(ppFd=pBuffer.pFile; ppFd!=pFd; ppFd=&((ppFd).pNext));
  //Debug.Assert( (ppFd)==pFd );
  //ppFd = pFd.pNext;
  //pFd.pNext = 0;

  //if( pBuffer.pFile==null ){
  //  int i;
  //  TestvfsBuffer pp;
  //  for(pp=p.pBuffer; pp!=pBuffer; pp=((pp).pNext));
  //  pp = (pp).pNext;
  //  Debugger.Break();//TODO
  //  //for(i=0; pBuffer.aPage[i]!= null; i++){
  //  //  ckfree((char )pBuffer.aPage[i]);
  //  //}
  //  //ckfree((char )pBuffer);
  //}
  //pFd.pShm = null;

  return rc;
}

  enum DB_enum_CMD { 
    CMD_SHM, CMD_DELETE, CMD_FILTER, CMD_IOERR, CMD_SCRIPT, 
    CMD_DEVCHAR, CMD_SECTORSIZE, CMD_FULLERR, CMD_CANTOPENERR
  };
  class TestvfsSubcmd {
    public string zName;
    public DB_enum_CMD eCmd;
    public TestvfsSubcmd (string zName, DB_enum_CMD eCmd){this.zName=zName;this.eCmd=eCmd;}
  }
          class VfsMethod {
        public string zName;
        public int mask;
    public VfsMethod (string zName, int mask){this.zName=zName;this.mask=mask;}
      } 

      class DeviceFlag {
        public string zName;
        public int iValue;
          public DeviceFlag (string zName, int iValue){this.zName=zName;this.iValue=iValue;}
      }

      class _aFlag
      {
        public string zName;
        public int iValue;
        public _aFlag( string zName, int iValue )
        {
          this.zName = zName;
          this.iValue = iValue;
        }
      }

    
static int testvfs_obj_cmd(
  ClientData cd,
  Tcl_Interp interp,
  int objc,
  Tcl_Obj[] objv
){
  Debugger.Break();//TODO
//  Testvfs p = (Testvfs)cd;

// TestvfsSubcmd[] aSubcmd = new TestvfsSubcmd[]  {
//    new TestvfsSubcmd( "shm",         DB_enum_CMD.CMD_SHM         ),
//    new TestvfsSubcmd( "delete",      DB_enum_CMD.CMD_DELETE      ),
//    new TestvfsSubcmd( "filter",      DB_enum_CMD.CMD_FILTER      ),
//    new TestvfsSubcmd( "ioerr",       DB_enum_CMD.CMD_IOERR       ),
//    new TestvfsSubcmd( "fullerr",     DB_enum_CMD.CMD_FULLERR     ),
//    new TestvfsSubcmd( "cantopenerr", DB_enum_CMD.CMD_CANTOPENERR ),
//    new TestvfsSubcmd( "script",      DB_enum_CMD.CMD_SCRIPT      ),
//    new TestvfsSubcmd( "devchar",     DB_enum_CMD.CMD_DEVCHAR     ),
//    new TestvfsSubcmd( "sectorsize",  DB_enum_CMD.CMD_SECTORSIZE  ),
//    new TestvfsSubcmd( 0, 0 )
//  };
//  int i=0;
  
//  if( objc<2 ){
//    TCL.Tcl_WrongNumArgs( interp, 1, objv, "SUBCOMMAND ..." );
//    return TCL.TCL_ERROR;
//  }
//  if ( TCL.Tcl_GetIndexFromObjStruct(
//        interp, objv[1], aSubcmd, aSubcmd.Length, "subcommand", 0, ref i) 
//  ){
//    return TCL.TCL_ERROR;
//  }
//  TCL.Tcl_ResetResult( interp );

//  switch( aSubcmd[i].eCmd ){
//    case DB_enum_CMD.CMD_SHM: {
//      Tcl_Obj pObj;
//      int i;
//      TestvfsBuffer pBuffer;
//      string zName;
//      if( objc!=3 && objc!=4 ){
//        TCL.Tcl_WrongNumArgs( interp, 2, objv, "FILE ?VALUE?" );
//        return TCL.TCL_ERROR;
//      }
//      zName = ckalloc(p.pParent.mxPathname);
//      p.pParent.xFullPathname(
//          p.pParent, TCL.Tcl_GetString(objv[2]), 
//          p.pParent.mxPathname, zName
//      );
//      for(pBuffer=p.pBuffer; pBuffer; pBuffer=pBuffer.pNext){
//        if( 0==strcmp(pBuffer.zFile, zName) ) break;
//      }
//      ckfree(zName);
//      if( null==pBuffer ){
//        TCL.Tcl_AppendResult( interp, "no such file: ", TCL.Tcl_GetString( objv[2] ), 0 );
//        return TCL.TCL_ERROR;
//      }
//      if( objc==4 ){
//        int n;
//        u8 *a = TCL.Tcl_GetByteArrayFromObj(objv[3], &n);
//        int pgsz = pBuffer.pgsz;
//        if( pgsz==0 ) pgsz = 65536;
//        for(i=0; ipgsz<n; i++){
//          int nByte = pgsz;
//          tvfsAllocPage(pBuffer, i, pgsz);
//          if( n-ipgsz<pgsz ){
//            nByte = n;
//          }
//          memcpy(pBuffer.aPage[i], &a[ipgsz], nByte);
//        }
//      }

//      pObj = TCL.Tcl_NewObj();
//      for(i=0; pBuffer.aPage[i]!=null; i++){
//        int pgsz = pBuffer.pgsz;
//        if( pgsz==0 ) pgsz = 65536;
//        TCL.Tcl_AppendObjToObj(pObj, TCL.Tcl_NewByteArrayObj(pBuffer.aPage[i], pgsz));
//      }
//      TCL.Tcl_SetObjResult( interp, pObj );
//      break;
//    }    
//    case DB_enum_CMD.CMD_FILTER: {
//VfsMethod[] vfsmethod = new VfsMethod[] {
//        new VfsMethod( "xShmOpen",      TESTVFS_SHMOPEN_MASK ),
//        new VfsMethod( "xShmLock",      TESTVFS_SHMLOCK_MASK ),
//        new VfsMethod( "xShmBarrier",   TESTVFS_SHMBARRIER_MASK ),
//        new VfsMethod( "xShmUnmap",     TESTVFS_SHMCLOSE_MASK ),
//        new VfsMethod( "xShmMap",       TESTVFS_SHMMAP_MASK ),
//        new VfsMethod( "xSync",         TESTVFS_SYNC_MASK ),
//        new VfsMethod( "xDelete",       TESTVFS_DELETE_MASK ),
//        new VfsMethod( "xWrite",        TESTVFS_WRITE_MASK ),
//        new VfsMethod( "xTruncate",     TESTVFS_TRUNCATE_MASK ),
//        new VfsMethod( "xOpen",         TESTVFS_OPEN_MASK ),
//        new VfsMethod( "xClose",        TESTVFS_CLOSE_MASK ),
//        new VfsMethod( "xAccess",       TESTVFS_ACCESS_MASK ),
//        new VfsMethod( "xFullPathname", TESTVFS_FULLPATHNAME_MASK ),
//};
//      Tcl_Obj[] apElem = null;
//      int nElem = 0;
//      int i;
//      int mask = 0;
//      if( objc!=3 ){
//        TCL.Tcl_WrongNumArgs( interp, 2, objv, "LIST" );
//        return TCL.TCL_ERROR;
//      }
//      if ( TCL.Tcl_ListObjGetElements( interp, objv[2], ref nElem, ref apElem ) )
//      {
//        return TCL.TCL_ERROR;
//      }
//      TCL.Tcl_ResetResult( interp );
//      for(i=0; i<nElem; i++){
//        int iMethod;
//        string zElem = TCL.Tcl_GetString(apElem[i]);
//        for(iMethod=0; iMethod<ArraySize(vfsmethod); iMethod++){
//          if( strcmp(zElem, vfsmethod[iMethod].zName)==0 ){
//            mask |= vfsmethod[iMethod].mask;
//            break;
//          }
//        }
//        if( iMethod==ArraySize(vfsmethod) ){
//          TCL.Tcl_AppendResult( interp, "unknown method: ", zElem, 0 );
//          return TCL.TCL_ERROR;
//        }
//      }
//      p.mask = mask;
//      break;
//    }

//    case DB_enum_CMD.CMD_SCRIPT: {
//      if( objc==3 ){
//        int nByte;
//        if( p.pScript !=null){
//          TCL.Tcl_DecrRefCount( p.pScript );
//          p.pScript = 0;
//        }
//        TCL.Tcl_GetStringFromObj( objv[2], &nByte );
//        if( nByte>0 ){
//          p.pScript = TCL.Tcl_DuplicateObj(objv[2]);
//          TCL.Tcl_IncrRefCount( p.pScript );
//        }
//      }else if( objc!=2 ){
//        TCL.Tcl_WrongNumArgs( interp, 2, objv, "?SCRIPT?" );
//        return TCL.TCL_ERROR;
//      }

//      TCL.Tcl_ResetResult( interp );
//      if( p.pScript !=null) if( p.pScript )TCL.Tcl_SetObjResult(interp, p.pScript);

//      break;
//    }

//    /*
//    ** TESTVFS ioerr ?IFAIL PERSIST?
//    **
//    **   Where IFAIL is an integer and PERSIST is boolean.
//    */
//    case DB_enum_CMD.CMD_CANTOPENERR:
//    case DB_enum_CMD.CMD_IOERR:
//    case DB_enum_CMD.CMD_FULLERR: {
//      TestFaultInject pTest;
//      int iRet;

//      switch( aSubcmd[i].eCmd ){
//        case DB_enum_CMD.CMD_IOERR: pTest = p.ioerr_err; break;
//        case DB_enum_CMD.CMD_FULLERR: pTest = p.full_err; break;
//        case DB_enum_CMD.CMD_CANTOPENERR: pTest = p.cantopen_err; break;
//        default: Debug.Assert(false);
//      }
//      iRet = pTest.nFail;
//      pTest.nFail = 0;
//      pTest.eFault = 0;
//      pTest.iCnt = 0;

//      if( objc==4 ){
//        int iCnt, iPersist;
//        if ( TCL.TCL_OK != TCL.Tcl_GetIntFromObj( interp, objv[2], &iCnt )
//         || TCL.TCL_OK != TCL.Tcl_GetBooleanFromObj( interp, objv[3], &iPersist )
//        ){
//          return TCL.TCL_ERROR;
//        }
//        pTest.eFault = iPersist != 0 ? FAULT_INJECT_PERSISTENT : FAULT_INJECT_TRANSIENT;
//        pTest.iCnt = iCnt;
//      }else if( objc!=2 ){
//        TCL.Tcl_WrongNumArgs( interp, 2, objv, "?CNT PERSIST?" );
//        return TCL.TCL_ERROR;
//      }
//      TCL.Tcl_SetObjResult( interp, TCL.Tcl_NewIntObj( iRet ) );
//      break;
//    }

//    case DB_enum_CMD.CMD_DELETE: {
//      TCL.Tcl_DeleteCommand( interp, TCL.Tcl_GetString( objv[0] ) );
//      break;
//    }

//    case DB_enum_CMD.CMD_DEVCHAR: {
//_aFlag[] aFlag = new _aFlag[] {
//        new _aFlag( "default",               -1 ),
//        new _aFlag( "atomic",                SQLITE_IOCAP_ATOMIC      ),
//        new _aFlag( "atomic512",             SQLITE_IOCAP_ATOMIC512   ),
//        new _aFlag( "atomic1k",              SQLITE_IOCAP_ATOMIC1K    ),
//        new _aFlag( "atomic2k",              SQLITE_IOCAP_ATOMIC2K    ),
//        new _aFlag( "atomic4k",              SQLITE_IOCAP_ATOMIC4K    ),
//        new _aFlag( "atomic8k",              SQLITE_IOCAP_ATOMIC8K    ),
//        new _aFlag( "atomic16k",             SQLITE_IOCAP_ATOMIC16K   ),
//        new _aFlag( "atomic32k",             SQLITE_IOCAP_ATOMIC32K   ),
//        new _aFlag( "atomic64k",             SQLITE_IOCAP_ATOMIC64K   ),
//        new _aFlag( "sequential",            SQLITE_IOCAP_SEQUENTIAL  ),
//        new _aFlag( "safe_append",           SQLITE_IOCAP_SAFE_APPEND ),
//        new _aFlag( "undeletable_when_open", SQLITE_IOCAP_UNDELETABLE_WHEN_OPEN ),
//        new _aFlag( 0, 0 )
//      };
//      Tcl_Obj pRet;
//      int iFlag;

//      if( objc>3 ){
//        Tcl_WrongNumArgs(interp, 2, objv, "?ATTR-LIST?");
//        return TCL.TCL_ERROR;
//      }
//      if( objc==3 ){
//        int j;
//        int iNew = 0;
//        Tcl_Obj[] flags = null;
//        int nFlags = 0;

//        if ( TCL.Tcl_ListObjGetElements( interp, objv[2], ref nFlags, ref flags ) )
//        {
//          return TCL.TCL_ERROR;
//        }

//        for(j=0; j<nFlags; j++){
//          int idx = 0;
//          if( Tcl_GetIndexFromObjStruct(interp, flags[j], aFlag, 
//                aFlag.Length, "flag", 0, ref idx) 
//          ){
//            return TCL.TCL_ERROR;
//          }
//          if( aFlag[idx].iValue<0 && nFlags>1 ){
//            TCL.Tcl_AppendResult( interp, "bad flags: ", TCL.Tcl_GetString( objv[2] ), 0 );
//            return TCL.TCL_ERROR;
//          }
//          iNew |= aFlag[idx].iValue;
//        }

//        p.iDevchar = iNew;
//      }

//      pRet = TCL.Tcl_NewObj();
//      for(iFlag=0; iFlag<aFlag.Length ; iFlag++)//sizeof(aFlag)/sizeof(aFlag[0]); iFlag++)
//      {
//        if( p.iDevchar & aFlag[iFlag].iValue ){
//          TCL.Tcl_ListObjAppendElement(
//              interp, pRet, TCL.Tcl_NewStringObj(aFlag[iFlag].zName, -1)
//          );
//        }
//      }
//      TCL.Tcl_SetObjResult( interp, pRet );

//      break;
//    }

//    case DB_enum_CMD.CMD_SECTORSIZE: {
//      if( objc>3 ){
//        TCL.Tcl_WrongNumArgs( interp, 2, objv, "?VALUE?" );
//        return TCL.TCL_ERROR;
//      }
//      if( objc==3 ){
//        int iNew = 0;
//        if( Tcl_GetIntFromObj(interp, objv[2], ref iNew) ){
//          return TCL.TCL_ERROR;
//        }
//        p.iSectorsize = iNew;
//      }
//      TCL.Tcl_SetObjResult( interp, TCL.Tcl_NewIntObj( p.iSectorsize ) );
//      break;
//    }
//  }
  return TCL.TCL_OK;
}

static void testvfs_obj_del(ClientData cd){
  Testvfs p = (Testvfs)cd;
  if ( p.pScript !=null)
    TCL.Tcl_DecrRefCount( ref p.pScript );
  sqlite3_vfs_unregister(p.pVfs);
  Debugger.Break();//TODO
  //ckfree((char )p.pVfs);
  //ckfree((char )p);
}

/*
** Usage:  testvfs VFSNAME ?SWITCHES?
**
** Switches are:
**
**   -noshm   BOOLEAN             (True to omit shm methods. Default false)
**   -default BOOLEAN             (True to make the vfs default. Default false)
**
** This command creates two things when it is invoked: an SQLite VFS, and
** a Tcl command. Both are named VFSNAME. The VFS is installed. It is not
** installed as the default VFS.
**
** The VFS passes all file I/O calls through to the underlying VFS.
**
** Whenever the xShmMap method of the VFS
** is invoked, the SCRIPT is executed as follows:
**
**   SCRIPT xShmMap    FILENAME ID
**
** The value returned by the invocation of SCRIPT above is interpreted as
** an SQLite error code and returned to SQLite. Either a symbolic 
** "SQLITE_OK" or numeric "0" value may be returned.
**
** The contents of the shared-memory buffer associated with a given file
** may be read and set using the following command:
**
**   VFSNAME shm FILENAME ?NEWVALUE?
**
** When the xShmLock method is invoked by SQLite, the following script is
** run:
**
**   SCRIPT xShmLock    FILENAME ID LOCK
**
** where LOCK is of the form "OFFSET NBYTE lock/unlock shared/exclusive"
*/
static int testvfs_cmd(
  ClientData cd,
  Tcl_Interp interp,
  int objc,
  Tcl_Obj[] objv
){
  Debugger.Break();//TODO
//  sqlite3_vfs tvfs_vfs = new sqlite3_vfs(
//    2,                            /* iVersion */
//    0,                            /* szOsFile */
//    0,                            /* mxPathname */
//    null,                         /* pNext */
//    null,                            /* zName */
//    0,                            /* pAppData */
//    tvfsOpen,                     /* xOpen */
//    tvfsDelete,                   /* xDelete */
//    tvfsAccess,                   /* xAccess */
//    tvfsFullPathname,             /* xFullPathname */
//#if !SQLITE_OMIT_LOAD_EXTENSION
//    tvfsDlOpen,                   /* xDlOpen */
//    tvfsDlError,                  /* xDlError */
//    tvfsDlSym,                    /* xDlSym */
//    tvfsDlClose,                  /* xDlClose */
//#else
//    null,                            /* xDlOpen */
//    null,                            /* xDlError */
//    null,                            /* xDlSym */
//    null,                            /* xDlClose */
//#endif //* SQLITE_OMIT_LOAD_EXTENSION */
//    tvfsRandomness,               /* xRandomness */
//    tvfsSleep,                    /* xSleep */
//    tvfsCurrentTime,              /* xCurrentTime */
//    null,                         /* xGetLastError */
//    null,                          /* xCurrentTimeInt64 */
//    null, null, null
//    );

//  Testvfs p;                     /* New object */
//  sqlite3_vfs pVfs;              /* New VFS */
//  string zVfs;
//  int nByte;                      /* Bytes of space to allocate at p */

//  int i;
//  int isNoshm = 0;                /* True if -noshm is passed */
//  int isDefault = 0;              /* True if -default is passed */
//  int szOsFile = 0;               /* Value passed to -szosfile */
//  int mxPathname = -1;            /* Value passed to -mxpathname */
//  int iVersion = 2;               /* Value passed to -iversion */

//  if( objc<2 || 0!=(objc%2) ) goto bad_args;
//  for(i=2; i<objc; i += 2){
//    int nSwitch;
//    string zSwitch;
//    zSwitch = TCL.Tcl_GetStringFromObj(objv[i], &nSwitch); 

//    if( nSwitch>2 && 0==strncmp("-noshm", zSwitch, nSwitch) ){
//      if ( TCL.Tcl_GetBooleanFromObj( interp, objv[i + 1], &isNoshm ) )
//      {
//        return TCL.TCL_ERROR;
//      }
//    }
//    else if( nSwitch>2 && 0==strncmp("-default", zSwitch, nSwitch) ){
//      if ( TCL.Tcl_GetBooleanFromObj( interp, objv[i + 1], &isDefault ) )
//      {
//        return TCL.TCL_ERROR;
//      }
//    }
//    else if( nSwitch>2 && 0==strncmp("-szosfile", zSwitch, nSwitch) ){
//      if ( TCL.Tcl_GetIntFromObj( interp, objv[i + 1], &szOsFile ) )
//      {
//        return TCL.TCL_ERROR;
//      }
//    }
//    else if( nSwitch>2 && 0==strncmp("-mxpathname", zSwitch, nSwitch) ){
//      if ( TCL.Tcl_GetIntFromObj( interp, objv[i + 1], &mxPathname ) )
//      {
//        return TCL.TCL_ERROR;
//      }
//    }
//    else if( nSwitch>2 && 0==strncmp("-iversion", zSwitch, nSwitch) ){
//      if ( TCL.Tcl_GetIntFromObj( interp, objv[i + 1], &iVersion ) )
//      {
//        return TCL.TCL_ERROR;
//      }
//    }
//    else{
//      goto bad_args;
//    }
//  }

//  if( szOsFile<sizeof(TestvfsFile) ){
//    szOsFile = sizeof(TestvfsFile);
//  }

//  zVfs = TCL.Tcl_GetString(objv[1]);
//  nByte = sizeof(Testvfs) + strlen(zVfs)+1;
//  p = (Testvfs )ckalloc(nByte);
//  memset(p, 0, nByte);
//  p.iDevchar = -1;
//  p.iSectorsize = -1;

//  /* Create the new object command before querying SQLite for a default VFS
//  ** to use for 'real' IO operations. This is because creating the new VFS
//  ** may delete an existing [testvfs] VFS of the same name. If such a VFS
//  ** is currently the default, the new [testvfs] may end up calling the 
//  ** methods of a deleted object.
//  */
//  TCL.Tcl_CreateObjCommand( interp, zVfs, testvfs_obj_cmd, p, testvfs_obj_del );
//  p.pParent = sqlite3_vfs_find("");
//  p.interp = interp;

//  p.zName = (char )&p[1];
//  memcpy(p.zName, zVfs, strlen(zVfs)+1);

//  pVfs = new sqlite3_vfs();//(sqlite3_vfs )ckalloc(sizeof(sqlite3_vfs));
//  tvfs_vfs.CopyTo(pVfs);//memcpy( pVfs, &tvfs_vfs, sizeof( sqlite3_vfs ) );
//  pVfs.pAppData = p;
//  pVfs.iVersion = iVersion;
//  pVfs.zName = p.zName;
//  pVfs.mxPathname = p.pParent.mxPathname;
//  if( mxPathname>=0 && mxPathname<pVfs.mxPathname ){
//    pVfs.mxPathname = mxPathname;
//  }
//  pVfs.szOsFile = szOsFile;
//  p.pVfs = pVfs;
//  p.isNoshm = isNoshm;
//  p.mask = TESTVFS_ALL_MASK;

//  sqlite3_vfs_register(pVfs, isDefault);

//  return TCL.TCL_OK;

// bad_args:
//  TCL.Tcl_WrongNumArgs(interp, 1, objv, "VFSNAME ?-noshm BOOL? ?-default BOOL? ?-mxpathname INT? ?-szosfile INT? ?-iversion INT?");
  return TCL.TCL_ERROR;
}

static int Sqlitetestvfs_Init(Tcl_Interp interp){
  TCL.Tcl_CreateObjCommand( interp, "testvfs", testvfs_cmd, null, null );
  return TCL.TCL_OK;
}
#endif
}
#endif
}
