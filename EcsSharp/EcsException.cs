using System;

namespace EcsSharp
{
    public class EcsException : Exception
    {
        public EcsException(string message):base(message)
        {
        }
    }
}