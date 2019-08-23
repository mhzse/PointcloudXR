/*
    Author: Mikael Hertz (mikael.hertz@gmail.com)
*/

public class ComputeBufferDataFactory
{
    public static float[] CreateSelectBufferData(int size)
    {
        return new float[size];
    }

    public static float[] CreateDeleteBufferData(int size)
    {
        return new float[size];
    }

    public static float[] CreateOffsetBufferData(int size)
    {
        return new float[size * 3]; ;
    }

    public static float[] CreateTranslatedBufferData(int size)
    {
        return new float[size * 3]; ;
    }

    public static float[] CreateNearestPointBufferData(int size)
    {
        return new float[size * 6]; ;
    }
}
