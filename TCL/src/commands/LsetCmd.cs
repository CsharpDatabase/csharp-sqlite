/*
* LsetCmd.java
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
* RCS @(#) $Id: LsetCmd.java,v 1.3 2003/01/09 02:15:39 mdejong Exp $
*
*/
using System;
namespace tcl.lang
{

  /// <summary> This class implements the built-in "Lset" command in Tcl.</summary>

  class LsetCmd : Command
  {
    /// <summary> See Tcl user documentation for details.</summary>
    /// <exception cref=""> TclException If incorrect number of arguments.
    /// </exception>

    public TCL.CompletionCode cmdProc( Interp interp, TclObject[] argv )
    {
      if ( argv.Length != 4 )
      {
        throw new TclNumArgsException( interp, 1, argv, "lset list index element" );
      }

      int size = TclList.getLength( interp, argv[1] );
      int index = Util.getIntForIndex( interp, argv[2], size );
      TclObject list = argv[1];
      bool isDuplicate = false;

      // If the list object is unshared we can modify it directly. Otherwise
      // we create a copy to modify: this is "copy on write".

      if ( list.Shared )
      {
        list = list.duplicate();
        isDuplicate = true;
      }

      try
      { TclObject[] replace = new TclObject[1];
        replace[0]=argv[3];
        TclList.replace(interp,list,index,1,replace,0,0 );
        interp.setResult( list );
      }
      catch ( TclException e )
      {
        if ( isDuplicate )
        {
          list.release();
        }
        throw;
      }
      return TCL.CompletionCode.RETURN;
    }
  }
}
