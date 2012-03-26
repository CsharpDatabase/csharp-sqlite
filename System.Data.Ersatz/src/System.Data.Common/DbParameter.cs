//
// System.Data.Common.DbParameter.cs
//
// Author:
//   Tim Coleman (tim@timcoleman.com)
//
// Copyright (C) Tim Coleman, 2003
//

//
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System.Collections.Generic ;
using System.ComponentModel;

namespace System.Data.Common {
	public abstract class DbParameter : IDbDataParameter, IDataParameter
	{
		#region Constructors
		internal static Dictionary<DbType, Type> dbTypeMapping;
		protected DbParameter ()
		{
		}

		#endregion // Constructors

		#region Properties
#if !(WINDOWS_PHONE || NETFX_CORE)
		[Browsable (false)]
#endif
		public abstract DbType DbType { get; set; }

		[DefaultValue (ParameterDirection.Input)]
		public abstract ParameterDirection Direction { get; set; }

		[DefaultValue ("")]
		public abstract string ParameterName { get; set; }
		public abstract int Size { get; set; }
		byte IDbDataParameter.Precision { 
			get { return  0; }
			set {} 
		}
		byte IDbDataParameter.Scale { 
			get { return 0; }
			set {} 
		}

		[DefaultValue (null)]
		public abstract object Value { get; set; }
#if !(WINDOWS_PHONE || NETFX_CORE)
		[Browsable (false)]
#endif
		[EditorBrowsable (EditorBrowsableState.Never)]
		public abstract bool IsNullable { get; set; }

		[DefaultValue ("")]
		public abstract string SourceColumn { get; set; }

		[DefaultValue (false)]
		[EditorBrowsable (EditorBrowsableState.Advanced)]
		public abstract bool SourceColumnNullMapping { get; set; }

        //[DefaultValue (DataRowVersion.Current)]
        //public abstract DataRowVersion SourceVersion { get; set; }

		#endregion // Properties

		#region Methods
		[EditorBrowsable (EditorBrowsableState.Advanced)]
		public abstract void ResetDbType ();
		
		internal virtual object FrameworkDbType {
			get {return null;}
			set {}
		}

        internal protected static Dictionary<DbType, Type> DbTypeMapping
        {
			get { return dbTypeMapping;}
			set { dbTypeMapping = value;}
		}
		
		// LAMESPEC: Implementors should populate the dbTypeMapping accordingly
		internal virtual Type SystemType {
			get {
				return (Type) dbTypeMapping [DbType];
			}
		}
		#endregion // Methods
	}
}


