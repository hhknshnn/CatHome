using UnityEngine;

// Compatibility for existing head diagnostics. All four source body regions
// now share the same complete-triangle partition and unchanged fitting shell.
public static class CatJumpHeadCoverageBuilder
{
    public static int[] HeadTriangles(int[] triangles, int[] groups) =>
        CatJumpSurfaceCoverageBuilder.RegionTriangles(triangles, groups, 3);
    public static CatJumpSurfaceCoverageBuilder.Result Fit(Vector3[] vertices, int[] triangles) =>
        CatJumpSurfaceCoverageBuilder.Fit(vertices, triangles, "head");
    public static float TriangleContainment(Vector3[] vertices, int[] triangles, CatBodyGuardCatalog.Probe[] probes) =>
        CatJumpSurfaceCoverageBuilder.TriangleContainment(vertices, triangles, probes);
}
