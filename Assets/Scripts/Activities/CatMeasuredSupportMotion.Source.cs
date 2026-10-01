using UnityEngine;

public sealed partial class CatMeasuredSupportMotion
{
    CatSupportedLimbSkin preparationSource;
    string preparationBreed;
    CatActivity preparationOwner;
    CatBreedVisualTag preparationTag;
    // A future source sample must never overwrite the live frame's captured
    // supportedSkin. This separate per-actor scratch holds no physics result.
    public CatSupportedLimbSkin CapturePreparationSource(string breed,CatCareSkinCatalog.Profile profile,string[] paths,
        Matrix4x4 mapping,Matrix4x4[] sourceMatrices)
    {
        var tag=GetComponentInChildren<CatBreedVisualTag>();
        if(tag==null||tag.BreedId!=breed||profile==null)return null;
        if(preparationSource==null||preparationBreed!=breed||preparationOwner!=owner||preparationTag!=tag||!preparationSource.MatchesSource(profile,paths))
        {preparationSource=CatSupportedLimbSkin.BindSource(profile,paths);preparationBreed=breed;preparationOwner=owner;preparationTag=tag;}
        return preparationSource!=null&&preparationSource.CaptureSource(mapping,sourceMatrices)?preparationSource:null;
    }
}
