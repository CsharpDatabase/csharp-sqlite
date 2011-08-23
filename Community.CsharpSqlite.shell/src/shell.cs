using System;
using System.Diagnostics;
using System.IO;
using System.Text;

using Community.CsharpSqlite;
using dxCallback = Community.CsharpSqlite.Sqlite3.dxCallback;
using sqlite3 = Community.CsharpSqlite.Sqlite3.sqlite3;
using sqlite3_backup = Community.CsharpSqlite.Sqlite3.sqlite3_backup;
using sqlite3_context = Community.CsharpSqlite.Sqlite3.sqlite3_context;
using sqlite3_int64 = System.Int64;
using sqlite3_stmt = Community.CsharpSqlite.Sqlite3.Vdbe;
using sqlite3_value = Community.CsharpSqlite.Sqlite3.Mem;
using va_list = System.Object;

class Shell
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
  ** This file contains code to implement the "sqlite" command line
  ** utility for accessing SQLite databases.
  **
  ** $Id: shell.c,v 1.201 2009/02/04 22:46:47 drh Exp $
  **
  *************************************************************************
  **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
  **  C#-SQLite is an independent reimplementation of the SQLite software library 
  **
  *************************************************************************
  */
  //#if (_WIN32) || (WIN32)
  ///* This needs to come before any includes for MSVC compiler */
  //#define _CRT_SECURE_NO_WARNINGS
  //#endif

  //#include <stdlib.h>
  //#include <string.h>
  //#include <stdio.h>
  //#include <Debug.Assert.h>
  //#include "sqlite3.h"
  //#include <ctype.h>
  //#include <stdarg.h>

  //#if !(_WIN32) && !(WIN32) && !(__OS2__)
  //# include <signal.h>
  //# if !(__RTP__) && !(_WRS_KERNEL)
  //#  include <pwd.h>
  //# endif
  //# include <unistd.h>
  //# include <sys/types.h>
  //#endif

  //#if __OS2__
  //# include <unistd.h>
  //#endif

#if (HAVE_READLINE) //&& HAVE_READLINE==1
/# include <readline/readline.h>
/# include <readline/history.h>
#else
  //# define readline(p) local_getline(p,stdin)
  static string readline( string p )
  {
    return local_getline( p, stdin );
  }

  //# define add_history(X)
  //# define read_history(X)
  //# define write_history(X)
  //# define stifle_history(X)
  static void stifle_history( int x )
  {
  }
  static void write_history( string s )
  {
  }
#endif

#if  (_WIN32) ||  (WIN32)
  //# include <io.h>
  //#define isatty(h) _isatty(h)
  //#define access(f,m) _access((f),(m))
#else
/* Make sure isatty() has a prototype.
*/
extern int isatty();
#endif

#if (_WIN32_WCE)
/* Windows CE (arm-wince-mingw32ce-gcc) does not provide isatty()
* thus we always assume that we have a console. That can be
* overridden with the -batch command line option.
*/
/#define isatty(x) 1
#endif

#if !(_WIN32) && !(WIN32) && !(__OS2__) && !(__RTP__) && !(_WRS_KERNEL)
/#include <sys/time.h>
/#include <sys/resource.h>

/* Saved resource information for the beginning of an operation */
static struct rusage sBegin;

/* True if the timer is enabled */
static int enableTimer = 0;

/*
** Begin timing an operation
*/
static void beginTimer(void){
if( enableTimer ){
getrusage(RUSAGE_SELF, &sBegin);
}
}

/* Return the difference of two time_structs in seconds */
static double timeDiff(struct timeval *pStart, struct timeval *pEnd){
return (pEnd.tv_usec - pStart.tv_usec)*0.000001 + 
(double)(pEnd.tv_sec - pStart.tv_sec);
}

/*
** Print the timing results.
*/
static void endTimer(void){
if( enableTimer ){
struct rusage sEnd;
getrusage(RUSAGE_SELF, &sEnd);
printf("CPU Time: user %f sys %f\n",
timeDiff(&sBegin.ru_utime, &sEnd.ru_utime),
timeDiff(&sBegin.ru_stime, &sEnd.ru_stime));
}
}
/#define BEGIN_TIMER beginTimer()
/#define END_TIMER endTimer()
/#define HAS_TIMER 1
#else
  //#define BEGIN_TIMER 
  //#define END_TIMER
  //#define HAS_TIMER 0
  const bool HAS_TIMER = false;
#endif

  /*
** Used to prevent warnings about unused parameters
*/
  //#define UNUSED_PARAMETER(x) (void)(x)
  static void UNUSED_PARAMETER<T>( T x )
  {
  }

  /*
  ** If the following flag is set, then command execution stops
  ** at an error if we are not interactive.
  */
  static bool bail_on_error = false;

  /*
  ** Threat stdin as an interactive input if the following variable
  ** is true.  Otherwise, assume stdin is connected to a file or pipe.
  */
  static bool stdin_is_interactive = true;

  /*
  ** The following is the open SQLite database.  We make a pointer
  ** to this database a static variable so that it can be accessed
  ** by the SIGINT handler to interrupt database processing.
  */
  static Sqlite3.sqlite3 db = null;

  /*
  ** True if an interrupt (Control-C) has been received.
  */
  static volatile bool seenInterrupt = false;

  /*
  ** This is the name of our program. It is set in main(), used
  ** in a number of other places, mostly for error messages.
  */
  static string Argv0;

  /*
  ** Prompt strings. Initialized in main. Settable with
  **   .prompt main continue
  */
  static string mainPrompt = "";//[20];     /* First line prompt. default: "C#-sqlite> "*/
  static string continuePrompt = "";//[20]; /* Continuation prompt. default: "   ...> " */

  /*
  ** Write I/O traces to the following stream.
  */
#if SQLITE_ENABLE_IOTRACE
static FILE *iotrace = null;
#endif

  /*
** This routine works like printf in that its first argument is a
** format string and subsequent arguments are values to be substituted
** in place of % fields.  The result of formatting this string
** is written to iotrace.
*/
#if SQLITE_ENABLE_IOTRACE
static void iotracePrintf(string zFormat, params object[] ap ){
//va_list ap;
string z;
if( iotrace==null ) return;
va_start(ap, zFormat);
z = sqlite3_vmprintf(zFormat, ap);
va_end(ap);
fprintf(iotrace, "%s", z);
sqlite3_free(ref z);
}
#endif


  /*
** Determines if a string is a number of not.
*/
  static bool isNumber( string z )
  {
    int i = 0;
    return isNumber( z, ref i );
  }
  static bool isNumber( string z, int i )
  {
    return isNumber( z, ref i );
  }
  static bool isNumber( string z, ref int realnum )
  {
    int zIdx = 0;
    if ( z[zIdx] == '-' || z[zIdx] == '+' )
      zIdx++;
    if ( zIdx == z.Length || !isdigit( z[zIdx] ) )
    {
      return false;
    }
    zIdx++;
    realnum = 0;
    while ( zIdx < z.Length && isdigit( z[zIdx] ) )
    {
      zIdx++;
    }
    if ( z[zIdx] == '.' )
    {
      zIdx++;
      if ( zIdx < z.Length && !isdigit( z[zIdx] ) )
        return false;
      while ( zIdx < z.Length && isdigit( z[zIdx] ) )
      {
        zIdx++;
      }
      realnum = 1;
    }
    if ( z[zIdx] == 'e' || z[zIdx] == 'E' )
    {
      zIdx++;
      if ( zIdx < z.Length && ( z[zIdx] == '+' || z[zIdx] == '-' ) )
        zIdx++;
      if ( zIdx == z.Length || !isdigit( z[zIdx] ) )
        return false;
      while ( zIdx < z.Length && isdigit( z[zIdx] ) )
      {
        zIdx++;
      }
      realnum = 1;
    }
    return zIdx == z.Length;
  }

  /*
  ** A global char* and an SQL function to access its current value 
  ** from within an SQL statement. This program used to use the 
  ** sqlite_exec_printf() API to substitue a string into an SQL statement.
  ** The correct way to do this with sqlite3 is to use the bind API, but
  ** since the shell is built around the callback paradigm it would be a lot
  ** of work. Instead just use this hack, which is quite harmless.
  */
  static string zShellStatic = "";
  static void shellstaticFunc(
  sqlite3_context context,
  int argc,
  sqlite3_value[] argv
  )
  {
    Debug.Assert( 0 == argc );
    Debug.Assert( zShellStatic != null );
    UNUSED_PARAMETER( argc );
    UNUSED_PARAMETER( argv );
    Sqlite3.sqlite3_result_text( context, zShellStatic, -1, Sqlite3.SQLITE_STATIC );
  }


  /*
  ** This routine reads a line of text from FILE in, stores
  ** the text in memory obtained from malloc() and returns a pointer
  ** to the text.  null; is returned at end of file, or if malloc()
  ** fails.
  **
  ** The interface is like "readline" but no command-line editing
  ** is done.
  */
  static string local_getline( string zPrompt, TextReader _in )
  {
    StringBuilder zIn = new StringBuilder();
    StringBuilder zLine;
    int nLine;
    int n;
    bool eol;

    if ( !String.IsNullOrEmpty( zPrompt ) )
    {
      printf( "%s", zPrompt );
      fflush( stdout );
    }
    nLine = 100;
    zLine = new StringBuilder( nLine );// malloc( nLine );
    //if ( zLine == null ) return null;
    n = 0;
    eol = false;
    while ( !eol )
    {
      if ( n + 100 > nLine )
      {
        nLine = nLine * 2 + 100;
        zLine.Capacity = ( nLine ); //zLine[n] =  realloc( zLine, nLine );
        //if ( zLine == null ) return null;
      }
      if ( fgets( zIn, nLine - n, _in ) == 0 )
      {
        if ( zLine.Length == 0 )
        {
          free( ref zLine );
          return null;
        }
        //zLine.Append( '\0' );
        eol = true;
        break;
      }
      while ( n < zIn.Length && zIn[n] != '\0' )
      {
        n++;
      }
      if ( n > 0 && zIn[n - 1] == '\n' )
      {
        n--;
        zIn.Remove( n, 1 );
        if ( n < zLine.Length && zLine[n - 1] == '\r' )
        {
          n--;
          zIn.Remove( n, 1 );
        }
        //zIn.Length = 0;
        eol = true;
      }
      zLine.Append( zIn );
    }
    //zLine = realloc( zLine, n + 1 );
    return zLine.ToString();
  }



  /*
  ** Retrieve a single line of input text.
  **
  ** zPrior is a string of prior text retrieved.  If not the empty
  ** string, then issue a continuation prompt.
  */
  static string one_input_line( StringBuilder zPrior, TextReader _in )
  {
    string zPrompt;
    string zResult;
    if ( _in != null )
    {
      return local_getline( "", _in );
    }
    if ( zPrior != null && zPrior.Length > 0 )
    {
      zPrompt = continuePrompt;
    }
    else
    {
      zPrompt = mainPrompt;
    }
    zResult = readline( zPrompt );
#if (HAVE_READLINE) //&& HAVE_READLINE==1
if( zResult && *zResult ) add_history(zResult);
#endif
    return zResult;
  }

  class previous_mode_data
  {
    public bool valid;        /* Is there legit data in here? */
    public int mode;
    public bool showHeader;
    public int[] colWidth = new int[100];
  };

  /*
  ** An pointer to an instance of this structure is passed from
  ** the main program to the callback.  This is used to communicate
  ** state and mode information.
  */
  class callback_data
  {
    public sqlite3 db;                      /* The database */
    public bool echoOn;                     /* True to echo input commands */
    public int cnt;                         /* Number of records displayed so far */
    public TextWriter _out;                 /* Write results here */
    public int mode;                        /* An output mode setting */
    public bool writableSchema;             /* True if PRAGMA writable_schema=ON */
    public bool showHeader;                 /* True to show column names in List or Column mode */
    public string zDestTable;               /* Name of destination table when MODE_Insert */
    public string separator = "";           /* Separator character for MODE_List */
    public int[] colWidth = new int[100];   /* Requested width of each column when in column mode*/
    public int[] actualWidth = new int[100];/* Actual width of each column */
    public string nullvalue = "NULL";//[20];    /* The text to print when a null; comes back from
    //** the database */
    public previous_mode_data explainPrev = new previous_mode_data();
    /* Holds the mode information just before
    ** .explain ON */
    public StringBuilder outfile = new StringBuilder( 260 ); /* Filename for *out */
    public string zDbFilename;    /* name of the database file */

    internal callback_data Copy()
    {
      return (callback_data)this.MemberwiseClone();
    }
  };

  /*
  ** These are the allowed modes.
  */
  //#define MODE_Line     0  /* One column per line.  Blank line between records */
  //#define MODE_Column   1  /* One record per line in neat columns */
  //#define MODE_List     2  /* One record per line with a separator */
  //#define MODE_Semi     3  /* Same as MODE_List but append ";" to each line */
  //#define MODE_Html     4  /* Generate an XHTML table */
  //#define MODE_Insert   5  /* Generate SQL "insert" statements */
  //#define MODE_Tcl      6  /* Generate ANSI-C or TCL quoted elements */
  //#define MODE_Csv      7  /* Quote strings, numbers are plain */
  //#define MODE_Explain  8  /* Like MODE_Column, but do not truncate data */
  const int MODE_Line = 0;  /* One column per line.  Blank line between records */
  const int MODE_Column = 1;  /* One record per line in neat columns */
  const int MODE_List = 2;  /* One record per line with a separator */
  const int MODE_Semi = 3;  /* Same as MODE_List but append ";" to each line */
  const int MODE_Html = 4;  /* Generate an XHTML table */
  const int MODE_Insert = 5;  /* Generate SQL "insert" statements */
  const int MODE_Tcl = 6;  /* Generate ANSI-C or TCL quoted elements */
  const int MODE_Csv = 7;  /* Quote strings, numbers are plain */
  const int MODE_Explain = 8;  /* Like MODE_Column, but do not truncate data */
  static string[] modeDescr = new string[] {
"line",
"column",
"list",
"semi",
"html",
"insert",
"tcl",
"csv",
"explain",
};

  /*
  ** Number of elements in an array
  */
  //#define ArraySize(X)  (int)(sizeof(X)/sizeof(X[0]))
  static int ArraySize<T>( T[] X )
  {
    return X.Length;
  }
  /*
  ** Compute a string length that is limited to what can be stored in
  ** lower 30 bits of a 32-bit signed integer.
  */
  static int strlen30( StringBuilder z )
  {
    //string z2 = z;
    //while( *z2 ){ z2++; }
    return 0x3fffffff & z.Length;//(int)(z2 - z);
  }
  static int strlen30( string z )
  {
    //string z2 = z;
    //while( *z2 ){ z2++; }
    return 0x3fffffff & z.Length;//(int)(z2 - z);
  }

  /*
  ** Output the given string as a quoted string using SQL quoting conventions.
  */
  static void output_quoted_string( TextWriter _out, string z )
  {
    int i;
    int nSingle = 0;
    for ( i = 0; z[i] != '\0'; i++ )
    {
      if ( z[i] == '\'' )
        nSingle++;
    }
    if ( nSingle == 0 )
    {
      fprintf( _out, "'%s'", z );
    }
    else
    {
      fprintf( _out, "'" );
      while ( z != "" )
      {
        for ( i = 0; i < z.Length && z[i] != '\''; i++ )
        {
        }
        if ( i == 0 )
        {
          fprintf( _out, "''" );
          //z++;
        }
        else if ( z[i] == '\'' )
        {
          fprintf( _out, "%.*s''", i, z );
          //z += i + 1;
        }
        else
        {
          fprintf( _out, "%s", z );
          break;
        }
      }
      fprintf( _out, "'" );
    }
  }

  /*
  ** Output the given string as a quoted according to C or TCL quoting rules.
  */
  static void output_c_string( TextWriter _out, string z )
  {
    char c;
    fputc( '"', _out );
    int zIdx = 0;
    while ( zIdx < z.Length && ( c = z[zIdx++] ) != '\0' )
    {
      if ( c == '\\' )
      {
        fputc( c, _out );
        fputc( c, _out );
      }
      else if ( c == '\t' )
      {
        fputc( '\\', _out );
        fputc( 't', _out );
      }
      else if ( c == '\n' )
      {
        fputc( '\\', _out );
        fputc( 'n', _out );
      }
      else if ( c == '\r' )
      {
        fputc( '\\', _out );
        fputc( 'r', _out );
      }
      else if ( !isprint( c ) )
      {
        fprintf( _out, "\\%03o", c & 0xff );
      }
      else
      {
        fputc( c, _out );
      }
    }
    fputc( '"', _out );
  }

  /*
  ** Output the given string with characters that are special to
  ** HTML escaped.
  */
  static void output_html_string( TextWriter _out, string z )
  {
    int i;
    while ( z != "" )
    {
      for ( i = 0; i < z.Length && z[i] != '<' && z[i] != '&'; i++ )
      {
      }
      if ( i > 0 )
      {
        fprintf( _out, "%.*s", i, z );
      }
      if ( i < z.Length && z[i] == '<' )
      {
        fprintf( _out, "&lt;" );
      }
      else if ( i < z.Length && z[i] == '&' )
      {
        fprintf( _out, "&amp;" );
      }
      else
      {
        break;
      }
      z += i + 1;
    }
  }

  /*
  ** If a field contains any character identified by a 1 in the following
  ** array, then the string must be quoted for CSV.
  */
  static byte[] needCsvQuote = new byte[]  {
1, 1, 1, 1, 1, 1, 1, 1,   1, 1, 1, 1, 1, 1, 1, 1,   
1, 1, 1, 1, 1, 1, 1, 1,   1, 1, 1, 1, 1, 1, 1, 1,   
1, 0, 1, 0, 0, 0, 0, 1,   0, 0, 0, 0, 0, 0, 0, 0, 
0, 0, 0, 0, 0, 0, 0, 0,   0, 0, 0, 0, 0, 0, 0, 0, 
0, 0, 0, 0, 0, 0, 0, 0,   0, 0, 0, 0, 0, 0, 0, 0, 
0, 0, 0, 0, 0, 0, 0, 0,   0, 0, 0, 0, 0, 0, 0, 0, 
0, 0, 0, 0, 0, 0, 0, 0,   0, 0, 0, 0, 0, 0, 0, 0, 
0, 0, 0, 0, 0, 0, 0, 0,   0, 0, 0, 0, 0, 0, 0, 1, 
1, 1, 1, 1, 1, 1, 1, 1,   1, 1, 1, 1, 1, 1, 1, 1,   
1, 1, 1, 1, 1, 1, 1, 1,   1, 1, 1, 1, 1, 1, 1, 1,   
1, 1, 1, 1, 1, 1, 1, 1,   1, 1, 1, 1, 1, 1, 1, 1,   
1, 1, 1, 1, 1, 1, 1, 1,   1, 1, 1, 1, 1, 1, 1, 1,   
1, 1, 1, 1, 1, 1, 1, 1,   1, 1, 1, 1, 1, 1, 1, 1,   
1, 1, 1, 1, 1, 1, 1, 1,   1, 1, 1, 1, 1, 1, 1, 1,   
1, 1, 1, 1, 1, 1, 1, 1,   1, 1, 1, 1, 1, 1, 1, 1,   
1, 1, 1, 1, 1, 1, 1, 1,   1, 1, 1, 1, 1, 1, 1, 1,   
};

  /*
  ** Output a single term of CSV.  Actually, p.separator is used for
  ** the separator, which may or may not be a comma.  p.nullvalue is
  ** the null value.  Strings are quoted using ANSI-C rules.  Numbers
  ** appear outside of quotes.
  */
  static void output_csv( callback_data p, string z, int bSep )
  {
    TextWriter _out = p._out;
    if ( z == null )
    {
      fprintf( _out, "%s", p.nullvalue );
    }
    else
    {
      int i;
      int nSep = strlen30( p.separator );
      for ( i = 0; i < z.Length; i++ )
      {
        if ( needCsvQuote[z[i]] != 0
        || ( z[i] == p.separator[0] &&
        ( nSep == 1 || z == p.separator ) ) )
        {
          i = 0;
          break;
        }
      }
      if ( i == 0 )
      {
        putc( '"', _out );
        for ( i = 0; i < z.Length; i++ )
        {
          if ( z[i] == '"' )
            putc( '"', _out );
          putc( z[i], _out );
        }
        putc( '"', _out );
      }
      else
      {
        fprintf( _out, "%s", z );
      }
    }
    if ( bSep != 0 )
    {
      fprintf( p._out, "%s", p.separator );
    }
  }

#if SIGINT
/*
** This routine runs when the user presses Ctrl-C
*/
static void interrupt_handler(int NotUsed){
UNUSED_PARAMETER(NotUsed);
seenInterrupt = 1;
if( db ) sqlite3_interrupt(db);
}
#endif

  /*
** This is the callback routine that the SQLite library
** invokes for each row of a query result.
*/
  static int callback( object pArg, sqlite3_int64 nArg, object azArgs, object azCols )
  {
    int i;
    callback_data p = (callback_data)pArg;
    string[] azArg = (string[])azArgs;
    string[] azCol = (string[])azCols;

    switch ( p.mode )
    {
      case MODE_Line:
        {
          int w = 5;
          if ( azArg == null )
            break;
          for ( i = 0; i < nArg; i++ )
          {
            int len = strlen30( azCol[i] != null ? azCol[i] : "" );
            if ( len > w )
              w = len;
          }
          if ( p.cnt++ > 0 )
            fprintf( p._out, "\n" );
          for ( i = 0; i < nArg; i++ )
          {
            fprintf( p._out, "%*s = %s\n", w, azCol[i],
                azArg[i] != null ? azArg[i] : p.nullvalue );
          }
          break;
        }
      case MODE_Explain:
      case MODE_Column:
        {
          if ( p.cnt++ == 0 )
          {
            for ( i = 0; i < nArg; i++ )
            {
              int w, n;
              if ( i < ArraySize( p.colWidth ) )
              {
                w = p.colWidth[i];
              }
              else
              {
                w = 0;
              }
              if ( w <= 0 )
              {
                w = strlen30( azCol[i] != null ? azCol[i] : "" );
                if ( w < 10 )
                  w = 10;
                n = strlen30( azArg != null && azArg[i] != null ? azArg[i] : p.nullvalue );
                if ( w < n )
                  w = n;
              }
              if ( i < ArraySize( p.actualWidth ) )
              {
                p.actualWidth[i] = w;
              }
              if ( p.showHeader )
              {
                fprintf( p._out, "%-*.*s%s", w, w, azCol[i], i == nArg - 1 ? "\n" : "  " );
              }
            }
            if ( p.showHeader )
            {
              for ( i = 0; i < nArg; i++ )
              {
                int w;
                if ( i < ArraySize( p.actualWidth ) )
                {
                  w = p.actualWidth[i];
                }
                else
                {
                  w = 10;
                }
                fprintf( p._out, "%-*.*s%s", w, w, "-----------------------------------" +
                       "----------------------------------------------------------",
                        i == nArg - 1 ? "\n" : "  " );
              }
            }
          }
          if ( azArg == null )
            break;
          for ( i = 0; i < nArg; i++ )
          {
            int w;
            if ( i < ArraySize( p.actualWidth ) )
            {
              w = p.actualWidth[i];
            }
            else
            {
              w = 10;
            }
            if ( p.mode == MODE_Explain && azArg[i] != null &&
            strlen30( azArg[i] ) > w )
            {
              w = strlen30( azArg[i] );
            }
            fprintf( p._out, "%-*.*s%s", w, w,
            azArg[i] != null ? azArg[i] : p.nullvalue, i == nArg - 1 ? "\n" : "  " );
          }
          break;
        }
      case MODE_Semi:
      case MODE_List:
        {
          if ( p.cnt++ == 0 && p.showHeader )
          {
            for ( i = 0; i < nArg; i++ )
            {
              fprintf( p._out, "%s%s", azCol[i], i == nArg - 1 ? "\n" : p.separator );
            }
          }
          if ( azArg == null )
            break;
          for ( i = 0; i < nArg; i++ )
          {
            string z = azArg[i];
            if ( z == null )
              z = p.nullvalue;
            fprintf( p._out, "%s", z );
            if ( i < nArg - 1 )
            {
              fprintf( p._out, "%s", p.separator );
            }
            else if ( p.mode == MODE_Semi )
            {
              fprintf( p._out, ";\n" );
            }
            else
            {
              fprintf( p._out, "\n" );
            }
          }
          break;
        }
      case MODE_Html:
        {
          if ( p.cnt++ == 0 && p.showHeader )
          {
            fprintf( p._out, "<TR>" );
            for ( i = 0; i < nArg; i++ )
            {
              fprintf( p._out, "<TH>%s</TH>", azCol[i] );
            }
            fprintf( p._out, "</TR>\n" );
          }
          if ( azArg == null )
            break;
          fprintf( p._out, "<TR>" );
          for ( i = 0; i < nArg; i++ )
          {
            fprintf( p._out, "<TD>" );
            output_html_string( p._out, azArg[i] != null ? azArg[i] : p.nullvalue );
            fprintf( p._out, "</TD>\n" );
          }
          fprintf( p._out, "</TR>\n" );
          break;
        }
      case MODE_Tcl:
        {
          if ( p.cnt++ == 0 && p.showHeader )
          {
            for ( i = 0; i < nArg; i++ )
            {
              output_c_string( p._out, azCol[i] != null ? azCol[i] : "" );
              fprintf( p._out, "%s", p.separator );
            }
            fprintf( p._out, "\n" );
          }
          if ( azArg == null )
            break;
          for ( i = 0; i < nArg; i++ )
          {
            output_c_string( p._out, azArg[i] != null ? azArg[i] : p.nullvalue );
            fprintf( p._out, "%s", p.separator );
          }
          fprintf( p._out, "\n" );
          break;
        }
      case MODE_Csv:
        {
          if ( p.cnt++ == 0 && p.showHeader )
          {
            for ( i = 0; i < nArg; i++ )
            {
              output_csv( p, azCol[i] != null ? azCol[i] : "", i < nArg - 1 ? 1 : 0 );
            }
            fprintf( p._out, "\n" );
          }
          if ( azArg == null )
            break;
          for ( i = 0; i < nArg; i++ )
          {
            output_csv( p, azArg[i], i < nArg - 1 ? 1 : 0 );
          }
          fprintf( p._out, "\n" );
          break;
        }
      case MODE_Insert:
        {
          if ( azArg == null )
            break;
          fprintf( p._out, "INSERT INTO %s VALUES(", p.zDestTable );
          for ( i = 0; i < nArg; i++ )
          {
            string zSep = i > 0 ? "," : "";
            if ( azArg[i] == null )
            {
              fprintf( p._out, "%sNULL", zSep );
            }
            else if ( isNumber( azArg[i], 0 ) )
            {
              fprintf( p._out, "%s%s", zSep, azArg[i] );
            }
            else
            {
              if ( zSep[0] != '\0' )
                fprintf( p._out, "%s", zSep );
              output_quoted_string( p._out, azArg[i] );
            }
          }
          fprintf( p._out, ");\n" );
          break;
        }
    }
    return 0;
  }

  /*
  ** Set the destination table field of the callback_data structure to
  ** the name of the table given.  Escape any quote characters in the
  ** table name.
  */
  static void set_table_name( callback_data p, string zName )
  {
    int i, n;
    bool needQuote;
    string z = "";

    if ( p.zDestTable != null )
    {
      free( ref p.zDestTable );
      p.zDestTable = null;
    }
    if ( zName == null )
      return;
    needQuote = !isalpha( zName[0] ) && zName != "_";
    for ( i = n = 0; i < zName.Length; i++, n++ )
    {
      if ( !isalnum( zName[i] ) && zName[i] != '_' )
      {
        needQuote = true;
        if ( zName[i] == '\'' )
          n++;
      }
    }
    if ( needQuote )
      n += 2;
    //z = p.zDestTable  = malloc( n + 1 );
    //if ( z == 0 )
    //{
    //  fprintf( stderr, "Out of memory!\n" );
    //  exit( 1 );
    //}
    //n = 0;
    if ( needQuote )
      z += '\'';
    for ( i = 0; i < zName.Length; i++ )
    {
      z += zName[i];
      if ( zName[i] == '\'' )
        z += '\'';
    }
    if ( needQuote )
      z += '\'';
    //z[n] = 0;
    p.zDestTable = z;
  }

  /* zIn is either a pointer to a null;-terminated string in memory obtained
  ** from malloc(), or a null; pointer. The string pointed to by zAppend is
  ** added to zIn, and the result returned in memory obtained from malloc().
  ** zIn, if it was not null;, is freed.
  **
  ** If the third argument, quote, is not '\0', then it is used as a 
  ** quote character for zAppend.
  */
  static void appendText( StringBuilder zIn, string zAppend, char quote )
  {
    int len;
    int i;
    int nAppend = strlen30( zAppend );
    int nIn = ( zIn != null ? strlen30( zIn ) : 0 );

    len = nAppend + nIn;
    if ( quote != '\0' )
    {
      len += 2;
      for ( i = 0; i < nAppend; i++ )
      {
        if ( zAppend[i] == quote )
          len++;
      }
    }

    //zIn = realloc( zIn, len );
    //if ( !zIn )
    //{
    //  return 0;
    //}

    if ( quote != '\0' )
    {
      zIn.Append( quote );
      for ( i = 0; i < nAppend; i++ )
      {
        zIn.Append( zAppend[i] );
        if ( zAppend[i] == quote )
          zIn.Append( quote );
      }
      zIn.Append( quote );
      //zCsr++ = '\0';
      Debug.Assert( zIn.Length == len );
    }
    else
    {
      zIn.Append( zAppend );//memcpy( zIn[nIn], zAppend, nAppend );
      //zIn[len - 1] = '\0';
    }
  }


  /*
  ** Execute a query statement that has a single result column.  Print
  ** that result column on a line by itself with a semicolon terminator.
  **
  ** This is used, for example, to show the schema of the database by
  ** querying the sqlite.SQLITE_MASTER table.
  */
  static int run_table_dump_query( TextWriter _out, sqlite3 db, string zSelect )
  {
    sqlite3_stmt pSelect = new sqlite3_stmt();
    int rc;
    string sDummy = null;
    rc = Sqlite3.sqlite3_prepare( db, zSelect, -1, ref pSelect, ref sDummy );
    if ( rc != Sqlite3.SQLITE_OK || null == pSelect )
    {
      return rc;
    }
    rc = Sqlite3.sqlite3_step( pSelect );
    while ( rc == Sqlite3.SQLITE_ROW )
    {
      fprintf( _out, "%s;\n", Sqlite3.sqlite3_column_text( pSelect, 0 ) );
      rc = Sqlite3.sqlite3_step( pSelect );
    }
    return Sqlite3.sqlite3_finalize( pSelect );
  }


  /*
  ** This is a different callback routine used for dumping the database.
  ** Each row received by this callback consists of a table name,
  ** the table type ("index" or "table") and SQL to create the table.
  ** This routine should print text sufficient to recreate the table.
  */
  static int dump_callback( object pArg, sqlite3_int64 nArg, object azArg, object azCol )
  {
    int rc;
    string zTable;
    string zType;
    string zSql;
    callback_data p = (callback_data)pArg;

    UNUSED_PARAMETER( azCol );
    if ( nArg != 3 )
      return 1;
    zTable = ( (string[])azArg )[0];
    zType = ( (string[])azArg )[1];
    zSql = ( (string[])azArg )[2];

    if ( strcmp( zTable, "sqlite_sequence" ) == 0 )
    {
      fprintf( p._out, "DELETE FROM sqlite_sequence;\n" );
    }
    else if ( strcmp( zTable, "sqlite_stat1" ) == 0 )
    {
      fprintf( p._out, "ANALYZE sqlite_master;\n" );
    }
    else if ( strncmp( zTable, "sqlite_", 7 ) == 0 )
    {
      return 0;
    }
    else if ( strncmp( zSql, "CREATE VIRTUAL TABLE", 20 ) == 0 )
    {
      string zIns;
      if ( !p.writableSchema )
      {
        fprintf( p._out, "PRAGMA writable_schema=ON;\n" );
        p.writableSchema = true;
      }
      zIns = Sqlite3.sqlite3_mprintf(
      "INSERT INTO sqlite_master(type,name,tbl_name,rootpage,sql)" +
      "VALUES('table','%q','%q',0,'%q');",
      zTable, zTable, zSql );
      fprintf( p._out, "%s\n", zIns );
      //sqlite.sqlite3_free( ref zIns );
      return 0;
    }
    else
    {
      fprintf( p._out, "%s;\n", zSql );
    }

    if ( strcmp( zType, "table" ) == 0 )
    {
      sqlite3_stmt pTableInfo = null;
      StringBuilder zSelect = new StringBuilder( 100 );
      StringBuilder zTableInfo = new StringBuilder( 100 );
      StringBuilder zTmp = new StringBuilder( 100 );

      appendText( zTableInfo, "PRAGMA table_info(", '\0' );
      appendText( zTableInfo, zTable, '"' );
      appendText( zTableInfo, ");", '\0' );

      string sDummy = null;
      rc = Sqlite3.sqlite3_prepare( p.db, zTableInfo.ToString(), -1, ref pTableInfo, ref sDummy );
      //if ( zTableInfo ) free( ref zTableInfo );
      if ( rc != Sqlite3.SQLITE_OK || null == pTableInfo )
      {
        return 1;
      }

      appendText( zSelect, "SELECT 'INSERT INTO ' || ", '\0' );
      appendText( zTmp, zTable.ToString(), '"' );
      if ( zTmp.Length != 0 )
      {
        appendText( zSelect, zTmp.ToString(), '\'' );
      }
      appendText( zSelect, " || ' VALUES(' || ", '\0' );
      rc = Sqlite3.sqlite3_step( pTableInfo );
      while ( rc == Sqlite3.SQLITE_ROW )
      {
        string zText = (string)Sqlite3.sqlite3_column_text( pTableInfo, 1 );
        appendText( zSelect, "quote(", '\0' );
        appendText( zSelect, zText, '"' );
        rc = Sqlite3.sqlite3_step( pTableInfo );
        if ( rc == Sqlite3.SQLITE_ROW )
        {
          appendText( zSelect, ") || ',' || ", '\0' );
        }
        else
        {
          appendText( zSelect, ") ", '\0' );
        }
      }
      rc = Sqlite3.sqlite3_finalize( pTableInfo );
      if ( rc != Sqlite3.SQLITE_OK )
      {
        //if ( zSelect ) free( ref zSelect );
        return 1;
      }
      appendText( zSelect, "|| ')' FROM  ", '\0' );
      appendText( zSelect, zTable, '"' );

      rc = run_table_dump_query( p._out, p.db, zSelect.ToString() );
      if ( rc == Sqlite3.SQLITE_CORRUPT )
      {
        appendText( zSelect, " ORDER BY rowid DESC", '\0' );
        rc = run_table_dump_query( p._out, p.db, zSelect.ToString() );
      }
      //if ( zSelect ) free( ref zSelect );
    }
    return 0;
  }

  /*
  ** Run zQuery.  Use dump_callback() as the callback routine so that
  ** the contents of the query are output as SQL statements.
  **
  ** If we get a sqlite.SQLITE_CORRUPT error, rerun the query after appending
  ** "ORDER BY rowid DESC" to the end.
  */
  static int run_schema_dump_query(
  callback_data p,
  string zQuery,
  string pzErrMsg
  )
  {
    int rc;
    rc = Sqlite3.sqlite3_exec( p.db, zQuery, (dxCallback)dump_callback, p, ref pzErrMsg );
    if ( rc == Sqlite3.SQLITE_CORRUPT )
    {
      StringBuilder zQ2;
      int len = strlen30( zQuery );
      //if ( pzErrMsg ) sqlite3_free( ref pzErrMsg );
      zQ2 = new StringBuilder( len + 100 );// malloc( len + 100 );
      if ( zQ2 == null )
        return rc;
      Sqlite3.sqlite3_snprintf( zQ2.Capacity, zQ2, "%s ORDER BY rowid DESC", zQuery );
      rc = Sqlite3.sqlite3_exec( p.db, zQ2.ToString(), (dxCallback)dump_callback, p, ref pzErrMsg );
      free( ref zQ2 );
    }
    return rc;
  }

  /*
  ** Text of a help message
  */
  static string zHelp =
  ".backup ?DB? FILE      Backup DB (default \"main\") to FILE\n" +
  ".bail ON|OFF           Stop after hitting an error.  Default OFF\n" +
  ".databases             List names and files of attached databases\n" +
  ".dump ?TABLE? ...      Dump the database in an SQL text format\n" +
  ".echo ON|OFF           Turn command echo on or off\n" +
  ".exit                  Exit this program\n" +
  ".explain ON|OFF        Turn output mode suitable for EXPLAIN on or off.\n" +
  ".header(s) ON|OFF      Turn display of headers on or off\n" +
  ".help                  Show this message\n" +
  ".import FILE TABLE     Import data from FILE into TABLE\n" +
  ".indices TABLE         Show names of all indices on TABLE\n" +
#if SQLITE_ENABLE_IOTRACE
".iotrace FILE          Enable I/O diagnostic logging to FILE\n" +
#endif
#if !SQLITE_OMIT_LOAD_EXTENSION
 ".load FILE ?ENTRY?     Load an extension library\n" +
#endif
 ".mode MODE ?TABLE?     Set output mode where MODE is one of:\n" +
  "                         csv      Comma-separated values\n" +
  "                         column   Left-aligned columns.  (See .width)\n" +
  "                         html     HTML <table> code\n" +
  "                         insert   SQL insert statements for TABLE\n" +
  "                         line     One value per line\n" +
  "                         list     Values delimited by .separator string\n" +
  "                         tabs     Tab-separated values\n" +
  "                         tcl      TCL list elements\n" +
  ".nullvalue STRING      Print STRING in place of null; values\n" +
  ".output FILENAME       Send output to FILENAME\n" +
  ".output stdout         Send output to the screen\n" +
  ".prompt MAIN CONTINUE  Replace the standard prompts\n" +
  ".quit                  Exit this program\n" +
  ".read FILENAME         Execute SQL in FILENAME\n" +
  ".restore ?DB? FILE     Restore content of DB (default \"main\") from FILE\n" +
  ".schema ?TABLE?        Show the CREATE statements\n" +
  ".separator STRING      Change separator used by output mode and .import\n" +
  ".show                  Show the current values for various settings\n" +
  ".tables ?PATTERN?      List names of tables matching a LIKE pattern\n" +
  ".timeout MS            Try opening locked tables for MS milliseconds\n" +
#if HAS_TIMER
".timer ON|OFF          Turn the CPU timer measurement on or off\n" +
#endif
 ".width NUM NUM ...     Set column widths for \"column\" mode\n"
  ;

  /* Forward reference */
  //static int process_input(callback_data p, FILE _in);

  /*
  ** Make sure the database is open.  If it is not, then open it.  If
  ** the database fails to open, print an error message and exit.
  */
  static void open_db( callback_data p )
  {
    if ( p.db == null )
    {
      Sqlite3.sqlite3_open( p.zDbFilename, out p.db );
      db = p.db;
      if ( db != null && Sqlite3.sqlite3_errcode( db ) == Sqlite3.SQLITE_OK )
      {
        Sqlite3.sqlite3_create_function( db, "shellstatic", 0, Sqlite3.SQLITE_UTF8, 0,
        (Sqlite3.dxFunc)shellstaticFunc, null, null );
      }
      if ( db == null || Sqlite3.SQLITE_OK != Sqlite3.sqlite3_errcode( db ) )
      {
        fprintf( stderr, "Unable to open database \"%s\": %s\n",
        p.zDbFilename, Sqlite3.sqlite3_errmsg( db ) );
        exit( 1 );
      }
#if !SQLITE_OMIT_LOAD_EXTENSION
      Sqlite3.sqlite3_enable_load_extension( p.db, 1 );
#endif
    }
  }

  /*
  ** Do C-language style dequoting.
  **
  **    \t    . tab
  **    \n    . newline
  **    \r    . carriage return
  **    \NNN  . ascii character NNN in octal
  **    \\    . backslash
  */
  static void resolve_backslashes( StringBuilder z )
  {
    int i, j;
    char c;
    for ( i = j = 0; i < z.Length && ( c = z[i] ) != 0; i++, j++ )
    {
      if ( c == '\\' )
      {
        c = z[++i];
        if ( c == 'n' )
        {
          c = '\n';
        }
        else if ( c == 't' )
        {
          c = '\t';
        }
        else if ( c == 'r' )
        {
          c = '\r';
        }
        else if ( c >= '0' && c <= '7' )
        {
          c -= '0';
          if ( z[i + 1] >= '0' && z[i + 1] <= '7' )
          {
            i++;
            c = (char)( ( c << 3 ) + z[i] - '0' );
            if ( z[i + 1] >= '0' && z[i + 1] <= '7' )
            {
              i++;
              c = (char)( ( c << 3 ) + z[i] - '0' );
            }
          }
        }
      }
      z[j] = c;
    }
    z.Length = j;//z[j] = '\0';
  }

  /*
  ** Interpret zArg as a boolean value.  Return either 0 or 1.
  */
  static int booleanValue( StringBuilder zArg )
  {
    return booleanValue( zArg.ToString() );
  }
  static int booleanValue( string zArg )
  {
    int val = Char.IsDigit( zArg[0] ) ? Convert.ToInt32( zArg ) : 0;// atoi( zArg );
    int j;
    //for ( j = 0 ; zArg[j] ; j++ )
    //{
    //  zArg[j] = (char)tolower( zArg[j] );
    //}
    zArg = zArg.ToLower();
    if ( strcmp( zArg, "on" ) == 0 )
    {
      val = 1;
    }
    else if ( strcmp( zArg, "yes" ) == 0 )
    {
      val = 1;
    }
    return val;
  }

  /*
  ** If an input line begins with "." then invoke this routine to
  ** process that line.
  **
  ** Return 1 on error, 2 to exit, and 0 otherwise.
  */
  static int do_meta_command( StringBuilder zLine, callback_data p )
  {
    int i = 1;
    int i0;
    int nArg = 0;
    int n, c;
    int rc = 0;
    StringBuilder[] azArg = new StringBuilder[100];

    /* Parse the input line into tokens.
    */
    while ( i < zLine.Length && nArg < ArraySize( azArg ) )
    {
      while ( isspace( zLine[i] ) )
      {
        i++;
      }
      if ( zLine[i] == '\0' )
        break;
      if ( zLine[i] == '\'' || zLine[i] == '"' )
      {
        int delim = zLine[i++];
        i0 = i;
        while ( zLine[i] != '\0' && zLine[i] != delim )
        {
          i++;
        }
        if ( zLine[i] == delim )
        {
          zLine[i++] = '\0';
        }
        azArg[nArg++] = new StringBuilder( zLine.ToString().Substring( i0, i - i0 ) );
        if ( delim == '"' )
          resolve_backslashes( azArg[nArg - 1] );
      }
      else
      {
        i0 = i;
        while ( i < zLine.Length && !isspace( zLine[i] ) )
        {
          i++;
        }
        //if ( zLine[i] != '\0' ) zLine[i++] = '\0';
        azArg[nArg++] = new StringBuilder( zLine.ToString().Substring( i0, i - i0 ) );
        resolve_backslashes( azArg[nArg - 1] );
      }
    }

    /* Process the input line.
    */
    if ( nArg == 0 )
      return rc;
    n = strlen30( azArg[0] );
    c = azArg[0][0];
    if ( c == 'b' && n >= 3 && strncmp( azArg[0], "backup", n ) == 0 && nArg > 1 )
    {
      string zDestFile;
      string zDb;
      sqlite3 pDest = null;
      sqlite3_backup pBackup;
      //int rc;
      if ( nArg == 2 )
      {
        zDestFile = azArg[1].ToString();
        zDb = "main";
      }
      else
      {
        zDestFile = azArg[2].ToString();
        zDb = azArg[1].ToString();
      }
      rc = Sqlite3.sqlite3_open( zDestFile, out pDest );
      if ( rc != Sqlite3.SQLITE_OK )
      {
        fprintf( stderr, "Error: cannot open %s\n", zDestFile );
        Sqlite3.sqlite3_close( pDest );
        return 1;
      }
      open_db( p );
      pBackup = Sqlite3.sqlite3_backup_init( pDest, "main", p.db, zDb );
      if ( pBackup == null )
      {
        fprintf( stderr, "Error: %s\n", Sqlite3.sqlite3_errmsg( pDest ) );
        Sqlite3.sqlite3_close( pDest );
        return 1;
      }
      while ( ( rc = Sqlite3.sqlite3_backup_step( pBackup, 100 ) ) == Sqlite3.SQLITE_OK )
      {
      }
      Sqlite3.sqlite3_backup_finish( pBackup );
      if ( rc == Sqlite3.SQLITE_DONE )
      {
        rc = Sqlite3.SQLITE_OK;
      }
      else
      {
        fprintf( stderr, "Error: %s\n", Sqlite3.sqlite3_errmsg( pDest ) );
      }
      Sqlite3.sqlite3_close( pDest );
    }
    else

      if ( c == 'b' && n >= 3 && strncmp( azArg[0], "bail", n ) == 0 && nArg > 1 )
      {
        bail_on_error = booleanValue( azArg[1] ) == 1;
      }
      else

        if ( c == 'd' && n > 1 && strncmp( azArg[0], "databases", n ) == 0 )
        {
          callback_data data;
          string zErrMsg = null;
          open_db( p );
          data = p.Copy();// memcpy( data, p, sizeof( data ) );
          data.showHeader = true;
          data.mode = MODE_Column;
          data.colWidth[0] = 3;
          data.colWidth[1] = 15;
          data.colWidth[2] = 58;
          data.cnt = 0;
          Sqlite3.sqlite3_exec( p.db, "PRAGMA database_list; ", (dxCallback)callback, data, ref zErrMsg );
          if ( zErrMsg != "" )
          {
            fprintf( stderr, "Error: %s\n", zErrMsg );
            //sqlite3_free( ref zErrMsg );
          }
        }
        else

          if ( c == 'd' && strncmp( azArg[0], "dump", n ) == 0 )
          {
            string zErrMsg = null;
            open_db( p );
            fprintf( p._out, "BEGIN TRANSACTION;\n" );
            p.writableSchema = false;
            string sDummy = "";
            Sqlite3.sqlite3_exec( p.db, "PRAGMA writable_schema=ON", null, null, ref sDummy );
            if ( nArg == 1 )
            {
              run_schema_dump_query( p,
              "SELECT name, type, sql FROM sqlite_master " +
              "WHERE sql NOT null; AND type=='table'", null
              );
              run_table_dump_query( p._out, p.db,
              "SELECT sql FROM sqlite_master " +
              "WHERE sql NOT null; AND type IN ('index','trigger','view')"
              );
            }
            else
            {
              int ii;
              for ( ii = 1; ii < nArg; ii++ )
              {
                zShellStatic = azArg[ii].ToString();
                run_schema_dump_query( p,
                  "SELECT name, type, sql FROM sqlite_master " +
                  "WHERE tbl_name LIKE shellstatic() AND type=='table'" +
                  "  AND sql NOT null;", null );
                run_table_dump_query( p._out, p.db,
                  "SELECT sql FROM sqlite_master " +
                  "WHERE sql NOT null;" +
                  "  AND type IN ('index','trigger','view')" +
                  "  AND tbl_name LIKE shellstatic()"
                );
                zShellStatic = "";
              }
            }
            if ( p.writableSchema )
            {
              fprintf( p._out, "PRAGMA writable_schema=OFF;\n" );
              p.writableSchema = false;
            }
            Sqlite3.sqlite3_exec( p.db, "PRAGMA writable_schema=OFF", 0, 0, 0 );
            if ( zErrMsg != "" )
            {
              fprintf( stderr, "Error: %s\n", zErrMsg );
              //sqlite3_free( ref zErrMsg );
            }
            else
            {
              fprintf( p._out, "COMMIT;\n" );
            }
          }
          else

            if ( c == 'e' && strncmp( azArg[0], "echo", n ) == 0 && nArg > 1 )
            {
              p.echoOn = booleanValue( azArg[1] ) == 1;
            }
            else

              if ( c == 'e' && strncmp( azArg[0], "exit", n ) == 0 )
              {
                rc = 2;
              }
              else

                if ( c == 'e' && strncmp( azArg[0], "explain", n ) == 0 )
                {
                  int val = nArg >= 2 ? booleanValue( azArg[1] ) : 1;
                  if ( val == 1 )
                  {
                    if ( !p.explainPrev.valid )
                    {
                      p.explainPrev.valid = true;
                      p.explainPrev.mode = p.mode;
                      p.explainPrev.showHeader = p.showHeader;
                      p.explainPrev.colWidth = new int[p.colWidth.Length];
                      Array.Copy( p.colWidth, p.explainPrev.colWidth, p.colWidth.Length );//memcpy( p.explainPrev.colWidth, p.colWidth, sizeof( p.colWidth ) );
                    }
                    /* We could put this code under the !p.explainValid
                    ** condition so that it does not execute if we are already in
                    ** explain mode. However, always executing it allows us an easy
                    ** was to reset to explain mode in case the user previously
                    ** did an .explain followed by a .width, .mode or .header
                    ** command.
                    */
                    p.mode = MODE_Explain;
                    p.showHeader = true;
                    Array.Clear( p.colWidth, 0, p.colWidth.Length );// memset( p.colWidth, 0, ArraySize( p.colWidth ) );
                    p.colWidth[0] = 4;                  /* addr */
                    p.colWidth[1] = 13;                 /* opcode */
                    p.colWidth[2] = 4;                  /* P1 */
                    p.colWidth[3] = 4;                  /* P2 */
                    p.colWidth[4] = 4;                  /* P3 */
                    p.colWidth[5] = 13;                 /* P4 */
                    p.colWidth[6] = 2;                  /* P5 */
                    p.colWidth[7] = 13;                  /* Comment */
                  }
                  else if ( p.explainPrev.valid )
                  {
                    p.explainPrev.valid = false;
                    p.mode = p.explainPrev.mode;
                    p.showHeader = p.explainPrev.showHeader;
                    p.colWidth = new int[p.explainPrev.colWidth.Length];
                    Array.Copy( p.explainPrev.colWidth, p.colWidth, p.colWidth.Length );//memcpy( p.colWidth, p.explainPrev.colWidth, sizeof( p.colWidth ) );
                  }
                }
                else

                  if ( c == 'h' && ( strncmp( azArg[0], "header", n ) == 0 ||
                                 strncmp( azArg[0], "headers", n ) == 0 ) && nArg > 1 )
                  {
                    p.showHeader = booleanValue( azArg[1] ) == 1;
                  }
                  else

                    if ( c == 'h' && strncmp( azArg[0], "help", n ) == 0 )
                    {
                      fprintf( stderr, "%s", zHelp );
                    }
                    else

                      if ( c == 'i' && strncmp( azArg[0], "import", n ) == 0 && nArg >= 3 )
                      {
                        string zTable = azArg[2].ToString();     /* Insert data into this table */
                        string zFile = azArg[1].ToString();      /* The file from which to extract data */
                        sqlite3_stmt pStmt = null;          /* A statement */
                        //int rc;                       /* Result code */
                        int nCol;                     /* Number of columns in the table */
                        int nByte;                    /* Number of bytes in an SQL string /
int i, j;                     /* Loop counters */
                        int nSep;                     /* Number of bytes in p.separator[] */
                        StringBuilder zSql;           /* An SQL statement */
                        //StringBuilder zLine;          /* A single line of input from the file */
                        string[] azCol;               /* zLine[] broken up into columns */
                        string zCommit;               /* How to commit changes */
                        StreamReader _in;             /* The input file */
                        int lineno = 0;               /* Line number of input file */

                        open_db( p );
                        nSep = strlen30( p.separator );
                        if ( nSep == 0 )
                        {
                          fprintf( stderr, "non-null separator required for import\n" );
                          return 0;
                        }
                        zSql = new StringBuilder( Sqlite3.sqlite3_mprintf( "SELECT * FROM '%q'", zTable ) );
                        if ( zSql == null )
                          return 0;
                        nByte = strlen30( zSql );
                        string sDummy = null;
                        rc = Sqlite3.sqlite3_prepare( p.db, zSql.ToString(), -1, ref pStmt, ref sDummy );
                        //sqlite3_free( ref zSql );
                        if ( rc != 0 )
                        {
                          fprintf( stderr, "Error: %s\n", Sqlite3.sqlite3_errmsg( db ) );
                          nCol = 0;
                          rc = 1;
                        }
                        else
                        {
                          nCol = Sqlite3.sqlite3_column_count( pStmt );
                        }
                        Sqlite3.sqlite3_finalize( pStmt );
                        if ( nCol == 0 )
                          return 0;
                        zSql = new StringBuilder( nByte + 20 + nCol * 2 );//malloc( nByte + 20 + nCol * 2 );
                        if ( zSql == null )
                          return 0;
                        Sqlite3.sqlite3_snprintf( nByte + 20, zSql, "INSERT INTO '%q' VALUES(?", zTable );
                        int j = strlen30( zSql );
                        for ( i = 1; i < nCol; i++ )
                        {
                          zSql.Append( ',' );
                          zSql.Append( '?' );
                        }
                        zSql.Append( ')' );
                        rc = Sqlite3.sqlite3_prepare( p.db, zSql.ToString(), -1, ref pStmt, ref sDummy );
                        free( ref zSql );
                        if ( rc != 0 )
                        {
                          fprintf( stderr, "Error: %s\n", Sqlite3.sqlite3_errmsg( db ) );
                          Sqlite3.sqlite3_finalize( pStmt );
                          return 1;
                        }
                        try
                        {
                          _in = new StreamReader( zFile );// fopen( zFile, "rb" );
                          zLine.Length = 0;
                        }
                        catch
                        {
                          _in = null;
                        }
                        if ( _in == null )
                        {
                          fprintf( stderr, "cannot open file: %s\n", zFile );
                          Sqlite3.sqlite3_finalize( pStmt );
                          return 0;
                        }
                        azCol = new string[nCol + 1];//malloc( sizeof(azCol[0])*(nCol+1) );
                        if ( azCol == null )
                        {
                          _in.Close();// fclose( _in );
                          return 0;
                        }
                        Sqlite3.sqlite3_exec( p.db, "BEGIN", 0, 0, 0 );
                        zCommit = "COMMIT";
                        while ( ( zLine.Append( local_getline( "", _in ) ) ).Length != 0 )
                        {
                          string z;
                          i = 0;
                          lineno++;
                          azCol = zLine.ToString().Split( p.separator.ToCharArray() );
                          //azCol[0] = zLine.ToString();
                          //int zDx = 0;
                          //for (i = 0; zDx < zLine.Length && zLine[zDx] != '\0' && zLine[zDx] != '\n' && zLine[zDx] != '\r'; zDx++)
                          //{
                          //  z = zLine.ToString( i, zDx - i + 1 );
                          //  if ( z[zDx-i] == p.separator[0] && strncmp( z.Substring(zDx), p.separator, nSep ) == 0 )
                          //  {
                          //    z = null;
                          //    i++;
                          //    if ( i < nCol )
                          //    {
                          //      azCol[i] = zLine.ToString( zDx + nSep, zLine.Length - zDx + nSep + 1 );
                          //      zDx += nSep - 1;
                          //    }
                          //  }
                          //}
                          //z = null;
                          if ( azCol.Length != nCol )
                          {
                            fprintf( stderr, "%s line %d: expected %d columns of data but found %d\n",
                               zFile, lineno, nCol, i + 1 );
                            zCommit = "ROLLBACK";
                            free( ref zLine );
                            break;
                          }
                          for ( i = 0; i < nCol; i++ )
                          {
                            Sqlite3.sqlite3_bind_text( pStmt, i + 1, azCol[i], -1, Sqlite3.SQLITE_STATIC );
                          }
                          Sqlite3.sqlite3_step( pStmt );
                          rc = Sqlite3.sqlite3_reset( pStmt );
                          zLine.Length = 0;// free(zLine);
                          if ( rc != Sqlite3.SQLITE_OK )
                          {
                            fprintf( stderr, "Error: %s\n", Sqlite3.sqlite3_errmsg( db ) );
                            zCommit = "ROLLBACK";
                            rc = 1;
                            break;
                          }
                        }
                        //free( ref azCol );
                        _in.Close();// fclose( _in );
                        Sqlite3.sqlite3_finalize( pStmt );
                        Sqlite3.sqlite3_exec( p.db, zCommit.ToString(), null, null, ref sDummy );
                      }
                      else

                        if ( c == 'i' && strncmp( azArg[0], "indices", n ) == 0 && nArg > 1 )
                        {
                          callback_data data;
                          string zErrMsg = null;
                          open_db( p );
                          data = p.Copy(); //memcpy( data, p, sizeof( data ) );
                          data.showHeader = false;
                          data.mode = MODE_List;
                          zShellStatic = azArg[1].ToString();
                          Sqlite3.sqlite3_exec( p.db,
                            "SELECT name FROM sqlite_master " +
                            "WHERE type='index' AND tbl_name LIKE shellstatic() " +
                            "UNION ALL " +
                            "SELECT name FROM sqlite_temp_master " +
                            "WHERE type='index' AND tbl_name LIKE shellstatic() " +
                            "ORDER BY 1",
                            (dxCallback)callback, data, ref zErrMsg
                          );
                          zShellStatic = "";
                          if ( zErrMsg != "" )
                          {
                            fprintf( stderr, "Error: %s\n", zErrMsg );
                            //sqlite3_free( ref zErrMsg );
                          }
                        }
                        else

#if SQLITE_ENABLE_IOTRACE
if( c=='i' && strncmp(azArg[0], "iotrace", n)==0 ){
extern void (*sqlite3IoTrace)(const char*, ...);
if( iotrace && iotrace!=stdout ) fclose(iotrace);
iotrace = 0;
if( nArg<2 ){
sqlite3IoTrace = 0;
}else if( strcmp(azArg[1], "-")==0 ){
sqlite3IoTrace = iotracePrintf;
iotrace = stdout;
}else{
iotrace = fopen(azArg[1], "w");
if( iotrace==0 ){
fprintf(stderr, "cannot open \"%s\"\n", azArg[1]);
sqlite3IoTrace = 0;
}else{
sqlite3IoTrace = iotracePrintf;
}
}
}else
#endif

#if !SQLITE_OMIT_LOAD_EXTENSION
                          if ( c == 'l' && strncmp( azArg[0], "load", n ) == 0 && nArg >= 2 )
                          {
                            string zFile, zProc;
                            string zErrMsg = null;
                            //int rc;
                            zFile = azArg[1].ToString();
                            zProc = nArg >= 3 ? azArg[2].ToString() : null;
                            open_db( p );
                            rc = Sqlite3.sqlite3_load_extension( p.db, zFile, zProc, ref  zErrMsg );
                            if ( rc != Sqlite3.SQLITE_OK )
                            {
                              fprintf( stderr, "%s\n", zErrMsg );
                              //sqlite3_free( ref zErrMsg );
                              rc = 1;
                            }
                          }
                          else
#endif

                            if ( c == 'm' && strncmp( azArg[0], "mode", n ) == 0 && nArg >= 2 )
                            {
                              int n2 = strlen30( azArg[1] );
                              if ( strncmp( azArg[1], "line", n2 ) == 0
                                  ||
                                  strncmp( azArg[1], "lines", n2 ) == 0 )
                              {
                                p.mode = MODE_Line;
                              }
                              else if ( strncmp( azArg[1], "column", n2 ) == 0
                                        ||
                                        strncmp( azArg[1], "columns", n2 ) == 0 )
                              {
                                p.mode = MODE_Column;
                              }
                              else if ( strncmp( azArg[1], "list", n2 ) == 0 )
                              {
                                p.mode = MODE_List;
                              }
                              else if ( strncmp( azArg[1], "html", n2 ) == 0 )
                              {
                                p.mode = MODE_Html;
                              }
                              else if ( strncmp( azArg[1], "tcl", n2 ) == 0 )
                              {
                                p.mode = MODE_Tcl;
                              }
                              else if ( strncmp( azArg[1], "csv", n2 ) == 0 )
                              {
                                p.mode = MODE_Csv;
                                snprintf( 2, ref p.separator, "," );
                              }
                              else if ( strncmp( azArg[1], "tabs", n2 ) == 0 )
                              {
                                p.mode = MODE_List;
                                snprintf( 2, ref p.separator, "\t" );
                              }
                              else if ( strncmp( azArg[1], "insert", n2 ) == 0 )
                              {
                                p.mode = MODE_Insert;
                                if ( nArg >= 3 )
                                {
                                  set_table_name( p, azArg[2].ToString() );
                                }
                                else
                                {
                                  set_table_name( p, "table" );
                                }
                              }
                              else
                              {
                                fprintf( stderr, "mode should be one of: " +
                                   "column csv html insert line list tabs tcl\n" );
                              }
                            }
                            else

                              if ( c == 'n' && strncmp( azArg[0], "nullvalue", n ) == 0 && nArg == 2 )
                              {
                                snprintf( 20, ref p.nullvalue,
                                                 "%.*s", 19 - 1, azArg[1] );
                              }
                              else

                                if ( c == 'o' && strncmp( azArg[0], "output", n ) == 0 && nArg == 2 )
                                {
                                  if ( p._out != stdout )
                                  {
                                    p._out.Close();// fclose( p._out );
                                  }
                                  if ( strcmp( azArg[1], "stdout" ) == 0 )
                                  {
                                    p._out = stdout;
                                    Sqlite3.sqlite3_snprintf( p.outfile.Capacity, p.outfile, "stdout" );
                                  }
                                  else
                                  {
                                    p._out = new StreamWriter( azArg[1].ToString() );// fopen( azArg[1], "wb" );
                                    if ( p._out == null )
                                    {
                                      fprintf( stderr, "can't write to \"%s\"\n", azArg[1] );
                                      p._out = stdout;
                                    }
                                    else
                                    {
                                      Sqlite3.sqlite3_snprintf( p.outfile.Capacity, p.outfile, "%s", azArg[1] );
                                    }
                                  }
                                }
                                else

                                  if ( c == 'p' && strncmp( azArg[0], "prompt", n ) == 0 && ( nArg == 2 || nArg == 3 ) )
                                  {
                                    if ( nArg >= 2 )
                                    {
                                      mainPrompt = azArg[1].ToString();//strncpy( mainPrompt, azArg[1], (int)ArraySize( mainPrompt ) - 1 );
                                    }
                                    if ( nArg >= 3 )
                                    {
                                      continuePrompt = azArg[2].ToString();// continuePromptstrncpy( continuePrompt, azArg[2], (int)ArraySize( continuePrompt ) - 1 );
                                    }
                                  }
                                  else

                                    if ( c == 'q' && strncmp( azArg[0], "quit", n ) == 0 )
                                    {
                                      rc = 2;
                                    }
                                    else

                                      if ( c == 'r' && n >= 3 && strncmp( azArg[0], "read", n ) == 0 && nArg == 2 )
                                      {
                                        TextReader alt;
                                        try
                                        {
                                          alt = new StreamReader( azArg[1].ToString() );// fopen( azArg[1], "rb" );
                                        }
                                        catch
                                        {
                                          alt = null;
                                        }
                                        if ( alt == null )
                                        {
                                          fprintf( stderr, "can't open \"%s\"\n", azArg[1] );
                                        }
                                        else
                                        {
                                          process_input( p, alt );
                                          alt.Close();// fclose( alt );
                                        }
                                      }
                                      else

                                        if ( c == 'r' && n >= 3 && strncmp( azArg[0], "restore", n ) == 0 && nArg > 1 )
                                        {
                                          string zSrcFile;
                                          string zDb;
                                          sqlite3 pSrc = null;
                                          sqlite3_backup pBackup;
                                          //int rc;
                                          int nTimeout = 0;

                                          if ( nArg == 2 )
                                          {
                                            zSrcFile = azArg[1].ToString();
                                            zDb = "main";
                                          }
                                          else
                                          {
                                            zSrcFile = azArg[2].ToString();
                                            zDb = azArg[1].ToString();
                                          }
                                          rc = Sqlite3.sqlite3_open( zSrcFile, out pSrc );
                                          if ( rc != Sqlite3.SQLITE_OK )
                                          {
                                            fprintf( stderr, "Error: cannot open %s\n", zSrcFile );
                                            Sqlite3.sqlite3_close( pSrc );
                                            return 1;
                                          }
                                          open_db( p );
                                          pBackup = Sqlite3.sqlite3_backup_init( p.db, zDb, pSrc, "main" );
                                          if ( pBackup == null )
                                          {
                                            fprintf( stderr, "Error: %s\n", Sqlite3.sqlite3_errmsg( p.db ) );
                                            Sqlite3.sqlite3_close( pSrc );
                                            return 1;
                                          }
                                          while ( ( rc = Sqlite3.sqlite3_backup_step( pBackup, 100 ) ) == Sqlite3.SQLITE_OK
                                                || rc == Sqlite3.SQLITE_BUSY )
                                          {
                                            if ( rc == Sqlite3.SQLITE_BUSY )
                                            {
                                              if ( nTimeout++ >= 3 )
                                                break;
                                              Sqlite3.sqlite3_sleep( 100 );
                                            }
                                          }
                                          Sqlite3.sqlite3_backup_finish( pBackup );
                                          if ( rc == Sqlite3.SQLITE_DONE )
                                          {
                                            rc = Sqlite3.SQLITE_OK;
                                          }
                                          else if ( rc == Sqlite3.SQLITE_BUSY || rc == Sqlite3.SQLITE_LOCKED )
                                          {
                                            fprintf( stderr, "source database is busy\n" );
                                          }
                                          else
                                          {
                                            fprintf( stderr, "Error: %s\n", Sqlite3.sqlite3_errmsg( p.db ) );
                                          }
                                          Sqlite3.sqlite3_close( pSrc );
                                        }
                                        else

                                          if ( c == 's' && strncmp( azArg[0], "schema", n ) == 0 )
                                          {
                                            callback_data data;
                                            string zErrMsg = null;
                                            open_db( p );
                                            data = p.Copy(); //memcpy( data, p, sizeof( data ) );
                                            data.showHeader = false;
                                            data.mode = MODE_Semi;
                                            if ( nArg > 1 )
                                            {
                                              //int i;
                                              azArg[1] = new StringBuilder( azArg[1].ToString().ToLower() );//for ( i = 0 ; azArg[1][i] ; i++ ) azArg[1][i] =  
                                              if ( strcmp( azArg[1], "sqlite_master" ) == 0 )
                                              {
                                                string[] new_argv = new string[2], new_colv = new string[2];
                                                new_argv[0] = "CREATE TABLE sqlite_master (\n" +
                                                              "  type text,\n" +
                                                              "  name text,\n" +
                                                              "  tbl_name text,\n" +
                                                              "  rootpage integer,\n" +
                                                              "  sql text\n" +
                                                              ")";
                                                new_argv[1] = null;
                                                new_colv[0] = "sql";
                                                new_colv[1] = null;
                                                callback( data, 1, new_argv, new_colv );
                                              }
                                              else if ( strcmp( azArg[1], "sqlite_temp_master" ) == 0 )
                                              {
                                                string[] new_argv = new string[2], new_colv = new string[2];
                                                new_argv[0] = "CREATE TEMP TABLE sqlite_temp_master (\n" +
                                                              "  type text,\n" +
                                                              "  name text,\n" +
                                                              "  tbl_name text,\n" +
                                                              "  rootpage integer,\n" +
                                                              "  sql text\n" +
                                                              ")";
                                                new_argv[1] = null;
                                                new_colv[0] = "sql";
                                                new_colv[1] = null;
                                                callback( data, 1, new_argv, new_colv );
                                              }
                                              else
                                              {
                                                zShellStatic = azArg[1].ToString();
                                                Sqlite3.sqlite3_exec( p.db,
                                                  "SELECT sql FROM " +
                                                  "  (SELECT sql sql, type type, tbl_name tbl_name, name name" +
                                                  "     FROM sqlite_master UNION ALL" +
                                                  "   SELECT sql, type, tbl_name, name FROM sqlite_temp_master) " +
                                                  "WHERE tbl_name LIKE shellstatic() AND type!='meta' AND sql NOTNULL " +
                                                  "ORDER BY substr(type,2,1), name",
                                                  (dxCallback)callback, data, ref zErrMsg );
                                                zShellStatic = "";
                                              }
                                            }
                                            else
                                            {
                                              Sqlite3.sqlite3_exec( p.db,
                                                 "SELECT sql FROM " +
                                                 "  (SELECT sql sql, type type, tbl_name tbl_name, name name" +
                                                 "     FROM sqlite_master UNION ALL" +
                                                 "   SELECT sql, type, tbl_name, name FROM sqlite_temp_master) " +
                                                 "WHERE type!='meta' AND sql NOTNULL AND name NOT LIKE 'sqlite_%'" +
                                                 "ORDER BY substr(type,2,1), name",
                                                 (dxCallback)callback, data, ref zErrMsg
                                              );
                                            }
                                            if ( zErrMsg != "" )
                                            {
                                              fprintf( stderr, "Error: %s\n", zErrMsg );
                                              //sqlite3_free( ref zErrMsg );
                                            }
                                          }
                                          else

                                            if ( c == 's' && strncmp( azArg[0], "separator", n ) == 0 && nArg == 2 )
                                            {
                                              snprintf( 2, ref p.separator,
                                                               "%.*s", 2 - 1, azArg[1] );
                                            }
                                            else

                                              if ( c == 's' && strncmp( azArg[0], "show", n ) == 0 )
                                              {
                                                int ii;
                                                fprintf( p._out, "%9.9s: %s\n", "echo", p.echoOn ? "on" : "off" );
                                                fprintf( p._out, "%9.9s: %s\n", "explain", p.explainPrev.valid ? "on" : "off" );
                                                fprintf( p._out, "%9.9s: %s\n", "headers", p.showHeader ? "on" : "off" );
                                                fprintf( p._out, "%9.9s: %s\n", "mode", modeDescr[p.mode] );
                                                fprintf( p._out, "%9.9s: ", "nullvalue" );
                                                output_c_string( p._out, p.nullvalue );
                                                fprintf( p._out, "\n" );
                                                fprintf( p._out, "%9.9s: %s\n", "output",
                                                        strlen30( p.outfile ) != 0 ? p.outfile.ToString() : "stdout" );
                                                fprintf( p._out, "%9.9s: ", "separator" );
                                                output_c_string( p._out, p.separator );
                                                fprintf( p._out, "\n" );
                                                fprintf( p._out, "%9.9s: ", "width" );
                                                for ( ii = 0; ii < (int)ArraySize( p.colWidth ) && p.colWidth[ii] != 0; ii++ )
                                                {
                                                  fprintf( p._out, "%d ", p.colWidth[ii] );
                                                }
                                                fprintf( p._out, "\n" );
                                              }
                                              else

                                                if ( c == 't' && n > 1 && strncmp( azArg[0], "tables", n ) == 0 )
                                                {
                                                  string[] azResult = null;
                                                  int nRow = 0;
                                                  string zErrMsg = "";
                                                  open_db( p );
                                                  if ( nArg == 1 )
                                                  {
                                                    int Dummy0 = 0;
                                                    rc = Sqlite3.sqlite3_get_table( p.db,
                                                      "SELECT name FROM sqlite_master " +
                                                      "WHERE type IN ('table','view') AND name NOT LIKE 'sqlite_%'" +
                                                      "UNION ALL " +
                                                      "SELECT name FROM sqlite_temp_master " +
                                                      "WHERE type IN ('table','view') " +
                                                      "ORDER BY 1",
                                                      ref azResult, ref nRow, ref Dummy0, ref zErrMsg
                                                    );
                                                  }
                                                  else
                                                  {
                                                    zShellStatic = azArg[1].ToString();
                                                    int Dummy0 = 0;
                                                    rc = Sqlite3.sqlite3_get_table( p.db,
                                                      "SELECT name FROM sqlite_master " +
                                                      "WHERE type IN ('table','view') AND name LIKE '%'||shellstatic()||'%' " +
                                                      "UNION ALL " +
                                                      "SELECT name FROM sqlite_temp_master " +
                                                      "WHERE type IN ('table','view') AND name LIKE '%'||shellstatic()||'%' " +
                                                      "ORDER BY 1",
                                                      ref azResult, ref nRow, ref Dummy0, ref zErrMsg
                                                    );
                                                    zShellStatic = "";
                                                  }
                                                  if ( zErrMsg != "" )
                                                  {
                                                    fprintf( stderr, "Error: %s\n", zErrMsg );
                                                    //sqlite3_free( ref zErrMsg );
                                                  }
                                                  if ( rc == Sqlite3.SQLITE_OK )
                                                  {
                                                    int len, maxlen = 0;
                                                    int ii, j;
                                                    int nPrintCol, nPrintRow;
                                                    for ( ii = 1; ii <= nRow; ii++ )
                                                    {
                                                      if ( azResult[ii] == null )
                                                        continue;
                                                      len = strlen30( azResult[ii] );
                                                      if ( len > maxlen )
                                                        maxlen = len;
                                                    }
                                                    nPrintCol = 80 / ( maxlen + 2 );
                                                    if ( nPrintCol < 1 )
                                                      nPrintCol = 1;
                                                    nPrintRow = ( nRow + nPrintCol - 1 ) / nPrintCol;
                                                    for ( ii = 0; ii < nPrintRow; ii++ )
                                                    {
                                                      for ( j = ii + 1; j <= nRow; j += nPrintRow )
                                                      {
                                                        string zSp = j <= nPrintRow ? "" : "  ";
                                                        printf( "%s%-*s", zSp, maxlen, azResult[j] != null ? azResult[j] : "" );
                                                      }
                                                      printf( "\n" );
                                                    }
                                                  }
                                                  else
                                                  {
                                                    rc = 1;
                                                  }
                                                  //sqlite.sqlite3_free_table( azResult );
                                                }
                                                else

                                                  if ( c == 't' && n > 4 && strncmp( azArg[0], "timeout", n ) == 0 && nArg >= 2 )
                                                  {
                                                    open_db( p );
                                                    Sqlite3.sqlite3_busy_timeout( p.db, Convert.ToInt32( azArg[1] ) );//atoi( azArg[1] ) );
                                                  }
                                                  else

#if HAS_TIMER  
if( c=='t' && n>=5 && strncmp(azArg[0], "timer", n)==0 && nArg>1 ){
enableTimer = booleanValue(azArg[1]);
}else
#endif

                                                    if ( c == 'w' && strncmp( azArg[0], "width", n ) == 0 )
                                                    {
                                                      int j;
                                                      Debug.Assert( nArg <= ArraySize( azArg ) );
                                                      for ( j = 1; j < nArg && j < ArraySize( p.colWidth ); j++ )
                                                      {
                                                        p.colWidth[j - 1] = Char.IsDigit( azArg[j][0] ) ? Convert.ToInt32( azArg[j].ToString() ) : 1;//atoi( azArg[j] );
                                                      }
                                                    }
                                                    else
                                                    {
                                                      fprintf( stderr, "unknown command or invalid arguments: " +
                                                        " \"%s\". Enter \".help\" for help\n", azArg[0] );
                                                    }

    return rc;
  }

  /*
  ** Return TRUE if a semicolon occurs anywhere in the first N characters
  ** of string z[].
  */
  static bool _contains_semicolon( string z, int N )
  {
    int i;
    for ( i = 0; i < N; i++ )
    {
      if ( z[i] == ';' )
        return true;
    }
    return false;
  }

  /*
  ** Test to see if a line consists entirely of whitespace.
  */
  static bool _all_whitespace( string z )
  {
    return z.Trim().Length == 0;
    //int zIdx = 0;
    //for ( ; zIdx < z.Length ; zIdx++ )
    //{
    //  if ( isspace( z[zIdx] ) ) continue;
    //  if ( z[zIdx] == '/' && z[zIdx + 1] == '*' )
    //  {
    //    zIdx += 2;
    //    while ( zIdx < z.Length && ( z[zIdx] != '*' || z[zIdx + 1] != '/' ) ) { zIdx++; }
    //    if ( zIdx == z.Length ) return false;
    //    zIdx++;
    //    continue;
    //  }
    //  if ( z[zIdx] == '-' && z[zIdx + 1] == '-' )
    //  {
    //    zIdx += 2;
    //    while ( zIdx < z.Length && z[zIdx] != '\n' ) { zIdx++; }
    //    if ( zIdx == z.Length ) return true;
    //    continue;
    //  }
    //  return false;
    //}
    //return true;
  }

  /*
  ** Return TRUE if the line typed in is an SQL command terminator other
  ** than a semi-colon.  The SQL Server style "go" command is understood
  ** as is the Oracle "/".
  */
  static int _is_command_terminator( string zLine )
  {
    zLine = zLine.Trim();// while ( isspace( zLine ) ) { zLine++; };
    if ( zLine.Length == 0 )
      return 0;
    if ( zLine[0] == '/' )//&& _all_whitespace(zLine[1]) )
    {
      return 1;  /* Oracle */
    }
    if ( Char.ToLower( zLine[0] ) == 'g' && Char.ToLower( zLine[1] ) == 'o' )
    //&& _all_whitespace(&zLine[2]) )
    {
      return 1;  /* SQL Server */
    }
    return 0;
  }

  /*
  ** Return true if zSql is a complete SQL statement.  Return false if it
  ** ends in the middle of a string literal or C-style comment.
  */
  static int _is_complete( string zSql, int nSql )
  {
    int rc;
    if ( zSql == null )
      return 1;
    //zSql[nSql] = ';';
    //zSql[nSql + 1] = '\0';
    rc = Sqlite3.sqlite3_complete( zSql + ";\0" );
    //zSql[nSql] = '\0';
    return rc;
  }

  /*
  ** Read input from _in and process it.  If _in==0 then input
  ** is interactive - the user is typing it it.  Otherwise, input
  ** is coming from a file or device.  A prompt is issued and history
  ** is saved only if input is interactive.  An interrupt signal will
  ** cause this routine to exit immediately, unless input is interactive.
  **
  ** Return the number of errors.
  */
  static int process_input( callback_data p, TextReader _in )
  {
    StringBuilder zLine = new StringBuilder();
    ;
    StringBuilder zSql = new StringBuilder();
    int nSql = 0;
    int nSqlPrior = 0;
    string zErrMsg = null;
    int rc;
    int errCnt = 0;
    int lineno = 0;
    int startline = 0;

    while ( errCnt == 0 || !bail_on_error || ( _in == null && stdin_is_interactive ) )
    {
      fflush( p._out );
      //free( ref zLine );
      zLine.Length = 0;
      zLine.Append( one_input_line( zSql, _in ) );
      if ( _in != null && ( (System.IO.StreamReader)( _in ) ).EndOfStream )
      {
        break;  /* We have reached EOF */
      }
      if ( seenInterrupt )
      {
        if ( _in != null )
          break;
        seenInterrupt = false;
      }
      lineno++;
      if ( p.echoOn )
        printf( "%s\n", zLine );
      if ( ( zSql == null || zSql.Length == 0 ) && _all_whitespace( zLine.ToString() ) )
        continue;
      if ( zLine.Length != 0 && zLine[0] == '.' && nSql == 0 )
      {
        rc = do_meta_command( zLine, p );
        if ( rc == 2 )
        {
          break;
        }
        else if ( rc != 0 )
        {
          errCnt++;
        }
        continue;
      }
      if ( _is_command_terminator( zLine.ToString() ) != 0 && _is_complete( zSql.ToString(), nSql ) != 0 )
      {
        zLine.Append( ";" );// memcpy( zLine, ";", 2 );
      }
      nSqlPrior = nSql;
      if ( zSql.Length == 0 )
      {
        int i;
        for ( i = 0; zLine[i] != '\0' && isspace( zLine[i] ); i++ )
        {
        }
        if ( zLine[i] != 0 )
        {
          nSql = strlen30( zLine );
          zSql.Length = 0;
          zSql.Capacity = nSql + 3;// malloc( nSql + 3 );
          //if ( zSql == null )
          //{
          //  fprintf( stderr, "out of memory\n" );
          //  exit( 1 );
          //}
          zSql.Append( zLine.ToString(), 0, nSql );// memcpy( zSql, zLine, nSql + 1 );
          startline = lineno;
        }
      }
      else
      {
        int len = strlen30( zLine );
        //zSql = realloc( zSql, nSql + len + 4 );
        //if ( zSql == null )
        //{
        //  fprintf( stderr, "%s: out of memory!\n", Argv0 );
        //  exit( 1 );
        //}
        zSql.Append( '\n' );
        nSql++;
        zSql.Append( zLine.ToString(), 0, len );// memcpy( zSql[nSql], zLine, len + 1 );
        nSql += len;
      }
      if ( zSql.Length != 0 && _contains_semicolon( zSql.ToString().Substring( nSqlPrior ), nSql - nSqlPrior )
        && Sqlite3.sqlite3_complete( zSql.ToString() ) != 0 )
      {
        p.cnt = 0;
        open_db( p );
#if !(_WIN32) && !(WIN32) && !(__OS2__) && !(__RTP__) && !(_WRS_KERNEL)
BEGIN_TIMER;
#endif
        rc = Sqlite3.sqlite3_exec( p.db, zSql.ToString(), (dxCallback)callback, p, ref zErrMsg );
#if !(_WIN32) && !(WIN32) && !(__OS2__) && !(__RTP__) && !(_WRS_KERNEL)
END_TIMER;
#endif
        if ( rc != 0 || zErrMsg != "" )
        {
          string zPrefix = "";
          if ( _in != null || !stdin_is_interactive )
          {
            snprintf( 20, ref zPrefix,
                         "SQL error near line %d:", startline );
          }
          else
          {
            snprintf( 20, ref zPrefix, "SQL error:" );
          }
          if ( zErrMsg != "" )
          {
            printf( "%s %s\n", zPrefix, zErrMsg );
            //sqlite3_free( ref zErrMsg );
            zErrMsg = null;
          }
          else
          {
            printf( "%s %s\n", zPrefix, Sqlite3.sqlite3_errmsg( p.db ) );
          }
          errCnt++;
        }
        ///free( ref zSql );
        zSql.Length = 0;
        nSql = 0;
      }
    }
    if ( zSql != null )
    {
      if ( !_all_whitespace( zSql.ToString() ) )
        fprintf( stderr, "Incomplete SQL: %s\n", zSql );
      //free( ref zSql );
    }
    free( ref zLine );
    return errCnt;
  }

  /*
  ** Return a pathname which is the user's home directory
  */
  static string find_home_dir()
  {
    int p = (int)Environment.OSVersion.Platform;

    // If running on Unix
    if ( ( p == 4 ) || ( p == 6 ) || ( p == 128 ) )
      return Environment.GetFolderPath( Environment.SpecialFolder.Personal );

    string home_dir;

    home_dir = getenv( "USERPROFILE" );
    if ( "" == home_dir )
    {
      home_dir = getenv( "HOME" );
    }

    if ( "" == home_dir )
    {
      string zDrive, zPath;
      int n;
      zDrive = getenv( "HOMEDRIVE" );
      zPath = getenv( "HOMEPATH" );
      if ( zDrive != "" && zPath != "" )
      {
        n = strlen30( zDrive ) + strlen30( zPath ) + 1;
        home_dir = "";// malloc( n );
        //if ( home_dir == null ) return "";
        snprintf( n, ref home_dir, "%s%s", zDrive, zPath );
        return home_dir;
      }
      home_dir = "c:\\";
    }

    if ( home_dir == "" )
      home_dir = "\\";

    return home_dir;
  }

  /*
  ** Read input from the file given by sqliterc_override.  Or if that
  ** parameter is null;, take input from ~/.sqliterc
  */
  static void process_sqliterc(
  callback_data p,        /* Configuration data */
  string sqliterc_override   /* Name of config file. null; to use default */
  )
  {
    string home_dir = null;
    ;
    string sqliterc = sqliterc_override;
    //string zBuf = "";
    StreamReader _in = null;
    ;
    int nBuf;

    if ( sqliterc == null )
    {
      home_dir = find_home_dir();
      if ( home_dir == "" )
      {
        Console.Error.WriteLine( "{0}: cannot locate your home directory!\n", Environment.GetCommandLineArgs()[0] );
        return;
      }
      sqliterc = Path.Combine( home_dir, ".sqliterc" );
    }
    if ( File.Exists( sqliterc ) )
    {
      try
      {
        using ( _in = new StreamReader( sqliterc ) )
        {
          if ( stdin_is_interactive )
          {
            Console.WriteLine( "-- Loading resources from {0}", sqliterc );
          }
          process_input( p, _in );
        }
      }
      catch
      {
      }
    }
  }

  /*
  ** Show available command line options
  */
  static string zOptions =
  "   -init filename       read/process named file\n" +
  "   -echo                print commands before execution\n" +
  "   -[no]header          turn headers on or off\n" +
  "   -bail                stop after hitting an error\n" +
  "   -interactive         force interactive I/O\n" +
  "   -batch               force batch I/O\n" +
  "   -column              set output mode to 'column'\n" +
  "   -csv                 set output mode to 'csv'\n" +
  "   -html                set output mode to HTML\n" +
  "   -line                set output mode to 'line'\n" +
  "   -list                set output mode to 'list'\n" +
  "   -separator 'x'       set output field separator (|)\n" +
  "   -nullvalue 'text'    set text string for null; values\n" +
  "   -version             show C#-SQLite version\n"
  ;
  static void usage( int showDetail )
  {
    fprintf( stderr,
    "Usage: %s [OPTIONS] FILENAME [SQL]\n" +
    "FILENAME is the name of an SQLite database. A new database is created\n" +
    "if the file does not previously exist.\n", Argv0 );
    if ( showDetail != 0 )
    {
      fprintf( stderr, "OPTIONS include:\n%s", zOptions );
    }
    else
    {
      fprintf( stderr, "Use the -help option for additional information\n" );
    }
    exit( 1 );
  }

  /*
  ** Initialize the state information in data
  */
  static void main_init( ref callback_data data )
  {
    data = new callback_data();//memset(data, 0, sizeof(*data));
    data.mode = MODE_List;
    data.separator = "|";// memcpy( data.separator, "|", 2 );
    data.showHeader = false;
    snprintf( 20, ref mainPrompt, "C#-sqlite> " );
    snprintf( 20, ref continuePrompt, "   ...> " );
  }

  static int main( int argc, string[] argv )
  {
    string zErrMsg = null;
    callback_data data = null;
    string zInitFile = null;
    StringBuilder zFirstCmd = new StringBuilder();
    int i;
    int rc = 0;

    Argv0 = argv.Length == 0 ? null : argv[0];
    main_init( ref data );
    stdin_is_interactive = stdin.Equals( Console.In );// isatty( 0 );

    /* Make sure we have a valid signal handler early, before anything
    ** else is done.
    */
#if SIGINT
signal(SIGINT, interrupt_handler);
#endif

    /* Do an initial pass through the command-line argument to locate
** the name of the database file, the name of the initialization file,
** and the first command to execute.
*/
    for ( i = 0; i < argc - 1; i++ )
    {
      string z;
      if ( argv[i][0] != '-' )
        break;
      z = argv[i];
      int zDx = 0;
      if ( z[zDx] == '-' && z[zDx] == '-' )
        zDx++;
      if ( strcmp( argv[i], "-separator" ) == 0 || strcmp( argv[i], "-nullvalue" ) == 0 )
      {
        i++;
      }
      else if ( strcmp( argv[i], "-init" ) == 0 )
      {
        i++;
        zInitFile = argv[i];
      }
    }
    if ( i < argc )
    {
#if (SQLITE_OS_OS2) && SQLITE_OS_OS2
data.zDbFilename = (string )convertCpPathToUtf8( argv[i++] );
#else
      data.zDbFilename = argv[i++];
#endif
    }
    else
    {
#if !SQLITE_OMIT_MEMORYDB
      data.zDbFilename = ":memory:";
#else
data.zDbFilename = "";
#endif
    }
    if ( i < argc )
    {
      zFirstCmd.Append( argv[i++] );
    }
    data._out = stdout;

#if SQLITE_OMIT_MEMORYDB
if( data.zDbFilename=="" ){
fprintf(stderr, "C#-SQLite: Error, no database filename specified\n");
exit(0);
return 0;
}
#endif

    /* Go ahead and open the database file if it already exists.  If the
** file does not exist, delay opening it.  This prevents empty database
** files from being created if a user mistypes the database name argument
** to the sqlite command-line tool.
*/
    if ( File.Exists( data.zDbFilename ) )
    {
      open_db( data );
    }

    /* Process the initialization file if there is one.  If no -init option
    ** is given on the command line, look for a file named ~/.sqliterc and
    ** try to process it.
    */
    process_sqliterc( data, zInitFile );

    /* Make a second pass through the command-line argument and set
    ** options.  This second pass is delayed until after the initialization
    ** file is processed so that the command-line arguments will override
    ** settings in the initialization file.
    */
    for ( i = 1; i < argc && argv[i][0] == '-'; i++ )
    {
      string z = argv[i];
      if ( z[1] == '-' )
      {
        z.Remove( 0, 1 );
      }
      if ( strcmp( z, "-init" ) == 0 )
      {
        i++;
      }
      else if ( strcmp( z, "-html" ) == 0 )
      {
        data.mode = MODE_Html;
      }
      else if ( strcmp( z, "-list" ) == 0 )
      {
        data.mode = MODE_List;
      }
      else if ( strcmp( z, "-line" ) == 0 )
      {
        data.mode = MODE_Line;
      }
      else if ( strcmp( z, "-column" ) == 0 )
      {
        data.mode = MODE_Column;
      }
      else if ( strcmp( z, "-csv" ) == 0 )
      {
        data.mode = MODE_Csv;
        data.separator = ",";//memcpy( data.separator, ",", 2 );
      }
      else if ( strcmp( z, "-separator" ) == 0 )
      {
        i++;
        snprintf( 8, ref data.separator,
                 "%.*s", 8 - 1, argv[i] );
      }
      else if ( strcmp( z, "-nullvalue" ) == 0 )
      {
        i++;
        snprintf( 20, ref data.nullvalue,
                 "%.*s", (int)20 - 1, argv[i] );
      }
      else if ( strcmp( z, "-header" ) == 0 )
      {
        data.showHeader = true;
      }
      else if ( strcmp( z, "-noheader" ) == 0 )
      {
        data.showHeader = false;
      }
      else if ( strcmp( z, "-echo" ) == 0 )
      {
        data.echoOn = true;
      }
      else if ( strcmp( z, "-bail" ) == 0 )
      {
        bail_on_error = true;
      }
      else if ( strcmp( z, "-version" ) == 0 )
      {
        printf( "%s\n", Sqlite3.sqlite3_libversion() );
        return 0;
      }
      else if ( strcmp( z, "-interactive" ) == 0 )
      {
        stdin_is_interactive = true;
      }
      else if ( strcmp( z, "-batch" ) == 0 )
      {
        stdin_is_interactive = false;
      }
      else if ( strcmp( z, "-help" ) == 0 || strcmp( z, "--help" ) == 0 )
      {
        usage( 1 );
      }
      else
      {
        fprintf( stderr, "%s: unknown option: %s\n", Argv0, z );
        fprintf( stderr, "Use -help for a list of options.\n" );
        return 1;
      }
    }

    if ( zFirstCmd.Length > 0 )
    {
      /* Run just the command that follows the database name
      */
      if ( zFirstCmd[0] == '.' )
      {
        do_meta_command( zFirstCmd, data );
        exit( 0 );
      }
      else
      {
        //int rc;
        open_db( data );
        rc = Sqlite3.sqlite3_exec( data.db, zFirstCmd.ToString(), (dxCallback)callback, data, ref zErrMsg );
        if ( rc != 0 && zErrMsg != "" )
        {
          fprintf( stderr, "SQL error: %s\n", zErrMsg );
          exit( 1 );
        }
      }
    }
    else
    {
      /* Run commands received from standard input
      */
      if ( stdin_is_interactive )
      {
        string zHome;
        string zHistory = "";
        int nHistory;
        printf(
        "               C#-SQLite version %s\n" +
        "An independent reimplementation of the SQLite software library\n" +
        "==============================================================\n" +
        "\n" +
        "Enter \".help\" for instructions\n" +
        "Enter SQL statements terminated with a \";\"\n",
        Sqlite3.sqlite3_libversion()
        );
        zHome = find_home_dir();
        if ( zHome != "" )
        {
          nHistory = strlen30( zHome ) + 20;
          //if ( ( zHistory = malloc( nHistory ) ) != 0 )
          //{
          snprintf( nHistory, ref zHistory, "%s/.sqlite_history", zHome );
          //}
        }
#if (HAVE_READLINE) //&& HAVE_READLINE==1
if( zHistory ) read_history(zHistory);
#endif
        rc = process_input( data, null );
        if ( zHistory != null )
        {
          stifle_history( 100 );
          write_history( zHistory );
          free( ref zHistory );
        }
        free( ref zHome );
      }
      else
      {
        rc = process_input( data, stdin );
      }
    }
    set_table_name( data, null );
    if ( db != null )
    {
      if ( Sqlite3.sqlite3_close( db ) != Sqlite3.SQLITE_OK )
      {
        fprintf( stderr, "error closing database: %s\n", Sqlite3.sqlite3_errmsg( db ) );
      }
    }
    return rc;
  }

  // Helper Variables for C#
  static TextReader stdin = Console.In;
  static TextWriter stdout = Console.Out;
  static TextWriter stderr = Console.Error;

  // Helper Functions for C#
  private static void fflush( TextWriter tw )
  {
    tw.Flush();
  }
  private static int fgets( StringBuilder p, int p_2, TextReader _in )
  {
    try
    {
      p.Length = 0;
      p.Append( _in.ReadLine() );
      if ( p.Length > 0 )
        p.Append( '\n' );
      return p.Length;
    }
    catch
    {
      return 0;
    }
  }
  private static void fputc( char c, TextWriter _out )
  {
    _out.Write( c );
  }
  static void free( ref string z )
  {
    z = null;
  }
  static void free( ref StringBuilder z )
  {
    z = null;
  }
  private static string getenv( string p )
  {
    switch ( p )
    {
      case "USERPROFILE":
        return Environment.GetEnvironmentVariable( "UserProfile" );
      default:
        throw new Exception( "The method or operation is not implemented." );
    }
  }
  static bool isalnum( char c )
  {
    return char.IsLetterOrDigit( c );
  }
  static bool isalpha( char c )
  {
    return char.IsLetter( c );
  }
  static bool isdigit( char c )
  {
    return char.IsDigit( c );
  }
  private static bool isspace( char c )
  {
    return char.IsWhiteSpace( c );
  }
  //
  static bool isprint( char c )
  {
    return !char.IsControl( c );
  }
  private static void putc( char c, TextWriter _out )
  {
    _out.Write( c );
  }
  static int strcmp( string s0, string s1 )
  {
    return s0 == s1 ? 0 : 1;
  }
  static int strcmp( StringBuilder s0, string s1 )
  {
    return s0.ToString() == s1 ? 0 : 1;
  }
  static int strncmp( string s0, string s1, int n2 )
  {
    int n0 = n2 > s0.Length ? s0.Length : n2;
    int n1 = n2 > s1.Length ? s1.Length : n2;
    return s0.Substring( 0, n0 ).Equals( s1.Substring( 0, n1 ) ) ? 0 : 1;
  }
  static int strncmp( StringBuilder s0, string s1, int n2 )
  {
    int n0 = n2 > s0.Length ? s0.Length : n2;
    int n1 = n2 > s1.Length ? s1.Length : n2;
    return s0.ToString( 0, n0 ).Equals( s1.Substring( 0, n1 ) ) ? 0 : 1;
  }
  //
  static void fprintf( TextWriter tw, string zFormat, params va_list[] ap )
  {
    tw.Write( Sqlite3.sqlite3_mprintf( zFormat, ap ) );
  }
  static void printf( string zFormat, params va_list[] ap )
  {
    stdout.Write( Sqlite3.sqlite3_mprintf( zFormat, ap ) );
  }

  public static void Main( string[] args )
  {
    main( args.Length, args );
  }

  private static void exit( int p )
  {
    if ( p == 0 )
    {
      Console.WriteLine( "Enter to CONTINUE:" );
      Console.ReadKey();
    }
    else
    {
      throw new Exception( "The method or operation is not implemented." );
    }
  }

  private static void snprintf( int n, ref string zBuf, string zFormat, params va_list[] ap )
  {
    StringBuilder sbBuf = new StringBuilder( 100 );
    Sqlite3.sqlite3_snprintf( n, sbBuf, zFormat, ap );
    zBuf = sbBuf.ToString();
  }
}
