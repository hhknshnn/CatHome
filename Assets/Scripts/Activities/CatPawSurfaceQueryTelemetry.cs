using System;

public static partial class CatPawReachResolver
{
#if UNITY_EDITOR
    // Optional QA observer, inactive by default. Never a source of permission.
    public static Action<string,double,int,bool> SurfaceQueryMeasurement;
#endif
    static long BeginSurfaceMeasure()
    {
#if UNITY_EDITOR
        if(SurfaceQueryMeasurement!=null)return System.Diagnostics.Stopwatch.GetTimestamp();
#endif
        return 0;
    }
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    static void EndSurfaceMeasure(long started,string stage,int sample,bool clear)
    {
#if UNITY_EDITOR
        if(started!=0)SurfaceQueryMeasurement?.Invoke(stage,
            (System.Diagnostics.Stopwatch.GetTimestamp()-started)*1000.0/System.Diagnostics.Stopwatch.Frequency,sample,clear);
#endif
    }
}
