namespace EcsSharp.StructComponents
{
    public interface IStructComponent
    {
        object UntypedData { get; }
    }

    public interface IStructComponent<T> : IStructComponent
    {
        public T Data { get; init; }
    }

    public abstract class StructComponent<T> : IStructComponent<T>
    {
        protected StructComponent()
        {
        }

        protected StructComponent(T data) => Data = data;

        public T Data { get; init; }

        public object UntypedData => Data;

        protected bool Equals(StructComponent<T> other) => Data.Equals(other.Data);

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((StructComponent<T>)obj);
        }

        public override int GetHashCode() => Data.GetHashCode();

        public override string ToString() => Data.ToString();
        
    }
}