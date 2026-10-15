using UnityEngine;

internal static class TrackboundGeometry
{
    public static GameObject Primitive(
        PrimitiveType shape,
        string objectName,
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale,
        Material material,
        bool hasCollision = false)
    {
        var instance = GameObject.CreatePrimitive(shape);
        instance.name = objectName;
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = localPosition;
        instance.transform.localScale = localScale;

        var renderer = instance.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;

        if (!hasCollision)
        {
            var collider = instance.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }
        }

        return instance;
    }

    public static Material Material(string materialName, Color color, float metallic, float smoothness)
    {
        var shader = Shader.Find("HDRP/Lit") ?? Shader.Find("Standard");
        var material = new Material(shader) { name = materialName };

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", metallic);
        }
        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", smoothness);
        }
        material.enableInstancing = true;
        return material;
    }
}