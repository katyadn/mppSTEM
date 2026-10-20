using Microclimate.Domain.Enums;
using Microclimate.Domain.Thresholds;
using Microclimate.Domain.ValueObjects;

namespace Microclimate.Domain.Metrics;

/// <summary>Усі властивості метрики в одному місці: одиниця, фізичні межі датчика, пороги за замовчуванням.</summary>
public sealed record MetricDefinition(MetricType Metric, string Unit, MetricRange PhysicalRange, ThresholdLimits DefaultLimits);
