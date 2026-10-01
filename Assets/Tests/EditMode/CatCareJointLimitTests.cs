using System;
using NUnit.Framework;
using UnityEngine;

public sealed class CatCareJointLimitTests
{
    [Test]public void ActualNativeOvershootWitnesses_ClampToOriginalThirtyTwentyFiveTwentyDegrees()
    {
        var sources=new[]{new Quaternion(.121032111f,-.0251984f,-.007901833f,.9922973f),
            new Quaternion(-.0561944656f,1.21729954E-7f,1.22429554E-7f,.9984199f),
            new Quaternion(-.343208432f,.07085629f,-.02740743f,.9361817f)};
        var requests=new[]{new Quaternion(-.139786631f,-.0214076452f,.009548046f,.989904165f),
            new Quaternion(-.262924641f,.0105702635f,.0588560179f,.962961555f),
            new Quaternion(-.4441351f,.165615708f,-.113046207f,.87323314f)};
        var limits=new[]{30f,25f,20f};
        for(int i=0;i<3;i++)
        {
            Assert.That(Angle(sources[i],requests[i]),Is.GreaterThan(limits[i]+.02));
            Quaternion result=CatCareJointLimit.Clamp(sources[i],requests[i],limits[i]);
            Assert.That(Angle(sources[i],result),Is.EqualTo(limits[i]).Within(.00002));
            Assert.That(Norm(result),Is.EqualTo(1).Within(.000001));
        }
    }
    [Test]public void DeterministicRotations_PreserveInteriorAndShortestArcWithoutWideningBound()
    {
        var random=new System.Random(9162026);
        for(int sample=0;sample<600;sample++)
        {
            var source=Unit(random);var request=Unit(random);float limit=sample%3==0?30:sample%3==1?25:20;
            var result=CatCareJointLimit.Clamp(source,request,limit);double requested=Angle(source,request),actual=Angle(source,result);
            Assert.That(actual,Is.EqualTo(Math.Min(limit,requested)).Within(.000025),"sample"+sample);
            Assert.That(Angle(result,CatCareJointLimit.Clamp(source,Negate(request),limit)),Is.LessThan(.000025));
            Assert.That(Angle(result,CatCareJointLimit.Clamp(Scale(source,.7f),Scale(request,1.4f),limit)),Is.LessThan(.000025));
            Assert.That(Norm(result),Is.EqualTo(1).Within(.000001));
        }
        var first=new Quaternion(0,0,0,1);var inside=new Quaternion(0,(float)Math.Sin(.03),0,(float)Math.Cos(.03));
        Assert.That(Angle(inside,CatCareJointLimit.Clamp(first,inside,20)),Is.LessThan(.000001));
        Assert.That(Angle(first,CatCareJointLimit.Clamp(first,inside,0)),Is.LessThan(.000001));
    }
    static Quaternion Unit(System.Random random)
    {var q=new Quaternion((float)(random.NextDouble()*2-1),(float)(random.NextDouble()*2-1),(float)(random.NextDouble()*2-1),(float)(random.NextDouble()*2-1));return Scale(q,(float)(1/Norm(q)));}
    static Quaternion Scale(Quaternion q,float scale)=>new Quaternion(q.x*scale,q.y*scale,q.z*scale,q.w*scale);
    static Quaternion Negate(Quaternion q)=>Scale(q,-1);
    static double Norm(Quaternion q)=>Math.Sqrt((double)q.x*q.x+(double)q.y*q.y+(double)q.z*q.z+(double)q.w*q.w);
    static double Angle(Quaternion a,Quaternion b)
    {
        // Relative-quaternion atan2 retains precision near identity; acos(dot)
        // loses several microdegrees even for two bit-identical float quaternions.
        double x=(double)a.w*b.x-(double)a.x*b.w-(double)a.y*b.z+(double)a.z*b.y;
        double y=(double)a.w*b.y+(double)a.x*b.z-(double)a.y*b.w-(double)a.z*b.x;
        double z=(double)a.w*b.z-(double)a.x*b.y+(double)a.y*b.x-(double)a.z*b.w;
        double w=(double)a.w*b.w+(double)a.x*b.x+(double)a.y*b.y+(double)a.z*b.z;
        return 2*Math.Atan2(Math.Sqrt(x*x+y*y+z*z),Math.Abs(w))*180/Math.PI;
    }
}
