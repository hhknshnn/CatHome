using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

/// <summary>
/// Test-only snapshots of the original collision mesh. Editor readback bypasses
/// the imported mesh's runtime readability flag without changing that flag,
/// cloning/cooking a collider, or retaining native allocations between samples.
/// Clear between fixtures; animated/deformed mesh data must not use this cache.
/// </summary>
internal sealed class QaMeshTopologyCache
{
    internal sealed class Topology
    {
        internal readonly Vector3[] vertices;
        internal readonly int[] triangles;
        internal Topology(Vector3[] vertices, int[] triangles)
        { this.vertices = vertices; this.triangles = triangles; }
    }

    readonly Dictionary<Mesh, Topology> cache = new Dictionary<Mesh, Topology>();
    internal int Count => cache.Count;
    internal void Clear() => cache.Clear();

    internal Topology Get(Mesh mesh)
    {
        if (mesh == null) throw new ArgumentNullException(nameof(mesh));
        if (cache.TryGetValue(mesh, out var result)) return result;
        result = Read(mesh);
        cache.Add(mesh, result);
        return result;
    }

    static Topology Read(Mesh mesh)
    {
#if UNITY_EDITOR
        using (var snapshot = UnityEditor.MeshUtility.AcquireReadOnlyMeshData(mesh))
#else
        using (var snapshot = Mesh.AcquireReadOnlyMeshData(mesh))
#endif
        {
            var data = snapshot[0];
            Vector3[] vertices;
            using (var nativeVertices = new NativeArray<Vector3>(data.vertexCount, Allocator.Temp))
            {
                data.GetVertices(nativeVertices);
                vertices = nativeVertices.ToArray();
            }

            int indexCount = 0;
            for (int sub = 0; sub < data.subMeshCount; sub++)
            {
                var descriptor = data.GetSubMesh(sub);
                if (descriptor.topology != MeshTopology.Triangles || descriptor.indexCount % 3 != 0)
                    throw new InvalidOperationException("Collision metrology requires triangle topology: " + mesh.name + ", submesh " + sub);
                indexCount = checked(indexCount + descriptor.indexCount);
            }
            var triangles = new int[indexCount];
            int offset = 0;
            // mesh.triangles concatenates submeshes in this same order. Apply
            // each baseVertex so RaycastHit.triangleIndex indexes the original
            // mesh vertices for both 16-bit and 32-bit index buffers.
            for (int sub = 0; sub < data.subMeshCount; sub++)
            {
                int count = data.GetSubMesh(sub).indexCount;
                using (var indices = new NativeArray<int>(count, Allocator.Temp))
                {
                    data.GetIndices(indices, sub, applyBaseVertex: true);
                    for (int i = 0; i < count; i++)
                    {
                        int index = indices[i];
                        if ((uint)index >= (uint)vertices.Length)
                            throw new InvalidOperationException("Collision mesh index is outside its original vertices: " + mesh.name);
                        triangles[offset + i] = index;
                    }
                }
                offset += count;
            }
            return new Topology(vertices, triangles);
        }
    }
}
