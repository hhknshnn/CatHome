using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public sealed partial class CatMeshContactSurface
{
    const string PackedResourceName="Home/CatMeshContactSurfaceData";
    const string PackedAssetPath="Assets/Resources/Home/CatMeshContactSurfaceData.bytes";
    [SerializeField] string packedResource;
    [SerializeField] Mesh[] packedMeshes=Array.Empty<Mesh>();
    [NonSerialized] Geometry[] decodedGeometry;
    [NonSerialized] ResourceRequest packedRequest;
    [NonSerialized] Task<Geometry[]> decodeTask;
    [NonSerialized] bool packedFailed;
    bool PackedPending=>!string.IsNullOrEmpty(packedResource)&&decodedGeometry==null&&!packedFailed;
    Geometry[] GeometryData=>decodedGeometry??geometries;

    // Unity resource access stays on the main thread. Only immutable bytes and
    // managed geometry arrays are decoded on the worker; it calls no Unity API.
    bool EnsurePacked(bool synchronous)
    {
        if(string.IsNullOrEmpty(packedResource)||decodedGeometry!=null)return true;
        if(packedFailed)return false;
        try
        {
            if(decodeTask==null)
            {
                TextAsset payload;
                if(synchronous)payload=Resources.Load<TextAsset>(packedResource);
                else
                {
                    if(packedRequest==null)packedRequest=Resources.LoadAsync<TextAsset>(packedResource);
                    if(!packedRequest.isDone)return false;
                    payload=packedRequest.asset as TextAsset;
                }
                if(payload==null)throw new InvalidDataException("Missing packed contact geometry: "+packedResource);
                byte[] bytes=payload.bytes;Mesh[] meshes=packedMeshes;
                Resources.UnloadAsset(payload);packedRequest=null;
                if(synchronous)decodedGeometry=DecodeGeometry(bytes,meshes);
                else decodeTask=Task.Run(()=>DecodeGeometry(bytes,meshes));
            }
            if(decodedGeometry==null)
            {
                if(!synchronous&&!decodeTask.IsCompleted)return false;
                decodedGeometry=decodeTask.GetAwaiter().GetResult();decodeTask=null;
            }
            byMesh=null;return true;
        }
        catch(Exception error)
        {
            packedFailed=true;Debug.LogError("Contact geometry could not load: "+error.Message,this);return false;
        }
    }

    static Geometry[] DecodeGeometry(byte[] bytes,Mesh[] meshes)
    {
        using(var stream=new MemoryStream(bytes,false))
        using(var reader=new BinaryReader(stream))
        {
            if(reader.ReadInt32()!=0x43484731)throw new InvalidDataException("Unknown contact geometry format.");
            int count=ReadCount(reader,4096);
            if(count!=meshes.Length)throw new InvalidDataException("Contact geometry mesh count differs.");
            var result=new Geometry[count];
            for(int i=0;i<count;i++)
            {
                var geometry=new Geometry{mesh=meshes[i]};
                geometry.vertices=new Vector3[ReadCount(reader,4000000)];
                for(int v=0;v<geometry.vertices.Length;v++)geometry.vertices[v]=ReadVector(reader);
                geometry.triangles=ReadIndices(reader);
                geometry.nodes=new BvhNode[ReadCount(reader,4000000)];
                for(int n=0;n<geometry.nodes.Length;n++)
                {
                    var bounds=new Bounds();bounds.center=ReadVector(reader);bounds.extents=ReadVector(reader);
                    geometry.nodes[n]=new BvhNode{bounds=bounds,left=reader.ReadInt32(),right=reader.ReadInt32(),first=reader.ReadInt32(),count=reader.ReadInt32()};
                }
                geometry.triangleOrder=ReadIndices(reader);result[i]=geometry;
            }
            if(stream.Position!=stream.Length)throw new InvalidDataException("Unexpected trailing contact geometry.");
            return result;
        }
    }
    static int ReadCount(BinaryReader reader,int maximum)
    {int count=reader.ReadInt32();if(count<0||count>maximum)throw new InvalidDataException("Invalid contact geometry count.");return count;}
    static int[] ReadIndices(BinaryReader reader)
    {var values=new int[ReadCount(reader,12000000)];for(int i=0;i<values.Length;i++)values[i]=reader.ReadInt32();return values;}
    static Vector3 ReadVector(BinaryReader reader)=>new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());

#if UNITY_EDITOR
    static byte[] EncodeGeometry(Geometry[] values)
    {
        using(var stream=new MemoryStream())
        using(var writer=new BinaryWriter(stream))
        {
            writer.Write(0x43484731);writer.Write(values.Length);
            foreach(var geometry in values)
            {
                writer.Write(geometry.vertices.Length);foreach(var v in geometry.vertices)WriteVector(writer,v);
                WriteIndices(writer,geometry.triangles);writer.Write(geometry.nodes.Length);
                foreach(var node in geometry.nodes)
                {WriteVector(writer,node.bounds.center);WriteVector(writer,node.bounds.extents);writer.Write(node.left);writer.Write(node.right);writer.Write(node.first);writer.Write(node.count);}
                WriteIndices(writer,geometry.triangleOrder);
            }
            writer.Flush();return stream.ToArray();
        }
    }
    static void WriteVector(BinaryWriter writer,Vector3 value){writer.Write(value.x);writer.Write(value.y);writer.Write(value.z);}
    static void WriteIndices(BinaryWriter writer,int[] values){writer.Write(values.Length);foreach(int value in values)writer.Write(value);}
    void WritePacked(Geometry[] source)
    {
        var meshes=new Mesh[source.Length];for(int i=0;i<source.Length;i++)meshes[i]=source[i].mesh;
        byte[] bytes=EncodeGeometry(source),roundtrip=EncodeGeometry(DecodeGeometry(bytes,meshes));
        if(bytes.Length!=roundtrip.Length)throw new InvalidDataException("Contact geometry round trip changed its size.");
        for(int i=0;i<bytes.Length;i++)if(bytes[i]!=roundtrip[i])throw new InvalidDataException("Contact geometry round trip changed byte "+i);
        File.WriteAllBytes(PackedAssetPath,bytes);
        UnityEditor.AssetDatabase.ImportAsset(PackedAssetPath,UnityEditor.ImportAssetOptions.ForceSynchronousImport);
        packedResource=PackedResourceName;packedMeshes=meshes;decodedGeometry=source;geometries=Array.Empty<Geometry>();
        packedRequest=null;decodeTask=null;packedFailed=false;byMesh=null;
    }
    public static string PackExistingGeometry()
    {
        if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Pack outside Play Mode.");
        var asset=UnityEditor.AssetDatabase.LoadAssetAtPath<CatMeshContactSurface>(AssetPath);
        if(asset==null||!asset.EnsurePacked(true))throw new InvalidOperationException("Source contact geometry unavailable.");
        var source=asset.GeometryData;asset.WritePacked(source);
        UnityEditor.EditorUtility.SetDirty(asset);UnityEditor.AssetDatabase.SaveAssetIfDirty(asset);loaded=asset;
        return source.Length+" meshes; exact binary round trip passed; "+new FileInfo(PackedAssetPath).Length+" bytes.";
    }
#endif
}
