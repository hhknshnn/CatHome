using System.Collections.Generic;
using UnityEngine;

/// <summary>A small board-local path keeps the cat's approach out of cushions and the yarn.</summary>
public static class CozyCatPath
{
    const float Cell=.20f;
    const int Width=37, Height=29;
    static Vector2 Point(int i)=>new Vector2(-3.6f+(i%Width)*Cell,-2.8f+(i/Width)*Cell);
    static bool Free(Vector2 p,IList<Rect> cushions,Vector2 ball)
    {
        if(Vector2.Distance(p,ball)<.40f)return false;
        foreach(var c in cushions)
            if(p.x>c.xMin-.24f&&p.x<c.xMax+.24f&&p.y>c.yMin-.24f&&p.y<c.yMax+.24f)return false;
        return true;
    }
    static bool Clear(Vector2 a,Vector2 b,IList<Rect> cushions,Vector2 ball)
    {
        int n=Mathf.CeilToInt(Vector2.Distance(a,b)/.08f);
        for(int i=1;i<=n;i++)if(!Free(Vector2.Lerp(a,b,(float)i/n),cushions,ball))return false;
        return true;
    }
    public static List<Vector3> Find(Vector3 start,Vector3 end,IList<Rect> cushions,Vector2 ball)
    {
        var a=new Vector2(start.x,start.z);var b=new Vector2(end.x,end.z);
        var result=new List<Vector3>();
        if(Clear(a,b,cushions,ball)){result.Add(end);return result;}
        int count=Width*Height,first=-1;float nearest=float.MaxValue;
        var previous=new int[count];for(int i=0;i<count;i++)previous[i]=-2;
        for(int i=0;i<count;i++)
        {
            float d=(Point(i)-a).sqrMagnitude;
            if(d<nearest&&Free(Point(i),cushions,ball)&&Clear(a,Point(i),cushions,ball)){nearest=d;first=i;}
        }
        if(first<0)return result;
        var queue=new Queue<int>();queue.Enqueue(first);previous[first]=-1;
        int best=first;float score=(Point(first)-b).sqrMagnitude;
        while(queue.Count>0)
        {
            int node=queue.Dequeue();var p=Point(node);float d=(p-b).sqrMagnitude;
            if(d<score){score=d;best=node;}
            if(d<Cell*Cell*.3f&&Clear(p,b,cushions,ball)){best=node;break;}
            foreach(int step in new[]{-1,1,-Width,Width})
            {
                int next=node+step;
                if(next<0||next>=count||Mathf.Abs(next%Width-node%Width)>1||previous[next]!=-2||!Free(Point(next),cushions,ball))continue;
                previous[next]=node;queue.Enqueue(next);
            }
        }
        var points=new List<Vector2>();for(int i=best;i>=0;i=previous[i])points.Add(Point(i));points.Reverse();
        if(Clear(points[points.Count-1],b,cushions,ball))points.Add(b);
        var current=a;
        for(int i=0;i<points.Count;)
        {
            int last=i;for(int j=i+1;j<points.Count&&Clear(current,points[j],cushions,ball);j++)last=j;
            current=points[last];result.Add(new Vector3(current.x,start.y,current.y));i=last+1;
        }
        return result;
    }
}
