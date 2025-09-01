using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public struct AtomicDouble
{
    [StructLayout(LayoutKind.Explicit)]
    public struct DoubleLongUnion
    {
        [FieldOffset(0)] public double Double;
        [FieldOffset(0)] public long Long;
        
        public DoubleLongUnion(double d)
        {
            Long = 0;
            Double = d;
        }
        
        public DoubleLongUnion(long l)
        {
            Double = 0;
            Long = l;
        }
    }

    private NativeArray<long> value;

    public AtomicDouble(Allocator allocator, double initialValue = 0)
    {
        value = new NativeArray<long>(1, allocator);
        Set(initialValue);
    }

    public void Dispose() => value.Dispose();

    public unsafe void Add(double amount)
    {
        long* ptr = (long*)value.GetUnsafePtr();
        long oldLong, newLong;

        do
        {
            oldLong = *ptr;
            DoubleLongUnion u = new DoubleLongUnion(oldLong);
            u.Double += amount;
            newLong = u.Long;
        } while (Interlocked.CompareExchange(ref *ptr, newLong, oldLong) != oldLong);
    }

    public double Get()
    {
        DoubleLongUnion u = new DoubleLongUnion(value[0]);
        return u.Double;
    }

    private void Set(double val)
    {
        DoubleLongUnion u = new DoubleLongUnion(val);
        value[0] = u.Long;
    }
}