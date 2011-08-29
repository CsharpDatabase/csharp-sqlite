using System;
using System.Diagnostics;
using System.Text;

using i64 = System.Int64;
using u8 = System.Byte;

namespace Community.CsharpSqlite
{
#if TCLSH
  using tcl.lang;
  using sqlite3_int64 = System.Int64;
  using sqlite_int64 = System.Int64;
  using sqlite3_stmt = Sqlite3.Vdbe;
  using sqlite3_value = Sqlite3.Mem;
  using Tcl_Interp = tcl.lang.Interp;
  using Tcl_Obj = tcl.lang.TclObject;
  using ClientData = System.Object;

  using fuzzer_cost = System.Int32;

  public partial class Sqlite3
  {
    /*
    ** 2011 March 24
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
    ** Code for demonstartion virtual table that generates variations
    ** on an input word at increasing edit distances from the original.
    **
    ** A fuzzer virtual table is created like this:
    **
    **     CREATE VIRTUAL TABLE temp.f USING fuzzer;
    **
    ** The name of the new virtual table in the example above is "f".
    ** Note that all fuzzer virtual tables must be TEMP tables.  The
    ** "temp." prefix in front of the table name is required when the
    ** table is being created.  The "temp." prefix can be omitted when
    ** using the table as long as the name is unambiguous.
    **
    ** Before being used, the fuzzer needs to be programmed by giving it
    ** character transformations and a cost associated with each transformation.
    ** Examples:
    **
    **    INSERT INTO f(cFrom,cTo,Cost) VALUES('','a',100);
    **
    ** The above statement says that the cost of inserting a letter 'a' is
    ** 100.  (All costs are integers.  We recommend that costs be scaled so
    ** that the average cost is around 100.)
    **
    **    INSERT INTO f(cFrom,cTo,Cost) VALUES('b','',87);
    **
    ** The above statement says that the cost of deleting a single letter
    ** 'b' is 87.
    **
    **    INSERT INTO f(cFrom,cTo,Cost) VALUES('o','oe',38);
    **    INSERT INTO f(cFrom,cTo,Cost) VALUES('oe','o',40);
    **
    ** This third example says that the cost of transforming the single
    ** letter "o" into the two-letter sequence "oe" is 38 and that the
    ** cost of transforming "oe" back into "o" is 40.
    **
    ** After all the transformation costs have been set, the fuzzer table
    ** can be queried as follows:
    **
    **    SELECT word, distance FROM f
    **     WHERE word MATCH 'abcdefg'
    **       AND distance<200;
    **
    ** This first query outputs the string "abcdefg" and all strings that
    ** can be derived from that string by appling the specified transformations.
    ** The strings are output together with their total transformation cost
    ** (called "distance") and appear in order of increasing cost.  No string
    ** is output more than once.  If there are multiple ways to transform the
    ** target string into the output string then the lowest cost transform is
    ** the one that is returned.  In the example, the search is limited to 
    ** strings with a total distance of less than 200.
    **
    ** It is important to put some kind of a limit on the fuzzer output.  This
    ** can be either in the form of a LIMIT clause at the end of the query,
    ** or better, a "distance<NNN" constraint where NNN is some number.  The
    ** running time and memory requirement is exponential in the value of NNN 
    ** so you want to make sure that NNN is not too big.  A value of NNN that
    ** is about twice the average transformation cost seems to give good results.
    **
    ** The fuzzer table can be useful for tasks such as spelling correction.
    ** Suppose there is a second table vocabulary(w) where the w column contains
    ** all correctly spelled words.   Let $word be a word you want to look up.
    **
    **   SELECT vocabulary.w FROM f, vocabulary
    **    WHERE f.word MATCH $word
    **      AND f.distance<=200
    **      AND f.word=vocabulary.w
    **    LIMIT 20
    **
    ** The query above gives the 20 closest words to the $word being tested.
    ** (Note that for good performance, the vocubulary.w column should be
    ** indexed.)
    **
    ** A similar query can be used to find all words in the dictionary that
    ** begin with some prefix $prefix:
    **
    **   SELECT vocabulary.w FROM f, vocabulary
    **    WHERE f.word MATCH $prefix
    **      AND f.distance<=200
    **      AND vocabulary.w BETWEEN f.word AND (f.word || x'F7BFBFBF')
    **    LIMIT 50
    **
    ** This last query will show up to 50 words out of the vocabulary that
    ** match or nearly match the $prefix.
    *************************************************************************
    ** Included in SQLite3 port to C#-SQLite; 2008 Noah B Hart
    ** C#-SQLite is an independent reimplementation of the SQLite software library
    **
    ** SQLITE_SOURCE_ID: 2011-06-23 19:49:22 4374b7e83ea0a3fbc3691f9c0c936272862f32f2
    **
    *************************************************************************
    */
    //#include "sqlite3.h"
    //#include <stdlib.h>
    //#include <string.h>
    //#include <Debug.Assert.h>
    //#include <stdio.h>

#if !SQLITE_OMIT_VIRTUALTABLE

    /*
** Forward declaration of objects used by this implementation
*/
    //typedef struct fuzzer_vtab fuzzer_vtab;
    //typedef struct fuzzer_cursor fuzzer_cursor;
    //typedef struct fuzzer_rule fuzzer_rule;
    //typedef struct fuzzer_seen fuzzer_seen;
    //typedef struct fuzzer_stem fuzzer_stem;

    /*
    ** Type of the "cost" of an edit operation.  Might be changed to
    ** "float" or "double" or "sqlite3_int64" in the future.
    */
    //typedef int fuzzer_cost;


    /*
    ** Each transformation rule is stored as an instance of this object.
    ** All rules are kept on a linked list sorted by rCost.
    */
    class fuzzer_rule
    {
      public fuzzer_rule pNext;        /* Next rule in order of increasing rCost */
      public fuzzer_cost rCost;         /* Cost of this transformation */
      public int nFrom, nTo;            /* Length of the zFrom and zTo strings */
      public string zFrom;              /* Transform from */
      public string zTo = "    ";       /* Transform to (extra space appended) */
    };

    /*
    ** A stem object is used to generate variants.  It is also used to record
    ** previously generated outputs.
    **
    ** Every stem is added to a hash table as it is output.  Generation of
    ** duplicate stems is suppressed.
    **
    ** Active stems (those that might generate new outputs) are kepts on a linked
    ** list sorted by increasing cost.  The cost is the sum of rBaseCost and
    ** pRule.rCost.
    */
    class fuzzer_stem
    {
      public string zBasis;             /* Word being fuzzed */
      public int nBasis;                /* Length of the zBasis string */
      public fuzzer_rule pRule;         /* Current rule to apply */
      public int n;                     /* Apply pRule at this character offset */
      public fuzzer_cost rBaseCost;     /* Base cost of getting to zBasis */
      public fuzzer_cost rCostX;        /* Precomputed rBaseCost + pRule.rCost */
      public fuzzer_stem pNext;         /* Next stem in rCost order */
      public fuzzer_stem pHash;         /* Next stem with same hash on zBasis */
    };

    /* 
    ** A fuzzer virtual-table object 
    */
    class fuzzer_vtab : sqlite3_vtab
    {
      //sqlite3_vtab base;         /* Base class - must be first */
      public string zClassName;        /* Name of this class.  Default: "fuzzer" */
      public fuzzer_rule pRule;        /* All active rules in this fuzzer */
      public fuzzer_rule pNewRule;     /* New rules to add when last cursor expires */
      public int nCursor;              /* Number of active cursors */
    };

    //#define FUZZER_HASH  4001    /* Hash table size */
    const int FUZZER_HASH = 4001;
    //#define FUZZER_NQUEUE  20    /* Number of slots on the stem queue */
    const int FUZZER_NQUEUE = 20;

    /* A fuzzer cursor object */
    class fuzzer_cursor : sqlite3_vtab_cursor
    {
      //sqlite3_vtab_cursor base;  /* Base class - must be first */
      public sqlite3_int64 iRowid;     /* The rowid of the current word */
      public new fuzzer_vtab pVtab;    /* The virtual table this cursor belongs to */
      public fuzzer_cost rLimit;       /* Maximum cost of any term */
      public fuzzer_stem pStem;        /* Stem with smallest rCostX */
      public fuzzer_stem pDone;        /* Stems already processed to completion */
      public fuzzer_stem[] aQueue = new fuzzer_stem[FUZZER_NQUEUE];  /* Queue of stems with higher rCostX */
      public int mxQueue;              /* Largest used index in aQueue[] */
      public string zBuf;              /* Temporary use buffer */
      public int nBuf;                 /* Bytes allocated for zBuf */
      public int nStem;                /* Number of stems allocated */
      public fuzzer_rule nullRule = new fuzzer_rule();            /* Null rule used first */
      public fuzzer_stem[] apHash = new fuzzer_stem[FUZZER_HASH]; /* Hash of previously generated terms */
    };

    /* Methods for the fuzzer module */
    static int fuzzerConnect(
      sqlite3 db,
      object pAux,
      int argc, string[] argv,
      out sqlite3_vtab ppVtab,
      out string pzErr
    )
    {
      fuzzer_vtab pNew;
      int n;
      if ( !argv[1].StartsWith( "temp", StringComparison.CurrentCultureIgnoreCase ) )
      {
        pzErr = sqlite3_mprintf( "%s virtual tables must be TEMP", argv[0] );
        ppVtab = null;
        return SQLITE_ERROR;
      }
      //n = strlen(argv[0]) + 1;
      pNew = new fuzzer_vtab();//sqlite3_malloc( sizeof(pNew) + n );
      //if( pNew==0 ) return SQLITE_NOMEM;
      //pNew.zClassName = (char*)&pNew[1];
      pNew.zClassName = argv[0];//memcpy(pNew.zClassName, argv[0], n);
      sqlite3_declare_vtab( db, "CREATE TABLE x(word,distance,cFrom,cTo,cost)" );
      //memset(pNew, 0, sizeof(pNew));
      pzErr = "";
      ppVtab = pNew;
      return SQLITE_OK;
    }
    /* Note that for this virtual table, the xCreate and xConnect
    ** methods are identical. */

    static int fuzzerDisconnect( ref object pVtab )
    {
      fuzzer_vtab p = (fuzzer_vtab)pVtab;
      Debug.Assert( p.nCursor == 0 );
      do
      {
        while ( p.pRule != null )
        {
          fuzzer_rule pRule = p.pRule;
          p.pRule = pRule.pNext;
          pRule = null;//sqlite3_free(pRule);
        }
        p.pRule = p.pNewRule;
        p.pNewRule = null;
      } while ( p.pRule != null );
      pVtab = null;//sqlite3_free(p);
      return SQLITE_OK;
    }
    /* The xDisconnect and xDestroy methods are also the same */

    /*
    ** The two input rule lists are both sorted in order of increasing
    ** cost.  Merge them together into a single list, sorted by cost, and
    ** return a pointer to the head of that list.
    */
    static fuzzer_rule fuzzerMergeRules( fuzzer_rule pA, fuzzer_rule pB )
    {
      fuzzer_rule head = new fuzzer_rule();
      fuzzer_rule pTail;

      pTail = head;
      while ( pA != null && pB != null )
      {
        if ( pA.rCost <= pB.rCost )
        {
          pTail.pNext = pA;
          pTail = pA;
          pA = pA.pNext;
        }
        else
        {
          pTail.pNext = pB;
          pTail = pB;
          pB = pB.pNext;
        }
      }
      if ( pA == null )
      {
        pTail.pNext = pB;
      }
      else
      {
        pTail.pNext = pA;
      }
      return head.pNext;
    }


    /*
    ** Open a new fuzzer cursor.
    */
    static int fuzzerOpen( sqlite3_vtab pVTab, out sqlite3_vtab_cursor ppCursor )
    {
      fuzzer_vtab p = (fuzzer_vtab)pVTab;
      fuzzer_cursor pCur;
      pCur = new fuzzer_cursor();//= sqlite3_malloc( sizeof(pCur) );
      ///if( pCur==0 ) return SQLITE_NOMEM;
      //memset(pCur, 0, sizeof(pCur));
      pCur.pVtab = p;
      ppCursor = pCur;
      if ( p.nCursor == 0 && p.pNewRule != null )
      {
        uint i;
        fuzzer_rule pX;
        fuzzer_rule[] a = new fuzzer_rule[15];
        //for(i=0; i<sizeof(a)/sizeof(a[0]); i++) a[i] = 0;
        while ( ( pX = p.pNewRule ) != null )
        {
          p.pNewRule = pX.pNext;
          pX.pNext = null;
          for ( i = 0; a[i] != null && i < a.Length; i++ )//<sizeof(a)/sizeof(a[0])-1; i++)
          {
            pX = fuzzerMergeRules( a[i], pX );
            a[i] = null;
          }
          a[i] = fuzzerMergeRules( a[i], pX );
        }
        for ( pX = a[0], i = 1; i < a.Length; i++ )//sizeof(a)/sizeof(a[0]); i++)
        {
          pX = fuzzerMergeRules( a[i], pX );
        }
        p.pRule = fuzzerMergeRules( p.pRule, pX );
      }
      p.nCursor++;
      return SQLITE_OK;
    }

    /*
    ** Free all stems in a list.
    */
    static void fuzzerClearStemList( ref fuzzer_stem pStem )
    {
      //while( pStem ){
      //  fuzzer_stem pNext = pStem.pNext;
      //  sqlite3_free(pStem);
      //  pStem = pNext;
      //}
      pStem = null;
    }

    /*
    ** Free up all the memory allocated by a cursor.  Set it rLimit to 0
    ** to indicate that it is at EOF.
    */
    static void fuzzerClearCursor( fuzzer_cursor pCur, int clearHash )
    {
      int i;
      fuzzerClearStemList( ref pCur.pStem );
      fuzzerClearStemList( ref pCur.pDone );
      for ( i = 0; i < FUZZER_NQUEUE; i++ )
        fuzzerClearStemList( ref pCur.aQueue[i] );
      pCur.rLimit = (fuzzer_cost)0;
      if ( clearHash != 0 && pCur.nStem != 0 )
      {
        pCur.mxQueue = 0;
        pCur.pStem = null;
        pCur.pDone = null;
        Array.Clear( pCur.aQueue, 0, pCur.aQueue.Length );//memset(pCur.aQueue, 0, sizeof(pCur.aQueue));
        Array.Clear( pCur.apHash, 0, pCur.apHash.Length );//memset(pCur.apHash, 0, sizeof(pCur.apHash));
      }
      pCur.nStem = 0;
    }

    /*
    ** Close a fuzzer cursor.
    */
    static int fuzzerClose( ref sqlite3_vtab_cursor cur )
    {
      fuzzer_cursor pCur = (fuzzer_cursor)cur;
      fuzzerClearCursor( pCur, 0 );
      //sqlite3_free(pCur.zBuf);
      pCur.pVtab.nCursor--;
      cur = null;//sqlite3_free( pCur );
      return SQLITE_OK;
    }

    /*
    ** Compute the current output term for a fuzzer_stem.
    */
    static int fuzzerRender(
      fuzzer_stem pStem,    /* The stem to be rendered */
      ref string pzBuf,     /* Write results into this buffer.  realloc if needed */
      ref int pnBuf         /* Size of the buffer */
    )
    {
      fuzzer_rule pRule = pStem.pRule;
      int n;
      string z;

      n = pStem.nBasis + pRule.nTo - pRule.nFrom;
      if ( ( pnBuf ) < n + 1 )
      {
        //(*pzBuf) = sqlite3_realloc((*pzBuf), n+100);
        //if( (*pzBuf)==0 ) return SQLITE_NOMEM;
        ( pnBuf ) = n + 100;
      }
      n = pStem.n;
      //z = pzBuf;
      if ( n < 0 )
      {
        z = pStem.zBasis.Substring( 0, pStem.nBasis + 1 );//memcpy(z, pStem.zBasis, pStem.nBasis+1);
      }
      else
      {
        z = pStem.zBasis.Substring( 0, n );//memcpy( z, pStem.zBasis, n );
        if ( pRule.nTo != 0 )
          z += pRule.zTo.Substring( 0, pRule.nTo );//memcpy(&z[n], pRule.zTo, pRule.nTo);
        z += pStem.zBasis.Substring( n + pRule.nFrom, pStem.nBasis - n - pRule.nFrom );
        //memcpy(&z[n+pRule.nTo], &pStem.zBasis[n+pRule.nFrom], pStem.nBasis-n-pRule.nFrom+1);
      }
      pzBuf = z;
      return SQLITE_OK;
    }

    /*
    ** Compute a hash on zBasis.
    */
    static uint fuzzerHash( string z )
    {
      uint h = 0;
      //while( *z ){ h = (h<<3) ^ (h>>29) ^ *(z++); }
      for ( int i = 0; i < z.Length; i++ )
        h = ( h << 3 ) ^ ( h >> 29 ) ^ (byte)z[i];
      return h % FUZZER_HASH;
    }

    /*
    ** Current cost of a stem
    */
    static fuzzer_cost fuzzerCost( fuzzer_stem pStem )
    {
      return pStem.rCostX = pStem.rBaseCost + pStem.pRule.rCost;
    }

#if FALSE
/*
** Print a description of a fuzzer_stem on stderr.
*/
static void fuzzerStemPrint(
  const char *zPrefix,
  fuzzer_stem pStem,
  const char *zSuffix
){
  if( pStem.n<0 ){
    fprintf(stderr, "%s[%s](%d)-.self%s",
       zPrefix,
       pStem.zBasis, pStem.rBaseCost,
       zSuffix
    );
  }else{
    char *zBuf = 0;
    int nBuf = 0;
    if( fuzzerRender(pStem, &zBuf, &nBuf)!=SQLITE_OK ) return;
    fprintf(stderr, "%s[%s](%d)-.{%s}(%d)%s",
      zPrefix,
      pStem.zBasis, pStem.rBaseCost, zBuf, pStem.,
      zSuffix
    );
    sqlite3_free(zBuf);
  }
}
#endif

    /*
** Return 1 if the string to which the cursor is point has already
** been emitted.  Return 0 if not.  Return -1 on a memory allocation
** failures.
*/
    static int fuzzerSeen( fuzzer_cursor pCur, fuzzer_stem pStem )
    {
      uint h;
      fuzzer_stem pLookup;

      if ( fuzzerRender( pStem, ref pCur.zBuf, ref pCur.nBuf ) == SQLITE_NOMEM )
      {
        return -1;
      }
      h = fuzzerHash( pCur.zBuf );
      pLookup = pCur.apHash[h];
      while ( pLookup != null && pLookup.zBasis.CompareTo( pCur.zBuf ) != 0 )
      {
        pLookup = pLookup.pHash;
      }
      return pLookup != null ? 1 : 0;
    }

    /*
    ** Advance a fuzzer_stem to its next value.   Return 0 if there are
    ** no more values that can be generated by this fuzzer_stem.  Return
    ** -1 on a memory allocation failure.
    */
    static int fuzzerAdvance( fuzzer_cursor pCur, fuzzer_stem pStem )
    {
      fuzzer_rule pRule;
      while ( ( pRule = pStem.pRule ) != null )
      {
        while ( pStem.n < pStem.nBasis - pRule.nFrom )
        {
          pStem.n++;
          if ( pRule.nFrom == 0
                  || memcmp( pStem.zBasis.Substring( pStem.n ), pRule.zFrom, pRule.nFrom ) == 0//|| memcmp( &pStem.zBasis[pStem.n], pRule.zFrom, pRule.nFrom ) == 0
          )
          {
            /* Found a rewrite case.  Make sure it is not a duplicate */
            int rc = fuzzerSeen( pCur, pStem );
            if ( rc < 0 )
              return -1;
            if ( rc == 0 )
            {
              fuzzerCost( pStem );
              return 1;
            }
          }
        }
        pStem.n = -1;
        pStem.pRule = pRule.pNext;
        if ( pStem.pRule != null && fuzzerCost( pStem ) > pCur.rLimit )
          pStem.pRule = null;
      }
      return 0;
    }

    /*
    ** The two input stem lists are both sorted in order of increasing
    ** rCostX.  Merge them together into a single list, sorted by rCostX, and
    ** return a pointer to the head of that new list.
    */
    static fuzzer_stem fuzzerMergeStems( fuzzer_stem pA, fuzzer_stem pB )
    {
      fuzzer_stem head = new fuzzer_stem();
      fuzzer_stem pTail;

      pTail = head;
      while ( pA != null && pB != null )
      {
        if ( pA.rCostX <= pB.rCostX )
        {
          pTail.pNext = pA;
          pTail = pA;
          pA = pA.pNext;
        }
        else
        {
          pTail.pNext = pB;
          pTail = pB;
          pB = pB.pNext;
        }
      }
      if ( pA == null )
      {
        pTail.pNext = pB;
      }
      else
      {
        pTail.pNext = pA;
      }
      return head.pNext;
    }

    /*
    ** Load pCur.pStem with the lowest-cost stem.  Return a pointer
    ** to the lowest-cost stem.
    */
    static fuzzer_stem fuzzerLowestCostStem( fuzzer_cursor pCur )
    {
      fuzzer_stem pBest, pX;
      int iBest;
      int i;

      if ( pCur.pStem == null )
      {
        iBest = -1;
        pBest = null;
        for ( i = 0; i <= pCur.mxQueue; i++ )
        {
          pX = pCur.aQueue[i];
          if ( pX == null )
            continue;
          if ( pBest == null || pBest.rCostX > pX.rCostX )
          {
            pBest = pX;
            iBest = i;
          }
        }
        if ( pBest != null )
        {
          pCur.aQueue[iBest] = pBest.pNext;
          pBest.pNext = null;
          pCur.pStem = pBest;
        }
      }
      return pCur.pStem;
    }

    /*
    ** Insert pNew into queue of pending stems.  Then find the stem
    ** with the lowest rCostX and move it into pCur.pStem.
    ** list.  The insert is done such the pNew is in the correct order
    ** according to fuzzer_stem.zBaseCost+fuzzer_stem.pRule.rCost.
    */
    static fuzzer_stem fuzzerInsert( fuzzer_cursor pCur, fuzzer_stem pNew )
    {
      fuzzer_stem pX;
      int i;

      /* If pCur.pStem exists and is greater than pNew, then make pNew
      ** the new pCur.pStem and insert the old pCur.pStem instead.
      */
      if ( ( pX = pCur.pStem ) != null && pX.rCostX > pNew.rCostX )
      {
        pNew.pNext = null;
        pCur.pStem = pNew;
        pNew = pX;
      }

      /* Insert the new value */
      pNew.pNext = null;
      pX = pNew;
      for ( i = 0; i <= pCur.mxQueue; i++ )
      {
        if ( pCur.aQueue[i] != null )
        {
          pX = fuzzerMergeStems( pX, pCur.aQueue[i] );
          pCur.aQueue[i] = null;
        }
        else
        {
          pCur.aQueue[i] = pX;
          break;
        }
      }
      if ( i > pCur.mxQueue )
      {
        if ( i < FUZZER_NQUEUE )
        {
          pCur.mxQueue = i;
          pCur.aQueue[i] = pX;
        }
        else
        {
          Debug.Assert( pCur.mxQueue == FUZZER_NQUEUE - 1 );
          pX = fuzzerMergeStems( pX, pCur.aQueue[FUZZER_NQUEUE - 1] );
          pCur.aQueue[FUZZER_NQUEUE - 1] = pX;
        }
      }
      //for ( i = 0; i <= pCur.mxQueue; i++ )
      //{
      //  if (pCur.aQueue[i] == 0) 
      //     fprintf (stderr, "%d null", i);
      //  else 
      //     fprintf (stderr, "%d %s %d %d",i, pCur.aQueue[i].zBasis, pCur.aQueue[i].n, pCur.aQueue[i].rCostX );
      //}
      //     fprintf (stderr, " (lowest %d )\n", fuzzerLowestCostStem(pCur).rCostX);
      return fuzzerLowestCostStem( pCur );
    }

    /*
    ** Allocate a new fuzzer_stem.  Add it to the hash table but do not
    ** link it into either the pCur.pStem or pCur.pDone lists.
    */
    static fuzzer_stem fuzzerNewStem(
      fuzzer_cursor pCur,
      string zWord,
      fuzzer_cost rBaseCost
    )
    {
      fuzzer_stem pNew;
      uint h;

      pNew = new fuzzer_stem();//= sqlite3_malloc( sizeof(pNew) + strlen(zWord) + 1 );
      //if( pNew==0 ) return 0;
      //memset(pNew, 0, sizeof(pNew));
      //pNew.zBasis = (char*)&pNew[1];
      pNew.nBasis = zWord.Length;//strlen( zWord );
      pNew.zBasis = zWord;//memcpy(pNew.zBasis, zWord, pNew.nBasis+1);
      pNew.pRule = pCur.pVtab.pRule;
      pNew.n = -1;
      pNew.rBaseCost = pNew.rCostX = rBaseCost;
      h = fuzzerHash( pNew.zBasis );
      pNew.pHash = pCur.apHash[h];
      pCur.apHash[h] = pNew;
      pCur.nStem++;
      return pNew;
    }


    /*
    ** Advance a cursor to its next row of output
    */
    static int fuzzerNext( sqlite3_vtab_cursor cur )
    {
      fuzzer_cursor pCur = (fuzzer_cursor)cur;
      int rc;
      fuzzer_stem pStem, pNew;

      pCur.iRowid++;

      /* Use the element the cursor is currently point to to create
      ** a new stem and insert the new stem into the priority queue.
      */
      pStem = pCur.pStem;
      if ( pStem.rCostX > 0 )
      {
        rc = fuzzerRender( pStem, ref pCur.zBuf, ref pCur.nBuf );
        if ( rc == SQLITE_NOMEM )
          return SQLITE_NOMEM;
        pNew = fuzzerNewStem( pCur, pCur.zBuf, pStem.rCostX );
        if ( pNew != null )
        {
          if ( fuzzerAdvance( pCur, pNew ) == 0 )
          {
            pNew.pNext = pCur.pDone;
            pCur.pDone = pNew;
          }
          else
          {
            if ( fuzzerInsert( pCur, pNew ) == pNew )
            {
              return SQLITE_OK;
            }
          }
        }
        else
        {
          return SQLITE_NOMEM;
        }
      }

      /* Adjust the priority queue so that the first element of the
      ** stem list is the next lowest cost word.
      */
      while ( ( pStem = pCur.pStem ) != null )
      {
        if ( fuzzerAdvance( pCur, pStem ) != 0 )
        {
          pCur.pStem = null;
          pStem = fuzzerInsert( pCur, pStem );
          if ( ( rc = fuzzerSeen( pCur, pStem ) ) != 0 )
          {
            if ( rc < 0 )
              return SQLITE_NOMEM;
            continue;
          }
          return SQLITE_OK;  /* New word found */
        }
        pCur.pStem = null;
        pStem.pNext = pCur.pDone;
        pCur.pDone = pStem;
        if ( fuzzerLowestCostStem( pCur ) != null )
        {
          rc = fuzzerSeen( pCur, pCur.pStem );
          if ( rc < 0 )
            return SQLITE_NOMEM;
          if ( rc == 0 )
          {
            return SQLITE_OK;
          }
        }
      }

      /* Reach this point only if queue has been exhausted and there is
      ** nothing left to be output. */
      pCur.rLimit = (fuzzer_cost)0;
      return SQLITE_OK;
    }

    /*
    ** Called to "rewind" a cursor back to the beginning so that
    ** it starts its output over again.  Always called at least once
    ** prior to any fuzzerColumn, fuzzerRowid, or fuzzerEof call.
    */
    static int fuzzerFilter(
      sqlite3_vtab_cursor pVtabCursor,
      int idxNum, string idxStr,
      int argc, sqlite3_value[] argv
    )
    {
      fuzzer_cursor pCur = (fuzzer_cursor)pVtabCursor;
      string zWord = "";
      fuzzer_stem pStem;

      fuzzerClearCursor( pCur, 1 );
      pCur.rLimit = 2147483647;
      if ( idxNum == 1 )
      {
        zWord = (string)sqlite3_value_text( argv[0] );
      }
      else if ( idxNum == 2 )
      {
        pCur.rLimit = (fuzzer_cost)sqlite3_value_int( argv[0] );
      }
      else if ( idxNum == 3 )
      {
        zWord = (string)sqlite3_value_text( argv[0] );
        pCur.rLimit = (fuzzer_cost)sqlite3_value_int( argv[1] );
      }
      if ( zWord == null )
        zWord = "";
      pCur.pStem = pStem = fuzzerNewStem( pCur, zWord, (fuzzer_cost)0 );
      if ( pStem == null )
        return SQLITE_NOMEM;
      pCur.nullRule.pNext = pCur.pVtab.pRule;
      pCur.nullRule.rCost = 0;
      pCur.nullRule.nFrom = 0;
      pCur.nullRule.nTo = 0;
      pCur.nullRule.zFrom = "";
      pStem.pRule = pCur.nullRule;
      pStem.n = pStem.nBasis;
      pCur.iRowid = 1;
      return SQLITE_OK;
    }

    /*
    ** Only the word and distance columns have values.  All other columns
    ** return NULL
    */
    static int fuzzerColumn( sqlite3_vtab_cursor cur, sqlite3_context ctx, int i )
    {
      fuzzer_cursor pCur = (fuzzer_cursor)cur;
      if ( i == 0 )
      {
        /* the "word" column */
        if ( fuzzerRender( pCur.pStem, ref pCur.zBuf, ref pCur.nBuf ) == SQLITE_NOMEM )
        {
          return SQLITE_NOMEM;
        }
        sqlite3_result_text( ctx, pCur.zBuf, -1, SQLITE_TRANSIENT );
      }
      else if ( i == 1 )
      {
        /* the "distance" column */
        sqlite3_result_int( ctx, pCur.pStem.rCostX );
      }
      else
      {
        /* All other columns are NULL */
        sqlite3_result_null( ctx );
      }
      return SQLITE_OK;
    }

    /*
    ** The rowid.
    */
    static int fuzzerRowid( sqlite3_vtab_cursor cur, out sqlite_int64 pRowid )
    {
      fuzzer_cursor pCur = (fuzzer_cursor)cur;
      pRowid = pCur.iRowid;
      return SQLITE_OK;
    }

    /*
    ** When the fuzzer_cursor.rLimit value is 0 or less, that is a signal
    ** that the cursor has nothing more to output.
    */
    static int fuzzerEof( sqlite3_vtab_cursor cur )
    {
      fuzzer_cursor pCur = (fuzzer_cursor)cur;
      return pCur.rLimit <= (fuzzer_cost)0 ? 1 : 0;
    }

    /*
    ** Search for terms of these forms:
    **
    **       word MATCH $str
    **       distance < $value
    **       distance <= $value
    **
    ** The distance< and distance<= are both treated as distance<=.
    ** The query plan number is as follows:
    **
    **   0:    None of the terms above are found
    **   1:    There is a "word MATCH" term with $str in filter.argv[0].
    **   2:    There is a "distance<" term with $value in filter.argv[0].
    **   3:    Both "word MATCH" and "distance<" with $str in argv[0] and
    **         $value in argv[1].
    */
    static int fuzzerBestIndex( sqlite3_vtab tab, ref  sqlite3_index_info pIdxInfo )
    {
      int iPlan = 0;
      int iDistTerm = -1;
      int i;
      sqlite3_index_constraint pConstraint;
      //pConstraint = pIdxInfo.aConstraint;
      for ( i = 0; i < pIdxInfo.nConstraint; i++ )//, pConstraint++)
      {
        pConstraint = pIdxInfo.aConstraint[i];
        if ( pConstraint.usable == false )
          continue;
        if ( ( iPlan & 1 ) == 0
         && pConstraint.iColumn == 0
         && pConstraint.op == SQLITE_INDEX_CONSTRAINT_MATCH
        )
        {
          iPlan |= 1;
          pIdxInfo.aConstraintUsage[i].argvIndex = 1;
          pIdxInfo.aConstraintUsage[i].omit = true;
        }
        if ( ( iPlan & 2 ) == 0
         && pConstraint.iColumn == 1
         && ( pConstraint.op == SQLITE_INDEX_CONSTRAINT_LT
               || pConstraint.op == SQLITE_INDEX_CONSTRAINT_LE )
        )
        {
          iPlan |= 2;
          iDistTerm = i;
        }
      }
      if ( iPlan == 2 )
      {
        pIdxInfo.aConstraintUsage[iDistTerm].argvIndex = 1;
      }
      else if ( iPlan == 3 )
      {
        pIdxInfo.aConstraintUsage[iDistTerm].argvIndex = 2;
      }
      pIdxInfo.idxNum = iPlan;
      if ( pIdxInfo.nOrderBy == 1
       && pIdxInfo.aOrderBy[0].iColumn == 1
       && pIdxInfo.aOrderBy[0].desc == false
      )
      {
        pIdxInfo.orderByConsumed = true;
      }
      pIdxInfo.estimatedCost = (double)10000;

      return SQLITE_OK;
    }

    /*
    ** Disallow all attempts to DELETE or UPDATE.  Only INSERTs are allowed.
    **
    ** On an insert, the cFrom, cTo, and cost columns are used to construct
    ** a new rule.   All other columns are ignored.  The rule is ignored
    ** if cFrom and cTo are identical.  A NULL value for cFrom or cTo is
    ** interpreted as an empty string.  The cost must be positive.
    */
    static int fuzzerUpdate(
      sqlite3_vtab pVTab,
      int argc,
      sqlite3_value[] argv,
      out sqlite_int64 pRowid
    )
    {
      fuzzer_vtab p = (fuzzer_vtab)pVTab;
      fuzzer_rule pRule;
      string zFrom;
      int nFrom;
      string zTo;
      int nTo;
      fuzzer_cost rCost;
      if ( argc != 7 )
      {
        //sqlite3_free( pVTab.zErrMsg );
        pVTab.zErrMsg = sqlite3_mprintf( "cannot delete from a %s virtual table",
                                         p.zClassName );
        pRowid = 0;
        return SQLITE_CONSTRAINT;
      }
      if ( sqlite3_value_type( argv[0] ) != SQLITE_NULL )
      {
        //sqlite3_free( pVTab.zErrMsg );
        pVTab.zErrMsg = sqlite3_mprintf( "cannot update a %s virtual table",
                                         p.zClassName );
        pRowid = 0;
        return SQLITE_CONSTRAINT;
      }
      zFrom = sqlite3_value_text( argv[4] );
      if ( zFrom == null )
        zFrom = "";
      zTo = sqlite3_value_text( argv[5] );
      if ( zTo == null )
        zTo = "";
      if ( zFrom.CompareTo( zTo ) == 0 )//strcmp(zFrom,zTo)==0 )
      {
        /* Silently ignore null transformations */
        pRowid = 0;
        return SQLITE_OK;
      }
      rCost = sqlite3_value_int( argv[6] );
      if ( rCost <= 0 )
      {
        //sqlite3_free(pVTab.zErrMsg);
        pVTab.zErrMsg = sqlite3_mprintf( "cost must be positive" );
        pRowid = 0;
        return SQLITE_CONSTRAINT;
      }
      nFrom = zFrom.Length;//strlen( zFrom );
      nTo = zTo.Length;//strlen(zTo);
      pRule = new fuzzer_rule();//  sqlite3_malloc( sizeof( pRule ) + nFrom + nTo );
      //if ( pRule == null )
      //{
      //  return SQLITE_NOMEM;
      //}
      //pRule.zFrom = pRule.zTo[nTo + 1];
      pRule.nFrom = nFrom;
      pRule.zFrom = zFrom;//memcpy( pRule.zFrom, zFrom, nFrom + 1 );
      pRule.zTo = zTo;//memcpy( pRule.zTo, zTo, nTo + 1 );
      pRule.nTo = nTo;
      pRule.rCost = rCost;
      pRule.pNext = p.pNewRule;
      p.pNewRule = pRule;
      pRowid = 0;
      return SQLITE_OK;
    }

    /*
    ** A virtual table module that provides read-only access to a
    ** Tcl global variable namespace.
    */
    static sqlite3_module fuzzerModule = new sqlite3_module(
      0,                           /* iVersion */
      fuzzerConnect,
      fuzzerConnect,
      fuzzerBestIndex,
      fuzzerDisconnect,
      fuzzerDisconnect,
      fuzzerOpen,                  /* xOpen - open a cursor */
      fuzzerClose,                 /* xClose - close a cursor */
      fuzzerFilter,                /* xFilter - configure scan constraints */
      fuzzerNext,                  /* xNext - advance a cursor */
      fuzzerEof,                   /* xEof - check for end of scan */
      fuzzerColumn,                /* xColumn - read data */
      fuzzerRowid,                 /* xRowid - read data */
      fuzzerUpdate,                /* xUpdate - INSERT */
      null,                        /* xBegin */
      null,                        /* xSync */
      null,                        /* xCommit */
      null,                        /* xRollback */
      null,                        /* xFindMethod */
      null                         /* xRename */
    );

#endif ///* SQLITE_OMIT_VIRTUALTABLE */


    /*
** Register the fuzzer virtual table
*/
    static int fuzzer_register( sqlite3 db )
    {
      int rc = SQLITE_OK;
#if !SQLITE_OMIT_VIRTUALTABLE
      rc = sqlite3_create_module( db, "fuzzer", fuzzerModule, null );
#endif
      return rc;
    }

#if SQLITE_TEST
    //#include <tcl.h>
    /*
    ** Decode a pointer to an sqlite3 object.
    */
    //extern int getDbPointer(Tcl_Interp *interp, const char *zA, sqlite3 *ppDb);

    /*
    ** Register the echo virtual table module.
    */
    static int register_fuzzer_module(
      ClientData clientData, /* Pointer to sqlite3_enable_XXX function */
      Tcl_Interp interp,     /* The TCL interpreter that invoked this command */
      int objc,              /* Number of arguments */
      Tcl_Obj[] objv         /* Command arguments */
    )
    {
      sqlite3 db;
      if ( objc != 2 )
      {
        TCL.Tcl_WrongNumArgs( interp, 1, objv, "DB" );
        return TCL.TCL_ERROR;
      }
      if ( getDbPointer( interp, TCL.Tcl_GetString( objv[1] ), out db ) != 0 )
        return TCL.TCL_ERROR;
      fuzzer_register( db );
      return TCL.TCL_OK;
    }


    /*
    ** Register commands with the TCL interpreter.
    */
    /*
    ** Register commands with the TCL interpreter.
    */
    public static int Sqlitetestfuzzer_Init( Tcl_Interp interp )
    {
      //static struct {
      //   string zName;
      //   Tcl_ObjCmdProc *xProc;
      //   void *clientData;
      //} 
      _aObjCmd[] aObjCmd = new _aObjCmd[] {
     new _aObjCmd( "register_fuzzer_module",   register_fuzzer_module, 0 ),
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
#endif // TCLSH
}
