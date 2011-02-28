namespace Community.CsharpSqlite
{
#if !NO_TCL
  using tcl.lang;
  using Tcl_Interp = tcl.lang.Interp;
  using Tcl_Obj = tcl.lang.TclObject;


  public partial class Sqlite3
  {
    /*
    ** 2007 May 05
    **
    ** The author disclaims copyright to this source code.  In place of
    ** a legal notice, here is a blessing:
    **
    **    May you do good and not evil.
    **    May you find forgiveness for yourself and forgive others.
    **    May you share freely, never taking more than you give.
    **
    *************************************************************************
    ** Code for testing the btree.c module in SQLite.  This code
    ** is not included in the SQLite library.  It is used for automated
    ** testing of the SQLite library.
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  SQLITE_SOURCE_ID: 2009-12-07 16:39:13 1ed88e9d01e9eda5cbc622e7614277f29bcc551c
    **
    **  $Header$
    *************************************************************************
    */
    //#include "btreeInt.h"
    //#include <tcl.h>

    /*
    ** Usage: sqlite3_shared_cache_report
    **
    ** Return a list of file that are shared and the number of
    ** references to each file.
    */
    static int sqlite3BtreeSharedCacheReport(
    object clientData,
    Tcl_Interp interp,
    int objc,
    Tcl_Obj[] objv
    )
    {
#if !SQLITE_OMIT_SHARED_CACHE
extern BtShared *sqlite3SharedCacheList;
BtShared *pBt;
Tcl_Obj *pRet = Tcl_NewObj();
for(pBt=GLOBAL(BtShared*,sqlite3SharedCacheList); pBt; pBt=pBt->pNext){
const char *zFile = sqlite3PagerFilename(pBt->pPager);
Tcl_ListObjAppendElement(interp, pRet, Tcl_NewStringObj(zFile, -1));
Tcl_ListObjAppendElement(interp, pRet, Tcl_NewIntObj(pBt->nRef));
}
Tcl_SetObjResult(interp, pRet);
#endif
      return TCL.TCL_OK;
    }

    /*
    ** Print debugging information about all cursors to standard output.
    */
    static void sqlite3BtreeCursorList( Btree p )
    {
#if SQLITE_DEBUG
      BtCursor pCur;
      BtShared pBt = p.pBt;
      for ( pCur = pBt.pCursor; pCur != null; pCur = pCur.pNext )
      {
        MemPage pPage = pCur.apPage[pCur.iPage];
        string zMode = pCur.wrFlag != 0 ? "rw" : "ro";
        sqlite3DebugPrintf( "CURSOR %p rooted at %4d(%s) currently at %d.%d%s\n",
        pCur, pCur.pgnoRoot, zMode,
        pPage != null ? pPage.pgno : 0, pCur.aiIdx[pCur.iPage],
        ( pCur.eState == CURSOR_VALID ) ? "" : " eof"
        );
      }
#endif
    }
  }
#endif
}
