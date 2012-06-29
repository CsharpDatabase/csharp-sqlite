using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using FILE = System.IO.TextWriter;
using GETPROCTIMES = System.IntPtr;
using HANDLE = System.IntPtr;
using HINSTANCE = System.IntPtr;
using sqlite3_int64 = System.Int64;
using u32 = System.UInt32;
using va_list = System.Object;

namespace Community.CsharpSqlite
{
    using dxCallback = Sqlite3.dxCallback;
    using FILETIME = Sqlite3.FILETIME;
    using sqlite3 = Sqlite3.sqlite3;
    using sqlite3_stmt = Sqlite3.Vdbe;
    using sqlite3_value = Sqlite3.Mem;


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
        *************************************************************************
        **  Included in SQLite3 port to C#-SQLite;  2008 Noah B Hart
        **  C#-SQLite is an independent reimplementation of the SQLite software library 
        **
        *************************************************************************
        */
        //#if defined(_WIN32) || defined(WIN32)
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

        //#if !defined(_WIN32) && !defined(WIN32) && !defined(__OS2__)
        //# include <signal.h>
        //# if !defined(__RTP__) && !defined(_WRS_KERNEL)
        //#  include <pwd.h>
        //# endif
        //# include <unistd.h>
        //# include <sys/types.h>
        //#endif

        //#if __OS2__
        //# include <unistd.h>
        //#endif

        //#if HAVE_EDITLINE
        //# include <editline/editline.h>
        //#endif
        //#if defined(HAVE_READLINE) && HAVE_READLINE==1
        //# include <readline/readline.h>
        //# include <readline/history.h>
        //#endif
#if !(HAVE_EDITLINE) //&& (!(HAVE_READLINE) || HAVE_READLINE!=1)
        //# define readline(p) local_getline(p,stdin)
        static string readline(string p)
        {
            return local_getline(p, stdin);
        }
        //# define add_history(X)
        static void add_history(object p) { }
        //# define read_history(X)
        static void read_history(object p) { }
        //# define write_history(X)
        static void write_history(object p) { }
        //# define stifle_history(X)
        static void stifle_history(object p) { }
#endif

#if (_WIN32) || (WIN32)
        //# include <io.h>
        //#define isatty(h) _isatty(h)
        static bool isatty(object h) { return stdin.Equals(Console.In); }
        //#define access(f,m) _access((f),(m))
#else
/* Make sure isatty() has a prototype.
*/
extern int isatty();
#endif

        //#if defined(_WIN32_WCE)
        ///* Windows CE (arm-wince-mingw32ce-gcc) does not provide isatty()
        // * thus we always assume that we have a console. That can be
        // * overridden with the -batch command line option.
        // */
        //#define isatty(x) 1
        //#endif

        /* True if the timer is enabled */
        static bool enableTimer = false;

#if FALSE//!defined(_WIN32) && !defined(WIN32) && !defined(__OS2__) && !defined(__RTP__) && !defined(_WRS_KERNEL)
//#include <sys/time.h>
//#include <sys/resource.h>

///* Saved resource information for the beginning of an operation */
//static struct rusage sBegin;

///*
//** Begin timing an operation
//*/
//static void beginTimer(){
//  if( enableTimer ){
//    getrusage(RUSAGE_SELF, sBegin);
//  }
//}

///* Return the difference of two time_structs in seconds */
//static double timeDiff(timeval pStart, struct timeval pEnd){
//  return (pEnd.tv_usec - pStart.tv_usec)*0.000001 + 
//         (double)(pEnd.tv_sec - pStart.tv_sec);
//}

///*
//** Print the timing results.
//*/
//static void endTimer(){
//  if( enableTimer ){
//    struct rusage sEnd;
//    getrusage(RUSAGE_SELF, sEnd);
//    printf("CPU Time: user %f sys %f\n",
//       timeDiff(sBegin.ru_utime, sEnd.ru_utime),
//       timeDiff(sBegin.ru_stime, sEnd.ru_stime));
//  }
//}

//#define BEGIN_TIMER beginTimer()
//#define END_TIMER endTimer()
//#define HAS_TIMER 1

#elif ((_WIN32) || (WIN32))

        //#include <windows.h>

        /* Saved resource information for the beginning of an operation */
        static Process hProcess;
        //static FILETIME ftKernelBegin;
        //static FILETIME ftUserBegin;
        static TimeSpan tsUserBegin;
        static TimeSpan tsKernelBegin;

        //typedef BOOL (WINAPI *GETPROCTIMES)(HANDLE, LPFILETIME, LPFILETIME, LPFILETIME, LPFILETIME);

        /*
        ** Check to see if we have timer support.  Return 1 if necessary
        ** support found (or found previously).
        */
        //static bool has_timer()
        //{
        //    if (getProcessTimesAddr != IntPtr.Zero)
        //    {
        //        return true;
        //    }
        //    else
        //    {
        //        /* GetProcessTimes() isn't supported in WIN95 and some other Windows versions.
        //        ** See if the version we are running on has it, and if it does, save off
        //        ** a pointer to it and the current process handle.
        //        */
        //        hProcess = Process.GetCurrentProcess();
        //        if (hProcess != null)
        //        {
        //            HINSTANCE hinstLib = LoadLibrary("Kernel32.dll");
        //            if (null != hinstLib)
        //            {
        //                getProcessTimesAddr = (GETPROCTIMES)GetProcAddress(hinstLib, "GetProcessTimes");
        //                if (null != getProcessTimesAddr)
        //                {
        //                    return true;
        //                }
        //                FreeLibrary(hinstLib);
        //            }
        //        }
        //    }
        //    return true;
        //}

        /*
        ** Begin timing an operation
        */
        static void beginTimer()
        {
            if (enableTimer)//&& getProcessTimesAddr != IntPtr.Zero)
            {
                //FILETIME ftCreation, ftExit;
                //getProcessTimesAddr(hProcess, ftCreation, ftExit, ftKernelBegin, ftUserBegin);
                tsUserBegin = Process.GetCurrentProcess().UserProcessorTime;
                tsKernelBegin = Process.GetCurrentProcess().TotalProcessorTime - Process.GetCurrentProcess().UserProcessorTime;
            }
        }

        /* Return the difference of two TimeSpan structs in seconds */
        static double timeDiff(TimeSpan pStart, TimeSpan pEnd)
        {
            //sqlite3_int64 i64Start = ((sqlite3_int64)pStart.dwLowDateTime);
            //sqlite3_int64 i64End = ((sqlite3_int64)pEnd.dwLowDateTime);
            //return (double)((i64End - i64Start) / 10000000.0);
            return timeDiff(pStart, pEnd) / 10000000.0;
        }

        /*
        ** Print the timing results.
        */
        static void endTimer()
        {
            if (enableTimer)// && getProcessTimesAddr != IntPtr.Zero)
            {
                //FILETIME ftCreation, ftExit, ftKernelEnd, ftUserEnd;
                //getProcessTimesAddr(hProcess, ftCreation, ftExit, ftKernelEnd, ftUserEnd);
                TimeSpan tsKernelEnd, tsUserEnd;
                tsUserEnd = Process.GetCurrentProcess().UserProcessorTime;
                tsKernelEnd = Process.GetCurrentProcess().TotalProcessorTime - Process.GetCurrentProcess().UserProcessorTime;

                printf("CPU Time: user %f sys %f\n",
                timeDiff(tsUserBegin, tsUserEnd),
                timeDiff(tsKernelBegin, tsKernelEnd));
            }
        }

        //#define BEGIN_TIMER beginTimer()
        //#define END_TIMER endTimer()
        //#define HAS_TIMER HAS_TIMER
        static bool HAS_TIMER = true;
#else
//#define BEGIN_TIMER 
//#define END_TIMER
//#define HAS_TIMER 0
#endif

        /*
** Used to prevent warnings about unused parameters
*/
        //#define UNUSED_PARAMETER(x) ()(x)
        static void UNUSED_PARAMETER<T>(T x) { }

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
        static sqlite3 db = null;

        /*
        ** True if an interrupt (Control-C) has been received.
        */
        static bool seenInterrupt = false;

        /*
        ** This is the name of our program. It is set in main(), used
        ** in a number of other places, mostly for error messages.
        */
        static string Argv0;

        /*
        ** Prompt strings. Initialized in main. Settable with
        **   .prompt main continue
        */
        static string mainPrompt;      /* First line prompt. default: "sqlite> "*/
        static string continuePrompt;  /* Continuation prompt. default: "   ...> " */

        /*
        ** Write I/O traces to the following stream.
        */
#if SQLITE_ENABLE_IOTRACE
static FILE iotrace = null;
#endif

        /*
** This routine works like printf in that its first argument is a
** format string and subsequent arguments are values to be substituted
** in place of % fields.  The result of formatting this string
** is written to iotrace.
*/
#if SQLITE_ENABLE_IOTRACE
static void iotracePrintf(string zFormat, ...){
va_list ap;
string z;
if( iotrace== null ) return;
va_start(ap, zFormat);
z = Sqlite3.SQLITE_vmprintf(zFormat, ap);
va_end(ap);
fprintf(iotrace, "%s", z);
Sqlite3.sqlite3_free(z);
}
#endif


        /*
** Determines if a string is a number of not.
*/
        //static int isNumber(string z, ref int realnum){
        //  if( *z=='-' || *z=='+' ) z++;
        //  if( !isdigit(*z) ){
        //    return 0;
        //  }
        //  z++;
        //  //if( realnum ) *realnum = 0;
        //      realnum = 0;
        //  while( isdigit(*z) ){ z++; }
        //  if( *z=='.' ){
        //    z++;
        //    if( !isdigit(*z) ) return 0;
        //    while( isdigit(*z) ){ z++; }
        //    //if( realnum ) *realnum = 1;
        //    realnum = 1;
        //  }
        //  if( *z=='e' || *z=='E' ){
        //    z++;
        //    if( *z=='+' || *z=='-' ) z++;
        //    if( !isdigit(*z) ) return 0;
        //    while( isdigit(*z) ){ z++; }
        //    //if( realnum ) *realnum = 1;
        //    realnum = 1;
        //  }
        //  return *z== null;
        //}
        static bool isNumber(string z)
        {
            int i = 0;
            return isNumber(z, ref i);
        }
        static bool isNumber(string z, int i)
        {
            return isNumber(z, ref i);
        }
        static bool isNumber(string z, ref int realnum)
        {
            int zIdx = 0;
            if (z[zIdx] == '-' || z[zIdx] == '+')
                zIdx++;
            if (zIdx == z.Length || !isdigit(z[zIdx]))
            {
                return false;
            }
            zIdx++;
            realnum = 0;
            while (zIdx < z.Length && isdigit(z[zIdx]))
            {
                zIdx++;
            }
            if (z[zIdx] == '.')
            {
                zIdx++;
                if (zIdx < z.Length && !isdigit(z[zIdx]))
                    return false;
                while (zIdx < z.Length && isdigit(z[zIdx]))
                {
                    zIdx++;
                }
                realnum = 1;
            }
            if (z[zIdx] == 'e' || z[zIdx] == 'E')
            {
                zIdx++;
                if (zIdx < z.Length && (z[zIdx] == '+' || z[zIdx] == '-'))
                    zIdx++;
                if (zIdx == z.Length || !isdigit(z[zIdx]))
                    return false;
                while (zIdx < z.Length && isdigit(z[zIdx]))
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
        ** Sqlite3.sqlite3_exec_printf() API to substitue a string into an SQL statement.
        ** The correct way to do this with sqlite3 is to use the bind API, but
        ** since the shell is built around the callback paradigm it would be a lot
        ** of work. Instead just use this hack, which is quite harmless.
        */
        static string zShellStatic = "";
        static void shellstaticFunc(
        Sqlite3.sqlite3_context context,
        int argc,
        sqlite3_value[] argv
        )
        {
            Debug.Assert(0 == argc);
            Debug.Assert(String.IsNullOrEmpty(zShellStatic));
            UNUSED_PARAMETER(argc);
            UNUSED_PARAMETER(argv);
            Sqlite3.sqlite3_result_text(context, zShellStatic, -1, Sqlite3.SQLITE_STATIC);
        }


        /*
        ** This routine reads a line of text from FILE in, stores
        ** the text in memory obtained from malloc() and returns a pointer
        ** to the text.  null is returned at end of file, or if malloc()
        ** fails.
        **
        ** The interface is like "readline" but no command-line editing
        ** is done.
        */
        static string local_getline(string zPrompt, TextReader In)
        {
            StringBuilder zIn = new StringBuilder();
            StringBuilder zLine;
            int nLine;
            int n;
            bool eol;

            if (zPrompt != null)
            {
                printf("%s", zPrompt);
                fflush(stdout);
            }
            nLine = 100;
            zLine = new StringBuilder(nLine);//malloc( nLine );
            //if( zLine== null ) return 0;
            n = 0;
            eol = false;
            while (!eol)
            {
                if (n + 100 > nLine)
                {
                    nLine = nLine * 2 + 100;
                    zLine.Capacity = (nLine);//= realloc(zLine, nLine);
                    //if (zLine == null)
                    //    return null;
                }
                if (fgets(zIn, nLine - n, In) == 0)
                {
                    if (zLine.Length == 0)
                    {
                        zLine = null;//free(zLine);
                        return null;
                    }
                    //zLine[n] = 0;
                    eol = true;
                    break;
                }
                n = 0;
                while (n < zLine.Length && zLine[n] != '\0') { n++; }
                n = zIn.Length - 1;
                if (zIn[n] == '\n')
                {
                    n--;
                    if (n > 0 && zIn[n - 1] == '\r')
                        n--;

                    zIn.Length = n + 1;
                    eol = true;
                }
                zLine.Append(zIn);
            }
            //zLine = realloc( zLine, n+1 );
            return zLine.ToString();
        }

        /*
        ** Retrieve a single line of input text.
        **
        ** zPrior is a string of prior text retrieved.  If not the empty
        ** string, then issue a continuation prompt.
        */
        static string one_input_line(string zPrior, TextReader In)
        {
            string zPrompt;
            string zResult;
            if (In != null)
            {
                return local_getline("", In).ToString();
            }
            if (zPrior != null && zPrior.Length > 0)
            {
                zPrompt = continuePrompt;
            }
            else
            {
                zPrompt = mainPrompt;
            }
            zResult = readline(zPrompt);
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
            public int[] colWidth = new int[200];
        };

        /*
        ** An pointer to an instance of this structure is passed from
        ** the main program to the callback.  This is used to communicate
        ** state and mode information.
        */
        class callback_data
        {
            public sqlite3 db;            /* The database */
            public bool echoOn;           /* True to echo input commands */
            public bool statsOn;          /* True to display memory stats before each finalize */
            public int cnt;               /* Number of records displayed so far */
            public FILE Out;             /* Write results here */
            public int mode;              /* An output mode setting */
            public bool writableSchema;  /* True if PRAGMA writable_schema=ON */
            public bool showHeader;      /* True to show column names in List or Column mode */
            public string zDestTable;     /* Name of destination table when MODE_Insert */
            public string separator = ""; /* Separator character for MODE_List */
            public int[] colWidth = new int[200];      /* Requested width of each column when in column mode*/
            public int[] actualWidth = new int[200];   /* Actual width of each column */
            public string nullvalue = "NULL";          /* The text to print when a null comes back from
** the database */
            public previous_mode_data explainPrev = new previous_mode_data();
            /* Holds the mode information just before
            ** .explain ON */
            public StringBuilder outfile = new StringBuilder(260); /* Filename for Out */
            public string zDbFilename;    /* name of the database file */
            public string zVfs;           /* Name of VFS to use */
            public sqlite3_stmt pStmt;   /* Current statement if any. */
            public FILE pLog;            /* Write log output here */

            internal callback_data Copy()
            {
                return (callback_data)this.MemberwiseClone();
            }
        };
        // Store callback data variant 
        class callback_data_extra
        {
            public string[] azCols; //(string *)pData;      /* Names of result columns */
            public string[] azVals;//azCols[nCol];         /* Results */
            public int[] aiTypes;   //(int *)&azVals[nCol]; /* Result types */
        }
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
        const int MODE_Line = 0;
        const int MODE_Column = 1;
        const int MODE_List = 2;
        const int MODE_Semi = 3;
        const int MODE_Html = 4;
        const int MODE_Insert = 5;
        const int MODE_Tcl = 6;
        const int MODE_Csv = 7;
        const int MODE_Explain = 8;

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
        static int ArraySize<T>(T[] X) { return X.Length; }

        /*
        ** Compute a string length that is limited to what can be stored in
        ** lower 30 bits of a 32-bit signed integer.
        */
        static int strlen30(StringBuilder z)
        {
            //string z2 = z;
            //while( *z2 ){ z2++; }
            return 0x3fffffff & z.Length;//(int)(z2 - z);
        }
        static int strlen30(string z)
        {
            //string z2 = z;
            //while( *z2 ){ z2++; }
            return 0x3fffffff & z.Length;//(int)(z2 - z);
        }


        /*
        ** A callback for the Sqlite3.SQLITE_log() interface.
        */
        static void shellLog(object pArg, int iErrCode, string zMsg)
        {
            callback_data p = (callback_data)pArg;
            if (p.pLog == null)
                return;
            fprintf(p.pLog, "(%d) %s\n", iErrCode, zMsg);
            fflush(p.pLog);
        }

        /*
        ** Output the given string as a hex-encoded blob (eg. X'1234' )
        */
        static void output_hex_blob(FILE Out, byte[] pBlob, int nBlob)
        {
            int i;
            //string zBlob = (string )pBlob;
            fprintf(Out, "X'");
            for (i = 0; i < nBlob; i++) { fprintf(Out, "%02x", pBlob[i]); }
            fprintf(Out, "'");
        }

        /*
        ** Output the given string as a quoted string using SQL quoting conventions.
        */
        static void output_quoted_string(TextWriter Out, string z)
        {
            int i;
            int nSingle = 0;
            for (i = 0; z[i] != '\0'; i++)
            {
                if (z[i] == '\'')
                    nSingle++;
            }
            if (nSingle == 0)
            {
                fprintf(Out, "'%s'", z);
            }
            else
            {
                fprintf(Out, "'");
                while (z != "")
                {
                    for (i = 0; i < z.Length && z[i] != '\''; i++)
                    {
                    }
                    if (i == 0)
                    {
                        fprintf(Out, "''");
                        //z++;
                    }
                    else if (z[i] == '\'')
                    {
                        fprintf(Out, "%.*s''", i, z);
                        //z += i + 1;
                    }
                    else
                    {
                        fprintf(Out, "%s", z);
                        break;
                    }
                }
                fprintf(Out, "'");
            }
        }

        /*
        ** Output the given string as a quoted according to C or TCL quoting rules.
        */
        static void output_c_string(TextWriter Out, string z)
        {
            char c;
            fputc('"', Out);
            int zIdx = 0;
            while (zIdx < z.Length && (c = z[zIdx++]) != '\0')
            {
                if (c == '\\')
                {
                    fputc(c, Out);
                    fputc(c, Out);
                }
                else if (c == '\t')
                {
                    fputc('\\', Out);
                    fputc('t', Out);
                }
                else if (c == '\n')
                {
                    fputc('\\', Out);
                    fputc('n', Out);
                }
                else if (c == '\r')
                {
                    fputc('\\', Out);
                    fputc('r', Out);
                }
                else if (!isprint(c))
                {
                    fprintf(Out, "\\%03o", c & 0xff);
                }
                else
                {
                    fputc(c, Out);
                }
            }
            fputc('"', Out);
        }


        /*
        ** Output the given string with characters that are special to
        ** HTML escaped.
        */
        static void output_html_string(TextWriter Out, string z)
        {
            int i;
            while (z != "")
            {
                for (i = 0; i < z.Length && z[i] != '<' && z[i] != '&'; i++)
                {
                }
                if (i > 0)
                {
                    fprintf(Out, "%.*s", i, z);
                }
                if (i < z.Length && z[i] == '<')
                {
                    fprintf(Out, "&lt;");
                }
                else if (i < z.Length && z[i] == '&')
                {
                    fprintf(Out, "&amp;");
                }
                else if (i < z.Length && z[i] == '\"')
                {
                    fprintf(Out, "&quot;");
                }
                else if (i < z.Length && z[i] == '\'')
                {
                    fprintf(Out, "&#39;");
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
        static void output_csv(callback_data p, string z, bool bSep)
        {
            TextWriter Out = p.Out;
            if (z == null)
            {
                fprintf(Out, "%s", p.nullvalue);
            }
            else
            {
                int i;
                int nSep = strlen30(p.separator);
                for (i = 0; i < z.Length; i++)
                {
                    if (needCsvQuote[z[i]] != 0
                    || (z[i] == p.separator[0] &&
                    (nSep == 1 || z == p.separator)))
                    {
                        i = 0;
                        break;
                    }
                }
                if (i == 0)
                {
                    putc('"', Out);
                    for (i = 0; i < z.Length; i++)
                    {
                        if (z[i] == '"')
                            putc('"', Out);
                        putc(z[i], Out);
                    }
                    putc('"', Out);
                }
                else
                {
                    fprintf(Out, "%s", z);
                }
            }
            if (bSep)
            {
                fprintf(p.Out, "%s", p.separator);
            }
        }


#if SIGINT
/*
** This routine runs when the user presses Ctrl-C
*/
static void interrupt_handler(int NotUsed){
UNUSED_PARAMETER(NotUsed);
seenInterrupt = 1;
if( db ) Sqlite3.SQLITE_interrupt(db);
}
#endif

        /*
** This is the callback routine that the shell
** invokes for each row of a query result.
*/
        static int shell_callback(object pArg, sqlite3_int64 nArg, object p2, object p3)
        {
            int i;
            callback_data p = (callback_data)pArg;

            //Unpack
            string[] azArg = ((callback_data_extra)p2).azVals;
            string[] azCol = ((callback_data_extra)p2).azCols;
            int[] aiType = ((callback_data_extra)p2).aiTypes;

            switch (p.mode)
            {
                case MODE_Line:
                    {
                        int w = 5;
                        if (azArg == null)
                            break;
                        for (i = 0; i < nArg; i++)
                        {
                            int len = strlen30(azCol[i] != null ? azCol[i] : "");
                            if (len > w)
                                w = len;
                        }
                        if (p.cnt++ > 0)
                            fprintf(p.Out, "\n");
                        for (i = 0; i < nArg; i++)
                        {
                            fprintf(p.Out, "%*s = %s\n", w, azCol[i],
                            azArg[i] != null ? azArg[i] : p.nullvalue);
                        }
                        break;
                    }
                case MODE_Explain:
                case MODE_Column:
                    {
                        if (p.cnt++ == 0)
                        {
                            for (i = 0; i < nArg; i++)
                            {
                                int w, n;
                                if (i < ArraySize(p.colWidth))
                                {
                                    w = p.colWidth[i];
                                }
                                else
                                {
                                    w = 0;
                                }
                                if (w <= 0)
                                {
                                    w = strlen30(azCol[i] != null ? azCol[i] : "");
                                    if (w < 10)
                                        w = 10;
                                    n = strlen30(azArg != null && azArg[i] != null ? azArg[i] : p.nullvalue);
                                    if (w < n)
                                        w = n;
                                }
                                if (i < ArraySize(p.actualWidth))
                                {
                                    p.actualWidth[i] = w;
                                }
                                if (p.showHeader)
                                {
                                    fprintf(p.Out, "%-*.*s%s", w, w, azCol[i], i == nArg - 1 ? "\n" : "  ");
                                }
                            }
                            if (p.showHeader)
                            {
                                for (i = 0; i < nArg; i++)
                                {
                                    int w;
                                    if (i < ArraySize(p.actualWidth))
                                    {
                                        w = p.actualWidth[i];
                                    }
                                    else
                                    {
                                        w = 10;
                                    }
                                    fprintf(p.Out, "%-*.*s%s", w, w, "-----------------------------------" +
                                    "----------------------------------------------------------",
                                    i == nArg - 1 ? "\n" : "  ");
                                }
                            }
                        }
                        if (azArg == null)
                            break;
                        for (i = 0; i < nArg; i++)
                        {
                            int w;
                            if (i < ArraySize(p.actualWidth))
                            {
                                w = p.actualWidth[i];
                            }
                            else
                            {
                                w = 10;
                            }
                            if (p.mode == MODE_Explain && azArg[i] != null &&
                            strlen30(azArg[i]) > w)
                            {
                                w = strlen30(azArg[i]);
                            }
                            fprintf(p.Out, "%-*.*s%s", w, w,
                            azArg[i] != null ? azArg[i] : p.nullvalue, i == nArg - 1 ? "\n" : "  ");
                        }
                        break;
                    }
                case MODE_Semi:
                case MODE_List:
                    {
                        if (p.cnt++ == null && p.showHeader)
                        {
                            for (i = 0; i < nArg; i++)
                            {
                                fprintf(p.Out, "%s%s", azCol[i], i == nArg - 1 ? "\n" : p.separator);
                            }
                        }
                        if (azArg == null)
                            break;
                        for (i = 0; i < nArg; i++)
                        {
                            string z = azArg[i];
                            if (z == null)
                                z = p.nullvalue;
                            fprintf(p.Out, "%s", z);
                            if (i < nArg - 1)
                            {
                                fprintf(p.Out, "%s", p.separator);
                            }
                            else if (p.mode == MODE_Semi)
                            {
                                fprintf(p.Out, ";\n");
                            }
                            else
                            {
                                fprintf(p.Out, "\n");
                            }
                        }
                        break;
                    }
                case MODE_Html:
                    {
                        if (p.cnt++ == null && p.showHeader)
                        {
                            fprintf(p.Out, "<TR>");
                            for (i = 0; i < nArg; i++)
                            {
                                fprintf(p.Out, "<TH>");
                                output_html_string(p.Out, azCol[i]);
                                fprintf(p.Out, "</TH>\n");
                            }
                            fprintf(p.Out, "</TR>\n");
                        }
                        if (azArg == null)
                            break;
                        fprintf(p.Out, "<TR>");
                        for (i = 0; i < nArg; i++)
                        {
                            fprintf(p.Out, "<TD>");
                            output_html_string(p.Out, azArg[i] != null ? azArg[i] : p.nullvalue);
                            fprintf(p.Out, "</TD>\n");
                        }
                        fprintf(p.Out, "</TR>\n");
                        break;
                    }
                case MODE_Tcl:
                    {
                        if (p.cnt++ == null && p.showHeader)
                        {
                            for (i = 0; i < nArg; i++)
                            {
                                output_c_string(p.Out, azCol[i] != null ? azCol[i] : "");
                                fprintf(p.Out, "%s", p.separator);
                            }
                            fprintf(p.Out, "\n");
                        }
                        if (azArg == null)
                            break;
                        for (i = 0; i < nArg; i++)
                        {
                            output_c_string(p.Out, azArg[i] != null ? azArg[i] : p.nullvalue);
                            fprintf(p.Out, "%s", p.separator);
                        }
                        fprintf(p.Out, "\n");
                        break;
                    }
                case MODE_Csv:
                    {
                        if (p.cnt++ == null && p.showHeader)
                        {
                            for (i = 0; i < nArg; i++)
                            {
                                output_csv(p, azCol[i] != null ? azCol[i] : "", i < nArg - 1);
                            }
                            fprintf(p.Out, "\n");
                        }
                        if (azArg == null)
                            break;
                        for (i = 0; i < nArg; i++)
                        {
                            output_csv(p, azArg[i], i < nArg - 1);
                        }
                        fprintf(p.Out, "\n");
                        break;
                    }
                case MODE_Insert:
                    {
                        p.cnt++;
                        if (azArg == null)
                            break;
                        fprintf(p.Out, "INSERT INTO %s VALUES(", p.zDestTable);
                        for (i = 0; i < nArg; i++)
                        {
                            string zSep = i > 0 ? "," : "";
                            if ((azArg[i] == null) || (aiType != null && i < aiType.Length && aiType[i] == Sqlite3.SQLITE_NULL))
                            {
                                fprintf(p.Out, "%snull", zSep);
                            }
                            else if (aiType != null && aiType[i] == Sqlite3.SQLITE_TEXT)
                            {
                                if (!String.IsNullOrEmpty(zSep))
                                    fprintf(p.Out, "%s", zSep);
                                output_quoted_string(p.Out, azArg[i]);
                            }
                            else if (aiType != null && (aiType[i] == Sqlite3.SQLITE_INTEGER || aiType[i] == Sqlite3.SQLITE_FLOAT))
                            {
                                fprintf(p.Out, "%s%s", zSep, azArg[i]);
                            }
                            else if (aiType != null && aiType[i] == Sqlite3.SQLITE_BLOB && p.pStmt != null)
                            {
                                byte[] pBlob = Sqlite3.sqlite3_column_blob(p.pStmt, i);
                                int nBlob = Sqlite3.sqlite3_column_bytes(p.pStmt, i);
                                if (!String.IsNullOrEmpty(zSep))
                                    fprintf(p.Out, "%s", zSep);
                                output_hex_blob(p.Out, pBlob, nBlob);
                            }
                            else if (isNumber(azArg[i], 0))
                            {
                                fprintf(p.Out, "%s%s", zSep, azArg[i]);
                            }
                            else
                            {
                                if (!String.IsNullOrEmpty(zSep))
                                    fprintf(p.Out, "%s", zSep);
                                output_quoted_string(p.Out, azArg[i]);
                            }
                        }
                        fprintf(p.Out, ");\n");
                        break;
                    }
            }
            return 0;
        }

        /*
        ** This is the callback routine that the SQLite library
        ** invokes for each row of a query result.
        */
        static int callback(object pArg, sqlite3_int64 nArg, object azArg, object azCol)
        {
            /* since we don't have type info, call the shell_callback with a null value */
            callback_data_extra cde = new callback_data_extra();
            cde.azVals = (string[])azArg;
            cde.azCols = (string[])azCol;
            cde.aiTypes = null;
            return shell_callback(pArg, (int)nArg, cde, null);
        }

        /*
        ** Set the destination table field of the callback_data structure to
        ** the name of the table given.  Escape any quote characters in the
        ** table name.
        */
        static void set_table_name(callback_data p, string zName)
        {
            int i, n;
            bool needQuote;
            string z = "";

            if (p.zDestTable != null)
            {
                //free(ref p.zDestTable);
                p.zDestTable = null;
            }
            if (zName == null)
                return;
            needQuote = !isalpha(zName[0]) && zName != "_";
            for (i = n = 0; i < zName.Length; i++, n++)
            {
                if (!isalnum(zName[i]) && zName[i] != '_')
                {
                    needQuote = true;
                    if (zName[i] == '\'')
                        n++;
                }
            }
            if (needQuote)
                n += 2;
            //z = p.zDestTable  = malloc( n + 1 );
            //if ( z == 0 )
            //{
            //  fprintf( stderr, "Out of memory!\n" );
            //  exit( 1 );
            //}
            //n = 0;
            if (needQuote)
                z += '\'';
            for (i = 0; i < zName.Length; i++)
            {
                z += zName[i];
                if (zName[i] == '\'')
                    z += '\'';
            }
            if (needQuote)
                z += '\'';
            //z[n] = 0;
            p.zDestTable = z;
        }

        /* zIn is either a pointer to a null-terminated string in memory obtained
        ** from malloc(), or a null pointer. The string pointed to by zAppend is
        ** added to zIn, and the result returned in memory obtained from malloc().
        ** zIn, if it was not null, is freed.
        **
        ** If the third argument, quote, is not '\0', then it is used as a 
        ** quote character for zAppend.
        */
        static void appendText(StringBuilder zIn, string zAppend, int noQuote)
        { appendText(zIn, zAppend, '\0'); }

        static void appendText(StringBuilder zIn, string zAppend, char quote)
        {
            int len;
            int i;
            int nAppend = strlen30(zAppend);
            int nIn = (zIn != null ? strlen30(zIn) : 0);

            len = nAppend + nIn;
            if (quote != '\0')
            {
                len += 2;
                for (i = 0; i < nAppend; i++)
                {
                    if (zAppend[i] == quote)
                        len++;
                }
            }

            //zIn = realloc( zIn, len );
            //if ( !zIn )
            //{
            //  return 0;
            //}

            if (quote != '\0')
            {
                zIn.Append(quote);
                for (i = 0; i < nAppend; i++)
                {
                    zIn.Append(zAppend[i]);
                    if (zAppend[i] == quote)
                        zIn.Append(quote);
                }
                zIn.Append(quote);
                //zCsr++ = '\0';
                Debug.Assert(zIn.Length == len);

            }
            else
            {
                zIn.Append(zAppend);//memcpy( zIn[nIn], zAppend, nAppend );
                //zIn[len - 1] = '\0';
            }
        }



        /*
        ** Execute a query statement that has a single result column.  Print
        ** that result column on a line by itself with a semicolon terminator.
        **
        ** This is used, for example, to show the schema of the database by
        ** querying the Sqlite3.SQLITE_MASTER table.
        */
        static int run_table_dump_query(
        FILE Out,          /* Send output here */
        sqlite3 db,        /* Database to query */
        StringBuilder zSelect,    /* SELECT statement to extract content */
        string zFirstRow   /* Print before first row, if not null */
        )
        { return run_table_dump_query(Out, db, zSelect.ToString(), zFirstRow); }

        static int run_table_dump_query(
        FILE Out,          /* Send output here */
        sqlite3 db,        /* Database to query */
        string zSelect,    /* SELECT statement to extract content */
        string zFirstRow   /* Print before first row, if not null */
        )
        {
            sqlite3_stmt pSelect = null;
            int rc;
            rc = Sqlite3.sqlite3_prepare(db, zSelect, -1, ref pSelect, 0);
            if (rc != Sqlite3.SQLITE_OK || null == pSelect)
            {
                return rc;
            }
            rc = Sqlite3.sqlite3_step(pSelect);
            while (rc == Sqlite3.SQLITE_ROW)
            {
                if (zFirstRow != null)
                {
                    fprintf(Out, "%s", zFirstRow);
                    zFirstRow = null;
                }
                fprintf(Out, "%s;\n", Sqlite3.sqlite3_column_text(pSelect, 0));
                rc = Sqlite3.sqlite3_step(pSelect);
            }
            return Sqlite3.sqlite3_finalize(pSelect);
        }

        /*
        ** Allocate space and save off current error string.
        */
        static string save_err_msg(
        sqlite3 db            /* Database to query */
        )
        {
            //int nErrMsg = 1 + strlen30(Sqlite3.sqlite3_errmsg(db));
            //string zErrMsg = Sqlite3.sqlite3_malloc(nErrMsg);
            //if (zErrMsg != null)
            //{
            //   memcpy(zErrMsg, Sqlite3.sqlite3_errmsg(db), nErrMsg);
            //}
            return Sqlite3.sqlite3_errmsg(db); //zErrMsg;
        }

        /*
        ** Display memory stats.
        */
        static int display_stats(
        sqlite3 db,                /* Database to query */
        callback_data pArg, /* Pointer to struct callback_data */
        int bReset                  /* True to reset the stats */
        )
        {
            int iCur;
            int iHiwtr;

            if (pArg != null && pArg.Out != null)
            {

                iHiwtr = iCur = -1;
                Sqlite3.sqlite3_status(Sqlite3.SQLITE_STATUS_MEMORY_USED, ref iCur, ref iHiwtr, bReset);
                fprintf(pArg.Out, "Memory Used:                         %d (max %d) bytes\n", iCur, iHiwtr);
                iHiwtr = iCur = -1;
                Sqlite3.sqlite3_status(Sqlite3.SQLITE_STATUS_MALLOC_COUNT, ref  iCur, ref iHiwtr, bReset);
                fprintf(pArg.Out, "Number of Outstanding Allocations:   %d (max %d)\n", iCur, iHiwtr);
                /*
                ** Not currently used by the CLI.
                **    iHiwtr = iCur = -1;
                **    Sqlite3.SQLITE_status(Sqlite3.SQLITE_STATUS_PAGECACHE_USED,ref iCur,ref iHiwtr, bReset);
                **    fprintf(pArg.Out, "Number of Pcache Pages Used:         %d (max %d) pages\n", iCur, iHiwtr);
                */
                iHiwtr = iCur = -1;
                Sqlite3.sqlite3_status(Sqlite3.SQLITE_STATUS_PAGECACHE_OVERFLOW, ref  iCur, ref iHiwtr, bReset);
                fprintf(pArg.Out, "Number of Pcache Overflow Bytes:     %d (max %d) bytes\n", iCur, iHiwtr);
                /*
                ** Not currently used by the CLI.
                **    iHiwtr = iCur = -1;
                **    Sqlite3.SQLITE_status(Sqlite3.SQLITE_STATUS_SCRATCH_USED,ref iCur,ref iHiwtr, bReset);
                **    fprintf(pArg.Out, "Number of Scratch Allocations Used:  %d (max %d)\n", iCur, iHiwtr);
                */
                iHiwtr = iCur = -1;
                Sqlite3.sqlite3_status(Sqlite3.SQLITE_STATUS_SCRATCH_OVERFLOW, ref  iCur, ref  iHiwtr, bReset);
                fprintf(pArg.Out, "Number of Scratch Overflow Bytes:    %d (max %d) bytes\n", iCur, iHiwtr);
                iHiwtr = iCur = -1;
                Sqlite3.sqlite3_status(Sqlite3.SQLITE_STATUS_MALLOC_SIZE, ref  iCur, ref  iHiwtr, bReset);
                fprintf(pArg.Out, "Largest Allocation:                  %d bytes\n", iHiwtr);
                iHiwtr = iCur = -1;
                Sqlite3.sqlite3_status(Sqlite3.SQLITE_STATUS_PAGECACHE_SIZE, ref  iCur, ref iHiwtr, bReset);
                fprintf(pArg.Out, "Largest Pcache Allocation:           %d bytes\n", iHiwtr);
                iHiwtr = iCur = -1;
                Sqlite3.sqlite3_status(Sqlite3.SQLITE_STATUS_SCRATCH_SIZE, ref  iCur, ref iHiwtr, bReset);
                fprintf(pArg.Out, "Largest Scratch Allocation:          %d bytes\n", iHiwtr);
#if YYTRACKMAXSTACKDEPTH
iHiwtr = iCur = -1;
Sqlite3.SQLITE_status(Sqlite3.SQLITE_STATUS_PARSER_STACK,ref iCur,ref iHiwtr, bReset);
fprintf(pArg.Out, "Deepest Parser Stack:                %d (max %d)\n", iCur, iHiwtr);
#endif
            }

            if (pArg != null && pArg.Out != null && db != null)
            {
                iHiwtr = iCur = -1;
                Sqlite3.sqlite3_db_status(db, Sqlite3.SQLITE_DBSTATUS_LOOKASIDE_USED, ref iCur, ref iHiwtr, bReset);
                fprintf(pArg.Out, "Lookaside Slots Used:                %d (max %d)\n", iCur, iHiwtr);
                Sqlite3.sqlite3_db_status(db, Sqlite3.SQLITE_DBSTATUS_LOOKASIDE_HIT, ref iCur, ref iHiwtr, bReset);
                fprintf(pArg.Out, "Successful lookaside attempts:       %d\n", iHiwtr);
                Sqlite3.sqlite3_db_status(db, Sqlite3.SQLITE_DBSTATUS_LOOKASIDE_MISS_SIZE, ref iCur, ref iHiwtr, bReset);
                fprintf(pArg.Out, "Lookaside failures due to size:      %d\n", iHiwtr);
                Sqlite3.sqlite3_db_status(db, Sqlite3.SQLITE_DBSTATUS_LOOKASIDE_MISS_FULL, ref iCur, ref iHiwtr, bReset);
                fprintf(pArg.Out, "Lookaside failures due to OOM:       %d\n", iHiwtr);
                iHiwtr = iCur = -1;
                Sqlite3.sqlite3_db_status(db, Sqlite3.SQLITE_DBSTATUS_CACHE_USED, ref iCur, ref iHiwtr, bReset);
                fprintf(pArg.Out, "Pager Heap Usage:                    %d bytes\n", iCur);
                iHiwtr = iCur = -1;
                Sqlite3.sqlite3_db_status(db, Sqlite3.SQLITE_DBSTATUS_SCHEMA_USED, ref iCur, ref iHiwtr, bReset);
                fprintf(pArg.Out, "Schema Heap Usage:                   %d bytes\n", iCur);
                iHiwtr = iCur = -1;
                Sqlite3.sqlite3_db_status(db, Sqlite3.SQLITE_DBSTATUS_STMT_USED, ref iCur, ref iHiwtr, bReset);
                fprintf(pArg.Out, "Statement Heap/Lookaside Usage:      %d bytes\n", iCur);
            }

            if (pArg != null && pArg.Out != null && db != null && pArg.pStmt != null)
            {
                iCur = Sqlite3.sqlite3_stmt_status(pArg.pStmt, Sqlite3.SQLITE_STMTSTATUS_FULLSCAN_STEP, bReset);
                fprintf(pArg.Out, "Fullscan Steps:                      %d\n", iCur);
                iCur = Sqlite3.sqlite3_stmt_status(pArg.pStmt, Sqlite3.SQLITE_STMTSTATUS_SORT, bReset);
                fprintf(pArg.Out, "Sort Operations:                     %d\n", iCur);
                iCur = Sqlite3.sqlite3_stmt_status(pArg.pStmt, Sqlite3.SQLITE_STMTSTATUS_AUTOINDEX, bReset);
                fprintf(pArg.Out, "Autoindex Inserts:                   %d\n", iCur);
            }

            return 0;
        }

        /*
        ** Execute a statement or set of statements.  Print 
        ** any result rows/columns depending on the current mode 
        ** set via the supplied callback.
        **
        ** This is very similar to SQLite's built-in Sqlite3.sqlite3_exec() 
        ** function except it takes a slightly different callback 
        ** and callback data argument.
        */
        static int shell_exec(
        sqlite3 db,                       /* An open database */
        string zSql,                      /* SQL to be evaluated */
        dxCallback xCallback,             //int (*xCallback)(void*,int,char**,char**,int*),   /* Callback function */
            /* (not the same as Sqlite3.sqlite3_exec) */
        callback_data pArg,               /* Pointer to struct callback_data */
        ref string pzErrMsg               /* Error msg written here */
        )
        {
            sqlite3_stmt pStmt = null;        /* Statement to execute. */
            int rc = Sqlite3.SQLITE_OK;       /* Return Code */
            string zLeftover = null;          /* Tail of unprocessed SQL */

            //  if( pzErrMsg )
            {
                pzErrMsg = null;
            }

            while (!String.IsNullOrEmpty(zSql) && (Sqlite3.SQLITE_OK == rc))
            {
                rc = Sqlite3.sqlite3_prepare_v2(db, zSql, -1, ref pStmt, ref zLeftover);
                if (Sqlite3.SQLITE_OK != rc)
                {
                    //if (pzErrMsg != null)
                    {
                        pzErrMsg = save_err_msg(db);
                    }
                }
                else
                {
                    if (null == pStmt)
                    {
                        /* this happens for a comment or white-space */
                        zSql = zLeftover.TrimStart();
                        //while (isspace(zSql[0]))
                        //    zSql++;
                        continue;
                    }

                    /* save off the prepared statment handle and reset row count */
                    if (pArg != null)
                    {
                        pArg.pStmt = pStmt;
                        pArg.cnt = 0;
                    }

                    /* echo the sql statement if echo on */
                    if (pArg != null && pArg.echoOn)
                    {
                        string zStmtSql = Sqlite3.sqlite3_sql(pStmt);
                        fprintf(pArg.Out, "%s\n", zStmtSql != null ? zStmtSql : zSql);
                    }

                    /* perform the first step.  this will tell us if we
                    ** have a result set or not and how wide it is.
                    */
                    rc = Sqlite3.sqlite3_step(pStmt);
                    /* if we have a result set... */
                    if (Sqlite3.SQLITE_ROW == rc)
                    {
                        /* if we have a callback... */
                        if (xCallback != null)
                        {
                            /* allocate space for col name ptr, value ptr, and type */
                            int nCol = Sqlite3.sqlite3_column_count(pStmt);
                            //void pData = Sqlite3.SQLITE_malloc(3*nCol*sizeof(const char*) + 1);
                            //if( !pData ){
                            //  rc = Sqlite3.SQLITE_NOMEM;
                            //}else
                            {
                                string[] azCols = new string[nCol];//(string *)pData;    /* Names of result columns */
                                string[] azVals = new string[nCol];//azCols[nCol];       /* Results */
                                int[] aiTypes = new int[nCol];//(int *)&azVals[nCol]; /* Result types */
                                int i;
                                //Debug.Assert(sizeof(int) <= sizeof(string )); 
                                /* save off ptrs to column names */
                                for (i = 0; i < nCol; i++)
                                {
                                    azCols[i] = (string)Sqlite3.sqlite3_column_name(pStmt, i);
                                }
                                do
                                {
                                    /* extract the data and data types */
                                    for (i = 0; i < nCol; i++)
                                    {
                                        azVals[i] = (string)Sqlite3.sqlite3_column_text(pStmt, i);
                                        aiTypes[i] = Sqlite3.sqlite3_column_type(pStmt, i);
                                        if (null == azVals[i] && (aiTypes[i] != Sqlite3.SQLITE_NULL))
                                        {
                                            rc = Sqlite3.SQLITE_NOMEM;
                                            break; /* from for */
                                        }
                                    } /* end for */

                                    /* if data and types extracted successfully... */
                                    if (Sqlite3.SQLITE_ROW == rc)
                                    {
                                        /* call the supplied callback with the result row data */
                                        callback_data_extra cde = new callback_data_extra();
                                        cde.azVals = azVals;
                                        cde.azCols = azCols;
                                        cde.aiTypes = aiTypes;

                                        if (xCallback(pArg, nCol, cde, null) != 0)
                                        {
                                            rc = Sqlite3.SQLITE_ABORT;
                                        }
                                        else
                                        {
                                            rc = Sqlite3.sqlite3_step(pStmt);
                                        }
                                    }
                                } while (Sqlite3.SQLITE_ROW == rc);
                                //Sqlite3.sqlite3_free(ref pData);
                            }
                        }
                        else
                        {
                            do
                            {
                                rc = Sqlite3.sqlite3_step(pStmt);
                            } while (rc == Sqlite3.SQLITE_ROW);
                        }
                    }

                    /* print usage stats if stats on */
                    if (pArg != null && pArg.statsOn)
                    {
                        display_stats(db, pArg, 0);
                    }

                    /* Finalize the statement just executed. If this fails, save a 
                    ** copy of the error message. Otherwise, set zSql to point to the
                    ** next statement to execute. */
                    rc = Sqlite3.sqlite3_finalize(pStmt);
                    if (rc == Sqlite3.SQLITE_OK)
                    {
                        zSql = zLeftover.TrimStart();
                        //while (isspace(zSql[0]))
                        //    zSql++;
                    }
                    else //if (pzErrMsg)
                    {
                        pzErrMsg = save_err_msg(db);
                    }

                    /* clear saved stmt handle */
                    if (pArg != null)
                    {
                        pArg.pStmt = null;
                    }
                }
            } /* end while */

            return rc;
        }


        /*
        ** This is a different callback routine used for dumping the database.
        ** Each row received by this callback consists of a table name,
        ** the table type ("index" or "table") and SQL to create the table.
        ** This routine should print text sufficient to recreate the table.
        */
        static int dump_callback(object pArg, sqlite3_int64 nArg, object pazArg, object pazCol)
        {
            int rc;
            string zTable;
            string zType;
            string zSql;
            string zPrepStmt = null;
            callback_data p = (callback_data)pArg;
            string[] azArg = (string[])pazArg;
            string[] azCol = (string[])pazCol;

            UNUSED_PARAMETER(azCol);
            if (nArg != 3)
                return 1;
            zTable = azArg[0];
            zType = azArg[1];
            zSql = azArg[2];

            if (zTable.Equals("sqlite_sequence", StringComparison.InvariantCultureIgnoreCase))
            {
                zPrepStmt = "DELETE FROM sqlite_sequence;\n";
            }
            else if (zTable.Equals("sqlite_stat1", StringComparison.InvariantCultureIgnoreCase))
            {
                fprintf(p.Out, "ANALYZE Sqlite3.SQLITE_master;\n");
            }
            else if (zTable.StartsWith("SQLITE_", StringComparison.InvariantCultureIgnoreCase))
            {
                return 0;
            }
            else if (zSql.StartsWith("CREATE VIRTUAL TABLE", StringComparison.InvariantCultureIgnoreCase))
            {
                string zIns;
                if (!p.writableSchema)
                {
                    fprintf(p.Out, "PRAGMA writable_schema=ON;\n");
                    p.writableSchema = true;
                }
                zIns = Sqlite3.sqlite3_mprintf(
                "INSERT INTO Sqlite3.SQLITE_master(type,name,tbl_name,rootpage,sql)" +
                "VALUES('table','%q','%q',0,'%q');",
                zTable, zTable, zSql);
                fprintf(p.Out, "%s\n", zIns);
                zIns = null;//Sqlite3.sqlite3_free(zIns);
                return 0;
            }
            else
            {
                fprintf(p.Out, "%s;\n", zSql);
            }

            if (zType.Equals("table", StringComparison.InvariantCultureIgnoreCase))
            {
                sqlite3_stmt pTableInfo = null;
                StringBuilder zSelect = new StringBuilder();
                StringBuilder zTableInfo = new StringBuilder();
                StringBuilder zTmp = new StringBuilder();
                int nRow = 0;

                appendText(zTableInfo, "PRAGMA table_info(", 0);
                appendText(zTableInfo, zTable, '"');
                appendText(zTableInfo, ");", 0);

                rc = Sqlite3.sqlite3_prepare(p.db, zTableInfo, -1, ref pTableInfo, 0);
                zTableInfo = null;//free(zTableInfo);
                if (rc != Sqlite3.SQLITE_OK || null == pTableInfo)
                {
                    return 1;
                }

                appendText(zSelect, "SELECT 'INSERT INTO ' || ", 0);
                appendText(zTmp, zTable, '"');
                //if (zTmp!=null)
                {
                    appendText(zSelect, zTmp.ToString(), '\'');
                }
                appendText(zSelect, " || ' VALUES(' || ", 0);
                rc = Sqlite3.sqlite3_step(pTableInfo);
                while (rc == Sqlite3.SQLITE_ROW)
                {
                    string zText = (string)Sqlite3.sqlite3_column_text(pTableInfo, 1);
                    appendText(zSelect, "quote(", 0);
                    appendText(zSelect, zText, '"');
                    rc = Sqlite3.sqlite3_step(pTableInfo);
                    if (rc == Sqlite3.SQLITE_ROW)
                    {
                        appendText(zSelect, ") || ',' || ", 0);
                    }
                    else
                    {
                        appendText(zSelect, ") ", 0);
                    }
                    nRow++;
                }
                rc = Sqlite3.sqlite3_finalize(pTableInfo);
                if (rc != Sqlite3.SQLITE_OK || nRow == 0)
                {
                    zSelect = null;//free(zSelect);
                    return 1;
                }
                appendText(zSelect, "|| ')' FROM  ", 0);
                appendText(zSelect, zTable, '"');

                rc = run_table_dump_query(p.Out, p.db, zSelect, zPrepStmt);
                if (rc == Sqlite3.SQLITE_CORRUPT)
                {
                    appendText(zSelect, " ORDER BY rowid DESC", 0);
                    rc = run_table_dump_query(p.Out, p.db, zSelect, "");
                }
                if (zSelect != null)
                    zSelect = null;//free(zSelect);
            }
            return 0;
        }

        /*
        ** Run zQuery.  Use dump_callback() as the callback routine so that
        ** the contents of the query are output as SQL statements.
        **
        ** If we get a Sqlite3.SQLITE_CORRUPT error, rerun the query after appending
        ** "ORDER BY rowid DESC" to the end.
        */
        static int run_schema_dump_query(
        callback_data p,
        string zQuery,
        string pzErrMsg
        )
        {
            int rc;
            rc = Sqlite3.sqlite3_exec(p.db, zQuery, dump_callback, p, ref pzErrMsg);
            if (rc == Sqlite3.SQLITE_CORRUPT)
            {
                StringBuilder zQ2;
                int len = strlen30(zQuery);
                if (pzErrMsg != null)
                    pzErrMsg = null;//Sqlite3.sqlite3_free(pzErrMsg);
                zQ2 = new StringBuilder(len + 100);//zQ2 = malloc(len + 100);
                //if (zQ2 == null)
                //    return rc;
                Sqlite3.sqlite3_snprintf(zQ2.Capacity, zQ2, "%s ORDER BY rowid DESC", zQuery);
                rc = Sqlite3.sqlite3_exec(p.db, zQ2.ToString(), dump_callback, p, ref pzErrMsg);
                zQ2 = null;//free(zQ2);
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
        "                         If TABLE specified, only dump tables matching\n" +
        "                         LIKE pattern TABLE.\n" +
        ".echo ON|OFF           Turn command echo on or off\n" +
        ".exit                  Exit this program\n" +
        ".explain ?ON|OFF?      Turn output mode suitable for EXPLAIN on or off.\n" +
        "                         With no args, it turns EXPLAIN on.\n" +
        ".header(s) ON|OFF      Turn display of headers on or off\n" +
        ".help                  Show this message\n" +
        ".import FILE TABLE     Import data from FILE into TABLE\n" +
        ".indices ?TABLE?       Show names of all indices\n" +
        "                         If TABLE specified, only show indices for tables\n" +
        "                         matching LIKE pattern TABLE.\n" +
#if SQLITE_ENABLE_IOTRACE
".iotrace FILE          Enable I/O diagnostic logging to FILE\n" +
#endif
#if !SQLITE_OMIT_LOAD_EXTENSION
 ".load FILE ?ENTRY?     Load an extension library\n" +
#endif
 ".log FILE|off          Turn logging on or off.  FILE can be stderr/stdout\n" +
        ".mode MODE ?TABLE?     Set output mode where MODE is one of:\n" +
        "                         csv      Comma-separated values\n" +
        "                         column   Left-aligned columns.  (See .width)\n" +
        "                         html     HTML <table> code\n" +
        "                         insert   SQL insert statements for TABLE\n" +
        "                         line     One value per line\n" +
        "                         list     Values delimited by .separator string\n" +
        "                         tabs     Tab-separated values\n" +
        "                         tcl      TCL list elements\n" +
        ".nullvalue STRING      Print STRING in place of null values\n" +
        ".output FILENAME       Send output to FILENAME\n" +
        ".output stdout         Send output to the screen\n" +
        ".prompt MAIN CONTINUE  Replace the standard prompts\n" +
        ".quit                  Exit this program\n" +
        ".read FILENAME         Execute SQL in FILENAME\n" +
        ".restore ?DB? FILE     Restore content of DB (default \"main\") from FILE\n" +
        ".schema ?TABLE?        Show the CREATE statements\n" +
        "                         If TABLE specified, only show tables matching\n" +
        "                         LIKE pattern TABLE.\n" +
        ".separator STRING      Change separator used by output mode and .import\n" +
        ".show                  Show the current values for various settings\n" +
        ".stats ON|OFF          Turn stats on or off\n" +
        ".tables ?TABLE?        List names of tables\n" +
        "                         If TABLE specified, only list tables matching\n" +
        "                         LIKE pattern TABLE.\n" +
        ".timeout MS            Try opening locked tables for MS milliseconds\n" +
        ".width NUM1 NUM2 ...   Set column widths for \"column\" mode\n"
        ;

        static string zTimerHelp =
        ".timer ON|OFF          Turn the CPU timer measurement on or off\n"
        ;

        /* Forward reference */
        //static int process_input(callback_data p, FILE In);

        /*
        ** Make sure the database is open.  If it is not, then open it.  If
        ** the database fails to open, print an error message and exit.
        */
        static void open_db(callback_data p)
        {
            if (p.db == null)
            {
                Sqlite3.sqlite3_open(p.zDbFilename, out p.db);
                db = p.db;
                if (db != null && Sqlite3.sqlite3_errcode(db) == Sqlite3.SQLITE_OK)
                {
                    Sqlite3.sqlite3_create_function(db, "shellstatic", 0, Sqlite3.SQLITE_UTF8, 0,
                    shellstaticFunc, null, null);
                }
                if (db == null || Sqlite3.SQLITE_OK != Sqlite3.sqlite3_errcode(db))
                {
                    fprintf(stderr, "Error: unable to open database \"%s\": %s\n",
                    p.zDbFilename, Sqlite3.sqlite3_errmsg(db));
                    exit(1);
                }
#if !SQLITE_OMIT_LOAD_EXTENSION
                Sqlite3.sqlite3_enable_load_extension(p.db, 1);
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
        static void resolve_backslashes(ref string z)
        {
            StringBuilder sb = new StringBuilder(z);
            resolve_backslashes(sb);
            z = sb.ToString();
        }
        static void resolve_backslashes(StringBuilder z)
        {
            int i, j;
            char c;
            for (i = j = 0; i < z.Length && (c = z[i]) != 0; i++, j++)
            {
                if (c == '\\')
                {
                    c = z[++i];
                    if (c == 'n')
                    {
                        c = '\n';
                    }
                    else if (c == 't')
                    {
                        c = '\t';
                    }
                    else if (c == 'r')
                    {
                        c = '\r';
                    }
                    else if (c >= '0' && c <= '7')
                    {
                        c -= '0';
                        if (z[i + 1] >= '0' && z[i + 1] <= '7')
                        {
                            i++;
                            c = (char)((c << 3) + z[i] - '0');
                            if (z[i + 1] >= '0' && z[i + 1] <= '7')
                            {
                                i++;
                                c = (char)((c << 3) + z[i] - '0');
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
        static bool booleanValue(StringBuilder zArg)
        {
            return booleanValue(zArg.ToString());
        }

        static bool booleanValue(string zArg)
        {
            if (String.IsNullOrEmpty(zArg))
                return false;
            int val = Char.IsDigit(zArg[0]) ? Convert.ToInt32(zArg) : 0;// atoi( zArg );
            int j;
            //for (j = 0; zArg[j]; j++)
            //{
            //    zArg[j] = (char)tolower(zArg[j]);
            //}
            if (zArg.Equals("on", StringComparison.InvariantCultureIgnoreCase))
            {
                val = 1;
            }
            else if (zArg.Equals("yes", StringComparison.InvariantCultureIgnoreCase))
            {
                val = 1;
            }
            return val != 0;
        }

        class _aCtrl
        {
            public string zCtrlName;    /* Name of a test-control option */
            public int ctrlCode;        /* Integer code for that option */

            public _aCtrl(string zCtrlName, int ctrlCode)
            {
                this.zCtrlName = zCtrlName;
                this.ctrlCode = ctrlCode;
            }
        }

        /*
        ** If an input line begins with "." then invoke this routine to
        ** process that line.
        **
        ** Return 1 on error, 2 to exit, and 0 otherwise.
        */
        static int do_meta_command(StringBuilder zLine, callback_data p)
        {
            int i = 1;
            int i0 = 0;
            int nArg = 0;
            int n, c;
            int rc = 0;
            string[] azArg = new string[50];

            /* Parse the input line into tokens.
            */
            while (i < zLine.Length && nArg < ArraySize(azArg))
            {
                while (i < zLine.Length && Char.IsWhiteSpace(zLine[i])) { i++; }
                if (i == zLine.Length)
                    break;
                if (zLine[i] == '\'' || zLine[i] == '"')
                {
                    int delim = zLine[i++];
                    i0 = i;
                    azArg[nArg++] = zLine[i].ToString();
                    while (zLine[i] != '\0' && zLine[i] != delim) { i++; }
                    if (zLine[i] == delim)
                    {
                        zLine[i++] = '\0';
                    }
                    if (delim == '"')
                        resolve_backslashes(ref azArg[nArg - 1]);
                }
                else
                {
                    i0 = i;
                    while (i < zLine.Length && !isspace(zLine[i])) { i++; }
                    azArg[nArg++] = zLine.ToString().Substring(i0, i - i0);
                    //if (i < zLine.Length - 1)
                    //    zLine[i++] = '\0';
                    resolve_backslashes(ref azArg[nArg - 1]);
                }
            }

            /* Process the input line.
            */
            if (nArg == 0)
                return 0; /* no tokens, no error */
            n = strlen30(azArg[0]);
            c = azArg[0][0];
            if (c == 'b' && n >= 3 && azArg[0].StartsWith("backup", StringComparison.InvariantCultureIgnoreCase) && nArg > 1 && nArg < 4)
            {
                string zDestFile;
                string zDb;
                sqlite3 pDest;
                Sqlite3.sqlite3_backup pBackup;
                if (nArg == 2)
                {
                    zDestFile = azArg[1];
                    zDb = "main";
                }
                else
                {
                    zDestFile = azArg[2];
                    zDb = azArg[1];
                }
                rc = Sqlite3.sqlite3_open(zDestFile, out pDest);
                if (rc != Sqlite3.SQLITE_OK)
                {
                    fprintf(stderr, "Error: cannot open \"%s\"\n", zDestFile);
                    Sqlite3.sqlite3_close(pDest);
                    return 1;
                }
                open_db(p);
                pBackup = Sqlite3.sqlite3_backup_init(pDest, "main", p.db, zDb);
                if (pBackup == null)
                {
                    fprintf(stderr, "Error: %s\n", Sqlite3.sqlite3_errmsg(pDest));
                    Sqlite3.sqlite3_close(pDest);
                    return 1;
                }
                while ((rc = Sqlite3.sqlite3_backup_step(pBackup, 100)) == Sqlite3.SQLITE_OK) { }
                Sqlite3.sqlite3_backup_finish(pBackup);
                if (rc == Sqlite3.SQLITE_DONE)
                {
                    rc = 0;
                }
                else
                {
                    fprintf(stderr, "Error: %s\n", Sqlite3.sqlite3_errmsg(pDest));
                    rc = 1;
                }
                Sqlite3.sqlite3_close(pDest);
            }
            else

                if (c == 'b' && n >= 3 && azArg[0].StartsWith("bail", StringComparison.InvariantCultureIgnoreCase) && nArg > 1 && nArg < 3)
                {
                    bail_on_error = booleanValue(azArg[1]);
                }
                else

                    if (c == 'd' && n > 1 && azArg[0].StartsWith("databases", StringComparison.InvariantCultureIgnoreCase) && nArg == 1)
                    {
                        callback_data data;
                        string zErrMsg = null;
                        open_db(p);
                        data = p.Copy();// memcpy(data, p, sizeof(data));
                        data.showHeader = true;
                        data.mode = MODE_Column;
                        data.colWidth[0] = 3;
                        data.colWidth[1] = 15;
                        data.colWidth[2] = 58;
                        data.cnt = 0;
                        Sqlite3.sqlite3_exec(p.db, "PRAGMA database_list; ", callback, data, ref zErrMsg);
                        if (!String.IsNullOrEmpty(zErrMsg))
                        {
                            fprintf(stderr, "Error: %s\n", zErrMsg);
                            zErrMsg = null;//Sqlite3.sqlite3_free(zErrMsg);
                            rc = 1;
                        }
                    }
                    else

                        if (c == 'd' && azArg[0].StartsWith("dump", StringComparison.InvariantCultureIgnoreCase) && nArg < 3)
                        {
                            string zErrMsg = null;
                            open_db(p);
                            /* When playing back a "dump", the content might appear in an order
                            ** which causes immediate foreign key constraints to be violated.
                            ** So disable foreign-key constraint enforcement to prevent problems. */
                            fprintf(p.Out, "PRAGMA foreign_keys=OFF;\n");
                            fprintf(p.Out, "BEGIN TRANSACTION;\n");
                            p.writableSchema = false;
                            Sqlite3.sqlite3_exec(p.db, "PRAGMA writable_schema=ON", 0, 0, 0);
                            if (nArg == 1)
                            {
                                run_schema_dump_query(p,
                                "SELECT name, type, sql FROM sqlite_master " +
                                "WHERE sql NOT null AND type=='table' AND name!='Sqlite3.SQLITE_sequence'", null
                                );
                                run_schema_dump_query(p,
                                "SELECT name, type, sql FROM sqlite_master " +
                                "WHERE name=='Sqlite3.SQLITE_sequence'", null
                                );
                                run_table_dump_query(p.Out, p.db,
                                "SELECT sql FROM sqlite_master " +
                                "WHERE sql NOT null AND type IN ('index','trigger','view')", null
                                );
                            }
                            else
                            {
                                int ii;
                                for (ii = 1; ii < nArg; ii++)
                                {
                                    zShellStatic = azArg[ii];
                                    run_schema_dump_query(p,
                                    "SELECT name, type, sql FROM sqlite_master " +
                                    "WHERE tbl_name LIKE shellstatic() AND type=='table'" +
                                    "  AND sql NOT null", null);
                                    run_table_dump_query(p.Out, p.db,
                                    "SELECT sql FROM sqlite_master " +
                                    "WHERE sql NOT null" +
                                    "  AND type IN ('index','trigger','view')" +
                                    "  AND tbl_name LIKE shellstatic()", null
                                    );
                                    zShellStatic = null;
                                }
                            }
                            if (p.writableSchema)
                            {
                                fprintf(p.Out, "PRAGMA writable_schema=OFF;\n");
                                p.writableSchema = false;
                            }
                            Sqlite3.sqlite3_exec(p.db, "PRAGMA writable_schema=OFF", 0, 0, 0);
                            if (!String.IsNullOrEmpty(zErrMsg))
                            {
                                fprintf(stderr, "Error: %s\n", zErrMsg);
                                zErrMsg = null;//Sqlite3.sqlite3_free(zErrMsg);
                            }
                            else
                            {
                                fprintf(p.Out, "COMMIT;\n");
                            }
                        }
                        else

                            if (c == 'e' && azArg[0].StartsWith("echo", StringComparison.InvariantCultureIgnoreCase) && nArg > 1 && nArg < 3)
                            {
                                p.echoOn = booleanValue(azArg[1]);
                            }
                            else

                                if (c == 'e' && azArg[0].StartsWith("exit", StringComparison.InvariantCultureIgnoreCase) && nArg == 1)
                                {
                                    rc = 2;
                                }
                                else

                                    if (c == 'e' && azArg[0].StartsWith("explain", StringComparison.InvariantCultureIgnoreCase) && nArg < 3)
                                    {
                                        int val = nArg >= 2 ? booleanValue(azArg[1]) ? 1 : 0 : 1;
                                        if (val == 1)
                                        {
                                            if (!p.explainPrev.valid)
                                            {
                                                p.explainPrev.valid = true;
                                                p.explainPrev.mode = p.mode;
                                                p.explainPrev.showHeader = p.showHeader;
                                                p.explainPrev.colWidth = new int[p.colWidth.Length];
                                                Array.Copy(p.colWidth, p.explainPrev.colWidth, p.colWidth.Length);//memcpy( p.explainPrev.colWidth, p.colWidth, sizeof( p.colWidth ) );
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
                                            Array.Clear(p.colWidth, 0, p.colWidth.Length);//memset(p.colWidth, 0, ArraySize(p.colWidth));
                                            p.colWidth[0] = 4;                  /* addr */
                                            p.colWidth[1] = 13;                 /* opcode */
                                            p.colWidth[2] = 4;                  /* P1 */
                                            p.colWidth[3] = 4;                  /* P2 */
                                            p.colWidth[4] = 4;                  /* P3 */
                                            p.colWidth[5] = 13;                 /* P4 */
                                            p.colWidth[6] = 2;                  /* P5 */
                                            p.colWidth[7] = 13;                  /* Comment */
                                        }
                                        else if (p.explainPrev.valid)
                                        {
                                            p.explainPrev.valid = false;
                                            p.mode = p.explainPrev.mode;
                                            p.showHeader = p.explainPrev.showHeader;
                                            Array.Copy(p.colWidth, p.explainPrev.colWidth, p.colWidth.Length);//memcpy(p.colWidth, p.explainPrev.colWidth, sizeof(p.colWidth));
                                        }
                                    }
                                    else

                                        if (c == 'h' && (azArg[0].StartsWith("header", StringComparison.InvariantCultureIgnoreCase) ||
                                        azArg[0].StartsWith("headers", StringComparison.InvariantCultureIgnoreCase) && nArg > 1 && nArg < 3))
                                        {
                                            p.showHeader = booleanValue(azArg[1]);
                                        }
                                        else

                                            if (c == 'h' && azArg[0].StartsWith("help", StringComparison.InvariantCultureIgnoreCase))
                                            {
                                                fprintf(stderr, "%s", zHelp);
                                                if (HAS_TIMER)
                                                {
                                                    fprintf(stderr, "%s", zTimerHelp);
                                                }
                                            }
                                            else

                                                if (c == 'i' && azArg[0].StartsWith("import", StringComparison.InvariantCultureIgnoreCase) && nArg == 3)
                                                {
                                                    string zTable = azArg[2];       /* Insert data into this table */
                                                    string zFile = azArg[1];        /* The file from which to extract data */
                                                    sqlite3_stmt pStmt = null;      /* A statement */
                                                    int nCol;                       /* Number of columns in the table */
                                                    int nByte;                      /* Number of bytes in an SQL string */
                                                    int ii, j;                      /* Loop counters */
                                                    int nSep;                       /* Number of bytes in p.separator[] */
                                                    StringBuilder zSql;             /* An SQL statement */
                                                    //string zLine;                 /* A single line of input from the file */
                                                    string[] azCol;                 /* zLine[] broken up into columns */
                                                    string zCommit;                 /* How to commit changes */
                                                    TextReader In;                  /* The input file */
                                                    int lineno = 0;                 /* Line number of input file */

                                                    open_db(p);
                                                    nSep = strlen30(p.separator);
                                                    if (nSep == 0)
                                                    {
                                                        fprintf(stderr, "Error: non-null separator required for import\n");
                                                        return 1;
                                                    }
                                                    zSql = new StringBuilder(Sqlite3.sqlite3_mprintf("SELECT * FROM '%q'", zTable));
                                                    //if (zSql == null)
                                                    //{
                                                    //    fprintf(stderr, "Error: out of memory\n");
                                                    //    return 1;
                                                    //}
                                                    nByte = strlen30(zSql);
                                                    rc = Sqlite3.sqlite3_prepare(p.db, zSql, -1, ref pStmt, 0);
                                                    zSql = null;//Sqlite3.sqlite3_free(zSql);
                                                    if (rc != 0)
                                                    {
                                                        if (pStmt != null)
                                                            Sqlite3.sqlite3_finalize(pStmt);
                                                        fprintf(stderr, "Error: %s\n", Sqlite3.sqlite3_errmsg(db));
                                                        return 1;
                                                    }
                                                    nCol = Sqlite3.sqlite3_column_count(pStmt);
                                                    Sqlite3.sqlite3_finalize(pStmt);
                                                    pStmt = null;
                                                    if (nCol == 0)
                                                        return 0; /* no columns, no error */
                                                    zSql = new StringBuilder(nByte + 20 + nCol * 2);
                                                    //if (zSql == null)
                                                    //{
                                                    //    fprintf(stderr, "Error: out of memory\n");
                                                    //    return 1;
                                                    //}
                                                    Sqlite3.sqlite3_snprintf(nByte + 20, zSql, "INSERT INTO '%q' VALUES(?", zTable);
                                                    j = strlen30(zSql);
                                                    for (ii = 1; ii < nCol; ii++)
                                                    {
                                                        zSql[j++] = ',';
                                                        zSql[j++] = '?';
                                                    }
                                                    zSql[j++] = ')';
                                                    zSql[j] = '\0';
                                                    rc = Sqlite3.sqlite3_prepare(p.db, zSql, -1, ref pStmt, 0);
                                                    zSql = null;//free(zSql);
                                                    if (rc != 0)
                                                    {
                                                        fprintf(stderr, "Error: %s\n", Sqlite3.sqlite3_errmsg(db));
                                                        if (pStmt != null)
                                                            Sqlite3.sqlite3_finalize(pStmt);
                                                        return 1;
                                                    }
                                                    In = new StreamReader(zFile);// fopen(zFile, "rb");
                                                    if (In == null)
                                                    {
                                                        fprintf(stderr, "Error: cannot open \"%s\"\n", zFile);
                                                        Sqlite3.sqlite3_finalize(pStmt);
                                                        return 1;
                                                    }
                                                    azCol = new string[nCol + 1];//malloc( sizeof(azCol[0])*(nCol+1) );
                                                    //if( azCol== null ){
                                                    //fprintf(stderr, "Error: out of memory\n");
                                                    //fclose(In );
                                                    //Sqlite3.sqlite3_finalize(pStmt!=null);
                                                    //return 1;
                                                    //}
                                                    Sqlite3.sqlite3_exec(p.db, "BEGIN", 0, 0, 0);
                                                    zCommit = "COMMIT";
                                                    while ((zLine.Append(local_getline(null, In).ToString())).Length != 0)
                                                    {
                                                        string z;
                                                        //i = 0;
                                                        lineno++;
                                                        azCol = zLine.ToString().Split(p.separator.ToCharArray());
                                                        //azCol[0] = zLine;
                                                        //for (i = 0, z = zLine; *z && *z != '\n' && *z != '\r'; z++)
                                                        //{
                                                        //    if (*z == p.separator[0] && z.StartsWith(p.separator))
                                                        //    {
                                                        //        *z = 0;
                                                        //        i++;
                                                        //        if (i < nCol)
                                                        //        {
                                                        //            azCol[i] = z[nSep];
                                                        //            z += nSep - 1;
                                                        //        }
                                                        //    }
                                                        //} /* end for */
                                                        //*z = 0;
                                                        if (azCol.Length != nCol)
                                                        {
                                                            fprintf(stderr,
                                                            "Error: %s line %d: expected %d columns of data but found %d\n",
                                                            zFile, lineno, nCol, azCol.Length);
                                                            zCommit = "ROLLBACK";
                                                            zLine = null;//free(zLine);
                                                            rc = 1;
                                                            break; /* from while */
                                                        }
                                                        for (ii = 0; ii < nCol; ii++)
                                                        {
                                                            Sqlite3.sqlite3_bind_text(pStmt, ii + 1, azCol[ii], -1, Sqlite3.SQLITE_STATIC);
                                                        }
                                                        Sqlite3.sqlite3_step(pStmt);
                                                        rc = Sqlite3.sqlite3_reset(pStmt);
                                                        zLine = null;//free(zLine);
                                                        if (rc != Sqlite3.SQLITE_OK)
                                                        {
                                                            fprintf(stderr, "Error: %s\n", Sqlite3.sqlite3_errmsg(db));
                                                            zCommit = "ROLLBACK";
                                                            rc = 1;
                                                            break; /* from while */
                                                        }
                                                    } /* end while */
                                                    azCol = null;//free(azCol);
                                                    In.Close();//fclose(in);
                                                    Sqlite3.sqlite3_finalize(pStmt);
                                                    Sqlite3.sqlite3_exec(p.db, zCommit, 0, 0, 0);
                                                }
                                                else

                                                    if (c == 'i' && azArg[0].StartsWith("indices", StringComparison.InvariantCultureIgnoreCase) && nArg < 3)
                                                    {
                                                        callback_data data;
                                                        string zErrMsg = null;
                                                        open_db(p);
                                                        data = p.Copy();// memcpy(data, p, sizeof(data));
                                                        data.showHeader = false;
                                                        data.mode = MODE_List;
                                                        if (nArg == 1)
                                                        {
                                                            rc = Sqlite3.sqlite3_exec(p.db,
                                                            "SELECT name FROM sqlite_master " +
                                                            "WHERE type='index' AND name NOT LIKE 'Sqlite3.SQLITE_%' " +
                                                            "UNION ALL " +
                                                            "SELECT name FROM sqlite_temp_master " +
                                                            "WHERE type='index' " +
                                                            "ORDER BY 1",
                                                            callback, data, ref zErrMsg
                                                            );
                                                        }
                                                        else
                                                        {
                                                            zShellStatic = azArg[1];
                                                            rc = Sqlite3.sqlite3_exec(p.db,
                                                            "SELECT name FROM sqlite_master " +
                                                            "WHERE type='index' AND tbl_name LIKE shellstatic() " +
                                                            "UNION ALL " +
                                                            "SELECT name FROM sqlite_temp_master " +
                                                            "WHERE type='index' AND tbl_name LIKE shellstatic() " +
                                                            "ORDER BY 1",
                                                            callback, data, ref zErrMsg
                                                            );
                                                            zShellStatic = null;
                                                        }
                                                        if (!String.IsNullOrEmpty(zErrMsg))
                                                        {
                                                            fprintf(stderr, "Error: %s\n", zErrMsg);
                                                            zErrMsg = null;//Sqlite3.sqlite3_free(zErrMsg);
                                                            rc = 1;
                                                        }
                                                        else if (rc != Sqlite3.SQLITE_OK)
                                                        {
                                                            fprintf(stderr, "Error: querying Sqlite3.SQLITE_master and Sqlite3.SQLITE_temp_master\n");
                                                            rc = 1;
                                                        }
                                                    }
                                                    else

#if SQLITE_ENABLE_IOTRACE
if( c=='i' && "iotrace", n)== null ){
extern void (*sqlite3IoTrace)(const char*, ...);
if( iotrace && iotrace!=stdout ) iotrace.Close();//fclose(iotrace);
iotrace = null;
if( nArg<2 ){
sqlite3IoTrace = 0;
}else if( azArg[1].Equals("-") ){
sqlite3IoTrace = iotracePrintf;
iotrace = stdout;
}else{
iotrace = new StreamWriter(azArg[1].ToString());// fopen(azArg[1], "w");
if( iotrace== null ){
fprintf(stderr, "Error: cannot open \"%s\"\n", azArg[1]);
sqlite3IoTrace = 0;
rc = 1;
}else{
sqlite3IoTrace = iotracePrintf;
}
}
}else
#endif

#if !SQLITE_OMIT_LOAD_EXTENSION
                                                        if (c == 'l' && azArg[0].StartsWith("load", StringComparison.InvariantCultureIgnoreCase) && nArg >= 2)
                                                        {
                                                            string zFile, zProc;
                                                            string zErrMsg = null;
                                                            zFile = azArg[1];
                                                            zProc = nArg >= 3 ? azArg[2] : null;
                                                            open_db(p);
                                                            rc = Sqlite3.sqlite3_load_extension(p.db, zFile, zProc, ref zErrMsg);
                                                            if (rc != Sqlite3.SQLITE_OK)
                                                            {
                                                                fprintf(stderr, "Error: %s\n", zErrMsg);
                                                                zErrMsg = null;//Sqlite3.sqlite3_free( zErrMsg );
                                                                rc = 1;
                                                            }
                                                        }
                                                        else
#endif

                                                            if (c == 'l' && azArg[0].StartsWith("log", StringComparison.InvariantCultureIgnoreCase) && nArg >= 2)
                                                            {
                                                                string zFile = azArg[1];
                                                                if (p.pLog != null && p.pLog != stdout && p.pLog != stderr)
                                                                {
                                                                    p.pLog.Close();//fclose(p.pLog);
                                                                    p.pLog = null;
                                                                }
                                                                if (zFile.Equals("stdout", StringComparison.InvariantCultureIgnoreCase))
                                                                {
                                                                    p.pLog = stdout;
                                                                }
                                                                else if (zFile.Equals("stderr", StringComparison.InvariantCultureIgnoreCase))
                                                                {
                                                                    p.pLog = stderr;
                                                                }
                                                                else if (zFile.Equals("off", StringComparison.InvariantCultureIgnoreCase))
                                                                {
                                                                    p.pLog = null;
                                                                }
                                                                else
                                                                {
                                                                    p.pLog = new StreamWriter(zFile);// fopen(zFile, "w");
                                                                    if (p.pLog == null)
                                                                    {
                                                                        fprintf(stderr, "Error: cannot open \"%s\"\n", zFile);
                                                                    }
                                                                }
                                                            }
                                                            else

                                                                if (c == 'm' && azArg[0].StartsWith("mode", StringComparison.InvariantCultureIgnoreCase) && nArg == 2)
                                                                {
                                                                    int n2 = strlen30(azArg[1]);
                                                                    if ((n2 == 4 && azArg[1].StartsWith("line", StringComparison.InvariantCultureIgnoreCase))
                                                                    ||
                                                                    (n2 == 5 && azArg[1].StartsWith("lines", StringComparison.InvariantCultureIgnoreCase)))
                                                                    {
                                                                        p.mode = MODE_Line;
                                                                    }
                                                                    else if ((n2 == 6 && azArg[1].StartsWith("column", StringComparison.InvariantCultureIgnoreCase))
                                                                    ||
                                                                    (n2 == 7 && azArg[1].StartsWith("columns", StringComparison.InvariantCultureIgnoreCase)))
                                                                    {
                                                                        p.mode = MODE_Column;
                                                                    }
                                                                    else if (n2 == 4 && azArg[1].StartsWith("list", StringComparison.InvariantCultureIgnoreCase))
                                                                    {
                                                                        p.mode = MODE_List;
                                                                    }
                                                                    else if (n2 == 4 && azArg[1].StartsWith("html", StringComparison.InvariantCultureIgnoreCase))
                                                                    {
                                                                        p.mode = MODE_Html;
                                                                    }
                                                                    else if (n2 == 3 && azArg[1].StartsWith("tcl", StringComparison.InvariantCultureIgnoreCase))
                                                                    {
                                                                        p.mode = MODE_Tcl;
                                                                    }
                                                                    else if (n2 == 3 && azArg[1].StartsWith("csv", StringComparison.InvariantCultureIgnoreCase))
                                                                    {
                                                                        p.mode = MODE_Csv;
                                                                        snprintf(2, ref p.separator, ",");
                                                                    }
                                                                    else if (n2 == 4 && azArg[1].StartsWith("tabs", StringComparison.InvariantCultureIgnoreCase))
                                                                    {
                                                                        p.mode = MODE_List;
                                                                        snprintf(2, ref p.separator, "\t");
                                                                    }
                                                                    else if (n2 == 6 && azArg[1].StartsWith("insert", StringComparison.InvariantCultureIgnoreCase))
                                                                    {
                                                                        p.mode = MODE_Insert;
                                                                        set_table_name(p, "table");
                                                                    }
                                                                    else
                                                                    {
                                                                        fprintf(stderr, "Error: mode should be one of: " +
                                                                        "column csv html insert line list tabs tcl\n");
                                                                        rc = 1;
                                                                    }
                                                                }
                                                                else

                                                                    if (c == 'm' && azArg[0].StartsWith("mode", StringComparison.InvariantCultureIgnoreCase) && nArg == 3)
                                                                    {
                                                                        int n2 = strlen30(azArg[1]);
                                                                        if (n2 == 6 && azArg[1].StartsWith("insert", StringComparison.InvariantCultureIgnoreCase))
                                                                        {
                                                                            p.mode = MODE_Insert;
                                                                            set_table_name(p, azArg[2]);
                                                                        }
                                                                        else
                                                                        {
                                                                            fprintf(stderr, "Error: invalid arguments: " +
                                                                            " \"%s\". Enter \".help\" for help\n", azArg[2]);
                                                                            rc = 1;
                                                                        }
                                                                    }
                                                                    else

                                                                        if (c == 'n' && azArg[0].StartsWith("nullvalue", StringComparison.InvariantCultureIgnoreCase) && nArg == 2)
                                                                        {
                                                                            snprintf(9, ref p.nullvalue,
                                                                            "%.*s", (int)p.nullvalue.Length - 1, azArg[1]);
                                                                        }
                                                                        else

                                                                            if (c == 'o' && azArg[0].StartsWith("output", StringComparison.InvariantCultureIgnoreCase) && nArg == 2)
                                                                            {
                                                                                if (p.Out != stdout)
                                                                                {
                                                                                    p.Out.Close();//fclose(p.Out);
                                                                                }
                                                                                if (azArg[1].Equals("stdout", StringComparison.InvariantCultureIgnoreCase))
                                                                                {
                                                                                    p.Out = stdout;
                                                                                    Sqlite3.sqlite3_snprintf(p.outfile.Capacity, p.outfile, "stdout");
                                                                                }
                                                                                else
                                                                                {
                                                                                    p.Out = new StreamWriter(azArg[1].ToString());// fopen(azArg[1], "wb");
                                                                                    if (p.Out == null)
                                                                                    {
                                                                                        fprintf(stderr, "Error: cannot write to \"%s\"\n", azArg[1]);
                                                                                        p.Out = stdout;
                                                                                        rc = 1;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        Sqlite3.sqlite3_snprintf(p.outfile.Capacity, p.outfile, "%s", azArg[1]);
                                                                                    }
                                                                                }
                                                                            }
                                                                            else

                                                                                if (c == 'p' && azArg[0].StartsWith("prompt", StringComparison.InvariantCultureIgnoreCase) && (nArg == 2 || nArg == 3))
                                                                                {
                                                                                    if (nArg >= 2)
                                                                                    {
                                                                                        mainPrompt = azArg[1].ToString();//strncpy(mainPrompt, azArg[1], (int)ArraySize(mainPrompt) - 1);
                                                                                    }
                                                                                    if (nArg >= 3)
                                                                                    {
                                                                                        continuePrompt = azArg[1].ToString();//strncpy(continuePrompt, azArg[2], (int)ArraySize(continuePrompt) - 1);
                                                                                    }
                                                                                }
                                                                                else

                                                                                    if (c == 'q' && azArg[0].StartsWith("quit", StringComparison.InvariantCultureIgnoreCase) && nArg == 1)
                                                                                    {
                                                                                        rc = 2;
                                                                                    }
                                                                                    else

                                                                                        if (c == 'r' && n >= 3 && azArg[0].StartsWith("read", StringComparison.InvariantCultureIgnoreCase) && nArg == 2)
                                                                                        {
                                                                                            StreamReader alt = new StreamReader(azArg[1].ToString());// fopen(azArg[1], "rb");
                                                                                            if (alt == null)
                                                                                            {
                                                                                                fprintf(stderr, "Error: cannot open \"%s\"\n", azArg[1]);
                                                                                                rc = 1;
                                                                                            }
                                                                                            else
                                                                                            {
                                                                                                rc = process_input(p, alt);
                                                                                                alt.Close();//fclose(alt);
                                                                                            }
                                                                                        }
                                                                                        else

                                                                                            if (c == 'r' && n >= 3 && azArg[0].StartsWith("restore", StringComparison.InvariantCultureIgnoreCase) && nArg > 1 && nArg < 4)
                                                                                            {
                                                                                                string zSrcFile;
                                                                                                string zDb;
                                                                                                sqlite3 pSrc;
                                                                                                Sqlite3.sqlite3_backup pBackup;
                                                                                                int nTimeout = 0;

                                                                                                if (nArg == 2)
                                                                                                {
                                                                                                    zSrcFile = azArg[1];
                                                                                                    zDb = "main";
                                                                                                }
                                                                                                else
                                                                                                {
                                                                                                    zSrcFile = azArg[2];
                                                                                                    zDb = azArg[1];
                                                                                                }
                                                                                                rc = Sqlite3.sqlite3_open(zSrcFile, out pSrc);
                                                                                                if (rc != Sqlite3.SQLITE_OK)
                                                                                                {
                                                                                                    fprintf(stderr, "Error: cannot open \"%s\"\n", zSrcFile);
                                                                                                    Sqlite3.sqlite3_close(pSrc);
                                                                                                    return 1;
                                                                                                }
                                                                                                open_db(p);
                                                                                                pBackup = Sqlite3.sqlite3_backup_init(p.db, zDb, pSrc, "main");
                                                                                                if (pBackup == null)
                                                                                                {
                                                                                                    fprintf(stderr, "Error: %s\n", Sqlite3.sqlite3_errmsg(p.db));
                                                                                                    Sqlite3.sqlite3_close(pSrc);
                                                                                                    return 1;
                                                                                                }
                                                                                                while ((rc = Sqlite3.sqlite3_backup_step(pBackup, 100)) == Sqlite3.SQLITE_OK
                                                                                                || rc == Sqlite3.SQLITE_BUSY)
                                                                                                {
                                                                                                    if (rc == Sqlite3.SQLITE_BUSY)
                                                                                                    {
                                                                                                        if (nTimeout++ >= 3)
                                                                                                            break;
                                                                                                        Sqlite3.sqlite3_sleep(100);
                                                                                                    }
                                                                                                }
                                                                                                Sqlite3.sqlite3_backup_finish(pBackup);
                                                                                                if (rc == Sqlite3.SQLITE_DONE)
                                                                                                {
                                                                                                    rc = 0;
                                                                                                }
                                                                                                else if (rc == Sqlite3.SQLITE_BUSY || rc == Sqlite3.SQLITE_LOCKED)
                                                                                                {
                                                                                                    fprintf(stderr, "Error: source database is busy\n");
                                                                                                    rc = 1;
                                                                                                }
                                                                                                else
                                                                                                {
                                                                                                    fprintf(stderr, "Error: %s\n", Sqlite3.sqlite3_errmsg(p.db));
                                                                                                    rc = 1;
                                                                                                }
                                                                                                Sqlite3.sqlite3_close(pSrc);
                                                                                            }
                                                                                            else

                                                                                                if (c == 's' && azArg[0].StartsWith("schema", StringComparison.InvariantCultureIgnoreCase) && nArg < 3)
                                                                                                {
                                                                                                    callback_data data;
                                                                                                    string zErrMsg = null;
                                                                                                    open_db(p);
                                                                                                    data = p.Copy();// memcpy(data, p, sizeof(data));
                                                                                                    data.showHeader = false;
                                                                                                    data.mode = MODE_Semi;
                                                                                                    if (nArg > 1)
                                                                                                    {
                                                                                                        //int i;
                                                                                                        //for (i = 0; azArg[1][i]; i++)
                                                                                                        //    azArg[1][i] = (char)tolower(azArg[1][i]);
                                                                                                        azArg[1] = azArg[1].ToLower();
                                                                                                        if (azArg[1].Equals("sqlite_master", StringComparison.InvariantCultureIgnoreCase))
                                                                                                        {
                                                                                                            string[] new_argv = new string[2];
                                                                                                            string[] new_colv = new string[2];
                                                                                                            new_argv[0] = "CREATE TABLE Sqlite3.SQLITE_master (\n" +
                                                                                                            "  type text,\n" +
                                                                                                            "  name text,\n" +
                                                                                                            "  tbl_name text,\n" +
                                                                                                            "  rootpage integer,\n" +
                                                                                                            "  sql text\n" +
                                                                                                            ")";
                                                                                                            new_argv[1] = null;
                                                                                                            new_colv[0] = "sql";
                                                                                                            new_colv[1] = null;
                                                                                                            callback(data, 1, new_argv, new_colv);
                                                                                                            rc = Sqlite3.SQLITE_OK;
                                                                                                        }
                                                                                                        else if (azArg[1].Equals("sqlite_temp_master", StringComparison.InvariantCultureIgnoreCase))
                                                                                                        {
                                                                                                            string[] new_argv = new string[2];
                                                                                                            string[] new_colv = new string[2];
                                                                                                            new_argv[0] = "CREATE TEMP TABLE sqlite_temp_master(\n" +
                                                                                                            "  type text,\n" +
                                                                                                            "  name text,\n" +
                                                                                                            "  tbl_name text,\n" +
                                                                                                            "  rootpage integer,\n" +
                                                                                                            "  sql text\n" +
                                                                                                            ")";
                                                                                                            new_argv[1] = null;
                                                                                                            new_colv[0] = "sql";
                                                                                                            new_colv[1] = null;
                                                                                                            callback(data, 1, new_argv, new_colv);
                                                                                                            rc = Sqlite3.SQLITE_OK;
                                                                                                        }
                                                                                                        else
                                                                                                        {
                                                                                                            zShellStatic = azArg[1];
                                                                                                            rc = Sqlite3.sqlite3_exec(p.db,
                                                                                                            "SELECT sql FROM " +
                                                                                                            "  (SELECT sql sql, type type, tbl_name tbl_name, name name" +
                                                                                                            "     FROM sqlite_master UNION ALL" +
                                                                                                            "   SELECT sql, type, tbl_name, name FROM sqlite_temp_master) " +
                                                                                                            "WHERE tbl_name LIKE shellstatic() AND type!='meta' AND sql NOTnull " +
                                                                                                            "ORDER BY substr(type,2,1), name",
                                                                                                            callback, data, ref zErrMsg);
                                                                                                            zShellStatic = null;
                                                                                                        }
                                                                                                    }
                                                                                                    else
                                                                                                    {
                                                                                                        rc = Sqlite3.sqlite3_exec(p.db,
                                                                                                        "SELECT sql FROM " +
                                                                                                        "  (SELECT sql sql, type type, tbl_name tbl_name, name name" +
                                                                                                        "     FROM sqlite_master UNION ALL" +
                                                                                                        "   SELECT sql, type, tbl_name, name FROM sqlite_temp_master) " +
                                                                                                        "WHERE type!='meta' AND sql NOTnull AND name NOT LIKE 'Sqlite3.SQLITE_%'" +
                                                                                                        "ORDER BY substr(type,2,1), name",
                                                                                                        callback, data, ref zErrMsg
                                                                                                        );
                                                                                                    }
                                                                                                    if (!String.IsNullOrEmpty(zErrMsg))
                                                                                                    {
                                                                                                        fprintf(stderr, "Error: %s\n", zErrMsg);
                                                                                                        zErrMsg = null;//Sqlite3.sqlite3_free(zErrMsg);
                                                                                                        rc = 1;
                                                                                                    }
                                                                                                    else if (rc != Sqlite3.SQLITE_OK)
                                                                                                    {
                                                                                                        fprintf(stderr, "Error: querying schema information\n");
                                                                                                        rc = 1;
                                                                                                    }
                                                                                                    else
                                                                                                    {
                                                                                                        rc = 0;
                                                                                                    }
                                                                                                }
                                                                                                else

                                                                                                    if (c == 's' && azArg[0].StartsWith("separator", StringComparison.InvariantCultureIgnoreCase) && nArg == 2)
                                                                                                    {
                                                                                                        snprintf(2, ref p.separator, "%.*s", 2 - 1, azArg[1]);
                                                                                                    }
                                                                                                    else

                                                                                                        if (c == 's' && azArg[0].StartsWith("show", StringComparison.InvariantCultureIgnoreCase) && nArg == 1)
                                                                                                        {
                                                                                                            int ii;
                                                                                                            fprintf(p.Out, "%9.9s: %s\n", "echo", p.echoOn ? "on" : "off");
                                                                                                            fprintf(p.Out, "%9.9s: %s\n", "explain", p.explainPrev.valid ? "on" : "off");
                                                                                                            fprintf(p.Out, "%9.9s: %s\n", "headers", p.showHeader ? "on" : "off");
                                                                                                            fprintf(p.Out, "%9.9s: %s\n", "mode", modeDescr[p.mode]);
                                                                                                            fprintf(p.Out, "%9.9s: ", "nullvalue");
                                                                                                            output_c_string(p.Out, p.nullvalue);
                                                                                                            fprintf(p.Out, "\n");
                                                                                                            fprintf(p.Out, "%9.9s: %s\n", "output",
                                                                                                            strlen30(p.outfile) != 0 ? p.outfile.ToString() : "stdout");
                                                                                                            fprintf(p.Out, "%9.9s: ", "separator");
                                                                                                            output_c_string(p.Out, p.separator);
                                                                                                            fprintf(p.Out, "\n");
                                                                                                            fprintf(p.Out, "%9.9s: %s\n", "stats", p.statsOn ? "on" : "off");
                                                                                                            fprintf(p.Out, "%9.9s: ", "width");
                                                                                                            for (ii = 0; ii < (int)ArraySize(p.colWidth) && p.colWidth[ii] != 0; ii++)
                                                                                                            {
                                                                                                                fprintf(p.Out, "%d ", p.colWidth[ii]);
                                                                                                            }
                                                                                                            fprintf(p.Out, "\n");
                                                                                                        }
                                                                                                        else

                                                                                                            if (c == 's' && azArg[0].StartsWith("stats", StringComparison.InvariantCultureIgnoreCase) && nArg > 1 && nArg < 3)
                                                                                                            {
                                                                                                                p.statsOn = booleanValue(azArg[1]);
                                                                                                            }
                                                                                                            else

                                                                                                                if (c == 't' && n > 1 && azArg[0].StartsWith("tables", StringComparison.InvariantCultureIgnoreCase) && nArg < 3)
                                                                                                                {
                                                                                                                    string[] azResult = null;
                                                                                                                    int nRow = 0;
                                                                                                                    string zErrMsg = null;
                                                                                                                    open_db(p);
                                                                                                                    if (nArg == 1)
                                                                                                                    {
                                                                                                                        rc = Sqlite3.sqlite3_get_table(p.db,
                                                                                                                        "SELECT name FROM sqlite_master " +
                                                                                                                        "WHERE type IN ('table','view') AND name NOT LIKE 'Sqlite3.SQLITE_%' " +
                                                                                                                        "UNION ALL " +
                                                                                                                        "SELECT name FROM sqlite_temp_master " +
                                                                                                                        "WHERE type IN ('table','view') " +
                                                                                                                        "ORDER BY 1",
                                                                                                                        ref azResult, ref nRow, null, ref zErrMsg
                                                                                                                        );
                                                                                                                    }
                                                                                                                    else
                                                                                                                    {
                                                                                                                        zShellStatic = azArg[1];
                                                                                                                        rc = Sqlite3.sqlite3_get_table(p.db,
                                                                                                                        "SELECT name FROM sqlite_master " +
                                                                                                                        "WHERE type IN ('table','view') AND name LIKE shellstatic() " +
                                                                                                                        "UNION ALL " +
                                                                                                                        "SELECT name FROM sqlite_temp_master " +
                                                                                                                        "WHERE type IN ('table','view') AND name LIKE shellstatic() " +
                                                                                                                        "ORDER BY 1",
                                                                                                                        ref azResult, ref nRow, null, ref zErrMsg
                                                                                                                        );
                                                                                                                        zShellStatic = null;
                                                                                                                    }
                                                                                                                    if (!String.IsNullOrEmpty(zErrMsg))
                                                                                                                    {
                                                                                                                        fprintf(stderr, "Error: %s\n", zErrMsg);
                                                                                                                        zErrMsg = null;//Sqlite3.sqlite3_free(zErrMsg);
                                                                                                                        rc = 1;
                                                                                                                    }
                                                                                                                    else if (rc != Sqlite3.SQLITE_OK)
                                                                                                                    {
                                                                                                                        fprintf(stderr, "Error: querying Sqlite3.SQLITE_master and Sqlite3.SQLITE_temp_master\n");
                                                                                                                        rc = 1;
                                                                                                                    }
                                                                                                                    else
                                                                                                                    {
                                                                                                                        int len, maxlen = 0;
                                                                                                                        int ii, j;
                                                                                                                        int nPrintCol, nPrintRow;
                                                                                                                        for (ii = 1; ii <= nRow; ii++)
                                                                                                                        {
                                                                                                                            if (azResult[ii] == null)
                                                                                                                                continue;
                                                                                                                            len = strlen30(azResult[ii]);
                                                                                                                            if (len > maxlen)
                                                                                                                                maxlen = len;
                                                                                                                        }
                                                                                                                        nPrintCol = 80 / (maxlen + 2);
                                                                                                                        if (nPrintCol < 1)
                                                                                                                            nPrintCol = 1;
                                                                                                                        nPrintRow = (nRow + nPrintCol - 1) / nPrintCol;
                                                                                                                        for (ii = 0; ii < nPrintRow; ii++)
                                                                                                                        {
                                                                                                                            for (j = ii + 1; j <= nRow; j += nPrintRow)
                                                                                                                            {
                                                                                                                                string zSp = j <= nPrintRow ? "" : "  ";
                                                                                                                                printf("%s%-*s", zSp, maxlen, !String.IsNullOrEmpty(azResult[j]) ? azResult[j] : "");
                                                                                                                            }
                                                                                                                            printf("\n");
                                                                                                                        }
                                                                                                                    }
                                                                                                                    Sqlite3.sqlite3_free_table(ref azResult);
                                                                                                                }
                                                                                                                else

                                                                                                                    if (c == 't' && n >= 8 && azArg[0].StartsWith("testctrl", StringComparison.InvariantCultureIgnoreCase) && nArg >= 2)
                                                                                                                    {
                                                                                                                        //static const struct {
                                                                                                                        //   string zCtrlName;   /* Name of a test-control option */
                                                                                                                        //   int ctrlCode;            /* Integer code for that option */
                                                                                                                        //} 
                                                                                                                        _aCtrl[] aCtrl = new _aCtrl[] {
new _aCtrl( "prng_save",             Sqlite3.SQLITE_TESTCTRL_PRNG_SAVE              ),
new _aCtrl( "prng_restore",          Sqlite3.SQLITE_TESTCTRL_PRNG_RESTORE           ),
new _aCtrl( "prng_reset",            Sqlite3.SQLITE_TESTCTRL_PRNG_RESET             ),
new _aCtrl( "bitvec_test",           Sqlite3.SQLITE_TESTCTRL_BITVEC_TEST            ),
new _aCtrl( "fault_install",         Sqlite3.SQLITE_TESTCTRL_FAULT_INSTALL          ),
new _aCtrl( "benign_malloc_hooks",   Sqlite3.SQLITE_TESTCTRL_BENIGN_MALLOC_HOOKS    ),
new _aCtrl( "pending_byte",          Sqlite3.SQLITE_TESTCTRL_PENDING_BYTE           ),
new _aCtrl( "Debug.Assert",          Sqlite3.SQLITE_TESTCTRL_ASSERT                 ),
new _aCtrl( "always",                Sqlite3.SQLITE_TESTCTRL_ALWAYS                 ),
new _aCtrl( "reserve",               Sqlite3.SQLITE_TESTCTRL_RESERVE                ),
new _aCtrl( "optimizations",         Sqlite3.SQLITE_TESTCTRL_OPTIMIZATIONS          ),
new _aCtrl( "iskeyword",             Sqlite3.SQLITE_TESTCTRL_ISKEYWORD              ),
new _aCtrl( "pghdrsz",               Sqlite3.SQLITE_TESTCTRL_PGHDRSZ                ),
new _aCtrl( "scratchmalloc",         Sqlite3.SQLITE_TESTCTRL_SCRATCHMALLOC          ),
};
                                                                                                                        int testctrl = -1;
                                                                                                                        //int rc = 0;
                                                                                                                        int ii;//, n;
                                                                                                                        open_db(p);

                                                                                                                        /* convert testctrl text option to value. allow any unique prefix
                                                                                                                        ** of the option name, or a numerical value. */
                                                                                                                        //n = strlen30(azArg[1]);
                                                                                                                        for (ii = 0; ii < aCtrl.Length; ii++)//(int)(sizeof(aCtrl)/sizeof(aCtrl[0])); i++)
                                                                                                                        {
                                                                                                                            if (aCtrl[ii].zCtrlName.StartsWith(azArg[1], StringComparison.InvariantCultureIgnoreCase))
                                                                                                                            {
                                                                                                                                if (testctrl < 0)
                                                                                                                                {
                                                                                                                                    testctrl = aCtrl[ii].ctrlCode;
                                                                                                                                }
                                                                                                                                else
                                                                                                                                {
                                                                                                                                    fprintf(stderr, "ambiguous option name: \"%s\"\n", azArg[ii]);
                                                                                                                                    testctrl = -1;
                                                                                                                                    break;
                                                                                                                                }
                                                                                                                            }
                                                                                                                        }
                                                                                                                        if (testctrl < 0)
                                                                                                                            testctrl = Convert.ToInt32(azArg[1]);//atoi
                                                                                                                        if ((testctrl < Sqlite3.SQLITE_TESTCTRL_FIRST) || (testctrl > Sqlite3.SQLITE_TESTCTRL_LAST))
                                                                                                                        {
                                                                                                                            fprintf(stderr, "Error: invalid testctrl option: %s\n", azArg[1]);
                                                                                                                        }
                                                                                                                        else
                                                                                                                        {
                                                                                                                            switch (testctrl)
                                                                                                                            {

                                                                                                                                /* Sqlite3.sqlite3_test_control(int, db, Int) */
                                                                                                                                case Sqlite3.SQLITE_TESTCTRL_OPTIMIZATIONS:
                                                                                                                                case Sqlite3.SQLITE_TESTCTRL_RESERVE:
                                                                                                                                    if (nArg == 3)
                                                                                                                                    {
                                                                                                                                        int opt = (int)System.Int64.Parse(azArg[2]);
                                                                                                                                        rc = Sqlite3.sqlite3_test_control(testctrl, p.db, opt);
                                                                                                                                        printf("%d (0x%08x)\n", rc, rc);
                                                                                                                                    }
                                                                                                                                    else
                                                                                                                                    {
                                                                                                                                        fprintf(stderr, "Error: testctrl %s takes a single int option\n",
                                                                                                                                        azArg[1]);
                                                                                                                                    }
                                                                                                                                    break;

                                                                                                                                /* Sqlite3.sqlite3_test_control(int) */
                                                                                                                                case Sqlite3.SQLITE_TESTCTRL_PRNG_SAVE:
                                                                                                                                case Sqlite3.SQLITE_TESTCTRL_PRNG_RESTORE:
                                                                                                                                case Sqlite3.SQLITE_TESTCTRL_PRNG_RESET:
                                                                                                                                case Sqlite3.SQLITE_TESTCTRL_PGHDRSZ:
                                                                                                                                    if (nArg == 2)
                                                                                                                                    {
                                                                                                                                        rc = Sqlite3.sqlite3_test_control(testctrl);
                                                                                                                                        printf("%d (0x%08x)\n", rc, rc);
                                                                                                                                    }
                                                                                                                                    else
                                                                                                                                    {
                                                                                                                                        fprintf(stderr, "Error: testctrl %s takes no options\n", azArg[1]);
                                                                                                                                    }
                                                                                                                                    break;

                                                                                                                                /* Sqlite3.sqlite3_test_control(int, uint) */
                                                                                                                                case Sqlite3.SQLITE_TESTCTRL_PENDING_BYTE:
                                                                                                                                    if (nArg == 3)
                                                                                                                                    {
                                                                                                                                        u32 opt = (u32)Convert.ToInt32(azArg[2]);//atoi
                                                                                                                                        rc = Sqlite3.sqlite3_test_control(testctrl, opt);
                                                                                                                                        printf("%d (0x%08x)\n", rc, rc);
                                                                                                                                    }
                                                                                                                                    else
                                                                                                                                    {
                                                                                                                                        fprintf(stderr, "Error: testctrl %s takes a single unsigned" +
                                                                                                                                            " int option\n", azArg[1]);
                                                                                                                                    }
                                                                                                                                    break;

                                                                                                                                /* Sqlite3.sqlite3_test_control(int, Int) */
                                                                                                                                case Sqlite3.SQLITE_TESTCTRL_ASSERT:
                                                                                                                                case Sqlite3.SQLITE_TESTCTRL_ALWAYS:
                                                                                                                                    if (nArg == 3)
                                                                                                                                    {
                                                                                                                                        int opt = Convert.ToInt32(azArg[2]);//atoi
                                                                                                                                        rc = Sqlite3.sqlite3_test_control(testctrl, opt);
                                                                                                                                        printf("%d (0x%08x)\n", rc, rc);
                                                                                                                                    }
                                                                                                                                    else
                                                                                                                                    {
                                                                                                                                        fprintf(stderr, "Error: testctrl %s takes a single int option\n",
                                                                                                                                            azArg[1]);
                                                                                                                                    }
                                                                                                                                    break;

                                                                                                                                /* Sqlite3.sqlite3_test_control(int, string ) */
#if SQLITE_N_KEYWORD
case Sqlite3.SQLITE_TESTCTRL_ISKEYWORD:           
if( nArg==3 ){
string opt = azArg[2];        
rc = Sqlite3.sqlite3_test_control(testctrl, opt);
printf("%d (0x%08x)\n", rc, rc);
} else {
fprintf(stderr,"Error: testctrl %s takes a single string  option\n",
azArg[1]);
}
break;
#endif

                                                                                                                                case Sqlite3.SQLITE_TESTCTRL_BITVEC_TEST:
                                                                                                                                case Sqlite3.SQLITE_TESTCTRL_FAULT_INSTALL:
                                                                                                                                case Sqlite3.SQLITE_TESTCTRL_BENIGN_MALLOC_HOOKS:
                                                                                                                                case Sqlite3.SQLITE_TESTCTRL_SCRATCHMALLOC:
                                                                                                                                default:
                                                                                                                                    fprintf(stderr, "Error: CLI support for testctrl %s not implemented\n",
                                                                                                                                    azArg[1]);
                                                                                                                                    break;
                                                                                                                            }
                                                                                                                        }
                                                                                                                    }
                                                                                                                    else

                                                                                                                        if (c == 't' && n > 4 && azArg[0].StartsWith("timeout", StringComparison.InvariantCultureIgnoreCase) && nArg == 2)
                                                                                                                        {
                                                                                                                            open_db(p);
                                                                                                                            Sqlite3.sqlite3_busy_timeout(p.db, Convert.ToInt32(azArg[1]));//atoi
                                                                                                                        }
                                                                                                                        else

                                                                                                                            if (HAS_TIMER && c == 't' && n >= 5 && azArg[0].StartsWith("timer", StringComparison.InvariantCultureIgnoreCase)
                                                                                                                            && nArg == 2
                                                                                                                            )
                                                                                                                            {
                                                                                                                                enableTimer = booleanValue(azArg[1]);
                                                                                                                            }
                                                                                                                            else

                                                                                                                                if (c == 'v' && azArg[0].StartsWith("version", StringComparison.InvariantCultureIgnoreCase))
                                                                                                                                {
                                                                                                                                    printf("SQLite %s %s\n",
                                                                                                                                    Sqlite3.sqlite3_libversion(), Sqlite3.sqlite3_sourceid());
                                                                                                                                }
                                                                                                                                else

                                                                                                                                    if (c == 'w' && azArg[0].StartsWith("width", StringComparison.InvariantCultureIgnoreCase) && nArg > 1)
                                                                                                                                    {
                                                                                                                                        int j;
                                                                                                                                        Debug.Assert(nArg <= ArraySize(azArg));
                                                                                                                                        for (j = 1; j < nArg && j < ArraySize(p.colWidth); j++)
                                                                                                                                        {
                                                                                                                                            p.colWidth[j - 1] = Convert.ToInt32(azArg[j]);//atoi
                                                                                                                                        }
                                                                                                                                    }
                                                                                                                                    else
                                                                                                                                    {
                                                                                                                                        fprintf(stderr, "Error: unknown command or invalid arguments: " +
                                                                                                                                        " \"%s\". Enter \".help\" for help\n", azArg[0]);
                                                                                                                                        rc = 1;
                                                                                                                                    }

            return rc;
        }

        /*
        ** Return TRUE if a semicolon occurs anywhere in the first N characters
        ** of string z[].
        */
        static bool _contains_semicolon(string z, int N)
        {
            //int i;
            //for (i = 0; i < N; i++) { if (z[i] == ';') return 1; }
            //return 0;
            return z.Contains(";");
        }

        /*
        ** Test to see if a line consists entirely of whitespace.
        */
        static bool _all_whitespace(StringBuilder z)
        {
            return String.IsNullOrEmpty(z.ToString().Trim());
        }
        static bool _all_whitespace(string z)
        {
            return String.IsNullOrEmpty(z.Trim());
        }

        /*
        ** Return TRUE if the line typed in is an SQL command terminator other
        ** than a semi-colon.  The SQL Server style "go" command is understood
        ** as is the Oracle "/".
        */
        static bool _is_command_terminator(StringBuilder zLine)
        {
            return _is_command_terminator(zLine.ToString());
        }
        static bool _is_command_terminator(string zLine)
        {
            zLine = zLine.Trim();// while ( isspace( zLine ) ) { zLine++; };
            if (zLine.Length == 0)
                return false;
            if (zLine[0] == '/')//&& _all_whitespace(zLine[1]) )
            {
                return true;  /* Oracle */
            }
            if (Char.ToLower(zLine[0]) == 'g' && Char.ToLower(zLine[1]) == 'o')
            //&& _all_whitespace(&zLine[2]) )
            {
                return true;  /* SQL Server */
            }
            return false;
        }
        /*
        ** Return true if zSql is a complete SQL statement.  Return false if it
        ** ends in the middle of a string literal or C-style comment.
        */
        static bool _is_complete(string zSql, int nSql)
        {
            int rc;
            if (zSql == null)
                return true;
            //zSql[nSql] = ';';
            //zSql[nSql + 1] = 0;
            rc = Sqlite3.sqlite3_complete(zSql + ";\0");
            //zSql[nSql] = 0;
            return rc != 0;
        }

        /*
        ** Read input from In and process it.  If In== null then input
        ** is interactive - the user is typing it it.  Otherwise, Input
        ** is coming from a file or device.  A prompt is issued and history
        ** is saved only if input is interactive.  An interrupt signal will
        ** cause this routine to exit immediately, unless input is interactive.
        **
        ** Return the number of errors.
        */
        static int process_input(callback_data p, TextReader In)
        {
            StringBuilder zLine = new StringBuilder(1024);
            string zSql = null;
            int nSql = 0;
            int nSqlPrior = 0;
            string zErrMsg = null;
            int rc;
            int errCnt = 0;
            int lineno = 0;
            int startline = 0;

            while (errCnt == null || !bail_on_error || (In == null && stdin_is_interactive))
            {
                fflush(p.Out);
                //free(zLine);
                zLine.Length = 0;
                zLine.Append(one_input_line(zSql, In));
                if (In != null && ((System.IO.StreamReader)(In)).EndOfStream)
                {
                    break;  /* We have reached EOF */
                }
                //if (zLine == null)
                //{
                //    break;  /* We have reached EOF */
                //}
                if (seenInterrupt)
                {
                    if (In != null)
                        break;
                    seenInterrupt = false;
                }
                lineno++;
                if (String.IsNullOrEmpty(zSql) && _all_whitespace(zLine))
                    continue;
                if (zLine.Length > 0 && zLine[0] == '.' && nSql == 0)
                {
                    if (p.echoOn)
                        printf("%s\n", zLine);
                    rc = do_meta_command(zLine, p);
                    if (rc == 2)
                    { /* exit requested */
                        break;
                    }
                    else if (rc != 0)
                    {
                        errCnt++;
                    }
                    continue;
                }
                if (_is_command_terminator(zLine) && _is_complete(zSql, nSql))
                {
                    zLine.Append(";");// memcpy(zLine, ";", 2);
                }
                nSqlPrior = nSql;
                if (zSql == null)
                {
                    int i;
                    for (i = 0; i < zLine.Length && isspace(zLine[i]); i++) { }
                    if (i < zLine.Length)
                    {
                        nSql = strlen30(zLine);
                        //zSql = malloc(nSql + 3);
                        //if (zSql == null)
                        //{
                        //    fprintf(stderr, "Error: out of memory\n");
                        //    exit(1);
                        //}
                        zSql += zLine.ToString(0, nSql);//memcpy(zSql, zLine, nSql + 1);
                        startline = lineno;
                    }
                }
                else
                {
                    int len = strlen30(zLine);
                    //zSql = realloc(zSql, nSql + len + 4);
                    //if (zSql == null)
                    //{
                    //    fprintf(stderr, "Error: out of memory\n");
                    //    exit(1);
                    //}
                    zSql += '\n';
                    zSql += zLine;//memcpy(zSql[nSql], zLine, len + 1);
                    nSql = zLine.Length;
                }
                if (!String.IsNullOrEmpty(zSql) && _contains_semicolon(zSql.Substring(nSqlPrior), nSql - nSqlPrior)
                && Sqlite3.sqlite3_complete(zSql) != 0)
                {
                    p.cnt = 0;
                    open_db(p);
                    beginTimer();
                    rc = shell_exec(p.db, zSql, shell_callback, p, ref zErrMsg);
                    endTimer();
                    if (rc != 0 || zErrMsg != null)
                    {
                        string zPrefix = null;
                        if (In != null || !stdin_is_interactive)
                        {
                            snprintf(100, ref zPrefix,
                            "Error: near line %d:", startline);
                        }
                        else
                        {
                            snprintf(100, ref zPrefix, "Error:");
                        }
                        if (zErrMsg != null)
                        {
                            fprintf(stderr, "%s %s\n", zPrefix, zErrMsg);
                            //Sqlite3.sqlite3_free(zErrMsg);
                            zErrMsg = null;
                        }
                        else
                        {
                            fprintf(stderr, "%s %s\n", zPrefix, Sqlite3.sqlite3_errmsg(p.db));
                        }
                        errCnt++;
                    }
                    //free(zSql);
                    zSql = null;
                    nSql = 0;
                }
            }
            if (zSql != null)
            {
                if (!_all_whitespace(zSql))
                {
                    fprintf(stderr, "Error: incomplete SQL: %s\n", zSql);
                }
                zSql = null;//free(zSql);
            }
            zLine = null;//free(zLine);
            return errCnt;
        }

        /*
        ** Return a pathname which is the user's home directory.  A
        ** 0 return indicates an error of some kind.  Space to hold the
        ** resulting string is obtained from malloc().  The calling
        ** function should free the result.
        */
        static string find_home_dir()
        {
            string home_dir = null;

            //#if !defined(_WIN32) && !defined(WIN32) && !defined(__OS2__) && !defined(_WIN32_WCE) && !defined(__RTP__) && !defined(_WRS_KERNEL)
            //  struct passwd pwent;
            //  uid_t uid = getuid();
            //  if( (pwent=getpwuid(uid)) != null) {
            //    home_dir = pwent.pw_dir;
            //  }
            //#endif

#if (_WIN32_WCE)
/* Windows CE (arm-wince-mingw32ce-gcc) does not provide getenv()
*/
home_dir = strdup("/");
#else

#if (_WIN32) || (WIN32) || (__OS2__)
            //if (home_dir)
            {
                home_dir = getenv("USERPROFILE");
            }
#endif

            if (!String.IsNullOrEmpty(home_dir))
            {
                home_dir = getenv("HOME");
            }

#if (_WIN32) || (WIN32) || (__OS2__)
            if (String.IsNullOrEmpty(home_dir))
            {
                string zDrive, zPath;
                int n;
                zDrive = getenv("HOMEDRIVE");
                zPath = getenv("HOMEPATH");
                if (!String.IsNullOrEmpty(zDrive) && !String.IsNullOrEmpty(zPath))
                {
                    n = strlen30(zDrive) + strlen30(zPath) + 1;
                    //home_dir = malloc(n);
                    //if (home_dir == null)
                    //    return 0;
                    snprintf(n, ref home_dir, "%s%s", zDrive, zPath);
                    return home_dir;
                }
                home_dir = "c:\\";
            }
#endif

#endif //* !_WIN32_WCE */

            //if (home_dir)
            //{
            //    int n = strlen30(home_dir) + 1;
            //    string z = malloc(n);
            //    if (z)
            //        memcpy(z, home_dir, n);
            //    home_dir = z;
            //}

            return home_dir;
        }

        /*
        ** Read input from the file given by sqliterc_override.  Or if that
        ** parameter is null, take input from ~/.sqliterc
        **
        ** Returns the number of errors.
        */
        static int process_sqliterc(
        callback_data p,        /* Configuration data */
        string sqliterc_override   /* Name of config file. null to use default */
        )
        {
            string home_dir = null;
            string sqliterc = sqliterc_override;
            StringBuilder zBuf = null;
            StreamReader In = null;
            int nBuf;
            int rc = 0;

            if (sqliterc == null)
            {
                home_dir = find_home_dir();
                if (home_dir == null)
                {
#if !(__RTP__) && !(_WRS_KERNEL)
                    fprintf(stderr, "%s: Error: cannot locate your home directory\n", Argv0);
#endif
                    return 1;
                }
                nBuf = strlen30(home_dir) + 16;
                zBuf = new StringBuilder(nBuf);
                if (zBuf == null)
                {
                    fprintf(stderr, "%s: Error: out of memory\n", Argv0);
                    return 1;
                }
                Sqlite3.sqlite3_snprintf(nBuf, zBuf, "%s/.sqliterc", home_dir);
                home_dir = null;//free(home_dir);
                sqliterc = zBuf.ToString();
            }
            if (File.Exists(sqliterc))
            {
                try
                {
                    In = new StreamReader(sqliterc);// fopen(sqliterc, "rb");
                    if (In != null)
                    {
                        if (stdin_is_interactive)
                        {
                            fprintf(stderr, "-- Loading resources from %s\n", sqliterc);
                        }
                        rc = process_input(p, In);
                        In.Close();//fclose(In);
                    }
                } catch
                {
                }
            }
            zBuf = null;//free(zBuf);
            return rc;
        }

        /*
        ** Show available command line options
        */
        static string zOptions =
        "   -help                show this message\n" +
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
        "   -stats               print memory stats before each finalize\n" +
        "   -nullvalue 'text'    set text string for null values\n" +
        "   -version             show SQLite version\n" +
        "   -vfs NAME            use NAME as the default VFS\n" +
#if SQLITE_ENABLE_VFSTRACE
"   -vfstrace            enable tracing of all VFS calls\n" +
#endif
 "";
        static void usage(bool showDetail)
        {
            fprintf(stderr,
            "Usage: %s [OPTIONS] FILENAME [SQL]\n" +
            "FILENAME is the name of an SQLite database. A new database is created\n" +
            "if the file does not previously exist.\n", Argv0);
            if (showDetail)
            {
                fprintf(stderr, "OPTIONS include:\n%s", zOptions);
            }
            else
            {
                fprintf(stderr, "Use the -help option for additional information\n");
            }
            exit(1);
        }

        /*
        ** Initialize the state information in data
        */
        static void main_init(ref callback_data data)
        {
            data = new callback_data();//memset(data, 0, sizeof(*data));
            data.mode = MODE_List;
            data.separator = "|";//memcpy(data.separator, "|", 2);
            data.showHeader = false;
            Sqlite3.sqlite3_initialize();
            Sqlite3.sqlite3_config(Sqlite3.SQLITE_CONFIG_URI, 1);
            Sqlite3.sqlite3_config(Sqlite3.SQLITE_CONFIG_LOG, new object[] { (Sqlite3.dxLog)shellLog, data, null });
            snprintf(10, ref mainPrompt, "sqlite> ");
            snprintf(10, ref continuePrompt, "   ...> ");
            Sqlite3.sqlite3_config(Sqlite3.SQLITE_CONFIG_SINGLETHREAD);
        }

        static int main(int argc, string[] argv)
        {
            string zErrMsg = null;
            callback_data data = null;
            string zInitFile = null;
            StringBuilder zFirstCmd = null;
            int i;
            int rc = 0;

            if (!Sqlite3.sqlite3_sourceid().Equals(Sqlite3.SQLITE_SOURCE_ID, StringComparison.InvariantCultureIgnoreCase))
            {
                fprintf(stderr, "SQLite header and source version mismatch\n%s\n%s\n",
                Sqlite3.sqlite3_sourceid(), Sqlite3.SQLITE_SOURCE_ID);
                exit(1);
            }
            Argv0 = argv.Length == 0 ? null : argv[0];
            main_init(ref data);
            stdin_is_interactive = isatty(0);

            /* Make sure we have a valid signal handler early, before anything
            ** else is done.
            */
#if SIGINT
signal(SIGINT, Interrupt_handler);
#endif

            /* Do an initial pass through the command-line argument to locate
** the name of the database file, the name of the initialization file,
** the size of the alternative malloc heap,
** and the first command to execute.
*/
            for (i = 0; i < argc - 1; i++)
            {
                string z;
                if (argv[i][0] != '-')
                    break;
                z = argv[i];
                if (z[0] == '-' && z[1] == '-')
                    z = z.Remove(0, 1);//z++;
                if (argv[i].Equals("-separator", StringComparison.InvariantCultureIgnoreCase) || argv[i].Equals("-nullvalue", StringComparison.InvariantCultureIgnoreCase))
                {
                    i++;
                }
                else if (argv[i].Equals("-init", StringComparison.InvariantCultureIgnoreCase))
                {
                    i++;
                    zInitFile = argv[i];
                    /* Need to check for batch mode here to so we can avoid printing
                    ** informational messages (like from process_sqliterc) before 
                    ** we do the actual processing of arguments later in a second pass.
                    */
                }
                else if (argv[i].Equals("-batch", StringComparison.InvariantCultureIgnoreCase))
                {
                    stdin_is_interactive = false;
                }
                else if (argv[i].Equals("-heap", StringComparison.InvariantCultureIgnoreCase))
                {
                    int j, c;
                    string zSize;
                    sqlite3_int64 szHeap;

                    zSize = argv[++i];
                    szHeap = Convert.ToInt32(zSize);//atoi
                    for (j = 0; (c = zSize[j]) != null; j++)
                    {
                        if (c == 'M') { szHeap *= 1000000; break; }
                        if (c == 'K') { szHeap *= 1000; break; }
                        if (c == 'G') { szHeap *= 1000000000; break; }
                    }
                    if (szHeap > 0x7fff0000)
                        szHeap = 0x7fff0000;
#if (SQLITE_ENABLE_MEMSYS3) || (SQLITE_ENABLE_MEMSYS5)
Sqlite3.SQLITE_config(Sqlite3.SQLITE_CONFIG_HEAP, malloc((int)szHeap), (int)szHeap, 64);
#endif
#if SQLITE_ENABLE_VFSTRACE
}else if( argv[i].Equals("-vfstrace", StringComparison.InvariantCultureIgnoreCase) ){
extern int vfstrace_register(
string zTraceName,
string zOldVfsName,
int (*xOut)(const char*,void*),
void pOutArg,
int makeDefault
);
vfstrace_register("trace",0,(int(*)(const char*,void*))fputs,stderr,1);
#endif
                }
                else if (argv[i].Equals("-vfs", StringComparison.InvariantCultureIgnoreCase))
                {
                    Sqlite3.sqlite3_vfs pVfs = Sqlite3.sqlite3_vfs_find(argv[++i]);
                    if (pVfs != null)
                    {
                        Sqlite3.sqlite3_vfs_register(pVfs, 1);
                    }
                    else
                    {
                        fprintf(stderr, "no such VFS: \"%s\"\n", argv[i]);
                        exit(1);
                    }
                }
            }
            if (i < argc)
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
data.zDbFilename = 0;
#endif
            }
            if (i < argc)
            {
                zFirstCmd.Append(argv[i++]);
            }
            if (i < argc)
            {
                fprintf(stderr, "%s: Error: too many options: \"%s\"\n", Argv0, argv[i]);
                fprintf(stderr, "Use -help for a list of options.\n");
                return 1;
            }
            data.Out = stdout;

#if SQLITE_OMIT_MEMORYDB
if( data.zDbFilename== null ){
fprintf(stderr,"%s: Error: no database filename specified\n", Argv0);
return 1;
}
#endif

            /* Go ahead and open the database file if it already exists.  If the
** file does not exist, delay opening it.  This prevents empty database
** files from being created if a user mistypes the database name argument
** to the sqlite command-line tool.
*/
            if (File.Exists(data.zDbFilename)) //(access(data.zDbFilename, 0) == 0)
            {
                open_db(data);
            }

            /* Process the initialization file if there is one.  If no -init option
            ** is given on the command line, look for a file named ~/.sqliterc and
            ** try to process it.
            */
            rc = process_sqliterc(data, zInitFile);
            if (rc > 0)
            {
                return rc;
            }

            /* Make a second pass through the command-line argument and set
            ** options.  This second pass is delayed until after the initialization
            ** file is processed so that the command-line arguments will override
            ** settings in the initialization file.
            */
            for (i = 0; i < argc && argv[i][0] == '-'; i++)
            {
                string z = argv[i];
                if (z[1] == '-') { z = z.Remove(0, 1); } //z++;
                if (z.Equals("-init", StringComparison.InvariantCultureIgnoreCase))
                {
                    i++;
                }
                else if (z.Equals("-html", StringComparison.InvariantCultureIgnoreCase))
                {
                    data.mode = MODE_Html;
                }
                else if (z.Equals("-list", StringComparison.InvariantCultureIgnoreCase))
                {
                    data.mode = MODE_List;
                }
                else if (z.Equals("-line", StringComparison.InvariantCultureIgnoreCase))
                {
                    data.mode = MODE_Line;
                }
                else if (z.Equals("-column", StringComparison.InvariantCultureIgnoreCase))
                {
                    data.mode = MODE_Column;
                }
                else if (z.Equals("-csv", StringComparison.InvariantCultureIgnoreCase))
                {
                    data.mode = MODE_Csv;
                    data.separator = ",";//memcpy(data.separator, ",", 2);
                }
                else if (z.Equals("-separator", StringComparison.InvariantCultureIgnoreCase))
                {
                    i++;
                    if (i >= argc)
                    {
                        fprintf(stderr, "%s: Error: missing argument for option: %s\n", Argv0, z);
                        fprintf(stderr, "Use -help for a list of options.\n");
                        return 1;
                    }
                    snprintf(data.separator.Length, ref data.separator,
                    "%.*s", data.separator.Length - 1, argv[i]);
                }
                else if (z.Equals("-nullvalue", StringComparison.InvariantCultureIgnoreCase))
                {
                    i++;
                    if (i >= argc)
                    {
                        fprintf(stderr, "%s: Error: missing argument for option: %s\n", Argv0, z);
                        fprintf(stderr, "Use -help for a list of options.\n");
                        return 1;
                    }
                    snprintf(99, ref data.nullvalue,
                    "%.*s", 99 - 1, argv[i]);
                }
                else if (z.Equals("-header", StringComparison.InvariantCultureIgnoreCase))
                {
                    data.showHeader = true;
                }
                else if (z.Equals("-noheader", StringComparison.InvariantCultureIgnoreCase))
                {
                    data.showHeader = false;
                }
                else if (z.Equals("-echo", StringComparison.InvariantCultureIgnoreCase))
                {
                    data.echoOn = true;
                }
                else if (z.Equals("-stats", StringComparison.InvariantCultureIgnoreCase))
                {
                    data.statsOn = true;
                }
                else if (z.Equals("-bail", StringComparison.InvariantCultureIgnoreCase))
                {
                    bail_on_error = true;
                }
                else if (z.Equals("-version", StringComparison.InvariantCultureIgnoreCase))
                {
                    printf("%s %s\n", Sqlite3.sqlite3_libversion(), Sqlite3.sqlite3_sourceid());
                    return 0;
                }
                else if (z.Equals("-interactive", StringComparison.InvariantCultureIgnoreCase))
                {
                    stdin_is_interactive = true;
                }
                else if (z.Equals("-batch", StringComparison.InvariantCultureIgnoreCase))
                {
                    stdin_is_interactive = false;
                }
                else if (z.Equals("-heap", StringComparison.InvariantCultureIgnoreCase))
                {
                    i++;
                }
                else if (z.Equals("-vfs", StringComparison.InvariantCultureIgnoreCase))
                {
                    i++;
                }
                else if (z.Equals("-vfstrace", StringComparison.InvariantCultureIgnoreCase))
                {
                    i++;
                }
                else if (z.Equals("-help", StringComparison.InvariantCultureIgnoreCase) || z.Equals("--help", StringComparison.InvariantCultureIgnoreCase))
                {
                    usage(true);
                }
                else
                {
                    fprintf(stderr, "%s: Error: unknown option: %s\n", Argv0, z);
                    fprintf(stderr, "Use -help for a list of options.\n");
                    return 1;
                }
            }

            if (zFirstCmd != null && zFirstCmd.Length > 0)
            {
                /* Run just the command that follows the database name
                */
                if (zFirstCmd[0] == '.')
                {
                    rc = do_meta_command(zFirstCmd, data);
                }
                else
                {
                    open_db(data);
                    rc = shell_exec(data.db, zFirstCmd.ToString(), shell_callback, data, ref zErrMsg);
                    if (zErrMsg != null)
                    {
                        fprintf(stderr, "Error: %s\n", zErrMsg);
                        return rc != 0 ? rc : 1;
                    }
                    else if (rc != 0)
                    {
                        fprintf(stderr, "Error: unable to process SQL \"%s\"\n", zFirstCmd);
                        return rc;
                    }
                }
            }
            else
            {
                /* Run commands received from standard input
                */
                if (stdin_is_interactive)
                {
                    string zHome;
                    string zHistory = null;
                    int nHistory;
                    printf(
#if (SQLITE_HAS_CODEC) && (SQLITE_ENABLE_CEROD)
                    "SQLite version %s with the CEROD Extension\n" + 
                    "Copyright 2006 Hipp, Wyrick & Company, Inc.\n" +
#else
                    "SQLite version %s\n" +
#endif
                    "(source %.19s)\n" +
                    "Enter \".help\" for instructions\n" +
                    "Enter SQL statements terminated with a \";\"\n",
                    Sqlite3.sqlite3_libversion(), Sqlite3.sqlite3_sourceid()
                    );
                    zHome = find_home_dir();
                    if (zHome != null)
                    {
                        nHistory = strlen30(zHome) + 20;
                        //if ((zHistory = malloc(nHistory)) != null)
                        {
                            snprintf(nHistory, ref zHistory, "%s/.Sqlite3.SQLITE_history", zHome);
                        }
                    }
#if (HAVE_READLINE)// && HAVE_READLINE==1
if( zHistory ) read_history(zHistory);
#endif
                    rc = process_input(data, null);
                    if (zHistory != null)
                    {
                        stifle_history(100);
                        write_history(zHistory);
                        zHistory = null;//free(zHistory);
                    }
                    zHome = null;//free(zHome);
                }
                else
                {
                    rc = process_input(data, stdin);
                }
            }
            set_table_name(data, null);
            if (data.db != null)
            {
                Sqlite3.sqlite3_close(data.db);
            }
            return rc;
        }
        // C# DllImports
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern void FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        // Helper Variables for C#
        static TextReader stdin = Console.In;
        static TextWriter stdout = Console.Out;
        static TextWriter stderr = Console.Error;

        // Helper Functions for C#
        private static void exit(int p)
        {
            if (p == 0)
            {
                Console.WriteLine("Enter to CONTINUE:");
            }
            else
            {
                Console.WriteLine(String.Format("Error: {0}", p));
            }
            Console.ReadKey();
        }
        private static void fflush(TextWriter tw)
        {
            tw.Flush();
        }
        private static int fgets(StringBuilder p, int p_2, TextReader In)
        {
            try
            {
                p.Length = 0;
                p.Append(In.ReadLine());
                if (p.Length > 0)
                    p.Append('\n');
                return p.Length;
            } catch
            {
                return 0;
            }
        }
        private static void fputc(char c, TextWriter Out)
        {
            Out.Write(c);
        }
        private static string getenv(string p)
        {
            switch (p)
            {
                case "USERPROFILE":
                    return Environment.GetEnvironmentVariable("UserProfile");
                case "HOME":
                    {
                        return Environment.GetEnvironmentVariable("Home");
                    }
                case "HOMEDRIVE":
                    {
                        return Environment.GetEnvironmentVariable("HomeDrive");
                    }
                case "HOMEPATH":
                    {
                        return Environment.GetEnvironmentVariable("HomePath");
                    }
                default:
                    throw new Exception("The method or operation is not implemented.");
            }
        }
        static bool isalnum(char c)
        {
            return char.IsLetterOrDigit(c);
        }
        static bool isalpha(char c)
        {
            return char.IsLetter(c);
        }
        static bool isdigit(char c)
        {
            return char.IsDigit(c);
        }
        static bool isprint(char c)
        {
            return !char.IsControl(c);
        }
        private static bool isspace(char c)
        {
            return char.IsWhiteSpace(c);
        }
        static void fprintf(TextWriter tw, string zFormat, params va_list[] ap)
        {
            tw.Write(Sqlite3.sqlite3_mprintf(zFormat, ap));
        }
        public static void Main(string[] args)
        {
            main(args.Length, args);
        }

        static void printf(string zFormat, params va_list[] ap)
        {
            stdout.Write(Sqlite3.sqlite3_mprintf(zFormat, ap));
        }
        private static void putc(char c, TextWriter _out)
        {
            _out.Write(c);
        }
        private static void snprintf(int n, ref string zBuf, string zFormat, params va_list[] ap)
        {
            StringBuilder sbBuf = new StringBuilder(100);
            Sqlite3.sqlite3_snprintf(n, sbBuf, zFormat, ap);
            zBuf = sbBuf.ToString();
        }
    }
}