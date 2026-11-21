using System;

namespace EcsSharp.Helpers.Synchronizer
{
    public class SynchronizerConfig
    {

        public string Id { get; set; }
        public TimeSpan InvocationWarningTimeout { get; set;}
        public TimeSpan ExecutionWarningTimeout { get; set;}
        public string ThreadName { get; set;}
        public int MaxStackSize { get; set;}
        public bool UseObjectPool { get; set; }
        public bool SingleWriter { get; set; }
        public int BoundedSynchronizerMaxItems { get; set; }

        


        public void VerifyConfig()
        {
            if (ExecutionWarningTimeout <= TimeSpan.Zero)
            {
                throw new ApplicationException("ExecutionWarningTimeout in SynchronizerConfig cannot be smaller or equal to TimeSpan.Zero");
            }

            if (InvocationWarningTimeout <= TimeSpan.Zero)
            {
                throw new ApplicationException("InvocationWarningTimeout in SynchronizerConfig cannot be smaller or equal to TimeSpan.Zero");
            }
        }

        public override string ToString() => $"{nameof(Id)}: {Id}, {nameof(InvocationWarningTimeout)}: {InvocationWarningTimeout}, {nameof(ExecutionWarningTimeout)}: {ExecutionWarningTimeout}, {nameof(ThreadName)}: {ThreadName}, {nameof(MaxStackSize)}: {MaxStackSize}";
    }
}