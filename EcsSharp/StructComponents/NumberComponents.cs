namespace EcsSharp.StructComponents
{
    public class Int16Component : StructComponent<int>
    {
        public static implicit operator Int16Component(short d) =>
            new Int16Component
            {
                Data = d
            };
    }

    public class UInt16Component : StructComponent<ushort>
    {
        public static implicit operator UInt16Component(ushort d) =>
            new UInt16Component
            {
                Data = d
            };  
    }

    public class Int32Component : StructComponent<int>
    {
        public static implicit operator Int32Component(int d) =>
            new Int32Component
            {
                Data = d
            };
    }

    public class Int32Component<T> : Int32Component
        where T : Int32Component<T>, new()

    {
        public static implicit operator Int32Component<T>(int d) =>
            new T
            {
                Data = d
            };

        public static implicit operator int(Int32Component<T> d) => d.Data;
        public static implicit operator T(Int32Component<T> d) => (T)d.Data;
        public static implicit operator Int32Component<T>(T d) => d;
    }

    public class UInt32Component : StructComponent<uint>
    {
        public static implicit operator UInt32Component(uint d) =>
            new UInt32Component
            {
                Data = d
            };
    }

    public class Int64Component : StructComponent<long>
    {
        public static implicit operator Int64Component(long d) =>
            new Int64Component
            {
                Data = d
            };
    }

    public class UInt64Component : StructComponent<ulong>
    {
        public static implicit operator UInt64Component(ulong d) =>
            new UInt64Component
            {
                Data = d
            };
    }
    public class DoubleComponent<T>: DoubleComponent
        where T: DoubleComponent<T>, new()

    {
        public static implicit operator DoubleComponent<T>(double d) =>
            new T
            {
                Data = d
            };

        public static implicit operator double(DoubleComponent<T> d) => d.Data;
        public static implicit operator T(DoubleComponent<T> d) => (T)d.Data;
        public static implicit operator DoubleComponent<T>(T d) => d;
    }
    public class DoubleComponent : StructComponent<double>
    {
        public static implicit operator DoubleComponent(double d) =>
            new DoubleComponent
            {
                Data = d
            };
    }

    public class FloatComponent : StructComponent<float>
    {
        public static implicit operator FloatComponent(float d) =>
            new FloatComponent
            {
                Data = d
            };
    }
}