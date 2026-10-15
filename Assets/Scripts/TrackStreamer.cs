using System.Collections.Generic;
using UnityEngine;

public sealed class TrackStreamer : MonoBehaviour
{
    private const float SegmentLength = 128f;
    private readonly Dictionary<int, GameObject> _segments = new Dictionary<int, GameObject>();
    private Material _railMaterial;
    private Material _sleeperMaterial;
    private Mesh _cubeMesh;

    public void Initialize(Material railMaterial, Material sleeperMaterial)
    {
        _railMaterial = railMaterial;
        _sleeperMaterial = sleeperMaterial;
        var source = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _cubeMesh = source.GetComponent<MeshFilter>().sharedMesh;
        Destroy(source);
    }

    private void Update()
    {
        if (_cubeMesh == null || TrackboundBootstrap.Instance == null || TrackboundBootstrap.Instance.Train == null)
        {
            return;
        }

        var trainZ = TrackboundBootstrap.Instance.Train.transform.position.z;
        var center = Mathf.FloorToInt(trainZ / SegmentLength);
        var required = new HashSet<int>();
        for (var index = center - 8; index <= center + 16; index++)
        {
            required.Add(index);
            if (!_segments.ContainsKey(index))
            {
                CreateSegment(index);
            }
        }

        var stale = new List<int>();
        foreach (var segment in _segments)
        {
            if (!required.Contains(segment.Key))
            {
                Destroy(segment.Value);
                stale.Add(segment.Key);
            }
        }
        foreach (var key in stale)
        {
            _segments.Remove(key);
        }
    }

    private void CreateSegment(int index)
    {
        var root = new GameObject($"Rail segment {index}");
        root.transform.SetParent(transform, false);
        root.transform.position = new Vector3(0, 0, index * SegmentLength);

        var rails = new List<CombineInstance>(2);
        foreach (var side in new[] { -1, 1 })
        {
            rails.Add(Combine(new Vector3(side * 0.76f, 0.38f, 0), new Vector3(0.16f, 0.28f, SegmentLength)));
        }
        AddCombinedMesh(root.transform, "Twin steel rails", rails, _railMaterial);

        var sleepers = new List<CombineInstance>(40);
        for (var i = 0; i < 40; i++)
        {
            var z = -SegmentLength * 0.5f + (i + 0.5f) * SegmentLength / 40f;
            sleepers.Add(Combine(new Vector3(0, 0.12f, z), new Vector3(3.1f, 0.18f, 0.26f)));
        }
        AddCombinedMesh(root.transform, "Timber sleepers", sleepers, _sleeperMaterial);
        _segments.Add(index, root);
    }

    private CombineInstance Combine(Vector3 position, Vector3 scale)
    {
        return new CombineInstance
        {
            mesh = _cubeMesh,
            transform = Matrix4x4.TRS(position, Quaternion.identity, scale)
        };
    }

    private static void AddCombinedMesh(Transform parent, string objectName, List<CombineInstance> instances, Material material)
    {
        var child = new GameObject(objectName);
        child.transform.SetParent(parent, false);
        var mesh = new Mesh { name = objectName };
        mesh.CombineMeshes(instances.ToArray(), true, true);
        child.AddComponent<MeshFilter>().sharedMesh = mesh;
        child.AddComponent<MeshRenderer>().sharedMaterial = material;
    }
}