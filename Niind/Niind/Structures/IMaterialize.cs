namespace Niind.Structures
{
    public interface IMaterialize<out T>
    {
        T ToManagedObject();
    }
}