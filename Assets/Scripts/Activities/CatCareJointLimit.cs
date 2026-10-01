using System;
using UnityEngine;

/// <summary>Source-relative shortest-arc limit without approximate quaternion interpolation.</summary>
public static class CatCareJointLimit
{
    public static Quaternion Clamp(Quaternion source, Quaternion requested, float maximumDegrees)
    {
        double an=Norm(source),bn=Norm(requested);
        if(!(an>1e-12))return Quaternion.identity;
        double ax=source.x/an,ay=source.y/an,az=source.z/an,aw=source.w/an;
        if(!(bn>1e-12)||!(maximumDegrees>0))return Result(ax,ay,az,aw);
        double bx=requested.x/bn,by=requested.y/bn,bz=requested.z/bn,bw=requested.w/bn;
        double dot=ax*bx+ay*by+az*bz+aw*bw;
        if(dot<0){bx=-bx;by=-by;bz=-bz;bw=-bw;}
        // inv(source) * requested, computed in double precision. Its vector
        // magnitude and scalar give the true half angle even near zero/pi.
        double dx=aw*bx-ax*bw-ay*bz+az*by;
        double dy=aw*by+ax*bz-ay*bw-az*bx;
        double dz=aw*bz-ax*by+ay*bx-az*bw;
        double dw=aw*bw+ax*bx+ay*by+az*bz;
        double sine=Math.Sqrt(dx*dx+dy*dy+dz*dz);
        double halfLimit=Math.Min(180,maximumDegrees)*Math.PI/360;
        if(sine<1e-15||Math.Atan2(sine,Math.Max(0,dw))<=halfLimit)
            return Result(bx,by,bz,bw);
        double factor=Math.Sin(halfLimit)/sine;
        dx*=factor;dy*=factor;dz*=factor;dw=Math.Cos(halfLimit);
        return Result(aw*dx+ax*dw+ay*dz-az*dy,
            aw*dy-ax*dz+ay*dw+az*dx,
            aw*dz+ax*dy-ay*dx+az*dw,
            aw*dw-ax*dx-ay*dy-az*dz);
    }
    static double Norm(Quaternion q)=>Math.Sqrt((double)q.x*q.x+(double)q.y*q.y+(double)q.z*q.z+(double)q.w*q.w);
    static Quaternion Result(double x,double y,double z,double w)
    {
        double norm=Math.Sqrt(x*x+y*y+z*z+w*w);
        return new Quaternion((float)(x/norm),(float)(y/norm),(float)(z/norm),(float)(w/norm));
    }
}
