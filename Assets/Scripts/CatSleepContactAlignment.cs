using System.Collections.Generic;
using UnityEngine;

/// <summary>Fit the visible sleeping body to a measured mattress without resizing the breed.</summary>
[DefaultExecutionOrder(550)]
public sealed class CatSleepContactAlignment:MonoBehaviour
{
    SleepInteraction sleep;
    Transform visual;
    SkinnedMeshRenderer skin;
    Vector3 original;
    bool adjusted;
    Mesh sample;
    readonly List<Vector3> vertices=new List<Vector3>();
    void Update(){Restore();}
    void LateUpdate()
    {
        if(sleep==null)sleep=GetComponent<SleepInteraction>();
        if(sleep==null || !sleep.IsSettledOnBed || sleep.SleepSurface==null || sleep.SleepSurface.GetComponent<CatActivitySurface>()==null)return;
        var animator=GetComponentInChildren<Animator>();if(animator==null)return;
        if(visual!=animator.transform){Restore();visual=animator.transform;skin=visual.GetComponentInChildren<SkinnedMeshRenderer>();original=visual.localPosition;}
        if(skin==null)return;visual.localPosition=original;
        if(sample==null)sample=new Mesh{name="Sleeping body contact"};
        skin.BakeMesh(sample,true);sample.GetVertices(vertices);
        var tag=visual.GetComponentInParent<CatBreedVisualTag>();var entry=CatBreedCatalog.Load().Find(tag!=null?tag.BreedId:CatBreedCatalog.DefaultBreedId);
        if(entry==null)return;
        float min=float.PositiveInfinity;
        foreach(int i in entry.ContactVertexIndices)if(i<vertices.Count)min=Mathf.Min(min,skin.transform.TransformPoint(vertices[i]).y);
        if(float.IsInfinity(min))return;
        visual.position+=Vector3.up*(sleep.SleepSurface.position.y-min+.005f);adjusted=true;
    }
    void Restore(){if(adjusted && visual!=null)visual.localPosition=original;adjusted=false;}
    void OnDisable(){Restore();}
    void OnDestroy(){Restore();if(sample!=null)Destroy(sample);}
}
