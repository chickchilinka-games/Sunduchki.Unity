using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Performance Filled Graph")]
public class PerformanceFilledGraph : Graphic
{
    public PerformanceDataSource dataSource;

    protected void Update()
    {
        if (dataSource != null)
            SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (dataSource == null || dataSource.Samples.Count < 2)
            return;

        var rect = GetPixelAdjustedRect();
        var count = dataSource.Samples.Count;
        var xStep = rect.width / (count - 1);
        var maxMs = dataSource.Samples.Max();

        for (var i = 0; i < count - 1; i++)
        {
            var ms1 = dataSource.Samples[i];
            var ms2 = dataSource.Samples[i + 1];

            var normalized1 = Mathf.Clamp01(ms1 / maxMs);
            var normalized2 = Mathf.Clamp01(ms2 / maxMs);

            var point1 = new Vector2(rect.x + i * xStep, rect.y + normalized1 * rect.height);
            var point2 = new Vector2(rect.x + (i + 1) * xStep, rect.y + normalized2 * rect.height);

            var bottom1 = new Vector2(rect.x + i * xStep, rect.y);
            var bottom2 = new Vector2(rect.x + (i + 1) * xStep, rect.y);

            var startIndex = vh.currentVertCount;
            var vertex = UIVertex.simpleVert;
            vertex.color = color;

            vertex.position = bottom1;
            vertex.uv0 = new Vector2((bottom1.x - rect.x) / rect.width, (bottom1.y - rect.y) / rect.height);
            vh.AddVert(vertex);

            vertex.position = point1;
            vertex.uv0 = new Vector2((point1.x - rect.x) / rect.width, (point1.y - rect.y) / rect.height);
            vh.AddVert(vertex);

            vertex.position = point2;
            vertex.uv0 = new Vector2((point2.x - rect.x) / rect.width, (point2.y - rect.y) / rect.height);
            vh.AddVert(vertex);

            vertex.position = bottom2;
            vertex.uv0 = new Vector2((bottom2.x - rect.x) / rect.width, (bottom2.y - rect.y) / rect.height);
            vh.AddVert(vertex);

            vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            vh.AddTriangle(startIndex, startIndex + 2, startIndex + 3);
        }
    }
}