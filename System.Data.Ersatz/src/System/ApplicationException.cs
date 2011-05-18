﻿using System;

namespace System
{
    public class ApplicationException :Exception 
    {
        public ApplicationException()
            : base()
        {

        }

        public ApplicationException(string message)
            : base(message)
        {

        }

        public ApplicationException(string message, Exception innerException)
            : base(message, innerException)
        {
            
        }
    }
}
