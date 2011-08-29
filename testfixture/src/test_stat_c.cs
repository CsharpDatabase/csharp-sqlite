using System.Diagnostics;

using u8 = System.Byte;
using u32 = System.UInt32;

namespace Community.CsharpSqlite
{
#if TCLSH
  using tcl.lang;
  using DbPage = Sqlite3.PgHdr;
  using sqlite_int64 = System.Int64;
  using sqlite3_stmt = Sqlite3.Vdbe;
  using sqlite3_value = Sqlite3.Mem;
  using Tcl_CmdInfo = tcl.lang.WrappedCommand;
  using Tcl_Interp = tcl.lang.Interp;
  using Tcl_Obj = tcl.lang.TclObject;
  using ClientData = System.Object;
  using System;
#endif

  public partial class Sqlite3
  {
    /*
    ** 2010 July 12
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
    ** This file contains an implementation of the "dbstat" virtual table.
    **
    ** The dbstat virtual table is used to extract low-level formatting
    ** information from an SQLite database in order to implement the
    ** "sqlite3_analyzer" utility.  See the ../tool/spaceanal.tcl script
    ** for an example implementation.
    *************************************************************************
    **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
    **  C#-SQLite is an independent reimplementation of the SQLite software library
    **
    **  SQLITE_SOURCE_ID: 2011-06-23 19:49:22 4374b7e83ea0a3fbc3691f9c0c936272862f32f2
    **
    **************************************************************************/

    //#include "sqliteInt.h"

#if !SQLITE_OMIT_VIRTUALTABLE

    /*
** Page paths:
** 
**   The value of the 'path' column describes the path taken from the 
**   root-node of the b-tree structure to each page. The value of the 
**   root-node path is '/'.
**
**   The value of the path for the left-most child page of the root of
**   a b-tree is '/000/'. (Btrees store content ordered from left to right
**   so the pages to the left have smaller keys than the pages to the right.)
**   The next to left-most child of the root page is
**   '/001', and so on, each sibling page identified by a 3-digit hex 
**   value. The children of the 451st left-most sibling have paths such
**   as '/1c2/000/, '/1c2/001/' etc.
**
**   Overflow pages are specified by appending a '+' character and a 
**   six-digit hexadecimal value to the path to the cell they are linked
**   from. For example, the three overflow pages in a chain linked from 
**   the left-most cell of the 450th child of the root page are identified
**   by the paths:
**
**      '/1c2/000+000000'         // First page in overflow chain
**      '/1c2/000+000001'         // Second page in overflow chain
**      '/1c2/000+000002'         // Third page in overflow chain
**
**   If the paths are sorted using the BINARY collation sequence, then
**   the overflow pages associated with a cell will appear earlier in the
**   sort-order than its child page:
**
**      '/1c2/000/'               // Left-most child of 451st child of root
*/
    const string VTAB_SCHEMA =
      "CREATE TABLE xx( " +
      "  name       STRING,           /* Name of table or index */" +
      "  path       INTEGER,          /* Path to page from root */" +
      "  pageno     INTEGER,          /* Page number */" +
      "  pagetype   STRING,           /* 'internal', 'leaf' or 'overflow' */" +
      "  ncell      INTEGER,          /* Cells on page (0 for overflow) */" +
      "  payload    INTEGER,          /* Bytes of payload on this page */" +
      "  unused     INTEGER,          /* Bytes of unused space on this page */" +
      "  mx_payload INTEGER           /* Largest payload size of all cells */" +
      ");";

#if FALSE
//#define VTAB_SCHEMA2                                                        \
  "CREATE TABLE yy( "                                                       \
  "  pageno   INTEGER,            /* B-tree page number */"                 \
  "  cellno   INTEGER,            /* Cell number within page */"            \
  "  local    INTEGER,            /* Bytes of content stored locally */"    \
  "  payload  INTEGER,            /* Total cell payload size */"            \
  "  novfl    INTEGER             /* Number of overflow pages */"           \
  ");"
#endif


    //typedef struct StatTable StatTable;
    //typedef struct StatCursor StatCursor;
    //typedef struct StatPage StatPage;
    //typedef struct StatCell StatCell;

    class StatCell
    {
      public int nLocal;                     /* Bytes of local payload */
      public u32 iChildPg;                   /* Child node (or 0 if this is a leaf) */
      public int nOvfl;                      /* Entries in aOvfl[] */
      public u32[] aOvfl;                    /* Array of overflow page numbers */
      public int nLastOvfl;                  /* Bytes of payload on final overflow page */
      public int iOvfl;                      /* Iterates through aOvfl[] */
    };

    class StatPage
    {
      public u32 iPgno;
      public DbPage pPg;
      public int iCell;

      public string zPath;                    /* Path to this page */

      /* Variables populated by statDecodePage(): */
      public u8 flags;                       /* Copy of flags byte */
      public int nCell;                      /* Number of cells on page */
      public int nUnused;                    /* Number of unused bytes on page */
      public StatCell[] aCell;               /* Array of parsed cells */
      public u32 iRightChildPg;              /* Right-child page number (or 0) */
      public int nMxPayload;                 /* Largest payload of any cell on this page */
    };

    class StatCursor : sqlite3_vtab_cursor
    {
      //sqlite3_vtab_cursor base;
      public sqlite3_stmt pStmt;             /* Iterates through set of root pages */
      public int isEof;                      /* After pStmt has returned SQLITE_DONE */

      public StatPage[] aPage = new StatPage[32];
      public int iPage;                      /* Current entry in aPage[] */

      /* Values to return. */
      public string zName;                    /* Value of 'name' column */
      public string zPath;                    /* Value of 'path' column */
      public u32 iPageno;                    /* Value of 'pageno' column */
      public string zPagetype;                /* Value of 'pagetype' column */
      public int nCell;                      /* Value of 'ncell' column */
      public int nPayload;                   /* Value of 'payload' column */
      public int nUnused;                    /* Value of 'unused' column */
      public int nMxPayload;                 /* Value of 'mx_payload' column */
    };

    class StatTable : sqlite3_vtab
    {
      //sqlite3_vtab base;
      public sqlite3 db;
    };

    //#if !get2byte
    //# define get2byte(x)   ((x)[0]<<8 | (x)[1])
    //#endif

    /*
    ** Connect to or create a statvfs virtual table.
    */
    static int statConnect(
      sqlite3 db,
      object pAux,
      int argc,
      string[] argv,
      out sqlite3_vtab ppVtab,
      out string pzErr
    )
    {
      StatTable pTab;

      pTab = new StatTable();//(StatTable )sqlite3_malloc(sizeof(StatTable));
      //memset(pTab, 0, sizeof(StatTable));
      pTab.db = db;

      sqlite3_declare_vtab( db, VTAB_SCHEMA );
      ppVtab = pTab;
      pzErr = "";
      return SQLITE_OK;
    }

    /*
    ** Disconnect from or destroy a statvfs virtual table.
    */
    static int statDisconnect( ref object pVtab )
    {
      pVtab = null;//sqlite3_free( pVtab );
      return SQLITE_OK;
    }

    /*
    ** There is no "best-index". This virtual table always does a linear
    ** scan of the binary VFS log file.
    */
    static int statBestIndex( sqlite3_vtab tab, ref sqlite3_index_info pIdxInfo )
    {

      /* Records are always returned in ascending order of (name, path). 
      ** If this will satisfy the client, set the orderByConsumed flag so that 
      ** SQLite does not do an external sort.
      */
      if ( ( pIdxInfo.nOrderBy == 1
         && pIdxInfo.aOrderBy[0].iColumn == 0
         && pIdxInfo.aOrderBy[0].desc == false
         ) ||
          ( pIdxInfo.nOrderBy == 2
         && pIdxInfo.aOrderBy[0].iColumn == 0
         && pIdxInfo.aOrderBy[0].desc == false
         && pIdxInfo.aOrderBy[1].iColumn == 1
         && pIdxInfo.aOrderBy[1].desc == false
         )
      )
      {
        pIdxInfo.orderByConsumed = true;
      }

      pIdxInfo.estimatedCost = 10.0;
      return SQLITE_OK;
    }

    /*
    ** Open a new statvfs cursor.
    */
    static int statOpen( sqlite3_vtab pVTab, out sqlite3_vtab_cursor ppCursor )
    {
      StatTable pTab = (StatTable)pVTab;
      StatCursor pCsr;
      int rc;

      pCsr = new StatCursor();//(StatCursor )sqlite3_malloc(sizeof(StatCursor));
      //memset(pCsr, 0, sizeof(StatCursor));
      pCsr.pVtab = pVTab;

      rc = sqlite3_prepare_v2( pTab.db,
          "SELECT 'sqlite_master' AS name, 1 AS rootpage, 'table' AS type" +
          "  UNION ALL  " +
          "SELECT name, rootpage, type FROM sqlite_master WHERE rootpage!=0" +
          "  ORDER BY name", -1,
          ref pCsr.pStmt, 0
      );
      if ( rc != SQLITE_OK )
      {
        pCsr = null;//sqlite3_free( pCsr );
        ppCursor = null;
        return rc;
      }

      ppCursor = (sqlite3_vtab_cursor)pCsr;
      return SQLITE_OK;
    }

    static void statClearPage( ref StatPage p )
    {
      int i;
      if ( p != null && p.aCell != null )
      {
        for ( i = 0; i < p.nCell; i++ )
        {
          p.aCell[i].aOvfl = null;//sqlite3_free( p.aCell[i].aOvfl );
        }
        sqlite3PagerUnref( p.pPg );
        //  sqlite3_free( p.aCell );
        // sqlite3_free( p.zPath );
      }
      p = new StatPage();//memset( p, 0, sizeof( StatPage ) );
    }

    static void statResetCsr( StatCursor pCsr )
    {
      int i;
      sqlite3_reset( pCsr.pStmt );
      for ( i = 0; i < ArraySize( pCsr.aPage ); i++ )
      {
        statClearPage( ref pCsr.aPage[i] );
      }
      pCsr.iPage = 0;
      //sqlite3_free(pCsr.zPath);
      pCsr.zPath = null;
    }

    /*
    ** Close a statvfs cursor.
    */
    static int statClose( ref sqlite3_vtab_cursor pCursor )
    {
      StatCursor pCsr = (StatCursor)pCursor;
      statResetCsr( pCsr );
      sqlite3_finalize( pCsr.pStmt );
      pCsr = null;//sqlite3_free( pCsr );
      return SQLITE_OK;
    }

    static void getLocalPayload(
      int nUsable,                    /* Usable bytes per page */
      u8 flags,                       /* Page flags */
      int nTotal,                     /* Total record (payload) size */
      out int pnLocal                 /* OUT: Bytes stored locally */
    )
    {
      int nLocal;
      int nMinLocal;
      int nMaxLocal;

      if ( flags == 0x0D )
      {              /* Table leaf node */
        nMinLocal = ( nUsable - 12 ) * 32 / 255 - 23;
        nMaxLocal = nUsable - 35;
      }
      else
      {                          /* Index interior and leaf nodes */
        nMinLocal = ( nUsable - 12 ) * 32 / 255 - 23;
        nMaxLocal = ( nUsable - 12 ) * 64 / 255 - 23;
      }

      nLocal = nMinLocal + ( nTotal - nMinLocal ) % ( nUsable - 4 );
      if ( nLocal > nMaxLocal )
        nLocal = nMinLocal;
      pnLocal = nLocal;
    }

    static int statDecodePage( Btree pBt, StatPage p )
    {
      int nUnused;
      int iOff;
      int nHdr;
      int isLeaf;

      u8[] aData = sqlite3PagerGetData( p.pPg );
      u8[] aHdr = new byte[p.iPgno == 1 ? aData.Length - 100 : aData.Length];
      Buffer.BlockCopy( aData, p.iPgno == 1 ? 100 : 0, aHdr, 0, aHdr.Length );

      p.flags = aHdr[0];
      p.nCell = get2byte( aHdr, 3 );
      p.nMxPayload = 0;

      isLeaf = ( p.flags == 0x0A || p.flags == 0x0D ) ? 1 : 0;
      nHdr = 12 - isLeaf * 4 + ( ( p.iPgno == 1 ) ? 1 : 0 ) * 100;

      nUnused = get2byte( aHdr, 5 ) - nHdr - 2 * p.nCell;
      nUnused += (int)aHdr[7];
      iOff = get2byte( aHdr, 1 );
      while ( iOff != 0 )
      {
        nUnused += get2byte( aData, iOff + 2 );
        iOff = get2byte( aData, iOff );
      }
      p.nUnused = nUnused;
      p.iRightChildPg = isLeaf != 0 ? 0 : sqlite3Get4byte( aHdr, 8 );

      if ( p.nCell != 0 )
      {
        int i;                        /* Used to iterate through cells */
        int nUsable = sqlite3BtreeGetPageSize( pBt ) - sqlite3BtreeGetReserve( pBt );

        p.aCell = new StatCell[p.nCell + 1];//    sqlite3_malloc( ( p.nCell + 1 ) * sizeof( StatCell ) );
        //memset(p.aCell, 0, (p.nCell+1) * sizeof(StatCell));

        for ( i = 0; i < p.nCell; i++ )
        {
          p.aCell[i] = new StatCell();
          StatCell pCell = p.aCell[i];

          iOff = get2byte( aData, nHdr + i * 2 );
          if ( 0 == isLeaf )
          {
            pCell.iChildPg = sqlite3Get4byte( aData, iOff );
            iOff += 4;
          }
          if ( p.flags == 0x05 )
          {
            /* A table interior node. nPayload==0. */
          }
          else
          {
            u32 nPayload;             /* Bytes of payload total (local+overflow) */
            int nLocal;               /* Bytes of payload stored locally */
            iOff += getVarint32( aData, iOff, out nPayload );
            if ( p.flags == 0x0D )
            {
              ulong dummy;
              iOff += sqlite3GetVarint( aData, iOff, out dummy );
            }
            if ( nPayload > p.nMxPayload )
              p.nMxPayload = (int)nPayload;
            getLocalPayload( nUsable, p.flags, (int)nPayload, out nLocal );
            pCell.nLocal = nLocal;
            Debug.Assert( nPayload >= nLocal );
            Debug.Assert( nLocal <= ( nUsable - 35 ) );
            if ( nPayload > nLocal )
            {
              int j;
              int nOvfl = (int)( ( nPayload - nLocal ) + nUsable - 4 - 1 ) / ( nUsable - 4 );
              pCell.nLastOvfl = (int)( nPayload - nLocal ) - ( nOvfl - 1 ) * ( nUsable - 4 );
              pCell.nOvfl = nOvfl;
              pCell.aOvfl = new uint[nOvfl];//sqlite3_malloc(sizeof(u32)*nOvfl);
              pCell.aOvfl[0] = sqlite3Get4byte( aData, iOff + nLocal );
              for ( j = 1; j < nOvfl; j++ )
              {
                int rc;
                u32 iPrev = pCell.aOvfl[j - 1];
                DbPage pPg = null;
                rc = sqlite3PagerGet( sqlite3BtreePager( pBt ), iPrev, ref pPg );
                if ( rc != SQLITE_OK )
                {
                  Debug.Assert( pPg == null );
                  return rc;
                }
                pCell.aOvfl[j] = sqlite3Get4byte( sqlite3PagerGetData( pPg ) );
                sqlite3PagerUnref( pPg );
              }
            }
          }
        }
      }

      return SQLITE_OK;
    }

    /*
    ** Move a statvfs cursor to the next entry in the file.
    */
    static int statNext( sqlite3_vtab_cursor pCursor )
    {
      int rc = 0;
      int nPayload;
      StatCursor pCsr = (StatCursor)pCursor;
      StatTable pTab = (StatTable)pCursor.pVtab;
      Btree pBt = pTab.db.aDb[0].pBt;
      Pager pPager = sqlite3BtreePager( pBt );

      //sqlite3_free(pCsr.zPath);
      pCsr.zPath = null;

      if ( pCsr.aPage[0].pPg == null )
      {
        rc = sqlite3_step( pCsr.pStmt );
        if ( rc == SQLITE_ROW )
        {
          u32 nPage;
          u32 iRoot = (u32)sqlite3_column_int64( pCsr.pStmt, 1 );
          sqlite3PagerPagecount( pPager, out nPage );
          if ( nPage == 0 )
          {
            pCsr.isEof = 1;
            return sqlite3_reset( pCsr.pStmt );
          }
          rc = sqlite3PagerGet( pPager, iRoot, ref pCsr.aPage[0].pPg );
          pCsr.aPage[0].iPgno = iRoot;
          pCsr.aPage[0].iCell = 0;
          pCsr.aPage[0].zPath = sqlite3_mprintf( "/" );
          pCsr.iPage = 0;
        }
        else
        {
          pCsr.isEof = 1;
          return sqlite3_reset( pCsr.pStmt );
        }
      }
      else
      {

        /* Page p itself has already been visited. */
        StatPage p = pCsr.aPage[pCsr.iPage];
        StatPage p1 = pCsr.aPage[pCsr.iPage + 1];

        while ( p.iCell < p.nCell )
        {
          StatCell pCell = p.aCell[p.iCell];
          if ( pCell.iOvfl < pCell.nOvfl )
          {
            int nUsable = sqlite3BtreeGetPageSize( pBt ) - sqlite3BtreeGetReserve( pBt );
            pCsr.zName = sqlite3_column_text( pCsr.pStmt, 0 );
            pCsr.iPageno = pCell.aOvfl[pCell.iOvfl];
            pCsr.zPagetype = "overflow";
            pCsr.nCell = 0;
            pCsr.nMxPayload = 0;
            pCsr.zPath = sqlite3_mprintf(
                "%s%.3x+%.6x", p.zPath, p.iCell, pCell.iOvfl
            );
            if ( pCell.iOvfl < pCell.nOvfl - 1 )
            {
              pCsr.nUnused = 0;
              pCsr.nPayload = nUsable - 4;
            }
            else
            {
              pCsr.nPayload = pCell.nLastOvfl;
              pCsr.nUnused = nUsable - 4 - pCsr.nPayload;
            }
            pCell.iOvfl++;
            return SQLITE_OK;
          }
          if ( p.iRightChildPg != 0 )
            break;
          p.iCell++;
        }

        while ( 0 == p.iRightChildPg || p.iCell > p.nCell )
        {
          statClearPage( ref p );
          pCsr.aPage[pCsr.iPage] = p;
          if ( pCsr.iPage == 0 )
            return statNext( pCursor );
          pCsr.iPage--;
          p = pCsr.aPage[pCsr.iPage];
          if ( pCsr.aPage[pCsr.iPage + 1] == null )
            pCsr.aPage[pCsr.iPage + 1] = new StatPage();
          p1 = pCsr.aPage[pCsr.iPage + 1];
        }
        pCsr.iPage++;
        Debug.Assert( p == pCsr.aPage[pCsr.iPage - 1] );

        if ( p.iCell == p.nCell )
        {
          p1.iPgno = p.iRightChildPg;
        }
        else
        {
          p1.iPgno = p.aCell[p.iCell].iChildPg;
        }
        rc = sqlite3PagerGet( pPager, p1.iPgno, ref p1.pPg );
        p1.iCell = 0;
        p1.zPath = sqlite3_mprintf( "%s%.3x/", p.zPath, p.iCell );
        p.iCell++;
      }


      /* Populate the StatCursor fields with the values to be returned
      ** by the xColumn() and xRowid() methods.
      */
      if ( rc == SQLITE_OK )
      {
        int i;
        StatPage p = pCsr.aPage[pCsr.iPage];
        pCsr.zName = sqlite3_column_text( pCsr.pStmt, 0 );
        pCsr.iPageno = p.iPgno;

        statDecodePage( pBt, p );

        switch ( p.flags )
        {
          case 0x05:             /* table internal */
          case 0x02:             /* index internal */
            pCsr.zPagetype = "internal";
            break;
          case 0x0D:             /* table leaf */
          case 0x0A:             /* index leaf */
            pCsr.zPagetype = "leaf";
            break;
          default:
            pCsr.zPagetype = "corrupted";
            break;
        }
        pCsr.nCell = p.nCell;
        pCsr.nUnused = p.nUnused;
        pCsr.nMxPayload = p.nMxPayload;
        pCsr.zPath = sqlite3_mprintf( "%s", p.zPath );
        nPayload = 0;
        for ( i = 0; i < p.nCell; i++ )
        {
          nPayload += p.aCell[i].nLocal;
        }
        pCsr.nPayload = nPayload;
      }

      return rc;
    }

    static int statEof( sqlite3_vtab_cursor pCursor )
    {
      StatCursor pCsr = (StatCursor)pCursor;
      return pCsr.isEof;
    }

    static int statFilter(
      sqlite3_vtab_cursor pCursor,
      int idxNum, string idxStr,
      int argc, sqlite3_value[] argv
    )
    {
      StatCursor pCsr = (StatCursor)pCursor;

      statResetCsr( pCsr );
      return statNext( pCursor );
    }

    static int statColumn(
      sqlite3_vtab_cursor pCursor,
      sqlite3_context ctx,
      int i
    )
    {
      StatCursor pCsr = (StatCursor)pCursor;
      switch ( i )
      {
        case 0:            /* name */
          sqlite3_result_text( ctx, pCsr.zName, -1, SQLITE_STATIC );
          break;
        case 1:            /* path */
          sqlite3_result_text( ctx, pCsr.zPath, -1, SQLITE_TRANSIENT );
          break;
        case 2:            /* pageno */
          sqlite3_result_int64( ctx, pCsr.iPageno );
          break;
        case 3:            /* pagetype */
          sqlite3_result_text( ctx, pCsr.zPagetype, -1, SQLITE_STATIC );
          break;
        case 4:            /* ncell */
          sqlite3_result_int( ctx, pCsr.nCell );
          break;
        case 5:            /* payload */
          sqlite3_result_int( ctx, pCsr.nPayload );
          break;
        case 6:            /* unused */
          sqlite3_result_int( ctx, pCsr.nUnused );
          break;
        case 7:            /* mx_payload */
          sqlite3_result_int( ctx, pCsr.nMxPayload );
          break;
      }
      return SQLITE_OK;
    }

    static int statRowid( sqlite3_vtab_cursor pCursor, out sqlite_int64 pRowid )
    {
      StatCursor pCsr = (StatCursor)pCursor;
      pRowid = pCsr.iPageno;
      return SQLITE_OK;
    }

    static sqlite3_module dbstat_module = new sqlite3_module(
      0,                            /* iVersion */
        statConnect,                  /* xCreate */
        statConnect,                  /* xConnect */
        statBestIndex,                /* xBestIndex */
        statDisconnect,               /* xDisconnect */
        statDisconnect,               /* xDestroy */
        statOpen,                     /* xOpen - open a cursor */
        statClose,                    /* xClose - close a cursor */
        statFilter,                   /* xFilter - configure scan constraints */
        statNext,                     /* xNext - advance a cursor */
        statEof,                      /* xEof - check for end of scan */
        statColumn,                   /* xColumn - read data */
        statRowid,                    /* xRowid - read data */
        null,                            /* xUpdate */
        null,                            /* xBegin */
        null,                            /* xSync */
        null,                            /* xCommit */
        null,                            /* xRollback */
        null,                            /* xFindMethod */
        null                             /* xRename */
      );

    static int sqlite3_dbstat_register( sqlite3 db )
    {
      sqlite3_create_module( db, "dbstat", dbstat_module, 0 );
      return SQLITE_OK;
    }

#endif

#if SQLITE_TEST
    //#include <tcl.h>

    static int test_dbstat(
      object clientData,
      Tcl_Interp interp,
      int objc,
      Tcl_Obj[] objv
    )
    {
#if SQLITE_OMIT_VIRTUALTABLE
  Tcl_AppendResult(interp, "dbstat not available because of "
                           "SQLITE_OMIT_VIRTUALTABLE", (void*)0);
  return TCL.TCL_ERROR;
#else
      //sqlite3 db;
      string zDb;
      Tcl_CmdInfo cmdInfo;

      if ( objc != 2 )
      {
        TCL.Tcl_WrongNumArgs( interp, 1, objv, "DB" );
        return TCL.TCL_ERROR;
      }

      zDb = TCL.Tcl_GetString( objv[1] );
      if ( !TCL.Tcl_GetCommandInfo( interp, zDb, out cmdInfo ) )
      {
        sqlite3 db = ( (SqliteDb)cmdInfo.objClientData ).db;
        sqlite3_dbstat_register( db );
      }
      return TCL.TCL_OK;
#endif
    }

    public static int SqlitetestStat_Init( Tcl_Interp interp )
    {
      TCL.Tcl_CreateObjCommand( interp, "register_dbstat_vtab", test_dbstat, null, null );
      return TCL.TCL_OK;
    }
#endif
  }
}
