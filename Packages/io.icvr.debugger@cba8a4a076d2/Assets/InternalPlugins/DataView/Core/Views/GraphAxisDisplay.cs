using System.Linq;
using UnityEngine;
using TMPro;

public class GraphAxisDisplay : MonoBehaviour
{
    [Header("Graph Settings")]
    [SerializeField]
    private RectTransform graphContainer;
    
    [SerializeField]
    private PerformanceDataSource dataSource;

    [Header("UI References")]
    [SerializeField]
    private RectTransform marker30fps;

    [SerializeField]
    private RectTransform marker60fps;

    [SerializeField]
    private TMP_Text maxValueLabel;

// Benchmark values in milliseconds.
    private const float TARGET_MS_30 = 33.3f;
    private const float TARGET_MS_60 = 16.6f;

    private void Update()
    {
        RefreshAxis();
    }

    /// <summary>
    /// Refreshes the positions of the FPS markers and updates the max value label based on the current data source.
    /// </summary>
    public void RefreshAxis()
    {
        if (graphContainer == null)
        {
            Debug.LogWarning("Graph container is not assigned.", this);
            return;
        }
        
        var graphMaxValue = 50f;
        if (dataSource != null && dataSource.Samples is { Count: > 0 })
        {
            graphMaxValue = dataSource.Samples.Max();
        }

        var containerHeight = graphContainer.rect.height;
        var normalized30 = Mathf.Clamp01(TARGET_MS_30 / graphMaxValue);
        var normalized60 = Mathf.Clamp01(TARGET_MS_60 / graphMaxValue);

        // Update marker positions along the y-axis.
        if (marker30fps != null)
        {
            if (TARGET_MS_30 > (graphMaxValue - 5f))
            {
                marker30fps.gameObject.SetActive(false);
            }
            else
            {
                marker30fps.gameObject.SetActive(true);
                marker30fps.anchoredPosition = new Vector2(marker30fps.anchoredPosition.x, normalized30 * containerHeight);
            }
        }

        if (marker60fps != null)
        {
            if (TARGET_MS_60 > (graphMaxValue - 5f))
            {
                marker60fps.gameObject.SetActive(false);
            }
            else
            {
                marker60fps.gameObject.SetActive(true);
                marker60fps.anchoredPosition = new Vector2(marker60fps.anchoredPosition.x, normalized60 * containerHeight);
            }
        }

        // Update the TMP_Text label with the maximum sample value.
        if (maxValueLabel != null)
        {
            maxValueLabel.text = $"{graphMaxValue:F1}ms\n({(graphMaxValue > 0 ? 1000 / graphMaxValue : 0):.#} FPS)";
        }
    }
}