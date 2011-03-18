/*
* ListCmd.java
*
* Copyright (c) 1997 Cornell University.
* Copyright (c) 1997 Sun Microsystems, Inc.
*
* See the file "license.terms" for information on usage and
* redistribution of this file, and for a DISCLAIMER OF ALL
* WARRANTIES.
* 
* Included in SQLite3 port to C# for use in testharness only;  2008 Noah B Hart
*
* RCS @(#) $Id: ListCmd.java,v 1.1.1.1 1998/10/14 21:09:19 cvsadmin Exp $
*
*/
using System;
using System.Text;

namespace tcl.lang
{

  /// <summary> This class implements the built-in "list" command in Tcl.</summary>
  class ListCmd : Command
  {

    /// <summary> See Tcl user documentation for details.</summary>
    public TCL.CompletionCode cmdProc( Interp interp, TclObject[] argv )
    {
      TclObject list = TclList.newInstance();

      list.preserve();
      try
      {
        if ( argv.Length > 1 && argv[1].ToString().StartsWith( "{*}" ) )
        {
          StringBuilder sbuf = new StringBuilder( argv[1].ToString().Length );
          StringBuilder sArgv = new StringBuilder( argv[1].ToString() );
          for ( int i = 3; i < sArgv.Length; i++ )
          {
            //if (sArgv[i] == '{' && ++bBrace == 1) continue;
            //if (sArgv[i] == '}' && --bBrace == 0) continue;
            sbuf.Append( sArgv[i] );
          }
          TclList.append( interp, list, TclString.newInstance( sbuf.ToString().Trim() ) );
        }
        else
          for ( int i = 1; i < argv.Length; i++ )
            TclList.append( interp, list, argv[i] );
        interp.setResult( list );
      }
      finally
      {
        list.release();
      }
      return TCL.CompletionCode.RETURN;
    }
  }
}
