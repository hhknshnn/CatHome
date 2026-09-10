# Adapt the exact installed URP shader, retaining its lighting/depth contracts.
# The only custom pixel operation is one neutral material-detail sample.
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$package = Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Library/PackageCache') -Directory | Where-Object { $_.Name -like 'com.unity.render-pipelines.universal@*' } | Select-Object -First 1
$shaderRoot = Join-Path $projectRoot 'Assets/Art/ModernPolish/Shaders'
New-Item -ItemType Directory -Path $shaderRoot -Force | Out-Null
$shader = Get-Content -LiteralPath (Join-Path $package.FullName 'Shaders/Lit.shader') -Raw
$shader = $shader.Replace('Shader "Universal Render Pipeline/Lit"', 'Shader "CatHome/Modern Surface"')
$shader = $shader.Replace('#include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"', '#include "ModernSurfaceForwardPass.hlsl"')
$shader = $shader.Replace('CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.LitShader"', '// Keep packed detail channels independent of the Lit detail keyword inspector.')
$shader = $shader.Replace('#pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED', '// Modern surface detail is always one projected packed sample.')
$shader = $shader.Replace('_DetailAlbedoMap("Detail Albedo x2", 2D)', '_DetailAlbedoMap("Packed surface detail (linear)", 2D)')
$shader = $shader.Replace('_DetailNormalMapScale("Scale", Range(0.0, 2.0)) = 1.0', '_DetailNormalMapScale("Micro relief in metres", Range(0.0, 0.01)) = 0.001')
$shader = "// Derived from the installed URP Lit shader ($($package.Name)); Unity Companion License.`n// Copyright Unity Technologies ApS; see URP-LICENSE.md beside this file.`n// Local delta: one packed projected surface sample in the forward material path.`n" + $shader
[IO.File]::WriteAllText((Join-Path $shaderRoot 'ModernSurface.shader'), $shader)
$forward = Get-Content -LiteralPath (Join-Path $package.FullName 'Shaders/LitForwardPass.hlsl') -Raw
$helper = @'

// Object-metric projection works for constant-palette UVs and moving scenery.
// RGB = half neutral albedo, roughness variation, and baked micro-height.
// One texture sample; no triplanar triple sampling, extra light or geometry.
void ApplyModernSurface(Varyings input, inout SurfaceData surfaceData, inout InputData inputData)
{
    float4x4 objectMatrix = GetObjectToWorldMatrix();
    float3 metricScale = float3(length(objectMatrix._m00_m10_m20),
        length(objectMatrix._m01_m11_m21), length(objectMatrix._m02_m12_m22));
    float3 p = TransformWorldToObject(input.positionWS) * metricScale;
    float3 localNormal = normalize(mul(inputData.normalWS, (float3x3)objectMatrix));
    float3 weights = abs(localNormal);
    float2 projected = weights.y >= weights.x && weights.y >= weights.z ? p.xz :
        (weights.x >= weights.z ? p.zy : p.xy);
    projected = projected * _DetailAlbedoMap_ST.xy + _DetailAlbedoMap_ST.zw;
    half3 detail = SAMPLE_TEXTURE2D(_DetailAlbedoMap, sampler_DetailAlbedoMap, projected).rgb;
    surfaceData.albedo *= lerp(1.0h, detail.r * 2.0h, _DetailAlbedoMapScale);
    surfaceData.smoothness = saturate(surfaceData.smoothness * (.76h + detail.g * .48h));

    // Surface-gradient bump does not require tangent UVs. Clamp its contribution
    // at projection seams and tiny triangles; existing macro normals remain.
    float3 dpdx = ddx(input.positionWS), dpdy = ddy(input.positionWS);
    float3 r1 = cross(dpdy, inputData.normalWS), r2 = cross(inputData.normalWS, dpdx);
    float determinant = dot(dpdx, r1);
    float3 gradient = sign(determinant) * (ddx(detail.b) * r1 + ddy(detail.b) * r2) * _DetailNormalMapScale;
    float area = max(abs(determinant), 1e-10);
    gradient *= min(1.0, area * .30 / max(length(gradient), 1e-10));
    inputData.normalWS = normalize(area * inputData.normalWS - gradient);
}

'@
$forward = $forward.Replace('// Used in Standard (Physically Based) shader' + "`r`n" + 'void LitPassFragment(', $helper + '// Used in Standard (Physically Based) shader' + "`r`n" + 'void LitPassFragment(')
if (!$forward.Contains('void ApplyModernSurface')) {
    $forward = $forward.Replace('void LitPassFragment(', $helper + 'void LitPassFragment(')
}
$forward = $forward.Replace('    InitializeBakedGIData(input, inputData);', "    ApplyModernSurface(input, surfaceData, inputData);`n    InitializeBakedGIData(input, inputData);")
$forward = "// Derived from installed Unity URP LitForwardPass.hlsl; Unity Companion License.`n// Copyright Unity Technologies ApS; see URP-LICENSE.md beside this file.`n// The custom ApplyModernSurface block is Cat Home authored code.`n" + $forward
[IO.File]::WriteAllText((Join-Path $shaderRoot 'ModernSurfaceForwardPass.hlsl'), $forward)
Copy-Item -LiteralPath (Join-Path $package.FullName 'LICENSE.md') -Destination (Join-Path $shaderRoot 'URP-LICENSE.md') -Force
Write-Output "Modern surface shader generated from $($package.Name)."
