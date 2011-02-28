/*
* TclRegexp.java
*
* Copyright (c) 1999 Sun Microsystems, Inc.
*
* See the file "license.terms" for information on usage and
* redistribution of this file, and for a DISCLAIMER OF ALL
* WARRANTIES.
* 
* SCCS: %Z% %M% %I% %E% %U%
* Included in SQLite3 port to C# for use in testharness only;  2008 Noah B Hart
* $Header: TCL/src/base/TclRegexp.cs,v 47be2d23056c 2011/02/28 18:04:55 Noah $
*/
using System;
using Regexp = sunlabs.brazil.util.regexp.Regexp;
namespace tcl.lang
{
	
	public class TclRegexp
	{
		private TclRegexp()
		{
		}
		
		public static Regexp compile(Interp interp, TclObject exp, bool nocase)
		{
			try
			{
				
				return new Regexp(exp.ToString(), nocase);
			}
			catch (System.ArgumentException e)
			{
				string msg = e.Message;
				if (msg.Equals("missing )"))
				{
					msg = "unmatched ()";
				}
				else if (msg.Equals("missing ]"))
				{
					msg = "unmatched []";
				}
				msg = "couldn't compile regular expression pattern: " + msg;
				throw new TclException(interp, msg);
			}
		}
	}
}
