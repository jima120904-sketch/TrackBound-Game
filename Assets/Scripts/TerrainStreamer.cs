using System;
using System.Collections;
using System.Collections.Generic;
using Trackbound.Core;
using UnityEngine;

public sealed class TerrainStreamer : MonoBehaviour
{
    private const int ChunkSize = 128;
    private const int Radius = 8;
    private const int Resolution = 17;

    private sealed class TerrainChunk
    {
        public GameObject Root;
        public Matrix4x4[] Trunks;
        public Matrix4x4[] Canopies;
    }

    private readonly Dictionary<Vector2Int, TerrainChunk> _chunks = new Dictionary<Vector2Int, TerrainChunk>();
    private readonly Matrix4x4[] _instanceBatch = new Matrix4x4[1023];
    private Transform _train;
    private Material _terrainMaterial;
    private Material _trunkMaterial;
    private Material _foliageMaterial;
    private Mesh _trunkMesh;
    private Mesh _canopyMesh;
    private Coroutine _refresh;
    private Vector2Int _center = new Vector2Int(int.MinValue, int.MinValue);
    private int _seed;

    public void Initialize(Transform train, Material terrainMaterial, Material trunkMaterial, Material foliageMaterial, int seed)
    {
        _train = train;
        _terrainMaterial = terrainMaterial;
        _trunkMaterial = trunkMaterial;
        _foliageMaterial = foliageMaterial;
        _seed = seed;

        var trunkSource = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        _trunkMesh = trunkSource.GetComponent<MeshFilter>().sharedMesh;
        Destroy(trunkSource);
        _canopyMesh = CreateConeMesh();
    }

    private void Start()
    {
        RefreshIfNeeded(true);
    }

    private void Update()
    {
        RefreshIfNeeded(false);
        DrawTrees(true);
        DrawTrees(false);
    }

    private void RefreshIfNeeded(bool force)
    {
        if (_train == null)
        {
            return;
        }

        var current = new Vector2Int(
            Mathf.FloorToInt(_train.position.x / ChunkSize),
            Mathf.FloorToInt(_train.position.z / ChunkSize));
        if (!force && current == _center)
        {
            return;
        }

        _center = current;
        if (_refresh != null)
        {
            StopCoroutine(_refresh);
        }
        _refresh = StartCoroutine(BuildRegion(current));
    }

    private IEnumerator BuildRegion(Vector2Int center)
    {
        var required = new HashSet<Vector2Int>();
        var coordinates = new List<Vector2Int>((Radius * 2 + 1) * (Radius * 2 + 1));
        for (var z = -Radius; z <= Radius; z++)
        {
            for (var x = -Radius; x <= Radius; x++)
            {
                var coordinate = new Vector2Int(center.x + x, center.y + z);
                required.Add(coordinate);
                coordinates.Add(coordinate);
            }
        }
        coordinates.Sort((a, b) =>
        {
            var da = (a - center).sqrMagnitude;
            var db = (b - center).sqrMagnitude;
            return da.CompareTo(db);
        });

        var stale = new List<Vector2Int>();
        foreach (var pair in _chunks)
        {
            if (!required.Contains(pair.Key))
            {
                Destroy(pair.Value.Root);
                stale.Add(pair.Key);
            }
        }
        foreach (var coordinate in stale)
        {
            _chunks.Remove(coordinate);
        }

        var builtThisFrame = 0;
        foreach (var coordinate in coordinates)
        {
            if (!_chunks.ContainsKey(coordinate))
            {
                _chunks.Add(coordinate, CreateChunk(coordinate));
                builtThisFrame++;
                if (builtThisFrame >= 5)
                {
                    builtThisFrame = 0;
                    yield return null;
                }
            }
        }

        UpdateCollision(center);
        _refresh = null;
    }

    private TerrainChunk CreateChunk(Vector2Int coordinate)
    {
        var root = new GameObject($"Terrain {coordinate.x},{coordinate.y}");
        root.transform.SetParent(transform, false);
        root.transform.position = new Vector3(coordinate.x * ChunkSize, 0, coordinate.y * ChunkSize);

        var vertices = new Vector3[Resolution * Resolution];
        var uv = new Vector2[vertices.Length];
        var triangles = new int[(Resolution - 1) * (Resolution - 1) * 6];
        var cellSize = ChunkSize / (float)(Resolution - 1);
        for (var z = 0; z < Resolution; z++)
        {
            for (var x = 0; x < Resolution; x++)
            {
                var worldX = coordinate.x * ChunkSize + x * cellSize;
                var worldZ = coordinate.y * ChunkSize + z * cellSize;
                var roadBlend = SmoothStep(12, 180, Mathf.Abs(worldX));
                var height = (float)ProceduralTerrain.Height(worldX, worldZ, _seed) * roadBlend;
                var index = z * Resolution + x;
                vertices[index] = new Vector3(x * cellSize, height, z * cellSize);
                uv[index] = new Vector2(x / (float)(Resolution - 1), z / (float)(Resolution - 1));
            }
        }

        var triangle = 0;
        for (var z = 0; z < Resolution - 1; z++)
        {
            for (var x = 0; x < Resolution - 1; x++)
            {
                var a = z * Resolution + x;
                var b = a + Resolution;
                triangles[triangle++] = a;
                triangles[triangle++] = b;
                triangles[triangle++] = a + 1;
                triangles[triangle++] = a + 1;
                triangles[triangle++] = b;
                triangles[triangle++] = b + 1;
            }
        }

        var mesh = new Mesh { name = $"Terrain mesh {coordinate.x},{coordinate.y}" };
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        root.AddComponent<MeshFilter>().sharedMesh = mesh;
        root.AddComponent<MeshRenderer>().sharedMaterial = _terrainMaterial;

        var trees = CreateTreeMatrices(coordinate);
        return new TerrainChunk { Root = root, Trunks = trees.Item1, Canopies = trees.Item2 };
    }

    private Tuple<Matrix4x4[], Matrix4x4[]> CreateTreeMatrices(Vector2Int coordinate)
    {
        var random = new System.Random(unchecked(_seed ^ (coordinate.x * 73856093) ^ (coordinate.y * 19349663)));
        var trunks = new List<Matrix4x4>(6);
        var canopies = new List<Matrix4x4>(6);
        for (var i = 0; i < 6; i++)
        {
            var localX = (float)random.NextDouble() * ChunkSize;
            var localZ = (float)random.NextDouble() * ChunkSize;
            var worldX = coordinate.x * ChunkSize + localX;
            var worldZ = coordinate.y * ChunkSize + localZ;
            if (Mathf.Abs(worldX) < 24)
            {
                continue;
            }

            var roadBlend = SmoothStep(12, 180, Mathf.Abs(worldX));
            var height = (float)ProceduralTerrain.Height(worldX, worldZ, _seed) * roadBlend;
            var treeHeight = Mathf.Lerp(10, 25, (float)random.NextDouble());
            var position = new Vector3(worldX, height, worldZ);
            trunks.Add(Matrix4x4.TRS(position + Vector3.up * (treeHeight * 0.18f), Quaternion.identity, new Vector3(0.42f, treeHeight * 0.2f, 0.42f)));
            canopies.Add(Matrix4x4.TRS(position + Vector3.up * (treeHeight * 0.55f), Quaternion.identity, new Vector3(treeHeight * 0.62f, treeHeight * 0.72f, treeHeight * 0.62f)));
        }

        return Tuple.Create(trunks.ToArray(), canopies.ToArray());
    }

    private void UpdateCollision(Vector2Int center)
    {
        foreach (var pair in _chunks)
        {
            var offset = pair.Key - center;
            var near = Mathf.Abs(offset.x) <= 1 && Mathf.Abs(offset.y) <= 1;
            var collider = pair.Value.Root.GetComponent<MeshCollider>();
            if (near && collider == null)
            {
                collider = pair.Value.Root.AddComponent<MeshCollider>();
                collider.sharedMesh = pair.Value.Root.GetComponent<MeshFilter>().sharedMesh;
            }
            else if (!near && collider != null)
            {
                Destroy(collider);
            }
        }
    }

    private void DrawTrees(bool trunks)
    {
        var mesh = trunks ? _trunkMesh : _canopyMesh;
        var material = trunks ? _trunkMaterial : _foliageMaterial;
        if (mesh == null || material == null)
        {
            return;
        }

        var count = 0;
        foreach (var chunk in _chunks.Values)
        {
            var matrices = trunks ? chunk.Trunks : chunk.Canopies;
            foreach (var matrix in matrices)
            {
                _instanceBatch[count++] = matrix;
                if (count == _instanceBatch.Length)
                {
                    Graphics.DrawMeshInstanced(mesh, 0, material, _instanceBatch, count);
                    count = 0;
                }
            }
        }
        if (count > 0)
        {
            Graphics.DrawMeshInstanced(mesh, 0, material, _instanceBatch, count);
        }
    }

    private static Mesh CreateConeMesh()
    {
        const int sides = 10;
        var vertices = new Vector3[sides + 2];
        var triangles = new int[sides * 6];
        vertices[0] = new Vector3(0, 1, 0);
        vertices[sides + 1] = Vector3.zero;
        for (var i = 0; i < sides; i++)
        {
            var angle = i * Mathf.PI * 2 / sides;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * 0.5f, 0, Mathf.Sin(angle) * 0.5f);
            var next = (i + 1) % sides + 1;
            var offset = i * 6;
            triangles[offset] = 0;
            triangles[offset + 1] = i + 1;
            triangles[offset + 2] = next;
            triangles[offset + 3] = sides + 1;
            triangles[offset + 4] = next;
            triangles[offset + 5] = i + 1;
        }

        var mesh = new Mesh { name = "Instanced pine crown" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static float SmoothStep(float min, float max, float value)
    {
        var t = Mathf.Clamp01((value - min) / (max - min));
        return t * t * (3 - 2 * t);
    }
}