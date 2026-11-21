using System;

namespace EcsSharp.Helpers.Synchronizer
{
    public class SynchronizerConfigBuilder
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public TimeSpan InvocationWarningTimeout { get; set; } = Consts.DefaultInvocationWarningTimeout;
        public TimeSpan ExecutionWarningTimeout { get; set; } = Consts.DefaultExecutionWarningTimeout;
        public string ThreadName { get; set; } = Consts.DefaultThreadName;
        public int MaxStackSize { get; set; }
        public bool UseObjectPool { get; set; }
        public bool SingleWriter { get; set; }
        public int BoundedSynchronizerMaxItems { get; set; }

        public SynchronizerConfigBuilder WithId(string value)
        {
            Id = value;
            return this;
        }

        public SynchronizerConfigBuilder WithInvocationWarningTimeout(TimeSpan value)
        {
            InvocationWarningTimeout = value;
            return this;
        }

        public SynchronizerConfigBuilder WithExecutionWarningTimeout(TimeSpan value)
        {
            ExecutionWarningTimeout = value;
            return this;
        }

        public SynchronizerConfigBuilder WithThreadName(string value)
        {
            ThreadName = value;
            return this;
        }

        public SynchronizerConfigBuilder WithMaxStackSize(int value)
        {
            MaxStackSize = value;
            return this;
        }

        public SynchronizerConfigBuilder WithUseObjectPool(bool value)
        {
            UseObjectPool = value;
            return this;
        }

        public SynchronizerConfigBuilder WithSingleWriter(bool value)
        {
            SingleWriter = value;
            return this;
        }

        public SynchronizerConfigBuilder WithBoundedSynchronizerMaxItems(int value)
        {
            BoundedSynchronizerMaxItems = value;
            return this;
        }

        public SynchronizerConfig Build() =>
            new SynchronizerConfig
            {
                Id = Id,
                InvocationWarningTimeout = InvocationWarningTimeout,
                ExecutionWarningTimeout = ExecutionWarningTimeout,
                ThreadName = ThreadName,
                MaxStackSize = MaxStackSize,
                UseObjectPool = UseObjectPool,
                SingleWriter = SingleWriter,
                BoundedSynchronizerMaxItems = BoundedSynchronizerMaxItems
            };

        
    }
}