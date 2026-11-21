namespace EcsSharp.StructComponents
{
    public abstract class Int16Component : StructComponent<int>
    {
    }

    public abstract class Int16Component<T> : Int16Component
        where T : Int16Component<T>, new()

    {
        public static implicit operator Int16Component<T>(short d) =>
            new T
            {
                Data = d
            };

        public static implicit operator int(Int16Component<T> d) => d.Data;
        public static implicit operator T(Int16Component<T>   d) => (T)d.Data;
        public static implicit operator Int16Component<T>(T   d) => d;
    }

    public abstract class UInt16Component : StructComponent<ushort>
    {
    }
    public abstract class UInt16Component<T> : UInt16Component
        where T : UInt16Component<T>, new()

    {
        public static implicit operator UInt16Component<T>(ushort d) =>
            new T
            {
                Data = d
            };

        public static implicit operator uint(UInt16Component<T> d) => d.Data;
        public static implicit operator T(UInt16Component<T>    d) => (T)d.Data;
        public static implicit operator UInt16Component<T>(T    d) => d;
    }

    public abstract class Int32Component : StructComponent<int>
    {
    }

    public abstract class Int32Component<T> : Int32Component
        where T : Int32Component<T>, new()

    {
        public static implicit operator Int32Component<T>(int d) =>
            new T
            {
                Data = d
            };

        public static implicit operator int(Int32Component<T> d) => d.Data;
        public static implicit operator T(Int32Component<T>   d) => (T)d.Data;
        public static implicit operator Int32Component<T>(T   d) => d;
    }

    public abstract class UInt32Component : StructComponent<uint>
    {
    }

    public abstract class UInt32Component<T> : UInt32Component
        where T : UInt32Component<T>, new()

    {
        public static implicit operator UInt32Component<T>(uint d) =>
            new T
            {
                Data = d
            };

        public static implicit operator uint(UInt32Component<T> d) => d.Data;
        public static implicit operator T(UInt32Component<T>    d) => (T)d.Data;
        public static implicit operator UInt32Component<T>(T    d) => d;
    }

    public abstract class Int64Component : StructComponent<long>
    {
    }

    public abstract class Int64Component<T> : Int64Component
        where T : Int64Component<T>, new()

    {
        public static implicit operator Int64Component<T>(long d) =>
            new T
            {
                Data = d
            };

        public static implicit operator long(Int64Component<T> d) => d.Data;
        public static implicit operator T(Int64Component<T>    d) => (T)d.Data;
        public static implicit operator Int64Component<T>(T    d) => d;
    }

    public abstract class UInt64Component : StructComponent<ulong>
    {
    }

    public abstract class UInt64Component<T> : UInt64Component
        where T : UInt64Component<T>, new()

    {
        public static implicit operator UInt64Component<T>(ulong d) =>
            new T
            {
                Data = d
            };

        public static implicit operator ulong(UInt64Component<T> d) => d.Data;
        public static implicit operator T(UInt64Component<T>     d) => (T)d.Data;
        public static implicit operator UInt64Component<T>(T     d) => d;
    }

    public abstract class DoubleComponent : StructComponent<double>
    {
    }

    public abstract class DoubleComponent<T> : DoubleComponent
        where T : DoubleComponent<T>, new()

    {
        public static implicit operator DoubleComponent<T>(double d) =>
            new T
            {
                Data = d
            };

        public static implicit operator double(DoubleComponent<T> d) => d.Data;
        public static implicit operator T(DoubleComponent<T>      d) => (T)d.Data;
        public static implicit operator DoubleComponent<T>(T      d) => d;
    }

    public abstract class FloatComponent : StructComponent<float>
    {
    }

    public abstract class FloatComponent<T> : FloatComponent
        where T : FloatComponent<T>, new()

    {
        public static implicit operator FloatComponent<T>(float d) =>
            new T
            {
                Data = d
            };

        public static implicit operator float(FloatComponent<T> d) => d.Data;
        public static implicit operator T(FloatComponent<T>     d) => (T)d.Data;
        public static implicit operator FloatComponent<T>(T     d) => d;
    }
}